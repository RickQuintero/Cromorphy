using UnityEngine;

public class TongueSystemAttack : MonoBehaviour
{
    [Header("Tongue Shape")]
    [SerializeField] private float    segmentLength   = 0.15f;
    [SerializeField] private float    maxRange        = 4f;
    [SerializeField] private float    minThickness    = 0.03f;
    [SerializeField] private float    maxThickness    = 0.14f;
    [SerializeField] private Sprite   segmentSprite;
    [SerializeField] private Material segmentMaterial;
    [SerializeField] private string   sortingLayerName = "Default";
    [SerializeField] private int      sortingOrder    = 5;

    [Header("Tongue Motion")]
    [SerializeField] private float launchSpeed     = 14f;
    [SerializeField] private float retractSpeed    = 9f;
    [SerializeField] private float holdDuration    = 0.25f;
    [SerializeField] private float wiggleFrequency = 10f;
    [SerializeField] private float wiggleAmplitude = 0.06f;

    [Header("Charge")]
    [SerializeField] private float chargeTime = 1.2f;  // hold duration before auto-launch

    [Header("Detection")]
    [SerializeField] private LayerMask preyMask;
    [SerializeField] private LayerMask groundMask;     // fallback surface layer

    [Header("References")]
    [Tooltip("Child of this transform — starts at origin, flies to prey.")]
    public  Transform   ikTarget;
    [Tooltip("Child circle sprite — will be scaled from 0 to maxRange*2 while charging.")]
    [SerializeField] private Transform   rangeIndicator;
    [SerializeField] private InputReader _input;
    [Tooltip("Animator that receives IsAttacking bool (mouth open/close animation).")]
    [SerializeField] private Animator    _mouthAnimator;

    // ── State ─────────────────────────────────────────────────────────────────
    private enum State { Idle, Charging, Launching, HoldingPrey, Retracting }
    private State _state = State.Idle;

    private Segment[] _segments;
    private int       _maxSegments;
    private Vector3   _launchDestination;
    private float     _holdTimer;
    private float     _chargeTimer;

    // ── Setup ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        _maxSegments = Mathf.CeilToInt(maxRange / segmentLength) + 2;
        _segments    = new Segment[_maxSegments];

        for (int i = 0; i < _maxSegments; i++)
        {
            var go = new GameObject("TongSeg_" + i);
            go.transform.SetParent(transform);

            var sr          = go.AddComponent<SpriteRenderer>();
            sr.sprite            = segmentSprite;
            sr.sortingLayerName  = sortingLayerName;
            sr.sortingOrder      = sortingOrder;
            if (segmentMaterial != null) sr.material = segmentMaterial;

            go.SetActive(false);
            _segments[i] = new Segment(go.transform, segmentLength);
        }

