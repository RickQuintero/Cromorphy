using UnityEngine;

public enum PlayerState { Normal, Water }
public enum MovementState { Movement, Ragdoll }

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

    [Tooltip("Seconds after leaving the ground the player can still jump (prevents missed jumps at edges).")]
    public float coyoteTime = 0.12f;

    [Tooltip("Seconds a jump press is remembered before landing (jump early, executes on touch).")]
    public float jumpBufferTime = 0.12f;

    [Tooltip("All Rigidbody2D bodies in the chain. The root RB is added automatically — list only the additional ones here.")]
    public Rigidbody2D[] bodyRigidbodies;

    // ── Water ─────────────────────────────────────────────────────────────
    [Header("Water")]
    [Tooltip("Layer mask for water volumes.")]
    public LayerMask waterLayer;

    [Tooltip("Minimum ray hits against waterLayer before switching to Water state.")]
    public int waterHitsRequired = 3;

    [Tooltip("Gravity scale applied to all bodies while in water (near-zero = floaty).")]
    public float waterGravity = 0.05f;

    [Tooltip("Move force while in water (applied on both axes).")]
    public float waterMoveForce = 8f;

    [Tooltip("Max speed while in water.")]
    public float waterMaxSpeed = 5f;

    [Tooltip("Damping applied to all bodies while in water.")]
    public float waterDamping = 3f;

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

    // ── Ground Detection ──────────────────────────────────────────────────
    [Header("Ground Detection")]
    [Tooltip("How far each of the 8 directional water rays travels.")]
    public float groundCheckDistance = 0.7f;

    [Tooltip("How many limbs must be grounded before the player is considered grounded (default 2).")]
    public int groundedLimbsRequired = 2;

    // ── Head ──────────────────────────────────────────────────────────────
    [Header("Head")]
    public Transform Head;

    [Tooltip("Sprite renderers that flip on the X axis when the player changes direction.")]
    public SpriteRenderer[] flipSprites;

    [Tooltip("Max tilt angle (degrees) when moving fully up or down.")]
    public float maxHeadTilt   = 30f;
    public float headTiltSpeed = 6f;

    // ── Procedural Legs ───────────────────────────────────────────────────
    [Header("Procedural Legs")]
    public ProceduralLegPlacement2D[] limbs;
    public float stepDistance = 0.3f;
    public bool  doubleStep   = false;

    public void SetGrassSpeedMultiplier(float multiplier) => _grassSpeedMultiplier = multiplier;

    // ── Ragdoll ───────────────────────────────────────────────────────────
    [Header("Ragdoll")]
    [Tooltip("Force magnitude used to pull the player toward the killer's position when carried.")]
    public float killerFollowForce = 20f;

    // ── Step Sound ────────────────────────────────────────────────────────
    [Header("Step Sound")]
    public AudioSource stepAudio;
    public float stepSoundDelay = 0.1f;

    // ── Private ───────────────────────────────────────────────────────────
    private Rigidbody2D _rb;
    [SerializeField] private InputReader _input;

    private PlayerState    _state         = PlayerState.Normal;
    private MovementState  _movementState = MovementState.Movement;
    private Transform      _killerTransform;
    private Vector2        _killerOffset;

    private bool    _isGrounded;
    private Vector2 _groundNormal = Vector2.up;
    private bool  _jumpQueued;
    private bool  _jumpConsumed;
    private float _jumpTime        = -999f;
    private float _lastGroundedTime = -999f;  // coyote time
    private float _jumpPressedTime  = -999f;  // jump buffer

    // True during the lockout window right after a jump fires
    private bool IsJumping => Time.time < _jumpTime + jumpLockoutDuration;

    private float   _grassSpeedMultiplier = 1f;

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
        if (_movementState == MovementState.Ragdoll)
        {
            TickRagdollFollow();
            return;
        }

        Vector2 input = _input != null ? _input.MoveInput : Vector2.zero;
        UpdateGravity();
        ApplyMovement(input);
        //ApplyBraking();
        ClampSpeeds();
        UpdateLimbs();

        if (_jumpQueued && _state == PlayerState.Normal)
        {
            _jumpQueued = false;
            DoJump(input);
        }
        else
        {
            _jumpQueued = false;
        }
    }

    private void Update()
    {
        if (_movementState == MovementState.Ragdoll) return;

        Vector2 input = _input != null ? _input.MoveInput : Vector2.zero;

        if (_input != null)
        {
            // Record exact frame the button was pressed for jump buffering
            if (_input.JumpDown) _jumpPressedTime = Time.time;
            if (!_input.JumpHeld) _jumpConsumed = false;
        }

        // Coyote time: allow jumping briefly after walking off a ledge
        // Jump buffer: honour a press made slightly before landing
        bool canJump       = Time.time - _lastGroundedTime <= coyoteTime && !IsJumping;
        bool bufferedJump  = Time.time - _jumpPressedTime  <= jumpBufferTime;
        if (canJump && bufferedJump && !_jumpConsumed)
            _jumpQueued = true;

        SetOrientation(input);
        SetHeadRotation(input);
        MoveByRotation();
        MoveArmsByRotation();
        TickStepSound();
    }

    // ── Movement ──────────────────────────────────────────────────────────

    private void ApplyMovement(Vector2 input)
    {
        if (_state == PlayerState.Water)
        {
            // Full 2D movement in water — both axes always active
            _rb.AddForce(new Vector2(input.x, input.y) * waterMoveForce, ForceMode2D.Force);
            return;
        }

        // No movement force while airborne — horizontal included
        if (!_isGrounded) return;

        _rb.AddForce((Vector2)transform.right * input.x * moveForce, ForceMode2D.Force);
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
        if (_state == PlayerState.Water)
        {
            if (_rb.linearVelocity.magnitude > waterMaxSpeed)
                _rb.linearVelocity = _rb.linearVelocity.normalized * waterMaxSpeed;
            return;
        }

        if (IsJumping) return;
        if (!_isGrounded) return; // only clamp on ground — in air, let the player overspeed a bit for better jump arcs and midair control
        float effectiveMaxSpeed = maxSpeed * _grassSpeedMultiplier;
        if (_rb.linearVelocity.magnitude > effectiveMaxSpeed)
            _rb.linearVelocity = _rb.linearVelocity.normalized * effectiveMaxSpeed;
    }

    // ── Jump ──────────────────────────────────────────────────────────────

    private void DoJump(Vector2 input)
    {
        // No _isGrounded check here — coyote + buffer logic in Update already gates this
        _jumpTime        = Time.time;
        _jumpConsumed    = true;
        _jumpPressedTime = -999f;   // consume the buffer so it can't re-fire on landing
        _lastGroundedTime = -999f;  // consume coyote time so it can't double-jump

        // Jump in the input direction; fall back to the surface normal when no input is held.
        Vector2 dir     = input.sqrMagnitude > 0.01f ? input.normalized : _groundNormal;
        Vector2 impulse = dir * jumpForce;
        AudioManager.Instance.PlayEffect("JUMPSOUND");
        _rb.AddForce(impulse, ForceMode2D.Force);

        if (bodyRigidbodies != null)
            foreach (var rb in bodyRigidbodies)
                if (rb != null) rb.AddForce(impulse, ForceMode2D.Force);
    }

    // ── Gravity ───────────────────────────────────────────────────────────

    private void UpdateGravity()
    {
        // ── Water detection ───────────────────────────────────────────────
        int waterHits = 0;
        foreach (var dir in _rayDirs)
        {
            RaycastHit2D hit = Physics2D.Raycast(
                transform.position, dir, groundCheckDistance, waterLayer);
            if (hit.collider != null) waterHits++;
        }
        _state = waterHits >= waterHitsRequired ? PlayerState.Water : PlayerState.Normal;

        // ── Water physics ─────────────────────────────────────────────────
        if (_state == PlayerState.Water)
        {
            _isGrounded      = false;
            _rb.gravityScale = waterGravity;
            if (bodyRigidbodies != null)
                foreach (var rb in bodyRigidbodies)
                    if (rb != null)
                    {
                        rb.gravityScale   = waterGravity;
                        rb.linearDamping  = waterDamping;
                        rb.angularDamping = waterDamping;
                    }
            return;
        }

        // ── Normal: jump lockout window ───────────────────────────────────
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

        // ── Normal: ground detection via limbs ───────────────────────────────
        int     groundedCount = 0;
        Vector2 normalSum     = Vector2.zero;

        ProceduralLegPlacement2D[] allLimbs = { LeftLegLimb, RightLegLimb, LeftArmLimb, RightArmLimb };
        foreach (var limb in allLimbs)
        {
            if (limb != null && limb.legGrounded)
            {
                groundedCount++;
                normalSum += limb.SurfaceNormal;
            }
        }

        _isGrounded = groundedCount >= groundedLimbsRequired;
        if (_isGrounded)
        {
            _groundNormal     = normalSum.normalized;
            _lastGroundedTime = Time.time;
        }

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
        if (input.x < -0.05f)
        {
            if (Head != null) Head.localScale = new Vector3(-1f, Head.localScale.y, 1f);
            if (flipSprites != null)
                foreach (var sr in flipSprites)
                    if (sr != null) sr.flipY = true;
        }
        else if (input.x > 0.05f)
        {
            if (Head != null) Head.localScale = new Vector3(1f, Head.localScale.y, 1f);
            if (flipSprites != null)
                foreach (var sr in flipSprites)
                    if (sr != null) sr.flipY = false;
        }
    }

    private void SetHeadRotation(Vector2 input)
    {
        if (Head == null) return;

        // Tilt toward the vertical input direction; returns to 0 when released
        float targetAngle = input.y * maxHeadTilt * Head.localScale.x; // flip tilt direction when facing left
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

        // In water: step threshold is halved so limbs flow more actively
        float threshold = _state == PlayerState.Water ? stepDistance * 0.5f : stepDistance;

        if (_distanceTraveled >= threshold)
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

        if (doubleStep || _state == PlayerState.Water)
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

    // ── Ragdoll ───────────────────────────────────────────────────────────

    /// <summary>
    /// Switches the player into a physics-driven dead state.
    /// killer == null → fall freely (spike death).
    /// killer != null → loosely follow that transform (carried by predator).
    /// </summary>
    public void EnterRagdoll(Transform killer = null)
    {
        if (_movementState == MovementState.Ragdoll) return;
        _movementState   = MovementState.Ragdoll;
        _killerTransform = killer;

        if (killer != null)
            _killerOffset = (Vector2)transform.position - (Vector2)killer.position;

        _rb.gravityScale   = 1f;
        _rb.linearDamping  = 0f;
        _rb.angularDamping = 0f;

        if (bodyRigidbodies != null)
            foreach (var rb in bodyRigidbodies)
                if (rb != null)
                {
                    rb.gravityScale   = 1f;
                    rb.linearDamping  = 0f;
                    rb.angularDamping = 0f;
                }

        GameManager.Instance?.TriggerDeath();
    }

    private void TickRagdollFollow()
    {
        if (_killerTransform == null || !_killerTransform.gameObject.activeInHierarchy) return;
        Vector2 target = (Vector2)_killerTransform.position + _killerOffset;
        _rb.AddForce((target - _rb.position) * killerFollowForce, ForceMode2D.Force);
    }

    // ── Gizmos ────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        foreach (var dir in _rayDirs)
        {
            // Water rays
            RaycastHit2D waterHit = Physics2D.Raycast(
                transform.position, dir, groundCheckDistance, waterLayer);
            if (waterHit.collider != null)
            {
                Gizmos.color = new Color(0f, 0.5f, 1f, 0.8f);
                Gizmos.DrawWireSphere(waterHit.point, 0.05f);
            }
        }

        // State ring: cyan = grounded, blue = water, grey = airborne
        if (Application.isPlaying)
        {
            if (_state == PlayerState.Water)
                Gizmos.color = new Color(0f, 0.5f, 1f, 1f);
            else
                Gizmos.color = _isGrounded ? Color.cyan : Color.grey;
        }
        else
        {
            Gizmos.color = Color.grey;
        }
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
