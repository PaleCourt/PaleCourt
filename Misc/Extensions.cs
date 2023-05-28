using System;
using System.Collections;
using System.Linq;
using FiveKnights.BossManagement;
using FrogCore.Ext;
using JetBrains.Annotations;
using Modding;
using UnityEngine;
using UnityEngine.Playables;
using Logger = Modding.Logger;

namespace FiveKnights
{
    // Pure is used here so you can't forget to yield return.
    public static class Extensions
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
            if (anim.GetCurrentAnimatorClipInfo(0).Length == 0)
            {
                Logger.Log($"Warning: Could not find animator {anim.name} clip in GetCurrentFrame.");
                return 999999;
            }
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
            if (FiveKnights.AnimClips.ContainsKey(name))
            {
                var clip = FiveKnights.AnimClips[name];
                float normTime = frame / (clip.length * clip.frameRate);
                anim.Play(name, -1, normTime);
            }
            else
            {
                Modding.Logger.Log("WARNING: Could not find aninmation clip, reverting to old method.");
                AnimatorClipInfo att = anim.GetCurrentAnimatorClipInfo(0)[0];
                float normTime = frame / (att.clip.length * att.clip.frameRate);
                anim.Play(name, 0, normTime);
            }
        }
        
        [Pure]
        public static IEnumerator PlayBlocking(this Animator self, string anim)
        {
            self.Play(anim, -1, 0f);

            yield return new WaitForEndOfFrame();

            while (self.GetCurrentAnimatorStateInfo(0).normalizedTime <= 1)
                yield return null;
        }

        [Pure]
        public static IEnumerator PlayToFrame(this Animator self, string anim, int frame)
        {
            self.Play(anim, -1, 0f);

            // Wait for animation start.
            yield return new WaitForEndOfFrame();

            yield return new WaitWhile(() => self.GetCurrentFrame() < frame);
        }
        
        [Pure]
        public static IEnumerator PlayToFrameAt(this Animator self, string anim, int start, int frame)
        {
            self.PlayAt(anim, start);

            // Wait for animation start.
            yield return new WaitForEndOfFrame();

            yield return new WaitWhile(() => self.GetCurrentFrame() < frame);
        }
        
        [Pure]
        public static IEnumerator WaitToFrame(this Animator self, int frame)
        {
            yield return new WaitForEndOfFrame();
            while (self.GetCurrentFrame() < frame)
                yield return null;
        }

        [Pure]
        public static IEnumerator PlayToEnd(this Animator self, string name)
        {
            self.Play(name, -1, 0f);
            yield return new WaitForEndOfFrame();
            while (self.GetCurrentAnimatorStateInfo(0).normalizedTime <= 1)
            {
                yield return null;
            }
        }
        
        [Pure]
        public static IEnumerator PlayToEnd(this Animator self)
        {
            yield return new WaitForEndOfFrame();
            while (self.GetCurrentAnimatorStateInfo(0).normalizedTime <= 1)
                yield return null;
        }
        
        [Pure]
        public static IEnumerator WaitForFramesWithActions(this Animator self, params (int frame, Action act)[] acts)
        {
            yield return new WaitForEndOfFrame();
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

            yield return new WaitForEndOfFrame();

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

            yield return new WaitForEndOfFrame();

            while (predicate())
                yield return null;
        }

        public static bool Within(this float self, float rhs, float threshold)
        {
            return Math.Abs(self - rhs) <= threshold;
        }

        public static bool RecordWithoutNotes(this EnemyDeathEffects deathEffects, bool withoutnotes = true, int defaultkills = 2)
        {
            try
            {
                string pd = "kills" + ReflectionHelper.GetField<EnemyDeathEffects, string>(deathEffects, "playerDataName");
                int kills = PlayerData.instance.GetInt(pd);
                if (kills > 0 && withoutnotes)
                    PlayerData.instance.SetInt(pd, defaultkills);
                else
                    PlayerData.instance.SetInt(pd, 1);
                deathEffects.RecordJournalEntry();
                return kills > 0 && withoutnotes;
            }
            catch(Exception e)
            {
                FiveKnights.Instance.Log(e);
                return withoutnotes;
            }
        }

        public static AudioSource PlayAudio(this MonoBehaviour mb, AudioClip clip, float volume = 1f,
            float pitchVariation = 0f, Transform posOverride = null, Func<bool> destroyWhen = null)
		{
            GameObject audioPlayer = new GameObject("Audio Player", typeof(AudioSource), typeof(AutoDestroy));
            audioPlayer.transform.position = posOverride == null ? mb.transform.position : posOverride.position;
            
            AutoDestroy autoDestroy = audioPlayer.GetComponent<AutoDestroy>();
            autoDestroy.Time = clip.length + 1f;
            autoDestroy.ShouldDestroy = destroyWhen;

            AudioSource audioSource = audioPlayer.GetComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.pitch = UnityEngine.Random.Range(1f - pitchVariation, 1f + pitchVariation);
            audioSource.outputAudioMixerGroup = HeroController.instance.GetComponent<AudioSource>().outputAudioMixerGroup;
            audioSource.Play();

            return audioSource;
        }
    }
}
