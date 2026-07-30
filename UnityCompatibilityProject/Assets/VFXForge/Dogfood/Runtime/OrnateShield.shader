Shader "VFXForge/Dogfood/OrnateShield"
{
    Properties
    {
        [HDR] _PrimaryColor("Primary Color", Color) = (0.094, 0.875, 1, 1)
        [HDR] _SecondaryColor("Secondary Color", Color) = (0.97, 0.99, 1, 1)
        [HDR] _AccentColor("Accent Color", Color) = (1, 0.64, 0.16, 1)
        _Emission("Emission", Range(0, 12)) = 5.2
        _Sharpness("Sharpness", Range(0, 1)) = 0.84
        _Age01("Age", Range(0, 1)) = 0
        _LayerAlpha("Layer Alpha", Range(0, 1)) = 1
        _LayerMode("Layer Mode", Range(0, 2)) = 0
        _OrnamentPhase("Ornament Phase", Range(0, 1)) = 0
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
                float4 _AccentColor;
                float _Emission;
                float _Sharpness;
                float _Age01;
                float _LayerAlpha;
                float _LayerMode;
                float _OrnamentPhase;
                float _Seed;
            CBUFFER_END

            float Hash21(float2 value)
            {
                value = frac(value * float2(124.37, 457.19));
                value += dot(value, value + 43.17 + _Seed);
                return frac(value.x * value.y);
            }

            float Noise(float2 value)
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

            float Line(float value, float center, float width)
            {
                return 1.0 - smoothstep(
                    width,
                    width * 2.2,
                    abs(value - center));
            }

            float Circuit(float2 uv)
            {
                float2 grid = frac(uv * float2(8.0, 12.0));
                float vertical = Line(grid.x, 0.5, 0.045);
                float horizontal = Line(grid.y, 0.5, 0.035);
                float nodes = 1.0 - smoothstep(
                    0.05,
                    0.13,
                    length(grid - 0.5));
                float alternate = step(
                    0.5,
                    Hash21(floor(uv * float2(8.0, 12.0))));
                return saturate(
                    nodes + lerp(vertical, horizontal, alternate) * 0.7);
            }

            float Runes(float2 uv)
            {
                float segment = frac(uv.y * 9.0 + _OrnamentPhase);
                float side = abs(uv.x - 0.5);
                float chevron = 1.0 - smoothstep(
                    0.025,
                    0.065,
                    abs(side - abs(segment - 0.5) * 0.72));
                float cross = Line(segment, 0.5, 0.035)
                    * smoothstep(0.08, 0.22, side)
                    * (1.0 - smoothstep(0.34, 0.47, side));
                return saturate(chevron + cross);
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
                float border = 1.0 - smoothstep(
                    0.008,
                    lerp(0.07, 0.028, _Sharpness),
                    edgeDistance);
                float circuit = Circuit(uv);
                float spine = Line(uv.x, 0.5, 0.018);
                float rails = Line(uv.x, 0.13, 0.024)
                    + Line(uv.x, 0.87, 0.024);
                float runes = Runes(uv);
                float scan = 1.0 - smoothstep(
                    0.012,
                    0.11,
                    abs(uv.y - frac(_Age01 * 1.35
                        + _OrnamentPhase)));
                float sigil = Line(uv.x, 0.5, 0.032)
                    * smoothstep(0.14, 0.30, uv.y)
                    * (1.0 - smoothstep(0.70, 0.86, uv.y));

                float fieldAlpha = 0.035
                    + circuit * 0.20
                    + border * 0.23
                    + scan * 0.12
                    + sigil * 0.28;
                float rimAlpha = border * 0.88
                    + circuit * border * 0.34
                    + sigil * 0.42;
                float ornamentAlpha = border * 0.72
                    + spine * 0.92
                    + rails * 0.56
                    + runes * 0.48
                    + scan * 0.16;

                float modeField = 1.0 - step(0.5, _LayerMode);
                float modeRim = step(0.5, _LayerMode)
                    * (1.0 - step(1.5, _LayerMode));
                float modeOrnament = step(1.5, _LayerMode);
                float alpha = fieldAlpha * modeField
                    + rimAlpha * modeRim
                    + ornamentAlpha * modeOrnament;

                float noise = Noise(
                    uv * float2(21.0, 17.0)
                    + float2(_Seed, _OrnamentPhase * 9.0));
                float dissolveProgress = smoothstep(
                    0.78,
                    1.0,
                    _Age01);
                float dissolve = lerp(
                    1.0,
                    smoothstep(
                        dissolveProgress - 0.20,
                        dissolveProgress + 0.12,
                        noise),
                    dissolveProgress);
                float pulse = 0.86
                    + 0.14 * sin(
                        (_Age01 * 9.0 + _OrnamentPhase)
                        * 6.28318);
                alpha *= _LayerAlpha
                    * dissolve
                    * pulse
                    * input.color.a;

                float whiteEnergy = saturate(
                    border * 0.82
                    + circuit * 0.28
                    + spine * 0.55
                    + sigil * 0.68);
                float accentEnergy = saturate(
                    modeOrnament * (runes * 0.66 + rails * 0.22));
                float3 color = lerp(
                    _PrimaryColor.rgb,
                    _SecondaryColor.rgb,
                    whiteEnergy);
                color = lerp(color, _AccentColor.rgb, accentEnergy);
                float brightness = lerp(
                    0.56,
                    max(1.0, _Emission * 0.68),
                    saturate(
                        whiteEnergy * 0.72
                        + accentEnergy * 0.48));
                color *= brightness * input.color.rgb;
                return half4(color, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
