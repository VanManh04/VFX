#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using NaughtyAttributes;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

[Serializable]
public class VertexCache : AnimationCache
{
    [Expandable] public VertexCacheData VertexCacheData;

    public override ScriptableObject cacheData { get; protected set; }

    public override void Init()
    {
        SetCacheData<VertexCacheData, VertexUpdate>(VertexCacheData);

        VertexCacheData = cacheData as VertexCacheData;
    }

    public override void Bake()
    {
        if (VertexCacheData == null) return;
        BakeVertex();
    }

    public override void Clear()
    {
    }

    #region VertexBake

    [SerializeField] private SkinnedMeshRenderer _skinMesh;
    private Mesh _mesh;


    [Button]

    [UsedImplicitly]
    private void BakeVertex()
    {
        var getAllStateNames = Animator.GetAllStateNames();

        if (getAllStateNames == null || getAllStateNames.Count == 0)
            return;

        VertexCacheData?.StateInfors?.Clear();

        _mesh = new Mesh();

        foreach (var stateName in getAllStateNames)
        {
            RecordAnimVertex(Animator.GetAnimationClip(stateName), stateName);
        }

        // VertexCacheData.CalculateBoundingBox();
        if (VertexCacheData != null)
        {
            VertexCacheData.BotName = "/" + Animator.gameObject.name + ".byte";
            VertexCacheData.SaveTexture();

            _mesh = null;
            EditorUtility.SetDirty(VertexCacheData);
        }

        AssetDatabase.SaveAssets();
    }

    private void RecordAnimVertex(AnimationClip clip, string stateName)
    {
        var animationInfo = new VAT_Utilities.AnimationInfo
        {
            clipLength = clip.length,
            frameRate = clip.frameRate,
        };

        var meshInfo = new VertexCacheData.StateInfo
        {
            stateName = stateName,
            animationInfo = animationInfo,
            frameInfos = new List<VertexCacheData.FrameInfo>()
        };


        for (float i = 0; i < clip.length; i += 1.0f / clip.frameRate)
        {
            Animator.Play(stateName, 0, i / clip.length);
            Animator.Update(0);
            AddFrameInfo(meshInfo.frameInfos);
        }

        Animator.Play(stateName, 0, 0);
        Animator.Update(0);

        VertexCacheData.StateInfors.Add(meshInfo);
    }

    private void AddFrameInfo(List<VertexCacheData.FrameInfo> frameInfos)
    {
        if (_skinMesh == null) return;
        _skinMesh.BakeMesh(_mesh);
        var vertices = _mesh.vertices;
        var frameInfoVertices = new Vector3[vertices.Length];
        Array.Copy(vertices, frameInfoVertices, vertices.Length);
        var frameInfo = new VertexCacheData.FrameInfo { vertices = frameInfoVertices };
        frameInfos.Add(frameInfo);
    }

    #endregion
}
#endif