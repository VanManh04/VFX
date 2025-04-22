using System;
using System.Collections.Generic;
using UnityEngine;

public class VAT_AnimationUpdater : MonoBehaviour
{
    private readonly List<AnimationController> _animationControllers = new List<AnimationController>();
    public static    VAT_AnimationUpdater      Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void Add(AnimationController animationController)
    {
        _animationControllers.Add(animationController);
    }

    public void Remove(AnimationController animationController)
    {
        _animationControllers.Remove(animationController);
    }

    private void Update()
    {
        for (var i = 0; i < _animationControllers.Count; i++)
        {
            _animationControllers[i].UpdateAnim();
        }
    }
}