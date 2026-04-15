using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Mouse AI — Horizontal Wander Edition.
/// Uses Left/Right raycasts to find platform limits, then picks a NavMesh point between them.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class Ent_Mouse : AIAgent
{
    [Header("Speeds")]
    public float wanderSpeed = 2f;
    public float fleeSpeed   = 5f;

    [Header("Horizontal Wander")]
    [Tooltip("Maximum distance to look left/right for a wander point.")]
    public float maxWanderRange = 5f;
    public float wanderArrivalDist = 0.3f;
    public float wanderPauseMin = 1f;
    public float wanderPauseMax = 3f;

    [Header("Detection")]
    public LayerMask predatorLayer;
    public float fleeDetectionRadius = 4f;
    public float fleeCooldown = 2f;

    private NavMeshAgent _agent;
    private Vector3      _currentDestination;
    
    // Debugging values for Gizmos
    private float _leftLimitX;
    private float _rightLimitX;

    [System.NonSerialized] public bool  WanderPausing;
    [System.NonSerialized] public float WanderPauseTimer;
    [System.NonSerialized] public float WanderPauseDuration;

    protected override void Awake()
    {
        base.Awake();
        _agent = GetComponent<NavMeshAgent>();
        _agent.updatePosition = false;
        _agent.updateRotation = false;
        _agent.updateUpAxis   = false; 
        
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
        _currentDestination = transform.position;
        WanderPausing       = false;
        Target              = null;
        _agent.enabled      = true;
        StateMachine.SetState(AIStateID.Wander);
    }

    protected override void FixedUpdate()
    {
        _agent.nextPosition = transform.position;
        base.FixedUpdate();
    }

    // ── Movement & Pathing ───────────────────────────────────────────────

    public void FollowPath(float speed)
    {
        if (_agent.pathPending || !_agent.hasPath) return;

        Vector2 steering = _agent.steeringTarget;
        Vector2 dir      = steering - (Vector2)transform.position;

        if (dir.sqrMagnitude < 0.01f) return;

        // Apply horizontal velocity, keep existing vertical velocity (gravity)
        Rb.linearVelocity = new Vector2(Mathf.Sign(dir.x) * speed, Rb.linearVelocity.y);
    }

    public void SetDestination(Vector3 pos)
    {
        _currentDestination = pos;
        _agent.isStopped    = false;
        _agent.SetDestination(pos);
    }

    /// <summary>
    /// Finds a point strictly to the left or right by raycasting for walls,
    /// then sampling the NavMesh to ensure it is on a walkable surface.
    /// </summary>
    public bool TryGetHorizontalWanderPoint(out Vector3 point)
    {
        Vector2 origin = transform.position;

        // 1. Raycast Left and Right to find the physical boundaries (walls)
        RaycastHit2D hitLeft  = Physics2D.Raycast(origin, Vector2.left, maxWanderRange, solidLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(origin, Vector2.right, maxWanderRange, solidLayer);

        _leftLimitX  = hitLeft.collider  != null ? hitLeft.point.x  : origin.x - maxWanderRange;
        _rightLimitX = hitRight.collider != null ? hitRight.point.x : origin.x + maxWanderRange;

        // 2. Pick a random X between those boundaries
        float randomX = Random.Range(_leftLimitX, _rightLimitX);
        Vector3 candidatePos = new Vector3(randomX, origin.y, 0f);

        // 3. Snap to NavMesh to ensure it's not "inside" the floor or floating
        // We use a small search radius to keep it on the current platform
        if (NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
        {
            point = hit.position;
            return true;
        }

        point = transform.position;
        return false;
    }

    public bool TryDetectPredator(out Transform predator)
    {
        predator = null;
        Collider2D hit = Physics2D.OverlapCircle(transform.position, fleeDetectionRadius, predatorLayer);
        if (hit != null) { predator = hit.transform; return true; }
        return false;
    }

    // ── Gizmos ───────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw the horizontal "search" beam
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(_leftLimitX, transform.position.y, 0), 
                        new Vector3(_rightLimitX, transform.position.y, 0));

        // Destination Marker
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_currentDestination, 0.2f);

        // Predator Radius
        Gizmos.color = new Color(1, 0, 0, 0.15f);
        Gizmos.DrawWireSphere(transform.position, fleeDetectionRadius);
    }

    // ═════════════════════════════════════════════════════════════════════
    // STATES (Same structure, updated logic)
    // ═════════════════════════════════════════════════════════════════════

    private class MouseWanderState : AIState
    {
        public override AIStateID ID => AIStateID.Wander;
        private bool _hasDestination;

        public override void Enter(AIAgent a) => _hasDestination = false;

        public override void FixedTick(AIAgent a)
        {
            var m = (Ent_Mouse)a;

            if (m.TryDetectPredator(out Transform predator))
            {
                m.Target = predator;
                m.StateMachine.SetState(AIStateID.Flee);
                return;
            }

            if (m.WanderPausing)
            {
                m.Rb.linearVelocity = new Vector2(0, m.Rb.linearVelocity.y);
                if ((m.WanderPauseTimer += Time.fixedDeltaTime) >= m.WanderPauseDuration)
                {
                    m.WanderPausing = false;
                    _hasDestination = false;
                }
                return;
            }

            if (!_hasDestination)
            {
                if (m.TryGetHorizontalWanderPoint(out Vector3 p))
                {
                    m.SetDestination(p);
                    _hasDestination = true;
                }
                return;
            }

            m.FollowPath(m.wanderSpeed);

            if (Vector2.Distance(m.transform.position, m._currentDestination) <= m.wanderArrivalDist)
            {
                m.WanderPausing = true;
                m.WanderPauseTimer = 0f;
                m.WanderPauseDuration = Random.Range(m.wanderPauseMin, m.wanderPauseMax);
            }
        }
    }

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
                // Run away horizontally
                float runDir = Mathf.Sign(m.transform.position.x - predator.position.x);
                Vector3 fleeTarget = m.transform.position + new Vector3(runDir * 2f, 0, 0);
                
                if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                    m.SetDestination(hit.position);

                m.FollowPath(m.fleeSpeed);
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