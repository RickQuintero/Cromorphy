using UnityEngine;

/// <summary>
/// Procedural snake body. Works standalone — does NOT require Ent_Snake on the same GameObject.
///
/// Behaviours:
///   Moving  — segments trail the head through a FABRIK-style length constraint,
///             with a sine wave applied perpendicular to the movement direction.
///   Idle    — segments coil into a slow-rotating elliptical spiral around the head.
///             A pulsating ripple keeps the body visually alive while still.
///   Blend   — smooth transition between both modes driven by head speed.
///
/// Inspector setup:
///   1. Set headTransform to the snake head GameObject.
///   2. Assign segmentSprite (rounded rect or oval works best).
///   3. Tune segmentCount / segmentLength to match the art scale.
///   4. The script auto-generates child GameObjects for every segment.
/// </summary>
[DefaultExecutionOrder(100)]   // Run after head has moved
public class Ent_Snake_ProceduralBody : MonoBehaviour
{
    [Header("Head Reference")]
    [Tooltip("The head that drives the body. Body trails behind this transform.")]
    public Transform headTransform;

    [Header("Segments")]
    [Tooltip("Sprite rendered for each body segment (rounded rectangle recommended).")]
    public Sprite   segmentSprite;
    public Material segmentMaterial;
    public string   sortingLayerName = "Default";
    public int      sortingOrder     = 4;

    [Range(4, 32)]
    [Tooltip("Number of body segments (not counting the head).")]
    public int   segmentCount  = 14;
    [Tooltip("World-space length of each segment.")]
    public float segmentLength = 0.18f;

    [Header("Thickness Taper")]
    [Tooltip("Thickness of the segment closest to the head.")]
    public float maxThickness = 0.22f;
    [Tooltip("Thickness of the tail-tip segment.")]
    public float minThickness = 0.05f;

    [Header("Idle Coil")]
    [Tooltip("Radius of the coiling loop when the snake is still.")]
    public float idleRadius   = 0.5f;
    [Tooltip("How many full loops the body wraps into (1.5 = one full loop + half).")]
    public float idleLoops    = 1.5f;
    [Tooltip("Radians per second the coil rotates.")]
    public float idleRotSpeed = 1.5f;
    [Tooltip("Vertical squash of the coil (1 = circle, 0.6 = flattened ellipse).")]
    public float idleSquash   = 0.65f;
    [Tooltip("Magnitude of the sine ripple layered on top of the coil.")]
    public float idlePulsate  = 0.08f;

    [Header("Move Wave")]
    [Tooltip("Perpendicular wave amplitude while the snake is moving.")]
    public float moveWaveAmplitude = 0.28f;
    [Tooltip("Number of full wave cycles across the body length.")]
    public float moveWaveCycles    = 1.1f;
    [Tooltip("Speed the wave scrolls from head to tail (units per second).")]
    public float moveWaveSpeed     = 5f;

    [Header("Smoothing")]
    [Tooltip("How fast each segment smoothly follows its target (higher = stiffer rope).")]
    public float followSpeed    = 14f;
    [Tooltip("Head speed (units/s) below which the body switches to idle coil mode.")]
    public float speedThreshold = 0.15f;
    [Tooltip("Blend transition speed between idle and moving modes.")]
    public float blendSpeed     = 3.5f;

    // ── Runtime ───────────────────────────────────────────────────────────

    private Transform[]      _segs;
    private SpriteRenderer[] _srs;
    private Vector3[]        _smoothPos;        // smoothed display positions
    private Vector3[]        _constraintPos;    // FABRIK constraint positions (no wave offset)

    private float   _blend;        // 0 = idle, 1 = moving
    private float   _idleAngle;    // accumulated coil rotation
    private float   _wavePhase;    // scrolling wave offset
    private Vector3 _prevHeadPos;
    private Vector2 _headVel;

    // ── Awake ─────────────────────────────────────────────────────────────

    void Awake()
    {
        _segs          = new Transform[segmentCount];
        _srs           = new SpriteRenderer[segmentCount];
        _smoothPos     = new Vector3[segmentCount];
        _constraintPos = new Vector3[segmentCount];

        Vector3 seed = headTransform != null ? headTransform.position : transform.position;

        for (int i = 0; i < segmentCount; i++)
        {
            var go              = new GameObject($"SnakeSeg_{i:00}");
            go.transform.SetParent(transform);

            var sr              = go.AddComponent<SpriteRenderer>();
            sr.sprite           = segmentSprite;
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder     = sortingOrder - i;   // head-closest rendered on top
            if (segmentMaterial != null) sr.material = segmentMaterial;

            _segs[i]          = go.transform;
            _srs[i]           = sr;

            // Seed positions in a line behind the head so the first frame looks clean
            _constraintPos[i] = seed - Vector3.right * segmentLength * (i + 1);
            _smoothPos[i]     = _constraintPos[i];
        }

        _prevHeadPos = seed;
    }

