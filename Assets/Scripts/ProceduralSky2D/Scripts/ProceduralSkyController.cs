using System;
using UnityEngine;

[Serializable]
public class SkyCloud
{
    public float x;
    [Range(0.4f,  0.90f)] public float y     = 0.65f;
    [Range(0.05f, 0.35f)] public float scale  = 0.15f;
    [Range(0.005f, 0.08f)] public float speed = 0.02f;
    [HideInInspector] public int direction = 1;

    public Vector4 ToVector4() => new Vector4(x, y, scale, 1f);
}

[ExecuteInEditMode]
public class ProceduralSkyController : MonoBehaviour
{
    public Material SkyMaterial;

    [Header("State")]
    public bool isDay     = true;
    public bool isNight   = false;
    public bool isSunrise = false;

    [Header("Auto Cycle")]
    public bool  autoCycle          = false;
    public float dayDuration        = 120f;
    public float nightDuration      = 60f;
    public float transitionDuration = 15f;

    [Header("Clouds")]
    public SkyCloud[] clouds = new SkyCloud[6];
    [Tooltip("Smaller clouds move faster, simulating depth")]
    public bool cloudParallax = true;

    [Header("Sun & Moon Position")]
    [Range(0, 1)] public float sunX  = 0.50f;
    [Range(0, 1)] public float sunY  = 0.55f;
    [Range(0, 1)] public float moonX = 0.65f;
    [Range(0, 1)] public float moonY = 0.65f;

    // -------------------------------------------------------------------------
    // Internal state machine
    // -------------------------------------------------------------------------

    enum SkyState { Day, SunsetTransition, Night, SunriseTransition }

    SkyState _state      = SkyState.Day;
    float    _stateTimer = 0f;

    float _dayBlend    = 1f, _targetDay    = 1f;
    float _sunsetBlend = 0f, _targetSunset = 0f;
    float _nightBlend  = 0f, _targetNight  = 0f;

    // -------------------------------------------------------------------------
    // Shader property IDs
    // -------------------------------------------------------------------------

    static readonly int ID_DayBlend    = Shader.PropertyToID("_DayBlend");
    static readonly int ID_SunsetBlend = Shader.PropertyToID("_SunsetBlend");
    static readonly int ID_NightBlend  = Shader.PropertyToID("_NightBlend");
    static readonly int ID_SunVis      = Shader.PropertyToID("_SunVisibility");
    static readonly int ID_MoonVis     = Shader.PropertyToID("_MoonVisibility");
    static readonly int ID_SunPos      = Shader.PropertyToID("_SunPosition");
    static readonly int ID_MoonPos     = Shader.PropertyToID("_MoonPosition");
    static readonly int ID_Aspect      = Shader.PropertyToID("_AspectRatio");

    static readonly int[] ID_Clouds =
    {
        Shader.PropertyToID("_Cloud0"), Shader.PropertyToID("_Cloud1"),
        Shader.PropertyToID("_Cloud2"), Shader.PropertyToID("_Cloud3"),
        Shader.PropertyToID("_Cloud4"), Shader.PropertyToID("_Cloud5"),
    };

    // -------------------------------------------------------------------------
    // Unity messages
    // -------------------------------------------------------------------------

    void OnEnable()
    {
        InitClouds();
        SnapBlends();
        Push();
    }

    void OnValidate()
    {
        SnapBlends();
        Push();
    }

    void Update()
    {
        if (!SkyMaterial) return;

        float dt = Application.isPlaying ? Time.deltaTime : 0f;

        if (autoCycle && Application.isPlaying)
            TickCycle(dt);
        else
            ReadBools();

        LerpBlends(dt);

        if (Application.isPlaying)
            MoveClouds(dt);

        Push();
    }

    // -------------------------------------------------------------------------
    // State machine
    // -------------------------------------------------------------------------

    void TickCycle(float dt)
    {
        _stateTimer += dt;

        switch (_state)
        {
            case SkyState.Day:
                isDay = true; isNight = false; isSunrise = false;
                _targetDay = 1; _targetSunset = 0; _targetNight = 0;
                if (_stateTimer >= dayDuration)
                    Advance(SkyState.SunsetTransition);
                break;

            case SkyState.SunsetTransition:
                isDay = false; isNight = false; isSunrise = false;
                float sp = Mathf.Clamp01(_stateTimer / transitionDuration);
                _targetDay    = 1f - sp;
                _targetSunset = Mathf.Sin(sp * Mathf.PI); // peaks at mid-transition
                _targetNight  = sp;
                Normalize(ref _targetDay, ref _targetSunset, ref _targetNight);
                if (_stateTimer >= transitionDuration)
                    Advance(SkyState.Night);
                break;

            case SkyState.Night:
                isDay = false; isNight = true; isSunrise = false;
                _targetDay = 0; _targetSunset = 0; _targetNight = 1;
                if (_stateTimer >= nightDuration)
                    Advance(SkyState.SunriseTransition);
                break;

            case SkyState.SunriseTransition:
                isDay = false; isNight = false; isSunrise = true;
                float rp = Mathf.Clamp01(_stateTimer / transitionDuration);
                _targetNight  = 1f - rp;
                _targetSunset = Mathf.Sin(rp * Mathf.PI);
                _targetDay    = rp;
                Normalize(ref _targetDay, ref _targetSunset, ref _targetNight);
                if (_stateTimer >= transitionDuration)
                    Advance(SkyState.Day);
                break;
        }
    }

