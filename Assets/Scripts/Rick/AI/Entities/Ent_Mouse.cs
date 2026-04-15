using UnityEngine;

/// <summary>
/// Mouse AI — simple prey. Cannot climb walls or ceilings.
///
/// Uses standard Unity gravity (gravityScale 1) and direct horizontal velocity.
///
/// Detection raycasts:
///   Downward (single)   — ground check, prevents walking off edges
///   Forward  (single)   — wall detection, flips direction
///   Edge check          — forward + offset downward, detects platform end
///   OverlapCircle       — predator proximity check (simpler than raycasts for radius)
///
/// Ecosystem lifecycle:
///   WANDER ⟷ FLEE
///
/// Setup requirements:
///   - solidLayer    → terrain/walls
///   - predatorLayer → snake layer
///   - Register in EntityPoolManager.
/// </summary>
public class Ent_Mouse : AIAgent
{
    // ── Inspector ─────────────────────────────────────────────────────────

    [Header("Mouse Speeds")]
    public float wanderSpeed = 2f;
    public float fleeSpeed   = 5f;

    [Header("Ground & Wall Detection")]
    [Tooltip("Length of the single downward raycast for ground and edge detection.")]
    public float groundCheckDist = 0.4f;

    [Tooltip("Forward raycast length for wall detection.")]
    public float wallCheckDist = 0.3f;

    [Header("Predator Detection")]
    [Tooltip("Layer snakes live on.")]
    public LayerMask predatorLayer;

    [Tooltip("Radius within which the mouse detects a predator and switches to Flee.")]
    public float fleeDetectionRadius = 3f;

    [Tooltip("Seconds without a predator in range before returning to Wander.")]
    public float fleeCooldown = 1.5f;

    // ── Runtime (read by nested states) ──────────────────────────────────

