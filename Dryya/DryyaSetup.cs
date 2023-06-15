using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FiveKnights.BossManagement;
using FiveKnights.Misc;
using SFCore.Utils;
using FrogCore.Ext;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using Random = UnityEngine.Random;
using SendRandomEventV3 = On.HutongGames.PlayMaker.Actions.SendRandomEventV3;

namespace FiveKnights.Dryya
{
    public class DryyaSetup : MonoBehaviour
    {
        private int _hp = 1500;

        private readonly float LeftX = OWArenaFinder.IsInOverWorld ? 422 : 61.0f;
        private readonly float RightX = OWArenaFinder.IsInOverWorld ? 455 : 91.0f;
        private readonly float GroundY = OWArenaFinder.IsInOverWorld ? 101.0837f : 10.625f;
        private readonly float SlamY = OWArenaFinder.IsInOverWorld ? 96.5f : (CustomWP.boss == CustomWP.Boss.All ? 5.7f : 5.9f);
        private readonly int DreamConvoAmount = OWArenaFinder.IsInOverWorld ? 3 : 4;
        private readonly string DreamConvoKey = OWArenaFinder.IsInOverWorld ? "DRYYA_DREAM" : "DRYYA_GG_DREAM";

        private PlayMakerFSM _mageLord;
        private PlayMakerFSM _control;

        private EnemyDeathEffectsUninfected _deathEffects;
        private EnemyDreamnailReaction _dreamNailReaction;
        private EnemyHitEffectsUninfected _hitEffects;
        private ExtraDamageable _extraDamageable;
        private HealthManager _hm;
        private SpriteFlash _spriteFlash;
        private GameObject _corpse;
        private tk2dSprite _sprite;
        private tk2dSpriteAnimator _anim;
        private Rigidbody2D _rb;
        private BoxCollider2D _bc;
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
            _mageLord = FiveKnights.preloadedGO["Mage"].LocateMyFSM("Mage Lord");
            _control = gameObject.LocateMyFSM("Control");

            AddComponents();
            GetComponents();

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
            
            _control.InsertMethod("Dive Land Heavy", 0, () => SpawnShockwaves(1.3f, 25f, 1));

            //_control.InsertCoroutine("Dagger Throw", 0, () => SpawnDaggers());

            // Manually spawn beams
            ModifyBeams();
            // Make sure Dryya stays inbounds
            ModifySuper();
			// Play audio clips at the right times
			ModifyAudio();
            // Modify dagger throw
            ModifyDaggers();

			GameCameras.instance.cameraShakeFSM.FsmVariables.FindFsmBool("RumblingMed").Value = false;
            AssignFields();
            _hm.OnDeath += DeathHandler;
            On.EnemyDreamnailReaction.RecieveDreamImpact += OnReceiveDreamImpact;
            On.HealthManager.TakeDamage += OnTakeDamage;
            On.HutongGames.PlayMaker.Actions.SendRandomEventV3.OnEnter += SendRandomEventV3OnOnEnter;

        }

        private void SendRandomEventV3OnOnEnter(SendRandomEventV3.orig_OnEnter orig, HutongGames.PlayMaker.Actions.SendRandomEventV3 self)
        {
            // Makes it so the dagger throw only happens if close to player
            if (self.State.Name is "Choice P1" or "Choice P2")
            {
                if (HeroController.instance.transform.position.x.Within(transform.position.x, 8f))
                {
                    if (Random.Range(0, 3) == 0)
                    {
                        self.Fsm.Event("DAGGER"); 
                        self.Finish();
                        return;
                    }
                }
            }
            orig(self);
        }

        private IEnumerator Start()
        {
            var rb = gameObject.GetComponent<Rigidbody2D>();
            yield return new WaitWhile(()=> rb.velocity.y == 0f);
            yield return new WaitWhile(()=> rb.velocity.y != 0f);
            MusicControl();

            GameObject area = null;
            foreach(GameObject i in FindObjectsOfType<GameObject>().Where(x => x.name.Contains("Area Title Holder")))
            {
                area = i.transform.Find("Area Title").gameObject;
            }
            area = Instantiate(area);
            area.SetActive(true);
            AreaTitleCtrl.ShowBossTitle(
                this, area, 2f,
                "", "", "",
                "Dryya", "Fierce");
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

            GameManager.instance.AwardAchievement("PALE_COURT_DRYYA_ACH");
        }

