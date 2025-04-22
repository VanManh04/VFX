
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

public class AnimatorCustomEditor :  EditorWindow
{
    [MenuItem("CONTEXT/Animator/Cache Animator Data")]
    static void DoSomething(MenuCommand command)
    {
        Animator animator = (Animator)command.context;
        
        if(!animator.TryGetComponent(out AnimationCacheController animationCacheController))
        {
            animationCacheController = animator.gameObject.AddComponent<AnimationCacheController>();
        }
        
        if (!animator.TryGetComponent(out AnimationController animationController))
        {
            animationController = animator.gameObject.AddComponent<AnimationController>();
        }
        
        animationCacheController.AnimationController = animationController;
        
        animationCacheController.Require();
        
        animator.enabled = false;
    }
}
#endif
