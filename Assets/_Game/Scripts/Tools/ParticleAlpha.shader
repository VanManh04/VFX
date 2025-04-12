Shader "Horus/Transparent/ParticleAlpha"
{
    Properties
    {
        [Toggle]_UseLerpColor("Use Lerp Color", Range(0, 1)) = 0
        [HDR]_Color1("BrightColor", Color) = (1, 1, 1, 1)
        _Color2("DarkColor", Color) = (1, 1, 1, 1)
        [Header(Base)][HDR]_TintColor("Tint Color", Color) = (0.5, 0.5, 0.5, 0.5)
        _MainTex("Particle Texture", 2D) = "white" {}
        _Boost ("Boost", Range(0, 10)) = 2
        _Clip("Clip", Range(0,1)) = 0.01
        _Pull("Pull", float) = 0
        [Enum(UnityEngine.Rendering.CullMode)]_Cull ("Cull", Range(0, 1)) = 0
        [Toggle]_EnableFog ("Fog", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }

            // Transparency settings
            Blend SrcAlpha OneMinusSrcAlpha
            Cull [_Cull]
            ZWrite Off
            Lighting Off
            Fog { Mode Off }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            struct appdata_t
            {
                float4 position : POSITION;
                float2 texcoord : TEXCOORD0;
                half4 color : COLOR;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                half4 color : COLOR;
                float fogFactor : TEXCOORD1;
            };

            // Shader properties
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _TintColor;
            float4 _Color1;
            float4 _Color2;
            float _Boost;
            float _Clip;
            float _Pull;
            sampler2D _MainTex;

            // Fog calculation (simplified)
            float ComputeFogFactor(float depth)
            {
                return 1.0; // Modify this for custom fog calculation if needed
            }

            // Vertex shader
            v2f vert(appdata_t v)
            {
                v2f o;
                float3 cameraPosition = _WorldSpaceCameraPos.xyz;
                float3 cameraDirection = normalize(cameraPosition - v.position.xyz);

                float3 worldPos = mul(unity_ObjectToWorld, v.position).xyz;
                worldPos.xyz += cameraDirection * _Pull;

                o.position = UnityObjectToClipPos(float4(worldPos, 1.0));

                // Custom UV transformation
                o.texcoord = v.texcoord * _MainTex_ST.xy + _MainTex_ST.zw;

                o.color = v.color;

                // Calculate fog factor if enabled
                o.fogFactor = ComputeFogFactor(o.position.z);

                return o;
            }

            // Fragment shader
            half4 frag(v2f i) : SV_Target
            {
                // Sample texture
                float4 mainTexture = tex2D(_MainTex, i.texcoord);

                // Lerp color based on texture red channel
                float4 combineColor;
                #if _USELERPCOLOR_ON
                    combineColor = lerp(_Color2, _Color1, mainTexture.r);
                #else
                    combineColor = _TintColor;
                #endif

                // Apply boost, tint color, and texture
                float4 col = _Boost * i.color * combineColor * mainTexture;
                clip(col.a - _Clip); // Clip based on alpha

                // Apply fog if enabled
                col.xyz = col.xyz * i.fogFactor;

                return col;
            }

            ENDCG
        }
    }

    Fallback "Diffuse"
}
