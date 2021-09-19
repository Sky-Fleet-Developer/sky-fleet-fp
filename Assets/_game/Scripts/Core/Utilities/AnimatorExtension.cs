using UnityEngine;

namespace Core.Utilities
{
    public static class AnimatorExtention
    {
        public static AnimationClip GetClip(this Animator animator, string name)
        {
            var clips = animator.runtimeAnimatorController.animationClips;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i].name == name)
                    return clips[i];
            }

            return null;
        }
    }
}