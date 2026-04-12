using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    // ── Movement ──────────────────────────────────────────────────────────
    [Header("Movement")]
    public float moveForce    = 12f;
    public float maxSpeed     = 8f;
    public float brakeDamping = 0.12f;

    // ── Gravity ───────────────────────────────────────────────────────────
    [Header("Gravity")]
    public float gravityGrounded = 0f;
    public float gravityAirborne = 3f;

    // ── Ground Detection (8 rays) ─────────────────────────────────────────
    [Header("Ground Detection")]
    public LayerMask solidLayer;

    [Tooltip("How far each of the 8 directional rays travels before giving up.")]
    public float groundCheckDistance = 0.7f;

    // ── Head ──────────────────────────────────────────────────────────────
    [Header("Head")]
    public Transform Head;

    [Tooltip("Max tilt angle (degrees) when moving fully up or down.")]
    public float maxHeadTilt   = 30f;
    public float headTiltSpeed = 6f;

    // ── Procedural Legs ───────────────────────────────────────────────────
    [Header("Procedural Legs")]
    public ProceduralLegPlacement2D[] limbs;
    public float stepDistance = 0.3f;
    public bool  doubleStep   = false;

    // ── Step Sound ────────────────────────────────────────────────────────
    [Header("Step Sound")]
    public AudioSource stepAudio;
    public float stepSoundDelay = 0.1f;

    // ── Private ───────────────────────────────────────────────────────────
    private Rigidbody2D _rb;
    [SerializeField] private InputReader _input;

    private bool  _isGrounded;
    private Vector2 _lastPosition;
    private float   _distanceTraveled;
    private int     _stepIndex;
    private bool    _pendingSound;
    private float   _soundTimer;

    // 8 directions: cardinal + diagonal
    private static readonly Vector2[] _rayDirs = {
        Vector2.up,
        Vector2.down,
        Vector2.left,
        Vector2.right,
        new Vector2( 1f,  1f).normalized,
        new Vector2(-1f,  1f).normalized,
        new Vector2( 1f, -1f).normalized,
        new Vector2(-1f, -1f).normalized,
    };

    // ─────────────────────────────────────────────────────────────────────

    private void Start() {
        _rb           = GetComponent<Rigidbody2D>();
        _lastPosition = transform.position;
    }

    private void FixedUpdate() {
        Vector2 input = _input != null ? _input.MoveInput : Vector2.zero;
        ApplyMovement(input);
        ApplyBraking();
        ClampSpeeds();
        UpdateGravity();
        UpdateLimbs();
    }

    private void Update() {
        Vector2 input = _input != null ? _input.MoveInput : Vector2.zero;
        SetOrientation(input);
        SetHeadRotation(input);
        TickStepSound();
    }

    // ── Movement ──────────────────────────────────────────────────────────

    private void ApplyMovement(Vector2 input) {
        _rb.AddForce((Vector2)transform.right * input.x * moveForce);
        _rb.AddForce(Vector2.up               * input.y * moveForce);
    }

    private void ApplyBraking() {
        _rb.linearVelocity  = Vector2.Lerp(_rb.linearVelocity, Vector2.zero, brakeDamping);
        _rb.angularVelocity = Mathf.Lerp(_rb.angularVelocity,  0f,           brakeDamping);
    }

    private void ClampSpeeds() {
        if (_rb.linearVelocity.magnitude > maxSpeed)
            _rb.linearVelocity = _rb.linearVelocity.normalized * maxSpeed;
    }

    // ── Gravity ───────────────────────────────────────────────────────────

    private void UpdateGravity() {
        _isGrounded = false;

        foreach (var dir in _rayDirs) {
            if (Physics2D.Raycast(transform.position, dir, groundCheckDistance, solidLayer)) {
                _isGrounded = true;
                break;
            }
        }

        _rb.gravityScale = _isGrounded ? gravityGrounded : gravityAirborne;
    }

    // ── Head ──────────────────────────────────────────────────────────────

    public void SetOrientation(Vector2 input) {
        if (Head == null) return;
        if      (input.x < -0.05f) Head.localScale = new Vector3( 1f, 1f, 1f);
        else if (input.x >  0.05f) Head.localScale = new Vector3(-1f, 1f, 1f);
    }

    private void SetHeadRotation(Vector2 input) 
    {
        if (Head == null) return;
        float targetAngle = -input.y * maxHeadTilt;
        float currentAngle = Head.localEulerAngles.z;
        if (currentAngle > 179f) currentAngle -= 359f; // Convert
        float newAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * headTiltSpeed);
        Head.localRotation = Quaternion.Euler(0f, 0f, newAngle);
    }

    // ── Legs ──────────────────────────────────────────────────────────────

    private void UpdateLimbs() {
        if (limbs == null || limbs.Length == 0) return;

        Vector2 velocity = _rb.linearVelocity;

        foreach (var limb in limbs)
            if (limb != null) limb.MoveVelocity(velocity);

        float moved = Vector2.Distance(transform.position, _lastPosition);
        _distanceTraveled += moved;
        _lastPosition      = transform.position;

        if (_distanceTraveled >= stepDistance) {
            _distanceTraveled = 0f;
            TriggerNextStep();
        }
    }

    private void TriggerNextStep() {
        if (limbs == null || limbs.Length == 0) return;

        int tries = 0;
        while (tries < limbs.Length && limbs[_stepIndex] == null) {
            _stepIndex = (_stepIndex + 1) % limbs.Length;
            tries++;
        }
        if (limbs[_stepIndex] == null) return;

        limbs[_stepIndex].Step();

        if (doubleStep) {
            int partner = (_stepIndex + 1) % limbs.Length;
            if (limbs[partner] != null) limbs[partner].Step();
        }

        if (stepAudio != null) { _pendingSound = true; _soundTimer = 0f; }

        _stepIndex = (_stepIndex + 1) % limbs.Length;
    }

    // ── Step Sound ────────────────────────────────────────────────────────

    private void TickStepSound() {
        if (!_pendingSound || stepAudio == null) return;
        _soundTimer += Time.deltaTime;
        if (_soundTimer >= stepSoundDelay) {
            _soundTimer   = 0f;
            _pendingSound = false;
            stepAudio.Play();
        }
    }

    // ── Gizmos ────────────────────────────────────────────────────────────

    private void OnDrawGizmos() {
        // 8 directional ground rays
        foreach (var dir in _rayDirs) {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, groundCheckDistance, solidLayer);
            Gizmos.color = hit.collider != null ? Color.green : new Color(1f, 0f, 0f, 0.4f);
            Gizmos.DrawLine(
                transform.position,
                (Vector2)transform.position + dir * groundCheckDistance);
            if (hit.collider != null)
                Gizmos.DrawWireSphere(hit.point, 0.04f);
        }

        // Grounded state ring
        Gizmos.color = Application.isPlaying && _isGrounded ? Color.cyan : Color.grey;
        Gizmos.DrawWireSphere(transform.position, 0.12f);
    }
}
