using System.Collections;
using UnityEngine;

public class ShaderPrewarmer : MonoBehaviour
{
    [SerializeField] private ShaderVariantCollection[] _shaderVariantCollectionsPreWarm;

    public static ShaderPrewarmer Instance { get; private set; }
    private       bool            preWarmDone;

    private void Start()
    {
        Instance = this;
        PrewarmShader();
    }

    private void PrewarmShader()
    {
        preWarmDone = false;
        StartCoroutine(PrewarmShaderCoroutine());
    }

    private IEnumerator PrewarmShaderCoroutine()
    {
        foreach (var shaderVariantCollection in _shaderVariantCollectionsPreWarm)
        {
            if (!shaderVariantCollection)
            {
                continue;
            }

            var countVariants = shaderVariantCollection.variantCount;
            try
            {
                shaderVariantCollection.WarmUpProgressively(countVariants);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Shader Compile Error, báo Phan Mạnh: " + shaderVariantCollection.name + " " + e);
            }

            while (!shaderVariantCollection.isWarmedUp)
            {
                yield return new WaitForEndOfFrame();
            }
        }

        preWarmDone = true;
    }
}