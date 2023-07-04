using System;
using System.Collections.Generic;
using UnityEngine;
using SFCore.Utils;
using Random = UnityEngine.Random;
using System.Collections;
using HutongGames.PlayMaker.Actions;

namespace FiveKnights
{
	public class BoonSpells : MonoBehaviour
	{
        private HeroController _hc = HeroController.instance;
        private PlayMakerFSM _spellControl;
        private GameObject _audioPlayer;

        private const float DaggerSpeed = 50f;
        private const int BlastDamage = 35;
        private const int BlastDamageShaman = 45;

        private void OnEnable()
		{
            _spellControl = _hc.spellControl;

            GameObject fireballParent = _spellControl.GetAction<SpawnObjectFromGlobalPool>("Fireball 2", 3).gameObject.Value;
            PlayMakerFSM fireballCast = fireballParent.LocateMyFSM("Fireball Cast");
            _audioPlayer = fireballCast.GetAction<AudioPlayerOneShotSingle>("Cast Right", 3).audioPlayer.Value;

            PlayMakerFSM _pvControl = Instantiate(FiveKnights.preloadedGO["PV"].LocateMyFSM("Control"), _hc.transform);

            if(!FiveKnights.preloadedGO.ContainsKey("Plume"))
			{
                GameObject plume = Instantiate(_pvControl.GetAction<SpawnObjectFromGlobalPool>("Plume Gen", 0).gameObject.Value);
                plume.SetActive(false);
                plume.layer = (int)GlobalEnums.PhysLayers.HERO_ATTACK;
                plume.tag = "Hero Spell";
                Destroy(plume.GetComponent<DamageHero>());
                DontDestroyOnLoad(plume);
                FiveKnights.preloadedGO["Plume"] = plume;
            }

            if(!FiveKnights.preloadedGO.ContainsKey("BoonDagger"))
			{
                GameObject dagger = Instantiate(_pvControl.GetAction<FlingObjectsFromGlobalPoolTime>("SmallShot LowHigh").gameObject.Value);
                dagger.SetActive(false);
                dagger.layer = (int)GlobalEnums.PhysLayers.HERO_ATTACK;
                dagger.tag = "Hero Spell";
                Destroy(dagger.GetComponent<DamageHero>());
                Destroy(dagger.LocateMyFSM("Control"));
                dagger.FindGameObjectInChildren("Dribble L").layer = 9;
                dagger.FindGameObjectInChildren("Glow").layer = 9;
                dagger.FindGameObjectInChildren("Beam").layer = 9;
                DontDestroyOnLoad(dagger);
                FiveKnights.preloadedGO["BoonDagger"] = dagger;
            }

            FiveKnights.Clips["Burst"] = (AudioClip)_pvControl.GetAction<AudioPlayerOneShotSingle>("Focus Burst", 8).audioClip.Value;
            FiveKnights.Clips["Plume Up"] = (AudioClip)_pvControl.GetAction<AudioPlayerOneShotSingle>("Plume Up", 1).audioClip.Value;

            ModifySpellFSM(true);
        }

        private void OnDisable()
		{
            ModifySpellFSM(false);
		}

		private void ModifySpellFSM(bool enabled)
		{
            if(enabled)
            {
                _spellControl.ChangeTransition("Level Check 3", "LEVEL 1", "Scream Antic1 Blasts");
                _spellControl.ChangeTransition("Level Check 3", "LEVEL 2", "Scream Antic2 Blasts");

                _spellControl.ChangeTransition("Quake1 Down", "HERO LANDED", "Q1 Land Plumes");
                _spellControl.ChangeTransition("Quake2 Down", "HERO LANDED", "Q2 Land Plumes");

                if(!PlayerData.instance.GetBool(nameof(PlayerData.equippedCharm_11)))
                {
                    _spellControl.ChangeTransition("Level Check", "LEVEL 1", "Fireball 1 SmallShots");
                    _spellControl.ChangeTransition("Level Check", "LEVEL 2", "Fireball 2 SmallShots");
                }
            }
            else
            {
                _spellControl.ChangeTransition("Level Check 3", "LEVEL 1", "Scream Antic1");
                _spellControl.ChangeTransition("Level Check 3", "LEVEL 2", "Scream Antic2");

                _spellControl.ChangeTransition("Quake1 Down", "HERO LANDED", "Quake1 Land");
                _spellControl.ChangeTransition("Quake2 Down", "HERO LANDED", "Q2 Land");

                _spellControl.ChangeTransition("Level Check", "LEVEL 1", "Fireball 1");
                _spellControl.ChangeTransition("Level Check", "LEVEL 2", "Fireball 2");
            }
        }

