using UnityEngine;

/// <summary>
/// Spawns entities from the pool at a regular interval.
/// Supports an allowed-index filter so each generator can be restricted
/// to specific entity types (enemies, NPCs, etc.).
///
/// Spawn modes:
///   useTorusSpawn = false  →  original spawnPoints behaviour (unchanged)
///   useTorusSpawn = true   →  random position inside a 3D torus volume
/// </summary>
public class EntityGenerator : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Transform[] spawnPoints;
    public float spawnRate = 2f;

    [Tooltip("Exact number of entities this generator will spawn in total, then it stops.")]
    public int maxEntitiesToSpawn = 10;

    [Tooltip("When true the generator begins spawning as soon as the scene starts.")]
    public bool startSpawning = true;

    [Header("Entity Selection")]
    [Tooltip("Indices matching EntityPoolManager.entities — only these types will spawn here.")]
    public int[] allowedEntityIndices;

    // -------------------------------------------------------------------------
    // Torus spawn — new fields
    // -------------------------------------------------------------------------

    [Header("Torus Spawn")]
    [Tooltip("Switch to torus-based spawning. spawnPoints are ignored while this is true.")]
    public bool useTorusSpawn = false;

    [Tooltip("Distance from the torus centre to the centre of the tube.")]
    public float majorRadius = 10f;

    [Tooltip("Radius of the tube itself (controls how thick the donut is).")]
    public float minorRadius = 3f;

    // -------------------------------------------------------------------------
    // Runtime
    // -------------------------------------------------------------------------

    private float _timer;
    private bool  _spawning;
    private int   _totalSpawned;

    /// <summary>How many entities this generator has spawned so far.</summary>
    public int TotalSpawned => _totalSpawned;

    /// <summary>True once the generator has reached its maxEntitiesToSpawn limit.</summary>
    public bool IsDone => _totalSpawned >= maxEntitiesToSpawn;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {
        if (startSpawning)
            StartSpawning();
    }

    private void Update()
    {
        if (!_spawning) return;
        if (EntityPoolManager.Instance == null) return;

        _timer += Time.deltaTime;
        if (_timer >= spawnRate)
        {
            _timer = 0f;
            SpawnOne();
        }
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    public void StartSpawning()
    {
        if (IsDone) return;
        _spawning = true;
        _timer    = 0f;
    }

    public void StopSpawning() => _spawning = false;

    /// <summary>Resets the counter and starts over.</summary>
    public void Reset()
    {
        _totalSpawned = 0;
        StartSpawning();
    }

    // -------------------------------------------------------------------------
    // Core logic — spawns exactly one entity per interval
    // -------------------------------------------------------------------------

    private void SpawnOne()
    {
        if (IsDone)
        {
            _spawning = false;
            return;
        }

        if (allowedEntityIndices == null || allowedEntityIndices.Length == 0)
        {
            Debug.LogWarning($"[EntityGenerator] '{name}' has no allowed entity indices set.");
            return;
        }

        int entityIndex = PickAvailableType();
        if (entityIndex == -1) return;

        // ── Choose spawn position based on active mode ────────────────────
        Vector3 spawnPosition;

        if (useTorusSpawn)
        {
            spawnPosition = GetRandomPointInTorus();
        }
        else
        {
            if (spawnPoints == null || spawnPoints.Length == 0) return;
            spawnPosition = spawnPoints[Random.Range(0, spawnPoints.Length)].position;
        }

        GameObject spawned = EntityPoolManager.Instance.Spawn(
            entityIndex, spawnPosition, Quaternion.identity);

        if (spawned != null)
            _totalSpawned++;

        if (IsDone)
        {
            _spawning = false;
            //Debug.Log($"[EntityGenerator] '{name}' finished — spawned {_totalSpawned} entities.");
        }
    }

    // -------------------------------------------------------------------------
    // Torus math
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns a uniformly distributed random point inside the torus volume,
    /// centred on this transform and respecting its rotation.
    ///
    /// How it works:
    ///   θ  — random angle around the torus hole (0 … 2π)
    ///   φ  — random angle around the tube cross-section (0 … 2π)
    ///   ρ  — random radius inside the tube; sqrt gives uniform area distribution
    ///        so the interior is filled evenly, not biased toward the centre
    ///
    ///   Local position:
    ///     x = (majorRadius + ρ·cos φ) · cos θ
    ///     y =  ρ · sin φ                          ← vertical inside the tube
    ///     z = (majorRadius + ρ·cos φ) · sin θ
    ///
    ///   The point is then rotated by the transform so the torus
    ///   honours the GameObject's orientation in world space.
    /// </summary>
    private Vector3 GetRandomPointInTorus()
    {
        float theta = Random.Range(0f, Mathf.PI * 2f);   // around the hole
        float phi   = Random.Range(0f, Mathf.PI * 2f);   // around the tube

        // sqrt keeps the distribution uniform over the tube's circular area
        float rho = Mathf.Sqrt(Random.value) * minorRadius;

        float tubeX = majorRadius + rho * Mathf.Cos(phi);
        float tubeY = rho * Mathf.Sin(phi);

        Vector3 localPoint = new Vector3(
            tubeX * Mathf.Cos(theta),   // x
            tubeY,                       // y
            tubeX * Mathf.Sin(theta)    // z
        );

        // Apply rotation and position so the torus lives in world space
        return transform.position + transform.rotation * localPoint;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private int PickAvailableType()
    {
        int[] shuffled = (int[])allowedEntityIndices.Clone();
        for (int i = 0; i < shuffled.Length; i++)
        {
            int r = Random.Range(i, shuffled.Length);
            (shuffled[i], shuffled[r]) = (shuffled[r], shuffled[i]);
        }

        foreach (int index in shuffled)
            if (EntityPoolManager.Instance.AvailableOfType(index) > 0)
                return index;

        return -1;
    }

    // -------------------------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        if (useTorusSpawn)
            DrawTorusGizmo();
        else
            DrawSpawnPointGizmos();
    }

    // Original gizmo — unchanged
    private void DrawSpawnPointGizmos()
    {
        Gizmos.color = Color.cyan;
        if (spawnPoints == null) return;
        foreach (Transform spawn in spawnPoints)
            if (spawn != null)
                Gizmos.DrawSphere(spawn.position, 0.5f);
    }

    /// <summary>
    /// Draws three visual cues for the torus:
    ///   1. Outer equator ring  (major + minor radius)
    ///   2. Inner equator ring  (major - minor radius)
    ///   3. Eight tube cross-sections evenly spaced around the hole
    ///
    /// All geometry is rotated with the transform so the gizmo
    /// always matches what GetRandomPointInTorus() will produce.
    /// </summary>
    private void DrawTorusGizmo()
    {
        const int   segments     = 64;   // smoothness of each drawn ring
        const int   tubeSections = 8;    // how many cross-section circles to show
        const float alpha        = 0.85f;

        Matrix4x4 matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        // ── 1. Outer equator ─────────────────────────────────────────────
        Gizmos.color = new Color(0f, 1f, 0.8f, alpha);
        DrawGizmoCircle(matrix, Vector3.zero, majorRadius + minorRadius, Vector3.up, segments);

        // ── 2. Inner equator ─────────────────────────────────────────────
        Gizmos.color = new Color(0f, 0.6f, 1f, alpha);
        DrawGizmoCircle(matrix, Vector3.zero, majorRadius - minorRadius, Vector3.up, segments);

        // ── 3. Tube cross-sections ────────────────────────────────────────
        Gizmos.color = new Color(1f, 0.9f, 0f, alpha);
        for (int i = 0; i < tubeSections; i++)
        {
            float theta = (Mathf.PI * 2f * i) / tubeSections;

            // Centre of this cross-section circle in local space
            Vector3 localCenter = new Vector3(
                majorRadius * Mathf.Cos(theta),
                0f,
                majorRadius * Mathf.Sin(theta)
            );

            // The outward radial direction — the cross-section circle faces this way
            Vector3 radial = new Vector3(Mathf.Cos(theta), 0f, Mathf.Sin(theta));

            DrawGizmoCircle(matrix, localCenter, minorRadius, radial, segments / 2);
        }

        // ── 4. Centre marker ──────────────────────────────────────────────
        Gizmos.color = new Color(1f, 0.4f, 0f, 1f);
        Gizmos.DrawSphere(transform.position, 0.15f);
    }

    /// <summary>
    /// Draws a wire circle defined in local matrix space.
    /// </summary>
    /// <param name="matrix">Local-to-world transform (position + rotation).</param>
    /// <param name="localCenter">Circle centre in local space.</param>
    /// <param name="radius">Circle radius.</param>
    /// <param name="normal">The axis the circle faces in local space.</param>
    /// <param name="segments">Number of line segments (higher = smoother).</param>
    private static void DrawGizmoCircle(
        Matrix4x4 matrix, Vector3 localCenter, float radius, Vector3 normal, int segments)
    {
        // Build two axes that are perpendicular to the given normal
        Vector3 right   = Vector3.Cross(normal, normal == Vector3.up ? Vector3.forward : Vector3.up).normalized;
        Vector3 forward = Vector3.Cross(right, normal).normalized;

        Vector3 prev = matrix.MultiplyPoint3x4(localCenter + right * radius);

        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            Vector3 next = matrix.MultiplyPoint3x4(
                localCenter + (right * Mathf.Cos(angle) + forward * Mathf.Sin(angle)) * radius);

            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}