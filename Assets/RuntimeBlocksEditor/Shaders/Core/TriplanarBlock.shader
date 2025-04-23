Shader "RuntimeBlocksEditor/TriplanarBlock"
{
    Properties
    {
        _BaseMap ("Base Color Map", 2D) = "white" {}
        _BaseColor ("Base Color Tint", Color) = (1,1,1,1)
        _HeightMap ("Height Map", 2D) = "gray" {}
        _HeightScale ("Height Scale", Range(0, 1)) = 0
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _OcclusionMap ("Occlusion Map", 2D) = "white" {}
        _EmissionMap ("Emission Map", 2D) = "black" {}
        _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        _EmissionStrength ("Emission Strength", Range(0, 20)) = 1
        _Blend ("Blend", Range(0, 1)) = 0.5
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0
        _AmbientOcclusion ("Ambient Occlusion", Range(0, 1)) = 1
        _NormalStrength ("Normal Strength", Range(0, 2)) = 1
        _TilingXZ ("Tiling Side (XZ)", Vector) = (1,1,0,0)
        _TilingY ("Tiling Top/Bottom (Y)", Vector) = (1,1,0,0)
        
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
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                float3 objectPos : TEXCOORD4;
            };
            
            TEXTURE2D(_BaseMap);
            TEXTURE2D(_NormalMap);
            TEXTURE2D(_OcclusionMap);
            TEXTURE2D(_EmissionMap);
            TEXTURE2D(_HeightMap);
            
            SAMPLER(sampler_BaseMap);
            SAMPLER(sampler_NormalMap);
            SAMPLER(sampler_OcclusionMap);
            SAMPLER(sampler_EmissionMap);
            SAMPLER(sampler_HeightMap);
            
            CBUFFER_START(UnityPerMaterial)
                float _Blend;
                float _Smoothness;
                float _Metallic;
                float _AmbientOcclusion;
                float _NormalStrength;
                float _EmissionStrength;
                float _HeightScale;
                float4 _EmissionColor;
                float4 _BaseColor;
                float4 _TilingXZ;
                float4 _TilingY;
                float4x4 _ObjectToWorldPrev;
                float _ColorIntensity;
                float _NoGrayScaleColoring;
                float4 _FirstOutlineColor;
                float _FirstOutlineWidth;
                float4 _SecondOutlineColor;
                float _SecondOutlineWidth;
                float _Angle;
            CBUFFER_END
            
            float3 TriplanarMapping(Texture2D tex, SamplerState samp, float3 worldPos, float3 worldNormal, float3 objectPos, float blend)
            {
                float3 weights = pow(abs(worldNormal), blend);
                weights = weights / (weights.x + weights.y + weights.z);
                
                float3 dirX = normalize(UNITY_MATRIX_M._m00_m10_m20);
                float3 dirY = normalize(UNITY_MATRIX_M._m01_m11_m21);
                float3 dirZ = normalize(UNITY_MATRIX_M._m02_m12_m22);
                
                float3 localPos = worldPos - objectPos;
                
                float2 uvX = float2(dot(localPos, dirZ), dot(localPos, dirY)) * _TilingXZ.xy;
                float2 uvY = float2(dot(localPos, dirX), dot(localPos, dirZ)) * _TilingY.xy;
                float2 uvZ = float2(dot(localPos, dirX), dot(localPos, dirY)) * _TilingXZ.xy;
                
                float3 texX = SAMPLE_TEXTURE2D(tex, samp, uvX).rgb;
                float3 texY = SAMPLE_TEXTURE2D(tex, samp, uvY).rgb;
                float3 texZ = SAMPLE_TEXTURE2D(tex, samp, uvZ).rgb;
                
                return texX * weights.x + texY * weights.y + texZ * weights.z;
            }
            
            float TriplanarMappingHeight(float3 worldPos, float3 worldNormal, float3 objectPos, float blend)
            {
                float3 weights = pow(abs(worldNormal), blend);
                weights = weights / (weights.x + weights.y + weights.z);
                
                float3 dirX = normalize(UNITY_MATRIX_M._m00_m10_m20);
                float3 dirY = normalize(UNITY_MATRIX_M._m01_m11_m21);
                float3 dirZ = normalize(UNITY_MATRIX_M._m02_m12_m22);
                
                float3 localPos = worldPos - objectPos;
                
                float2 uvX = float2(dot(localPos, dirZ), dot(localPos, dirY)) * _TilingXZ.xy;
                float2 uvY = float2(dot(localPos, dirX), dot(localPos, dirZ)) * _TilingY.xy;
                float2 uvZ = float2(dot(localPos, dirX), dot(localPos, dirY)) * _TilingXZ.xy;
                
                float heightX = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, uvX).r;
                float heightY = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, uvY).r;
                float heightZ = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, uvZ).r;
                
                return (heightX * weights.x + heightY * weights.y + heightZ * weights.z) * _HeightScale;
            }
            
            float3 TriplanarNormal(Texture2D tex, SamplerState samp, float3 worldPos, float3 worldNormal, float3 objectPos, float blend)
            {
                float3 weights = pow(abs(worldNormal), blend);
                weights = weights / (weights.x + weights.y + weights.z);
                
                float3 dirX = normalize(UNITY_MATRIX_M._m00_m10_m20);
                float3 dirY = normalize(UNITY_MATRIX_M._m01_m11_m21);
                float3 dirZ = normalize(UNITY_MATRIX_M._m02_m12_m22);
                
                float3 localPos = worldPos - objectPos;
                
                float2 uvX = float2(dot(localPos, dirZ), dot(localPos, dirY)) * _TilingXZ.xy;
                float2 uvY = float2(dot(localPos, dirX), dot(localPos, dirZ)) * _TilingY.xy;
                float2 uvZ = float2(dot(localPos, dirX), dot(localPos, dirY)) * _TilingXZ.xy;
                
                float3 tnormalX = UnpackNormal(SAMPLE_TEXTURE2D(tex, samp, uvX));
                float3 tnormalY = UnpackNormal(SAMPLE_TEXTURE2D(tex, samp, uvY));
                float3 tnormalZ = UnpackNormal(SAMPLE_TEXTURE2D(tex, samp, uvZ));
                
                tnormalX.xy *= _NormalStrength;
                tnormalY.xy *= _NormalStrength;
                tnormalZ.xy *= _NormalStrength;
                
                float3 normalX = float3(tnormalX.xy * float2(1,1), tnormalX.z);
                float3 normalY = float3(tnormalY.xy * float2(1,1), tnormalY.z);
                float3 normalZ = float3(tnormalZ.xy * float2(1,1), tnormalZ.z);
                
                float3x3 tbnX = float3x3(dirZ, dirY, worldNormal);
                float3x3 tbnY = float3x3(dirX, dirZ, worldNormal);
                float3x3 tbnZ = float3x3(dirX, dirY, worldNormal);
                
                float3 worldNormalX = mul(normalX, tbnX);
                float3 worldNormalY = mul(normalY, tbnY);
                float3 worldNormalZ = mul(normalZ, tbnZ);
                
                return normalize(worldNormalX * weights.x + worldNormalY * weights.y + worldNormalZ * weights.z);
            }
            
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
                
                output.objectPos = mul(UNITY_MATRIX_M, float4(0,0,0,1)).xyz;
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                
                float height = TriplanarMappingHeight(input.positionWS, normalWS, input.objectPos, _Blend);
                
                float3 positionWS = input.positionWS + normalWS * height;
                
                // Sample texture color
                float3 texColor = TriplanarMapping(_BaseMap, sampler_BaseMap, positionWS, normalWS, input.objectPos, _Blend);
                
                // Apply greyscale recoloring to base color
                float3 baseColor = ApplyGreyscaleRecolor(texColor, _BaseColor.rgb, _ColorIntensity);
                
                float3 normalMap = TriplanarNormal(_NormalMap, sampler_NormalMap, positionWS, normalWS, input.objectPos, _Blend);
                float occlusion = TriplanarMapping(_OcclusionMap, sampler_OcclusionMap, positionWS, normalWS, input.objectPos, _Blend).r;
                
                // Sample emission texture 
                float3 emissionTex = TriplanarMapping(_EmissionMap, sampler_EmissionMap, positionWS, normalWS, input.objectPos, _Blend);
                
                // Apply greyscale recoloring to emission, but allow transparency
                // Get emission mask from original texture intensity
                float emissionMask = RGBToGreyscale(emissionTex);
                
                // Apply greyscale recoloring to emission only where the emission texture has value
                // This preserves transparency of the emission map
                float3 emission = emissionMask * _EmissionColor.rgb * _EmissionStrength;
                
                float3 bitangent = normalize(cross(normalWS, input.tangentWS.xyz)) * input.tangentWS.w;
                float3x3 TBN = float3x3(normalize(input.tangentWS.xyz), bitangent, normalWS);
                float3 finalNormal = normalize(mul(normalMap, TBN));
                
                InputData inputData = (InputData)0;
                inputData.positionWS = positionWS;
                inputData.normalWS = finalNormal;
                inputData.viewDirectionWS = normalize(GetWorldSpaceViewDir(positionWS));
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
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _FirstOutlineColor;
                float _FirstOutlineWidth;
                float4 _SecondOutlineColor;
                float _SecondOutlineWidth;
                float _Angle;
                // Other properties still needed
                float _Blend;
                float _Smoothness;
                float _Metallic;
                float _AmbientOcclusion;
                float _NormalStrength;
                float _EmissionStrength;
                float _HeightScale;
                float4 _EmissionColor;
                float4 _BaseColor;
                float4 _TilingXZ;
                float4 _TilingY;
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
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                return _FirstOutlineColor;
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
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _FirstOutlineColor;
                float _FirstOutlineWidth;
                float4 _SecondOutlineColor;
                float _SecondOutlineWidth;
                float _Angle;
                // Other properties still needed
                float _Blend;
                float _Smoothness;
                float _Metallic;
                float _AmbientOcclusion;
                float _NormalStrength;
                float _EmissionStrength;
                float _HeightScale;
                float4 _EmissionColor;
                float4 _BaseColor;
                float4 _TilingXZ;
                float4 _TilingY;
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
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                return _SecondOutlineColor;
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
                float _Blend;
                float _Smoothness;
                float _Metallic;
                float _AmbientOcclusion;
                float _NormalStrength;
                float _EmissionStrength;
                float _HeightScale;
                float4 _EmissionColor;
                float4 _BaseColor;
                float4 _TilingXZ;
                float4 _TilingY;
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