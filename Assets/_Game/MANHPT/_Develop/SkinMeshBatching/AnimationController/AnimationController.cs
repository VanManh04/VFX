using System;
using System.Linq;
using JetBrains.Annotations;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class AnimationController : MonoBehaviour
{
    [ReadOnly] public  StateInforsData           StateInforsData;
    [ReadOnly] public  BaseAnimationUpdate[]     AnimationUpdates;
    [ReadOnly] public  StateInforsData.StateInfo CurrentStateInfo;
    [ReadOnly] public  int                       _currentFrame;
    [ReadOnly] public  float                     _clipLength;
    [ReadOnly] public  float                     _currentTime;
    [ReadOnly] public  float                     _frameRate;
    [ReadOnly] private bool                      _loop = true;

    private bool _initDone  = false;
    private int  _lastFrame = -1;

    private void Awake()
    {
        if (!_initDone) Init();
    }

    private void OnEnable()
    {
        VAT_AnimationUpdater.Instance.Add(this);
    }

    private void OnDisable()
    {
        VAT_AnimationUpdater.Instance.Remove(this);
    }

    [UsedImplicitly]
    public void RandomState()
    {
        CurrentStateInfo = StateInforsData.StateInfors[Random.Range(0, StateInforsData.StateInfors.Count)];
        ChangeState(CurrentStateInfo.stateName, duration: Random.value, isLoop: Random.value > 0.5f);
    }

    public void UpdateAnim()
    {
        if (!_initDone) return;
        
        CalculateCurrentTime();

        _currentFrame = Mathf.FloorToInt(_currentTime * _frameRate);
        
        _lastFrame = _currentFrame;
        
        var updateCount = AnimationUpdates.Length;
        for (var i = 0; i < updateCount; i++)
        {
            AnimationUpdates[i]?.FrameUpdate(_currentFrame);
        }
    }

    private void CalculateCurrentTime()
    {
        var deltaTime = Time.deltaTime;
        _currentTime += deltaTime;
        
        if (_loop)
        {
            if (_currentTime >= _clipLength)
                _currentTime %= _clipLength;
        }
        else
        {
            if (_currentTime > _clipLength)
                _currentTime = _clipLength;
        }
    }

    private void Init()
    {
        CurrentStateInfo = StateInforsData.GetDefaultStateInfo();
        InitUpdates();
        ChangeState(CurrentStateInfo.stateName);
        _initDone = true;
    }

    public void ChangeState(string stateName, float duration = 0, bool isLoop = false)
    {
        CurrentStateInfo = StateInforsData.GetStateInfo(stateName);
        _clipLength      = CurrentStateInfo.animationInfo.clipLength;
        _frameRate       = CurrentStateInfo.animationInfo.frameRate;
        _currentTime     = 0;
        _currentFrame    = 0;

        _loop = isLoop;

        for (var i = 0; i < AnimationUpdates.Length; i++)
        {
            var updateObject = AnimationUpdates[i];
            updateObject?.ChangeState(stateName, duration, isLoop);
        }
    }

    private void InitUpdates()
    {
        if (AnimationUpdates == null || AnimationUpdates.Length == 0)
            return;

        foreach (var updateObject in AnimationUpdates.Where(e => e != null))
        {
            updateObject.Init();
        }
    }
}