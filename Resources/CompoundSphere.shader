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
            uniform StructuredBuffer<float3> AddedColors;
            uniform StructuredBuffer<float> Textures;
            uniform float4 _Color;
            uniform uint Col;
            uint Row;
            uniform float ShouldRenderTextures;

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
            Output vert(Input v, const uint instance_id : SV_InstanceID)
            {
                Output o;
                uint ID = instance_id + Row + Col;
                float3 vertex = v.vertex.xyz * Scales[ID];
                const float4 pos = mul(Matrixes[ID], float4(vertex, v.vertex.w));
                o.vertex = mul(UNITY_MATRIX_VP, pos);
                o.instance_id = ID;
                o.uv = float3(v.uv, Textures[ID]);
                return o;
            }
            float3 GetColor(uint instanceID)
            {
                uint packed = Colors[instanceID];

               float r = (packed & 0xFF) / 255.0;
               float g = ((packed >> 8) & 0xFF) / 255.0;
               float b = ((packed >> 16) & 0xFF) / 255.0;

               return float3(r, g, b);
            }
            half4 frag(Output i) : SV_Target
            {
                float4 color = float4(GetColor(i.instance_id), 1);
                float4 addedcolor = float4(AddedColors[i.instance_id], 1);
                float4 finalColor;
                if(ShouldRenderTextures == 1){
                    finalColor = UNITY_SAMPLE_TEX2DARRAY(TextureArray, i.uv) + addedcolor;
                }
                else if(ShouldRenderTextures == 2){
                    finalColor = (UNITY_SAMPLE_TEX2DARRAY(TextureArray, i.uv) * color) + addedcolor;
                }
                else if(ShouldRenderTextures == 3){
                    finalColor = (UNITY_SAMPLE_TEX2DARRAY(TextureArray, i.uv) + color) + addedcolor;
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