Shader "QuantumSprites"
{
    Properties{
        _Atlas ("Atlas", 2D) = "white" {}
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
            uniform StructuredBuffer<int> Indices;
            uniform float4 _Color;
            Texture2D Atlas;
            SamplerState sampler_Atlas;
            struct UVRect
            {
                float2 offset;
                float2 size;
            };
            uniform StructuredBuffer<UVRect> UVRects;

            struct Input
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Output
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                uint instance_id : SV_InstanceID ;
            };
            Output vert(Input v, const uint instance_id : SV_InstanceID)
            {
                Output o;
                uint ID = instance_id;
                float3 vertex = v.vertex.xyz * Scales[ID];
                const float4 pos = mul(Matrixes[ID], float4(vertex, v.vertex.w));
                o.vertex = mul(UNITY_MATRIX_VP, pos);
                o.instance_id = ID;
                o.uv = v.uv;
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
                //UVRect rect = UVRects[i.instance_id];
                //float2 uv = rect.offset + i.uv * rect.size;
                //return Atlas.Sample(sampler_Atlas, uv) * color * _Color;
                return color * _Color;
            }
            ENDHLSL
        }
    }
}