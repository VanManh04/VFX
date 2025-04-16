using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using NaughtyAttributes;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Jobs;

public class AnimationBaker : MonoBehaviour
{
    [SerializeField] private AnimationCacheData _animationCacheData;
    [SerializeField] private List<Transform>    _objectsToRecord;
    [SerializeField] private float              _frameRate = 30f;

    private float _frameTimeAccumulator = 0f;

    public UnityEvent OnStartRecord;

    private TransformAccessArray _transformAccessArray;

    private int _currentFrame;
    private int _objectCount;
    private int _frameCount;

    private bool _isRecording;
    private bool _isPlaying;

    private void Awake()
    {
        _animationCacheData.DecompressData();
        _transformAccessArray = new TransformAccessArray(_objectsToRecord.ToArray());
        _objectCount          = _objectsToRecord.Count;
        _frameCount           = _animationCacheData.FrameCount;

        //GameLoop.Add(() => Process(Time.deltaTime), 4);
    }

    private void FixedUpdate()
    {
        Process(Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (_transformAccessArray.isCreated)
        {
            _transformAccessArray.Dispose();
        }
    }

    public void Process(float dt)
    {
        if (_isRecording)
        {
            _isPlaying = false;
            Record();
        }
        else if (_isPlaying)
        {
            // Accumulate time
            _frameTimeAccumulator += dt;
        
            // Calculate time per frame based on frame rate
            var timePerFrame = 1f / _frameRate;
        
            // Play frames according to accumulated time
            while (_isPlaying && _frameTimeAccumulator >= timePerFrame)
            {
                Play();
                _frameTimeAccumulator -= timePerFrame;
            }
        }
    }

    private void Record()
    {
        var frameData = new AnimationCacheData.FrameData
        {
            position = new Vector3[_objectCount],
            rotation = new Quaternion[_objectCount]
        };

        for (var i = 0; i < _objectsToRecord.Count; i++)
        {
            var obj = _objectsToRecord[i];
            frameData.position[i] = obj.position;
            frameData.rotation[i] = obj.rotation;
        }

        _animationCacheData.AddFrameData(frameData);
    }

    private unsafe void Play()
    {
        if (_currentFrame >= _frameCount)
        {
            StopPlay();
            return;
        }

        var currentFrameData = _animationCacheData.GetFrameData(_currentFrame);

        
        
        fixed (Vector3* posPtr = currentFrameData.position)
        fixed (Quaternion* rotPtr = currentFrameData.rotation)
        {
            var playAnimJob = new PlayAnimJob
            {
                position = posPtr,
                rotation = rotPtr,
            };

            playAnimJob.Schedule(_transformAccessArray).Complete();
        }

        _currentFrame++;
    }

    private void Save()
    {
        _animationCacheData.SaveData();
    }

    [Button]
    [UsedImplicitly]
    public void StartPlay()
    {
        _currentFrame = 0;
        _isPlaying    = true;
    }

    [Button]
    [UsedImplicitly]
    private void StopPlay()
    {
        _isPlaying = false;
    }

    [Button]
    [UsedImplicitly]
    private void StartRecord()
    {
        _animationCacheData.ClearData();
        _isRecording = true;
        OnStartRecord?.Invoke();
    }

    [Button]
    [UsedImplicitly]
    private void StopRecord()
    {
        _isRecording = false;
        Save();
    }

    [BurstCompile]
    private unsafe struct PlayAnimJob : IJobParallelForTransform
    {
        [NativeDisableUnsafePtrRestriction] public Vector3*    position;
        [NativeDisableUnsafePtrRestriction] public Quaternion* rotation;

        public void Execute(int index, TransformAccess transform)
        {
            transform.position = position[index];
            transform.rotation = rotation[index];
        }
    }
}