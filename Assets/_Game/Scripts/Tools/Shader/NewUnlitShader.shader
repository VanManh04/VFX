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
            Blend SrcAlpha OneMinusSrcAlpha // Ch? ?? hòa tr?n alpha
            ZWrite Off // T?t ghi vào depth buffer
            ZTest Always // Ki?m tra ?? sâu

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
            float _Slider; // V? trí v?t sáng
            float _LightWidth; // ?? r?ng v?t sáng
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

                // V? trí và tính toán v?t sáng
                float lightPos = _Slider; // L?y giá tr? slider cho v? trí v?t sáng
                float distanceFromLight = abs(i.uv.x - (1.0 - i.uv.y) - lightPos); // Kho?ng cách t? ?i?m trên ???ng chéo

                // Áp d?ng ?? sáng cho v?t
                float lightIntensity = smoothstep(_LightWidth, 0.0, distanceFromLight); // Áp d?ng smoothstep cho ?? sáng

                // Hi?n th? v?t sáng b?ng màu tr?ng
                if (lightIntensity > 0.0)
                {
                    col.rgb = float3(1.0, 1.0, 1.0); // Màu tr?ng cho v?t sáng
                }

                // N?u alpha d??i ng??ng, lo?i b? pixel (trong su?t)
                if (col.a < _Threshold)
                {
                    discard;
                }

                UNITY_APPLY_FOG(i.fogCoord, col); // Áp d?ng fog n?u có

                return col;
            }
            ENDCG
        }
    }

    Fallback "Diffuse"
}
