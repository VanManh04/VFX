Shader "Horus/UnlitVertexBlendTree"
{
    Properties
    {
        [_MainTex]_BaseMap("Base Map (RGB)", 2D) = "white" {}
        _VAT ("VAT ", 2D) = "white"{}
        _BoundingBoxMin("Bounding Box Min", Vector) = (0, 0, 0, 0)
        _BoundingBoxMax("Bounding Box Max", Vector) = (0, 0, 0, 0)
        _TimingData("Timing Data", Vector) = (0, 0, 0, 0)
        _PreviousFrameRange("_PreviousFrameRange", Vector) = (0, 0, 0, 0)
        _BoundingBoxMin_1("Bounding Box Min 1", Vector) = (0, 0, 0, 0)
        _BoundingBoxMax_1("Bounding Box Max 1", Vector) = (0, 0, 0, 0)
        _TimingData_1("Timing Data 1", Vector) = (0, 0, 0, 0)
        _FrameRange("_FrameRange", Vector) = (0, 0, 0, 0)
        _BlendFactor("Blend Factor", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float2 uv3 : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _BaseMap;
            sampler2D _VAT;

            UNITY_INSTANCING_BUFFER_START(Pros)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BoundingBoxMin)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BoundingBoxMax)
                UNITY_DEFINE_INSTANCED_PROP(float4, _TimingData)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BoundingBoxMin_1)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BoundingBoxMax_1)
                UNITY_DEFINE_INSTANCED_PROP(float4, _TimingData_1)
                UNITY_DEFINE_INSTANCED_PROP(float, _BlendFactor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _FrameRange)
                UNITY_DEFINE_INSTANCED_PROP(float4, _PreviousFrameRange)
            UNITY_INSTANCING_BUFFER_END(Pros)


            float GetFrame(float startTime, float duration, float frameRate, float currentTime, float2 frameRange)
            {
                float time = currentTime - startTime;
                float total_frame = floor(duration * frameRate);
                float frame = total_frame * 1.0f / frameRate;

                return frac(time / frame + frameRange.x);
            }

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                
                float4 boundingBoxMin = UNITY_ACCESS_INSTANCED_PROP(Pros, _BoundingBoxMin);
                float4 boundingBoxMax = UNITY_ACCESS_INSTANCED_PROP(Pros, _BoundingBoxMax);
                float4 timingData = UNITY_ACCESS_INSTANCED_PROP(Pros, _TimingData);
                float4 boundingBoxMin1 = UNITY_ACCESS_INSTANCED_PROP(Pros, _BoundingBoxMin_1);
                float4 boundingBoxMax1 = UNITY_ACCESS_INSTANCED_PROP(Pros, _BoundingBoxMax_1);
                float4 timingData1 = UNITY_ACCESS_INSTANCED_PROP(Pros, _TimingData_1);
                float blendFactor = UNITY_ACCESS_INSTANCED_PROP(Pros, _BlendFactor);
                
                v.uv2.x = v.uv3.x;
                v.uv2.y = GetFrame(timingData.x, timingData.y, timingData.z, _Time.y, UNITY_ACCESS_INSTANCED_PROP(Pros, _PreviousFrameRange).xy);
                float3 pos = tex2Dlod(_VAT, float4(v.uv2, 0, 0)).xyz;
                float3 boundingRange = boundingBoxMax.xyz - boundingBoxMin.xyz;
                pos = boundingRange * pos;
                pos += boundingBoxMin;
                v.uv2.y = GetFrame(timingData1.x, timingData1.y, timingData1.z, _Time.y, UNITY_ACCESS_INSTANCED_PROP(Pros, _FrameRange).xy);
                float3 pos1 = tex2Dlod(_VAT, float4(v.uv2, 0, 0)).xyz;
                float3 boundingRange1 = boundingBoxMax1.xyz - boundingBoxMin1.xyz;
                pos1 = boundingRange1 * pos1;
                pos1 += boundingBoxMin1;
                float3 resultPos = lerp(pos, pos1, blendFactor);
                o.vertex = UnityObjectToClipPos(resultPos);
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 col = tex2D(_BaseMap, i.uv);
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}