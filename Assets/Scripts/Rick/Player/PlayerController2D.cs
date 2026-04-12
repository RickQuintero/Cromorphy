using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    // ── Movement ──────────────────────────────────────────────────────────────

    [Header("Movement")]
    [Tooltip("Force applied along local right each FixedUpdate while input is held.")]
    public float moveForce    = 12f;

    [Tooltip("Hard cap on linear speed (world units / second).")]
    public float maxSpeed     = 8f;

    [Tooltip("Fraction of velocity removed each FixedUpdate (0 = no braking, 1 = instant stop). " +
             "Mirrors Senti's breakForce lerp.")]
    public float brakeDamping = 0.12f;


    // ── Gravity ───────────────────────────────────────────────────────────────

    [Header("Gravity")]
    [Tooltip("Rigidbody2D gravity scale while enough limbs are planted on a surface.")]
    public float gravityGrounded = 0f;

    [Tooltip("Rigidbody2D gravity scale while airborne (not enough limbs touching).")]
    public float gravityAirborne = 3f;

    [Tooltip("Minimum number of grounded limbs required to suppress gravity.")]
    public int   minGroundedLimbs = 2;

    // ── Procedural Legs ───────────────────────────────────────────────────────

    [Header("Procedural Legs")]
    [Tooltip("All limb controllers on this body. Order determines stepping sequence.")]
    public ProceduralLegPlacement2D[] limbs;

    [Tooltip("Distance the body must travel (world units) before the next step fires.")]
    public float stepDistance = 0.3f;

    [Tooltip("If true, two consecutive limbs step together (useful for symmetrical gaits).")]
    public bool  doubleStep   = false;

    // ── Step Sound ────────────────────────────────────────────────────────────

    [Header("Step Sound")]
    public AudioSource stepAudio;

    [Tooltip("Delay in seconds between a step trigger and the footstep sound. " +
             "Set to ~half stepDuration so the sound lands when the foot does.")]
    public float stepSoundDelay = 0.1f;

    // ── Private ───────────────────────────────────────────────────────────────

    private Rigidbody2D _rb;
    [SerializeField] private InputReader _input;
    // Distance-based stepping
    private Vector2 _lastPosition;
    private float   _distanceTraveled;
    private int     _stepIndex;

    // Deferred step sound
    private bool  _pendingSound;
    private float _soundTimer;

    // ─────────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        _rb    = GetComponent<Rigidbody2D>();
        _lastPosition = transform.position;
    }

    private void FixedUpdate()
    {
        Vector2 input = _input != null ? _input.MoveInput : Vector2.zero;
        ApplyMovement(input);
        ApplyBraking();
        ClampSpeeds();
        UpdateGravity();
        UpdateLimbs();
    }

    private void Update()
    {
        TickStepSound();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Movement (mirrors Senti_Manual_movement)
    // ─────────────────────────────────────────────────────────────────────────

    private void ApplyMovement(Vector2 input)
    {
        //moves the player vertical and horizontal
        _rb.AddForce((Vector2)transform.right * input.x * moveForce);
        _rb.AddForce(Vector2.up * input.y * moveForce);
    }

    private void ApplyBraking()
    {
        // Exponential decay toward zero — same Lerp approach as Senti's BreakSystem
        _rb.linearVelocity  = Vector2.Lerp(_rb.linearVelocity, Vector2.zero, brakeDamping);
        _rb.angularVelocity = Mathf.Lerp(_rb.angularVelocity, 0f,           brakeDamping);
    }

    private void ClampSpeeds()
    {
        if (_rb.linearVelocity.magnitude > maxSpeed)
            _rb.linearVelocity = _rb.linearVelocity.normalized * maxSpeed;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Gravity: disabled when enough limbs are grounded
    // ─────────────────────────────────────────────────────────────────────────

    private void UpdateGravity()
    {
        int grounded = CountGroundedLimbs();
        _rb.gravityScale = grounded >= minGroundedLimbs ? gravityGrounded : gravityAirborne;
    }
    private int CountGroundedLimbs()
    {
        if (limbs == null) return 0;
        int count = 0;
        foreach (var limb in limbs)
            if (limb != null && limb.legGrounded) count++;
        return count;
    }
    // ─────────────────────────────────────────────────────────────────────────
    // Procedural leg stepping (mirrors Senti's ProceduralLegs)
    // ─────────────────────────────────────────────────────────────────────────
    private void UpdateLimbs()
    {
        if (limbs == null || limbs.Length == 0) return;

        Vector2 velocity = _rb.linearVelocity;

        // Propagate velocity to every limb for predictive foot placement
        foreach (var limb in limbs)
            if (limb != null)
                limb.MoveVelocity(velocity);

        // Accumulate distance travelled
        float moved = Vector2.Distance(transform.position, _lastPosition);
        _distanceTraveled += moved;
        _lastPosition      = transform.position;

        // Trigger the next step once the threshold is crossed
        if (_distanceTraveled >= stepDistance)
        {
            _distanceTraveled = 0f;
            TriggerNextStep();
        }
    }
    private void TriggerNextStep()
    {
        if (limbs == null || limbs.Length == 0) return;

        // Skip null entries so the sequence stays clean
        int tries = 0;
        while (tries < limbs.Length && limbs[_stepIndex] == null)
        {
            _stepIndex = (_stepIndex + 1) % limbs.Length;
            tries++;
        }

        if (limbs[_stepIndex] == null) return;

        limbs[_stepIndex].Step();

        // Optional: fire a paired limb (opposite side of body) simultaneously
        if (doubleStep)
        {
            int partner = (_stepIndex + 1) % limbs.Length;
            if (limbs[partner] != null)
                limbs[partner].Step();
        }

        if (stepAudio != null)
        {
            _pendingSound = true;
            _soundTimer   = 0f; // reset so delay is measured from this step
        }

        _stepIndex = (_stepIndex + 1) % limbs.Length;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Step sound (deferred, mirrors Senti's stepSoundOffset logic)
    // ─────────────────────────────────────────────────────────────────────────
    private void TickStepSound()
    {
        if (!_pendingSound || stepAudio == null) return;

        _soundTimer += Time.deltaTime;
        if (_soundTimer >= stepSoundDelay)
        {
            _soundTimer   = 0f;
            _pendingSound = false;
            stepAudio.Play();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Editor Gizmos
    // ─────────────────────────────────────────────────────────────────────────
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Speed bar above the creature
        float speedRatio = _rb != null ? _rb.linearVelocity.magnitude / maxSpeed : 0f;
        Gizmos.color = Color.Lerp(Color.green, Color.red, speedRatio);
        Gizmos.DrawLine(
            (Vector2)transform.position + Vector2.up * 0.6f,
            (Vector2)transform.position + Vector2.up * 0.6f + (Vector2)transform.right * speedRatio * 0.5f);

        // Gravity state dot
        Gizmos.color = _rb != null && _rb.gravityScale <= 0.01f ? Color.cyan : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.12f);
    }
}
