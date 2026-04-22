// =============================================================
//   CHAMELEON CLOAK — Unity URP
//   Author: generated for your project
//
//   HOW THE CLOAKING WORKS
//   ──────────────────────
//   URP captures the scene (without transparent objects) into
//   _CameraOpaqueTexture BEFORE transparent objects are drawn.
//   We sample that texture at the sprite's screen-space pixel
//   position so the background "shows through" correctly,
//   regardless of camera movement or mesh position.
//
//   REQUIRED SETUP (one-time)
//   ──────────────────────────
//   1. Select your URP Asset (Project Settings → Graphics)
//   2. Check "Opaque Texture" (this populates _CameraOpaqueTexture)
//   3. Keep this material's Queue = Transparent
//   4. Optionally assign your own RenderTexture to _BackgroundTex
//      and enable "Use Custom RT" — useful for top-down cameras,
//      minimap setups, or when opaque texture is unavailable.
//
//   PARAMETERS AT A GLANCE
//   ──────────────────────
//   _CloakValue   0 = fully visible sprite (lit)
//                 1 = fully cloaked (background shows through)
//   Everything else is cosmetic — tweak in the Inspector.
// =============================================================

Shader "Custom/ChameleonCloak_URP"
{
    Properties
    {
        // ── Sprite ──────────────────────────────────────────────
        [Header(Sprite)]
        _MainTex            ("Sprite Texture",              2D)             = "white" {}
        _Color              ("Tint",                        Color)          = (1,1,1,1)
        _NormalMap          ("Normal Map (optional)",       2D)             = "bump"  {}
        [Toggle] _UseNormal ("Use Normal Map",              Float)          = 0

        // ── Cloaking Core ────────────────────────────────────────
        [Header(Cloaking)]
        _CloakValue         ("Cloak Value  [ 0=visible  1=cloaked ]",
                                                            Range(0,1))     = 0.0
        [Toggle] _UseCustomRT ("Use Custom Render Texture",Float)           = 0
        _BackgroundTex      ("Custom Background RT",        2D)             = "black" {}

        // ── Distortion ───────────────────────────────────────────
        [Header(Distortion)]
        _DistStr            ("Strength",                    Range(0,0.06))  = 0.018
        _DistSpd            ("Speed",                       Range(0,6))     = 1.8
        _DistScl            ("Scale",                       Range(0.5,12))  = 4.5
        _Dist2Str           ("Strength 2",                  Range(0,0.04))  = 0.009
        _Dist2Spd           ("Speed 2",                     Range(0,6))     = 0.85
        _Dist2Scl           ("Scale 2",                     Range(0.5,12))  = 7.0

        // ── Chromatic Aberration ─────────────────────────────────
        [Header(Chromatic Aberration)]
        _ChromaStr          ("Strength",                    Range(0,0.025)) = 0.006

        // ── Rim Glow ─────────────────────────────────────────────
        [Header(Rim Glow)]
        _RimColor           ("Color",                       Color)          = (0.25,1.0,0.45,1)
        _RimWidth           ("Width",                       Range(0,1))     = 0.28
        _RimStrength        ("Strength",                    Range(0,4))     = 1.8
        _RimPulseSpd        ("Pulse Speed",                 Range(0,6))     = 2.2
        _RimPulseMin        ("Pulse Min",                   Range(0,1))     = 0.4

        // ── Iridescence (in-world shimmer when cloaked) ──────────
        [Header(Iridescence)]
        _IriColor           ("Color",                       Color)          = (0.3,0.85,0.5,1)
        _IriStr             ("Strength",                    Range(0,1))     = 0.18
        _IriScl             ("Scale",                       Range(1,20))    = 6.0
        _IriSpd             ("Speed",                       Range(0,4))     = 1.2

        // ── Scale Dissolve (0→1 transition pattern) ──────────────
        [Header(Scale Dissolve)]
        _DissolveScl        ("Scale",                       Range(1,30))    = 12.0
        _DissolveSoft       ("Edge Softness",               Range(0,0.5))   = 0.12
        _DissolveRimW       ("Dissolve Rim Width",          Range(0,0.3))   = 0.08
        _DissolveRimColor   ("Dissolve Rim Color",          Color)          = (0.4,1.0,0.55,1)

        // ── Visibility hint (ghost outline when fully cloaked) ───
        [Header(Ghost Outline)]
        _GhostStr           ("Strength  [ 0 = fully invisible ]",
                                                            Range(0,1))     = 0.12
        _GhostColor         ("Color",                       Color)          = (0.5,1.0,0.6,1)
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
            Name "ChameleonCloak"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target   3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ── Samplers ─────────────────────────────────────────
            // The scene before transparent objects (enable in URP Asset)
            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            TEXTURE2D(_BackgroundTex);
            SAMPLER(sampler_BackgroundTex);

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            // ── Uniform buffer (SRP Batcher compatible) ──────────
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float4 _Color;
                float  _UseNormal;

                float  _CloakValue;
                float  _UseCustomRT;

                float  _DistStr,  _DistSpd,  _DistScl;
                float  _Dist2Str, _Dist2Spd, _Dist2Scl;
                float  _ChromaStr;

                float4 _RimColor;
                float  _RimWidth, _RimStrength, _RimPulseSpd, _RimPulseMin;

                float4 _IriColor;
                float  _IriStr, _IriScl, _IriSpd;

                float  _DissolveScl, _DissolveSoft, _DissolveRimW;
                float4 _DissolveRimColor;

                float  _GhostStr;
                float4 _GhostColor;
            CBUFFER_END

            // ═══════════════════════════════════════════════════════
            //  HELPERS
            // ═══════════════════════════════════════════════════════

            float Hash(float2 p)
            {
                p  = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }

            float VNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(Hash(i), Hash(i + float2(1,0)), u.x),
                    lerp(Hash(i + float2(0,1)), Hash(i + float2(1,1)), u.x),
                    u.y);
            }

            float FBM3(float2 p)
            {
                float v = 0.0, a = 0.5;
                UNITY_UNROLL
                for (int i = 0; i < 3; i++)
                {
                    v   += a * VNoise(p);
                    a   *= 0.5;
                    p    = p * 2.0 + float2(1.3, 0.7);
                }
                return v;
            }

            // Voronoi-ish cell distance — gives organic scale shapes
            float2 VoronoiCell(float2 p)
            {
                float2 ip = floor(p);
                float2 fp = frac(p);
                float minDist = 9999.0;
                float2 minPt  = 0.0;

                UNITY_UNROLL
                for (int y = -1; y <= 1; y++)
                UNITY_UNROLL
                for (int x = -1; x <= 1; x++)
                {
                    float2 nb = float2(x, y);
                    float2 pt = float2(Hash(ip + nb + float2(0.1, 0.0)),
                                       Hash(ip + nb + float2(0.0, 0.1)));
                    float2 diff = nb + pt - fp;
                    float  d    = dot(diff, diff);
                    if (d < minDist) { minDist = d; minPt = pt; }
                }
                return float2(sqrt(minDist), Hash(minPt));
            }

            // ── Two-layer distortion offset ───────────────────────
            float2 DistortionOffset(float2 uv, float T)
            {
                // Layer 1
                float d1 = VNoise(uv * _DistScl + float2( T * _DistSpd,  T * _DistSpd * 0.6));
                float d1b= VNoise(uv * _DistScl + float2(-T * _DistSpd * 0.4, T * _DistSpd * 0.9) + 5.3);
                // Layer 2 (rotated ~45°)
                float2 uv2r = float2(uv.x - uv.y, uv.x + uv.y) * 0.707;
                float d2 = VNoise(uv2r * _Dist2Scl + float2(T * _Dist2Spd, T * _Dist2Spd * 0.55));

                return float2(
                    (d1  - 0.5) * 2.0 * _DistStr + (d2  - 0.5) * 2.0 * _Dist2Str,
                    (d1b - 0.5) * 2.0 * _DistStr * 0.6);
            }

            // ── Sample background with chromatic aberration ───────
            half3 SampleBackground(float2 screenUV, float2 distOff, float cloak)
            {
                float2 uv = screenUV + distOff * cloak;
                float  ca = _ChromaStr * cloak;

                half r, g, b;
                if (_UseCustomRT > 0.5)
                {
                    r = SAMPLE_TEXTURE2D(_BackgroundTex, sampler_BackgroundTex, uv + float2( ca, 0)).r;
                    g = SAMPLE_TEXTURE2D(_BackgroundTex, sampler_BackgroundTex, uv            ).g;
                    b = SAMPLE_TEXTURE2D(_BackgroundTex, sampler_BackgroundTex, uv + float2(-ca, 0)).b;
                }
                else
                {
                    r = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + float2( ca, 0)).r;
                    g = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv            ).g;
                    b = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv + float2(-ca, 0)).b;
                }
                return half3(r, g, b);
            }

            // ═══════════════════════════════════════════════════════
            //  VERTEX
            // ═══════════════════════════════════════════════════════
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
                float4 screenPos   : TEXCOORD1;   // for _CameraOpaqueTexture
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color;
                // ComputeScreenPos gives homogeneous screen coords
                // divide by .w in the fragment to get 0-1 UV
                OUT.screenPos   = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            // ═══════════════════════════════════════════════════════
            //  FRAGMENT
            // ═══════════════════════════════════════════════════════
            half4 Frag(Varyings IN) : SV_Target
            {
                float T = _Time.y;

                // ── Sprite texture ────────────────────────────────
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                sprite      *= _Color * IN.color;

                // Clip fully transparent pixels always
                clip(sprite.a - 0.01);

                // ── Screen-space UV (0-1, correct aspect) ─────────
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                // Flip Y on some platforms (DX vs GL convention)
                #if UNITY_UV_STARTS_AT_TOP
                    screenUV.y = 1.0 - screenUV.y;
                #endif

                // ── Distortion offset (screen-space) ──────────────
                // We distort in a UV space that matches the background texture,
                // so we need to scale distortion by screen aspect ratio
                float2 distOff = DistortionOffset(screenUV * float2(1.0, _ScreenParams.y / _ScreenParams.x),
                                                  T);

                // ── Edge / rim detection ───────────────────────────
                // Sample alpha at 4 neighbours to find edge pixels
                float ts = _MainTex_TexelSize.x * 2.5;
                float aR = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( ts, 0)).a;
                float aL = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(-ts, 0)).a;
                float aU = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( 0,  ts)).a;
                float aD = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( 0, -ts)).a;
                float edgeStr = saturate((aR + aL + aU + aD) / 4.0);
                // Rim: bright where alpha is falling off (edges of sprite)
                float rim = 1.0 - smoothstep(_RimWidth, 1.0, sprite.a * edgeStr);
                // Pulsing
                float rimPulse = lerp(_RimPulseMin, 1.0,
                                      0.5 + 0.5 * sin(T * _RimPulseSpd));
                rim *= rimPulse;

                // ── Scale dissolve pattern ─────────────────────────
                // Voronoi noise creates organic cell shapes (like scales)
                float2 vCell   = VoronoiCell(IN.uv * _DissolveScl);
                float  vDist   = vCell.x;  // distance to nearest cell centre
                float  vID     = vCell.y;  // unique per-cell ID
                // Each cell has a random "activation time" so they dissolve
                // at different moments — looks like scales individually
                // switching off
                float  cellThreshold = vID;  // 0-1 per cell
                // dissolveT: when _CloakValue exceeds cellThreshold, that cell cloaks
                float  dissolveT = saturate((_CloakValue - cellThreshold) / _DissolveSoft + 0.5);
                // Dissolve rim: bright at the dissolve frontier
                float  dFront   = 1.0 - abs(dissolveT - 0.5) * 2.0;
                float  dissRim  = smoothstep(0.0, _DissolveRimW, dFront) * _DissolveRimW;

                // ── Normal map sample (optional) ───────────────────
                float2 normalOffset = 0.0;
                if (_UseNormal > 0.5)
                {
                    half3 nrm    = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv));
                    normalOffset = nrm.xy * 0.01 * _CloakValue;
                    distOff     += normalOffset;
                }

                // ── Background sample (cloaked pixels) ────────────
                half3 bgColor = SampleBackground(screenUV, distOff, _CloakValue);

                // ── Iridescence (shimmer visible on cloaked surface)
                float iriN    = FBM3(IN.uv * _IriScl + float2(T * _IriSpd, T * _IriSpd * 0.6));
                half3 iriCol  = _IriColor.rgb * iriN * _IriStr;

                // ── Build the cloaked pixel ────────────────────────
                // Rim contribution: adds the glow line on top of background
                half3 cloaked = bgColor
                              + _RimColor.rgb * rim * _RimStrength
                              + iriCol;

                // Ghost silhouette: very subtle sprite tint always visible
                float ghostFade = _GhostStr * _CloakValue;
                cloaked        += sprite.rgb * _GhostColor.rgb * ghostFade * sprite.a;

                // Dissolve rim adds a bright fringe during transition
                cloaked += _DissolveRimColor.rgb * dissRim * sprite.a;

                // ── Build the lit (visible) pixel ──────────────────
                // Simple URP main light diffuse for a 2D sprite feel
                half3 litColor = sprite.rgb;

                #ifdef _MAIN_LIGHT_SHADOWS
                    // Shadow support if you have it enabled
                #endif

                // Get main directional light
                Light mainLight = GetMainLight();
                float lightStr  = saturate(dot(float3(0, 0, 1), mainLight.direction)) * 0.5 + 0.5;
                litColor       *= mainLight.color * lightStr + unity_AmbientSky.rgb;

                // ── Blend visible ↔ cloaked using per-cell dissolve ─
                //   dissolveT = 0 → sprite visible
                //   dissolveT = 1 → cloaked
                half3 finalRGB = lerp(litColor, cloaked, dissolveT);

                // ── Alpha ─────────────────────────────────────────
                //   When cloaked, we need alpha=1 so the background
                //   sample is fully written (background * 1 + nothing * 0).
                //   When visible, use sprite alpha.
                //   Rim always slightly raises alpha so glow is visible.
                float rimAlpha  = rim * _RimStrength * 0.5 * _CloakValue;
                float baseAlpha = lerp(sprite.a, 1.0, dissolveT);
                float finalAlpha= saturate(baseAlpha + rimAlpha);

                return half4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
