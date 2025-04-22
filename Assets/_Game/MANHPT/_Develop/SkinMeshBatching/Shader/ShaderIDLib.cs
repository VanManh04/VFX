using UnityEngine;

public static class ShaderIDLib
{
    // A
    public static readonly int Appearance = Shader.PropertyToID("_Appearance");

    // B
    public static readonly int BoundingBoxMin = Shader.PropertyToID("_BoundingBoxMin");
    public static readonly int BoundingBoxMax = Shader.PropertyToID("_BoundingBoxMax");
    public static readonly int Bias           = Shader.PropertyToID("_Bias");
    public static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    
    // C
    public static readonly int Color  = Shader.PropertyToID("_Color");
    public static readonly int Color1 = Shader.PropertyToID("_Color1");
    public static readonly int Color2 = Shader.PropertyToID("_Color2");
    public static readonly int Color3 = Shader.PropertyToID("_Color3");
    public static readonly int Color4 = Shader.PropertyToID("_Color4");

    public static readonly int Color5 = Shader.PropertyToID("_Color5");

    // D
    public static readonly int Decor           = Shader.PropertyToID("_Decor");
    public static readonly int DecorGlossiness = Shader.PropertyToID("_DecorGlossiness");
    public static readonly int DissolveAmount  = Shader.PropertyToID("_DissolveAmount");
    public static readonly int DecorMetallic   = Shader.PropertyToID("_DecorMetallic");

    // E
    public static readonly int Explosion   = Shader.PropertyToID("_Explosion");
    public static readonly int EffectSpeed = Shader.PropertyToID("_EffectSpeed");

    // F
    public static readonly int Fill       = Shader.PropertyToID("_Fill");
    public static readonly int Fill1      = Shader.PropertyToID("_Fill1");
    public static readonly int FrameRange = Shader.PropertyToID("_FrameRange");
    public static readonly int Frame    = Shader.PropertyToID("_Frame");

    public static readonly int FresnelContrast = Shader.PropertyToID("_FresnelContrast");

    // G
    public static readonly int Glossiness = Shader.PropertyToID("_Glossiness");

    public static readonly int Glow = Shader.PropertyToID("_Glow");

    // H
    // I
    public static readonly int ImpactArray = Shader.PropertyToID("_ImpactArray");
    public static readonly int IsDestroyed = Shader.PropertyToID("_IsDestroyed");

    public static readonly int IsWeakness = Shader.PropertyToID("_IsWeakness");

    // J
    // K
    // L
    public static readonly int Layer1   = Shader.PropertyToID("_Layer1");
    public static readonly int Layer2   = Shader.PropertyToID("_Layer2");
    public static readonly int Layer3   = Shader.PropertyToID("_Layer3");
    public static readonly int Layer4   = Shader.PropertyToID("_Layer4");
    public static readonly int Layer5   = Shader.PropertyToID("_Layer5");
    public static readonly int Lightmap = Shader.PropertyToID("_Lightmap");
    public static readonly int Loop     = Shader.PropertyToID("_Loop");
    public static readonly int LerpTiming = Shader.PropertyToID("_LerpTiming");

    public static readonly int LerpTimingData = Shader.PropertyToID("_LerpTimingData");
    
    public static readonly int LitCol = Shader.PropertyToID("_LitCol");

    // M
    public static readonly int MainColor   = Shader.PropertyToID("_MainColor");
    public static readonly int MainTex     = Shader.PropertyToID("_MainTex");
    public static readonly int MainTexST   = Shader.PropertyToID("_MainTex_ST");
    public static readonly int Mask        = Shader.PropertyToID("_Mask");
    public static readonly int Metallic    = Shader.PropertyToID("_Metallic");
    public static readonly int MulOrBlend  = Shader.PropertyToID("_MulOrBlend");
    public static readonly int MulStrength = Shader.PropertyToID("_MulStrength");

    public static readonly int Muzzle = Shader.PropertyToID("_Muzzle");

    // N
    public static readonly int Normal     = Shader.PropertyToID("_Normal");
    public static readonly int NormalPush = Shader.PropertyToID("_NormalPush");

    // O
    public static readonly int Opacity = Shader.PropertyToID("_Opacity");

    // P
    public static readonly int PreviousBoundingBoxMin = Shader.PropertyToID("_PreviousBoundingBoxMin");
    public static readonly int PreviousBoundingBoxMax = Shader.PropertyToID("_PreviousBoundingBoxMax");
    public static readonly int PreviousFrameRange = Shader.PropertyToID("_PreviousFrameRange");
    public static readonly int PreviousTimingData = Shader.PropertyToID("_PreviousTimingData");
    public static readonly int PreviousFrame = Shader.PropertyToID("_previousFrame");
    // Q
    // R
    public static readonly int Range1 = Shader.PropertyToID("_Range1");
    public static readonly int Range2 = Shader.PropertyToID("_Range2");
    public static readonly int Range3 = Shader.PropertyToID("_Range3");
    public static readonly int Range4 = Shader.PropertyToID("_Range4");

    public static readonly int Range5 = Shader.PropertyToID("_Range5");

    // S
    public static readonly int ScaleOffset = Shader.PropertyToID("_ScaleOffset");
    public static readonly int ShadowColor = Shader.PropertyToID("_ShadowColor");

    // T
    public static readonly  int Temperature      = Shader.PropertyToID("_Temperature");
    public static readonly  int TurnOnLock       = Shader.PropertyToID("_TurnOnLock");
    public static readonly  int TurnOnOutline    = Shader.PropertyToID("_TurnOnOutline");
    public static readonly  int TurnOnSilhouette = Shader.PropertyToID("_TurnOnSilhouette");
    public static readonly int TimingData       = Shader.PropertyToID("_TimingData");

    // U
    // V
    public static readonly int Vat = Shader.PropertyToID("_VAT");
    // W
    // X
    // Y
    // Z
}