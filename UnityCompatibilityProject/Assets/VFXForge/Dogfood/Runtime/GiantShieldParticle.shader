Shader "VFXForge/Dogfood/GiantShieldParticle"
{
    Properties
    {
        [HDR] _Tint("Tint", Color) = (0.12, 0.84, 1, 1)
        _Softness("Softness", Range(0.5, 8)) = 3.8
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }
        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float _Softness;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS =
                    TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 centered = input.uv * 2.0 - 1.0;
                float longitudinal = pow(
                    saturate(1.0 - abs(centered.x)),
                    0.72);
                float transverse = pow(
                    saturate(1.0 - abs(centered.y)),
                    _Softness);
                float diamond = saturate(
                    1.0 - abs(centered.x) - abs(centered.y) * 0.45);
                float shape = max(
                    longitudinal * transverse,
                    diamond * 0.72);
                float alpha = shape * input.color.a * _Tint.a;
                float3 color = input.color.rgb
                    * _Tint.rgb
                    * lerp(1.4, 4.2, shape);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
