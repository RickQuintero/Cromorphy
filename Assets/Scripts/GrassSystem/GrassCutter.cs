using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  GRASS CUTTER
//  Bridges the ComboSystem/attack system to GrassComputeScript.CutGrass().
//
//  Attach to the Player GameObject.
//  GrassComputeScript must be in the scene.
//
//  Call CutAtPosition(position, radius) from ComboSystem.OnHitFrame()
//  or from HitDetection.Perform() — once per swing, not per frame.
// ─────────────────────────────────────────────────────────────────────────────
public class GrassCutter : MonoBehaviour
{
    [Header("Cut Settings")]
    [Tooltip("Radius of the cut area. Should match ComboSystem._baseRadius.")]
    [SerializeField] private float _cutRadius = 1.5f;

    private GrassComputeScript _grassSystem;

    private void Start()
    {
        _grassSystem = FindAnyObjectByType<GrassComputeScript>();
        if (_grassSystem == null)
            Debug.LogWarning("[GrassCutter] No GrassComputeScript found in scene.");
    }

    // ── Called by ComboSystem (via HitDetection or directly) ─────────────────

    /// <summary>
    /// Cut grass at a world position with the configured radius.
    /// Call this ONCE per attack swing — not per frame.
    /// </summary>
    public void CutAtPosition(Vector3 worldPosition)
    {
        _grassSystem?.CutGrass(worldPosition, _cutRadius);
        SpawnCutParticle(worldPosition);
    }

    /// <summary>
    /// Cut grass with a custom radius (e.g. finisher has larger cut area).
    /// </summary>
    public void CutAtPosition(Vector3 worldPosition, float radius)
    {
        _grassSystem?.CutGrass(worldPosition, radius);
        SpawnCutParticle(worldPosition);
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void SpawnCutParticle(Vector3 position)
    {
        // Only spawn if the grass painter actually placed blades in this area.
        // Ignores cut/uncut state — just asks "was grass painted here?"
        // This reliably prevents particles on stone, roads, buildings, etc.
        if (_grassSystem == null || !_grassSystem.HasGrassAt(position, _cutRadius))
            return;

        ParticleManager.Instance?.PlayEffect_FX(
            "Grass_particle",
            position,
            Quaternion.identity);
    }
}
