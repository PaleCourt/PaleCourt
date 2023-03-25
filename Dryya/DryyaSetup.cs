using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FiveKnights.BossManagement;
using FiveKnights.Misc;
using SFCore.Utils;
using FrogCore.Ext;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FiveKnights.Dryya
{
    public class DryyaSetup : MonoBehaviour
    {
        private int _hp = 1500;

        private readonly float LeftX = OWArenaFinder.IsInOverWorld ? 422 : 61.0f;
        private readonly float RightX = OWArenaFinder.IsInOverWorld ? 455 : 91.0f;
        private readonly float GroundY = OWArenaFinder.IsInOverWorld ? 101.0837f : 10.625f;
        private float SlamY = OWArenaFinder.IsInOverWorld ? 96.5f : 5.9f;

        private PlayMakerFSM _mageLord;
        private PlayMakerFSM _control;

        private EnemyDeathEffectsUninfected _deathEffects;
        private EnemyDreamnailReaction _dreamNailReaction;
        private EnemyHitEffectsUninfected _hitEffects;
        private HealthManager _hm;
        private SpriteFlash _spriteFlash;
        private GameObject _corpse;
        private tk2dSprite _sprite;
        private GameObject _ap;

        private GameObject _diveShockwave;
        private GameObject _ogrim;
        private GameObject _slash1Collider1;
        private GameObject _slash1Collider2;
        private GameObject _slash2Collider1;
        private GameObject _slash2Collider2;
        private GameObject _slash3Collider1;
        private GameObject _slash3Collider2;
        private GameObject _cheekySlashCollider1;
        private GameObject _cheekySlashCollider2;
        private GameObject _cheekySlashCollider3;
        private List<GameObject> _slashes;
        private GameObject _stabFlash;
        private List<ElegyBeam> _elegyBeams;

        private string[] _dreamNailDialogue =
        {
            "DRYYA_DIALOG_1",
            "DRYYA_DIALOG_2",
            "DRYYA_DIALOG_3",
            "DRYYA_DIALOG_4",
            "DRYYA_DIALOG_5",
        };

        private void Awake()
        {
            GameObject go = gameObject;
            go.SetActive(true);
            go.layer = 11;

			#region Colliders
			_corpse = gameObject.FindGameObjectInChildren("Corpse Dryya");
            _diveShockwave = gameObject.FindGameObjectInChildren("Dive Shockwave");
            _slash1Collider1 = gameObject.FindGameObjectInChildren("Slash 1 Collider 1");
            _slash1Collider2 = gameObject.FindGameObjectInChildren("Slash 1 Collider 2");
            _slash2Collider1 = gameObject.FindGameObjectInChildren("Slash 2 Collider 1");
            _slash2Collider2 = gameObject.FindGameObjectInChildren("Slash 2 Collider 2");
            _slash3Collider1 = gameObject.FindGameObjectInChildren("Slash 3 Collider 1");
            _slash3Collider2 = gameObject.FindGameObjectInChildren("Slash 3 Collider 2");
            _cheekySlashCollider1 = gameObject.FindGameObjectInChildren("Slash Collider 1");
            _cheekySlashCollider2 = gameObject.FindGameObjectInChildren("Slash Collider 2");
            _cheekySlashCollider3 = gameObject.FindGameObjectInChildren("Slash Collider 3");
            _slashes = new List<GameObject>
            {
                _slash1Collider1,
                _slash1Collider2,
                _slash2Collider1,
                _slash2Collider2,
                _slash3Collider1,
                _slash3Collider2,
                _cheekySlashCollider1,
                _cheekySlashCollider2,
                _cheekySlashCollider3,
            };
            _slashes.AddRange(transform.Find("Super").gameObject.GetComponentsInChildren<DamageHero>().Select(d => d.gameObject));
            #endregion

            _stabFlash = gameObject.FindGameObjectInChildren("Stab Flash");
            _ogrim = FiveKnights.preloadedGO["WD"];
            _dreamImpactPrefab = _ogrim.GetComponent<EnemyDreamnailReaction>().GetAttr<EnemyDreamnailReaction, GameObject>("dreamImpactPrefab");
            AddComponents();
            GetComponents();
            _mageLord = FiveKnights.preloadedGO["Mage"].LocateMyFSM("Mage Lord");
            _control = gameObject.LocateMyFSM("Control");
            _control.SetState("Init");
            _control.Fsm.GetFsmGameObject("Hero").Value = HeroController.instance.gameObject;
            _control.Fsm.GetFsmBool("GG Form").Value = false;
            _control.Fsm.GetFsmFloat("Ground").Value = GroundY;

            _control.InsertMethod("Activate", 0, () => _hm.enabled = true);
            
            _control.InsertMethod("Phase Check", 0, () => _control.Fsm.GetFsmInt("HP").Value = _hm.hp);

            _control.InsertMethod("Counter Stance", 0, () =>
            {
                _hm.IsInvincible = true;
                if (transform.localScale.x == 1)
                    _hm.InvincibleFromDirection = 8;
                else if (transform.localScale.x == -1)
                    _hm.InvincibleFromDirection = 9;
                
                _spriteFlash.flashFocusHeal();

                Vector2 fxPos = transform.position + Vector3.right * 1.3f * -transform.localScale.x + Vector3.up * 0.1f;
                Quaternion fxRot = Quaternion.Euler(0, 0, -transform.localScale.x * -60);
                GameObject counterFX = Instantiate(FiveKnights.preloadedGO["CounterFX"], fxPos, fxRot);
                counterFX.SetActive(true);
            });

            _control.InsertMethod("Counter End", 0, () => _hm.IsInvincible = false);
            _control.InsertMethod("Counter Slash Antic", 0, () => _hm.IsInvincible = false);

            _control.InsertCoroutine("Countered", 0, () => GameManager.instance.FreezeMoment(0.04f, 0.35f, 0.04f, 0f));
            
            _control.InsertMethod("Dive Land Heavy", 0, () => SpawnShockwaves(1.5f, 35f, 1));

            _control.InsertCoroutine("Dagger Throw", 0, () => SpawnDaggers());

            // Manually spawn beams
            ModifyBeams();
            // Make sure Dryya stays inbounds
            ModifySuper();
			// Play audio clips at the right times
			ModifyAudio();

			GameCameras.instance.cameraShakeFSM.FsmVariables.FindFsmBool("RumblingMed").Value = false;
            AssignFields();
            _hm.OnDeath += DeathHandler;
            On.EnemyDreamnailReaction.RecieveDreamImpact += OnReceiveDreamImpact;
            On.HealthManager.TakeDamage += OnTakeDamage;
        }

        private IEnumerator Start()
        {
            var rb = gameObject.GetComponent<Rigidbody2D>();
            yield return new WaitWhile(()=> rb.velocity.y == 0f);
            yield return new WaitWhile(()=> rb.velocity.y != 0f);
            MusicControl();
        }

        private void MusicControl()
        {
            if (!OWArenaFinder.IsInOverWorld) GGBossManager.Instance.PlayMusic(FiveKnights.Clips["DryyaMusic"], 1f);
            else OWBossManager.PlayMusic(FiveKnights.Clips["DryyaMusic"]);
        }
        
        private void DeathHandler()
        {
            if (!OWArenaFinder.IsInOverWorld) GGBossManager.Instance.PlayMusic(null, 1f);
            CustomWP.wonLastFight = true;
        }

        private GameObject _dreamImpactPrefab;
        private void OnReceiveDreamImpact(On.EnemyDreamnailReaction.orig_RecieveDreamImpact orig, EnemyDreamnailReaction self)
        {
            if (self.name.Contains("Dryya"))
            {
                _dreamNailReaction.SetConvoTitle(_dreamNailDialogue[Random.Range(0, _dreamNailDialogue.Length)]);
                _dreamImpactPrefab.Spawn(transform.position);
            }
            
            orig(self);
        }
        
        private void OnTakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.gameObject.name.Contains("Dryya"))
                _spriteFlash.flashFocusHeal();

            orig(self, hitInstance);
        }
        
        private void AddComponents()
        {
            _deathEffects = gameObject.AddComponent<EnemyDeathEffectsUninfected>();
            _deathEffects.SetJournalEntry(FiveKnights.journalentries["Dryya"]);

            _dreamNailReaction = gameObject.AddComponent<EnemyDreamnailReaction>();
            _dreamNailReaction.enabled = true;
            _dreamNailReaction.SetConvoTitle(_dreamNailDialogue[Random.Range(0, _dreamNailDialogue.Length)]);

            _hitEffects = gameObject.AddComponent<EnemyHitEffectsUninfected>();
            _hitEffects.enabled = true;

            _hm = gameObject.AddComponent<HealthManager>();
            _hm.enabled = false;
            _hm.hp = _hp;

            _spriteFlash = gameObject.AddComponent<SpriteFlash>();

            PlayMakerFSM nailClashTink = FiveKnights.preloadedGO["Slash"].LocateMyFSM("nail_clash_tink");

            foreach(GameObject slash in _slashes)
            {
                PlayMakerFSM pfsm = slash.AddComponent<PlayMakerFSM>();
                foreach(FieldInfo fi in typeof(PlayMakerFSM).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                    fi.SetValue(pfsm, fi.GetValue(nailClashTink));
            }

            _stabFlash.AddComponent<DeactivateAfter2dtkAnimation>();
        }

        private void GetComponents()
        { 
            _sprite = GetComponent<tk2dSprite>();
            _ap = transform.Find("Audio Player").gameObject; 
            EnemyDeathEffects deathEffects = GetComponent<EnemyDeathEffects>();
            deathEffects.corpseSpawnPoint = transform.position + Vector3.up * 2;
            
            deathEffects.SetAttr("corpsePrefab", _corpse);
            deathEffects.SetAttr("corpseFlingSpeed", 25.0f);

            Shader shader = _ogrim.GetComponent<tk2dSprite>().Collection.spriteDefinitions[0].material.shader;
            
            foreach (tk2dSpriteDefinition spriteDef in _sprite.Collection.spriteDefinitions)
                spriteDef.material.shader = shader;
            
            tk2dSprite shockwaveSprite = _diveShockwave.GetComponent<tk2dSprite>();
            foreach (tk2dSpriteDefinition spriteDef in shockwaveSprite.Collection.spriteDefinitions)
                spriteDef.material.shader = Shader.Find("tk2d/BlendVertexColor");
            
            tk2dSprite flashSprite = _stabFlash.GetComponent<tk2dSprite>();
            foreach (tk2dSpriteDefinition spriteDef in flashSprite.Collection.spriteDefinitions)
                spriteDef.material.shader = Shader.Find("tk2d/BlendVertexColor");
        }
        
        private void AssignFields()
        {
            EnemyDeathEffectsUninfected ogrimDeathEffects = _ogrim.GetComponent<EnemyDeathEffectsUninfected>();
            foreach (FieldInfo fi in typeof(EnemyDeathEffectsUninfected).GetFields(BindingFlags.Instance | BindingFlags.Public))
                fi.SetValue(_deathEffects, fi.GetValue(ogrimDeathEffects));
            
            EnemyHitEffectsUninfected ogrimHitEffects = _ogrim.GetComponent<EnemyHitEffectsUninfected>();
            foreach (FieldInfo fi in typeof(EnemyHitEffectsUninfected).GetFields(BindingFlags.Instance | BindingFlags.Public))
                fi.SetValue(_hitEffects, fi.GetValue(ogrimHitEffects));

            HealthManager ogrimHealth = _ogrim.GetComponent<HealthManager>();
            foreach (FieldInfo fi in typeof(HealthManager).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(x => x.Name.Contains("Prefab")))
                fi.SetValue(_hm, fi.GetValue(ogrimHealth));
        }

        private void SpawnShockwaves(float vertScale, float speed, int damage)
        {
            bool[] facingRightBools = {false, true};
            Vector2 pos = transform.position;
            foreach (bool facingRight in facingRightBools)
            {
                GameObject shockwave =
                    Instantiate(_mageLord.GetAction<SpawnObjectFromGlobalPool>("Quake Waves", 0).gameObject.Value);
                PlayMakerFSM shockFSM = shockwave.LocateMyFSM("shockwave");
                shockFSM.FsmVariables.FindFsmBool("Facing Right").Value = facingRight;
                shockFSM.FsmVariables.FindFsmFloat("Speed").Value = speed;
                shockwave.AddComponent<DamageHero>().damageDealt = damage;
                shockwave.SetActive(true);
                shockwave.transform.SetPosition2D(new Vector2(pos.x + (facingRight ? 0.5f : -0.5f), SlamY));
                shockwave.transform.SetScaleX(vertScale);
            }
        }

        private IEnumerator SpawnDaggers()
        {
            float yDist = transform.position.y - HeroController.instance.transform.position.y;
            float xDist = transform.position.x - HeroController.instance.transform.position.x;
            float hypotenuse = Mathf.Sqrt((yDist * yDist) + (xDist * xDist));
            float angle = Mathf.Rad2Deg * Mathf.Asin(xDist / hypotenuse);
            int daggers = 5;
            float startAngle = (180f - ((daggers - 1) * 10f)) - angle;
            GameObject dagger = FiveKnights.preloadedGO["Dagger"];
            for (int i = 0; i < daggers; i++)
            {
                GameObject.Instantiate(dagger, transform.position, Quaternion.Euler(0f, 0f, startAngle)).SetActive(true);
                startAngle += 20f;
                yield return new WaitForSeconds(0.06f);
            }
            yield return new WaitForSeconds(0.5f);
        }

        private void ModifyBeams()
        {
            string[] elegyStates = new string[] { "Beams Slash 1", "Beams Slash 2", "Beams Slash 3", "Beams Slash 4", "Beams Slash 5" };
            foreach(string state in elegyStates)
            {
                _control.InsertMethod(state, () =>
                {
                    GameObject beam = Instantiate(FiveKnights.preloadedGO["Beams"]);

                    // Randomize direction
                    beam.transform.localScale = new Vector3(beam.transform.localScale.x * Random.Range(0, 2) == 0 ? -1f : 1f,
                        beam.transform.localScale.y * Random.Range(0, 2) == 0 ? -1f : 1f,
                        beam.transform.localScale.z);

                    // Randomize offset except for the first one so the player can't just stay still
                    ElegyBeam elegy = beam.AddComponent<ElegyBeam>();
                    elegy.offset = state != "Beams Slash 1" ? new Vector2(Random.Range(-15f, 15f), Random.Range(-7.5f, 7.5f)) : Vector2.zero;

                    _elegyBeams.Add(elegy);
                }, 1);
            }
            _control.InsertMethod("Beams Slash 1", () =>
            {
                _elegyBeams = new List<ElegyBeam>();
            }, 1);

            // Use GameManager to start the coroutine so that it won't linger if she dies
            _control.InsertMethod("Beams Slash End", () => GameManager.instance.StartCoroutine(ActivateBeams()), 1);

            // Do a single elegy beam when doing the cheeky slash
            _control.InsertMethod("Cheeky Collider 1", () =>
            {
                GameObject beam = Instantiate(FiveKnights.preloadedGO["Beams"]);

                beam.transform.localScale = new Vector3(beam.transform.localScale.x * Random.Range(0, 2) == 0 ? -1f : 1f,
                    beam.transform.localScale.y * Random.Range(0, 2) == 0 ? -1f : 1f,
                    beam.transform.localScale.z);

                ElegyBeam elegy = beam.AddComponent<ElegyBeam>();
                elegy.offset = Vector2.zero;
                GameManager.instance.StartCoroutine(ActivateSingleBeam(elegy));
            }, 1);
        }

        private IEnumerator ActivateBeams()
		{
            foreach(ElegyBeam elegy in _elegyBeams)
            {
                elegy.activate = true;
                PlayAudio("Beams Clip", 0.85f, 1.15f, 0.1f);
                yield return new WaitForSeconds(0.05f);
            }
        }

        private IEnumerator ActivateSingleBeam(ElegyBeam elegy)
		{
            yield return new WaitForSeconds(0.5f);
            elegy.activate = true;
            PlayAudio("Beams Clip", 0.85f, 1.15f, 0.1f);
        }

        private void ModifySuper()
        {
            string[] superStates = new string[] { "Ground Stab 1", "Ground Air 1", "Air 1" };
            foreach(string state in superStates)
            {
                _control.InsertMethod(state, () =>
                {
                    if(HeroController.instance.transform.position.x > RightX - 6.5f)
                    {
                        transform.position = new Vector3(RightX - 4.5f, GroundY);
                        transform.localScale = new Vector3(1f, 1f);
                    }
                    if(HeroController.instance.transform.position.x < LeftX + 6.5f)
                    {
                        transform.position = new Vector3(LeftX + 4.5f, GroundY);
                        transform.localScale = new Vector3(-1f, 1f);
                    }
                }, 2);
            }
        }

		private void ModifyAudio()
		{
			_control.InsertMethod("Counter Stance", () => PlayAudio("Counter"), 0);
			_control.InsertMethod("Countered", () => PlayAudio("Counter"), 0);
			_control.InsertMethod("Dagger Throw", () => PlayAudio("Dagger Throw"), 0);
			_control.InsertMethod("Stab", () => PlayAudio("Dash"), 0);
			_control.InsertMethod("Ground Stab 4", () => PlayAudio("Dash Light", 0.85f, 1.15f), 0);
			_control.InsertMethod("Ground Air 11", () => PlayAudio("Dash Light", 0.85f, 1.15f), 0);
			_control.InsertMethod("Super 18", () => PlayAudio("Dash Light", 0.85f, 1.15f), 0);
			_control.InsertMethod("Air 1", () => PlayAudio("Dash Light", 0.85f, 1.15f), 0);
			_control.InsertMethod("Ground Air 4", () => PlayAudio("Dash Light", 0.85f, 1.15f), 0);
			_control.InsertMethod("Dive", () => PlayAudio("Dive"), 0);
			_control.InsertMethod("Dive Land Heavy", () => PlayAudio("Dive Land Hard"), 0);
			_control.InsertMethod("Dive Land Light", () => PlayAudio("Dive Land Soft"), 0);
			_control.InsertMethod("Dive Jump", () => PlayAudio("Jump"), 0);
			_control.InsertMethod("Ground Stab 7", () => PlayAudio("Jump", 0.85f, 1.15f), 0);
			_control.InsertMethod("Dagger Jump", () => PlayAudio("Jump"), 0);
			_control.InsertMethod("Ground Air 7", () => PlayAudio("Jump", 0.85f, 1.15f), 0);
			_control.InsertMethod("Evade Recover", () => PlayAudio("Land"), 0);
			_control.InsertMethod("Super 15", () => PlayAudio("Land", 0.85f, 1.15f), 0);
			_control.InsertMethod("Dagger End", () => PlayAudio("Land"), 0);
			_control.InsertMethod("Counter Collider 1", () => PlayAudio("Slash 1 Clip", 0.85f, 1.15f), 0);
			_control.InsertMethod("Beams Slash 1", () => PlayAudio("Slash 1 Clip", 0.85f, 1.15f), 0);
			_control.InsertMethod("Beams Slash 2", () => PlayAudio("Slash 1 Clip", 0.85f, 1.15f), 0);
			_control.InsertMethod("Beams Slash 3", () => PlayAudio("Slash 1 Clip", 0.85f, 1.15f), 0);
			_control.InsertMethod("Beams Slash 4", () => PlayAudio("Slash 1 Clip", 0.85f, 1.15f), 0);
			_control.InsertMethod("Beams Slash 5", () => PlayAudio("Slash 1 Clip", 0.85f, 1.15f), 0);
			_control.InsertMethod("Slash 1 Collider 1", () => PlayAudio("Slash 1 Clip", 0.85f, 1.15f), 0);
			_control.InsertMethod("Slash 2 Collider 1", () => PlayAudio("Slash 1 Clip", 0.85f, 1.15f), 0);
			_control.InsertMethod("Slash 3 Collider 1", () => PlayAudio("Slash 1 Clip", 0.85f, 1.15f), 0);
			_control.InsertMethod("Cheeky Collider 1", () => PlayAudio("Slash 1 Clip", 0.85f, 1.15f), 0);
		}

		private void PlayAudio(string clip, float minPitch = 1f, float maxPitch = 1f, float delay = 0f)
		{
            IEnumerator Play()
            {
                AudioClip audioClip = _control.Fsm.GetFsmObject(clip).Value as AudioClip;
                yield return new WaitForSeconds(delay);
                GameObject audioPlayerInstance = _ap.Spawn(transform.position, Quaternion.identity);
                AudioSource audio = audioPlayerInstance.GetComponent<AudioSource>();
                audio.outputAudioMixerGroup = HeroController.instance.GetComponent<AudioSource>().outputAudioMixerGroup;
                audio.pitch = Random.Range(minPitch, maxPitch);
                audio.volume = 1f;
                audio.PlayOneShot(audioClip);
                yield return new WaitForSeconds(audioClip.length + 3f);
                Destroy(audioPlayerInstance);
            }
            GameManager.instance.StartCoroutine(Play());
        }

        private void OnDestroy()
        {
            _hm.OnDeath += DeathHandler;
            On.EnemyDreamnailReaction.RecieveDreamImpact -= OnReceiveDreamImpact;
            On.HealthManager.TakeDamage -= OnTakeDamage;
        }

        private void Log(object o)
		{
            if(!FiveKnights.isDebug) return;
            Modding.Logger.Log("[Dryya] " + o);
        }
    }
}
