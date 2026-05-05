using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  PARTICLE BASE  —  Per-effect configuration (shown in ParticleManager Inspector)
//
//  Same as before but adds pool settings (prewarm, maxAlive).
//  API-compatible: ParticleName and ParticleObject unchanged.
// ─────────────────────────────────────────────────────────────────────────────
[System.Serializable]
public class ParticleBase
{
    [Tooltip("Unique name used to request this effect: ParticleManager.Instance.PlayEffect_FX(\"Hit\", ...)")]
    public string     ParticleName;

    [Tooltip("Prefab with a ParticleSystem on the root.")]
    public GameObject ParticleObject;

    [Tooltip("How many instances to pre-create on startup.")]
    public int prewarm  = 3;

    [Tooltip("Max simultaneous instances of this effect. Extra requests are silently ignored.")]
    public int maxAlive = 10;

    // Runtime — not serialized
    [System.NonSerialized] public int activeCount = 0;
    public bool HasRoom => activeCount < maxAlive;
}
