using UnityEngine;
/// <summary>
/// 2D character controller that moves a main object Head that is the end of a joint chain
/// then the other parts just follow by the joints. This is a more physics-based approach than directly setting the position of the character's root.
/// </summary>
public class PlayerMovement2D : MonoBehaviour
{
    [Header("Input reader")]
    public InputReader Input;
    [Header("Movement")]
    public float MoveSpeed    = 4f;
    public float Acceleration = 20f;
    public float Deceleration = 30f;

    [Header("Legs")]
    [Tooltip("All ProceduralLegPlacement components on this character.")]
    public ProceduralLegPlacement[] Legs;

    [Tooltip("Minimum number of grounded legs required to disable gravity for wall walking.")]
    public int GroundedLegs_ForGravityOff = 4;

    [Tooltip("Distance from the ideal resting foot position at which the leg should step.")]
    public float StepDistanceThreshold = 0.5f;

    [Header("Facing")]
    [Tooltip("Flip the character by changint the flip option in the sprite renderer")]
    public SpriteRenderer SpriteRenderer;

    // ── Public read state ──────────────────────────────────────────────────────
    /// <summary>True when enough legs are grounded (gravity is off).</summary>
    public bool    IsGrounded    { get; private set; }

    /// <summary>How many legs are currently reporting a grounded surface.</summary>
    public int     GroundedCount { get; private set; }

    /// <summary>Current Rigidbody2D velocity (Unity 6 API).</summary>
    public Vector2 Velocity      => _rb.linearVelocity;

    // ── Private ────────────────────────────────────────────────────────────────
    [SerializeField] private Rigidbody2D _rb;
    private float       _moveInput_X;
    private float       _moveInput_Y;

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    private void Update()
    {
        StepByDistance();
    }

    private void FixedUpdate()
    {
        CountGroundedLegs();
        ApplyGravity();
        ApplyMovement();
    }
    // ── Private ────────────────────────────────────────────────────────────────
    private void CountGroundedLegs()
    {
        int count = 0;
        if (Legs == null) return;

        foreach (var leg in Legs)
        {
            if (leg != null && leg.legGrounded)
                count++;
        }

        GroundedCount = count;
        IsGrounded    = count >= GroundedLegs_ForGravityOff;
    }

    private void StepByDistance()
    {
        if (Legs == null) return;

        foreach (ProceduralLegPlacement leg in Legs)
        {
            if (Vector2.Distance(_rb.transform.position, leg.ikTarget.position) > StepDistanceThreshold)
                leg.Step();
        }
    }
    private void ApplyGravity()
    {
        if (IsGrounded)
        {
            _rb.gravityScale = 0f;
        }
        else
        {
            _rb.gravityScale = 1f;
        }
    }
    private void ApplyMovement()
    {
        _moveInput_X = Input.Horizontal;
        _moveInput_Y = Input.Vertical;
        SetFacing(_moveInput_X);
        float target = _moveInput_X * MoveSpeed;
        float rate   = Mathf.Abs(_moveInput_X) > 0.01f ? Acceleration : Deceleration;
        float newX   = Mathf.MoveTowards(_rb.linearVelocity.x, target, rate * Time.fixedDeltaTime);
        _rb.linearVelocity = new Vector2(newX, _rb.linearVelocity.y);
        if (IsGrounded)
        {
            // When grounded, also apply vertical input for climbing.
            float targetY = _moveInput_Y * MoveSpeed;
            float newY    = Mathf.MoveTowards(_rb.linearVelocity.y, targetY, rate * Time.fixedDeltaTime);
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, newY);
        }

    }

    private void SetFacing(float dir)
    {
        if (SpriteRenderer != null)
            SpriteRenderer.flipX = dir < 0f;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        //nothing yet
    }
#endif
}