    void Advance(SkyState next) { _state = next; _stateTimer = 0f; }

    // Reads the inspector booleans and sets blend targets (manual control)
    void ReadBools()
    {
        if      (isDay)     { _targetDay = 1; _targetSunset = 0; _targetNight = 0; }
        else if (isNight)   { _targetDay = 0; _targetSunset = 0; _targetNight = 1; }
        else if (isSunrise) { _targetDay = 0; _targetSunset = 1; _targetNight = 0; }
    }

    // -------------------------------------------------------------------------
    // Blend interpolation
    // -------------------------------------------------------------------------

    void LerpBlends(float dt)
    {
        float step = (Application.isPlaying && transitionDuration > 0f)
            ? dt / transitionDuration
            : 1f;

        _dayBlend    = Mathf.MoveTowards(_dayBlend,    _targetDay,    step);
        _sunsetBlend = Mathf.MoveTowards(_sunsetBlend, _targetSunset, step);
        _nightBlend  = Mathf.MoveTowards(_nightBlend,  _targetNight,  step);
    }

    // Snaps blends to current target with no interpolation (editor / OnEnable)
    void SnapBlends()
    {
        ReadBools();
        _dayBlend    = _targetDay;
        _sunsetBlend = _targetSunset;
        _nightBlend  = _targetNight;
    }

    // -------------------------------------------------------------------------
    // Cloud movement
    // -------------------------------------------------------------------------

    void InitClouds()
    {
        System.Random rng = new System.Random(42); // fixed seed → consistent layout

        for (int i = 0; i < clouds.Length; i++)
        {
            if (clouds[i] == null) clouds[i] = new SkyCloud();

            SkyCloud c = clouds[i];
            c.x         = (float)rng.NextDouble();
            c.y         = Mathf.Lerp(0.50f, 0.82f, (float)rng.NextDouble());
            c.scale     = Mathf.Lerp(0.07f, 0.25f, (float)rng.NextDouble());
            c.speed     = Mathf.Lerp(0.005f, 0.04f, (float)rng.NextDouble());
            c.direction = rng.NextDouble() > 0.5 ? 1 : -1;
        }
    }

    void MoveClouds(float dt)
    {
        for (int i = 0; i < clouds.Length; i++)
        {
            SkyCloud c = clouds[i];
            if (c == null) continue;

            // Parallax: smaller (closer) clouds move faster
            float spd = cloudParallax
                ? c.speed * (0.15f / Mathf.Max(c.scale, 0.05f))
                : c.speed;

            c.x += c.direction * spd * dt;

            // Wrap off-screen (with padding so the cloud fully exits)
            if (c.x >  1.3f) c.x = -0.3f;
            if (c.x < -0.3f) c.x =  1.3f;
        }
    }

    // -------------------------------------------------------------------------
    // Push data to material
    // -------------------------------------------------------------------------

    void Push()
    {
        if (!SkyMaterial) return;

        SkyMaterial.SetFloat(ID_DayBlend,    _dayBlend);
        SkyMaterial.SetFloat(ID_SunsetBlend, _sunsetBlend);
        SkyMaterial.SetFloat(ID_NightBlend,  _nightBlend);

        // Sun is bright during day + fades at sunset; moon mirrors that
        SkyMaterial.SetFloat(ID_SunVis,  Mathf.Clamp01(_dayBlend + _sunsetBlend * 0.7f));
        SkyMaterial.SetFloat(ID_MoonVis, Mathf.Clamp01(_nightBlend + _sunsetBlend * 0.3f));

        SkyMaterial.SetVector(ID_SunPos,  new Vector4(sunX,  sunY,  0, 0));
        SkyMaterial.SetVector(ID_MoonPos, new Vector4(moonX, moonY, 0, 0));

        if (Camera.main)
            SkyMaterial.SetFloat(ID_Aspect, Camera.main.aspect);

        int count = Math.Min(clouds.Length, ID_Clouds.Length);
        for (int i = 0; i < count; i++)
            if (clouds[i] != null)
                SkyMaterial.SetVector(ID_Clouds[i], clouds[i].ToVector4());
    }

    // -------------------------------------------------------------------------
    // Public API — call from timeline, game events, etc.
    // -------------------------------------------------------------------------

    public void SetDay()
    {
        isDay = true; isNight = false; isSunrise = false;
        if (autoCycle) Advance(SkyState.Day);
    }

    public void SetNight()
    {
        isDay = false; isNight = true; isSunrise = false;
        if (autoCycle) Advance(SkyState.Night);
    }

    public void SetSunrise()
    {
        isDay = false; isNight = false; isSunrise = true;
        if (autoCycle) Advance(SkyState.SunriseTransition);
    }

    // -------------------------------------------------------------------------
    // Utility
    // -------------------------------------------------------------------------

    static void Normalize(ref float a, ref float b, ref float c)
    {
        float total = a + b + c;
        if (total > 0f) { a /= total; b /= total; c /= total; }
    }
}
