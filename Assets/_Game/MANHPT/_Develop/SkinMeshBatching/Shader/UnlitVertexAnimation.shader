Shader "Horus/UnlitVertexAnimation"
{
    Properties
    {
        [_BaseMap]_BaseMap("Base Map (RGB)", 2D) = "white" {}
        _VAT("VAT", 2D) = "white" {}
        _BoundingBoxMin("Bounding Box Min", Vector) = (0, 0, 0, 0)
        _BoundingBoxMax("Bounding Box Max", Vector) = (1, 1, 1, 1)
        _TimingData("Timing Data", Vector) = (0, 0, 0, 0)
        _Loop("Loop", Float) = 0
        _Frame("Frame", Float) = 0

    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _BaseMap;
            sampler2D _VAT;

            UNITY_INSTANCING_BUFFER_START(Pros)
                UNITY_DEFINE_INSTANCED_PROP(vector, _BoundingBoxMin)
                UNITY_DEFINE_INSTANCED_PROP(vector, _BoundingBoxMax)
                UNITY_DEFINE_INSTANCED_PROP(float, _Loop)
                UNITY_DEFINE_INSTANCED_PROP(float, _Frame)
            UNITY_INSTANCING_BUFFER_END(Pros)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv3 : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);

                float4 boundingBoxMin = UNITY_ACCESS_INSTANCED_PROP(Pros, _BoundingBoxMin);
                float4 boundingBoxMax = UNITY_ACCESS_INSTANCED_PROP(Pros, _BoundingBoxMax);

                float2 VAT_UV = v.uv3;
                VAT_UV.y = UNITY_ACCESS_INSTANCED_PROP(Pros, _Frame);
                float3 pos = tex2Dlod(_VAT, float4(VAT_UV, 0, 0)).xyz;
                float3 boundingRange = boundingBoxMax.xyz - boundingBoxMin.xyz;
                pos = boundingRange * pos;
                pos = pos + boundingBoxMin;
                o.vertex = UnityObjectToClipPos(pos);
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