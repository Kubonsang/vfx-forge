Shader "VFXForge/Dogfood/ProductionCrescentSlash"
{
    Properties
    {
        [HDR] _PrimaryColor("Primary Color", Color) = (0.07, 0.85, 1, 1)
        [HDR] _SecondaryColor("Secondary Color", Color) = (0.91, 1, 1, 1)
        _Emission("Emission", Range(0, 16)) = 5.5
        _Sharpness("Sharpness", Range(0, 1)) = 0.82
        _Age01("Age", Range(0, 1)) = 0
        _LayerAlpha("Layer Alpha", Range(0, 1)) = 1
        _NoiseStrength("Noise Strength", Range(0, 1)) = 0.24
        _Seed("Seed", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            CBUFFER_START(UnityPerMaterial)
                float4 _PrimaryColor; float4 _SecondaryColor; float _Emission; float _Sharpness;
                float _Age01; float _LayerAlpha; float _NoiseStrength; float _Seed;
            CBUFFER_END
            float Hash21(float2 value) { value=frac(value*float2(123.34,456.21)); value+=dot(value,value+45.32+_Seed); return frac(value.x*value.y); }
            float ValueNoise(float2 value)
            {
                float2 cell=floor(value);
                float2 local=frac(value);
                local=local*local*(3.0-2.0*local);
                float bottom=lerp(Hash21(cell),Hash21(cell+float2(1.0,0.0)),local.x);
                float top=lerp(Hash21(cell+float2(0.0,1.0)),Hash21(cell+float2(1.0,1.0)),local.x);
                return lerp(bottom,top,local.y);
            }
            Varyings Vert(Attributes input) { Varyings output; output.positionCS=TransformObjectToHClip(input.positionOS.xyz); output.uv=input.uv; output.color=input.color; return output; }
            half4 Frag(Varyings input):SV_Target
            {
                float tipMask=smoothstep(0.0,0.11,input.uv.x)*(1.0-smoothstep(0.84,1.0,input.uv.x));
                float revealHead=saturate(_Age01*7.5);
                float revealMask=smoothstep(1.0-revealHead-0.2,1.0-revealHead,input.uv.x);
                revealMask=lerp(revealMask,1.0,smoothstep(0.12,0.25,_Age01));
                float noise=ValueNoise(input.uv*float2(38.0,8.0)+float2(_Seed,0.0));
                float dissolvePhase=smoothstep(0.58,1.0,_Age01);
                float dissolveMask=smoothstep(dissolvePhase-0.22,dissolvePhase+0.18,noise);
                dissolveMask=lerp(1.0,1.0-dissolveMask,dissolvePhase);
                float edge=pow(saturate(1.0-abs(input.uv.y*2.0-1.0)),lerp(0.7,3.6,_Sharpness));
                float energyNoise=lerp(1.0,0.72+noise*0.5,_NoiseStrength);
                float alpha=_LayerAlpha*tipMask*revealMask*dissolveMask*edge*input.color.a;
                float3 color=lerp(_PrimaryColor.rgb,_SecondaryColor.rgb,saturate(edge*1.35));
                color*=lerp(1.0,_Emission,edge)*energyNoise*input.color.rgb;
                return half4(color,saturate(alpha));
            }
            ENDHLSL
        }
    }
}
