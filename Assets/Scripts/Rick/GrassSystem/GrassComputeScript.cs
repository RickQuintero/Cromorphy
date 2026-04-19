// Grass system — CPU mesh approach, identical pattern to Cainos PixelWater.
// MeshFilter + MeshRenderer render a plain Mesh whose vertices are updated
// each frame in C#. No StructuredBuffers, no compute shader, no render feature,
// no D3D12 SRV binding issues possible.
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class GrassComputeScript : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    public bool autoUpdate;
    public SO_GrassSettings currentPresets;

    [Header("Material")]
    [SerializeField] private Material _assignedMaterial;

    [Header("Cutting")]
    public float regrowTime = 8f;

    [Header("Player Interaction")]
    [Range(0.1f, 3f)] public float grassSampleRadius = 1.5f;
    [Range(0.1f, 1f)] public float grassSpeedMultiplier = 0.6f;
    [System.NonSerialized] public float currentGrassDensity = 0f;

    // ── Painted data (serialised so the editor tool can save it) ─────────────
    [SerializeField, HideInInspector]
    private List<GrassData> grassData = new List<GrassData>();

    public List<GrassData> SetGrassPaintedDataList
    {
        get => grassData;
        set => grassData = value;
    }

    public bool IsReady => _initialized && grassData.Count > 0;

    // ── Mesh ──────────────────────────────────────────────────────────────────
    private Mesh        _grassMesh;
    private MeshFilter  _mf;
    private MeshRenderer _mr;

    // Pre-allocated arrays — reused every frame, no GC
    private Vector3[] _verts;
    private Vector2[] _uvs;
    private Color[]   _colors;
    private int[]     _indices;

    // ── Cut state (pure CPU — no GPU buffers needed) ──────────────────────────
    private float[] _cutValues;   // 0 = full height, 1 = fully cut
    private float[] _growTimers;

    // ── Runtime state ─────────────────────────────────────────────────────────
    private bool      _initialized;
    private Camera    _cam;
    private Transform _playerTransform;
    private PlayerController2D _playerMovement;

    // ── Editor scene-view camera ──────────────────────────────────────────────
#if UNITY_EDITOR
    private SceneView _view;
    void OnDestroy() { SceneView.duringSceneGui -= OnScene; }
    void OnScene(SceneView sv) { _view = sv; }
    private void OnValidate() { if (!Application.isPlaying) RefreshCamera(); }
