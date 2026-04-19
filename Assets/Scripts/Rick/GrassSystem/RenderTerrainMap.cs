using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

// ─────────────────────────────────────────────────────────────────────────────
//  RENDER TERRAIN MAP — URP compatible
//
//  Original used Camera.Render() which forces URP to reinitialize the pipeline
//  and crashes the Blitter in Unity 6.
//
//  Fix: use the camera normally (let URP render it), or disable it entirely
//  if you don't need terrain blending (Blend with floor = OFF on material).
// ─────────────────────────────────────────────────────────────────────────────
[ExecuteInEditMode]
public class RenderTerrainMap : MonoBehaviour
{
    public Camera  camToDrawWith;
    [SerializeField] LayerMask layer;
    [SerializeField] Renderer[] renderers;
    [SerializeField] Terrain[]  terrains;

    public int   resolution    = 512;
    public float adjustScaling = 2.5f;
    public float repeatRate    = 5f;
    [SerializeField] bool RealTimeDiffuse;

    private RenderTexture _tempTex;
    private Bounds        _bounds;

    private void OnEnable()
    {
        _bounds = new Bounds(transform.position, Vector3.zero);
        _tempTex = new RenderTexture(resolution, resolution, 24);
        GetBounds();
        SetUpCam();
        // Don't call DrawDiffuseMap here — URP not ready yet during OnEnable
    }

    private void Start()
    {
        GetBounds();
        SetUpCam();
        StartCoroutine(DrawAfterFrame());

        if (RealTimeDiffuse)
            InvokeRepeating(nameof(UpdateTex), 1f, repeatRate);
    }

    // Wait one frame so URP pipeline is fully initialized before rendering
    private IEnumerator DrawAfterFrame()
    {
        yield return null;
        DrawDiffuseMap();
    }

    public void DrawDiffuseMap()
    {
        if (camToDrawWith == null || _tempTex == null) return;

        // Set up the camera to render into the RenderTexture
        camToDrawWith.targetTexture = _tempTex;
        camToDrawWith.enabled       = true;

        Shader.SetGlobalFloat("_OrthographicCamSizeTerrain",   camToDrawWith.orthographicSize);
        Shader.SetGlobalVector("_OrthographicCamPosTerrain",   camToDrawWith.transform.position);

        // URP-safe: let URP handle the render on the next frame
        // by keeping the camera enabled with a targetTexture assigned.
        // It will render on its own without Camera.Render().
        Shader.SetGlobalTexture("_TerrainDiffuse", _tempTex);

        // Disable after one frame so it doesn't keep rendering every frame
        if (!RealTimeDiffuse)
            StartCoroutine(DisableCamNextFrame());
    }

    private IEnumerator DisableCamNextFrame()
    {
        yield return null;
        if (camToDrawWith != null)
            camToDrawWith.enabled = false;
    }

    private void UpdateTex()
    {
        if (camToDrawWith != null)
        {
            camToDrawWith.targetTexture = _tempTex;
            camToDrawWith.enabled       = true;
            Shader.SetGlobalTexture("_TerrainDiffuse", _tempTex);
        }
    }

    private void GetBounds()
    {
        foreach (Renderer r in renderers)
        {
            if (_bounds.size.magnitude < 0.1f)
                _bounds = new Bounds(r.transform.position, Vector3.zero);
            _bounds.Encapsulate(r.bounds);
        }
        foreach (Terrain t in terrains)
        {
            if (_bounds.size.magnitude < 0.1f)
                _bounds = new Bounds(t.transform.position, Vector3.zero);
            Vector3 center = t.GetPosition() + t.terrainData.bounds.center;
            _bounds.Encapsulate(new Bounds(center, t.terrainData.bounds.size));
        }
    }

    private void SetUpCam()
    {
        if (camToDrawWith == null)
            camToDrawWith = GetComponentInChildren<Camera>();
        if (camToDrawWith == null) return;

        float size = _bounds.size.magnitude;
        camToDrawWith.cullingMask        = layer;
        camToDrawWith.orthographicSize   = size / adjustScaling;
        camToDrawWith.transform.parent   = null;
        camToDrawWith.transform.position = _bounds.center + new Vector3(0, _bounds.extents.y + 5f, 0);
        camToDrawWith.transform.parent   = transform;
        camToDrawWith.enabled            = false; // starts disabled
    }

    private void OnDisable()
    {
        if (_tempTex != null)
        {
            _tempTex.Release();
            _tempTex = null;
        }
    }
}
