using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AnimMeshUVGenerator", menuName = "ScriptableObjects/VAT/AnimMeshUVGenerator", order = 1)]
public class AnimMeshUVGenerator : ScriptableObject
{
    private bool _isInit;
    public void GenerateUV(Mesh mesh)
    {
        if(_isInit) return;
        var vertexCount   = mesh.vertexCount;
        var vertexIndices = new Vector4[vertexCount];

        for (var i = 0; i < vertexCount; i++)
        {
            vertexIndices[i] = new Vector4((i + 0.5f) / vertexCount, 0, 0, 0);
        }

        mesh.SetUVs(2, vertexIndices); // UV2
        _isInit = true;
    }
    
    public void Dispose()
    {
        _isInit = false;
    }
}