        if (rangeIndicator != null) rangeIndicator.gameObject.SetActive(false);
        if (ikTarget       != null) ikTarget.position = transform.position;
    }

    // ── Loop ──────────────────────────────────────────────────────────────────
    void Update()
    {
        HandleInput();
        UpdateState();
        UpdateIK();
    }

    // ── Input ─────────────────────────────────────────────────────────────────
    void HandleInput()
    {
        // Start charging on Aim press
        if (_input.AimDown && _state == State.Idle)
        {
            _state       = State.Charging;
            _chargeTimer = 0f;
            if (rangeIndicator != null) rangeIndicator.gameObject.SetActive(true);
        }

        // Release before full charge → cancel
        if (_input.AimUp && _state == State.Charging)
            ReturnToIdle();
    }

    // ── State machine ─────────────────────────────────────────────────────────
    void UpdateState()
    {
        switch (_state)
        {
            case State.Charging:
            {
                _chargeTimer += Time.deltaTime;
                float t = Mathf.Clamp01(_chargeTimer / chargeTime);

                // Grow the range indicator from 0 to maxRange*2
                if (rangeIndicator != null)
                {
                    float d = maxRange * 2f * t;
                    rangeIndicator.localScale = new Vector3(d, d, 1f);
                }

                if (t >= 1f) Launch();
                break;
            }

            case State.Launching:
            {
                // Auto-retract if player moves out of range mid-flight
                if (Vector3.Distance(transform.position, ikTarget.position) > maxRange * 1.25f)
                { _state = State.Retracting; break; }

                ikTarget.position = Vector3.MoveTowards(
                    ikTarget.position, _launchDestination, launchSpeed * Time.deltaTime);

                if (Vector3.Distance(ikTarget.position, _launchDestination) < 0.05f)
                {
                    ikTarget.position = _launchDestination;
                    _holdTimer        = 0f;
                    _state            = State.HoldingPrey;
                }
                break;
            }

            case State.HoldingPrey:
            {
                _holdTimer += Time.deltaTime;
                if (_holdTimer >= holdDuration) _state = State.Retracting;
                break;
            }

            case State.Retracting:
            {
                ikTarget.position = Vector3.MoveTowards(
                    ikTarget.position, transform.position, retractSpeed * Time.deltaTime);

                if (Vector3.Distance(ikTarget.position, transform.position) < 0.05f)
                {
                    ikTarget.position = transform.position;
                    ReturnToIdle();
                }
                break;
            }
        }
    }

    // ── Launch logic ──────────────────────────────────────────────────────────
    void Launch()
    {
        // 1 — try to find nearest prey
        Collider2D[] preyHits = Physics2D.OverlapCircleAll(transform.position, maxRange, preyMask);
        if (preyHits.Length > 0)
        {
            Collider2D nearest     = preyHits[0];
            float      nearestDist = Vector2.Distance(transform.position, nearest.transform.position);
            for (int i = 1; i < preyHits.Length; i++)
            {
                float d = Vector2.Distance(transform.position, preyHits[i].transform.position);
                if (d < nearestDist) { nearestDist = d; nearest = preyHits[i]; }
            }
            _launchDestination = nearest.transform.position;
            _state = State.Launching;
            SetAttacking(true);
            return;
        }

        // 2 — fallback: cast 16 rays outward, pick the farthest ground point
        Vector3 farthestPoint = transform.position;
        float   farthestDist  = 0f;
        const int fallbackRays = 16;
        for (int i = 0; i < fallbackRays; i++)
        {
            float        angle = i * Mathf.PI * 2f / fallbackRays;
            Vector2      dir   = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            RaycastHit2D hit   = Physics2D.Raycast(transform.position, dir, maxRange, groundMask);
            if (hit.collider != null && hit.distance > farthestDist)
            {
                farthestDist  = hit.distance;
                farthestPoint = hit.point;
            }
        }

        if (farthestDist < 0.1f) { ReturnToIdle(); return; } // nothing in range at all

        _launchDestination = farthestPoint;
        _state = State.Launching;
        SetAttacking(true);
    }

    void SetAttacking(bool attacking)
    {
        if (_mouthAnimator != null)
            _mouthAnimator.SetBool("IsAttacking", attacking);
    }

    void ReturnToIdle()
    {
        _state       = State.Idle;
        _chargeTimer = 0f;
        if (rangeIndicator != null)
        {
            rangeIndicator.localScale = Vector3.zero;
            rangeIndicator.gameObject.SetActive(false);
        }
        SetAttacking(false);
    }

    // ── IK visual ─────────────────────────────────────────────────────────────
    void UpdateIK()
    {
        Vector3 root = transform.position;
        Vector3 tip  = ikTarget.position;
        float   dist = Vector3.Distance(root, tip);

        if (dist < 0.01f)
        {
            for (int i = 0; i < _maxSegments; i++)
                _segments[i].transform.gameObject.SetActive(false);
            return;
        }

        int     needed = Mathf.Clamp(Mathf.CeilToInt(dist / segmentLength), 1, _maxSegments);
        Vector2 dir    = ((Vector2)(tip - root)).normalized;
        Vector2 perp   = new Vector2(-dir.y, dir.x);

        for (int i = 0; i < _maxSegments; i++)
            _segments[i].transform.gameObject.SetActive(i < needed);

        for (int i = 0; i < needed; i++)
        {
            float t        = (i + 0.5f) / needed;
            float envelope = Mathf.Sin(t * Mathf.PI);
            float wiggle   = Mathf.Sin(Time.time * wiggleFrequency - i * 1.2f)
                           * wiggleAmplitude * envelope;

            Vector3 basePos       = Vector3.Lerp(root, tip, t);
            _segments[i].position = basePos + (Vector3)(perp * wiggle);

            Vector3 lookTarget;
            if (i < needed - 1)
            {
                float   tN   = (i + 1.5f) / needed;
                float   envN = Mathf.Sin(tN * Mathf.PI);
                float   wN   = Mathf.Sin(Time.time * wiggleFrequency - (i + 1) * 1.2f)
                             * wiggleAmplitude * envN;
                lookTarget = Vector3.Lerp(root, tip, tN) + (Vector3)(perp * wN);
            }
            else
            {
                lookTarget = tip;
            }
            _segments[i].LookAt(lookTarget);

            float thickness = Mathf.Lerp(maxThickness, minThickness, (float)i / Mathf.Max(needed - 1, 1));
            _segments[i].transform.localScale = new Vector3(segmentLength * 1.1f, thickness, 1f);
        }
    }

    // ── Gizmo ─────────────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, maxRange);
    }

    // ── Segment ───────────────────────────────────────────────────────────────
    [System.Serializable]
    public class Segment
    {
        public Transform transform;
        public float     length;

        public Vector3 position
        {
            get => transform.position;
            set => transform.position = value;
        }

        public Segment(Transform t, float l) { transform = t; length = l; }

        public void LookAt(Vector3 p)
        {
            Vector2 dir   = (Vector2)(p - transform.position);
            float   angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
