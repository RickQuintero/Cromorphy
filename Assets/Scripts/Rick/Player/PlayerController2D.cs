using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    // ── Movement ──────────────────────────────────────────────────────────
    [Header("Movement")]
    public float moveForce    = 12f;
    public float maxSpeed     = 8f;
    public float brakeDamping = 0.12f;

    // ── Jump ──────────────────────────────────────────────────────────────
    [Header("Jump")]
    public float jumpForce = 16f;

    [Tooltip("Linear and angular damping applied to bodyRigidbodies when grounded (0 = free-fall).")]
    public float groundedDamping = 4f;

    [Tooltip("Seconds after jumping during which ground-check is suppressed and braking is skipped.")]
    public float jumpLockoutDuration = 0.25f;

    [Tooltip("All Rigidbody2D bodies in the chain. The root RB is added automatically — list only the additional ones here.")]
    public Rigidbody2D[] bodyRigidbodies;

    // ── Leg Root by Contact ───────────────────────────────────────────────
    [Header("Leg Root by Contact")]
    [Tooltip("Central pivot. Roots are placed opposite the contact point, at legRootOffset from this.")]
    public Transform BodyPartToFollow;
    public Transform LeftLegRoot;
    [Tooltip("The leg limb whose ground contact drives the left root position.")]
    public ProceduralLegPlacement2D LeftLegLimb;
    public Transform RightLegRoot;
    [Tooltip("The leg limb whose ground contact drives the right root position.")]
    public ProceduralLegPlacement2D RightLegLimb;
    [Tooltip("Distance from the pivot to the root (world units).")]
    public float legRootOffset = 0.3f;

    // ── Arm Root by Contact ───────────────────────────────────────────────
    [Header("Arm Root by Contact")]
    public Transform TorsoPartToFollow;
    public Transform LeftArmRoot;
    public ProceduralLegPlacement2D LeftArmLimb;
    public Transform RightArmRoot;
    public ProceduralLegPlacement2D RightArmLimb;
    public float armRootOffset = 0.3f;

    // ── Root Smoothing ────────────────────────────────────────────────────
    [Header("Root Smoothing")]
    [Tooltip("How fast the roots lerp toward their target world position.")]
    public float rootLerpSpeed = 8f;

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

    private bool    _isGrounded;
    private Vector2 _groundNormal = Vector2.up; // averaged normal of all active ray hits
    private bool  _jumpQueued;
    private bool  _jumpConsumed;
    private float _jumpTime = -999f;

    // True during the lockout window right after a jump fires
    private bool IsJumping => Time.time < _jumpTime + jumpLockoutDuration;

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

    private void Start()
    {
        _rb           = GetComponent<Rigidbody2D>();
        _lastPosition = transform.position;
    }

    private void FixedUpdate()
    {
        Vector2 input = _input != null ? _input.MoveInput : Vector2.zero;
        UpdateGravity();
        ApplyMovement(input);
        //ApplyBraking();
        ClampSpeeds();
        UpdateLimbs();

        if (_jumpQueued)
        {
            _jumpQueued = false;
            DoJump(input);
        }
    }

    private void Update()
    {
        Vector2 input = _input != null ? _input.MoveInput : Vector2.zero;

        bool wantJump = _input != null && _input.JumpHeld;
        if (wantJump && _isGrounded && !_jumpConsumed && !IsJumping)
            _jumpQueued = true;
        if (!wantJump)
            _jumpConsumed = false;

        SetOrientation(input);
        SetHeadRotation(input);
        MoveByRotation();
        MoveArmsByRotation();
        TickStepSound();
    }

    // ── Movement ──────────────────────────────────────────────────────────

    private void ApplyMovement(Vector2 input)
    {
        _rb.AddForce((Vector2)transform.right * input.x * moveForce, ForceMode2D.Force);
        if (!_isGrounded) return;
        _rb.AddForce(Vector2.up * input.y * moveForce, ForceMode2D.Force);
    }

    private void ApplyBraking()
    {
        // Skip braking entirely during the jump lockout — don't eat the impulse
        if (IsJumping) return;

        if (_isGrounded)
        {
            _rb.linearVelocity = Vector2.Lerp(_rb.linearVelocity, Vector2.zero, brakeDamping);
        }
        else
        {
            // Airborne: only damp horizontal, leave vertical (gravity + jump arc) alone
            float dampedX = Mathf.Lerp(_rb.linearVelocity.x, 0f, brakeDamping);
            _rb.linearVelocity = new Vector2(dampedX, _rb.linearVelocity.y);
        }
        _rb.angularVelocity = Mathf.Lerp(_rb.angularVelocity, 0f, brakeDamping);
    }

    private void ClampSpeeds()
    {
        if (IsJumping) return;
        if (!_isGrounded) return; // only clamp on ground — in air, let the player overspeed a bit for better jump arcs and midair control
        if (_rb.linearVelocity.magnitude > maxSpeed)
            _rb.linearVelocity = _rb.linearVelocity.normalized * maxSpeed;
    }

    // ── Jump ──────────────────────────────────────────────────────────────

    private void DoJump(Vector2 input)
    {
        if (!_isGrounded) return;

        _jumpTime     = Time.time;
        _jumpConsumed = true;

        // Jump in the input direction; fall back to the surface normal when no input is held.
        Vector2 dir     = input.sqrMagnitude > 0.01f ? input.normalized : _groundNormal;
        Vector2 impulse = dir * jumpForce;

        _rb.AddForce(impulse, ForceMode2D.Force);

        if (bodyRigidbodies != null)
            foreach (var rb in bodyRigidbodies)
                if (rb != null) rb.AddForce(impulse, ForceMode2D.Force);
    }

    // ── Gravity ───────────────────────────────────────────────────────────

    private void UpdateGravity()
    {
        // During the lockout window after a jump: stay airborne, don't re-check
        if (IsJumping)
        {
            _isGrounded      = false;
            _rb.gravityScale = 1f;
            if (bodyRigidbodies != null)
                foreach (var rb in bodyRigidbodies)
                    if (rb != null)
                    {
                        rb.gravityScale   = 1f;
                        rb.linearDamping  = 0f;
                        rb.angularDamping = 0f;
                    }
            return;
        }

        _isGrounded = false;
        Vector2 normalSum = Vector2.zero;
        int     hitCount  = 0;

        foreach (var dir in _rayDirs)
        {
            RaycastHit2D hit = Physics2D.Raycast(
                transform.position, dir, groundCheckDistance, solidLayer);

            if (hit.collider == null) continue;

            _isGrounded = true;
            normalSum  += hit.normal;
            hitCount++;
        }

        // Average all hit normals → jump pushes away from every touched surface
        if (hitCount > 0)
            _groundNormal = normalSum.normalized;

        // Grounded = gravityScale 0 (stick to surface). Airborne = 1 (normal Unity gravity).
        float gravity = _isGrounded ? 0f : 1f;
        float damping = _isGrounded ? groundedDamping : 0f;
        _rb.gravityScale = gravity;
        if (bodyRigidbodies != null)
            foreach (var rb in bodyRigidbodies)
                if (rb != null)
                {
                    rb.gravityScale    = gravity;
                    rb.linearDamping   = damping;
                    rb.angularDamping  = damping;
                }
    }

    // ── Head ──────────────────────────────────────────────────────────────

    public void SetOrientation(Vector2 input)
    {
        if (Head == null) return;

        // X: flip based on horizontal movement direction
        float scaleX = Head.localScale.x;
        if      (input.x < -0.05f) scaleX =  -1f;
        else if (input.x >  0.05f) scaleX = 1f;

        Head.localScale = new Vector3(scaleX, Head.localScale.y, 1f);
    }

    private void SetHeadRotation(Vector2 input)
    {
        if (Head == null) return;

        // Tilt toward the vertical input direction; returns to 0 when released
        float targetAngle = -input.y * maxHeadTilt * Head.localScale.x; // flip tilt direction when facing left
        float angle = Mathf.LerpAngle(Head.localEulerAngles.z, targetAngle, Time.deltaTime * headTiltSpeed);
        //only roates when moving in any direction
        if (input.magnitude > 0.1f)
        Head.localEulerAngles = new Vector3(0f, 0f, angle);
    }

    // ── Legs ──────────────────────────────────────────────────────────────

    private void UpdateLimbs()
    {
        if (limbs == null || limbs.Length == 0) return;

        Vector2 velocity = _rb.linearVelocity;
        foreach (var limb in limbs)
            if (limb != null) limb.MoveVelocity(velocity);

        float moved = Vector2.Distance(transform.position, _lastPosition);
        _distanceTraveled += moved;
        _lastPosition      = transform.position;

        if (_distanceTraveled >= stepDistance)
        {
            _distanceTraveled = 0f;
            TriggerNextStep();
        }
    }

    private void TriggerNextStep()
    {
        if (limbs == null || limbs.Length == 0) return;

        int tries = 0;
        while (tries < limbs.Length && limbs[_stepIndex] == null)
        {
            _stepIndex = (_stepIndex + 1) % limbs.Length;
            tries++;
        }
        if (limbs[_stepIndex] == null) return;

        limbs[_stepIndex].Step();

        if (doubleStep)
        {
            int partner = (_stepIndex + 1) % limbs.Length;
            if (limbs[partner] != null) limbs[partner].Step();
        }

        if (stepAudio != null) { _pendingSound = true; _soundTimer = 0f; }

        _stepIndex = (_stepIndex + 1) % limbs.Length;
    }

    // ── IK Roots ─────────────────────────────────────────────────────────

    // Moves 'root' to the opposite side of 'pivot' from the contact point.
    private void MoveRootByContact(
        Transform pivot, Transform root,
        ProceduralLegPlacement2D limb, float offset)
    {
        if (pivot == null || root == null || limb == null) return;

        Vector2 pivotPos  = pivot.position;
        Vector2 contact   = limb.GroundContact;
        Vector2 toContact = contact - pivotPos;

        if (toContact.sqrMagnitude < 0.001f) return;

        Vector2 worldTarget = pivotPos - toContact.normalized * offset;
        root.position = Vector2.Lerp(root.position, worldTarget, Time.deltaTime * rootLerpSpeed);
    }

    public void MoveByRotation()
    {
        MoveRootByContact(BodyPartToFollow, LeftLegRoot,  LeftLegLimb,  legRootOffset);
        MoveRootByContact(BodyPartToFollow, RightLegRoot, RightLegLimb, legRootOffset);
    }

    public void MoveArmsByRotation()
    {
        MoveRootByContact(TorsoPartToFollow, LeftArmRoot,  LeftArmLimb,  armRootOffset);
        MoveRootByContact(TorsoPartToFollow, RightArmRoot, RightArmLimb, armRootOffset);
    }

    // ── Step Sound ────────────────────────────────────────────────────────

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

    // ── Gizmos ────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        foreach (var dir in _rayDirs)
        {
            RaycastHit2D hit = Physics2D.Raycast(
                transform.position, dir, groundCheckDistance, solidLayer);
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

        // Averaged ground normal → jump direction
        if (Application.isPlaying && _isGrounded)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(
                transform.position,
                (Vector2)transform.position + _groundNormal * 0.5f);
        }
    }
}
