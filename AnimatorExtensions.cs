using UnityEngine;

namespace FiveKnights
{
    public static class AnimatorExtensions
    {
        /// <summary>
        /// Checks if an animation clip is playing.
        /// </summary>
        /// <param name="anim"></param>
        /// <returns>
        /// true if animation is playing
        /// false if animation is not playing
        /// </returns>
        public static bool IsPlaying(this Animator anim)
        {
            return anim.GetCurrentAnimatorStateInfo(0).normalizedTime <= 1f;
        }

        /// <summary>
        /// Checks if a specific animation clip is playing.
        /// </summary>
        /// <param name="anim"></param>
        /// <param name="s"></param>
        /// <returns>
        /// true if animation is playing
        /// false if animation is not playing
        /// </returns>
        public static bool IsPlaying(this Animator anim, string s)
        {
            return anim.GetCurrentAnimatorStateInfo(0).IsName(s) && anim.IsPlaying();
        }

        /// <summary>
        /// Returns the frame the animation clip is at.
        /// </summary>
        /// <param name="anim"></param>
        /// <returns>
        /// returns an integer frame number
        /// </returns>
        public static int GetCurrentFrame(this Animator anim)
        {
            AnimatorClipInfo att = anim.GetCurrentAnimatorClipInfo(0)[0];
            int currentFrame = (int)(anim.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f * (att.clip.length * att.clip.frameRate));
            return currentFrame;
        }

        /// <summary>
        /// Plays the clip named [name] at frame [frame].
        /// </summary>
        /// <param name="anim"></param>
        /// <param name="name"></param>
        /// <param name="frame"></param>
        public static void PlayAt(this Animator anim, string name, int frame)
        {
            AnimatorClipInfo att = anim.GetCurrentAnimatorClipInfo(0)[0];
            float normTime = frame / (att.clip.length * att.clip.frameRate);
            anim.Play(name, 0, normTime);
        }

        /// <summary>
        /// Pauses and unpauses the animation that is playing.
        /// </summary>
        /// <param name="anim"></param>
        public static void TogglePause(this Animator anim)
        {
            anim.enabled = !anim.enabled;
        }
    }
}