using UnityEngine;

public class PupilSystem : MonoBehaviour
{
    // ── References ────────────────────────────────────────────────────────
    [Header("References")]
    public Rigidbody2D mainBody;
    public Transform   pupil;

    // ── Following ─────────────────────────────────────────────────────────
    [Header("Following")]
    public float followSpeedThreshold = 0.5f;
    public float pupilOffset          = 0.06f;
    public float followSpeed          = 8f;

    // ── Wandering ─────────────────────────────────────────────────────────
    [Header("Wandering")]
    public float idleDelay      = 5f;
    public float wanderSpeed    = 1.5f;
    public float wanderInterval = 1.2f;

    // ── Prey Detection ────────────────────────────────────────────────────
    [Header("Prey Detection")]
    public float       detectionRadius  = 4f;
    public LayerMask   preyLayerMask;
    public float       preyLostTimeout  = 2.5f;

    // ── State ─────────────────────────────────────────────────────────────
    private enum Mode { Following, Wandering }
    private enum WanderSub { Moving, Waiting, LookingAtPrey }

    private Mode      _mode      = Mode.Wandering;
    private WanderSub _wanderSub = WanderSub.Moving;

    private float   _idleTimer;
    private float   _wanderWaitTimer;
    private float   _preyLostTimer;
    private Vector2 _wanderTarget;

    private static readonly float _reachEpsilon = 0.004f;

    void Start()
    {
        _wanderTarget = Vector2.zero;
        if (pupil != null) pupil.localPosition = Vector2.zero;
    }

    void Update()
    {
        if (mainBody == null || pupil == null) return;

        float speed = mainBody.linearVelocity.magnitude;
        bool  moving = speed > followSpeedThreshold;

        switch (_mode)
        {
            case Mode.Following:
                TickFollowing(moving);
                break;
            case Mode.Wandering:
                TickWandering(moving);
                break;
        }
    }

    // ── Following ─────────────────────────────────────────────────────────

    void TickFollowing(bool moving)
    {
        if (!moving)
        {
            _idleTimer += Time.deltaTime;
            if (_idleTimer >= idleDelay)
                EnterWandering();
        }
        else
        {
            _idleTimer = 0f;
            Vector2 dir    = mainBody.linearVelocity.normalized;
            Vector2 target = dir * pupilOffset;
            pupil.localPosition = Vector2.Lerp(pupil.localPosition, target, followSpeed * Time.deltaTime);
        }
    }

    // ── Wandering ─────────────────────────────────────────────────────────

    void TickWandering(bool moving)
    {
        if (moving)
        {
            EnterFollowing();
            return;
        }

        switch (_wanderSub)
        {
            case WanderSub.Moving:
                MovePupilToWanderTarget();
                CheckForPrey();
                break;

            case WanderSub.Waiting:
                _wanderWaitTimer -= Time.deltaTime;
                if (_wanderWaitTimer <= 0f)
                {
                    PickWanderTarget();
                    _wanderSub = WanderSub.Moving;
                }
                CheckForPrey();
                break;

            case WanderSub.LookingAtPrey:
                TickLookingAtPrey();
                break;
        }
    }

    void MovePupilToWanderTarget()
    {
        pupil.localPosition = Vector2.Lerp(pupil.localPosition, _wanderTarget, wanderSpeed * Time.deltaTime);
        if (Vector2.Distance(pupil.localPosition, _wanderTarget) < _reachEpsilon)
        {
            _wanderWaitTimer = wanderInterval;
            _wanderSub       = WanderSub.Waiting;
        }
    }

    void CheckForPrey()
    {
        Collider2D hit = Physics2D.OverlapCircle(mainBody.transform.position, detectionRadius, preyLayerMask);
        if (hit != null)
        {
            _preyLostTimer = 0f;
            _wanderSub     = WanderSub.LookingAtPrey;
        }
    }

    void TickLookingAtPrey()
    {
        Collider2D hit = Physics2D.OverlapCircle(mainBody.transform.position, detectionRadius, preyLayerMask);

        if (hit != null)
        {
            _preyLostTimer = 0f;
            Vector2 dir    = ((Vector2)(hit.transform.position - transform.position)).normalized;
            Vector2 target = dir * pupilOffset;
            pupil.localPosition = Vector2.Lerp(pupil.localPosition, target, followSpeed * Time.deltaTime);
        }
        else
        {
            _preyLostTimer += Time.deltaTime;
            if (_preyLostTimer >= preyLostTimeout)
            {
                PickWanderTarget();
                _wanderSub = WanderSub.Moving;
            }
        }
    }

    // ── Transitions ───────────────────────────────────────────────────────

    void EnterFollowing()
    {
        _mode      = Mode.Following;
        _idleTimer = 0f;
    }

    void EnterWandering()
    {
        _mode      = Mode.Wandering;
        _wanderSub = WanderSub.Moving;
        _idleTimer = 0f;
        PickWanderTarget();
    }

    void PickWanderTarget()
    {
        _wanderTarget = new Vector2(
            Random.Range(-0.05f, 0.05f),
            Random.Range(-0.03f, 0.03f));
    }
}
