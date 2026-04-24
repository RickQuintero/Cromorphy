using UnityEngine;

[ExecuteInEditMode]
public class SkyPlaneController : MonoBehaviour
{
    public Material SkyMaterial;

    // Sky
    [SerializeField] private Color _topColor    = Color.gray;
    [SerializeField] private Color _middleColor = Color.gray;
    [SerializeField] private Color _bottomColor = Color.gray;
    [SerializeField, Range(0.01f, 5f)] private float _topExponent    = 1f;
    [SerializeField, Range(0.01f, 5f)] private float _bottomExponent = 1f;

    // Stars
    [SerializeField] private bool    _starsEnabled       = true;
    [SerializeField] private Cubemap _starsCubemap;
    [SerializeField] private Color   _starsTint          = Color.gray;
    [SerializeField, Range(0f, 10f)] private float _starsExtinction     = 2f;
    [SerializeField, Range(0f, 25f)] private float _starsTwinklingSpeed = 10f;

    // Sun
    [SerializeField] private bool      _sunEnabled = true;
    [SerializeField] private Texture2D _sunTexture;
    [SerializeField] private Light     _sunLight;
    [SerializeField] private Color     _sunTint    = Color.gray;
    [SerializeField, Range(0.1f, 3f)]  private float _sunSize = 1f;
    [SerializeField, Range(0f, 2f)]    private float _sunHalo = 1f;

    // Moon
    [SerializeField] private bool      _moonEnabled = true;
    [SerializeField] private Texture2D _moonTexture;
    [SerializeField] private Light     _moonLight;
    [SerializeField] private Color     _moonTint    = Color.gray;
    [SerializeField, Range(0.1f, 3f)]  private float _moonSize = 1f;
    [SerializeField, Range(0f, 2f)]    private float _moonHalo = 1f;

    // Clouds
    [SerializeField] private bool    _cloudsEnabled  = true;
    [SerializeField] private Cubemap _cloudsCubemap;
    [SerializeField] private Color   _cloudsTint     = Color.gray;
    [SerializeField, Range(-0.75f, 0.75f)] private float _cloudsHeight   = 0f;
    [SerializeField, Range(0f, 360f)]      private float _cloudsRotation = 0f;

    // General
    [SerializeField, Range(0f, 8f)] private float _exposure = 1f;

    // Shader property IDs
    static readonly int TOP_COLOR            = Shader.PropertyToID("_TopColor");
    static readonly int MIDDLE_COLOR         = Shader.PropertyToID("_MiddleColor");
    static readonly int BOTTOM_COLOR         = Shader.PropertyToID("_BottomColor");
    static readonly int TOP_EXPONENT         = Shader.PropertyToID("_TopExponent");
    static readonly int BOTTOM_EXPONENT      = Shader.PropertyToID("_BottomExponent");
    static readonly int STARS_TINT           = Shader.PropertyToID("_StarsTint");
    static readonly int STARS_TEX            = Shader.PropertyToID("_StarsTex");
    static readonly int STARS_EXTINCTION     = Shader.PropertyToID("_StarsExtinction");
    static readonly int STARS_TWINKLING      = Shader.PropertyToID("_StarsTwinklingSpeed");
    static readonly int SUN_TEX              = Shader.PropertyToID("_SunTex");
    static readonly int SUN_TINT             = Shader.PropertyToID("_SunTint");
    static readonly int SUN_SIZE             = Shader.PropertyToID("_SunSize");
    static readonly int SUN_HALO             = Shader.PropertyToID("_SunHalo");
    static readonly int SUN_MATRIX           = Shader.PropertyToID("sunMatrix");
    static readonly int MOON_TEX             = Shader.PropertyToID("_MoonTex");
    static readonly int MOON_TINT            = Shader.PropertyToID("_MoonTint");
    static readonly int MOON_SIZE            = Shader.PropertyToID("_MoonSize");
    static readonly int MOON_HALO            = Shader.PropertyToID("_MoonHalo");
    static readonly int MOON_MATRIX          = Shader.PropertyToID("moonMatrix");
    static readonly int CLOUDS_TEX           = Shader.PropertyToID("_CloudsTex");
    static readonly int CLOUDS_TINT          = Shader.PropertyToID("_CloudsTint");
    static readonly int CLOUDS_ROTATION      = Shader.PropertyToID("_CloudsRotation");
    static readonly int CLOUDS_HEIGHT        = Shader.PropertyToID("_CloudsHeight");
    static readonly int EXPOSURE             = Shader.PropertyToID("_Exposure");

