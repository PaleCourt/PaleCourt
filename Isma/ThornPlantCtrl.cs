using HutongGames.PlayMaker.Actions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FiveKnights.Isma
{
    public class ThornPlantCtrl : MonoBehaviour
    {
        private Animator _anim;
        private MusicPlayer _ap;
        public bool secondWave = false;

        private void Awake()
        {
            _anim = gameObject.GetComponent<Animator>();

            DamageHero dh = gameObject.AddComponent<DamageHero>();
            dh.damageDealt = 1;
            dh.hazardType = 0;
            dh.shadowDashHazard = false;

            PlayMakerFSM spellControl = HeroController.instance.gameObject.LocateMyFSM("Spell Control");
            GameObject fireballParent = spellControl.GetAction<SpawnObjectFromGlobalPool>("Fireball 2", 3).gameObject.Value;
            PlayMakerFSM fireballCast = fireballParent.LocateMyFSM("Fireball Cast");
            GameObject actor = fireballCast.GetAction<AudioPlayerOneShotSingle>("Cast Right", 3).audioPlayer.Value;

            if(actor == null)
            {
                Modding.Logger.Log("ERROR: Actor not found.");
            }
            _ap = new MusicPlayer
            {
                Volume = 1f,
                Player = actor,
                MaxPitch = 1f,
                MinPitch = 1f,
                Spawn = gameObject
            };
        }

        private IEnumerator Start()
        {
            PolygonCollider2D pc = GetComponent<PolygonCollider2D>();
            _anim.enabled = true;

            _anim.Play("ThornPlantGrow");
            _ap.Clip = FiveKnights.Clips["IsmaAudAgonyIntro"];
            _ap.DoPlayRandomClip();
			yield return _anim.WaitToFrame(3);
            _anim.enabled = false;

			yield return new WaitForSeconds(secondWave ? 0.3f : 0.5f);
			_anim.enabled = true;
            _ap.Clip = FiveKnights.Clips["IsmaAudAgonyShoot"];
            _ap.DoPlayRandomClip();
            yield return _anim.WaitToFrame(8);
            _anim.enabled = false;
            pc.enabled = true;

            yield return new WaitForSeconds(0.4f);
            pc.enabled = false;
            _anim.enabled = true;
            yield return _anim.PlayBlocking("ThornPlantDie");
            Destroy(gameObject);
        }
    }
}
