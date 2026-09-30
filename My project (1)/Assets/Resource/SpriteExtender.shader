Shader "Custom/SpriteExtender"
{
    Properties
    {
        _MainTex ("Sprite", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _WoodColor ("Wood Edge Color", Color) = (0.76,0.57,0.36,1)
        _GrainStrength ("Wood Grain Strength", Range(0,1)) = 0.18
        _Depth ("Depth", Float) = 0.3
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half4 _WoodColor;
                half _GrainStrength;
                float _Depth;
                half _Cutoff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 surface : TEXCOORD1;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 surface : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.surface = input.surface;
                output.positionOS = input.positionOS.xyz;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 color;
                if (input.surface.x > 0.5)
                {
                    // Continuous local coordinates keep grain joined across side quads
                    // and attached to the piece as it moves or rotates.
                    float3 p = input.positionOS;
                    float along = p.x * 2.7 + p.y * 1.9;
                    float across = p.z / max(_Depth, 0.001);
                    float phase = across * 45.0 + sin(along * 2.0) * 0.65
                        + sin(along * 5.3 + across * 3.0) * 0.2;
                    // Fade fine lines when viewed from far away to prevent aliasing.
                    float grain = sin(phase) * (1.0 - smoothstep(0.5, 3.0, fwidth(phase)));
                    float finePhase = phase * 3.1 + along * 4.0;
                    float fine = sin(finePhase) * (1.0 - smoothstep(0.5, 3.0, fwidth(finePhase)));
                    float variation = 1.0 + _GrainStrength * (grain * 0.35 + fine * 0.12);
                    float shading = lerp(0.8, 1.0, input.surface.y);
                    color = half4(_WoodColor.rgb * variation * shading, _WoodColor.a * _Tint.a);
                }
                else
                {
                    color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Tint;
                }
                clip(color.a - max(_Cutoff, 0.001h));
                return half4(color.rgb, 1);
            }
            ENDHLSL
        }
    }
}
