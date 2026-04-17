Shader "Custom/Water2D_URP"
{
    // =============================================================
    //   2D WATER SHADER — Unity URP  (Mesh / SpriteRenderer)
    //   Sections:
    //     [1] Distortion
    //     [2] Body Color & Depth
    //     [3] Caustics
    //     [4] Surface Line (highlight + shimmer)
    //     [5] Foam
    //     [6] Specular & Fresnel
    //     [7] Reflection Tint
    // =============================================================
    Properties
    {
        [Header(Global)]
        _TimeScale          ("Time Scale",                  Range(0, 5))    = 1.0
        _UVTileX            ("UV Tile X",                   Range(0.1, 6))  = 2.5
        _UVTileY            ("UV Tile Y",                   Range(0.1, 6))  = 1.5

        [Header(Distortion)]
        _DistortionStrength ("Strength",                    Range(0, 0.1))  = 0.028
        _DistortionSpeed    ("Speed",                       Range(0, 5))    = 1.2
        _DistortionScale    ("Scale",                       Range(0.5, 10)) = 3.5
        _Distortion2Strength("Strength 2",                  Range(0, 0.1))  = 0.014
        _Distortion2Speed   ("Speed 2",                     Range(0, 5))    = 0.75
        _Distortion2Scale   ("Scale 2",                     Range(0.5, 10)) = 5.5

        [Header(Body Color)]
        _ColorShallow       ("Color Shallow",               Color)          = (0.20, 0.65, 0.75, 0.85)
        _ColorDeep          ("Color Deep",                  Color)          = (0.02, 0.18, 0.38, 0.97)
        _DepthGradient      ("Depth Gradient Bias",         Range(0, 1))    = 0.55
        _DepthEdgeFade      ("Top Edge Fade",               Range(0, 0.3))  = 0.05
        _AlphaOverall       ("Alpha Overall",               Range(0, 1))    = 1.0

        [Header(Caustics)]
        [Toggle] _CausticsEnabled ("Enabled",              Float)          = 1
        _CausticsScale      ("Scale",                       Range(1, 25))   = 7.0
        _CausticsSpeed      ("Speed",                       Range(0, 4))    = 0.8
        _CausticsStrength   ("Strength",                    Range(0, 1.5))  = 0.55
        _CausticsColor      ("Color",                       Color)          = (0.85, 1.0, 1.0, 1.0)
        _CausticsDepthFall  ("Depth Falloff",               Range(0, 8))    = 3.0

        [Header(Surface Line)]
        [Toggle] _LineEnabled ("Enabled",                  Float)          = 1
        _LinePosition       ("Position (Y)",                Range(0, 0.4))  = 0.04
        _LineThickness      ("Thickness",                   Range(0, 0.1))  = 0.018
        _LineFeather        ("Feather",                     Range(0, 0.1))  = 0.016
        _LineColor          ("Color",                       Color)          = (1, 1, 1, 0.85)
        _ShimmerOffset      ("Shimmer Offset",              Range(0, 0.2))  = 0.055
        _ShimmerThickness   ("Shimmer Thickness",           Range(0, 0.08)) = 0.007
        _ShimmerAlpha       ("Shimmer Alpha",               Range(0, 1))    = 0.35
        _LineWaveSpeed      ("Wave Speed",                  Range(0, 5))    = 1.5
        _LineWaveAmp        ("Wave Amplitude",              Range(0, 0.06)) = 0.012
        _LineWaveFreq       ("Wave Frequency",              Range(1, 25))   = 6.0

        [Header(Foam)]
        [Toggle] _FoamEnabled ("Enabled",                  Float)          = 1
        _FoamPosition       ("Band Height",                 Range(0, 0.4))  = 0.06
        _FoamFeather        ("Feather",                     Range(0, 0.3))  = 0.08
        _FoamSpeed          ("Speed",                       Range(0, 4))    = 0.6
        _FoamScale          ("Scale",                       Range(1, 18))   = 6.0
        _FoamDensity        ("Density",                     Range(0, 1))    = 0.55
        _FoamAlpha          ("Alpha",                       Range(0, 1))    = 0.60
        _FoamColor          ("Color",                       Color)          = (1, 1, 1, 1)

        [Header(Specular and Fresnel)]
        [Toggle] _SpecularEnabled ("Enabled",              Float)          = 1
        _LightDirX          ("Light Dir X",                 Range(-1, 1))   =  0.5
        _LightDirY          ("Light Dir Y",                 Range(-1, 1))   = -1.0
        _SpecularStrength   ("Strength",                    Range(0, 2))    = 0.5
        _SpecularShininess  ("Shininess",                   Range(1, 128))  = 40.0
        _FresnelStrength    ("Fresnel Edge Strength",       Range(0, 1))    = 0.30
        _FresnelPower       ("Fresnel Edge Power",          Range(0.5, 8))  = 2.5

        [Header(Reflection Tint)]
        [Toggle] _ReflectionEnabled ("Enabled",            Float)          = 1
        _ReflectionColor    ("Color",                       Color)          = (0.60, 0.80, 1.0, 1.0)
        _ReflectionStrength ("Strength",                    Range(0, 1))    = 0.18
        _ReflectionSpeed    ("Speed",                       Range(0, 2))    = 0.25
        _ReflectionScale    ("Scale",                       Range(1, 12))   = 4.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"        = "Transparent"
            "RenderPipeline"    = "UniversalPipeline"
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
        }

        Pass
        {
            Name "WaterForward"
            Tags { "LightMode" = "Universal2D" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target   3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ─── Uniforms ───────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                float  _TimeScale;
                float  _UVTileX, _UVTileY;

                float  _DistortionStrength,  _DistortionSpeed,  _DistortionScale;
                float  _Distortion2Strength, _Distortion2Speed, _Distortion2Scale;

                float4 _ColorShallow, _ColorDeep;
                float  _DepthGradient, _DepthEdgeFade, _AlphaOverall;

                float  _CausticsEnabled;
                float  _CausticsScale, _CausticsSpeed, _CausticsStrength, _CausticsDepthFall;
                float4 _CausticsColor;

                float  _LineEnabled;
                float  _LinePosition, _LineThickness, _LineFeather;
                float4 _LineColor;
                float  _ShimmerOffset, _ShimmerThickness, _ShimmerAlpha;
                float  _LineWaveSpeed, _LineWaveAmp, _LineWaveFreq;

                float  _FoamEnabled;
                float  _FoamPosition, _FoamFeather, _FoamSpeed;
                float  _FoamScale, _FoamDensity, _FoamAlpha;
                float4 _FoamColor;

                float  _SpecularEnabled;
                float  _LightDirX, _LightDirY;
                float  _SpecularStrength, _SpecularShininess;
                float  _FresnelStrength, _FresnelPower;

                float  _ReflectionEnabled;
                float4 _ReflectionColor;
                float  _ReflectionStrength, _ReflectionSpeed, _ReflectionScale;
            CBUFFER_END

            // ─── Structs ────────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            // ═══════════════════════════════════════════════════════
            //   HELPER FUNCTIONS
            // ═══════════════════════════════════════════════════════

            // ── Hash / value noise ──────────────────────────────────
            float Hash(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);   // quintic smoothstep
                float a = Hash(i + float2(0, 0));
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // ── Fractal Brownian Motion — 4 octaves ─────────────────
            float FBM(float2 p)
            {
                float v = 0.0, amp = 0.5;
                UNITY_UNROLL
                for (int i = 0; i < 4; i++)
                {
                    v   += amp * ValueNoise(p);
                    amp *= 0.5;
                    p    = p * 2.0 + float2(1.3, 0.7);
                }
                return v;
            }

            // ── Caustics: two counter-rotated FBM fields ────────────
            float CausticsPattern(float2 uv, float t)
            {
                float a1 = t * 0.5;
                float2 uv1 = float2(
                    uv.x * cos(a1) - uv.y * sin(a1),
                    uv.x * sin(a1) + uv.y * cos(a1));

                float a2 = -t * 0.37;
                float2 uv2 = float2(
                    uv.x * cos(a2) - uv.y * sin(a2),
                    uv.x * sin(a2) + uv.y * cos(a2));

                float n1 = FBM(uv1);
                float n2 = FBM(uv2 + float2(3.7, 1.5));

                float c = pow(abs(n1 - n2), 0.6);
                c = 1.0 - smoothstep(0.0, 0.4, c);
                return c * c;
            }

            // ── Soft band (two-sided fade) ───────────────────────────
            float Band(float y, float center, float halfW, float feather)
            {
                float d = abs(y - center) - halfW;
                return 1.0 - smoothstep(0.0, feather, d);
            }

            // ── One-sided surface mask ───────────────────────────────
            float SurfaceMask(float y, float edge, float feather)
            {
                return 1.0 - smoothstep(edge, edge + feather, y);
            }

            // ═══════════════════════════════════════════════════════
            //   VERTEX
            // ═══════════════════════════════════════════════════════
            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                OUT.color       = IN.color;
                return OUT;
            }

            // ═══════════════════════════════════════════════════════
            //   FRAGMENT
            // ═══════════════════════════════════════════════════════
            half4 Frag(Varyings IN) : SV_Target
            {
                float T    = _Time.y * _TimeScale;   // _Time.y = elapsed seconds in URP
                float2 ruv = IN.uv;                  // raw 0-1 UV (Y=0 top, Y=1 bottom)
                float2 uv  = ruv * float2(_UVTileX, _UVTileY);

                // ── [1] DISTORTION ───────────────────────────────────
                float d1 = ValueNoise(uv * _DistortionScale
                           + float2(T * _DistortionSpeed, T * _DistortionSpeed * 0.6));
                float d2 = ValueNoise(uv * _Distortion2Scale
                           + float2(-T * _Distortion2Speed * 0.8, T * _Distortion2Speed));

                float2 distOffset = float2(
                    (d1 - 0.5) * 2.0 * _DistortionStrength  + (d2 - 0.5) * 2.0 * _Distortion2Strength,
                    (d1 - 0.5) * _DistortionStrength * 0.5);

                float2 duv  = uv  + distOffset;
                float2 druv = ruv + distOffset * 0.4;

                // ── [2] BODY COLOR & DEPTH ───────────────────────────
                // UV.y == 0 → surface (shallow), UV.y == 1 → deep
                float depthT   = pow(ruv.y, 1.0 - _DepthGradient);
                float4 body    = lerp(_ColorShallow, _ColorDeep, depthT);

                // Soft alpha fade at very top edge
                float topFade  = smoothstep(0.0, _DepthEdgeFade, ruv.y);
                body.a        *= topFade;

                float4 col = body;

                // ── [3] CAUSTICS ─────────────────────────────────────
                UNITY_BRANCH
                if (_CausticsEnabled > 0.5)
                {
                    float caus      = CausticsPattern(duv * _CausticsScale, T * _CausticsSpeed);
                    float depthFade = exp(-ruv.y * _CausticsDepthFall);
                    float causVal   = caus * _CausticsStrength * depthFade;
                    col.rgb        += _CausticsColor.rgb * causVal;
                }

                // ── [7] REFLECTION TINT ──────────────────────────────
                UNITY_BRANCH
                if (_ReflectionEnabled > 0.5)
                {
                    float refN    = FBM(duv * _ReflectionScale + float2(T * _ReflectionSpeed, 0.0));
                    float refFade = 1.0 - smoothstep(0.0, 0.6, ruv.y);
                    col.rgb       = lerp(col.rgb, _ReflectionColor.rgb,
                                        refN * _ReflectionStrength * refFade);
                }

                // ── [6] SPECULAR & FRESNEL ───────────────────────────
                UNITY_BRANCH
                if (_SpecularEnabled > 0.5)
                {
                    // Fake surface normal from FBM gradient
                    const float eps = 0.008;
                    float h0 = FBM(duv * 3.0);
                    float hx = FBM((duv + float2(eps, 0.0)) * 3.0);
                    float hy = FBM((duv + float2(0.0, eps)) * 3.0);
                    float3 n = normalize(float3(-(hx - h0) / eps,
                                               -(hy - h0) / eps,
                                                0.35));

                    float3 L    = normalize(float3(_LightDirX, _LightDirY, 1.5));
                    float spec  = pow(max(dot(n, L), 0.0), _SpecularShininess);
                    col.rgb    += spec * _SpecularStrength;

                    // Edge Fresnel brightening
                    float fx   = abs(ruv.x - 0.5) * 2.0;
                    float fres = pow(fx, _FresnelPower) * _FresnelStrength;
                    col.rgb   += _ColorShallow.rgb * fres;
                }

                // ── [5] FOAM ─────────────────────────────────────────
                UNITY_BRANCH
                if (_FoamEnabled > 0.5)
                {
                    float fn       = FBM(duv * _FoamScale
                                    + float2(T * _FoamSpeed * 0.3, T * _FoamSpeed * 0.15));
                    float foamMask = SurfaceMask(ruv.y, _FoamPosition, _FoamFeather);
                    float foamN    = step(1.0 - _FoamDensity, fn) * foamMask;
                    col.rgb        = lerp(col.rgb, _FoamColor.rgb, foamN * _FoamAlpha);
                }

                // ── [4] SURFACE LINE & SHIMMER ───────────────────────
                UNITY_BRANCH
                if (_LineEnabled > 0.5)
                {
                    // Primary wave offset
                    float wave = sin(ruv.x * _LineWaveFreq + T * _LineWaveSpeed) * _LineWaveAmp
                               + (ValueNoise(float2(ruv.x * 4.0, T * 0.5)) - 0.5) * _LineWaveAmp * 0.5;
                    float ly   = ruv.y + wave;

                    // Main highlight line
                    float mainLine = Band(ly, _LinePosition, _LineThickness * 0.5, _LineFeather);
                    // Break it up horizontally for a natural look
                    float hmod     = 0.5 + 0.5 * FBM(float2(ruv.x * 5.0, T * 0.4));
                    mainLine      *= hmod;
                    col.rgb        = lerp(col.rgb, _LineColor.rgb, mainLine * _LineColor.a);

                    // Secondary shimmer line
                    float wave2 = sin(ruv.x * (_LineWaveFreq * 1.3) + T * _LineWaveSpeed * 0.7 + 1.0)
                                * _LineWaveAmp * 0.6;
                    float ly2   = ruv.y + wave2;
                    float shim  = Band(ly2, _LinePosition + _ShimmerOffset,
                                       _ShimmerThickness * 0.5, _LineFeather * 0.5);
                    shim       *= (0.4 + 0.6 * FBM(float2(ruv.x * 7.0 + 2.3, T * 0.55)));
                    col.rgb     = lerp(col.rgb, _LineColor.rgb, shim * _ShimmerAlpha);
                }

                // ── FINAL ────────────────────────────────────────────
                // Preserve vertex color tint (SpriteRenderer tint support)
                col.rgb *= IN.color.rgb;
                col.a   *= _AlphaOverall * IN.color.a;
                col.a    = saturate(col.a);
                col.rgb  = min(col.rgb, 1.5);  // soft HDR headroom for bloom

                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
