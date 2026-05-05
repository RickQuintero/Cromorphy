using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Snake AI using NavMesh2D (NavMeshPlus) for pathfinding.
///
/// Architecture:
///   NavMeshAgent  — computes paths only (updatePosition/Rotation = false)
///   Rigidbody2D   — owns all actual movement via FollowPath()
///   8-dir raycasts — ground detection identical to PlayerController2D
///
/// States: SEARCH (NavMesh wander) → CHASE → ATTACK → RETURN TO NEST → despawn
///
/// Inspector setup:
///   - Add AgentOverride2d to this GameObject (required by NavMeshPlus for 2D)
///   - Assign bodyRigidbodies with every segment Rb that needs gravity synced
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class Ent_Snake : AIAgent
{
    [Header("Snake")]
    public Transform nestTransform;

    [Tooltip("Head child — X scale flipped to face movement direction.")]
    public Transform headTransform;

    [Tooltip("All body segment Rigidbody2Ds — gravity/damping synced with head.")]
    public Rigidbody2D[] bodyRigidbodies;

    [Header("Prey Layers")]
    [Tooltip("Include the Player layer here — IsPlayer is determined by component, not by layer.")]
    public LayerMask snakeFoodLayer;
    public float detectionRayLength  = 5f;
    public float groundCheckDistance = 0.7f;

    [Header("Wander")]
    [Tooltip("Radius around spawn point from which random NavMesh destinations are picked.")]
    public float wanderRadius = 8f;

    [Tooltip("Distance to destination that counts as arrived.")]
    public float wanderArrivalDist = 0.8f;

    [Tooltip("Random pause duration at each wander point (min / max seconds).")]
    public float wanderPauseMin = 0.5f;
    public float wanderPauseMax = 2.5f;

    [Header("Chase")]
    [Tooltip("Seconds without a visible target before returning to Search.")]
    public float chaseLostTimeout = 2f;

    [Header("Ground Physics")]
    public float groundedDamping = 4f;

    [Header("Timing")]
    public float attackDuration    = 0.8f;
    public float nestArrivalRadius = 0.5f;

    // ── Ground detection ──────────────────────────────────────────────────
    private static readonly Vector2[] _rayDirs =
    {
        Vector2.up,    Vector2.down,  Vector2.left,              Vector2.right,
        new Vector2( 1f,  1f).normalized, new Vector2(-1f,  1f).normalized,
        new Vector2( 1f, -1f).normalized, new Vector2(-1f, -1f).normalized,
    };

    private bool _isGrounded;
    private static readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[1];
    private ContactFilter2D _groundFilter;
    private ContactFilter2D _foodFilter;

    // ── NavMesh ───────────────────────────────────────────────────────────
    private NavMeshAgent _agent;
    private Vector3      _currentDestination;

    // ── Wander state (lives on snake so it persists across Search re-entries) ──
    private bool  _wanderPausing;
    private float _wanderPauseTimer;
    private float _wanderPauseDuration;
    private Vector3 _wanderCenter;     // recorded at spawn — wander stays near here

    // ── Shared state used by nested state classes ─────────────────────────
    [System.NonSerialized] public bool  TargetIsPlayer;
    [System.NonSerialized] public float AttackTimer;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        _agent = GetComponent<NavMeshAgent>();

        // Rigidbody2D drives position — agent computes paths only
        _agent.updatePosition = false;
        _agent.updateRotation = false;
        _agent.updateUpAxis   = false;   // required for NavMeshPlus 2D
        _agent.stoppingDistance = 0f;

        Rb.freezeRotation = true;

        _groundFilter = new ContactFilter2D { useLayerMask = true, layerMask = solidLayer };
        _foodFilter   = new ContactFilter2D { useLayerMask = true, layerMask = snakeFoodLayer };
    }

    protected override void RegisterStates()
    {
        StateMachine.RegisterState(new SearchState());
        StateMachine.RegisterState(new ChaseState());
        StateMachine.RegisterState(new AttackState());
        StateMachine.RegisterState(new ReturnState());
    }

    protected override void OnSpawned()
    {
        Target              = null;
        TargetIsPlayer      = false;
        AttackTimer         = 0f;
        _wanderPausing      = false;
        _wanderCenter       = transform.position;
        _currentDestination = transform.position;

        _agent.enabled  = true;
        _agent.isStopped = true;

        StateMachine.SetState(AIStateID.Search);
    }

    // ── FixedUpdate: gravity → sync agent → state tick ────────────────────

    protected override void FixedUpdate()
    {
        UpdateGravity();
        _agent.nextPosition = transform.position;   // keep agent in sync with Rb2D
        base.FixedUpdate();                         // state machine FixedTick
    }

    private void UpdateGravity()
    {
        _isGrounded = false;

        foreach (var dir in _rayDirs)
        {
            if (Physics2D.Raycast(transform.position, dir, _groundFilter, _hitBuffer, groundCheckDistance) > 0)
            { 
                _isGrounded = true; 
                break; 
            }
        }

        float gravity = _isGrounded ? 0f : 1f;
        float damping = _isGrounded ? groundedDamping : 0f;

        Rb.gravityScale = gravity;
        if (bodyRigidbodies != null)
            foreach (var rb in bodyRigidbodies)
                if (rb != null)
                {
                    rb.gravityScale   = gravity;
                    rb.linearDamping  = damping;
                    rb.angularDamping = damping;
                }
    }

    // ── Update: head flip from velocity direction ─────────────────────────

    protected override void Update()
    {
        base.Update();
        if (headTransform == null) return;

        float velX = Rb.linearVelocity.x;
        if (Mathf.Abs(velX) > 0.05f)
            headTransform.localScale = new Vector3(
                velX > 0f ? 1f : -1f,
                headTransform.localScale.y, 1f);
    }

    // ── Direct contact kill ───────────────────────────────────────────────
    // Handles the case where the player walks into the snake before the
    // 8-dir raycasts detect them (e.g. from directly above/below).
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & snakeFoodLayer) == 0) return;

        var pc = other.GetComponent<PlayerController2D>()
              ?? other.GetComponentInParent<PlayerController2D>();
        if (pc == null) return;

        pc.EnterRagdoll(transform);

        if (StateMachine.CurrentStateID != AIStateID.Attack)
        {
            Target         = other.transform.root;
            TargetIsPlayer = true;
            StateMachine.SetState(AIStateID.Attack);
        }
    }

    // ── NavMesh helpers (called by states) ────────────────────────────────

    /// <summary>
    /// Move Rigidbody2D toward the next NavMesh path waypoint at the given speed.
    /// Uses agent.steeringTarget which always points to the next valid corner.
    /// </summary>
    public void FollowPath(float speed)
    {
        if (_agent.pathPending || !_agent.hasPath) return;

        Vector2 steering = _agent.steeringTarget;
        Vector2 dir      = steering - (Vector2)transform.position;

        if (dir.sqrMagnitude < 0.01f) return;

        Rb.linearVelocity = dir.normalized * speed;
    }

    /// <summary>Set a new NavMesh destination.</summary>
    public void SetDestination(Vector3 pos)
    {
        _currentDestination  = pos;
        _agent.isStopped     = false;
        _agent.SetDestination(pos);
    }

    /// <summary>Stop all movement and suspend path following.</summary>
    public void StopMovement()
    {
        Rb.linearVelocity = Vector2.zero;
        _agent.isStopped  = true;
    }

    /// <summary>True when the agent is close enough to its current destination.</summary>
    public bool HasArrived(float threshold) =>
        !_agent.pathPending &&
        Vector2.Distance(transform.position, _currentDestination) <= threshold;

    /// <summary>
    /// Samples a random reachable point on the NavMesh within wanderRadius
    /// of the spawn center. Returns false if no valid point is found.
    /// </summary>
    public bool TryGetWanderPoint(out Vector3 point)
    {
        Vector2 random2D  = Random.insideUnitCircle * wanderRadius;
        Vector3 candidate = _wanderCenter + new Vector3(random2D.x, random2D.y, 0f);

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            point = hit.position;
            return true;
        }

        point = transform.position;
        return false;
    }

    // ── Detection (8-directional, same as PlayerController2D) ────────────

    public bool TryScanForTarget(out Transform found, out bool isPlayer)
    {
        found    = null;
        isPlayer = false;

        Vector2   origin     = headTransform != null ? (Vector2)headTransform.position : (Vector2)transform.position;
        Transform preyHit    = null;
        Transform playerHit  = null;
        float     preyDist   = float.MaxValue;
        float     playerDist = float.MaxValue;

        foreach (Vector2 d in _rayDirs)
        {
            if (Physics2D.Raycast(origin, d, _foodFilter, _hitBuffer, detectionRayLength) == 0) continue;

            RaycastHit2D hit    = _hitBuffer[0];
            bool         hitIsPlayer = hit.transform.GetComponentInParent<PlayerController2D>() != null;

            if (hitIsPlayer)
            {
                if (hit.distance < playerDist) { playerHit = hit.transform; playerDist = hit.distance; }
            }
            else
            {
                if (hit.distance < preyDist)   { preyHit   = hit.transform; preyDist   = hit.distance; }
            }
        }

        // Prefer prey over player (snake eats mice first)
        if (preyHit   != null) { found = preyHit;   isPlayer = false; return true; }
        if (playerHit != null) { found = playerHit; isPlayer = true;  return true; }
        return false;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        Vector2 origin = headTransform != null ? (Vector2)headTransform.position : (Vector2)transform.position;

        // Ground state
        Gizmos.color = Application.isPlaying && _isGrounded ? Color.cyan : Color.grey;
        Gizmos.DrawWireSphere(transform.position, 0.12f);

        // Ground check rays
        foreach (Vector2 d in _rayDirs)
        {
            bool hit = Physics2D.Raycast(transform.position, d, groundCheckDistance, solidLayer).collider != null;
            Gizmos.color = hit ? Color.green : new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + d * groundCheckDistance);
        }

        // Detection rays
        foreach (Vector2 d in _rayDirs)
        {
            bool hit = Physics2D.Raycast(origin, d, detectionRayLength, snakeFoodLayer).collider != null;
            Gizmos.color = hit ? Color.cyan : new Color(1f, 0.3f, 0.3f, 0.15f);
            Gizmos.DrawLine(origin, origin + d * detectionRayLength);
        }

        // Attack range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, attackRange);

        // Wander area
        Gizmos.color = new Color(0.2f, 1f, 0.5f, 1f);
        Gizmos.DrawWireSphere(transform.position, wanderRadius);
        if (Application.isPlaying)
        {
            Gizmos.color = new Color(0.2f, 1f, 0.5f, 0.8f);
            Gizmos.DrawWireSphere(_currentDestination, 0.2f);
        }

        // NavMesh path
        if (Application.isPlaying && _agent != null && _agent.hasPath)
        {
            Gizmos.color = Color.white;
            var corners = _agent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
                Gizmos.DrawLine(corners[i], corners[i + 1]);
        }

        if (nestTransform != null)
        { Gizmos.color = Color.green; Gizmos.DrawLine(transform.position, nestTransform.position); }

        if (Application.isPlaying && Target != null)
        { Gizmos.color = Color.red; Gizmos.DrawLine(origin, Target.position); }
    }

    // ═════════════════════════════════════════════════════════════════════
    // STATES
    // ═════════════════════════════════════════════════════════════════════

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Wander by picking random NavMesh points within wanderRadius.
    /// Pauses at each point for a random duration before picking the next.
    /// Breaks to Chase the instant a target is detected.
    /// </summary>
    private class SearchState : AIState
    {
        public override AIStateID ID => AIStateID.Search;

        private bool _hasDestination;

        public override void Enter(AIAgent a)
        {
            var s = (Ent_Snake)a;
            s._wanderPausing = false;
            _hasDestination  = false;
        }

        public override void FixedTick(AIAgent a)
        {
            var s = (Ent_Snake)a;

            // Detection scan has highest priority
            if (s.TryScanForTarget(out Transform found, out bool isPlayer))
            {
                s.Target = found; s.TargetIsPlayer = isPlayer;
                s.StateMachine.SetState(AIStateID.Chase);
                return;
            }

            // Pause at arrived waypoint
            if (s._wanderPausing)
            {
                s.StopMovement();
                s._wanderPauseTimer += Time.fixedDeltaTime;
                if (s._wanderPauseTimer >= s._wanderPauseDuration)
                {
                    s._wanderPausing = false;
                    _hasDestination  = false;
                }
                return;
            }

            // Pick a new destination if we don't have one
            if (!_hasDestination)
            {
                if (s.TryGetWanderPoint(out Vector3 point))
                {
                    s.SetDestination(point);
                    _hasDestination = true;
                }
                return; // wait for next frame whether successful or not
            }

            // Follow the path
            s.FollowPath(s.moveSpeed * 0.4f);

            // Check arrival
            if (s.HasArrived(s.wanderArrivalDist))
            {
                s._wanderPausing        = true;
                s._wanderPauseTimer     = 0f;
                s._wanderPauseDuration  = Random.Range(s.wanderPauseMin, s.wanderPauseMax);
                _hasDestination         = false;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Continuously update NavMesh destination to the target's position.
    /// Falls back to Search after chaseLostTimeout seconds without a target.
    /// </summary>
    private class ChaseState : AIState
    {
        public override AIStateID ID => AIStateID.Chase;
        private float _lostTimer;
        private float _pathTimer;

        public override void Enter(AIAgent a)
        {
            _lostTimer = 0f;
            _pathTimer = 0.25f; // trigger immediately on enter
            ((Ent_Snake)a)._agent.isStopped = false;
        }

        public override void FixedTick(AIAgent a)
        {
            var s = (Ent_Snake)a;

            if (s.Target == null || !s.Target.gameObject.activeInHierarchy)
            {
                if ((_lostTimer += Time.fixedDeltaTime) >= s.chaseLostTimeout)
                    s.StateMachine.SetState(AIStateID.Search);
                return;
            }

            _lostTimer = 0f;

            _pathTimer += Time.fixedDeltaTime;
            if (_pathTimer >= 0.25f)
            {
                s.SetDestination(s.Target.position);
                _pathTimer = 0f;
            }

            s.FollowPath(s.moveSpeed);

            if (Vector2.Distance(s.transform.position, s.Target.position) <= s.attackRange)
                s.StateMachine.SetState(AIStateID.Attack);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    private class AttackState : AIState
    {
        public override AIStateID ID => AIStateID.Attack;

        public override void Enter(AIAgent a)
        {
            var s = (Ent_Snake)a;
            s.AttackTimer = 0f;
            s.StopMovement();

            if (s.Target == null) { s.StateMachine.SetState(AIStateID.ReturnToNest); return; }

            if (s.TargetIsPlayer)
            {
                s.Target.GetComponent<PlayerController2D>()?.EnterRagdoll(s.transform);
            }
            else
            {
                //s.Target.GetComponent<EntityPoolMember>()?.ReturnToPool();
                s.Target = null;
            }
        }

        public override void Tick(AIAgent a)
        {
            var s = (Ent_Snake)a;
            if ((s.AttackTimer += Time.deltaTime) >= s.attackDuration)
                s.StateMachine.SetState(AIStateID.ReturnToNest);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    private class ReturnState : AIState
    {
        public override AIStateID ID => AIStateID.ReturnToNest;

        private float   _travelTimer;
        private Vector3 _returnPoint;   // nest position OR spawn position if no nest assigned

        public override void Enter(AIAgent a)
        {
            var s    = (Ent_Snake)a;
            s.Target = null;
            _travelTimer = 0f;

            // If no nest is assigned, return to the spawn position recorded at OnSpawned.
            _returnPoint = s.nestTransform != null
                ? s.nestTransform.position
                : s._wanderCenter;

            s.SetDestination(_returnPoint);
        }

        public override void FixedTick(AIAgent a)
        {
            var s = (Ent_Snake)a;

            _travelTimer += Time.fixedDeltaTime;

            // Guard: don't check arrival for the first 0.5 s so the snake can't
            // despawn in the same frame it spawned at (or near) the return point.
            if (_travelTimer >= 0.5f &&
                Vector2.Distance(s.transform.position, _returnPoint) <= s.nestArrivalRadius)
            {
                s._agent.enabled = false;
                s.Despawn();
                return;
            }

            s.FollowPath(s.moveSpeed * 0.6f);
        }
    }
}
