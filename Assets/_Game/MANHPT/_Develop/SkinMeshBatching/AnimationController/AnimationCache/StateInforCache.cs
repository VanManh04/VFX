#if UNITY_EDITOR
using System;
using System.IO;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

[Serializable]
public class StateInforCache
{
    [Expandable] public StateInforsData StateInforsData;

    private Animator _animator;

    public void Init(Animator animator)
    {
        _animator = animator;

        var getAllStateNames = animator.GetAllStateNames();

        if (getAllStateNames == null || getAllStateNames.Count == 0)
            return;

        StateInforsData = GetScriptableObjectData(StateInforsData);

        StateInforsData?.StateInfors?.Clear();

        foreach (var stateName in getAllStateNames)
        {
            var clip = animator.GetAnimationClip(stateName);

            if (!clip)
                continue;

            var newState = new StateInforsData.StateInfo
            {
                stateName = stateName,
                animationInfo = new VAT_Utilities.AnimationInfo
                {
                    frameRate = clip.frameRate,
                    clipLength = clip.length
                },
            };

            StateInforsData?.StateInfors?.Add(newState);
        }

        if (StateInforsData)
        {
            EditorUtility.SetDirty(StateInforsData);
            AssetDatabase.SaveAssets();
        }
    }

    private T GetScriptableObjectData<T>(T scriptableData) where T : ScriptableObject
    {
        if (scriptableData) return scriptableData;

        // Đường dẫn lưu file
        string path = "Assets/_Game/MANHPT/_Develop/SkinMeshBatching/AnimationController/ScriptableObjects/";

        // Kiểm tra và tạo thư mục nếu cần
        string directory = Path.GetDirectoryName(path);

        if (!Directory.Exists(directory))
        {
            if (directory != null) Directory.CreateDirectory(directory);
        }

        string assetPath = "Assets/_Game/MANHPT/_Develop/SkinMeshBatching/AnimationController/ScriptableObjects/" +
                           _animator.gameObject.name + "_" + typeof(T).Name + ".asset";

        if (!File.Exists(assetPath))
        {
            // Tạo ScriptableObject
            T asset = ScriptableObject.CreateInstance<T>();
            // Lưu asset vào project
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("ScriptableObject đã được tạo tại: " + path);
            scriptableData = asset;
        }
        else
        {
            scriptableData = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }

        return scriptableData;
    }
}
#endif