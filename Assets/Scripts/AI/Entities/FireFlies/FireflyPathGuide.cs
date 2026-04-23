using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FireflyPathGuide : MonoBehaviour
{
    public static FireflyPathGuide Instance { get; private set; }

    [Header("Navigation")]
    [SerializeField] private List<FireflyTargetTrigger> targets;
    [SerializeField] private float waypointReachRadius = 1.5f;

    [Header("References")]
    [SerializeField] private FireflySwarm swarm;

    private NavMeshAgent _agent;
    private readonly List<Vector2> _waypoints = new List<Vector2>();
    private int _currentWaypointIndex;
    private int _currentTargetIndex;
    private bool _wasDay;
    private bool _pathActive;
    private bool _advancing;

    /// <summary>The current world-space waypoint the swarm should steer toward.</summary>
    public Vector2 CurrentWaypoint =>
        _waypoints.Count > 0 ? _waypoints[_currentWaypointIndex] : (Vector2)transform.position;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _agent = GetComponentInChildren<NavMeshAgent>();
        if (_agent != null) _agent.enabled = false;
    }

    private void Start()
    {
        _wasDay = DayLightController.Instance != null && DayLightController.Instance.IsDay;
        if (!_wasDay) ActivatePath();
    }

    private void Update()
    {
        if (DayLightController.Instance == null) return;

        bool isDay = DayLightController.Instance.IsDay;

        if (isDay != _wasDay)
        {
            _wasDay = isDay;
            if (isDay) DeactivatePath();
            else       ActivatePath();
        }

        if (!_pathActive || _waypoints.Count == 0 || swarm == null) return;

        if (Vector2.Distance(swarm.AveragePosition, CurrentWaypoint) < waypointReachRadius)
            AdvanceWaypoint();
    }

    private void ActivatePath()
    {
        if (_agent == null || targets == null || targets.Count == 0) return;
        _currentTargetIndex  = 0;
        _currentWaypointIndex = 0;
        _pathActive = true;
        _agent.enabled = true;
        StartCoroutine(ComputePath());
    }

    private void DeactivatePath()
    {
        StopAllCoroutines();
        _pathActive = false;
        _advancing  = false;
        _waypoints.Clear();
        if (_agent != null) _agent.enabled = false;
    }

    private IEnumerator ComputePath()
    {
        if (_currentTargetIndex >= targets.Count) yield break;

        _agent.SetDestination(targets[_currentTargetIndex].transform.position);
        yield return null; // one frame for NavMesh to compute corners

        _waypoints.Clear();
        _currentWaypointIndex = 0;
        foreach (Vector3 corner in _agent.path.corners)
            _waypoints.Add(new Vector2(corner.x, corner.y));

        _advancing = false;
    }

    private void AdvanceWaypoint()
    {
        _currentWaypointIndex++;
        if (_currentWaypointIndex < _waypoints.Count) return;

        // All waypoints for this target consumed — call Trigger on the target and move on
        if (_currentTargetIndex < targets.Count && targets[_currentTargetIndex] != null)
            targets[_currentTargetIndex].Trigger(); // Trigger sets isActive=false and calls AdvanceTarget
        else
            AdvanceTarget(); // Fallback if no trigger assigned
    }

    /// <summary>Advances to the next target and recomputes the path. Called by FireflyTargetTrigger.</summary>
    public void AdvanceTarget()
    {
        if (_advancing) return;
        _advancing = true;

        _currentTargetIndex++;
        _waypoints.Clear();
        _currentWaypointIndex = 0;

        if (_currentTargetIndex < targets.Count)
            StartCoroutine(ComputePath());
        else
            _pathActive = false;
    }
}
