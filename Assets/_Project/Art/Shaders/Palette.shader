Shader "PovuciPotegni/Palette"
{
    Properties
    {
        [MainTexture] _BaseMap("Color palette (linear import)", 2D) = "white" {}
        _RopeRect("Rope tile: offset XY, size ZW", Vector) = (0,0,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _RopeRect;
        CBUFFER_END
        // UV0 addresses the palette; UV1 remains the lightmap unwrap.
        // UV2 stores toon/emission; UV3 stores lit-ground/repeating-rope flags.
        struct Attributes
        {
            float4 positionOS:POSITION; float3 normalOS:NORMAL;
            float2 uv:TEXCOORD0; float2 lightmapUV:TEXCOORD1;
            float2 style:TEXCOORD2; float2 mode:TEXCOORD3;
        };
        half3 PaletteColor(float2 uv, float rope)
        {
            if (rope > 0.5) uv = _RopeRect.xy + frac(uv) * _RopeRect.zw;
            return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb;
        }
        ENDHLSL
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Varyings
            {
                float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0;
                float3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2;
                float2 style:TEXCOORD3; float2 mode:TEXCOORD4;
                float2 lightmapUV:TEXCOORD5; float viewDepth:TEXCOORD6;
                half fog:TEXCOORD7;
            };
            Varyings Vertex(Attributes v)
            {
                Varyings o;
                o.positionWS=TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(v.normalOS);
                o.uv=v.uv; o.style=v.style; o.mode=v.mode;
                o.lightmapUV=v.lightmapUV*unity_LightmapST.xy+unity_LightmapST.zw;
                o.viewDepth=-TransformWorldToView(o.positionWS).z;
                o.fog=ComputeFogFactor(o.positionCS.z);
                return o;
            }
            half ToonRamp(half n)
            {
                half c=n*0.5+0.5;
                return lerp(lerp(120.0/255.0,190.0/255.0,smoothstep(0.25,0.42,c)),1.0,smoothstep(0.58,0.75,c));
            }
            half4 Fragment(Varyings i):SV_Target
            {
                half3 n=normalize(i.normalWS);
                half3 albedo=PaletteColor(i.uv,i.mode.y);
                if(i.mode.x>0.5)
                {
                    // Preserve the land material's URP Simple Lit lighting and baked GI.
                    InputData d=(InputData)0;
                    d.positionWS=i.positionWS; d.normalWS=n;
                    d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
                    d.shadowCoord=TransformWorldToShadowCoord(i.positionWS);
                    d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                    d.bakedGI=SAMPLE_GI(i.lightmapUV,SampleSH(n),n);
                    d.shadowMask=SAMPLE_SHADOWMASK(i.lightmapUV);
                    SurfaceData s=(SurfaceData)0;
                    #if !defined(UNITY_COLORSPACE_GAMMA)
                        albedo=SRGBToLinear(albedo);
                    #endif
                    s.albedo=albedo; s.alpha=1; s.occlusion=1;
                    s.normalTS=half3(0,0,1); s.smoothness=0.5;
                    half4 col=UniversalFragmentBlinnPhong(d,s);
                    col.rgb=MixFog(col.rgb,i.fog);
                    return col;
                }
                Light light=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half direct=i.style.x>0.5?ToonRamp(dot(n,light.direction)):saturate(dot(n,light.direction))*light.shadowAttenuation;
                half3 ambient=lerp(half3(0.310,0.541,0.247)*0.55,half3(0.839,0.925,1.0)*0.55,n.y*0.5+0.5)+0.08;
                half3 col=albedo*(ambient+half3(1,0.953,0.878)*0.7*direct)+i.style.y;
                col=lerp(col,half3(0.659,0.863,1),saturate((i.viewDepth-45)/65));
                #if !defined(UNITY_COLORSPACE_GAMMA)
                    col=SRGBToLinear(saturate(col));
                #endif
                return half4(col,1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection; float3 _LightPosition;
            float4 Vertex(Attributes v):SV_POSITION
            {
                float3 p=TransformObjectToWorld(v.positionOS.xyz);
                float3 n=TransformObjectToWorldNormal(v.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 l=normalize(_LightPosition-p);
                #else
                    float3 l=_LightDirection;
                #endif
                float4 c=TransformWorldToHClip(ApplyShadowBias(p,n,l));
                #if UNITY_REVERSED_Z
                    c.z=min(c.z,UNITY_NEAR_CLIP_VALUE);
                #else
                    c.z=max(c.z,UNITY_NEAR_CLIP_VALUE);
                #endif
                return c;
            }
            half4 Fragment():SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            float4 Vertex(Attributes v):SV_POSITION { return TransformObjectToHClip(v.positionOS.xyz); }
            half4 Fragment():SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "Meta"
            Tags { "LightMode"="Meta" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
            struct MetaV { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float2 style:TEXCOORD1; float2 mode:TEXCOORD2; };
            MetaV Vertex(Attributes v)
            {
                MetaV o;
                o.positionCS=UnityMetaVertexPosition(v.positionOS.xyz,v.lightmapUV,float2(0,0));
                o.uv=v.uv;o.style=v.style;o.mode=v.mode;return o;
            }
            half4 Fragment(MetaV i):SV_Target
            {
                MetaInput m=(MetaInput)0;
                half3 a=PaletteColor(i.uv,i.mode.y);
                #if !defined(UNITY_COLORSPACE_GAMMA)
                    a=SRGBToLinear(a);
                #endif
                m.Albedo=a; m.Emission=i.style.y.xxx;
                return UnityMetaFragment(m);
            }
            ENDHLSL
        }
    }
}
