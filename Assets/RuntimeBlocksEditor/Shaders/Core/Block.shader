Shader "RuntimeBlocksEditor/Block"
{
    Properties
    {
        _BaseMap ("Base Color Map", 2D) = "white" {}
        _BaseColor ("Base Color Tint", Color) = (1,1,1,1)
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _OcclusionMap ("Occlusion Map", 2D) = "white" {}
        _EmissionMap ("Emission Map", 2D) = "black" {}
        _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        _EmissionStrength ("Emission Strength", Range(0, 20)) = 1
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0
        _AmbientOcclusion ("Ambient Occlusion", Range(0, 1)) = 1
        _NormalStrength ("Normal Strength", Range(0, 2)) = 1
        
        // Color intensity property
        [Header(Color Settings)]
        _ColorIntensity ("Color Intensity", Range(0, 1)) = 1
        [Toggle] _NoGrayScaleColoring ("No Grayscale Coloring", Float) = 0
        
        // Outline properties
        [Header(Outline Settings)]
        _FirstOutlineColor("Primary Outline Color", Color) = (0.2,0.4,1,0.5)
        _FirstOutlineWidth("Primary Outline Width", Range(0.0, 0.2)) = 0.05
        _SecondOutlineColor("Secondary Outline Color", Color) = (0.2,0.4,1,1)
        _SecondOutlineWidth("Secondary Outline Width", Range(0.0, 0.2)) = 0.025
        _Angle("Outline Angle Threshold", Range(0.0, 180.0)) = 89
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        
        // Main Pass - must be rendered first to ensure proper depth buffer
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };
            
            TEXTURE2D(_BaseMap);
            TEXTURE2D(_NormalMap);
            TEXTURE2D(_OcclusionMap);
            TEXTURE2D(_EmissionMap);
            
            SAMPLER(sampler_BaseMap);
            SAMPLER(sampler_NormalMap);
            SAMPLER(sampler_OcclusionMap);
            SAMPLER(sampler_EmissionMap);
            
            CBUFFER_START(UnityPerMaterial)
                float _Smoothness;
                float _Metallic;
                float _AmbientOcclusion;
                float _NormalStrength;
                float _EmissionStrength;
                float4 _EmissionColor;
                float4 _BaseColor;
                float4x4 _ObjectToWorldPrev;
                float _ColorIntensity;
                float _NoGrayScaleColoring;
                float4 _FirstOutlineColor;
                float _FirstOutlineWidth;
                float4 _SecondOutlineColor;
                float _SecondOutlineWidth;
                float _Angle;
            CBUFFER_END
            
            // Convert RGB to greyscale value
            float RGBToGreyscale(float3 color)
            {
                // Standard luminance conversion
                return dot(color, float3(0.299, 0.587, 0.114));
            }
            
            // Apply greyscale recolor to a texture
            float3 ApplyGreyscaleRecolor(float3 texColor, float3 tintColor, float intensity)
            {
                // If NoGrayScaleColoring is enabled, just tint the texture directly
                if (_NoGrayScaleColoring > 0.5) {
                    return texColor * tintColor;
                }
                
                // Convert texture to greyscale
                float grey = RGBToGreyscale(texColor);
                
                // Check if tint is white (pure greyscale)
                if (all(tintColor > 0.95) && max(abs(tintColor.r - tintColor.g), max(abs(tintColor.r - tintColor.b), abs(tintColor.g - tintColor.b))) < 0.05) {
                    // Just return greyscale
                    return float3(grey, grey, grey);
                }
                
                // Apply the tint color
                float3 recoloredTex = grey * tintColor;
                
                // Apply intensity
                return lerp(texColor, recoloredTex, intensity);
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.tangentWS = float4(normalInput.tangentWS, input.tangentOS.w);
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                output.uv = input.uv;
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                
                // Sample textures normally using UV coordinates
                float3 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                float3 normalMap = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));
                float occlusion = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv).r;
                float3 emissionTex = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb;
                
                // Apply greyscale recoloring to base color
                float3 baseColor = ApplyGreyscaleRecolor(texColor, _BaseColor.rgb, _ColorIntensity);
                
                // Get emission mask from texture intensity
                float emissionMask = RGBToGreyscale(emissionTex);
                
                // Apply emission color with transparency
                float3 emission = emissionMask * _EmissionColor.rgb * _EmissionStrength;
                
                // Normal mapping
                normalMap.xy *= _NormalStrength;
                float3 bitangent = normalize(cross(normalWS, input.tangentWS.xyz)) * input.tangentWS.w;
                float3x3 TBN = float3x3(normalize(input.tangentWS.xyz), bitangent, normalWS);
                float3 finalNormal = normalize(mul(normalMap, TBN));
                
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = finalNormal;
                inputData.viewDirectionWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                inputData.shadowCoord = 0;
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = 0;
                inputData.bakedGI = SampleSH(finalNormal);
                inputData.normalizedScreenSpaceUV = 0;
                inputData.shadowMask = 1;
                
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = baseColor;
                surfaceData.metallic = _Metallic;
                surfaceData.specular = 0;
                surfaceData.smoothness = _Smoothness;
                surfaceData.occlusion = occlusion * _AmbientOcclusion;
                surfaceData.emission = emission;
                surfaceData.alpha = 1;
                surfaceData.clearCoatMask = 0;
                surfaceData.clearCoatSmoothness = 1;
                
                float4 finalColor = UniversalFragmentPBR(inputData, surfaceData);
                finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);
                
                return finalColor;
            }
            ENDHLSL
        }
        
        // First Outline Pass - wider, semi-transparent
        Pass
        {
            Name "FirstOutline"
            Tags{ "Queue" = "Transparent+100" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _FirstOutlineColor;
                float _FirstOutlineWidth;
                float4 _SecondOutlineColor;
                float _SecondOutlineWidth;
                float _Angle;
                float _Smoothness;
                float _Metallic;
                float _AmbientOcclusion;
                float _NormalStrength;
                float _EmissionStrength;
                float4 _EmissionColor;
                float4 _BaseColor;
                float4x4 _ObjectToWorldPrev;
                float _ColorIntensity;
                float _NoGrayScaleColoring;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Skip outline if width is effectively zero
                if (_FirstOutlineWidth <= 0.0001)
                {
                    output.positionCS = float4(0, 0, -10, 1); // Off-screen
                    output.color = float4(0, 0, 0, 0);
                    return output;
                }
                
                float3 posOS = input.positionOS.xyz;
                float3 normalOS = normalize(input.normalOS.xyz);
                
                // Calculate scale direction from vertex to origin
                float3 scaleDir = normalize(posOS);
                
                // Check the angle between normal and scale direction
                float angle = degrees(acos(dot(scaleDir, normalOS)));
                
                // Choose scaling method based on angle threshold
                if (angle > _Angle) {
                    // Use normal-based scaling for surfaces facing away from center
                    posOS += normalOS * _FirstOutlineWidth;
                } else {
                    // Use center-based scaling for edges
                    posOS += scaleDir * _FirstOutlineWidth;
                }
                
                // Transform to clip space
                output.positionCS = TransformObjectToHClip(posOS);
                output.color = _FirstOutlineColor;
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                return input.color;
            }
            ENDHLSL
        }
        
        // Second Outline Pass - thinner, solid
        Pass
        {
            Name "SecondOutline"
            Tags{ "Queue" = "Transparent+200" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _FirstOutlineColor;
                float _FirstOutlineWidth;
                float4 _SecondOutlineColor;
                float _SecondOutlineWidth;
                float _Angle;
                float _Smoothness;
                float _Metallic;
                float _AmbientOcclusion;
                float _NormalStrength;
                float _EmissionStrength;
                float4 _EmissionColor;
                float4 _BaseColor;
                float4x4 _ObjectToWorldPrev;
                float _ColorIntensity;
                float _NoGrayScaleColoring;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Skip outline if width is effectively zero
                if (_SecondOutlineWidth <= 0.0001)
                {
                    output.positionCS = float4(0, 0, -10, 1); // Off-screen
                    output.color = float4(0, 0, 0, 0);
                    return output;
                }
                
                float3 posOS = input.positionOS.xyz;
                float3 normalOS = normalize(input.normalOS.xyz);
                
                // Calculate scale direction from vertex to origin
                float3 scaleDir = normalize(posOS);
                
                // Check the angle between normal and scale direction
                float angle = degrees(acos(dot(scaleDir, normalOS)));
                
                // Choose scaling method based on angle threshold
                if (angle > _Angle) {
                    // Use normal-based scaling for surfaces facing away from center
                    posOS += normalOS * _SecondOutlineWidth;
                } else {
                    // Use center-based scaling for edges
                    posOS += scaleDir * _SecondOutlineWidth;
                }
                
                // Transform to clip space
                output.positionCS = TransformObjectToHClip(posOS);
                output.color = _SecondOutlineColor;
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                return input.color;
            }
            ENDHLSL
        }
        
        // Shadow caster pass - important for shadows
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Smoothness;
                float _Metallic;
                float _AmbientOcclusion;
                float _NormalStrength;
                float _EmissionStrength;
                float4 _EmissionColor;
                float4 _BaseColor;
                float4x4 _ObjectToWorldPrev;
                float _ColorIntensity;
                float _NoGrayScaleColoring;
                float4 _FirstOutlineColor;
                float _FirstOutlineWidth;
                float4 _SecondOutlineColor;
                float _SecondOutlineWidth;
                float _Angle;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                
                output.positionCS = positionCS;
                
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            
            ENDHLSL
        }
    }
} 