using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Ent_Fly : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed          = 3f;
    [SerializeField] private float dirChangeMin   = 1.5f;  // seconds between random direction changes
    [SerializeField] private float dirChangeMax   = 4f;

    [Header("Obstacle Detection")]
    [SerializeField] private float rayLength      = 0.6f;
    [SerializeField] private LayerMask groundMask;

    [Header("Debug")]
    [SerializeField] private bool drawRays        = true;

    private Rigidbody2D _rb;
    private Vector2     _direction;
    private float       _dirTimer;
    private float       _dirInterval;

    // 8 compass directions (right, up-right, up, up-left, left, down-left, down, down-right)
    private static readonly Vector2[] _rays = new Vector2[8];

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;

        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 0.25f;
            _rays[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        PickNewDirection();
    }

    private void FixedUpdate()
    {
        // Random direction timer
        _dirTimer += Time.fixedDeltaTime;
        if (_dirTimer >= _dirInterval)
            PickNewDirection();

        // Obstacle avoidance: if any ray hits, steer away
        Vector2 avoidance = Vector2.zero;
        for (int i = 0; i < 8; i++)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, _rays[i], rayLength, groundMask);
            if (hit.collider != null)
                avoidance -= _rays[i] * (1f - hit.fraction); // stronger push the closer the wall
        }

        Vector2 desired = _direction;
        if (avoidance != Vector2.zero)
        {
            desired = (_direction + avoidance).normalized;
            // If we're nearly fully blocked, pick a fresh random direction immediately
            if (avoidance.magnitude > 1.2f)
                PickNewDirection();
        }

        _rb.linearVelocity = desired * speed;
    }

    private void PickNewDirection()
    {
        _direction   = Random.insideUnitCircle.normalized;
        _dirTimer    = 0f;
        _dirInterval = Random.Range(dirChangeMin, dirChangeMax);
    }

    // Called by TongueTrigger before returning this object to the pool.
    public void ResetForPool()
    {
        if (_rb != null) _rb.bodyType = RigidbodyType2D.Dynamic;
        PickNewDirection();
    }

    private void OnDrawGizmos()
    {
        if (!drawRays) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < 8; i++)
        {
            float a   = i * Mathf.PI * 0.25f;
            Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            Gizmos.DrawRay(transform.position, d * rayLength);
        }
    }
}
