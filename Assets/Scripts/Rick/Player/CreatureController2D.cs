using UnityEngine;

/// <summary>
/// Surface-crawling AI. Moves along floors, walls, and ceilings.
/// Uses a CircleCast for surface detection and edge avoidance.
/// No gravity — position is held against the surface via adhesion velocity.
/// Designed as the head of a snake-like creature driven by Rigidbody2D.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class CreatureController2D : MonoBehaviour
{
    // ── Movement ──────────────────────────────────────────────────────────
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float rotationSpeed = 8f;

    [Tooltip("How strongly the creature sticks to the surface (corrects distance error per second).")]
    public float adhesionStrength = 6f;

    // ── Surface Detection ─────────────────────────────────────────────────
    [Header("Surface Detection")]
    public LayerMask solidLayer;

    [Tooltip("Radius of the circle used to probe the surface below.")]
    public float surfaceRadius = 0.25f;

    [Tooltip("Max distance the circle cast travels downward to find a surface.")]
    public float surfaceCastDistance = 1.2f;

    [Tooltip("Desired gap between the circle cast origin and the surface contact point.")]
    public float desiredSurfaceDistance = 0.1f;

    // ── Edge Detection ────────────────────────────────────────────────────
    [Header("Edge Detection")]
    [Tooltip("Radius of the circle cast looking ahead for surface continuity.")]
    public float edgeCheckRadius = 0.15f;

    [Tooltip("How far ahead (in movement direction) the edge cast travels downward.")]
    public float edgeCheckDistance = 0.8f;

    [Tooltip("Minimum seconds before the creature can change direction again.")]
    public float directionChangeCooldown = 0.4f;

    // ── Legs ──────────────────────────────────────────────────────────────
    [Header("Legs")]
    public ProceduralLegPlacement2D[] legs;
    public float timeBetweenSteps = 0.25f;
    public float stepDurationRatio = 2f;
    public bool  dynamicGait      = false;
    public float maxTargetDistance = 1f;

    // ── Alignment ─────────────────────────────────────────────────────────
    [Header("Alignment")]
    public bool useAlignment = true;

    // ── Public state ──────────────────────────────────────────────────────
    public bool  grounded       { get; private set; }
    public bool  isStepping     { get; private set; }
    public float moveDir        { get; private set; } = 1f; // +1 = right, -1 = left

    // ── Private runtime ───────────────────────────────────────────────────
    private Rigidbody2D _rb;

    private Vector2 _surfaceNormal     = Vector2.up;
    private float   _surfaceHitDist;          // distance traveled by the cast before contact
    private Vector2 _lastValidSurface;        // last confirmed surface contact point

    private float _lastDirChange;
    private float _lastStep;
    private int   _stepIndex;

    // ─────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────────────────

    void Start()
    {
        _rb                = GetComponent<Rigidbody2D>();
        _rb.gravityScale   = 0f;
        _rb.freezeRotation = true;

        _lastValidSurface = transform.position;
        _lastDirChange    = Time.time;
        _lastStep         = Time.time;
    }

    void FixedUpdate()
    {
        SampleSurface();
        CheckEdge();
        ApplyVelocity();
        if (useAlignment) AlignToSurface();
        StepLegs();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Surface
    // ─────────────────────────────────────────────────────────────────────

    private void SampleSurface()
    {
        RaycastHit2D hit = Physics2D.CircleCast(
            transform.position,
            surfaceRadius,
            -transform.up,          // local down
            surfaceCastDistance,
            solidLayer);

        grounded = hit.collider != null;

        if (grounded)
        {
            _surfaceNormal   = hit.normal;
            _surfaceHitDist  = hit.distance;
            _lastValidSurface = hit.point;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Edge avoidance
    // ─────────────────────────────────────────────────────────────────────

    private void CheckEdge()
    {
        if (Time.time < _lastDirChange + directionChangeCooldown) return;

        // Offset origin slightly ahead in the movement direction
        Vector2 movementDir = (Vector2)(transform.right * moveDir);
        Vector2 castOrigin  = (Vector2)transform.position + movementDir * (surfaceRadius + 0.15f);

        // Cast the circle downward from that offset — if no hit, the surface ends here
        RaycastHit2D ahead = Physics2D.CircleCast(
            castOrigin,
            edgeCheckRadius,
            -transform.up,
            edgeCheckDistance,
            solidLayer);

        if (ahead.collider == null)
        {
            // No ground ahead — flip direction in-place, position unchanged
            moveDir        = -moveDir;
            _lastDirChange = Time.time;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Movement
    // ─────────────────────────────────────────────────────────────────────

    private void ApplyVelocity()
    {
        if (!grounded)
        {
            // Drift to a stop when airborne — no gravity, no panic
            _rb.linearVelocity = Vector2.Lerp(_rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 4f);
            return;
        }

        // Tangent along the surface, aligned to transform.right
        Vector2 rightTangent = new Vector2(_surfaceNormal.y, -_surfaceNormal.x);
        if (Vector2.Dot(rightTangent, transform.right) < 0f) rightTangent = -rightTangent;

        Vector2 tangentialVel = rightTangent * (moveDir * moveSpeed);

        // Adhesion: push toward the desired surface distance
        float   distError   = desiredSurfaceDistance - _surfaceHitDist;
        Vector2 adhesionVel = -(Vector2)transform.up * (distError * adhesionStrength);

        _rb.linearVelocity = tangentialVel + adhesionVel;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Alignment
    // ─────────────────────────────────────────────────────────────────────

    private void AlignToSurface()
    {
        // Rotate so transform.up matches the surface normal
        float     targetAngle = Mathf.Atan2(_surfaceNormal.x, _surfaceNormal.y) * Mathf.Rad2Deg;
        Quaternion targetRot  = Quaternion.Euler(0f, 0f, -targetAngle);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Legs
    // ─────────────────────────────────────────────────────────────────────

    private void StepLegs()
    {
        if (legs == null || legs.Length == 0) return;

        Vector2 vel = _rb.linearVelocity;

        float interval = dynamicGait && vel.magnitude > 0.01f
            ? maxTargetDistance / vel.magnitude
            : timeBetweenSteps;

        foreach (var leg in legs)
            if (leg != null) leg.MoveVelocity(vel);

        if (Time.time > _lastStep + interval / legs.Length)
        {
            var leg = legs[_stepIndex];
            if (leg != null)
            {
                leg.stepDuration    = Mathf.Min(1f, (interval / legs.Length) * stepDurationRatio);
                //leg.worldVelocity   = vel;
                leg.Step();
            }
            _stepIndex = (_stepIndex + 1) % legs.Length;
            _lastStep  = Time.time;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Gizmos
    // ─────────────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        // Surface probe
        Gizmos.color = grounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, surfaceRadius);
        Gizmos.DrawLine(
            transform.position,
            (Vector2)transform.position - (Vector2)transform.up * surfaceCastDistance);

        // Edge check ahead
        Vector2 movDir     = Application.isPlaying ? (Vector2)(transform.right * moveDir) : (Vector2)transform.right;
        Vector2 edgeOrigin = (Vector2)transform.position + movDir * (surfaceRadius + 0.15f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(edgeOrigin, edgeCheckRadius);
        Gizmos.DrawLine(edgeOrigin, edgeOrigin - (Vector2)transform.up * edgeCheckDistance);

        // Surface normal
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + _surfaceNormal * 0.4f);

        // Last valid surface point
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(_lastValidSurface, 0.04f);
    }
}
