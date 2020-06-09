using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;

namespace FiveKnights
{
    public class AudioController : MonoBehaviour
    {
        private float MAX_VOL = 0.9f;
        private bool change;
        private AudioSource _aud;

        private void Start()
        {
            _aud = gameObject.GetComponent<AudioSource>();
            On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.OnEnter += TransitionToAudioSnapshot_OnEnter;
        }

        private IEnumerator MusicControl()
        {
            if (_aud.volume < 0.1f || MAX_VOL < 0.1f) yield break;
            change = true;
            for (float f = MAX_VOL; f >= 0.1f; f -= 0.1f)
            {
                _aud.volume = f;
                yield return new WaitForSeconds(0.01f);
            }
            yield return new WaitForSeconds(0.75f);
            for (float f = 0.1f; f <= MAX_VOL; f += 0.04f)
            {
                _aud.volume = f;
                yield return new WaitForSeconds(0.01f);
            }
            change = false;
        }

        private void Update()
        {
            if (GameManager.instance.gameState == GlobalEnums.GameState.PAUSED)
            {
                _aud.volume = 0.1f;
            }
            else
            {
                MAX_VOL = GameManager.instance.gameSettings.musicVolume / 10f;
                if (!change) _aud.volume = MAX_VOL;
            }
        }

        private void TransitionToAudioSnapshot_OnEnter(On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.orig_OnEnter orig, TransitionToAudioSnapshot self)
        {
            StartCoroutine(MusicControl());
            orig(self);
        }

        private void OnDestroy()
        {
            On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.OnEnter -= TransitionToAudioSnapshot_OnEnter;
        }
    }
}
