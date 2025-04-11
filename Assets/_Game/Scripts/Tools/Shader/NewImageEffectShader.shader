Shader "Custom/WhiteShine"
{
    Properties
    {
        _Progress ("Shine Progress", Range(0,1)) = 0.0
        _Width ("Shine Width", Range(0.01, 1)) = 0.2
        _Brightness ("Shine Brightness", Range(0, 5)) = 1.0
        _ShineColor ("Shine Color", Color) = (1,1,1,1)
        _Glossiness ("Shine Gloss", Range(1, 50)) = 10.0 
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Progress;
            float _Width;
            float _Brightness;
            float _Glossiness;
            fixed4 _ShineColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // duong cheo top-right  bottom-left
                float diag = (uv.x + uv.y) * 0.5;

                // vi tri vet sang
                float dist = abs(diag - _Progress);

                // cat vet sang
                if (diag - _Progress > 0.0)
                    discard;

                // Vet sang mem
                float shine = smoothstep(_Width, 0.0, dist);

                // Highlight bong o trung tam
                float gloss = pow(saturate(1.0 - dist / _Width), _Glossiness);

                // ket hop shine va gloss
                float finalShine = shine + gloss;

                // Fade o vien vet sang
                float fadeEdge = smoothstep(0.0, _Width * 1.5, _Progress) * smoothstep(1.0, 1.0 - _Width * 1.5, _Progress);

                float alpha = finalShine * _Brightness * fadeEdge;

                return fixed4(_ShineColor.rgb, alpha * _ShineColor.a);
            }

            ENDCG
        }
    }
}
