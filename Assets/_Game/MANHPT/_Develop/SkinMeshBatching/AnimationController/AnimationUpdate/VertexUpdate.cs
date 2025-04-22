using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

public class VertexUpdate : BaseAnimationUpdate, ISetCacheData
{
    [ReadOnly] [SerializeField] private VertexCacheData.StateInfo _currentStateInfo;
    [ReadOnly] [SerializeField] private Vector4 _currentTimingData;
    [ReadOnly] [SerializeField] private Vector4 _previousTimingData;
    [ReadOnly] [SerializeField] private string _currentStateName = "Idle";
    [ReadOnly] [SerializeField] private float _currentFrameRate = 30;
    [ReadOnly] [SerializeField] private int _currentLOD = 2;
    [ReadOnly] [SerializeField] private MeshRenderer _meshRenderer;

    [SerializeField] private Texture _baseMap;
    [SerializeField] private GlobalShader _globalShader;
    [SerializeField] private AnimMeshUVGenerator _animMeshUVGenerator;
    // [SerializeField] private ColliderCacheData _colliderCacheData;


    [SerializeField] private Animator _animator;
    [SerializeField] private SkinnedMeshRenderer _skinMesh;
    [SerializeField] private GameObject _animationRoot;


    private Texture _VAT;

    private Mesh _mesh;
    private MeshFilter _meshFilter;
    private Shader _animationShader;
    private Shader _crossFadeShader;

    private Material _crossFadeMaterial;
    private Material _animationLoopMaterial;
    
    private MaterialPropertyBlock _materialPropertyBlock;

    private CancellationTokenSource _cancellationTokenSource;

    [Expandable] public VertexCacheData CacheData;

    private float _crossFadeDuration;
    private bool _isCrossFade;
    private bool _isAnimation;
    private bool _isLoop;
    private Vector2 _frameRange;
    private Vector2 _previousFrameRange;

    private void OnDestroy()
    {
        Dispose();
    }

    private void Play()
    {
        Play(_currentStateName);
    }

    public override void FrameUpdate(int currentFrame)
    {
        CurrentFrame = currentFrame;
        if (_isCrossFade)
        {
            _meshRenderer.GetPropertyBlock(_materialPropertyBlock);
            _materialPropertyBlock.SetFloat(ShaderIDLib.LerpTiming,
                LerpTiming(_currentTimingData.x, _crossFadeDuration));
            _materialPropertyBlock.SetFloat(ShaderIDLib.PreviousFrame,
                GetFrame(_previousTimingData.x, _previousTimingData.y, _previousTimingData.z, _previousFrameRange));
            _materialPropertyBlock.SetFloat(ShaderIDLib.Frame,
                GetFrame(_currentTimingData.x, _currentTimingData.y, _currentTimingData.z, _frameRange));
            _meshRenderer.SetPropertyBlock(_materialPropertyBlock);
        }

        if (_isAnimation)
        {
            _meshRenderer.GetPropertyBlock(_materialPropertyBlock);
            _materialPropertyBlock.SetFloat(ShaderIDLib.Frame,
                GetFrame(_currentTimingData.x, _currentTimingData.y, _currentTimingData.z, _frameRange));
            _meshRenderer.SetPropertyBlock(_materialPropertyBlock);
        }
    }

    public override void ChangeState(string stateName, float duration = 0, bool isLoop = false)
    {
        if (duration == 0)
        {
            Play(stateName, isLoop);
        }
        else
        {
            CrossFade(stateName, duration, isLoop);
        }
    }


    public void SetData(ScriptableObject cacheData)
    {
        CacheData = cacheData as VertexCacheData;
    }


    public void CrossFade(string stateName, float crossFadeDuration, bool loop = false)
    {
        CrossFadeAsync(stateName, crossFadeDuration, loop, _cancellationTokenSource);
    }

