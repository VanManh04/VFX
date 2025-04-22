using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using NaughtyAttributes;
using SplineMesh;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;


public class MeshBaker : MonoBehaviour
{
    public bool isRecordOnStart;
    [SerializeField] private MeshFilter meshFilter;
    private bool _isPlaying;
    private bool _isRecording;
    private int _currentFrame;
    
    [SerializeField] List<MeshData> verticesList = new List<MeshData>();
    [SerializeField] private MeshCacheData _meshCacheData;
    
    [SerializeField] private float              _frameRate = 30f;
    
    private float _frameTimeAccumulator = 0f;

    private void OnValidate()
    {
        meshFilter = GetComponent<MeshFilter>();
    }
    
    void Start()
    {
        if(isRecordOnStart)
            StartRecord();
    }


    private void FixedUpdate()
    {
        Process(Time.deltaTime);
    }

    private void Process(float dt)
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

    private void Play()
    {
        if (_currentFrame >= _meshCacheData._data.Count)
            return;
        meshFilter.mesh.vertices = _meshCacheData._data[_currentFrame].vertices;
        _currentFrame++;
    }
    
    private void Record()
    {
        MeshData meshData = new MeshData();
        meshData.vertices = meshFilter.mesh.vertices;
        verticesList.Add(meshData);
    }

    public NativeArray<float3> ConvertArrayToFloat3(Vector3[] vector3Array) 
    {
        NativeArray<float3> nativeArray = new NativeArray<float3>(vector3Array.Length, Allocator.Temp);
        for (int i = 0; i < vector3Array.Length; i++)
        {
            nativeArray[i]  = vector3Array[i];
        }
        return nativeArray;
    }
    public Vector3[] ConvertArrayToVector3Arr(NativeArray<float3> nativeArrayfloat3) 
    {
        Vector3[] arrayVector3 = new Vector3[nativeArrayfloat3.Length];
        for (int i = 0; i < nativeArrayfloat3.Length; i++)
        {
            arrayVector3[i]  = nativeArrayfloat3[i];
        }
        return arrayVector3;
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
        _isRecording = true;
    }

    [Button]
    [UsedImplicitly]
    private void StopRecord()
    {
        _isRecording = false;
        _meshCacheData._data = verticesList;
    }
}
