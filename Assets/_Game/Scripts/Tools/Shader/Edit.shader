Shader "Custom/CombinedRoundedCornersShine"
{
    Properties
    {
        // Rounded 
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        _WidthHeightRadius ("WidthHeightRadius", Vector) = (0, 0, 0, 0)
        _OuterUV ("Image Outer UV", Vector) = (0, 0, 1, 1)

        // Shine 
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

            float4 _WidthHeightRadius;
            float4 _OuterUV;
            sampler2D _MainTex;

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

            float CalcAlpha(float2 uv, float2 dimensions, float radius)
            {
                float2 dist = abs(uv * 2 - 1) - dimensions;
                float cornerDist = length(max(dist, 0)) - radius;
                return saturate(1.0 - cornerDist);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uvSample = i.uv;

                uvSample.x = (uvSample.x - _OuterUV.x) / (_OuterUV.z - _OuterUV.x);
                uvSample.y = (uvSample.y - _OuterUV.y) / (_OuterUV.w - _OuterUV.y);

                half4 color = tex2D(_MainTex, uvSample);

                float alphaRounded = CalcAlpha(uvSample, _WidthHeightRadius.xy, _WidthHeightRadius.z);

                float diag = (uvSample.x + uvSample.y) * 0.5;
                float dist = abs(diag - _Progress);

                if (diag - _Progress > 0.0)
                    discard;

                float shine = smoothstep(_Width, 0.0, dist);
                float gloss = pow(saturate(1.0 - dist / _Width), _Glossiness);
                float finalShine = shine + gloss;

                float fadeEdge = smoothstep(0.0, _Width * 1.5, _Progress) * smoothstep(1.0, 1.0 - _Width * 1.5, _Progress);
                float alphaShine = finalShine * _Brightness * fadeEdge;

                float finalAlpha = alphaRounded * alphaShine;

                return fixed4(_ShineColor.rgb, finalAlpha);
            }

            ENDCG
        }
    }
}