#endif

    // ═════════════════════════════════════════════════════════════════════════
    // Lifecycle
    // ═════════════════════════════════════════════════════════════════════════

    private void OnEnable()
    {
        if (_initialized) Teardown();
        Setup();
    }

    private void OnDisable() => Teardown();

    private void Setup()
    {
#if UNITY_EDITOR
        SceneView.duringSceneGui += OnScene;
#endif
        RefreshCamera();

        if (grassData.Count == 0) return;
        if (_assignedMaterial == null)
        {
            Debug.LogWarning("[GrassComputeScript] Assign a material (GrassSurface_URP) in the Inspector.", this);
            return;
        }

        _mf = GetComponent<MeshFilter>();
        _mr = GetComponent<MeshRenderer>();

        // Build mesh arrays
        int count = grassData.Count;
        _verts   = new Vector3[count * 3];
        _uvs     = new Vector2[count * 3];
        _colors  = new Color[count * 3];
        _indices = new int[count * 3];

        _cutValues  = new float[count];
        _growTimers = new float[count];

        // UVs and colors are static — set once
        for (int i = 0; i < count; i++)
        {
            int v0 = i * 3, v1 = v0 + 1, v2 = v0 + 2;

            // UV layout: base = y 0, tip = y 1 (drives top/bottom tint in shader)
            _uvs[v0] = new Vector2(0f,   0f);
            _uvs[v1] = new Vector2(1f,   0f);
            _uvs[v2] = new Vector2(0.5f, 1f);

            // Per-blade color baked from painting
            Color c = new Color(grassData[i].color.x, grassData[i].color.y, grassData[i].color.z, 1f);
            _colors[v0] = _colors[v1] = _colors[v2] = c;

            _indices[v0] = v0; _indices[v1] = v1; _indices[v2] = v2;
        }

        // Create mesh
        _grassMesh = new Mesh { name = "GrassMesh" };
        _grassMesh.indexFormat = count > 21845 // > 65535/3 verts
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;

        _grassMesh.vertices = _verts;
        _grassMesh.uv       = _uvs;
        _grassMesh.colors   = _colors;
        _grassMesh.triangles = _indices;

        _mf.sharedMesh      = _grassMesh;
        _mr.sharedMaterial  = _assignedMaterial;
        _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _mr.receiveShadows  = false;

        _initialized = true;

        if (Application.isPlaying) FindPlayer();

        // Force one vertex update so the mesh isn't invisible on first frame
        UpdateMeshVertices();
    }

    private void Teardown()
    {
#if UNITY_EDITOR
        SceneView.duringSceneGui -= OnScene;
#endif
        if (_grassMesh != null)
        {
            if (Application.isPlaying) Destroy(_grassMesh);
            else                       DestroyImmediate(_grassMesh);
            _grassMesh = null;
        }

        if (_mf != null) _mf.sharedMesh = null;

        _verts = null;
        _uvs = null;
        _colors = null;
        _indices = null;
        _cutValues = _growTimers = null;
        _initialized = false;
    }

    private void RefreshCamera()
    {
#if UNITY_EDITOR
        _cam = Application.isPlaying ? Camera.main : _view?.camera;
#else
        _cam = Camera.main;
#endif
    }

    private void FindPlayer()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;
        _playerMovement  = p.GetComponent<PlayerController2D>();
        _playerTransform = p.transform;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Update
    // ═════════════════════════════════════════════════════════════════════════

    private void Update()
    {
        // Editor: rebuild on every frame when autoUpdate is on
        if (!Application.isPlaying && autoUpdate) { Teardown(); Setup(); return; }
        if (!_initialized) return;

        RefreshCamera();

        if (Application.isPlaying)
        {
            if (_playerTransform == null) FindPlayer();
            Regrow();
            SampleDensity();
        }

        UpdateMeshVertices();
    }

    // ── Regrowth (pure CPU, no GPU readback needed) ───────────────────────────
    private void Regrow()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < _cutValues.Length; i++)
        {
            if (_cutValues[i] <= 0f) continue;
            _growTimers[i] += dt;
            _cutValues[i]   = Mathf.Max(0f, 1f - _growTimers[i] / regrowTime);
            if (_cutValues[i] <= 0f) _growTimers[i] = 0f;
        }
    }

    // ── Vertex update — wind + interaction, runs every frame ─────────────────
    private void UpdateMeshVertices()
    {
        if (grassData.Count == 0 || _verts == null) return;

        float windSpeed    = currentPresets != null ? currentPresets.windSpeed    : 1f;
        float windStrength = currentPresets != null ? currentPresets.windStrength : 0.05f;
        float affectStr    = currentPresets != null ? currentPresets.affectStrength : 1f;
        float maxDist      = currentPresets != null ? currentPresets.maxDrawDistance : 60f;
        float maxDistSq    = maxDist * maxDist;

        Vector3 camPos = _cam != null ? _cam.transform.position : Vector3.zero;

        // Player position snapshot (avoid repeated property access in hot loop)
        bool    hasPlayer  = _playerTransform != null;
        float   px = hasPlayer ? _playerTransform.position.x : 0f;
        float   py = hasPlayer ? _playerTransform.position.y : 0f;
        float   interactR  = grassSampleRadius * 1.5f;
        float   now        = Time.time;

        for (int i = 0; i < grassData.Count; i++)
        {
            Vector3 pos = grassData[i].position;
            int v0 = i * 3, v1 = v0 + 1, v2 = v0 + 2;

            // Distance LOD: collapse blade to a point if beyond draw distance
            float dx = pos.x - camPos.x, dy = pos.y - camPos.y;
            if (dx * dx + dy * dy > maxDistSq)
            {
                _verts[v0] = _verts[v1] = _verts[v2] = pos;
                continue;
            }

            // Cut state
            float cutScale = _cutValues != null ? (1f - _cutValues[i]) : 1f;
            if (cutScale < 0.01f)
            {
                _verts[v0] = _verts[v1] = _verts[v2] = pos;
                continue;
            }

            float width  = grassData[i].length.x;
            float height = grassData[i].length.y * cutScale;

            // 2D blade orientation: up = normal, right = 90° CCW in XY
            Vector3 n     = grassData[i].normal;
            float   nLen  = Mathf.Sqrt(n.x * n.x + n.y * n.y);
            float   nx    = nLen > 0.001f ? n.x / nLen : 0f;
            float   ny    = nLen > 0.001f ? n.y / nLen : 1f;
            // bladeUp = (nx, ny, 0),  bladeRight = (ny, -nx, 0)

            // Wind: sinusoidal, phase varies per blade so they don't all move together
            float phase    = now * windSpeed + pos.x * 0.7f + pos.y * 0.3f;
            float windBend = Mathf.Sin(phase) * windStrength * height;

            // Interaction: push tip away from player and all ShaderInteractors
            float pushBend = 0f;
            if (hasPlayer)
            {
                float diffX = pos.x - px, diffY = pos.y - py;
                float distSq = diffX * diffX + diffY * diffY;
                if (distSq < interactR * interactR && distSq > 0.0001f)
                {
                    float dist      = Mathf.Sqrt(distSq);
                    float influence = (1f - dist / interactR) * affectStr * height;
                    pushBend += (diffX / dist) * influence;
                }
            }
            for (int s = 0; s < ShaderInteractor.all.Count; s++)
            {
                ShaderInteractor si = ShaderInteractor.all[s];
                if (si == null) continue;
                float ir    = si.radius * 1.5f;
                float diffX = pos.x - si.transform.position.x;
                float diffY = pos.y - si.transform.position.y;
                float distSq = diffX * diffX + diffY * diffY;
                if (distSq < ir * ir && distSq > 0.0001f)
                {
                    float dist      = Mathf.Sqrt(distSq);
                    float influence = (1f - dist / ir) * affectStr * height;
                    pushBend += (diffX / dist) * influence;
                }
            }

            float totalBendX = (windBend + pushBend) * ny;   // project bend onto right.x
            float totalBendY = (windBend + pushBend) * -nx;  // project bend onto right.y

            // Two base vertices (blade root, left and right)
            _verts[v0] = new Vector3(pos.x + ny * (-width * 0.5f),
                                     pos.y - nx * (-width * 0.5f), pos.z);
            _verts[v1] = new Vector3(pos.x + ny * ( width * 0.5f),
                                     pos.y - nx * ( width * 0.5f), pos.z);
            // Tip vertex: elevated along normal + lateral bend
            _verts[v2] = new Vector3(pos.x + nx * height + totalBendX,
                                     pos.y + ny * height + totalBendY, pos.z);
        }

        _grassMesh.vertices = _verts;
        _grassMesh.RecalculateBounds();
    }

    // ── Player density (used to slow player in thick grass) ───────────────────
    private void SampleDensity()
    {
        if (_playerTransform == null) return;

        float   radiusSq = grassSampleRadius * grassSampleRadius;
        float   px       = _playerTransform.position.x;
        float   py       = _playerTransform.position.y;
        int     count    = 0;
        float   density  = 0f;

        int limit = Mathf.Min(grassData.Count, 500);
        for (int i = 0; i < limit; i++)
        {
            float dx = grassData[i].position.x - px;
            float dy = grassData[i].position.y - py;
            if (dx * dx + dy * dy <= radiusSq)
            {
                density += _cutValues != null ? (1f - _cutValues[i]) : 1f;
                count++;
            }
        }

        currentGrassDensity = count > 0 ? density / count : 0f;
        if (_playerMovement != null)
            _playerMovement.SetGrassSpeedMultiplier(
                Mathf.Lerp(1f, grassSpeedMultiplier, currentGrassDensity));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Public API (used by GrassCutter and GrassPainterWindow)
    // ═════════════════════════════════════════════════════════════════════════

    public bool HasGrassAt(Vector3 position, float radius)
    {
        if (!_initialized) return false;
        float rSq = radius * radius;
        for (int i = 0; i < grassData.Count; i++)
        {
            float dx = grassData[i].position.x - position.x;
            float dy = grassData[i].position.y - position.y;
            if (dx * dx + dy * dy <= rSq) return true;
        }
        return false;
    }

    public void CutGrass(Vector3 position, float radius)
    {
        if (!_initialized || _cutValues == null) return;
        float rSq = radius * radius;
        for (int i = 0; i < grassData.Count; i++)
        {
            float dx = grassData[i].position.x - position.x;
            float dy = grassData[i].position.y - position.y;
            if (dx * dx + dy * dy <= rSq)
            {
                _cutValues[i]  = 1f;
                _growTimers[i] = 0f;
            }
        }
    }

    // Called by GrassPainterWindow after painting
    public void Reset()       { Teardown(); Setup(); }
    public void ResetFaster() { Teardown(); Setup(); }

    void OnDrawGizmos()
    {
        if (currentPresets == null || !currentPresets.drawBounds) return;
        if (_grassMesh == null) return;
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawWireCube(_grassMesh.bounds.center, _grassMesh.bounds.size);
    }
}

// ── Data structs (unchanged — painter tool depends on these) ─────────────────

[System.Serializable]
[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct GrassData
{
    public Vector3 position;
    public Vector3 normal;
    public Vector2 length;   // x=width, y=height
    public Vector3 color;
}

// Kept for API compatibility with GrassCutter and other systems
[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct GrassCutData
{
    public float cutValue;
    public float growTimer;
}
