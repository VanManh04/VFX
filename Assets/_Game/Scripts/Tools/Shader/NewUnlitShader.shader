Shader "Custom/DiagonalLightEffectWithSlider"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Slider ("Light Position", Range(0, 1)) = 0.5
        _LightWidth ("Light Width", Range(0.01, 0.5)) = 0.1
        _Threshold ("Alpha Threshold", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha // Ch? ?? h�a tr?n alpha
            ZWrite Off // T?t ghi v�o depth buffer
            ZTest Always // Ki?m tra ?? s�u

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _Slider; // V? tr� v?t s�ng
            float _LightWidth; // ?? r?ng v?t s�ng
            float _Threshold; // Ng??ng alpha

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex); // Bi?n ??i t?a ?? vertex
                o.uv = v.uv; // Chuy?n ??i uv
                UNITY_TRANSFER_FOG(o, o.vertex); // Chuy?n ti?p fog
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample texture
                fixed4 col = tex2D(_MainTex, i.uv);

                // V? tr� v� t�nh to�n v?t s�ng
                float lightPos = _Slider; // L?y gi� tr? slider cho v? tr� v?t s�ng
                float distanceFromLight = abs(i.uv.x - (1.0 - i.uv.y) - lightPos); // Kho?ng c�ch t? ?i?m tr�n ???ng ch�o

                // �p d?ng ?? s�ng cho v?t
                float lightIntensity = smoothstep(_LightWidth, 0.0, distanceFromLight); // �p d?ng smoothstep cho ?? s�ng

                // Hi?n th? v?t s�ng b?ng m�u tr?ng
                if (lightIntensity > 0.0)
                {
                    col.rgb = float3(1.0, 1.0, 1.0); // M�u tr?ng cho v?t s�ng
                }

                // N?u alpha d??i ng??ng, lo?i b? pixel (trong su?t)
                if (col.a < _Threshold)
                {
                    discard;
                }

                UNITY_APPLY_FOG(i.fogCoord, col); // �p d?ng fog n?u c�

                return col;
            }
            ENDCG
        }
    }

    Fallback "Diffuse"
}
