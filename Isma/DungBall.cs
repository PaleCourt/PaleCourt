using System.Collections;
using FiveKnights.Isma;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace FiveKnights.Isma
{
    internal class DungBall : MonoBehaviour
    {
        private MusicPlayer _ap;
        private bool _hit;

        public GameObject particles;
        public bool usingThornPillars = false;
        public float LeftX = 67f;
        public float RightX = 85f;
        
        private void Awake()
        {
            gameObject.name = "IsmaHitBall";
            gameObject.transform.localScale *= 1.4f;
            gameObject.layer = 11;
            DamageHero dh = gameObject.AddComponent<DamageHero>();
            dh.damageDealt = 1;
            dh.hazardType = (int)GlobalEnums.HazardType.SPIKES;
            dh.shadowDashHazard = false;
        }

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
                MaxPitch = 1f,
                MinPitch = 1f,
                Spawn = gameObject
            };
        }

        private void FixedUpdate()
        {
            if (!_hit && gameObject.transform.GetPositionY() < 7.4f)
            {
                if(EnemyPlantSpawn.PillarCount < EnemyPlantSpawn.MAX_PILLAR && 
                    transform.position.x > LeftX && transform.position.x < RightX && !usingThornPillars)
                {
                    _ap.Clip = FiveKnights.Clips["IsmaAudDungBreak"];
                    _ap.DoPlayRandomClip();
                    GameObject pillar = Instantiate(FiveKnights.preloadedGO["Plant"]);
                    pillar.name = "PillarEnemy";
                    pillar.transform.position = new Vector3(transform.position.x, 6.1f, 0.1f);
                    pillar.AddComponent<EnemyPlantSpawn.PillarMinion>();
                }
                StartCoroutine(DelayedKill());
                _hit = true;
                particles.transform.position = transform.position;
                particles.GetComponent<ParticleSystem>().Play();
            }
        }

        private IEnumerator DelayedKill()
        {
            yield return new WaitForSeconds(1.5f); 
            Destroy(gameObject);
        }
    }
}