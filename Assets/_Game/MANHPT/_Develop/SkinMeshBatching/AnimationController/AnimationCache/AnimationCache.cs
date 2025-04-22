#if UNITY_EDITOR
using System;
using System.IO;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public abstract class AnimationCache
{
    [AllowNesting] [ReadOnly] public BaseAnimationUpdate AnimationUpdate;
    [HideInInspector] public AnimationCacheType animationCacheType = AnimationCacheType.RootMotion;
    [HideInInspector] public Animator Animator;
    [HideInInspector] public Transform UpdateHolder;
    [HideInInspector] public abstract ScriptableObject cacheData { get; protected set; }
    public abstract void Init();
    public abstract void Bake();
    public abstract void Clear();

    public virtual void SetCacheData<T, K>(T data)
        where T : ScriptableObject
        where K : BaseAnimationUpdate
    {
        if (data) return;

        var scriptableData = GetScriptableObjectData(data);

        if (!scriptableData) return;

        GetAnimationUpdate<K>();

        if (AnimationUpdate is ISetCacheData setCacheData)
        {
            setCacheData.SetData(scriptableData);
        }
    }


    protected virtual void GetAnimationUpdate<T>() where T : BaseAnimationUpdate
    {
        var animationUpdate = UpdateHolder.GetComponent<T>();

        if (!animationUpdate)
        {
            animationUpdate = UpdateHolder.gameObject.AddComponent<T>();
        }

        AnimationUpdate = animationUpdate;
    }


    public virtual T GetScriptableObjectData<T>(T scriptableData) where T : ScriptableObject
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
                           Animator.gameObject.name + "_" + typeof(T).Name + ".asset";

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

        cacheData = scriptableData;

        return scriptableData;
    }
}
#endif