    [System.NonSerialized] public bool  IsGrounded;
    [System.NonSerialized] public float FleeTimer;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        Rb.gravityScale = 1f;   // mice fall normally, snakes do not
    }

    protected override void RegisterStates()
    {
        StateMachine.RegisterState(new MouseWanderState());
        StateMachine.RegisterState(new MouseFleeState());
    }

    protected override void OnSpawned()
    {
        Target        = null;
        FleeTimer     = 0f;
        IsGrounded    = false;
        FaceDirection = Random.value > 0.5f ? 1f : -1f;
        StateMachine.SetState(AIStateID.Wander);
    }

    // ── Detection helpers (called by states) ─────────────────────────────

    /// <summary>
    /// Single downward raycast. Updates IsGrounded and returns the result.
    /// </summary>
    public bool UpdateGroundCheck()
    {
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position, Vector2.down, groundCheckDist, solidLayer);
        IsGrounded = hit.collider != null;
        return IsGrounded;
    }

    /// <summary>Returns true if there is a wall directly ahead in FaceDirection.</summary>
    public bool IsWallAhead()
    {
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            new Vector2(FaceDirection, 0f),
            wallCheckDist,
            solidLayer);
        return hit.collider != null;
    }

    /// <summary>
    /// Returns true when the platform ends just ahead (no ground below the next step).
    /// Prevents the mouse from walking off ledges.
    /// </summary>
    public bool IsEdgeAhead()
    {
        // Offset the origin one wall-check ahead, then look straight down
        Vector2 origin = (Vector2)transform.position + new Vector2(FaceDirection * wallCheckDist, 0f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDist * 1.5f, solidLayer);
        return hit.collider == null;    // true = no ground ahead = edge
    }

    /// <summary>
    /// OverlapCircle scan for any predator within fleeDetectionRadius.
    /// More efficient than raycasts for an omnidirectional radius check.
    /// </summary>
    public bool TryDetectPredator(out Transform predator)
    {
        predator = null;
        Collider2D hit = Physics2D.OverlapCircle(transform.position, fleeDetectionRadius, predatorLayer);
        if (hit != null) { predator = hit.transform; return true; }
        return false;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        Vector2 pos = transform.position;
        float   dir = Application.isPlaying ? FaceDirection : 1f;

        // Ground check
        Gizmos.color = Application.isPlaying && IsGrounded
            ? Color.green : new Color(1f, 0.2f, 0.2f, 0.6f);
        Gizmos.DrawLine(pos, pos + Vector2.down * groundCheckDist);

        // Wall check
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(pos, pos + new Vector2(dir, 0f) * wallCheckDist);

        // Edge check origin
        Vector2 edgeOrigin = pos + new Vector2(dir * wallCheckDist, 0f);
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.8f);
        Gizmos.DrawLine(edgeOrigin, edgeOrigin + Vector2.down * groundCheckDist * 1.5f);

        // Flee detection radius
        Gizmos.color = new Color(1f, 0f, 0f, 0.12f);
        Gizmos.DrawWireSphere(pos, fleeDetectionRadius);
    }

    // ═════════════════════════════════════════════════════════════════════
    // STATES
    // ═════════════════════════════════════════════════════════════════════

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Random horizontal patrol.
    /// Flips on walls, edges, and occasionally at random.
    /// Switches to Flee the moment a predator enters the detection radius.
    /// </summary>
    private class MouseWanderState : AIState
    {
        public override AIStateID ID => AIStateID.Wander;

        private float _flipTimer;
        private const float RandomFlipInterval = 3.5f;

        public override void Enter(AIAgent agent)
        {
            _flipTimer = 0f;
        }

        public override void FixedTick(AIAgent agent)
        {
            var m = (Ent_Mouse)agent;
            m.UpdateGroundCheck();

            if (!m.IsGrounded)
            {
                // Airborne (fell off ledge) — let gravity work, don't fight it
                m.StopHorizontal();
                return;
            }

            // Obstacle avoidance: flip on wall or platform edge
            if (m.IsWallAhead() || m.IsEdgeAhead())
            {
                m.FaceDirection = -m.FaceDirection;
                _flipTimer = 0f;
            }

            // Occasional random direction change so the path looks natural
            _flipTimer += Time.fixedDeltaTime;
            if (_flipTimer >= RandomFlipInterval)
            {
                _flipTimer = 0f;
                if (Random.value < 0.35f)
                    m.FaceDirection = -m.FaceDirection;
            }

            m.Rb.linearVelocity = new Vector2(m.FaceDirection * m.wanderSpeed, m.Rb.linearVelocity.y);

            // Predator scan — single OverlapCircle is cheap enough to run every fixed frame
            if (m.TryDetectPredator(out Transform predator))
            {
                m.Target = predator;
                m.StateMachine.SetState(AIStateID.Flee);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Sprint in the opposite direction of the predator.
    /// Flips on walls (ignores edges — panic mode).
    /// Returns to Wander after fleeCooldown seconds with no predator detected.
    /// </summary>
    private class MouseFleeState : AIState
    {
        public override AIStateID ID => AIStateID.Flee;

        public override void Enter(AIAgent agent)
        {
            var m = (Ent_Mouse)agent;
            m.FleeTimer = 0f;

            // Initial flee direction: directly away from predator
            if (m.Target != null)
                m.FaceDirection = -Mathf.Sign(m.Target.position.x - m.transform.position.x);
        }

        public override void FixedTick(AIAgent agent)
        {
            var m = (Ent_Mouse)agent;
            m.UpdateGroundCheck();

            // Wall flip even while panicking (can't pass through walls)
            if (m.IsGrounded && m.IsWallAhead())
                m.FaceDirection = -m.FaceDirection;

            if (m.IsGrounded)
                m.Rb.linearVelocity = new Vector2(m.FaceDirection * m.fleeSpeed, m.Rb.linearVelocity.y);

            // Check if predator is still nearby
            if (m.TryDetectPredator(out Transform predator))
            {
                // Still detected — reset cooldown and continuously update flee direction
                m.FleeTimer  = 0f;
                m.Target     = predator;
                m.FaceDirection = -Mathf.Sign(predator.position.x - m.transform.position.x);
            }
            else
            {
                // Predator out of range — count down to safety
                m.FleeTimer += Time.fixedDeltaTime;
                if (m.FleeTimer >= m.fleeCooldown)
                {
                    m.Target = null;
                    m.StateMachine.SetState(AIStateID.Wander);
                }
            }
        }
    }
}
