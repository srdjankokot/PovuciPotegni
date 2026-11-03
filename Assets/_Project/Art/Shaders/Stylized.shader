// Reproduces the prototype's look (docs/SCENA.md). three.js r128 lit in gamma space without tone mapping,
// so lighting here runs on sRGB values too and is converted to linear only for output.
Shader "PovuciPotegni/Stylized"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Map (import without sRGB)", 2D) = "white" {}
        [ToggleUI] _Toon("Toon", Float) = 0
        _Emission("Emission", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float _Toon;
            float _Emission;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Values from docs/SCENA.md, in sRGB.
            static const half3 SkyLight = half3(0.839, 0.925, 1.0) * 0.55;
            static const half3 GroundLight = half3(0.310, 0.541, 0.247) * 0.55;
            static const half AmbientLight = 0.08;
            static const half3 SunLight = half3(1.0, 0.953, 0.878) * 0.7;
            static const half3 FogColor = half3(0.659, 0.863, 1.0);
            static const float FogStart = 45.0;
            static const float FogEnd = 110.0;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half3 albedo : TEXCOORD3;
                float viewDepth : TEXCOORD4;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.viewDepth = -TransformWorldToView(output.positionWS).z;

                #if defined(UNITY_COLORSPACE_GAMMA)
                    output.albedo = _BaseColor.rgb;
                #else
                    // Unity uploads colors as linear; undo that once per vertex.
                    output.albedo = LinearToSRGB(_BaseColor.rgb);
                #endif

                return output;
            }

            half ToonRamp(half nDotL)
            {
                // Three levels with soft transitions between them.
                half coordinate = nDotL * 0.5 + 0.5;
                half toMiddle = smoothstep(0.25, 0.42, coordinate);
                half toBright = smoothstep(0.58, 0.75, coordinate);
                return lerp(lerp(120.0 / 255.0, 190.0 / 255.0, toMiddle), 1.0, toBright);
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half3 normal = normalize(input.normalWS);
                Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half nDotL = dot(normal, light.direction);

                half direct = _Toon > 0.5
                    ? ToonRamp(nDotL)
                    : saturate(nDotL) * light.shadowAttenuation;

                half3 ambient = lerp(GroundLight, SkyLight, normal.y * 0.5 + 0.5) + AmbientLight;
                half3 albedo = input.albedo * SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                half3 color = albedo * (ambient + SunLight * direct) + _Emission;

                float fog = saturate((input.viewDepth - FogStart) / (FogEnd - FogStart));
                color = lerp(color, FogColor, fog);

                #if !defined(UNITY_COLORSPACE_GAMMA)
                    color = SRGBToLinear(saturate(color));
                #endif

                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            float4 Vertex(Attributes input) : SV_POSITION
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirection = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirection = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirection));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            half4 Fragment() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vertex
            #pragma fragment Fragment

            float4 Vertex(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            half4 Fragment() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
