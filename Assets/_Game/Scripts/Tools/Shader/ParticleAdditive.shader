Shader "Horus/Transparent/ParticleAdditive"
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

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

    sampler2D _MainTex;
    sampler2D _DissolveTexture;
    CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        float4 _DissolveTexture_ST;
        float4 _MainTex_TexelSize;
        float4 _TintColor;
        float4 _Color1;
        float4 _Color2;
        float _Boost;
        float _Clip;
        float _Pull;
        float _Exposure;
        float _BlurStrength;
    CBUFFER_END

    struct appdata_t
    {
        float4 positionOS : POSITION;
        float4 texcoord : TEXCOORD0;
        half4 color : COLOR;
    };

    struct v2f
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord : TEXCOORD0;
        half4 color : COLOR;

        #if _ENABLEFOG_ON
        float fogFactor : TEXCOORD1;
        #endif
    };

    v2f vert(appdata_t v)
    {
        v2f o;
        float3 cameraPosition = TransformWorldToObject(_WorldSpaceCameraPos).xyz;
        float3 cameraDirection = normalize(cameraPosition - v.positionOS.xyz);

        float4 worldPos = float4(TransformObjectToWorld(v.positionOS.xyz), 0);
        worldPos.xyz += cameraDirection * _Pull;

        o.positionCS = TransformWorldToHClip(worldPos.xyz);

        o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
        o.color = v.color;
        #if _ENABLEFOG_ON
        o.fogFactor = ComputeFogFactor(o.positionCS.z);
        #endif
        return o;
    }

    half4 frag(v2f i) : SV_Target
    {
        float4 mainTexture = tex2D(_MainTex, i.texcoord);

        float4 combineColor;
        #if _USELERPCOLOR_ON
        combineColor = lerp(_Color2, _Color1 , mainTexture.r);
        #else
        combineColor = _TintColor;
        #endif

        float4 col = _Boost * i.color * combineColor * mainTexture;
        clip(mainTexture.r - _Clip);

        #if _ENABLEFOG_ON
        col.xyz = MixFog(col.xyz, i.fogFactor);
        #endif
        return col;
    }
    ENDHLSL

    SubShader
    {
        Tags
        {
            "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"
        }
        Blend SrcAlpha One
        Cull [_Cull]
        Zwrite off
        Lighting off

        Fog
        {
            Mode off
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_particles
            #pragma shader_feature _ENABLEFOG_ON
            #pragma multi_compile_fog
            #pragma shader_feature _USELERPCOLOR_ON
            ENDHLSL
        }
    }
}