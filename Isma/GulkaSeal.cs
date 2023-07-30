using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using HutongGames.PlayMaker.Actions;

namespace FiveKnights.Isma
{
    // Recreation of tink effect from Kingsmould shield
    public class GulkaSeal : MonoBehaviour
    {
        private MusicPlayer _ap;
        private SpriteRenderer _sr;
        private GameObject _pt;
        private Coroutine _flashCoro;

        private void Start()
        {
            PlayMakerFSM spellControl = HeroController.instance.gameObject.LocateMyFSM("Spell Control");
            GameObject fireballParent = spellControl.GetAction<SpawnObjectFromGlobalPool>("Fireball 2", 3).gameObject.Value;
            PlayMakerFSM fireballCast = fireballParent.LocateMyFSM("Fireball Cast");
            GameObject actor = fireballCast.GetAction<AudioPlayerOneShotSingle>("Cast Right", 3).audioPlayer.Value;

            _ap = new MusicPlayer
            {
                Volume = 1f,
                Player = actor,
                MaxPitch = 0.85f,
                MinPitch = 1.15f,
                Spawn = gameObject,
                Clip = ParryTink.TinkClip
            };

            _sr = transform.GetComponent<SpriteRenderer>();
            _sr.color = new Color(1f, 1f, 1f, 0f);

            _pt = FiveKnights.preloadedGO["Shield"].transform.Find("Attack Pt").gameObject;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if(!other.gameObject.CompareTag("Nail Attack")) return;

            //_ap.DoPlayRandomClip();
            this.PlayAudio(ParryTink.TinkClip, 1f, 0.15f);
            ParticleSystem.EmissionModule emission = _pt.GetComponent<ParticleSystem>().emission;
            emission.enabled = true;

            if(_flashCoro != null) StopCoroutine(_flashCoro);
            _flashCoro = StartCoroutine(Flash());
        }

        private IEnumerator Flash()
        {
            for(int i = 0; i < 100; i++)
            {
                _sr.color = new Color(1f, 1f, 1f, 1f - i / 100f);
                yield return new WaitForSeconds(0.01f);
            }
            _sr.color = new Color(1f, 1f, 1f, 0f);
            ParticleSystem.EmissionModule emission = _pt.GetComponent<ParticleSystem>().emission;
            emission.enabled = false;
        }
    }
}
