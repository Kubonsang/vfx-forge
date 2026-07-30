Shader "VFXForge/Dogfood/GiantShieldBarrier"
{
    Properties
    {
        [HDR] _PrimaryColor("Primary Color", Color) = (0.086, 0.85, 1, 1)
        [HDR] _SecondaryColor("Secondary Color", Color) = (0.957, 1, 1, 1)
        _Emission("Emission", Range(0, 16)) = 6.5
        _Sharpness("Sharpness", Range(0, 1)) = 0.78
        _Age01("Age", Range(0, 1)) = 0
        _LayerAlpha("Layer Alpha", Range(0, 1)) = 1
        _PanelIndex("Panel Index", Range(0, 1)) = 0.5
        _RimMode("Rim Mode", Range(0, 1)) = 0
        _Seed("Seed", Float) = 0
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
                float4 _PrimaryColor;
                float4 _SecondaryColor;
                float _Emission;
                float _Sharpness;
                float _Age01;
                float _LayerAlpha;
                float _PanelIndex;
                float _RimMode;
                float _Seed;
            CBUFFER_END

            float Hash21(float2 value)
            {
                value = frac(value * float2(123.34, 456.21));
                value += dot(value, value + 41.73 + _Seed);
                return frac(value.x * value.y);
            }

            float ValueNoise(float2 value)
            {
                float2 cell = floor(value);
                float2 local = frac(value);
                local = local * local * (3.0 - 2.0 * local);
                float bottom = lerp(
                    Hash21(cell),
                    Hash21(cell + float2(1.0, 0.0)),
                    local.x);
                float top = lerp(
                    Hash21(cell + float2(0.0, 1.0)),
                    Hash21(cell + float2(1.0, 1.0)),
                    local.x);
                return lerp(bottom, top, local.y);
            }

            float HexGrid(float2 uv)
            {
                float2 gridPoint = uv * float2(9.0, 11.0);
                float row = floor(gridPoint.y);
                gridPoint.x += frac(row * 0.5);
                float2 local = abs(frac(gridPoint) - 0.5);
                float hex = max(
                    local.y,
                    local.x * 0.866025 + local.y * 0.5);
                return 1.0 - smoothstep(0.40, 0.485, hex);
            }

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
                float2 uv = input.uv;
                float edgeDistance = min(
                    min(uv.x, 1.0 - uv.x),
                    min(uv.y, 1.0 - uv.y));
                float rim = 1.0 - smoothstep(
                    0.006,
                    lerp(0.055, 0.025, _Sharpness),
                    edgeDistance);
                float hex = HexGrid(uv);
                float scanPosition = frac(_Age01 * 1.8 + _PanelIndex * 0.21);
                float scan = 1.0 - smoothstep(
                    0.015,
                    0.12,
                    abs(uv.y - scanPosition));
                float centerSigil =
                    (1.0 - smoothstep(0.015, 0.065, abs(uv.x - 0.5)))
                    * smoothstep(0.16, 0.34, uv.y)
                    * (1.0 - smoothstep(0.66, 0.84, uv.y));
                float pulse = 0.82
                    + 0.18 * sin((_Age01 * 18.0 + _PanelIndex * 3.1) * 6.28318);

                float dissolveProgress = smoothstep(0.76, 1.0, _Age01);
                float noise = ValueNoise(
                    uv * float2(23.0, 17.0)
                    + float2(_PanelIndex * 7.0, _Seed));
                float dissolve = lerp(
                    1.0,
                    smoothstep(dissolveProgress - 0.18,
                        dissolveProgress + 0.14,
                        noise),
                    dissolveProgress);

                float fieldAlpha =
                    0.075
                    + hex * 0.24
                    + scan * 0.18
                    + centerSigil * 0.34
                    + rim * 0.28;
                float rimAlpha = rim * 0.9
                    + centerSigil * 0.42
                    + hex * rim * 0.3;
                float alpha = lerp(fieldAlpha, rimAlpha, _RimMode)
                    * _LayerAlpha
                    * dissolve
                    * pulse
                    * input.color.a;

                float energy = saturate(
                    rim * 0.9
                    + hex * 0.46
                    + scan * 0.6
                    + centerSigil * 0.8);
                float3 color = lerp(
                    _PrimaryColor.rgb,
                    _SecondaryColor.rgb,
                    energy);
                color *= lerp(0.65, _Emission, energy)
                    * input.color.rgb;
                return half4(color, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