    void Awake()      => UpdateMaterial();
    void OnValidate() => UpdateMaterial();

    void Update()
    {
        if (!SkyMaterial) return;

        if (_sunEnabled && _sunLight)
            SkyMaterial.SetMatrix(SUN_MATRIX, _sunLight.transform.worldToLocalMatrix);

        if (_moonEnabled && _moonLight)
            SkyMaterial.SetMatrix(MOON_MATRIX, _moonLight.transform.worldToLocalMatrix);
    }

    void UpdateMaterial()
    {
        if (!SkyMaterial) return;

        SkyMaterial.SetColor(TOP_COLOR,       _topColor);
        SkyMaterial.SetColor(MIDDLE_COLOR,    _middleColor);
        SkyMaterial.SetColor(BOTTOM_COLOR,    _bottomColor);
        SkyMaterial.SetFloat(TOP_EXPONENT,    _topExponent);
        SkyMaterial.SetFloat(BOTTOM_EXPONENT, _bottomExponent);

        if (_starsEnabled)
        {
            SkyMaterial.DisableKeyword("STARS_OFF");
            SkyMaterial.SetTexture(STARS_TEX,       _starsCubemap);
            SkyMaterial.SetColor(STARS_TINT,         _starsTint);
            SkyMaterial.SetFloat(STARS_EXTINCTION,   _starsExtinction);
            SkyMaterial.SetFloat(STARS_TWINKLING,    _starsTwinklingSpeed);
        }
        else
        {
            SkyMaterial.EnableKeyword("STARS_OFF");
        }

        if (_sunEnabled)
        {
            SkyMaterial.DisableKeyword("SUN_OFF");
            SkyMaterial.SetTexture(SUN_TEX,  _sunTexture);
            SkyMaterial.SetFloat(SUN_SIZE,   _sunSize);
            SkyMaterial.SetFloat(SUN_HALO,   _sunHalo);
            SkyMaterial.SetColor(SUN_TINT,   _sunTint);
            if (_sunLight) _sunLight.gameObject.SetActive(true);
        }
        else
        {
            SkyMaterial.EnableKeyword("SUN_OFF");
            if (_sunLight) _sunLight.gameObject.SetActive(false);
        }

        if (_moonEnabled)
        {
            SkyMaterial.DisableKeyword("MOON_OFF");
            SkyMaterial.SetTexture(MOON_TEX, _moonTexture);
            SkyMaterial.SetFloat(MOON_SIZE,  _moonSize);
            SkyMaterial.SetFloat(MOON_HALO,  _moonHalo);
            SkyMaterial.SetColor(MOON_TINT,  _moonTint);
            if (_moonLight) _moonLight.gameObject.SetActive(true);
        }
        else
        {
            SkyMaterial.EnableKeyword("MOON_OFF");
            if (_moonLight) _moonLight.gameObject.SetActive(false);
        }

        if (_cloudsEnabled)
        {
            SkyMaterial.DisableKeyword("CLOUDS_OFF");
            SkyMaterial.SetTexture(CLOUDS_TEX,      _cloudsCubemap);
            SkyMaterial.SetColor(CLOUDS_TINT,        _cloudsTint);
            SkyMaterial.SetFloat(CLOUDS_ROTATION,    _cloudsRotation);
            SkyMaterial.SetFloat(CLOUDS_HEIGHT,      _cloudsHeight);
        }
        else
        {
            SkyMaterial.EnableKeyword("CLOUDS_OFF");
        }

        SkyMaterial.SetFloat(EXPOSURE, _exposure);
    }
}
