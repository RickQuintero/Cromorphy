// 2D Grass — standard CPU-mesh path, identical to how PixelWater renders.
// No StructuredBuffers. No SRV. No compute. No render feature.
// Vertex data comes from a plain Mesh updated each frame by GrassComputeScript.
Shader "Custom/GrassComputeSurface"
{
    Properties
    {
        _TopTint    ("Top Tint",    Color) = (0.35, 0.75, 0.15, 1)
        _BottomTint ("Bottom Tint", Color) = (0.08, 0.28, 0.04, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }
        Cull Off
        ZWrite On

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 posCS : SV_POSITION;
                float2 uv    : TEXCOORD0;
                float4 color : TEXCOORD1;
            };

            float4 _TopTint, _BottomTint;

            Varyings vert(Attributes i)
            {
                Varyings o;
                // Grass vertices are already in world space — skip object transform.
                o.posCS = TransformWorldToHClip(i.positionOS);
                o.uv    = i.uv;
                o.color = i.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return lerp(_BottomTint, _TopTint, i.uv.y) * i.color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
