using HutongGames.PlayMaker.Actions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace FiveKnights.Isma
{
    public class PlantCtrl : MonoBehaviour
    {
        private Animator _anim;
        private HealthManager _hm;
        private BoxCollider2D _bc;
        private MusicPlayer _ap;
        private ShadeOnlyPass _sp;

        private const int PLANTHP = 35;

        private void Awake()
        {
            _anim = gameObject.GetComponent<Animator>();
            _bc = gameObject.GetComponent<BoxCollider2D>();
            _sp = gameObject.AddComponent<ShadeOnlyPass>();
            _sp.disableCollider = _bc;
            _sp.colliderEnabled = false;

            PlayMakerFSM spellControl = HeroController.instance.gameObject.LocateMyFSM("Spell Control");
            GameObject fireballParent = spellControl.GetAction<SpawnObjectFromGlobalPool>("Fireball 2", 3).gameObject.Value;
            PlayMakerFSM fireballCast = fireballParent.LocateMyFSM("Fireball Cast");
            GameObject actor = fireballCast.GetAction<AudioPlayerOneShotSingle>("Cast Right", 3).audioPlayer.Value;

            _ap = new MusicPlayer
            {
                Volume = 1f,
                Player = actor,
                MaxPitch = 1f,
                MinPitch = 1f,
                Spawn = gameObject
            };

            gameObject.layer = (int)GlobalEnums.PhysLayers.HERO_DETECTOR;
            transform.position -= new Vector3(0f, 0.2f, 0f);
        }

		private IEnumerator Start()
        {
            yield return null;

            _hm = gameObject.AddComponent<HealthManager>();
            SetupHM();
            gameObject.AddComponent<Flash>();
            _hm.hp = PLANTHP;

            gameObject.SetActive(true);
            _anim.enabled = true;
            _anim.Play("PlantGrow");
            yield return null;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
            _anim.enabled = false;
            yield return new WaitForSeconds(0.9f);
            _anim.enabled = true;
            _ap.Clip = FiveKnights.Clips["IsmaAudVineGrow"];
            _ap.DoPlayRandomClip();
            yield return new WaitWhile(() => _anim.IsPlaying());
			_bc.isTrigger = false;
			_sp.colliderEnabled = true;

            GameObject bnc = new GameObject("PillarPogo")
            {
                layer = 11,
                transform =
                    {
                        position = gameObject.transform.position,
                        rotation = gameObject.transform.rotation,
                        localScale = gameObject.transform.localScale,
                        parent = gameObject.transform
                    }
            };
            var col = bnc.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = _bc.size;
            col.offset = _bc.offset;
            bnc.SetActive(true);
            bnc.transform.parent = gameObject.transform;
            bnc.AddComponent<PlantHitFx>().hitSound = FiveKnights.Clips["IsmaAudVineHit"];

            yield return new WaitForSeconds(0.55f);
        }

        private void SetupHM()
        {
            HealthManager wdHM = FiveKnights.preloadedGO["WD"].GetComponent<HealthManager>();
            HealthManager hm2 = gameObject.GetComponent<HealthManager>();
            foreach (FieldInfo fi in typeof(HealthManager).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => x.Name.Contains("Prefab")))
            {
                fi.SetValue(hm2, fi.GetValue(wdHM));
            }
        }
    }
}