    public async void CrossFadeAsync(string stateName, float crossFadeDuration, bool loop,
        CancellationTokenSource cancellationTokenSource)
    {
        _isCrossFade = true;
        _isAnimation = false;
        _isLoop = true;
        
        _crossFadeDuration = crossFadeDuration;

        _meshRenderer.sharedMaterial = _crossFadeMaterial;
        _meshRenderer.GetPropertyBlock(_materialPropertyBlock);
        _previousTimingData = _currentTimingData;
        var previousStateName = string.IsNullOrWhiteSpace(_currentStateInfo.stateName) ? stateName : _currentStateInfo.stateName;
        CacheData.GetFrameRangeAndBoundingBox(previousStateName, out _previousFrameRange, out var previousBoundingBox);
        _currentStateInfo = CacheData.GetStateInfo(stateName);
        CacheData.GetFrameRangeAndBoundingBox(stateName, out _frameRange, out var boundingBox);
        _currentFrameRate = _currentStateInfo.animationInfo.frameRate;
        _currentTimingData = new Vector4(Time.time, _currentStateInfo.animationInfo.clipLength, _currentFrameRate, _currentLOD);
        _materialPropertyBlock.SetVector(ShaderIDLib.PreviousBoundingBoxMin, previousBoundingBox.min);
        _materialPropertyBlock.SetVector(ShaderIDLib.PreviousBoundingBoxMax, previousBoundingBox.max);
        _materialPropertyBlock.SetVector(ShaderIDLib.BoundingBoxMin, boundingBox.min);
        _materialPropertyBlock.SetVector(ShaderIDLib.BoundingBoxMax, boundingBox.max);


        _meshRenderer.SetPropertyBlock(_materialPropertyBlock);

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(crossFadeDuration),
                cancellationToken: cancellationTokenSource.Token);
        }
        catch (OperationCanceledException e)
        {
            return;
        }

        _isCrossFade = false;
        _isAnimation = true;
        _isLoop = loop;
        
        _meshRenderer.sharedMaterial = _animationLoopMaterial;
        _meshRenderer.GetPropertyBlock(_materialPropertyBlock);
        _materialPropertyBlock.SetFloat(ShaderIDLib.Loop, loop ? 1.0f : 0.0f);
        _materialPropertyBlock.SetVector(ShaderIDLib.BoundingBoxMin, boundingBox.min);
        _materialPropertyBlock.SetVector(ShaderIDLib.BoundingBoxMax, boundingBox.max);
        _meshRenderer.SetPropertyBlock(_materialPropertyBlock);
    }

    public void Play(string stateName, bool loop = false)
    {
        _isAnimation = true;
        _isLoop = loop;
        _meshRenderer.sharedMaterial = _animationLoopMaterial;
        _meshRenderer.GetPropertyBlock(_materialPropertyBlock);
        _currentStateInfo = CacheData.GetStateInfo(stateName);
        CacheData.GetFrameRangeAndBoundingBox(stateName, out _frameRange, out var boundingBox);
        _currentFrameRate = _currentStateInfo.animationInfo.frameRate;
        _currentTimingData = new Vector4(Time.time, _currentStateInfo.animationInfo.clipLength, _currentFrameRate,
            _currentLOD);

        _materialPropertyBlock.SetFloat(ShaderIDLib.Loop, loop ? 1.0f : 0.0f);
        _materialPropertyBlock.SetVector(ShaderIDLib.BoundingBoxMin, boundingBox.min);
        _materialPropertyBlock.SetVector(ShaderIDLib.BoundingBoxMax, boundingBox.max);

        _meshRenderer.SetPropertyBlock(_materialPropertyBlock);
    }


    private void CrossFade()
    {
        CrossFade(_currentStateName, _crossFadeDuration);
    }


    public override void Init()
    {
        _materialPropertyBlock = new MaterialPropertyBlock();
        _cancellationTokenSource = new CancellationTokenSource();
        _crossFadeShader = _globalShader.UnlitVertexCrossfade;
        _animationShader = _globalShader.UnlitVertexAnimation;

        CacheData.InitTextures();
        _VAT = CacheData.VAT;
        _animMeshUVGenerator.GenerateUV(_skinMesh.sharedMesh);


        if (_meshRenderer == null)
        {
            _meshRenderer = gameObject.GetComponent<MeshRenderer>();

            if (_meshRenderer == null)
            {
                _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }
        }


        if (_meshFilter == null)
        {
            _meshFilter = gameObject.GetComponent<MeshFilter>();

            if (_meshFilter == null)
            {
                _meshFilter = gameObject.AddComponent<MeshFilter>();
            }
        }


        _animationLoopMaterial.enableInstancing = true;
        _crossFadeMaterial.enableInstancing = true;


        _meshFilter.sharedMesh = _skinMesh.sharedMesh;
        _meshRenderer.sharedMaterial = _animationLoopMaterial;
        _skinMesh.enabled = false;
        _animator.enabled = false;
        _animationRoot.SetActive(false);
    }

    private void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        CacheData.Dispose();
        _animMeshUVGenerator.Dispose();
    }

    #region HELPER

    float LerpTiming(float startTime, float duration)
    {
        float time = Time.time - startTime;
        float lerpTime = Mathf.Clamp(time / duration, 0, 1);
        return lerpTime;
    }

    float GetFrame(float startTime, float clipLength, float frameRate, Vector2 frameRange)
    {
        float time = Time.time - startTime;
        int total_frame = Mathf.FloorToInt(clipLength * frameRate);
        float frame = total_frame * 1.0f / frameRate;
        float current_frame;
        if(_isLoop)
            current_frame = Mathf.Repeat(time / frame, 1.0f) * (frameRange.y - frameRange.x) + frameRange.x;
        else
            current_frame = Mathf.Clamp(time / frame * (frameRange.y - frameRange.x) + frameRange.x, frameRange.x, frameRange.y);
        return current_frame;
    }

    #endregion
}