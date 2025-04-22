Shader "Horus/UnlitVertexAnimationNoLoop"
{
    Properties
    {
        [_BaseMap] _BaseMap("Base Map (RGB)", 2D) = "white" {}
        _VAT("VAT", 2D) = "white" {}
        _FrameRange("Frame Range", Vector) = (0, 0, 0, 0)
        _BoundingBoxMin("Bounding Box Min", Vector) = (0, 0, 0, 0)
        _BoundingBoxMax("Bounding Box Max", Vector) = (1, 1, 1, 1)
        _TimingData("Timing Data", Vector) = (0, 0, 0, 0)
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
            #pragma instancing_options procedural:setup
            #pragma target 3.0

            #include "UnityCG.cginc"

            sampler2D _BaseMap;
            sampler2D _VAT;

            UNITY_INSTANCING_BUFFER_START(Pros)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BoundingBoxMin)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BoundingBoxMax)
                UNITY_DEFINE_INSTANCED_PROP(float4, _TimingData)
                UNITY_DEFINE_INSTANCED_PROP(float4, _FrameRange)
            UNITY_INSTANCING_BUFFER_END(Pros)

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

            float GetFrame(float startTime, float duration, float frameRate, float currentTime, float2 frameRange)
            {
                float time = currentTime - startTime;
                float total_frame = floor(duration * frameRate);
                float frame = total_frame * 1.0f / frameRate;
                return clamp(time / frame + frameRange.x , frameRange.x, frameRange.y-1);
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);

                float4 boundingBoxMin = UNITY_ACCESS_INSTANCED_PROP(Pros, _BoundingBoxMin); // Fixed Props name
                float4 boundingBoxMax = UNITY_ACCESS_INSTANCED_PROP(Pros, _BoundingBoxMax); // Fixed Props name
                float4 timingData = UNITY_ACCESS_INSTANCED_PROP(Pros, _TimingData); // Fixed Props name

                v.uv2.x = v.uv3.x;
                v.uv2.y = GetFrame(timingData.x, timingData.y, timingData.z, _Time.y, UNITY_ACCESS_INSTANCED_PROP(Pros, _FrameRange).xy); // Fixed Props name
                float3 pos = tex2Dlod(_VAT, float4(v.uv2, 0, 0)).xyz;
                float3 boundingRange = boundingBoxMax.xyz - boundingBoxMin.xyz;
                pos = boundingRange * pos;
                pos = pos + boundingBoxMin;

                o.vertex = UnityObjectToClipPos(pos);
                o.uv = v.uv; // Added proper texture coordinate transformation
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