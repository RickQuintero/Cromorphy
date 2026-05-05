using UnityEngine;

/// <summary>
/// Mouse AI — pure physics horizontal wander.
/// No NavMesh. Picks a random direction, walks until a timer expires or a
/// wall is hit, then pauses or reverses. Jumps randomly while grounded.
/// Flees from anything on predatorLayer.
/// </summary>
public class Ent_Mouse : AIAgent
{
    [Header("Movement")]
    public float wanderSpeed    = 2f;
    public float fleeSpeed      = 5f;
    public float dirChangeMin   = 1.5f;
    public float dirChangeMax   = 4f;
    public float wanderPauseMin = 0.5f;
    public float wanderPauseMax = 2f;

    [Header("Jump")]
    public float jumpForce       = 8f;
    [Tooltip("Seconds between random jump attempts (min / max).")]
    public float jumpIntervalMin = 2f;
    public float jumpIntervalMax = 6f;

    [Header("Detection")]
    public LayerMask predatorLayer;
    public float fleeDetectionRadius = 4f;
    public float fleeCooldown        = 2f;
    public float wallCheckDistance   = 0.4f;
    public float groundCheckDistance = 0.25f;

    [Header("References")]
    public SpriteRenderer spriteRenderer;
    public Animator       animator;

    // ── Shared state consumed by nested state classes ─────────────────────
    [System.NonSerialized] public float MoveDir            = 1f;  // +1 right, -1 left
    [System.NonSerialized] public bool  IsGrounded;
    [System.NonSerialized] public bool  WanderPausing;
    [System.NonSerialized] public float WanderPauseTimer;
    [System.NonSerialized] public float WanderPauseDuration;
    [System.NonSerialized] public float DirTimer;
    [System.NonSerialized] public float DirInterval;
    [System.NonSerialized] public float JumpTimer;

    private static readonly int _hashIsWalking = Animator.StringToHash("IsWalking");
    private static readonly int _hashJump      = Animator.StringToHash("Jump");

    // ── Lifecycle ─────────────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        Rb.freezeRotation = true;
        Rb.gravityScale   = 1f;
    }

    protected override void RegisterStates()
    {
        StateMachine.RegisterState(new MouseWanderState());
        StateMachine.RegisterState(new MouseFleeState());
    }

    protected override void OnSpawned()
    {
        WanderPausing = false;
        Target        = null;
        MoveDir       = Random.value > 0.5f ? 1f : -1f;
        DirTimer      = 0f;
        DirInterval   = Random.Range(dirChangeMin, dirChangeMax);
        JumpTimer     = Random.Range(jumpIntervalMin, jumpIntervalMax);
        StateMachine.SetState(AIStateID.Wander);
    }

    protected override void FixedUpdate()
    {
        UpdateGrounded();
        base.FixedUpdate();     // ticks the FSM
        UpdateAnimator();
        UpdateSprite();
    }

    // ── Physics helpers called by states ──────────────────────────────────

    private void UpdateGrounded()
    {
        IsGrounded = Physics2D.Raycast(
            transform.position, Vector2.down, groundCheckDistance, solidLayer).collider != null;
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetBool(_hashIsWalking, IsGrounded && Mathf.Abs(Rb.linearVelocity.x) > 0.1f);
    }

    private void UpdateSprite()
    {
        if (spriteRenderer == null) return;
        if      (Rb.linearVelocity.x >  0.05f) spriteRenderer.flipX = false;
        else if (Rb.linearVelocity.x < -0.05f) spriteRenderer.flipX = true;
    }

    /// <summary>
    /// Casts a ray in the current move direction. Flips MoveDir if a wall is ahead.
    /// </summary>
    public void CheckWallAndFlip()
    {
        Vector2 ahead = new Vector2(MoveDir, 0f);
        if (Physics2D.Raycast(transform.position, ahead, wallCheckDistance, solidLayer).collider != null)
            MoveDir = -MoveDir;
    }

    public void DoJump()
    {
        Rb.linearVelocity = new Vector2(Rb.linearVelocity.x, 0f);   // zero out any downward velocity first
        Rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        if (animator != null) animator.SetTrigger(_hashJump);
        JumpTimer = Random.Range(jumpIntervalMin, jumpIntervalMax);
    }

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
        Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, fleeDetectionRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, Vector2.right * wallCheckDistance);
        Gizmos.DrawRay(transform.position, Vector2.left  * wallCheckDistance);

        if (Application.isPlaying)
        {
            Gizmos.color = IsGrounded ? Color.cyan : Color.grey;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    // STATES
    // ═════════════════════════════════════════════════════════════════════

    private class MouseWanderState : AIState
    {
        public override AIStateID ID => AIStateID.Wander;

        public override void Enter(AIAgent a)
        {
            var m = (Ent_Mouse)a;
            m.DirTimer    = 0f;
            m.DirInterval = Random.Range(m.dirChangeMin, m.dirChangeMax);
        }

        public override void FixedTick(AIAgent a)
        {
            var m = (Ent_Mouse)a;

            if (m.TryDetectPredator(out Transform predator))
            {
                m.Target = predator;
                m.StateMachine.SetState(AIStateID.Flee);
                return;
            }

            // Stopped at a rest point — wait it out
            if (m.WanderPausing)
            {
                m.Rb.linearVelocity = new Vector2(0f, m.Rb.linearVelocity.y);
                if ((m.WanderPauseTimer += Time.fixedDeltaTime) >= m.WanderPauseDuration)
                    m.WanderPausing = false;
                return;
            }

            // Random direction-change timer
            m.DirTimer += Time.fixedDeltaTime;
            if (m.DirTimer >= m.DirInterval)
            {
                m.MoveDir     = Random.value > 0.5f ? 1f : -1f;
                m.DirTimer    = 0f;
                m.DirInterval = Random.Range(m.dirChangeMin, m.dirChangeMax);

                // 30 % chance to pause instead of walking immediately
                if (Random.value < 0.3f)
                {
                    m.WanderPausing       = true;
                    m.WanderPauseTimer    = 0f;
                    m.WanderPauseDuration = Random.Range(m.wanderPauseMin, m.wanderPauseMax);
                    return;
                }
            }

            m.CheckWallAndFlip();
            m.Rb.linearVelocity = new Vector2(m.MoveDir * m.wanderSpeed, m.Rb.linearVelocity.y);

            // Random jump
            m.JumpTimer -= Time.fixedDeltaTime;
            if (m.JumpTimer <= 0f && m.IsGrounded)
                m.DoJump();
        }
    }

    // ─────────────────────────────────────────────────────────────────────

    private class MouseFleeState : AIState
    {
        public override AIStateID ID => AIStateID.Flee;
        private float _safeTimer;

        public override void Enter(AIAgent a) => _safeTimer = 0f;

        public override void FixedTick(AIAgent a)
        {
            var m = (Ent_Mouse)a;

            if (m.TryDetectPredator(out Transform predator))
            {
                _safeTimer = 0f;
                m.MoveDir  = Mathf.Sign(m.transform.position.x - predator.position.x);
                m.CheckWallAndFlip();
                m.Rb.linearVelocity = new Vector2(m.MoveDir * m.fleeSpeed, m.Rb.linearVelocity.y);
            }
            else
            {
                _safeTimer += Time.fixedDeltaTime;
                if (_safeTimer >= m.fleeCooldown)
                    m.StateMachine.SetState(AIStateID.Wander);
            }
        }
    }
}
