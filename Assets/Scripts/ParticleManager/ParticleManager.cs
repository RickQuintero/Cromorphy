using UnityEngine;
using System.Collections.Generic;

// ─────────────────────────────────────────────────────────────────────────────
//  PARTICLE MANAGER  —  Pooled refactor
//
//  Same external API as before:
//    ParticleManager.Instance.PlayEffect_FX("Hit", position, rotation);
//    ParticleManager.Instance.PlayEffect_FX("Hit", position, rotation, transform);
//
//  Internal change: Instantiate/Destroy replaced with object pool.
//  Mirrors EntityPoolManager pattern already used in this project.
//
//  SETUP (Inspector):
//    1. Add ParticleManager to [GameSystems] GameObject
//    2. Fill the particles[] array:
//         ParticleName  = "Hit"
//         ParticleObject = your particle prefab
//         prewarm       = 3     (pre-created instances)
//         maxAlive      = 10    (simultaneous cap)
//    3. Hit Play — pool is built in Awake
//
//  ARCHITECTURE (matches EntityPoolManager):
//    • One Queue<PooledParticle> per effect type
//    • One child container GameObject per effect type (keeps Hierarchy clean)
//    • Dynamic expansion: if pool is empty but maxAlive not reached → create more
//    • If maxAlive reached → request is silently dropped (no error spam)
// ─────────────────────────────────────────────────────────────────────────────
public class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance { get; private set; }

    [Header("Particle Effects")]
    public ParticleBase[] particles;

    // Per-effect: pool queue + container transform
    private Dictionary<string, Queue<PooledParticle>> _pools;
    private Dictionary<string, Transform>              _containers;
    private Dictionary<string, ParticleBase>           _configs;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildPools();
    }

    // ── Pool Construction ─────────────────────────────────────────────────────

    private void BuildPools()
    {
        _pools      = new Dictionary<string, Queue<PooledParticle>>();
        _containers = new Dictionary<string, Transform>();
        _configs    = new Dictionary<string, ParticleBase>();

        foreach (ParticleBase config in particles)
        {
            if (config.ParticleObject == null)
            {
                Debug.LogWarning($"[ParticleManager] '{config.ParticleName}' has no prefab assigned.");
                continue;
            }

            if (_pools.ContainsKey(config.ParticleName))
            {
                Debug.LogWarning($"[ParticleManager] Duplicate name '{config.ParticleName}'. Skipping.");
                continue;
            }

            // Container GameObject keeps Hierarchy clean (mirrors EntityPoolManager)
            var container = new GameObject(config.ParticleName + "_Pool");
            container.transform.SetParent(transform);

            _pools[config.ParticleName]      = new Queue<PooledParticle>();
            _containers[config.ParticleName] = container.transform;
            _configs[config.ParticleName]    = config;

            // Prewarm
            for (int i = 0; i < config.prewarm; i++)
                _pools[config.ParticleName].Enqueue(CreateInstance(config, container.transform));
        }
    }

    private PooledParticle CreateInstance(ParticleBase config, Transform container)
    {
        GameObject go = Instantiate(config.ParticleObject, container);
        go.SetActive(false);

        PooledParticle member = go.GetComponent<PooledParticle>()
                             ?? go.AddComponent<PooledParticle>();
        member.Initialise(config.ParticleName);
        return member;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Play a named particle effect at a world position.
    /// Returns the PooledParticle so callers can call ReturnToPool() early
    /// (e.g. looping effects). Returns null if the effect name doesn't exist
    /// or the maxAlive cap is reached.
    ///
    /// API-compatible replacement for the old PlayEffect_FX(name, pos, rot).
    /// </summary>
    public PooledParticle PlayEffect_FX(string effectName, Vector3 position, Quaternion rotation,
                                         Transform parent = null)
    {
        if (!_pools.TryGetValue(effectName, out var pool))
        {
            Debug.LogWarning($"[ParticleManager] Effect '{effectName}' not found.");
            return null;
        }

        ParticleBase config = _configs[effectName];

        // Hard cap — silently drop if reached (no error spam during heavy combat)
        if (!config.HasRoom) return null;

        // Dynamic expansion: grow pool if empty but cap not reached
        if (pool.Count == 0)
            pool.Enqueue(CreateInstance(config, _containers[effectName]));

        PooledParticle particle = pool.Dequeue();
        config.activeCount++;

        // Play handles positioning, parenting, and auto-return scheduling
        particle.Play(position, rotation, parent);

        return particle;
    }

    /// <summary>
    /// Called by PooledParticle.WaitAndReturn() when the ParticleSystem finishes,
    /// or by PooledParticle.ReturnToPool() for early manual returns.
    /// </summary>
    public void ReturnToPool(PooledParticle particle)
    {
        string name = particle.EffectName;

        if (!_pools.ContainsKey(name)) return;

        particle.gameObject.SetActive(false);
        particle.transform.SetParent(_containers[name], worldPositionStays: false);
        _pools[name].Enqueue(particle);

        _configs[name].activeCount = Mathf.Max(0, _configs[name].activeCount - 1);
    }

    // ── Debug helpers ─────────────────────────────────────────────────────────

    /// <summary>How many instances of an effect are currently active.</summary>
    public int ActiveCount(string effectName) =>
        _configs.TryGetValue(effectName, out var c) ? c.activeCount : 0;

    /// <summary>How many instances of an effect are sitting idle in the pool.</summary>
    public int PooledCount(string effectName) =>
        _pools.TryGetValue(effectName, out var q) ? q.Count : 0;
}
