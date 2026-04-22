using UnityEngine;

public class ProceduralLegPlacement2D : MonoBehaviour
{
    // ── References ────────────────────────────────────────────────────────
    [Header("References")]
    [Tooltip("Place this Transform near the foot anchor. 8 rays radiate from here to find the nearest surface.")]
    public Transform raycastOrigin;
    public Transform ikTarget;

    // ── Ground Detection ──────────────────────────────────────────────────
    [Header("Ground Detection")]
    public LayerMask solidLayer;
    [Tooltip("Offset from raycastOrigin that the foot aims for while airborne (e.g. (0, -0.3) hangs the leg down).")]
    public Vector2 airRestOffset = new Vector2(0f, -0.3f);
    [Tooltip("How fast the foot moves toward the air-rest position while not grounded.")]
    public float airSnapSpeed = 8f;
    [Tooltip("Length of each of the 8 directional rays.")]
    public float rayLength = 0.8f;

    // ── Stepping ──────────────────────────────────────────────────────────
    [Header("Stepping")]
    [Tooltip("When true the leg steps automatically based on stepTriggerDistance. " +
             "Disable to let PlayerController2D drive the step order manually via Step().")]
    public bool autoStep = true;

    [Tooltip("How far the nearest surface contact must drift before a new step fires. Only used when autoStep is true.")]
    public float stepTriggerDistance = 0.15f;

    [Tooltip("Minimum seconds between steps.")]
    public float stepCooldown = 0.1f;

    [Tooltip("How long the foot travels from old to new position.")]
    public float stepDuration = 0.15f;

    [Tooltip("Lift arc during a step. Must return 0 at t=0 and t=1 so the foot lands flush.")]
    public AnimationCurve stepHeightCurve;

    public float stepHeightMultiplier = 0.2f;

    // ── Public state ──────────────────────────────────────────────────────
    public bool    legGrounded  { get; private set; }
    public Vector2 GroundContact  => _stepTarget;
    public Vector2 SurfaceNormal  => _surfaceNormal;

    // ── Runtime ───────────────────────────────────────────────────────────
    private Vector2 _stepFrom;
    private Vector2 _stepTarget;
    private Vector2 _surfaceNormal = Vector2.up;
    private float   _stepStartTime;
    private float   _lastStepTime;
    private bool    _wasGrounded;

    private float StepPercent =>
        Mathf.Clamp01((Time.time - _stepStartTime) / Mathf.Max(stepDuration, 0.001f));

    // 8 directions — cardinal + diagonal
    private static readonly Vector2[] _dirs = {
        Vector2.up,
        Vector2.down,
        Vector2.left,
        Vector2.right,
        new Vector2( 1f,  1f).normalized,
        new Vector2(-1f,  1f).normalized,
        new Vector2( 1f, -1f).normalized,
        new Vector2(-1f, -1f).normalized,
    };

    // ── Caching & Physics ────────────────────────────────────────────────
    private static readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[1];
    private ContactFilter2D _filter;
    private Vector2 _cachedHitPoint;
    private Vector2 _cachedNormal;
    private Vector2 _lastOriginPos = new Vector2(float.MaxValue, float.MaxValue);
    private bool _lastHitResult;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Start()
    {
        _filter = new ContactFilter2D { useLayerMask = true, layerMask = solidLayer };

        _stepStartTime = Time.time - stepDuration; // mark as already complete
        _lastStepTime  = Time.time;

        if (FindBestHit(out Vector2 p, out Vector2 n))
        {
            _stepFrom      = p;
            _stepTarget    = p;
            _surfaceNormal = n;
        }
        else
        {
            _stepFrom   = OriginPos();
            _stepTarget = OriginPos();
        }

        if (ikTarget != null) ikTarget.position = _stepTarget;
    }

    void Update()
    {
        bool hit = FindBestHit(out Vector2 bestHit, out Vector2 bestNormal);
        legGrounded = hit;

        // Just landed — snap immediately, bypassing cooldown and distance checks
        if (hit && !_wasGrounded)
        {
            BeginStep(bestHit, bestNormal);
        }
        // Auto-step: only when enabled — disable to let an external controller sequence the steps
        else if (autoStep
            && hit
            && Time.time >= _lastStepTime + stepCooldown
            && Vector2.Distance(bestHit, _stepTarget) > stepTriggerDistance)
        {
            BeginStep(bestHit, bestNormal);
        }

        _wasGrounded = hit;
        if (legGrounded)
        {
            MoveIkTarget();
        }
        else
        {
            MoveIKTargetGravity();
        }
    }

