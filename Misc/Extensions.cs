using System;
using System.Collections;
using JetBrains.Annotations;
using UnityEngine;
using Vasi;

namespace FiveKnights
{
    // Pure is used here so you can't forget to yield return.
    public static class Extensions
    {
        [Pure]
        public static IEnumerator PlayBlocking(this Animator self, string anim)
        {
            self.Play(anim);

            yield return null;

            while (self.GetCurrentAnimatorStateInfo(0).normalizedTime <= 1)
                yield return null;
        }

        [Pure]
        public static IEnumerator PlayToFrame(this Animator self, string anim, int frame)
        {
            self.Play(anim);

            // Wait for animation start.
            yield return null;

            yield return new WaitWhile(() => self.GetCurrentFrame() < frame);
        }
        
        [Pure]
        public static IEnumerator PlayToFrameAt(this Animator self, string anim, int start, int frame)
        {
            self.PlayAt(anim, start);

            // Wait for animation start.
            yield return null;

            yield return new WaitWhile(() => self.GetCurrentFrame() < frame);
        }
        
        [Pure]
        public static IEnumerator WaitToFrame(this Animator self, int frame)
        {
            yield return null;
            while (self.GetCurrentFrame() < frame)
                yield return null;
        }

        [Pure]
        public static IEnumerator PlayToEnd(this Animator self)
        {
            yield return null;
            while (self.GetCurrentAnimatorStateInfo(0).normalizedTime <= 1)
                yield return null;
        }
        
        [Pure]
        public static IEnumerator WaitForFramesWithActions(this Animator self, params (int frame, Action act)[] acts)
        {
            yield return null;
            foreach ((int frame, Action act) in acts)
            {
                yield return self.WaitToFrame(frame);

                act();
            }
        }
        
        
        [Pure]
        public static IEnumerator PlayWithActions(this Animator self, string anim, params (int frame, Action act)[] acts)
        {
            self.PlayAt(anim, 0);

            yield return null;

            yield return self.WaitForFramesWithActions(acts);
        }

        [Pure]
        public static IEnumerator PlayToEndWithActions(this Animator self, string anim, params (int frame, Action act)[] acts)
        {
            yield return self.PlayWithActions(anim, acts);

            yield return self.PlayToEnd();
        }

        public static IEnumerator PlayBlockingWhile(this Animator self, string anim, Func<bool> predicate)
        {
            self.Play(anim);

            yield return null;

            while (predicate())
                yield return null;
        }

        public static bool Within(this float self, float rhs, float threshold)
        {
            return Math.Abs(self - rhs) <= threshold;
        }
    }
}