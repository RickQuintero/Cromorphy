Shader "Custom/ProceduralSky_RickVersion"
{
    Properties
    {
        // ---- Sky Gradient ----
        _DayTopColor        ("Day Top",       Color) = (0.15, 0.45, 0.85, 1)
        _DayHorizonColor    ("Day Horizon",   Color) = (0.55, 0.75, 0.95, 1)
        _DayBottomColor     ("Day Bottom",    Color) = (0.70, 0.85, 1.00, 1)

        _SunsetTopColor     ("Sunset Top",    Color) = (0.05, 0.05, 0.25, 1)
        _SunsetHorizonColor ("Sunset Horizon",Color) = (0.85, 0.35, 0.05, 1)
        _SunsetBottomColor  ("Sunset Bottom", Color) = (0.95, 0.55, 0.15, 1)

        _NightTopColor      ("Night Top",     Color) = (0.01, 0.01, 0.06, 1)
        _NightHorizonColor  ("Night Horizon", Color) = (0.02, 0.03, 0.12, 1)
        _NightBottomColor   ("Night Bottom",  Color) = (0.04, 0.05, 0.15, 1)

        _TopExponent    ("Top Exponent",    Range(0.01, 5)) = 1.5
        _BottomExponent ("Bottom Exponent", Range(0.01, 5)) = 2.0

        // Blend factors — controller sets these, they should sum to 1
        _DayBlend    ("Day Blend",    Range(0, 1)) = 1
        _SunsetBlend ("Sunset Blend", Range(0, 1)) = 0
        _NightBlend  ("Night Blend",  Range(0, 1)) = 0

        // ---- Stars ----
        _StarDensity      ("Star Density",       Range(0.9, 1.0)) = 0.97
        _StarBrightness   ("Star Brightness",    Range(0, 3))     = 1.5
        _StarSize         ("Star Size",          Range(0.01, 0.3)) = 0.12
        _StarTwinkleSpeed ("Star Twinkle Speed", Range(0, 15))    = 4.0
        _StarGridScale    ("Star Grid Scale",    Float)           = 80.0
        _StarColor        ("Star Color",         Color)           = (1, 0.95, 0.9, 1)

        // ---- Sun ----
        _SunColor      ("Sun Color",      Color)         = (1, 0.95, 0.70, 1)
        _SunPosition   ("Sun Position",   Vector)        = (0.5, 0.55, 0, 0)
        _SunSize       ("Sun Size",       Range(0, 0.3)) = 0.07
        _SunGlow       ("Sun Glow",       Range(0, 1))   = 0.5
        _SunVisibility ("Sun Visibility", Range(0, 1))   = 1

        // ---- Moon ----
        _MoonColor       ("Moon Color",       Color)        = (0.85, 0.9, 1.0, 1)
        _MoonPosition    ("Moon Position",    Vector)       = (0.65, 0.65, 0, 0)
        _MoonSize        ("Moon Size",        Range(0, 0.2)) = 0.05
        _MoonVisibility  ("Moon Visibility",  Range(0, 1))  = 0

        // ---- Clouds ----
        [NoScaleOffset] _CloudTex ("Cloud Texture", 2D) = "white" {}

        _CloudDayColor    ("Cloud Day Color",    Color) = (1.00, 1.00, 1.00, 0.85)
        _CloudSunsetColor ("Cloud Sunset Color", Color) = (1.00, 0.60, 0.30, 0.80)
        _CloudNightColor  ("Cloud Night Color",  Color) = (0.15, 0.18, 0.30, 0.70)

        // Cloud params per slot: x=UV position, y=UV height, z=half-size, w=opacity
        _Cloud0 ("Cloud 0", Vector) = (0.10, 0.65, 0.18, 1)
        _Cloud1 ("Cloud 1", Vector) = (0.35, 0.70, 0.14, 1)
        _Cloud2 ("Cloud 2", Vector) = (0.60, 0.60, 0.22, 1)
        _Cloud3 ("Cloud 3", Vector) = (0.80, 0.68, 0.12, 1)
        _Cloud4 ("Cloud 4", Vector) = (0.20, 0.75, 0.16, 1)
        _Cloud5 ("Cloud 5", Vector) = (0.55, 0.72, 0.10, 1)

        _AspectRatio ("Aspect Ratio", Float) = 1.778
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Opaque" }
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Sky
            half3 _DayTopColor,    _DayHorizonColor,    _DayBottomColor;
            half3 _SunsetTopColor, _SunsetHorizonColor, _SunsetBottomColor;
            half3 _NightTopColor,  _NightHorizonColor,  _NightBottomColor;
            half  _TopExponent, _BottomExponent;
            half  _DayBlend, _SunsetBlend, _NightBlend;

            // Stars
            half _StarDensity, _StarBrightness, _StarSize, _StarTwinkleSpeed, _StarGridScale;
            half4 _StarColor;

            // Sun / Moon
            half4  _SunColor;
            float2 _SunPosition;
            half   _SunSize, _SunGlow, _SunVisibility;

            half4  _MoonColor;
            float2 _MoonPosition;
            half   _MoonSize, _MoonVisibility;

            // Clouds
            sampler2D _CloudTex;
            half4  _CloudDayColor, _CloudSunsetColor, _CloudNightColor;
            float4 _Cloud0, _Cloud1, _Cloud2, _Cloud3, _Cloud4, _Cloud5;

            half _AspectRatio;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            // ---- Helpers ----

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            // ---- Sky gradient ----
            // UV.y == 0 → bottom, 0.5 → horizon, 1 → top
            half3 SkyGradient(float uvY)
            {
                float above = pow(saturate((uvY - 0.5) * 2.0), _TopExponent);
                float below = pow(saturate((0.5 - uvY) * 2.0), _BottomExponent);

                half3 day    = lerp(lerp(_DayHorizonColor,    _DayTopColor,    above), _DayBottomColor,    below);
                half3 sunset = lerp(lerp(_SunsetHorizonColor, _SunsetTopColor, above), _SunsetBottomColor, below);
                half3 night  = lerp(lerp(_NightHorizonColor,  _NightTopColor,  above), _NightBottomColor,  below);

                return day * _DayBlend + sunset * _SunsetBlend + night * _NightBlend;
            }

            // ---- Procedural stars ----
            // One star per grid cell; only upper sky (fades near horizon).
            float Stars(float2 uv)
            {
                float2 cell   = floor(uv * _StarGridScale);
                float2 cellUV = frac(uv * _StarGridScale);

                float h       = Hash(cell);
                float present = step(_StarDensity, h);

                float2 starPos = float2(Hash(cell + float2(0.1, 0.2)),
                                        Hash(cell + float2(0.3, 0.4)));

                float dist  = length(cellUV - starPos);
                float shape = 1.0 - smoothstep(0.0, _StarSize, dist);

                float phase   = Hash(cell + float2(5.6, 7.8));
                float twinkle = 0.6 + 0.4 * sin(_Time.y * _StarTwinkleSpeed + phase * 6.2832);

                return present * shape * twinkle * _StarBrightness;
            }

            // ---- Celestial disc (sun / moon) ----
            half4 Celestial(float2 uv, float2 pos, half size, half4 col, half glow, half visibility)
            {
                float2 d = uv - pos;
                d.x *= _AspectRatio;
                float dist = length(d);

                float disc = 1.0 - smoothstep(size * 0.85, size, dist);
                float halo = (1.0 - smoothstep(0, size * 5.0, dist)) * glow * 0.25;

                return half4(col.rgb, saturate(disc + halo) * col.a * visibility);
            }

            // ---- Single cloud sample ----
            // cloud.xy = center (UV), cloud.z = half-size (UV), cloud.w = opacity
            half SampleCloud(float2 uv, float4 cloud)
            {
                float2 localUV = (uv - cloud.xy) / (cloud.z * 2.0) + 0.5;

                // Soft fade at bounding-box edges so the quad clip is invisible
                float edgeFade =
                    smoothstep(0.0, 0.06, localUV.x) * smoothstep(0.0, 0.06, localUV.y) *
                    smoothstep(0.0, 0.06, 1.0 - localUV.x) * smoothstep(0.0, 0.06, 1.0 - localUV.y);

                return tex2D(_CloudTex, saturate(localUV)).a * edgeFade * cloud.w;
            }

            // ---- Fragment ----
            half4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // 1. Sky gradient
                half3 color = SkyGradient(uv.y);

                // 2. Stars — fade out toward horizon, only during night
                float starFade = smoothstep(0.38, 0.60, uv.y);
                float starVal  = Stars(uv) * _NightBlend * starFade;
                color = lerp(color, _StarColor.rgb, saturate(starVal));

                // 3. Sun
                half4 sun = Celestial(uv, _SunPosition, _SunSize, _SunColor, _SunGlow, _SunVisibility);
                color = lerp(color, sun.rgb, sun.a);

                // 4. Moon
                half4 moon = Celestial(uv, _MoonPosition, _MoonSize, _MoonColor, 0.2h, _MoonVisibility);
                color = lerp(color, moon.rgb, moon.a);

                // 5. Clouds — color shifts with time of day
                half4 cloudColor = _CloudDayColor    * _DayBlend
                                 + _CloudSunsetColor * _SunsetBlend
                                 + _CloudNightColor  * _NightBlend;

                float clouds = 0;
                clouds = max(clouds, SampleCloud(uv, _Cloud0));
                clouds = max(clouds, SampleCloud(uv, _Cloud1));
                clouds = max(clouds, SampleCloud(uv, _Cloud2));
                clouds = max(clouds, SampleCloud(uv, _Cloud3));
                clouds = max(clouds, SampleCloud(uv, _Cloud4));
                clouds = max(clouds, SampleCloud(uv, _Cloud5));

                color = lerp(color, cloudColor.rgb, clouds * cloudColor.a);

                return half4(color, 1.0);
            }
            ENDCG
        }
    }
}
