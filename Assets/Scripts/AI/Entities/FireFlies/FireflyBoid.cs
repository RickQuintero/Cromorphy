using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Rigidbody2D))]
public class FireflyBoid : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light2D boidLight;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Movement")]
    [SerializeField] private float maxSpeed      = 3f;
    [SerializeField] private float arrivalRadius = 0.8f;
    [SerializeField] private float seekWeight    = 1f;

    [Header("Boid Forces")]
    [SerializeField] private float neighborRadius    = 2.5f;
    [SerializeField] private float separationRadius  = 0.8f;
    [SerializeField] private float separationWeight  = 1.5f;
    [SerializeField] private float cohesionWeight    = 0.4f;
    [SerializeField] private float alignmentWeight   = 0.6f;

    private Rigidbody2D _rb;
    private Vector2 _targetPoint;
    private bool _wasDay;
    private bool _returned;

    private static readonly Collider2D[] _neighborBuffer = new Collider2D[32];

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        _returned = false;
        _wasDay = DayLightController.Instance != null && DayLightController.Instance.IsDay;
        if (boidLight != null) boidLight.enabled = !_wasDay;
    }

    private void FixedUpdate()
    {
        if (DayLightController.Instance == null) return;

        bool isDay = DayLightController.Instance.IsDay;

        if (isDay != _wasDay)
        {
            _wasDay = isDay;
            if (isDay)
            {
                ReturnSelf();
                return;
            }
            if (boidLight != null) boidLight.enabled = true;
        }

        if (isDay) return;

        Vector2 pos = _rb.position;
        Vector2 steering = ComputeSteering(pos);
        float dist = Vector2.Distance(pos, _targetPoint);
        float speed = maxSpeed * Mathf.Clamp01(dist / arrivalRadius);

        _rb.linearVelocity = Vector2.ClampMagnitude(steering, speed);
    }

    private Vector2 ComputeSteering(Vector2 pos)
    {
        Vector2 seek      = Vector2.zero;
        Vector2 separation = Vector2.zero;
        Vector2 avgPos    = Vector2.zero;
        Vector2 avgVel    = Vector2.zero;
        int neighborCount = 0;

        Vector2 toTarget = _targetPoint - pos;
        if (toTarget.sqrMagnitude > 0.001f)
            seek = toTarget.normalized * seekWeight;

        int count = Physics2D.OverlapCircleNonAlloc(pos, neighborRadius, _neighborBuffer);
        for (int i = 0; i < count; i++)
        {
            FireflyBoid other = _neighborBuffer[i].GetComponent<FireflyBoid>();
            if (other == null || other == this) continue;

            Vector2 otherPos = (Vector2)other.transform.position;
            float dist = Vector2.Distance(pos, otherPos);

            if (dist < separationRadius && dist > 0f)
                separation += (pos - otherPos) / dist;

            avgPos += otherPos;
            avgVel += other._rb.linearVelocity;
            neighborCount++;
        }

        Vector2 cohesion  = Vector2.zero;
        Vector2 alignment = Vector2.zero;
        if (neighborCount > 0)
        {
            cohesion  = ((avgPos / neighborCount) - pos).normalized * cohesionWeight;
            alignment = (avgVel / neighborCount).normalized * alignmentWeight;
        }

        return seek + separation * separationWeight + cohesion + alignment;
    }

    /// <summary>Sets the world-space point this boid steers toward. Called every frame by FireflySwarm.</summary>
    public void SetTarget(Vector2 point) => _targetPoint = point;

    /// <summary>Returns this boid to the pool. Safe to call multiple times.</summary>
    public void ReturnSelf()
    {
        if (_returned) return;
        _returned = true;
        _rb.linearVelocity = Vector2.zero;
        if (boidLight != null) boidLight.enabled = false;
        GetComponent<EntityPoolMember>()?.ReturnToPool();
    }
}