    // ── Step logic ────────────────────────────────────────────────────────

    private void BeginStep(Vector2 target, Vector2 normal)
    {
        _stepFrom      = ikTarget != null ? (Vector2)ikTarget.position : _stepTarget;
        _stepTarget    = target;
        _surfaceNormal = normal;
        _stepStartTime = Time.time;
        _lastStepTime  = Time.time;
    }

    private void MoveIkTarget()
    {
        if (ikTarget == null) return;

        float   t    = StepPercent;
        Vector2 flat = Vector2.Lerp(_stepFrom, _stepTarget, t);
        float   lift = stepHeightCurve != null
            ? stepHeightCurve.Evaluate(t) * stepHeightMultiplier
            : 0f;

        ikTarget.position = (Vector3)(flat + _surfaceNormal * lift);
    }
    private void MoveIKTargetGravity()
    {
        // While airborne, smoothly pull the foot to a rest position relative to the origin.
        // This prevents the foot from drifting to a wild position that causes a flip on landing.
        if (ikTarget == null) return;
        Vector2 airTarget = OriginPos() + airRestOffset;
        ikTarget.position = Vector2.MoveTowards(ikTarget.position, airTarget, airSnapSpeed * Time.deltaTime);
    }

    // ── Surface scan ─────────────────────────────────────────────────────

    // Casts all 8 rays and returns the nearest hit.
    private bool FindBestHit(out Vector2 hitPoint, out Vector2 normal)
    {
        Vector2 origin   = OriginPos();

        // --- SPATIAL CACHING ---
        if ((origin - _lastOriginPos).sqrMagnitude < 0.0025f)
        {
            hitPoint = _cachedHitPoint;
            normal = _cachedNormal;
            return _lastHitResult;
        }
        _lastOriginPos = origin;
        // -----------------------

        float   bestDist = float.MaxValue;
        hitPoint = origin;
        normal   = Vector2.up;
        bool found = false;

        foreach (var dir in _dirs)
        {
            if (Physics2D.Raycast(origin, dir, _filter, _hitBuffer, rayLength) > 0)
            {
                RaycastHit2D h = _hitBuffer[0];
                if (h.distance < bestDist)
                {
                    bestDist = h.distance;
                    hitPoint = h.point;
                    normal   = h.normal;
                    found    = true;
                }
            }
        }

        _cachedHitPoint = hitPoint;
        _cachedNormal = normal;
        _lastHitResult = found;

        return found;
    }

    private Vector2 OriginPos() =>
        raycastOrigin != null ? (Vector2)raycastOrigin.position : (Vector2)transform.position;

    // ── External API (called by PlayerController2D) ───────────────────────

    // Force an immediate step to the current best hit.
    public void Step()
    {
        if (FindBestHit(out Vector2 p, out Vector2 n)) BeginStep(p, n);
    }

    // No-op kept so PlayerController2D compiles without changes.
    public void MoveVelocity(Vector2 _) { }

    // ── Gizmos ───────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        Vector2 origin = OriginPos();

        // Origin marker
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, 0.04f);

        // 8 rays
        foreach (var dir in _dirs)
        {
            RaycastHit2D h = Physics2D.Raycast(origin, dir, rayLength, solidLayer);
            if (h.collider != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(origin, h.point);
                Gizmos.DrawWireSphere(h.point, 0.025f);
            }
            else
            {
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
                Gizmos.DrawLine(origin, origin + dir * rayLength);
            }
        }

        // Current step target and IK target
        if (Application.isPlaying)
        {
            Gizmos.color = legGrounded ? Color.cyan : Color.red;
            Gizmos.DrawWireSphere(_stepTarget, 0.04f);

            if (ikTarget != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(ikTarget.position, 0.03f);
                Gizmos.DrawLine(_stepTarget, ikTarget.position);
            }
        }
    }
}
