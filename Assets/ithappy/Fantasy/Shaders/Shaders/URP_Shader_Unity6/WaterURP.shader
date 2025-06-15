Shader "ithappy/WaterURP"
{
    Properties
    {
        [Header(Surface)]
        _MaskSurface ("Mask", 2D) = "black" {}
        _SurfaceOpacity ("Opacity", range(0, 1)) = 1
        _ColorSurface ("Color", color) = (0.9, 0.9, 0.9, 1)

        [Header(Color)]
        _ColorShallow ("Shallow", color) = (0.1, 0.1, 0.7, 1)
        _ColorDeep ("Deep", color) = (0.1, 0.2, 0.9, 1)
        _Depth ("Depth", float) = 1

        [Header(Normal)]
        _NormalMap ("Map", 2D) = "bump" {}
        _NormalStrength ("Strength", range(0, 1)) = 1

        [Header(Optics)]
        _Smoothness ("Smoothness", range(0, 1)) = 1
        _Refraction ("Refraction", float) = 0.03

        [Header(Ambient)]
        _AmbientFresnel ("Fresnel", float) = 1
        _ColorAmbient ("Color", color) = (0.9, 0.9, 1)

        [Header(Caustics)]
        [Toggle] _IsCaustics ("Enable", float) = 1
        _MaskCaustics ("Mask", 2D) = "black" {}

        [Header(Foam)]
        [Toggle] _IsFoam ("Enable", float) = 1
        _MaskFoam ("Mask", 2D) = "white" {}
        _FoamAmount ("Amount", float) = 0.5
        _FoamCutoff ("Cutoff", range(0, 1)) = 0.5
        _ColorFoam ("Color", color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent" 
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM

            #pragma vertex VertexFunction
            #pragma fragment FragmentFunction
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float2 uvWS : TEXCOORD0;
                float4 positionNDC : TEXCOORD1;
                float3 viewVectorWS : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                float3 tangentWS : TEXCOORD4;
                float3 bitangentWS : TEXCOORD5;
                float4 positionCS : SV_POSITION;
            };

            TEXTURE2D(_MaskSurface);
            SAMPLER(sampler_MaskSurface);
            float4 _MaskSurface_ST;
            half _SurfaceOpacity;
            half3 _ColorSurface;

            half3 _ColorShallow;
            half3 _ColorDeep;
            half _Depth;

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            float4 _NormalMap_ST;
            half _NormalStrength;

            half _Refraction;
            half _Smoothness;

            half _AmbientFresnel;
            half3 _ColorAmbient;

            bool _IsCaustics;
            TEXTURE2D(_MaskCaustics);
            SAMPLER(sampler_MaskCaustics);
            float4 _MaskCaustics_ST;

            bool _IsFoam;
            TEXTURE2D(_MaskFoam);
            SAMPLER(sampler_MaskFoam);
            float4 _MaskFoam_ST;
            half _FoamAmount;
            half _FoamCutoff;
            half3 _ColorFoam;

            Varyings VertexFunction(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                
                output.positionCS = vertexInput.positionCS;
                output.positionNDC = vertexInput.positionNDC;
                output.uvWS = vertexInput.positionWS.xz;
                output.viewVectorWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                output.normalWS = normalInput.normalWS;
                output.tangentWS = normalInput.tangentWS;
                output.bitangentWS = normalInput.bitangentWS;
                
                return output;
            }

            float Fresnel(float3 normal, float3 viewDir, float power)
            {
                return pow((1.0 - saturate(dot(normalize(normal), normalize(viewDir)))), power);
            }

            // Operations
            half3 NormalBlend(half3 A, half3 B)
            {
                return normalize(half3(A.rg + B.rg, A.b * B.b));
            }

            half4 FragmentFunction(Varyings input) : SV_Target
            {
                half3 viewDir = normalize(input.viewVectorWS);
                float2 screenUV = input.positionNDC.xy / input.positionNDC.w;

                // Calculating Normal
                half4 normalSample = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, 
                    input.uvWS * _NormalMap_ST.xy + _Time.y * _NormalMap_ST.zw);
                half3 normalTS = UnpackNormalScale(normalSample, _NormalStrength);
                half3 normal = TransformTangentToWorld(normalTS, half3x3(input.tangentWS, input.bitangentWS, input.normalWS));

                // Calculating Direct Depth
                float sceneDepthRaw = SampleSceneDepth(screenUV);
                float sceneDepth = LinearEyeDepth(sceneDepthRaw, _ZBufferParams);
                float surfaceDepth = LinearEyeDepth(input.positionCS.z / input.positionCS.w, _ZBufferParams);
                float depth = sceneDepth - surfaceDepth;
                half depthMask = saturate(depth / _Depth);

                // Calculating Refracted Depth
                float2 refractedUV = screenUV + normal.xz * _Refraction;
                float refractedDepthRaw = SampleSceneDepth(refractedUV);
                float refractedSceneDepth = LinearEyeDepth(refractedDepthRaw, _ZBufferParams);
                float refractedDepth = refractedSceneDepth - surfaceDepth;
                half refMask = saturate(refractedDepth / _Depth);

                // Shallow-Deep Coloring
                half3 waterColor = lerp(_ColorShallow, _ColorDeep, refMask);

                // Get main light
                Light mainLight = GetMainLight();
                
                // Specular Coloring
                half3 halfVector = normalize(mainLight.direction + viewDir);
                half specMask = pow(saturate(dot(normal, halfVector)), _Smoothness * 1000) * sqrt(_Smoothness);
                waterColor = lerp(waterColor, half3(1, 1, 1), specMask * mainLight.color);

                // Surface Mask Coloring
                half surfaceMask = SAMPLE_TEXTURE2D(_MaskSurface, sampler_MaskSurface, 
                    input.uvWS * _MaskSurface_ST.xy + _Time.y * _MaskSurface_ST.zw).r;
                waterColor = lerp(waterColor, _ColorSurface, surfaceMask * _SurfaceOpacity);

                // Fade Fresnel Coloring
                half fresnel = saturate(Fresnel(normal, viewDir, _AmbientFresnel) + 
                    Fresnel(half3(0, 1, 0), viewDir, _AmbientFresnel));
                waterColor = lerp(waterColor, _ColorAmbient, fresnel);

                // Caustics Coloring
                if(_IsCaustics)
                {
                    half3 causticsMask = SAMPLE_TEXTURE2D(_MaskCaustics, sampler_MaskCaustics, 
                        input.uvWS * _MaskCaustics_ST.xy + _Time.y * _MaskCaustics_ST.zw).rgb;
                    waterColor = lerp(waterColor, half3(1, 1, 1), causticsMask * (1 - depthMask));
                }

                // Foam Coloring
                if(_IsFoam)
                {
                    half foamMask = SAMPLE_TEXTURE2D(_MaskFoam, sampler_MaskFoam, 
                        input.uvWS * _MaskFoam_ST.xy + _Time.y * _MaskFoam_ST.zw).r * 
                        (1 - saturate(depth / _FoamAmount));
                    foamMask = step(_FoamCutoff, foamMask);
                    waterColor = lerp(waterColor, _ColorFoam, foamMask);
                }

                return half4(waterColor.rgb, 1);
            }

            ENDHLSL
        }

        Pass 
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ColorMask 0
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"

            ENDHLSL
        }

        Pass 
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"

            ENDHLSL
        }
    }
}