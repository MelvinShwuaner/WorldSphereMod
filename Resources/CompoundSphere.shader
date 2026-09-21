Shader "CompoundSphere"
{
    Properties{
        TextureArray("TextureArray", 2DArray) = "" {}
        _Color("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Pass
        {
            Tags
            {
                "RenderType"="Opaque"
                "RenderPipeline" = "UniversalRenderPipeline"
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma require 2darray

            #include "UnityCG.cginc"

            uniform StructuredBuffer<float4x4> Matrixes;
            uniform StructuredBuffer<float3> Scales;
            uniform StructuredBuffer<uint> Colors;
            uniform StructuredBuffer<uint> AddedColors;
            uniform StructuredBuffer<float> Textures;
            uniform StructuredBuffer<float> Waves;
            uniform float4 _Color;
            uniform uint Col;
            uniform float WavesEnabled;
            uint Row;
            uniform float ShouldRenderTextures;
            uniform float WaveFrequency;
            uniform float WaveAmplitude;
            uniform float WaveSpeed;
            uniform float Time;
            struct Input
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Output
            {
                float4 vertex : SV_POSITION;
                float3 uv : TEXCOORD0;
                uint instance_id : SV_InstanceID ;
            };
            UNITY_DECLARE_TEX2DARRAY(TextureArray);
            float WaveHeight(float2 worldPos)
            {
                float h = 0;
                h += sin(worldPos.x * WaveFrequency       + Time * WaveSpeed) * WaveAmplitude;
                h += sin(worldPos.y * WaveFrequency * 1.3 + Time * WaveSpeed * 0.7) * WaveAmplitude * 0.5;
                h += sin((worldPos.x + worldPos.y) * WaveFrequency * 0.7 + Time * WaveSpeed * 1.3) * WaveAmplitude * 0.25;
                return h;
            }
            Output vert(Input v, const uint instance_id : SV_InstanceID)
            {
                Output o;
                uint ID = instance_id + Row + Col;
                float3 vertex = v.vertex.xyz * Scales[ID];
                if(WavesEnabled && Waves[ID] == 1){
                   uint y = instance_id + Col;
                   uint x = Row;
                   vertex.z *= WaveHeight(float2(x,y));
                }
                const float4 pos = mul(Matrixes[ID], float4(vertex, v.vertex.w));
                o.vertex = mul(UNITY_MATRIX_VP, pos);
                o.instance_id = ID;
                o.uv = float3(v.uv, Textures[ID]);
                return o;
            }
            float3 GetColor(uint packed)
            {
               float r = (packed & 0xFF) / 255.0;
               float g = ((packed >> 8) & 0xFF) / 255.0;
               float b = ((packed >> 16) & 0xFF) / 255.0;

               return float3(r, g, b);
            }
            half4 frag(Output i) : SV_Target
            {
                float4 color = float4(GetColor(Colors[i.instance_id]), 1);
                float4 addedcolor = float4(GetColor(AddedColors[i.instance_id]), 1);
                float4 finalColor;
                if(ShouldRenderTextures == 1){
                    finalColor = UNITY_SAMPLE_TEX2DARRAY(TextureArray, i.uv) + addedcolor;
                }
                else{
                    finalColor = color + addedcolor;
                }
                return  finalColor * _Color;
            }
            ENDHLSL
        }
    }
}