        public void CastDaggers(bool upgraded)
		{
            bool shaman = PlayerData.instance.equippedCharm_19;
            int angleMin = shaman ? -30 : -25;
            int angleMax = shaman ? 30 : 25;
            int increment = shaman ? 20 : 25;
            for(int angle = angleMin; angle <= angleMax; angle += increment)
            {
                GameObject dagger = Instantiate(FiveKnights.preloadedGO["BoonDagger"], 
                    HeroController.instance.transform.position, Quaternion.identity);
                dagger.SetActive(false);
                if(angle != angleMin) Destroy(dagger.GetComponent<AudioSource>());

                Rigidbody2D rb = dagger.GetComponent<Rigidbody2D>();
                rb.isKinematic = true;
                float xVel = DaggerSpeed * Mathf.Cos(Mathf.Deg2Rad * angle) * -HeroController.instance.transform.localScale.x;
                float yVel = DaggerSpeed * Mathf.Sin(Mathf.Deg2Rad * angle);

                dagger.SetActive(true);
                rb.velocity = new Vector2(xVel, yVel);

                dagger.AddComponent<Dagger>().upgraded = upgraded;

                Destroy(dagger, 5f);
            }
        }

        public void CastPlumes(bool upgraded)
        {
            for(float x = 2; x <= 10; x += 2)
            {
                Vector2 pos = HeroController.instance.transform.position;
                float plumeY = pos.y - 1.8f;

                GameObject plumeL = Instantiate(FiveKnights.preloadedGO["Plume"], new Vector2(pos.x - x, plumeY), Quaternion.identity);
                plumeL.SetActive(true);
                plumeL.AddComponent<Plume>().upgraded = upgraded;

                GameObject plumeR = Instantiate(FiveKnights.preloadedGO["Plume"], new Vector2(pos.x + x, plumeY), Quaternion.identity);
                plumeR.SetActive(true);
                plumeR.AddComponent<Plume>().upgraded = upgraded;
            }
            PlayAudio("Plume Up", 1.5f, 1.5f, 0.5f, 0.25f);
        }

        public void CastBlasts(bool upgraded)
		{
            List<GameObject> blasts = new List<GameObject>();

            // Enemy iframes last 0.15s, 
            IEnumerator CastBlastsCoro()
            {
                blasts.Add(SpawnBlast(HeroController.instance.transform.position + Vector3.up * 4f, upgraded));
                yield return new WaitForSeconds(0.2f);

                for(int i = 0; i < (upgraded ? 3 : 1); i++)
                {
                    blasts.Add(SpawnBlast(HeroController.instance.transform.position + 
                        Vector3.up * Random.Range(2, 8) + Vector3.right * Random.Range(-3, 3), upgraded));
                    yield return new WaitForSeconds(0.2f);
                }
            }
			IEnumerator DisableColliderCoro()
			{
				yield return new WaitForSeconds(0.15f);

				for(int i = 0; i < blasts.Count; i++)
				{
					Destroy(blasts[i].GetComponent<CircleCollider2D>());
					yield return new WaitForSeconds(0.2f);
				}
			}
			IEnumerator DestroyBlastsCoro()
			{
                yield return new WaitForSeconds(0.5f);

                for(int i = 0; i < blasts.Count; i++)
				{
                    Destroy(blasts[i]);
                    yield return new WaitForSeconds(0.2f);
                }
            }
            StartCoroutine(CastBlastsCoro());
			StartCoroutine(DisableColliderCoro());
			StartCoroutine(DestroyBlastsCoro());
        }

        private GameObject SpawnBlast(Vector3 pos, bool upgraded)
		{
            GameObject blast = Instantiate(FiveKnights.preloadedGO["Blast"], pos, Quaternion.identity);
            blast.layer = (int)GlobalEnums.PhysLayers.HERO_ATTACK;
            blast.tag = "Hero Spell";
            blast.SetActive(true);
            Destroy(blast.FindGameObjectInChildren("hero_damager"));

            Animator anim = blast.GetComponent<Animator>();
            int hash = anim.GetCurrentAnimatorStateInfo(0).fullPathHash;
            anim.PlayInFixedTime(hash, -1, 0.75f);

            CircleCollider2D col = blast.AddComponent<CircleCollider2D>();
            col.offset = Vector2.down;
            col.radius = 3.5f;
            col.isTrigger = true;

            DamageEnemies de = blast.AddComponent<DamageEnemies>();
            de.damageDealt = PlayerData.instance.equippedCharm_19 ? BlastDamageShaman : BlastDamage;
            de.attackType = AttackTypes.Spell;
            de.ignoreInvuln = false;
            de.enabled = true;
            PlayAudio("Burst", 1.2f, 1.5f, 0.5f);

            return blast;
        }

        private void PlayAudio(string clip, float minPitch = 1f, float maxPitch = 1f, float volume = 1f, float delay = 0f)
		{
            IEnumerator Play()
            {
                AudioClip audioClip = FiveKnights.Clips[clip];
                yield return new WaitForSeconds(delay);
                GameObject audioPlayerInstance = _audioPlayer.Spawn(transform.position, Quaternion.identity);
                AudioSource audio = audioPlayerInstance.GetComponent<AudioSource>();
                audio.outputAudioMixerGroup = HeroController.instance.GetComponent<AudioSource>().outputAudioMixerGroup;
                audio.pitch = Random.Range(minPitch, maxPitch);
                audio.volume = volume;
                audio.PlayOneShot(audioClip);
                yield return new WaitForSeconds(audioClip.length + 3f);
                Destroy(audioPlayerInstance);
            }
            GameManager.instance.StartCoroutine(Play());
        }
	}
}
