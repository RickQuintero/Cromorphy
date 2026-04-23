using System.Collections.Generic;
using UnityEngine;

public class FireflySwarm : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FireflyMorseController morseController;
    [SerializeField] private FireflyPathGuide pathGuide;
    [SerializeField] private Transform player;

    [Header("Pool")]
    [Tooltip("Index of the FireflyBoid entry in EntityPoolManager.entities")]
    [SerializeField] private int poolIndex;
    [SerializeField] private int swarmSize  = 20;
    [SerializeField] private float spawnRadius = 3f;

    private readonly List<FireflyBoid> _activeBoids = new List<FireflyBoid>();
    private bool _wasDay;

    /// <summary>Average world position of all active boids. Used by FireflyPathGuide to detect waypoint progress.</summary>
    public Vector2 AveragePosition
    {
        get
        {
            if (_activeBoids.Count == 0) return transform.position;
            Vector2 sum = Vector2.zero;
            foreach (FireflyBoid b in _activeBoids)
                if (b != null) sum += (Vector2)b.transform.position;
            return sum / _activeBoids.Count;
        }
    }

    private void Awake()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    private void Start()
    {
        _wasDay = DayLightController.Instance != null && DayLightController.Instance.IsDay;
        if (!_wasDay) SpawnSwarm();
    }

    private void Update()
    {
        if (DayLightController.Instance == null) return;

        bool isDay = DayLightController.Instance.IsDay;

        if (isDay != _wasDay)
        {
            _wasDay = isDay;
            if (isDay) DespawnSwarm();
            else       SpawnSwarm();
        }

        if (!isDay && pathGuide != null)
        {
            Vector2 waypoint = pathGuide.CurrentWaypoint;
            foreach (FireflyBoid boid in _activeBoids)
                if (boid != null) boid.SetTarget(waypoint);
        }
    }

    private void SpawnSwarm()
    {
        if (EntityPoolManager.Instance == null) return;

        Vector3 origin = player != null ? player.position : transform.position;
        for (int i = 0; i < swarmSize; i++)
        {
            Vector3 pos = origin + (Vector3)(Random.insideUnitCircle * spawnRadius);
            GameObject obj = EntityPoolManager.Instance.Spawn(poolIndex, pos, Quaternion.identity);
            if (obj == null) break;

            FireflyBoid boid = obj.GetComponent<FireflyBoid>();
            if (boid != null) _activeBoids.Add(boid);
        }
    }

    private void DespawnSwarm()
    {
        foreach (FireflyBoid boid in _activeBoids)
            boid?.ReturnSelf();
        _activeBoids.Clear();
    }

    /// <summary>Plays a morse message through the FireflyGuide via FireflyMorseController.</summary>
    public void PlayMorseMessage(string message) => morseController?.PlayMorse(message);
}