        private GameObject _dreamImpactPrefab;
        private void OnReceiveDreamImpact(On.EnemyDreamnailReaction.orig_RecieveDreamImpact orig, EnemyDreamnailReaction self)
        {
            orig(self);
            if (self.name.Contains("Dryya"))
            {
                _spriteFlash.flashDreamImpact();
                _dreamImpactPrefab.Spawn(transform.position);
            }
        }

        private void OnTakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            orig(self, hitInstance);
            if(self.gameObject.name.Contains("Dryya"))
			{
                _spriteFlash.flashFocusHeal();
            }
        }

        private void AddComponents()
        {
            _deathEffects = gameObject.AddComponent<EnemyDeathEffectsUninfected>();
            _deathEffects.SetJournalEntry(FiveKnights.journalEntries["Dryya"]);

            _dreamNailReaction = gameObject.AddComponent<EnemyDreamnailReaction>();
            _dreamNailReaction.enabled = true;
            Vasi.Mirror.SetField(_dreamNailReaction, "convoAmount", DreamConvoAmount);
            _dreamNailReaction.SetConvoTitle(DreamConvoKey);

            _hitEffects = gameObject.AddComponent<EnemyHitEffectsUninfected>();
            _hitEffects.enabled = true;

            _hm = gameObject.AddComponent<HealthManager>();
            _hm.enabled = false;
            _hm.hp = _hp;

            _spriteFlash = gameObject.AddComponent<SpriteFlash>();

            _extraDamageable = gameObject.AddComponent<ExtraDamageable>();
            Vasi.Mirror.SetField(_extraDamageable, "impactClipTable",
                Vasi.Mirror.GetField<ExtraDamageable, RandomAudioClipTable>(_ogrim.GetComponent<ExtraDamageable>(), "impactClipTable"));
            Vasi.Mirror.SetField(_extraDamageable, "audioPlayerPrefab",
                Vasi.Mirror.GetField<ExtraDamageable, AudioSource>(_ogrim.GetComponent<ExtraDamageable>(), "audioPlayerPrefab"));

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
            _anim = GetComponent<tk2dSpriteAnimator>();
            _rb = GetComponent<Rigidbody2D>();
            _bc = GetComponent<BoxCollider2D>();
            _ap = transform.Find("Audio Player").gameObject;

            EnemyDeathEffects deathEffects = GetComponent<EnemyDeathEffects>();
            deathEffects.corpseSpawnPoint = transform.position + Vector3.up * 2;

            GameObject corpse = Instantiate(_corpse);
            corpse.SetActive(false);
            corpse.AddComponent<DryyaCorpse>().deathClip = _control.Fsm.GetFsmObject("VoiceDeath").Value as AudioClip;
            deathEffects.SetAttr("corpsePrefab", corpse);
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
            float startAngle = 180f - angle;// - 2 * 8f;
            GameObject dagger = FiveKnights.preloadedGO["Dagger"];
            
            for (int i = -6; i < 7; i++)
            {
                if (i == 1 || i == 2 || i == 3 || i == -1 || i == -2 || i == -3) continue;
                PlayAudio("Dagger Throw");
                GameObject dg = Instantiate(dagger, transform.position, Quaternion.Euler(0f, 0f, startAngle + 5 * i));
                dg.SetActive(true);

                yield return new WaitForSeconds(0.01f);
            }
        }

        private void ModifyDaggers()
        {
            var state = _control.AddState("ModifiedDaggerThrow");
            _control.ChangeTransition("Choice P1", "DAGGER", "ModifiedDaggerThrow");
            _control.ChangeTransition("Choice P2", "DAGGER", "ModifiedDaggerThrow");

            _control.GetAction<HutongGames.PlayMaker.Actions.SendRandomEventV3>("Choice P1", 0).weights[3] = 0;
            _control.GetAction<HutongGames.PlayMaker.Actions.SendRandomEventV3>("Choice P1", 0).eventMax[3] = 0;
            _control.GetAction<HutongGames.PlayMaker.Actions.SendRandomEventV3>("Choice P1", 0).missedMax[3] = 999;
            _control.GetAction<HutongGames.PlayMaker.Actions.SendRandomEventV3>("Choice P2", 1).weights[4] = 0;
            _control.GetAction<HutongGames.PlayMaker.Actions.SendRandomEventV3>("Choice P2", 1).eventMax[4] = 0;
            _control.GetAction<HutongGames.PlayMaker.Actions.SendRandomEventV3>("Choice P2", 1).missedMax[4] = 999;

            Vasi.FsmUtil.AddCoroutine(state, DaggerThrow);
            
            IEnumerator DaggerThrow()
            {
                _bc.isTrigger = true;
                float signX = Mathf.Sign(transform.localScale.x);
                _rb.velocity = new Vector2(0f, 0f);
                _rb.gravityScale = 0f;
                _rb.isKinematic = true;
                transform.position -= new Vector3(0f, 1.7f, 0f);
                _anim.Play("Throw");
                yield return new WaitWhile(() => _anim.CurrentFrame < 3);
                _anim.Pause();
                yield return new WaitForSeconds(0.15f);
                _anim.Resume();
                yield return new WaitWhile(() => _anim.CurrentFrame < 5);
                _anim.ClipFps = 24;
                PlayVoice(true);
                _rb.velocity = new Vector2(signX * 55f, 25f);
                yield return new WaitWhile(() => _anim.CurrentFrame < 8);
                _rb.velocity = new Vector2(signX * 30f, _rb.velocity.y);
                yield return new WaitWhile(() => _anim.CurrentFrame < 9);
                _rb.gravityScale = 3f;
                _rb.isKinematic = false;
                yield return new WaitWhile(() => _anim.CurrentFrame < 10);
                _anim.ClipFps = 12;
                StartCoroutine(SpawnDaggers());
                yield return new WaitWhile(() => transform.position.y > GroundY - 1.7f);
                transform.position = new Vector3(transform.position.x, GroundY);
                _bc.isTrigger = false;
                _rb.velocity = new Vector2(0f, 0f);
                _rb.gravityScale = 0f;
                _control.SetState("Idle");
            }
        }

        private void Update()
        {
            Vector2 pos = transform.position;
            if (pos.x > RightX && _rb.velocity.x > 0)
            {
                transform.position = new Vector3(RightX, pos.y);
                _rb.velocity = new Vector2(0f, _rb.velocity.y);
            }
            else if (pos.x < LeftX && _rb.velocity.x < 0)
            {
                transform.position = new Vector3(LeftX, pos.y);
                _rb.velocity = new Vector2(0f, _rb.velocity.y);
            }
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
                PlayAudio("Beams Clip", 0.85f, 1.15f, 1f, 0.1f);
                yield return new WaitForSeconds(0.05f);
            }
        }

        private IEnumerator ActivateSingleBeam(ElegyBeam elegy)
		{
            yield return new WaitForSeconds(0.5f);
            elegy.activate = true;
            PlayAudio("Beams Clip", 0.85f, 1.15f, 1f, 0.1f);
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
            // Voice
            _control.InsertMethod("Backstep 1", () => PlayVoice(false), 0);
            _control.InsertMethod("Cheeky Collider 1", () => PlayVoice(false), 0);
            _control.InsertMethod("Counter Collider 1", () => PlayVoice(false), 0);
            _control.InsertMethod("Dagger Jump", () => PlayVoice(true), 0);
            _control.InsertMethod("Dive", () => PlayVoice(false), 0);
            _control.InsertMethod("Slash 1 Collider 1", () => PlayVoice(true), 0);
            _control.InsertMethod("Stab", () => PlayVoice(false), 0);
			_control.InsertMethod("Beams Slash 1", () => PlayAudio("VoiceBeams" + Random.Range(1, 3)), 0);
			_control.InsertMethod("Super Start 3", () => PlayVoice(false), 0);
            _control.InsertMethod("Ground Stab 4", () => PlayVoice(false), 0);
            _control.InsertMethod("Ground Air 4", () => PlayVoice(false), 0);
            _control.InsertMethod("Air 1", () => PlayVoice(false), 0);

            // SFX
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

        private void PlayVoice(bool alt)
		{
            string clip = "Voice";
            if(alt)
            {
                clip += "Alt" + Random.Range(1, 4);
            }
            else
            {
                clip += Random.Range(1, 6);
            }
            PlayAudio(clip, 1f, 1f);
		}

		private void PlayAudio(string clip, float minPitch = 1f, float maxPitch = 1f, float volume = 1f, float delay = 0f)
		{
            AudioClip audioClip = _control.Fsm.GetFsmObject(clip).Value as AudioClip;
            this.PlayAudio(audioClip, volume, maxPitch - minPitch);
        }

        private void OnDestroy()
        {
            _hm.OnDeath += DeathHandler;
            On.EnemyDreamnailReaction.RecieveDreamImpact -= OnReceiveDreamImpact;
            On.HealthManager.TakeDamage -= OnTakeDamage;
            On.HutongGames.PlayMaker.Actions.SendRandomEventV3.OnEnter -= SendRandomEventV3OnOnEnter;
        }

        private void Log(object o)
		{
            if(!FiveKnights.isDebug) return;
            Modding.Logger.Log("[Dryya] " + o);
        }
    }
}
