Shader "VFXForge/Dogfood/HolyAegisShield"
{
    Properties
    {
        _PrimaryColor ("Primary Color", Color) = (0.02, 0.85, 0.48, 1)
        _SecondaryColor ("Secondary Color", Color) = (1.0, 0.72, 0.18, 1)
        _Emission ("Emission", Range(0, 12)) = 5.8
        _Sharpness ("Sharpness", Range(0, 1)) = 0.82
        _Age01 ("Age", Range(0, 1)) = 0
        _LayerAlpha ("Layer Alpha", Range(0, 1)) = 1
        _LayerMode ("Layer Mode", Range(0, 2)) = 0
        _Seed ("Seed", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "HolyAegisForward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _PrimaryColor;
                half4 _SecondaryColor;
                float _Emission;
                float _Sharpness;
                float _Age01;
                float _LayerAlpha;
                float _LayerMode;
                float _Seed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.uv = input.uv;
                return output;
            }

            float Hash21(float2 samplePosition)
            {
                samplePosition = frac(
                    samplePosition * float2(123.34, 345.45));
                samplePosition += dot(
                    samplePosition,
                    samplePosition + 34.345 + _Seed);
                return frac(
                    samplePosition.x * samplePosition.y);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 centered = input.uv - 0.5;
                float radius = length(centered);
                float angle = atan2(centered.y, centered.x);
                float noise = Hash21(
                    floor(input.uv * 18.0)
                    + floor(_Age01 * 7.0));
                float travelingBand = 0.5 + 0.5 * sin(
                    radius * 54.0
                    - _Age01 * 9.0
                    + angle * 3.0
                    + noise * 2.0);
                float crestBand = pow(
                    saturate(travelingBand),
                    lerp(1.4, 4.8, _Sharpness));
                float rimGleam = 0.5 + 0.5 * sin(
                    angle * 5.0
                    + input.positionWS.x * 1.8
                    - _Age01 * 4.0);

                float energyMode = 1.0 - step(0.5, _LayerMode);
                float goldMode =
                    step(0.5, _LayerMode)
                    * (1.0 - step(1.5, _LayerMode));
                float heraldryMode = step(1.5, _LayerMode);

                half3 emerald = lerp(
                    _PrimaryColor.rgb,
                    half3(0.18, 0.92, 0.52),
                    crestBand * 0.30);
                half3 gold = lerp(
                    _SecondaryColor.rgb,
                    half3(1.0, 0.78, 0.22),
                    rimGleam * 0.36);
                half3 heraldry = lerp(
                    _PrimaryColor.rgb * 0.34,
                    _SecondaryColor.rgb,
                    crestBand * 0.46);

                half3 color =
                    emerald * energyMode
                    + gold * goldMode
                    + heraldry * heraldryMode;
                float baseAlpha =
                    lerp(0.60, 0.78, crestBand) * energyMode
                    + lerp(0.78, 0.94, rimGleam) * goldMode
                    + lerp(0.66, 0.88, crestBand) * heraldryMode;
                float emissionBoost =
                    0.78
                    + crestBand * 0.20
                    + rimGleam * goldMode * 0.14;
                float hdrScale =
                    0.82 + max(0.0, _Emission) * 0.30;
                half3 emissive =
                    color
                    * hdrScale
                    * emissionBoost;

                return half4(
                    emissive,
                    saturate(baseAlpha * _LayerAlpha));
            }
            ENDHLSL
        }
    }
}
