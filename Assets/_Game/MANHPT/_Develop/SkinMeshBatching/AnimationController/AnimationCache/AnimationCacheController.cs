
#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using NaughtyAttributes;
using UnityEditor;

public class AnimationCacheController : MonoBehaviour
{
    [Header("REQUIRED")] public AnimationController AnimationController;
    [SerializeField] private Transform _updateHolder;
    public BaseAnimationUpdate[] _animationUpdates;
    [SerializeField] private Animator Animator;
    public StateInforCache StateInforCache = new StateInforCache();

    [Space(20)] [Header("CACHES")] [SerializeField]
    private AnimationCacheType cacheData;

    [ShowIf(nameof(cacheData), AnimationCacheType.Vertex)]
    public VertexCache VertexCache = new VertexCache();
    


    public List<AnimationCache> CacheList = new();

    private AnimationCache _currentCache;

    private void DropDownChange()
    {
        if (CacheList == null || CacheList.Count == 0) return;

        _currentCache = CacheList.FirstOrDefault(x => x.animationCacheType == cacheData);
    }

    public void Require()
    {
        FindRequiredComponents();
        FindUpdateHolder();

        StateInforCache?.Init(Animator);
        CacheList.Clear();
        VertexCache.animationCacheType = AnimationCacheType.Vertex;
        CacheList.Add(VertexCache);

        foreach (var cache in CacheList)
        {
            cache.Animator = Animator;
            cache.UpdateHolder = _updateHolder;
            cache?.Init();
        }

        _animationUpdates = _updateHolder.GetComponents<BaseAnimationUpdate>();
        AnimationController.StateInforsData = StateInforCache?.StateInforsData;
        AnimationController.AnimationUpdates = _animationUpdates.Where(e =>e  != null).ToArray();


        AssetDatabase.SaveAssets();
    }


    [Button]
    private void BakeAll()
    {
        if (Application.isPlaying) return;
        foreach (var cache in CacheList)
        {
            cache?.Bake();
        }
    }

    [Button]
    private void ClearAll()
    {
        if (Application.isPlaying) return;

        foreach (var cache in CacheList)
        {
            cache?.Clear();
        }
    }

    private void FindRequiredComponents()
    {
        if (!TryGetComponent(out Animator))
        {
            Animator = gameObject.AddComponent<Animator>();
        }
    }

    private void FindUpdateHolder()
    {
        if (_updateHolder == null)
        {
            var updater = transform.Find("UpdateHolder");

            if (updater)
            {
                _updateHolder = updater;
                return;
            }

            _updateHolder = new GameObject("UpdateHolder").transform;
            _updateHolder.SetParent(transform);
            _updateHolder.localPosition = Vector3.zero;
            _updateHolder.transform.SetSiblingIndex(0);
        }
    }
}

#endif