    // ── LateUpdate: runs after all movement scripts ───────────────────────

    void LateUpdate()
    {
        if (headTransform == null) return;

        Vector3 head = headTransform.position;
        float   dt   = Time.deltaTime;

        // ── Head velocity ─────────────────────────────────────────────────
        _headVel     = ((Vector2)head - (Vector2)_prevHeadPos) / Mathf.Max(dt, 0.0001f);
        float speed  = _headVel.magnitude;
        _prevHeadPos = head;

        // ── Mode blend: 0 = idle coil, 1 = moving wave ───────────────────
        _blend = Mathf.MoveTowards(_blend, speed > speedThreshold ? 1f : 0f, blendSpeed * dt);

        // ── Advance phase counters ─────────────────────────────────────────
        _wavePhase += moveWaveSpeed * dt;
        _idleAngle += idleRotSpeed  * dt;

        // ── FABRIK-style rope constraint (pull chain from head) ───────────
        // Each segment is snapped back to segmentLength from the previous anchor.
        // A single-pass forward solve is enough for snappy rope behaviour.
        Vector3 anchor = head;
        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 delta = _constraintPos[i] - anchor;
            float   dist  = delta.magnitude;
            if (dist > segmentLength && dist > 0.0001f)
                _constraintPos[i] = anchor + delta * (segmentLength / dist);
            anchor = _constraintPos[i];
        }

        // ── Movement direction & perpendicular for wave ───────────────────
        Vector2 moveDir = _headVel.sqrMagnitude > 0.01f ? _headVel.normalized : Vector2.right;
        Vector2 perp    = new Vector2(-moveDir.y, moveDir.x);

        // ── Per-segment update ────────────────────────────────────────────
        for (int i = 0; i < segmentCount; i++)
        {
            float t = (float)i / Mathf.Max(segmentCount - 1, 1);   // 0 (head side) → 1 (tail)

            // --- Moving target: constrained rope position + perpendicular wave ---
            // Phase offset makes the wave travel from head toward tail.
            float waveArg    = _wavePhase - i * (Mathf.PI * 2f * moveWaveCycles / segmentCount);
            float envelope   = Mathf.Sin(t * Mathf.PI);             // zero at both ends
            float waveOff    = Mathf.Sin(waveArg) * moveWaveAmplitude * envelope;
            Vector3 moveTgt  = _constraintPos[i] + (Vector3)(perp * waveOff);

            // --- Idle target: rotating elliptical coil around head ---
            // Each segment occupies a unique angle in the coil, creating a spiral.
            float coilAngle  = _idleAngle + t * Mathf.PI * 2f * idleLoops;
            float pulsation  = Mathf.Sin(_idleAngle * 2f + t * Mathf.PI) * idlePulsate;
            float r          = idleRadius * (0.85f + 0.15f * t) + pulsation;
            Vector3 idleTgt  = head + new Vector3(
                Mathf.Cos(coilAngle) * r,
                Mathf.Sin(coilAngle) * r * idleSquash,
                0f
            );

            // --- Blend, smooth, place ---
            Vector3 finalTgt  = Vector3.Lerp(idleTgt, moveTgt, _blend);
            _smoothPos[i]     = Vector3.Lerp(_smoothPos[i], finalTgt, followSpeed * dt);
            _segs[i].position = _smoothPos[i];

            // --- Orient: point each segment toward the next (or previous for tail) ---
            Vector2 segDir;
            if (i < segmentCount - 1)
                segDir = (Vector2)(_smoothPos[i] - _smoothPos[i + 1]);
            else if (i > 0)
                segDir = (Vector2)(_smoothPos[i - 1] - _smoothPos[i]);
            else
                segDir = moveDir;

            if (segDir.sqrMagnitude < 0.0001f) segDir = moveDir;
            _segs[i].rotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(segDir.y, segDir.x) * Mathf.Rad2Deg);

            // --- Thickness taper from head to tail ---
            float thick = Mathf.Lerp(maxThickness, minThickness, t);
            _segs[i].localScale = new Vector3(segmentLength * 1.1f, thick, 1f);
        }
    }

    // ── Gizmos ────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || headTransform == null) return;
        Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.35f);
        Gizmos.DrawWireSphere(headTransform.position, idleRadius);
    }
}
