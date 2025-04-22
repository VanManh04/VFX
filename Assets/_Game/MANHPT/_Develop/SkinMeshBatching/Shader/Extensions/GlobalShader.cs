using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "GlobalShader", menuName = "ScriptableObjects/Singletons/GlobalShader")]
public class GlobalShader : ScriptableObject
{
    public Shader SpriteRenderShader;
    public Shader SimpleModeUIShader;
    public Shader Nice_SliceModeUIShader;
    public Shader TileModeUIShader;
    public Shader FillModeUIShader;
    public Shader BorderModeUIShader;

    public Shader UnlitVertexCrossfade;
    public Shader UnlitVertexAnimation;
    
    public Material SpriteRenderMaterial;
    
    private static GlobalShader _instance;
    private static bool         _hasInstance = false;

    public static GlobalShader Instance
    {
        get
        {
            if (!_hasInstance)
            {
                _instance    = Resources.Load<GlobalShader>("ScriptableObjects/GlobalShader");
                _hasInstance = true;
            }

            return _instance;
        }
    }
}