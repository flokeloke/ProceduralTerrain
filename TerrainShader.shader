Shader "Custom/TerrainShaderURP"
{
    Properties
    {
        _textureScale("Texture Scale", Float) = 1.0 // Add the tiling scale property
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend One Zero
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #define MAX_TEXTURES 32

            float _textureScale; // The tiling scale property
            float _blendFactorMultiplier;

            float _MinTerrainHeight;
            float _MaxTerrainHeight;

            float _TerrainHeights[MAX_TEXTURES];
            Texture2DArray _terrainTextures;
            SamplerState sampler_terrainTextures;

            int _NumTextures;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.worldPos = mul(unity_ObjectToWorld, input.positionOS).xyz;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                float3 scaledWorldPos = input.worldPos / _textureScale;
                float worldPosY = input.worldPos.y;

                float heightValue = saturate((worldPosY - _MinTerrainHeight) / (_MaxTerrainHeight - _MinTerrainHeight));

                int layerIndex = -1;
                float blendFactor = 0.0;

                // Find the current layer and calculate the blend factor
                for (int i = 0; i < _NumTextures - 1; i++)
                {
                    if (heightValue >= _TerrainHeights[i] && heightValue <= _TerrainHeights[i + 1])
                    {
                        layerIndex = i;
                        // Calculate blend factor based on the height value within the layer range
                        blendFactor = saturate((heightValue - _TerrainHeights[i]) / (_TerrainHeights[i + 1] - _TerrainHeights[i]));
                        blendFactor = pow(blendFactor, _blendFactorMultiplier); // Apply the blend factor multiplier
                        break;
                    }
                }

                if (layerIndex == -1)
                {
                    layerIndex = _NumTextures - 1;
                }

                // Sample the current and next texture layers
                float2 scaledUV = input.worldPos.xz * _textureScale;
                half4 albedoCurrent = _terrainTextures.Sample(sampler_terrainTextures, float3(scaledUV.x, scaledUV.y, layerIndex));
                half4 albedoNext = _terrainTextures.Sample(sampler_terrainTextures, float3(scaledUV.x, scaledUV.y, layerIndex + 1));

                // Blend between the current and next texture layers
                half4 albedo = lerp(albedoCurrent, albedoNext, blendFactor);

                // Apply lighting
                Light mainLight = GetMainLight();
                half3 lightDir = normalize(mainLight.direction);
                half NdotL = saturate(dot(input.normalWS, lightDir));
                half3 diffuse = albedo.rgb * mainLight.color * NdotL;

                return half4(diffuse, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
