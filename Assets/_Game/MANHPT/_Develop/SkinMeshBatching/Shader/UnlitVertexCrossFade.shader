Shader "Horus/UnlitVertexCrossFade"
{
    Properties
    {
        [_BaseMap]_BaseMap("Base Map (RGB)", 2D) = "white" {}
        _VAT ("VAT ", 2D) = "white"{}
        _PreviousBoundingBoxMin("Pre Bounding Box Min", Vector) = (0, 0, 0, 0)
        _PreviousBoundingBoxMax("Pre Bounding Box Max", Vector) = (0, 0, 0, 0)
        _BoundingBoxMin("Bounding Box Min", Vector) = (0, 0, 0, 0)
        _BoundingBoxMax("Bounding Box Max", Vector) = (0, 0, 0, 0)
        _LerpTiming("LerpTiming", FLoat) = 0
        _previousFrame("Previous Frame", Float) = 0
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

            sampler2D _BaseMap;
            sampler2D _VAT;

            UNITY_INSTANCING_BUFFER_START(Pros)
                UNITY_DEFINE_INSTANCED_PROP(float4, _PreviousBoundingBoxMin)
                UNITY_DEFINE_INSTANCED_PROP(float4, _PreviousBoundingBoxMax)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BoundingBoxMin)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BoundingBoxMax)
                UNITY_DEFINE_INSTANCED_PROP(float, _LerpTiming)
                UNITY_DEFINE_INSTANCED_PROP(float, _previousFrame)
                UNITY_DEFINE_INSTANCED_PROP(float, _Frame)
            UNITY_INSTANCING_BUFFER_END(Pros)

            
            
            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);

                float4 previousBoundingBoxMin = UNITY_ACCESS_INSTANCED_PROP(Pros, _PreviousBoundingBoxMin);
                float4 previousBoundingBoxMax = UNITY_ACCESS_INSTANCED_PROP(Pros, _PreviousBoundingBoxMax);
                float4 boundingBoxMin = UNITY_ACCESS_INSTANCED_PROP(Pros, _BoundingBoxMin);
                float4 boundingBoxMax = UNITY_ACCESS_INSTANCED_PROP(Pros, _BoundingBoxMax);

                float2 VAT_UV1 = v.uv3;
                VAT_UV1.y = UNITY_ACCESS_INSTANCED_PROP(Pros, _previousFrame);
                float2 VAT_UV2 = v.uv3;
                VAT_UV2.y = UNITY_ACCESS_INSTANCED_PROP(Pros, _Frame);
                
                float3 pos = tex2Dlod(_VAT, float4(VAT_UV1, 0, 0)).xyz;
                float3 boundingRange = previousBoundingBoxMax.xyz - previousBoundingBoxMin.xyz;
                pos = boundingRange * pos;
                pos += previousBoundingBoxMin;

                float3 pos1 = tex2Dlod(_VAT, float4(VAT_UV2, 0, 0)).xyz;
                float3 boundingRange1 = boundingBoxMax.xyz - boundingBoxMin.xyz;
                pos1 = boundingRange1 * pos1;
                pos1 += boundingBoxMin;
                float3 resultPos = lerp(pos, pos1, UNITY_ACCESS_INSTANCED_PROP(Pros, _LerpTiming));
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