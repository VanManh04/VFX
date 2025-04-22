using NaughtyAttributes;
using UnityEngine;

public abstract class BaseAnimationUpdate : MonoBehaviour
{
    [ReadOnly] public int CurrentFrame;
    public abstract void Init();
    public abstract void FrameUpdate(int currentFrame);
    public abstract void ChangeState(string stateName , float duration = 0 , bool isLoop = false);
}