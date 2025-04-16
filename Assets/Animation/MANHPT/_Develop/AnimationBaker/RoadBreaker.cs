using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(AnimationBaker), typeof(CinemachineImpulseSource))]
public class RoadBreaker : MonoBehaviour//, IInteractable
{
    [SerializeField] private AnimationBaker           _animationBaker;
    [SerializeField] private CinemachineImpulseSource _impulseSource;
    [SerializeField] private ParticleSystem           _explosionEffect;
    [SerializeField] private ParticleSystem           _colliderEffect;
    [SerializeField] private int                      _delayTime = 1000;

    private bool _isPlayed;

    private void OnValidate()
    {
        if (_animationBaker == null) _animationBaker = GetComponent<AnimationBaker>();
        if (_impulseSource  == null) _impulseSource  = GetComponent<CinemachineImpulseSource>();
    }

    private void Awake()
    {
        _isPlayed = false;
    }

    //public void Interact(BaseCarController vehicle)
    //{
    //    if (_isPlayed) return;
    //    _animationBaker.StartPlay();
    //    _explosionEffect.Play();
    //    _         = TurnOffLight();
    //    _isPlayed = true;
    //}

    private async UniTaskVoid TurnOffLight()
    {
        await UniTask.Delay(_delayTime);
        _colliderEffect.Play();
        _impulseSource?.GenerateImpulse();
    }
}