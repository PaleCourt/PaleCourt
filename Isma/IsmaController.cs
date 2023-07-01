using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FiveKnights.BossManagement;
using FiveKnights;
using FrogCore.Ext;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using SFCore.Utils;
using TMPro;
using UnityEngine;
using Vasi; 
using Logger = Modding.Logger;
using Random = System.Random;

namespace FiveKnights.Isma
{
    public class IsmaController : MonoBehaviour
    {
        //Note: Dreamnail code was taken from Jngo's code :)
        public bool onlyIsma;

        private bool _attacking;
        private HealthManager _hm;
        private BoxCollider2D _bc;
        private HealthManager _hmDD;
        private Rigidbody2D _rb;
        private Rigidbody2D _rbDD;
        private EnemyDreamnailReaction _dreamNailReaction;
        private ExtraDamageable _extraDamageable;
        private EnemyHitEffectsUninfected _hitEffects;
        private EnemyDeathEffectsUninfected _deathFx;
        private EnemyDeathEffectsUninfected _ddDeathFx;
        private GameObject _dnailEff;
        private SpriteRenderer _sr;
        private GameObject _target;
        private GameObject dd;
        private PlayMakerFSM _ddFsm;
        private Animator _anim;
        private Texture _acidTexture;
        private List<AudioClip> _randVoice => FiveKnights.Clips.Values.Where(x => x != null && x.name.Contains("IsmaAudAtt")).ToList();
        private System.Random _rand;
        private int _healthPool;
        private bool _waitForHitStart;

        private readonly float LeftX = OWArenaFinder.IsInOverWorld ? 105f : 60.3f;
        private readonly float RightX = OWArenaFinder.IsInOverWorld ? 135f : 91.7f;
        private readonly float GroundY = OWArenaFinder.IsInOverWorld ||
            CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim ? 5.9f : 6.67f;
        private float MiddleX => (LeftX + RightX) / 2;
        private readonly int DreamConvoAmount = OWArenaFinder.IsInOverWorld ? 4 : (CustomWP.boss == CustomWP.Boss.All ? 5 : 3);
        private readonly string DreamConvoKey = OWArenaFinder.IsInOverWorld ? "ISMA_DREAM" : "ISMA_GG_DREAM";

        private readonly int MaxHP = CustomWP.lev > 0 ? 1650 : 1450;
        private readonly int WallHP = CustomWP.lev > 0 ? 1150 : 1050;
        private readonly int SpikeHP = CustomWP.lev > 0 ? 600 : 550;
        private readonly int MaxHPDuo = CustomWP.lev > 0 ? 1800 : 1600;
        private readonly int WallHPDuo = CustomWP.lev > 0 ? 1300 : 1150;
        private readonly int SpikeHPDuo = CustomWP.lev > 0 ? 700 : 600;
        private readonly int FrenzyHP = 250;

        private const float IDLE_TIME = 0f; // can be changed if we want to later ig???
        private const int GULKA_DAMAGE = 20;

        private bool _isDead;
        private bool _isIsmaHitLast;
        private bool _usingThornPillars;
        private bool _ogrimEvaded;
        private Coroutine _wallsCoro;

        public static float offsetTime;
        public static bool killAllMinions;
        public static bool eliminateMinions;
        public bool introDone;

        private void Awake()
        {
            _hm = gameObject.AddComponent<HealthManager>();
            _sr = gameObject.GetComponent<SpriteRenderer>();
            _anim = gameObject.GetComponent<Animator>();
            _bc = gameObject.GetComponent<BoxCollider2D>();
            _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            dd = FiveKnights.preloadedGO["WD"];
            _hmDD = dd.GetComponent<HealthManager>();
            _rbDD = dd.GetComponent<Rigidbody2D>();
            _ddFsm = dd.LocateMyFSM("Dung Defender");
			EnemyHPBarImport.DisableHPBar(dd);

            _extraDamageable = gameObject.AddComponent<ExtraDamageable>();
            Mirror.SetField(_extraDamageable, "impactClipTable", 
                Mirror.GetField<ExtraDamageable, RandomAudioClipTable>(dd.GetComponent<ExtraDamageable>(), "impactClipTable"));
            Mirror.SetField(_extraDamageable, "audioPlayerPrefab", 
                Mirror.GetField<ExtraDamageable, AudioSource>(dd.GetComponent<ExtraDamageable>(), "audioPlayerPrefab"));

            // Ogrim's dream nail dialogue is set in GGBossManager
            _dnailEff = dd.GetComponent<EnemyDreamnailReaction>().GetAttr<EnemyDreamnailReaction, GameObject>("dreamImpactPrefab");

            _dreamNailReaction = gameObject.AddComponent<EnemyDreamnailReaction>();
            _dreamNailReaction.enabled = true;
            Mirror.SetField(_dreamNailReaction, "convoAmount", DreamConvoAmount);
            _dreamNailReaction.SetConvoTitle(DreamConvoKey);

            _hitEffects = gameObject.AddComponent<EnemyHitEffectsUninfected>();
            _hitEffects.enabled = true;
            _deathFx = gameObject.AddComponent<EnemyDeathEffectsUninfected>();

            gameObject.AddComponent<Flash>();

            _rand = new Random();
            _healthPool = 9999; // Just a dummy health value while waiting for onlyIsma to be set

            EnemyPlantSpawn.isPhase2 = false;
            EnemyPlantSpawn.FoolCount = EnemyPlantSpawn.PillarCount = EnemyPlantSpawn.TurretCount = 0;
            killAllMinions = eliminateMinions = false;
        }

        private IEnumerator Start()
        {
            Log("Begin Isma");
            yield return null;
            _healthPool = onlyIsma ? MaxHP : MaxHPDuo;
            offsetTime = 0f;

            if (CustomWP.boss == CustomWP.Boss.All)
            {
                StartCoroutine(SilLeave());
                yield return new WaitForSeconds(0.15f);
            }
            if (onlyIsma)
            {
                GameCameras.instance.cameraShakeFSM.FsmVariables.FindFsmBool("RumblingMed").Value = false;
                yield return new WaitForSeconds(0.8f);
            }

            // Create seed columns for Godhome arena
            if(!OWArenaFinder.IsInOverWorld)
            {
                GameObject sc = new GameObject();
                sc.name = "SeedCols";

                GameObject sf = new GameObject();
                sf.name = "SeedFloor";
                sf.transform.position = new Vector3(73.3f, GroundY - 0.8f, 0f);
                BoxCollider2D sfcol = sf.AddComponent<BoxCollider2D>();
                sfcol.offset = new Vector2(3f, 0f);
                sfcol.size = new Vector2(19f, 1f);
                sf.transform.parent = sc.transform;

                if(onlyIsma)
				{
                    GameObject sr = new GameObject();
                    sr.name = "SeedSideR";
                    sr.transform.position = new Vector3(92.4f, GroundY + 6.8f, 0f);
                    BoxCollider2D srcol = sr.AddComponent<BoxCollider2D>();
                    srcol.size = new Vector2(1f, 7f);
                    sr.transform.parent = sc.transform;

                    GameObject sl = new GameObject();
                    sl.name = "SeedSideL";
                    sl.transform.position = new Vector3(59.4f, GroundY + 6.8f, 0f);
                    BoxCollider2D slcol = sl.AddComponent<BoxCollider2D>();
                    slcol.size = new Vector2(1f, 7f);
                    sl.transform.parent = sc.transform;
                }
			}

            // Save vanilla acid texture
            tk2dSpriteDefinition def = FiveKnights.preloadedGO["AcidSpit"].GetComponentInChildren<tk2dSprite>().GetCurrentSpriteDef();
            _acidTexture = def.material.mainTexture;
            def.material.mainTexture = FiveKnights.SPRITES["acid_b"].texture;

            foreach(Transform sidecols in GameObject.Find("SeedCols").transform)
            {
                sidecols.gameObject.AddComponent<EnemyPlantSpawn>();
                sidecols.gameObject.layer = (int)GlobalEnums.PhysLayers.ENEMY_DETECTOR;
            }

            gameObject.layer = 11;
            _target = HeroController.instance.gameObject;
            if(!onlyIsma) PositionIsma();
            else transform.position = new Vector3(LeftX + (RightX - LeftX) / 1.6f, GroundY, 1f);
            FaceHero();
            _bc.enabled = false;

            PlayMakerFSM roarFSM = HeroController.instance.gameObject.LocateMyFSM("Roar Lock");
            if(OWArenaFinder.IsInOverWorld)
            {
                roarFSM.GetFsmGameObjectVariable("Roar Object").Value = gameObject;
                roarFSM.SendEvent("ROAR ENTER");
            }
            On.HealthManager.TakeDamage += HealthManagerTakeDamage;
			On.HealthManager.Die += HealthManagerDie;
            On.SpellFluke.DoDamage += SpellFlukeOnDoDamage;
			On.EnemyDreamnailReaction.RecieveDreamImpact += OnReceiveDreamImpact;
			On.HutongGames.PlayMaker.Actions.ReceivedDamage.OnEnter += MarkDungBalls;
            AssignFields(gameObject);
            _ddFsm.FsmVariables.FindFsmInt("Rage HP").Value = 801;
            _hm.hp = _hmDD.hp = onlyIsma ? MaxHP : MaxHPDuo;
			EnemyHPBarImport.MarkAsBoss(gameObject);

            // Wait for a bit while Ogrim is down
            if(!OWArenaFinder.IsInOverWorld && !onlyIsma)
            {
                ToggleIsma(false);
                yield return new WaitForSeconds(1f);
                ToggleIsma(true);
            }

            _anim.Play("Apear"); // Yes I know it's "appear," I don't feel like changing the assetbundle buddo
            gameObject.transform.position = new Vector2(gameObject.transform.GetPositionX(), GroundY + 8f);
            _rb.velocity = new Vector2(0f, -40f);
            yield return new WaitWhile(() => gameObject.transform.GetPositionY() > GroundY);
            GameCameras.instance.cameraShakeFSM.SendEvent("SmallShake");
            _rb.velocity = new Vector2(0f, 0f);
            gameObject.transform.position = new Vector2(gameObject.transform.GetPositionX(), GroundY);
            yield return new WaitWhile(() => _anim.IsPlaying());

            if(OWArenaFinder.IsInOverWorld)
            {
                yield return new WaitForSeconds(0.75f);
                roarFSM.SendEvent("ROAR EXIT");
            }

			StartCoroutine("Start2");
            if(OWArenaFinder.IsInOverWorld)
            {
                OWBossManager.PlayMusic(FiveKnights.Clips["LoneIsmaIntro"]);
                yield return new WaitForSecondsRealtime(FiveKnights.Clips["LoneIsmaIntro"].length);
                OWBossManager.PlayMusic(FiveKnights.Clips["LoneIsmaLoop"]);
            }
            else if(onlyIsma)
            {
                GGBossManager.Instance.PlayMusic(FiveKnights.Clips["LoneIsmaIntro"]);
                yield return new WaitForSecondsRealtime(FiveKnights.Clips["LoneIsmaIntro"].length);
                GGBossManager.Instance.PlayMusic(FiveKnights.Clips["LoneIsmaLoop"]);
            }
            else
			{
                yield return new WaitForSeconds(1f);
                GGBossManager.Instance.PlayMusic(FiveKnights.Clips["OgrismaMusic"], 1f);
            }
        }

		private IEnumerator Start2()
        {
            float dir = FaceHero();
            introDone = true;
            GameObject area = null;
            foreach (GameObject i in FindObjectsOfType<GameObject>().Where(x => x.name.Contains("Area Title Holder")))
            {
                area = i.transform.Find("Area Title").gameObject;
            }
            if(!onlyIsma)
            {
                if(transform.position.x > dd.transform.position.x) StartCoroutine(ChangeIntroText(area, "Isma", "", "Kindly", true));
                else StartCoroutine(ChangeIntroText(area, "Ogrim", "", "Loyal", true));
            }
            
            GameObject area2 = Instantiate(area);
            area2.SetActive(true);
            if(!onlyIsma && transform.position.x > dd.transform.position.x)
            {
                AreaTitleCtrl.ShowBossTitle(this, area2, 2f, "", "", "", "Ogrim", "Loyal");
            }
            else
            {
                AreaTitleCtrl.ShowBossTitle(this, area2, 2f, "", "", "", "Isma", "Kindly");
            }

            if(onlyIsma) _bc.enabled = true;
            _waitForHitStart = true;
            yield return new WaitForSeconds(0.7f);
            _anim.Play("Bow");
            this.PlayAudio(FiveKnights.Clips["IsmaAudBow"], 1f);
            yield return new WaitForSeconds(0.05f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            yield return new WaitForSeconds(1f);
            _bc.enabled = false;
            _anim.Play("EndBow");
            yield return null;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
            _waitForHitStart = false;
            _rb.velocity = new Vector2(dir * 20f, 10f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            _rb.velocity = Vector2.zero;
            ToggleIsma(false);
            _attacking = false;
			_wallsCoro = StartCoroutine(SpawnWalls());

            if(onlyIsma)
            {
                StartCoroutine(AttackChoice());
            }
			else
			{
                StartCoroutine(DuoAttacks());
            }
        }

        private Action _prev;

        private IEnumerator AttackChoice()
        {
            // Always start with Seed Bomb
            yield return WaitToAttack();
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 0.8f) + offsetTime);
            _prev = SeedBomb;
            _prev.Invoke();

            while(true)
            {
                yield return new WaitWhile(() => _attacking);
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 0.8f) + offsetTime);
                _attacking = true;

                if(spawningWalls)
				{
                    Log("Using Seed Bomb and spawning walls");
                    _prev = SeedBomb;
                    _prev.Invoke();
                    if(!usedAgony)
					{
                        yield return new WaitWhile(() => _attacking);
                        yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 0.8f) + offsetTime);
                        _attacking = true;
                        Log("Doing Agony");
                        StartCoroutine(Agony());
                    }
                    continue;
                }
                if(startedIsmaDeath)
				{
                    StartCoroutine(IsmaLoneDeath());
                    break;
				}

                int[] weights = new int[] { 0, 2, 2 };
                Action[] attacks = new Action[] { SeedBomb, AirFist, WhipAttack };

                if(!_wallActive)
				{
                    weights[0] += Math.Max(EnemyPlantSpawn.MAX_FOOL - EnemyPlantSpawn.FoolCount - 1, 0);
                    weights[0] += EnemyPlantSpawn.MAX_TURRET - EnemyPlantSpawn.TurretCount;
                }
                else
				{
                    weights[0] += EnemyPlantSpawn.MAX_PILLAR - EnemyPlantSpawn.PillarCount;
                }
                if(_prev.Method.Name == "SeedBomb")
                {
                    weights[0]--;
                    weights[1]++;
                }
                if(_prev.Method.Name == "AirFist") weights[1]--;
                if(_prev.Method.Name == "WhipAttack") weights[2]--;

                List<Action> attackTable = new List<Action>();

                for(int i = 0; i < weights.Length; i++)
				{
                    for(int j = 0; j < weights[i]; j++)
                    {
                        attackTable.Add(attacks[i]);
                    }
				}

                _prev = attackTable[UnityEngine.Random.Range(0, attackTable.Count)];

                Log("Doing: " + _prev.Method.Name);
                _prev.Invoke();
            }
        }

        private IEnumerator DuoAttacks()
		{
            EnemyPlantSpawn.isPhase2 = true;

			#region WD FSM edits

            // Add short delay after dung toss
            _ddFsm.GetAction<SendEventByName>("After Throw?", 0).delay = 0.2f;
            _ddFsm.InsertAction("After Throw?", _ddFsm.GetAction<Tk2dPlayAnimation>("Idle", 0), 0);

            // Mark balls thrown by WD
            _ddFsm.InsertMethod("Throw 1", () => _ddFsm.FsmVariables.FindFsmGameObject("Dung Ball").Value.name = "Ogrim Thrown Ball", 2);

            // Increase delay after ground slam
            _ddFsm.GetAction<Wait>("G Slam Recover", 0).time = 1.2f;

            // Decrease screenshake while WD is underground
            _ddFsm.GetAction<SetFsmBool>("Tunneling R", 0).variableName.Value = "RumblingSmall";
            _ddFsm.GetAction<SetFsmBool>("Erupt Antic", 3).variableName.Value = "RumblingSmall";
            _ddFsm.GetAction<SetFsmBool>("Erupt Antic R", 2).variableName.Value = "RumblingSmall";
            _ddFsm.GetAction<SetFsmBool>("Erupt Out First", 1).variableName.Value = "RumblingSmall";
            _ddFsm.GetAction<SetFsmBool>("Erupt Out", 2).variableName.Value = "RumblingSmall";

            // WD rolls before using Ground Slam if in the middle of the arena
            void EvadeBeforeAttack()
			{
                if(!_ogrimEvaded && _wallActive && (FastApproximately(dd.transform.GetPositionX(), MiddleX, 4f) ||
                    (dd.transform.GetPositionX() < MiddleX - 4f && dd.transform.GetPositionX() - _target.transform.GetPositionX() > 0f) ||
                    (dd.transform.GetPositionX() > MiddleX + 4f && dd.transform.GetPositionX() - _target.transform.GetPositionX() < 0f)))
                {
                    _ddFsm.SetState("Evade Dir");
                    _ddFsm.GetAction<SendRandomEvent>("After Evade", 0).weights[0].Value = 0f;
                    _ogrimEvaded = true;
                }
                else
                {
                    _ddFsm.GetAction<SendRandomEvent>("After Evade", 0).weights[0].Value = 0.5f;
                    _ogrimEvaded = false;
                }
            }
            _ddFsm.InsertMethod("G Slam Antic", EvadeBeforeAttack, 0);

            // WD burrows for longer
            _ddFsm.GetAction<RandomFloat>("Timer", 1).min.Value = 2.1f;
			_ddFsm.GetAction<RandomFloat>("Timer", 1).max.Value = 2.1f;

            // Make WD bounce around the arena at a consistent speed
            _ddFsm.InsertMethod("RJ Launch", 6, () =>
            {
                _ddFsm.FsmVariables.FindFsmFloat("Throw Speed Crt").Value = 12f;
            });

            // Add anticheese to bounce by adding to a counter when knight hits WD and make him dive if he's been juggled too long
            _ddFsm.InsertMethod("Ball Hit Up", () =>
            {
                _ddFsm.FsmVariables.FindFsmInt("Bounces").Value--;
            }, 0);
            _ddFsm.InsertCoroutine("RJ In Air", 8, CheckPos);
            IEnumerator CheckPos()
            {
                yield return new WaitUntil(() => (!_ddFsm.FsmVariables.FindFsmBool("Still Bouncing").Value &&
                        _ddFsm.FsmVariables.FindFsmBool("Air Dive Height").Value) ||
                    (FastApproximately(dd.transform.position.y, 13.5f, 0.75f) && _rbDD.velocity.y > 0f &&
                        _ddFsm.FsmVariables.FindFsmInt("Bounces").Value < -3) ||
                    startedIsmaRage);
                _ddFsm.SendEvent("AIR DIVE");
            }
            #endregion

            // Dung Strike - Always
            StartCoroutine(DungStrike());

            // Vine Whip - Burrowing
            _ddFsm.InsertMethod("Timer", () =>
            {
                if(startedIsmaRage) return;
                IEnumerator WaitForWhipOrBomb()
                {
                    yield return WaitToAttack();
                    if(spawningWalls) SeedBomb();
                    else WhipAttack();
                }
                StartCoroutine(WaitForWhipOrBomb());
            }, 0);

            // Seed Bomb - Bouncing
            _ddFsm.InsertMethod("RJ Launch", () =>
            {
                IEnumerator WaitForBomb()
                {
                    yield return WaitToAttack();
                    SeedBomb();
                }
                StartCoroutine(WaitForBomb());
            }, 0);

            // Acid Spray/Air Fist - Spike slam
            _ddFsm.InsertMethod("G Slam", () =>
            {
                IEnumerator WaitForAirFist()
                {
                    yield return WaitToAttack();
                    AirFist();
                }
                StartCoroutine(WaitForAirFist());
            }, 0);

            yield return new WaitUntil(() => _wallActive);

            // Add delay before spawning spike wave after bouncing
            _ddFsm.GetAction<Wait>("AD In", 0).time.Value = 0.4f;

			// Thorn Pillars - Bouncing
			_ddFsm.RemoveAction("RJ Launch", 0);
            _ddFsm.InsertMethod("RJ Launch", () =>
            {
                _usingThornPillars = false;
				IEnumerator WaitForThornPillars()
				{
                    yield return WaitToAttack();
                    ThornPillars();
				}
				StartCoroutine(WaitForThornPillars());
			}, 0);
            _ddFsm.InsertMethod("RJ Speed Adjust", () => _usingThornPillars = true, 0);

            // Ogrim Strike - After bouncing
            StartCoroutine(OgrimStrike());

            // Agony - Spike wave
            _ddFsm.InsertMethod("Under", () =>
            {
                IEnumerator WaitForAgony()
                {
                    yield return new WaitForSeconds(0.6f);
                    yield return WaitToAttack();
                    yield return Agony();
                }
                StartCoroutine(WaitForAgony());
            }, 0);
        }

		private bool _wallActive;
        private GameObject wallR;
        private GameObject wallL;

        private IEnumerator SpawnWalls()
        {
            if(!startedOgrimRage && !startedIsmaRage)
            {
                yield return new WaitWhile(() => _healthPool > (onlyIsma ? WallHP : WallHPDuo));
                if(onlyIsma)
                {
                    yield return new WaitWhile(() => _attacking);
                    Log("Queueing Seed Bomb");
                    spawningWalls = true;
                    yield return new WaitWhile(() => spawningWalls);
                    yield return new WaitForSeconds(1f);
                    EnemyPlantSpawn spawner = GameObject.Find("SeedFloor").GetComponent<EnemyPlantSpawn>();
                    spawner.Phase2Spawn();
                }
                else
                {
                    yield return new WaitUntil(() => _ddFsm.ActiveStateName == "Idle");
                    _ddFsm.InsertMethod("Rage?", () => _ddFsm.SetState("TD Set"), 0);
                    spawningWalls = true;
                    yield return new WaitUntil(() => _ddFsm.ActiveStateName.Contains("Tunneling"));
                    _ddFsm.RemoveAction("Rage?", 0);
                    yield return new WaitForSeconds(0.3f);
                    dd.LocateMyFSM("Constrain X").FsmVariables.FindFsmFloat("Edge R").Value = 88f;
                    dd.LocateMyFSM("Constrain X").FsmVariables.FindFsmFloat("Edge L").Value = 64f;
                    _ddFsm.FsmVariables.FindFsmFloat("Dolphin Max X").Value = 88f;
                    _ddFsm.FsmVariables.FindFsmFloat("Dolphin Min X").Value = 64f;
                    _ddFsm.FsmVariables.FindFsmFloat("Max X").Value = 85f;
                    _ddFsm.FsmVariables.FindFsmFloat("Min X").Value = 67f;
                    yield return new WaitWhile(() => spawningWalls);
                    yield return new WaitForSeconds(1f);
                }
                EnemyPlantSpawn.isPhase2 = true;
                killAllMinions = true;
                yield return new WaitForSeconds(0.1f);
                killAllMinions = false;

                _wallActive = true;

                wallR = Instantiate(FiveKnights.preloadedGO["Wall"]);
                wallR.transform.localScale = new Vector3(wallR.transform.localScale.x * -1f, wallR.transform.localScale.y, wallR.transform.localScale.z);
                wallL = Instantiate(FiveKnights.preloadedGO["Wall"]);
                GameObject frontWR = wallR.transform.Find("FrontW").gameObject;
                GameObject frontWL = wallL.transform.Find("FrontW").gameObject;
                frontWR.layer = 8;
                frontWL.layer = 8;
                wallR.transform.position = new Vector3(RightX - 2.5f, GroundY, 0.1f);
                wallL.transform.position = new Vector3(LeftX + 2.5f, GroundY, 0.1f);
                Animator anim = frontWR.GetComponent<Animator>();
                this.PlayAudio(FiveKnights.Clips["IsmaAudWallGrow"], 1f);
                yield return new WaitWhile(() => anim.GetCurrentFrame() < 3);
                wallR.transform.Find("Petal").gameObject.SetActive(true);
                wallL.transform.Find("Petal").gameObject.SetActive(true);
                Vector2 hPos = _target.transform.position;
                if(hPos.x > RightX - 6f) _target.transform.position = new Vector2(RightX - 6f, hPos.y);
                else if(hPos.x < LeftX + 6f) _target.transform.position = new Vector2(LeftX + 6f, hPos.y);

                yield return new WaitWhile(() => _healthPool > (onlyIsma ? SpikeHP : SpikeHPDuo));

                if(onlyIsma)
                {
                    yield return new WaitWhile(() => _attacking);
                    spawningWalls = true;
                    yield return new WaitWhile(() => spawningWalls);
                    yield return new WaitForSeconds(1f);
                }
                else
				{
                    yield return new WaitUntil(() => _ddFsm.ActiveStateName == "Idle");
                    _ddFsm.InsertMethod("Rage?", () => _ddFsm.SetState("TD Set"), 0);
                    spawningWalls = true;
                    yield return new WaitUntil(() => _ddFsm.ActiveStateName.Contains("Tunneling"));
                    _ddFsm.RemoveAction("Rage?", 0);
                    yield return new WaitForSeconds(0.3f);
                    yield return new WaitWhile(() => spawningWalls);
                    yield return new WaitForSeconds(1f);
                }

                foreach(GameObject wall in new[] { wallR, wallL })
                {
                    GameObject spike = wall.transform.Find("Spike").gameObject;
                    GameObject spikeFront = spike.transform.Find("Front").gameObject;
                    spikeFront.layer = 17;
                    spikeFront.AddComponent<DamageHero>().damageDealt = 1;

                    spikeFront.AddComponent<PlantHitFx>().hitSound = FiveKnights.Clips["IsmaAudWallHit"];
                    spikeFront.AddComponent<NonBouncer>();
                    spike.SetActive(true);
                    this.PlayAudio(FiveKnights.Clips["IsmaAudWallGrow"], 1f);
                }

                eliminateMinions = false;
            }
            yield return new WaitWhile(() => !eliminateMinions);
            
            foreach (GameObject wall in new[] {wallL, wallR})
            {
                if(wall == null) continue;
                wall.transform.Find("FrontW").gameObject.GetComponent<Animator>().Play("WallFrontDestroy");
                wall.transform.Find("Back").gameObject.GetComponent<Animator>().Play("WallBackDestroy");
                wall.transform.Find("Petal").Find("P1").gameObject.GetComponent<Animator>().Play("PetalOneDest");
                wall.transform.Find("Petal").Find("P3").gameObject.GetComponent<Animator>().Play("PetalManyDest");
                wall.transform.Find("Spike").Find("Back").gameObject.GetComponent<Animator>().Play("SpikeBackDest");
                wall.transform.Find("Spike").Find("Middle").gameObject.GetComponent<Animator>().Play("SpikeMiddleDest");
                wall.transform.Find("Spike").Find("Front").gameObject.GetComponent<Animator>().Play("SpikeFrontDest");
                wall.transform.Find("FrontW").gameObject.GetComponent<BoxCollider2D>().enabled = false;
                wall.transform.Find("Spike").Find("Front").gameObject.GetComponent<BoxCollider2D>().enabled = false;
            }
        }

        private bool firstBomb = true;
        private bool spawningWalls = false;

        private void SeedBomb()
        {
            if(!onlyIsma) firstBomb = false;
            float dir = 0f;
            IEnumerator BombThrow()
            {
                float heroX = _target.transform.GetPositionX();
                float ismaX = heroX - MiddleX > 0f ? LeftX + 8f : RightX - 8f;
                if (_wallActive) ismaX = heroX - MiddleX > 0f ? LeftX + 11f : RightX - 11f;
                transform.position = new Vector2(ismaX, GroundY);
                dir = FaceHero();
                _rb.velocity = new Vector2(-20f * dir, 0f);
                ToggleIsma(true);
                _anim.Play("ThrowBomb", -1, 0f);
                PlayVoice();
                yield return new WaitForEndOfFrame();
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
                _anim.enabled = false;
                _rb.velocity = new Vector2(0, 0f);
                yield return new WaitForSeconds(0.3f);
                _anim.enabled = true;

                GameObject bombRe = transform.Find("Bomb").gameObject;
                GameObject seedRe = bombRe.transform.Find("Seed").gameObject;
                GameObject bomb = Instantiate(bombRe, bombRe.transform.position, bombRe.transform.rotation);
                GameObject seed = Instantiate(seedRe, seedRe.transform.position, seedRe.transform.rotation);
                Rigidbody2D rb = bomb.GetComponent<Rigidbody2D>();
                Animator anim = bomb.GetComponent<Animator>();
                bomb.transform.localScale *= 1.4f;
                bomb.SetActive(false);
                Vector3 scale = seed.transform.localScale;
                scale *= 2f;
                scale.x *= -1f;
                seed.transform.localScale = scale;
                seed.layer = (int)GlobalEnums.PhysLayers.ENEMIES;
                Destroy(seed.GetComponent<DamageHero>());

                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 5);
                bomb.SetActive(true);
                rb.gravityScale = 1.3f;
                StartCoroutine(AnimEnder());
                rb.velocity = new Vector2(dir * 30f, 40f);
                CollisionCheck cc = bomb.AddComponent<CollisionCheck>();
                rb.angularVelocity = dir * 900f;
                yield return new WaitWhile(() => !cc.Hit && !FastApproximately(bomb.transform.GetPositionX(), MiddleX, 0.6f));
                anim.Play("Bomb");
                yield return new WaitWhile(() => anim.GetCurrentFrame() < 1);
                rb.angularVelocity = 0f;
                rb.gravityScale = 0f;
                rb.velocity = Vector2.zero;
                this.PlayAudio(FiveKnights.Clips["IsmaAudSeedBomb"], 1f);
                yield return new WaitWhile(() => anim.GetCurrentFrame() <= 2); //whip and boxcollider seed
                if(firstBomb)
                {
                    for(int i = 200; i <= 340; i += 35)
                    {
                        GameObject localSeed = Instantiate(seed, bomb.transform.position, Quaternion.Euler(0f, 0f, i));
                        localSeed.name = "SeedEnemy";
                        localSeed.SetActive(true);
                        localSeed.GetComponent<Rigidbody2D>().velocity =
                            new Vector2(30f * Mathf.Cos(i * Mathf.Deg2Rad), 30f * Mathf.Sin(i * Mathf.Deg2Rad));
                        Destroy(localSeed, 3f);
                    }
                    firstBomb = false;
                }
                else if(spawningWalls)
                {
                    for(int i = 0; i < 3; i++)
                    {
                        for(int j = 0; j < 8; j++)
                        {
                            Vector2 targetPos = new Vector2(j < 4 ? LeftX + 2f * (j + 1) : RightX + 2f * (j - 8), 7.3f);
                            Vector2 path = targetPos - (Vector2)bomb.transform.position;
                            float rot = Mathf.Atan2(path.y, path.x);

                            GameObject localSeed = Instantiate(seed, bomb.transform.position, Quaternion.Euler(0f, 0f, rot));
                            localSeed.name = "VineWallSeed";
                            localSeed.SetActive(true);
                            localSeed.GetComponent<Rigidbody2D>().velocity =
                                new Vector2((25f + i * 5f) * Mathf.Cos(rot), (25f + i * 5f) * Mathf.Sin(rot));
                            Destroy(localSeed, 3f);
                        }
                    }
                    spawningWalls = false;
                }
                else
                {
                    int incr = _wallActive ? 50 : 40;
                    for(int i = 0; i <= 360; i += incr)
                    {
                        float rot = i + UnityEngine.Random.Range(0, 10) * (incr / 10);
                        GameObject localSeed = Instantiate(seed, bomb.transform.position, Quaternion.Euler(0f, 0f, rot));
                        localSeed.name = "SeedEnemy";
                        localSeed.SetActive(true);
                        localSeed.GetComponent<Rigidbody2D>().velocity =
                            new Vector2(30f * Mathf.Cos(rot * Mathf.Deg2Rad), 30f * Mathf.Sin(rot * Mathf.Deg2Rad));
                        Destroy(localSeed, 3f);
                    }
                }
                StartCoroutine(IdleTimer(IDLE_TIME));
                yield return new WaitWhile(() => anim.IsPlaying());
                Destroy(bomb);
            }

            IEnumerator AnimEnder()
            {
                /*yield return new WaitWhile(() => _anim.GetCurrentFrame() < 6);
                _anim.enabled = false;
                yield return new WaitForSeconds(0.1f);
                _anim.enabled = true;
                _rb.velocity = new Vector2(-20f * dir, 0f);
                yield return new WaitWhile(() => _anim.IsPlaying());
                _rb.velocity = new Vector2(0f, 0f);*/
                yield return _anim.WaitToFrame(8);
                yield return new WaitForSeconds(0.08f);
                transform.position += new Vector3(0f, 0.2f);
                yield return _anim.WaitToFrame(10);
                _bc.enabled = false;
                yield return _anim.PlayToEnd();
                ToggleIsma(false);
            }

            StartCoroutine(BombThrow());
        }

        private GameObject arm;
        private GameObject spike;
        private GameObject tentArm;

        private void AirFist()
        {
            IEnumerator AirFist()
            {
                float heroX = _target.transform.GetPositionX();
                float distance = UnityEngine.Random.Range(10, 12);
                float ismaX = heroX - MiddleX > 0f ? heroX - distance : heroX + distance;

                if (_wallActive)
                {
                    float rightMost = RightX - 8;
                    float leftMost = LeftX + 10;
                    ismaX = heroX - MiddleX > 0f 
                        ? UnityEngine.Random.Range((int)leftMost, (int)heroX - 6) 
                        : UnityEngine.Random.Range((int)heroX + 6, (int)rightMost);
                }
                
                transform.position = new Vector2(ismaX, UnityEngine.Random.Range(13, 16));
                ToggleIsma(true);
                float dir = FaceHero();
                
                arm = transform.Find("Arm2").gameObject;
                _anim.Play("AFistAntic");
                PlayVoice();
                _rb.velocity = new Vector2(dir * -20f, 0f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                _rb.velocity = new Vector2(0f, 0f);
                spike = transform.Find("SpikeArm").gameObject;
                yield return new WaitWhile(() => _anim.IsPlaying());

                _anim.Play("AFist");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 0);
                spike.SetActive(true);
                _anim.enabled = false;
                yield return new WaitForSeconds(0.3f);
                
                float rot = GetRot(arm, _target.transform.position, dir);
                float rotD = rot * Mathf.Rad2Deg;

                // Will check 35 degrees on either side of her
                if(rotD is < -55f or > 55f)
                {
                    _anim.enabled = true;
                    spike.SetActive(false);
                    yield return AcidThrow();
                    yield break;
                }
                
                arm.transform.SetRotation2D(rotD);
                GameObject parArm = arm.transform.Find("TentArm").gameObject;
                parArm.SetActive(false);
                tentArm = Instantiate(parArm, parArm.transform.position, parArm.transform.rotation);
                tentArm.SetActive(false);
                Vector3 tentArmScale = tentArm.transform.localScale;
                tentArm.transform.localScale = new Vector3(dir * tentArmScale.x, tentArmScale.y, tentArmScale.z) * 1.35f;
                
                yield return new WaitForSeconds(0.1f);
                spike.SetActive(false);
                _anim.enabled = true;
                yield return new WaitUntil(() => _anim.GetCurrentFrame() >= 1);
                arm.SetActive(true);
                tentArm.SetActive(true);
                tentArm.AddComponent<AFistFlash>();
                foreach(var i in tentArm.transform.GetComponentsInChildren<PolygonCollider2D>(true))
                {
                    i.gameObject.AddComponent<PlantHitFx>().hitSound = FiveKnights.Clips["IsmaAudVineHit"];
                }

                Animator tentAnim = tentArm.GetComponent<Animator>();
                tentAnim.speed = 1.9f;
                yield return tentAnim.PlayToFrameAt("NewArmAttack", 0, 12);
                tentAnim.enabled = false;
                yield return new WaitForSeconds(0.15f);
                tentAnim.enabled = true;
                tentAnim.speed = 2f;
                yield return tentAnim.PlayToEnd();
                tentAnim.speed = 1f;

                yield return EndAirFist(spike, tentArm, dir);
                Destroy(tentArm);
                StartCoroutine(IdleTimer(IDLE_TIME));
            }

            float GetRot(GameObject arm, Vector3 pos, float dir)
            {
                Vector2 diff = arm.transform.position - pos;
                float offset2 = 0f;
                if ((dir > 0 && diff.x > 0) || (dir < 0 && diff.x < 0)) offset2 += 180f;
                return Mathf.Atan(diff.y / diff.x) + offset2 * Mathf.Deg2Rad;
            }

            IEnumerator EndAirFist(GameObject spike, GameObject tentArm, float dir, float spd=0.4f)
            {
                _anim.enabled = true;
                Destroy(tentArm);
                spike.SetActive(true);
                _anim.Play("AFist2");
                yield return new WaitForSeconds(0.05f);
                spike.SetActive(false);
                yield return _anim.PlayToFrame("AFistEnd", 1);
                _bc.enabled = false;
                yield return _anim.PlayToEnd();
                ToggleIsma(false);
            }

            StartCoroutine(AirFist());
        }
        
        private IEnumerator AcidThrow()
        {
            float GetRot(Vector3 origPos, Vector3 tarPos)
            {
                Vector2 diff = origPos - tarPos;
                float tmpRot = Mathf.Atan(diff.y / diff.x) * Mathf.Rad2Deg;
                if (diff.x > 0)
                {
                    tmpRot = tmpRot + 180f;
                    tmpRot = tmpRot < 220f ? 220f : tmpRot;
                    Log($"What is deg1? {tmpRot}");
                }
                else
                {
                    tmpRot = tmpRot > -40f ? -40f : tmpRot;
                    Log($"What is deg2? {tmpRot}");
                }
                return tmpRot * Mathf.Deg2Rad;
            }
            
            yield return _anim.PlayToFrameAt("AcidSwipe", 0, 4);
            _anim.enabled = false;
            // Shortened delay if WD is present to avoid getting canceled by the next attack
            yield return new WaitForSeconds(onlyIsma ? 0.3f : 0.1f);
            _anim.enabled = true;
            
            yield return _anim.WaitToFrame(6);
            
            Vector2 tarPos = HeroController.instance.transform.position;
            Vector3 pos = transform.position - new Vector3(0f, 0.5f, 0f);
            
            float rot = GetRot(pos, tarPos);
            float mag = 20f;
            for (int i = -1; i < 2; i++)
            {
                var acid = Instantiate(FiveKnights.preloadedGO["AcidSpit"]);
                acid.AddComponent<AcidGnd>();
                acid.transform.position = pos;
                acid.SetActive(true);

                float currRot = rot + i * Mathf.PI / 3;
                acid.GetComponent<Rigidbody2D>().velocity =
                    new Vector2(mag * Mathf.Cos(currRot), mag * Mathf.Sin(currRot));
            }

            yield return _anim.PlayToEnd();
            ToggleIsma(false);
            StartCoroutine(IdleTimer(IDLE_TIME));
        }
        
        public class AcidGnd : MonoBehaviour
        {
            private Rigidbody2D _rb;
            private PlayMakerFSM _fsm;
            private const float SlimeY = 7.5f;
            private bool _isLanded;
            public void Awake()
            {
                _rb = GetComponent<Rigidbody2D>();
                _fsm = gameObject.LocateMyFSM("Vomit Glob");
                _isLanded = false;
                _fsm.GetAction<WaitRandom>("Land", 8).timeMin = 0.75f;
                _fsm.GetAction<WaitRandom>("Land", 8).timeMax = 1f;
            }

            public void FixedUpdate()
            {
                if (!_isLanded && _fsm.ActiveStateName == "In Air" && transform.position.y < SlimeY)
                {
                    _isLanded = true;
                    this.PlayAudio(FiveKnights.Clips["AcidSpitSnd"], 1f, 0f, HeroController.instance.transform);
                    var pos = transform.position;
                    transform.position = new Vector3(pos.x, SlimeY, pos.z);
                    _fsm.SetState("Land");
                    Destroy(this);
                }
            }
        }

        private const float WhipYOffset = 2.21f;
        private const float WhipXOffset = 1.13f;
        private GameObject whip;

        private void WhipAttack()
        {
            IEnumerator WhipAttack()
            {
                float heroX = _target.transform.GetPositionX();
                float ismaX = heroX - MiddleX > 0f ? LeftX + 8f : RightX - 8f;
                if (_wallActive) ismaX = heroX - MiddleX > 0f ? LeftX + 11f : RightX - 11f;
                transform.position = new Vector2(ismaX, GroundY);
                float dir = FaceHero();
                ToggleIsma(true);
                _rb.velocity = new Vector2(dir * -20f, 0f);
                _anim.Play("GFistAntic");
                PlayVoice();
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
                _anim.enabled = false;
                yield return new WaitForSeconds(0.7f);
                _anim.enabled = true;
                transform.position += new Vector3(WhipXOffset * Math.Sign(transform.localScale.x), WhipYOffset - 0.2f, 0f);
                var oldWhip = transform.Find("Whip").gameObject;
                whip = Instantiate(oldWhip);
                whip.transform.position = oldWhip.transform.position;
                whip.transform.localScale = oldWhip.transform.lossyScale;
                whip.SetActive(true);
                whip.AddComponent<WhipFlash>();
                foreach(var i in whip.transform.GetComponentsInChildren<PolygonCollider2D>(true))
                {
                    i.gameObject.AddComponent<PlantHitFx>().hitSound = FiveKnights.Clips["IsmaAudVineHit"];
                }
                this.PlayAudio(FiveKnights.Clips["IsmaAudGroundWhip"], 1f);
                
                yield return _anim.PlayToEndWithActions("GFistCopy",
           (0, () => { whip.Find("W1").SetActive(true); }),
                    (1, () => { whip.Find("W2").SetActive(true); whip.Find("W1").SetActive(false); }),
                    (2, () => { whip.Find("W3").SetActive(true); whip.Find("W2").SetActive(false); }),
                    (3, () => { whip.Find("W4").SetActive(true); whip.Find("W3").SetActive(false); }),
                    (4, () => { whip.Find("W5").SetActive(true); whip.Find("W4").SetActive(false); }),
                    (5, () => { whip.Find("W6").SetActive(true); whip.Find("W5").SetActive(false); }),
                    (6, () => { whip.Find("W7").SetActive(true); whip.Find("W6").SetActive(false); }),
                    (7, () => { whip.Find("W8").SetActive(true); whip.Find("W7").SetActive(false); }),
                    (8, () => { whip.Find("W9").SetActive(true); whip.Find("W8").SetActive(false); }),
                    (9, () => { whip.Find("W12").SetActive(true); whip.Find("W9").SetActive(false); }),
                    (10, () => { whip.Find("W13").SetActive(true); whip.Find("W12").SetActive(false); }),
                    (11, () => { whip.Find("W14").SetActive(true); whip.Find("W13").SetActive(false); }),
                    (12, () => { whip.Find("W15").SetActive(true); whip.Find("W14").SetActive(false); }),
                    (13, () => { whip.Find("W16").SetActive(true); whip.Find("W15").SetActive(false); }),
                    (14, () => { whip.Find("W17").SetActive(true); whip.Find("W16").SetActive(false); }),
                    (15, () => { whip.Find("W17").SetActive(false); })
                );
                
                transform.position -= new Vector3(WhipXOffset * Math.Sign(transform.localScale.x), WhipYOffset - 0.3f, 0f);
                yield return _anim.PlayToFrame("GFistEnd", 2);
                _bc.enabled = false;
                yield return _anim.PlayToEnd();
                ToggleIsma(false);
                Destroy(whip);
                StartCoroutine(IdleTimer(IDLE_TIME));
            }

            StartCoroutine(WhipAttack());
        }

        private IEnumerator BowWhipAttack()
        {
            float dir = -1f * FaceHero(true);
            PlayVoice();
            _anim.PlayAt("LoneDeath", 1);
            _rb.velocity = new Vector2(-dir * 17f, 32f);
            _rb.gravityScale = 1.5f;
            yield return null;
            _anim.enabled = false;
            yield return new WaitForSeconds(0.05f);
            _anim.enabled = true;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
            _anim.enabled = false;
            yield return new WaitWhile(() => transform.position.y > GroundY + 2.5f);
            transform.position = new Vector3(transform.position.x, GroundY + 2.5f, transform.position.z);
            _anim.enabled = true;
            _rb.gravityScale = 0f;
            _rb.velocity = Vector2.zero;
            yield return new WaitForSeconds(0.1f);
            dir = FaceHero();
            transform.position = new Vector3(transform.position.x, GroundY, transform.position.z);
            Log("Start play");
            transform.position += new Vector3(WhipXOffset * Math.Sign(transform.localScale.x), WhipYOffset - 0.2f, 0f);
            
            var oldWhip = transform.Find("Whip").gameObject;
            var whip = Instantiate(oldWhip);
            whip.transform.position = oldWhip.transform.position;
            whip.transform.localScale = oldWhip.transform.lossyScale;
            whip.SetActive(true);
            whip.AddComponent<WhipFlash>();
            foreach(var i in whip.transform.GetComponentsInChildren<PolygonCollider2D>(true))
            {
                i.gameObject.AddComponent<PlantHitFx>().hitSound = FiveKnights.Clips["IsmaAudVineHit"];
            }
            this.PlayAudio(FiveKnights.Clips["IsmaAudGroundWhip"], 1f);

            yield return _anim.PlayToEndWithActions("GFistCopy",
       (0, () => { whip.Find("W1").SetActive(true); }),
                (1, () => { whip.Find("W2").SetActive(true); whip.Find("W1").SetActive(false); }),
                (2, () => { whip.Find("W3").SetActive(true); whip.Find("W2").SetActive(false); }),
                (3, () => { whip.Find("W4").SetActive(true); whip.Find("W3").SetActive(false); }),
                (4, () => { whip.Find("W5").SetActive(true); whip.Find("W4").SetActive(false); }),
                (5, () => { whip.Find("W6").SetActive(true); whip.Find("W5").SetActive(false); }),
                (6, () => { whip.Find("W7").SetActive(true); whip.Find("W6").SetActive(false); }),
                (7, () => { whip.Find("W8").SetActive(true); whip.Find("W7").SetActive(false); }),
                (8, () => { whip.Find("W9").SetActive(true); whip.Find("W8").SetActive(false); }),
                (9, () => { whip.Find("W12").SetActive(true); whip.Find("W9").SetActive(false); }),
                (10, () => { whip.Find("W13").SetActive(true); whip.Find("W12").SetActive(false); }),
                (11, () => { whip.Find("W14").SetActive(true); whip.Find("W13").SetActive(false); }),
                (12, () => { whip.Find("W15").SetActive(true); whip.Find("W14").SetActive(false); }),
                (13, () => { whip.Find("W16").SetActive(true); whip.Find("W15").SetActive(false); }),
                (14, () => { whip.Find("W17").SetActive(true); whip.Find("W16").SetActive(false); }),
                (15, () => { whip.Find("W17").SetActive(false); })
            );
           
            transform.position -= new Vector3(WhipXOffset * Math.Sign(transform.localScale.x), WhipYOffset - 0.3f, 0f);
            yield return _anim.PlayToFrame("GFistEnd", 2);
            _bc.enabled = false;
            yield return _anim.PlayToEnd();
            ToggleIsma(false);
            StartCoroutine(IdleTimer(IDLE_TIME));
        }

        private bool _playingVoiceDS;
        private IEnumerator DungStrike()
        {
            tk2dSpriteAnimator tk = dd.GetComponent<tk2dSpriteAnimator>();
            bool prevRageBallMissed = false;
			while(true)
            {
                yield return new WaitUntil(() => tk.CurrentClip.name.Contains("Throw") || tk.CurrentClip.name.Contains("Erupt"));
                yield return WaitToAttack();
                while(tk.CurrentClip.name.Contains("Throw") ||
                    (tk.CurrentClip.name.Contains("Erupt") &&
                    (_ddFsm.FsmVariables.FindFsmInt("Rages").Value % 2 == 1 || prevRageBallMissed)))
                {
                    // Try to find a valid dung ball to hit each frame
                    GameObject go = LocateBall();
                    if(go == null)
					{
                        yield return new WaitForEndOfFrame();
                        continue;
					}

                    // Isma will move along with the ball and face the same direction as it, the animation starts moving left by default
                    IEnumerator TrackBall()
                    {
                        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
                        while(go != null && go.GetComponent<Renderer>().enabled && go.transform.position.y < 16f && go.transform.position.y > 13f)
                        {
                            Vector3 scale = transform.localScale;
                            // Ball moving right, so flip sprite
                            if(rb.velocity.x > 0f) transform.localScale = new Vector3(-Mathf.Abs(scale.x), scale.y, scale.z);
                            // Ball moving left, so keep sprite normal
                            else transform.localScale = new Vector3(Mathf.Abs(scale.x), scale.y, scale.z);
                            // Track position
                            transform.position = new Vector3(go.transform.position.x, 16f, transform.position.z);
                            yield return null;
                        }
                        // Wait for the animation to be disabled, then reenable the animation again once the ball falls to the right level
                        yield return new WaitUntil(() => !_anim.enabled);
                        _anim.enabled = true;
                        // Restart tracking while the dung ball is still in motion
                        while(go != null && go.GetComponent<Renderer>().enabled)
						{
                            Vector3 scale = transform.localScale;
                            if(rb.velocity.x > 0f) transform.localScale = new Vector3(-Mathf.Abs(scale.x), scale.y, scale.z);
                            else transform.localScale = new Vector3(Mathf.Abs(scale.x), scale.y, scale.z);
                            transform.position = new Vector3(go.transform.position.x, 16f, transform.position.z);
                            yield return null;
                        }
                    }

                    // Make sure she's oriented the right way
                    Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
                    Vector3 scale = transform.localScale;
                    if(rb.velocity.x > 0f) transform.localScale = new Vector3(-Mathf.Abs(scale.x), scale.y, scale.z);
                    transform.position = new Vector3(go.transform.position.x, 16f, transform.position.z);
                    ToggleIsma(true);
                    _anim.Play("BallStrike");

                    // Start tracking the ball
                    Log("Starting coroutine");
                    StartCoroutine(TrackBall());

                    // Freeze animation on the first frame
                    Log("Disabling animation");
                    yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                    _anim.enabled = false;
                    if(!_playingVoiceDS) StartCoroutine(DungStrikeVoice());

                    // Wait for the animation to reenable to hit the ball or if the ball was destroyed right when Isma went to hit it
                    yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                    // Make sure animation is enabled to avoid freezing Isma
                    _anim.enabled = true;
                    Log("After coroutine");

                    // Create ball related objects
                    GameObject squish = gameObject.transform.Find("Squish").gameObject;
                    GameObject ball = Instantiate(gameObject.transform.Find(_usingThornPillars ? "Ball" : "VineBall").gameObject);
                    GameObject particles = Instantiate(go.LocateMyFSM("Ball Control").FsmVariables.FindFsmGameObject("Break Chunks").Value);
                    DungBall db = ball.AddComponent<DungBall>();
                    db.particles = particles;
                    db.usingThornPillars = _usingThornPillars;
                    if(!_wallActive)
                    {
                        db.LeftX = LeftX + 1f;
                        db.RightX = RightX - 1f;
                    }

                    // Check if ball has been destroyed before hitting it
                    Log("Trying to hit ball");
                    if(go != null && go.GetComponent<Renderer>().enabled)
                    {
                        Log("Hitting ball");
                        Destroy(go);
                        squish.SetActive(true);
                        this.PlayAudio(FiveKnights.Clips["IsmaAudDungHit"], 0.8f);
                        yield return new WaitWhile(() => _anim.GetCurrentFrame() <= 2);
                        GameObject ballFx = ball.transform.Find("BallFx").gameObject;
                        squish.SetActive(false);
                        ball.SetActive(true);
                        ball.transform.position = gameObject.transform.Find("Ball").position;
                        ballFx.transform.parent = null;
                        Vector2 diff = ball.transform.position - _target.transform.position;
                        float offset2 = 0f;
                        if(diff.x > 0)
                        {
                            offset2 += 180f;
                        }
                        float rot = Mathf.Atan(diff.y / diff.x) + offset2 * Mathf.Deg2Rad;
                        ball.transform.SetRotation2D(rot * Mathf.Rad2Deg + 90f);
                        Vector2 vel = new Vector2(25f * Mathf.Cos(rot), 25f * Mathf.Sin(rot));
                        ball.GetComponent<Rigidbody2D>().velocity = vel;
                        yield return new WaitForSeconds(0.1f);
                        ballFx.GetComponent<Animator>().Play("FxEnd");
                        yield return new WaitForSeconds(0.1f);
                        Destroy(ballFx);
                    }
                    else
					{
                        if(_ddFsm.FsmVariables.FindFsmInt("Rages").Value % 2 == 1) prevRageBallMissed = true;
                    }
                    prevRageBallMissed = false;
                    // Finish playing the animation, then exit
                    Log("After hitting ball");
                    yield return new WaitWhile(() => _anim.GetCurrentFrame() < 6);
                    _rb.velocity = new Vector2(Mathf.Sign(transform.localScale.x) * 20f, 0f);
                    yield return new WaitWhile(() => _anim.IsPlaying());
                    _rb.velocity = new Vector2(0f, 0f);
                    ToggleIsma(false);
                    yield return new WaitForEndOfFrame();
                    Log("End of DungStrike");
                }
                StartCoroutine(IdleTimer(IDLE_TIME));
                yield return new WaitForSeconds(0.8f);
            }
        }

        private IEnumerator DungStrikeVoice()
		{
            _playingVoiceDS = true;
            PlayVoice();
            yield return new WaitForSeconds(2f);
            _playingVoiceDS = false;
        }

        private GameObject LocateBall()
		{
            tk2dSpriteAnimator tk = dd.GetComponent<tk2dSpriteAnimator>();
            bool TestBall(GameObject x)
			{
                if(x.GetComponent<Rigidbody2D>() == null) return false;
                if(!x.activeSelf || !(x.transform.GetPositionY() > 13f) || !(x.GetComponent<Rigidbody2D>().velocity.y > 0f)) return false;
                if(tk.CurrentClip.name.Contains("Throw") && x.name.Contains("Ogrim Thrown Ball")) return true;
                if(tk.CurrentClip.name.Contains("Erupt") && x.name.Contains("Dung Ball") && 
                    x.GetComponent<DungTracker>() == null &&
                    _ddFsm.FsmVariables.FindFsmInt("Rages").Value > 0 &&
                    FastApproximately(x.transform.GetPositionX(), _target.transform.GetPositionX(), 7.5f)) return true;
                return false;
			}
            GameObject[] balls = FindObjectsOfType<GameObject>().Where(x => TestBall(x)).ToArray();
            if(balls.Length > 0) return balls[_rand.Next(0, balls.Length)];
            return null;
        }

        private IEnumerator OgrimStrike()
		{
            tk2dSpriteAnimator tk = dd.GetComponent<tk2dSpriteAnimator>();
			// Remove previous method that allows Ogrim to dive at a lower height when by himself
            _ddFsm.RemoveAction("RJ In Air", 8);
            // Allow Ogrim to dive at lower height
            _ddFsm.GetAction<FloatTestToBool>("RJ In Air", 7).float2.Value = 13f;
			// Prevent Ogrim from diving without Isma hitting him
			_ddFsm.GetAction<BoolTestMulti>("RJ In Air", 8).Enabled = false;
            // Disable Ogrim's dive velocity change so we can add our own
            _ddFsm.GetAction<SetVelocity2d>("Air Dive", 4).Enabled = false;
            // Disable Ogrim's uncurl animation
            _ddFsm.GetAction<Tk2dPlayAnimationWithEvents>("Air Dive Antic", 1).Enabled = false;
            // Make Ogrim stop moving horizontally when he hits the ground but continue moving down
            _ddFsm.InsertMethod("AD In", () =>
            {
                _rbDD.velocity = new Vector2(0f, _rbDD.velocity.y);
            }, 1);
            // Reset Ogrim's rotation after he goes underground
            _ddFsm.InsertMethod("Under", () =>
            {
                dd.transform.SetRotation2D(0f);
            }, 0);

            while(true)
			{
                yield return new WaitUntil(() => tk.CurrentClip.name == "Roll");
                yield return new WaitWhile(() => _attacking);
                // Wait for Thorn Pillars
                yield return new WaitForSeconds(1f);
                yield return WaitToAttack();
                yield return new WaitUntil(() => (!_ddFsm.FsmVariables.FindFsmBool("Still Bouncing").Value && 
                        _ddFsm.FsmVariables.FindFsmBool("Air Dive Height").Value) ||
                    (FastApproximately(dd.transform.position.y, 13.5f, 0.75f) && _rbDD.velocity.y > 0f && 
                        _ddFsm.FsmVariables.FindFsmInt("Bounces").Value < -3) ||
                    startedIsmaRage);
                if(startedIsmaRage)
                {
                    _attacking = false;
                    break;
                }

                _ddFsm.SetState("Air Dive Antic");

                Vector2 pos = dd.transform.position;

                PlayVoice();
                float side = _rbDD.velocity.x > 0f ? 1f : -1f;
                float dir = FaceHero();
                gameObject.transform.position = new Vector2(pos.x + side * 2f, pos.y + 0.38f);

                Vector2 diff = dd.transform.position - new Vector3(_target.transform.GetPositionX(), GroundY);
                float offset2 = 0f;
                if(diff.x > 0)
                {
                    offset2 += 180f;
                }
                float rot = Mathf.Atan(diff.y / diff.x) + offset2 * Mathf.Deg2Rad;
                Vector2 vel = new Vector2(50f * Mathf.Cos(rot), 45f * Mathf.Sin(rot));
                bool setVel = false;
                _ddFsm.InsertMethod("Air Dive", () =>
                {
                    dd.transform.SetRotation2D(rot * Mathf.Rad2Deg + 90f);
                    _rbDD.velocity = vel;
                    setVel = true;
                }, 2);

                ToggleIsma(true);
                _anim.Play("BallStrike");
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                _anim.enabled = false;
                _rb.velocity = new Vector2(-dir * 5f, 0f);
                yield return new WaitForSeconds(0.1f);
                _rb.velocity = Vector2.zero;
                yield return new WaitForSeconds(0.1f);
                _anim.enabled = true;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
                _ddFsm.SendEvent("FINISHED");
                yield return new WaitUntil(() => setVel || _isDead);
                _ddFsm.RemoveAction("Air Dive", 2);
                yield return new WaitForSeconds(0.2f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 6);
                _rb.velocity = new Vector2(dir * 20f, 0f);
                yield return new WaitWhile(() => _anim.IsPlaying());
                _rb.velocity = Vector2.zero;
                ToggleIsma(false);
                yield return new WaitForEndOfFrame();
                StartCoroutine(IdleTimer(IDLE_TIME));
            }
		}

        private void ThornPillars()
		{
            IEnumerator DoThornPillars()
            {
                float heroX = _target.transform.GetPositionX();
                float ismaX = heroX - MiddleX > 0f ? LeftX + 9f : RightX - 9f;
                transform.position = new Vector2(ismaX, GroundY);
                float dir = FaceHero();
                ToggleIsma(true);
                _rb.velocity = new Vector2(dir * -20f, 0f);
                _anim.Play("ThornPillarsAntic");
                PlayVoice();
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                _rb.velocity = Vector2.zero;
                _anim.enabled = false;
                yield return new WaitForSeconds(0.3f);
                _anim.enabled = true;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("ThornPillars");
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
                _anim.enabled = false;
                yield return new WaitForSeconds(0.1f);

                float center = MiddleX + UnityEngine.Random.Range(-1f, 1f);
                for(int i = -2; i < 3; i++)
				{
                    GameObject pillar = Instantiate(FiveKnights.preloadedGO["ThornPlant"]);
                    pillar.AddComponent<ThornPlantCtrl>();
                    pillar.GetComponent<BoxCollider2D>().enabled = false;
                    pillar.layer = (int)GlobalEnums.PhysLayers.ENEMY_ATTACK;
                    pillar.transform.position = new Vector2(MiddleX + 4f * i * dir, 13.4f);
                    yield return new WaitForSeconds(0.1f);
                }
                yield return new WaitForSeconds(1f);
                for(int i = -2; i < 3; i++)
                {
                    GameObject pillar = Instantiate(FiveKnights.preloadedGO["ThornPlant"]);
                    pillar.AddComponent<ThornPlantCtrl>().secondWave = true;
                    pillar.GetComponent<BoxCollider2D>().enabled = false;
                    pillar.layer = (int)GlobalEnums.PhysLayers.ENEMY_ATTACK;
                    pillar.transform.position = new Vector2(MiddleX + 4f * i * dir + 2f * dir, 13.4f);
                    yield return new WaitForSeconds(0.1f);
                }
                yield return new WaitForSeconds(0.2f);
                _anim.enabled = true;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("ThornPillarsEnd");
                yield return null;
                yield return new WaitWhile(() => _anim.IsPlaying());
                ToggleIsma(false);
                yield return new WaitForSeconds(1f);
                StartCoroutine(IdleTimer(IDLE_TIME));
            }
            StartCoroutine(DoThornPillars());
        }

        private GameObject fakeIsma;
        private float agonyAnimSpd = 1.2f;
        private Vector2 agonySpread = new Vector2(35f, 40f);
        private GameObject agonyThorns;
        private bool usedAgony;

        private IEnumerator Agony()
        {
            ToggleIsma(true);
            Vector3 scIs = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector3(Mathf.Abs(scIs.x), scIs.y, scIs.z);
            gameObject.transform.SetPosition2D(MiddleX, GroundY + 11.6f);

            GameObject fakeIsma = new GameObject();
            fakeIsma.transform.position = gameObject.transform.position;
            fakeIsma.transform.localScale = gameObject.transform.localScale;
            GameObject thornorig = transform.Find("Thorn").gameObject;
            agonyThorns = Instantiate(thornorig);
            Vector3 orig = thornorig.transform.position;
            agonyThorns.transform.position = new Vector3(orig.x - 1f, orig.y - 4f, orig.z);
            agonyThorns.transform.parent = fakeIsma.transform;

            Animator tAnim = agonyThorns.transform.Find("T1").gameObject.GetComponent<Animator>();

            _anim.Play("AgonyLoopIntro");
            yield return null;
            yield return new WaitWhile(() => _anim.IsPlaying());
            PlayVoice();
            _anim.speed = 1.7f;

            yield return PerformAgony(agonyThorns, tAnim, onlyIsma ? 3 : 1);

            _anim.Play("AgonyLoopEnd");
            yield return null;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
            _rb.velocity = new Vector2(20f, 0f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            _anim.speed = 1f;
            _rb.velocity = Vector2.zero;

            usedAgony = true;
            ToggleIsma(false);
            StartCoroutine(IdleTimer(IDLE_TIME));
        }

        private IEnumerator PerformAgony(GameObject thorn, Animator tAnim, int loops = 0)
		{
            bool repeat = loops == 0;
            for(int j = 0; j < loops || repeat; j++)
            {
                _anim.PlayAt("AgonyLoop", 0);

                this.PlayAudio(FiveKnights.Clips["IsmaAudAgonyIntro"], 1f);

                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
                thorn.SetActive(true);

                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 9);

                if (j % 2 == 0 && j != 0) PlayVoice();

                Animator[] anims = thorn.GetComponentsInChildren<Animator>(true);
                Vector2 heroVel = _target.GetComponent<Rigidbody2D>().velocity;
                // Disabled velocity tracking for now
                float predTime = 0f;
                float yOff = 0f;
                float xOff = 1f;
                Vector3 predPos = _target.transform.position + new Vector3(heroVel.x * xOff, heroVel.y * yOff) * predTime;
                Vector2 diff = tAnim.transform.position - predPos;
                float rot = Mathf.Atan(diff.y / diff.x) * Mathf.Rad2Deg + (diff.x < 0 ? 180f : 0f);
                float smallOff = 4;
                float rotStart = rot;
                float[] arr =
                {
                    rot,
                    rotStart + UnityEngine.Random.Range(agonySpread.x, agonySpread.y),
                    rotStart + 2 * UnityEngine.Random.Range(agonySpread.x, agonySpread.y),
                    rotStart - UnityEngine.Random.Range(agonySpread.x, agonySpread.y),
                    rotStart - 2 * UnityEngine.Random.Range(agonySpread.x, agonySpread.y)
                };
                for(int i = 0; i < arr.Length; i++)
                {
                    float currRot = arr[i];
                    Animator t1 = anims[i * 2];
                    Animator t2 = anims[i * 2 + 1];
                    t1.gameObject.layer = t2.gameObject.layer = 17;
                    t1.gameObject.transform.rotation = Quaternion.Euler(0f, 0f, currRot - smallOff + 12.5f);
                    t2.gameObject.transform.rotation = Quaternion.Euler(0f, 0f, currRot + smallOff + 12.5f);
                }

                foreach(Animator i in thorn.GetComponentsInChildren<Animator>(true))
                {
                    i.gameObject.GetComponent<SpriteRenderer>().enabled = true;
                    i.Play("NewAThornAnim");
                    i.speed = agonyAnimSpd;
                }
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 9);
                _anim.enabled = false;

                yield return new WaitWhile(() => tAnim.GetCurrentFrame() < 4);

                foreach(Animator i in thorn.GetComponentsInChildren<Animator>(true))
                {
                    i.enabled = false;
                }
                yield return new WaitForSeconds(0.2f);
                foreach(Animator i in thorn.GetComponentsInChildren<Animator>(true))
                {
                    i.enabled = true;
                }
                for(int i = 0; i < arr.Length; i++)
                {
                    float currRot = arr[i];
                    Animator t1 = anims[i * 2];
                    Animator t2 = anims[i * 2 + 1];
                    t1.gameObject.transform.rotation = Quaternion.Euler(0f, 0f, currRot - smallOff);
                    t2.gameObject.transform.rotation = Quaternion.Euler(0f, 0f, currRot + smallOff);
                }

                yield return new WaitWhile(() => tAnim.GetCurrentFrame() < 5);
                this.PlayAudio(FiveKnights.Clips["IsmaAudAgonyShoot"], 1f);

                yield return new WaitWhile(() => tAnim.GetCurrentFrame() < 6);
                _anim.enabled = true;
                yield return new WaitWhile(() => tAnim.IsPlaying());
                foreach(Animator i in thorn.GetComponentsInChildren<Animator>(true))
                {
                    i.gameObject.GetComponent<SpriteRenderer>().enabled = false;
                    i.Play("IdleThorn");
                }
                thorn.SetActive(false);
                yield return new WaitWhile(() => _anim.IsPlaying());
            }
        }

        private IEnumerator LoopedAgony()
        {
            ToggleIsma(true);
            _attacking = true;
            gameObject.transform.SetPosition2D(MiddleX, GroundY + 11.1f);
            
            fakeIsma = new GameObject();
            fakeIsma.transform.position = gameObject.transform.position;
            fakeIsma.transform.localScale = gameObject.transform.localScale;
            GameObject thornorig = transform.Find("Thorn").gameObject;
            GameObject thorn = Instantiate(thornorig);
            Vector3 orig = thornorig.transform.position;
            thorn.transform.parent = fakeIsma.transform;
            thorn.transform.position = orig;
            foreach (Animator j in thorn.GetComponentsInChildren<Animator>(true))
            {
                j.transform.position = thornorig.transform.Find("T1").position;
            }

            Animator tAnim = thorn.transform.Find("T1").gameObject.GetComponent<Animator>();
            _anim.Play("AgonyLoopIntro");
            yield return new WaitForSeconds(0.05f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            _anim.speed = 1.6f;
            yield return PerformAgony(thorn, tAnim);
        }

        private float CalculateTrajectory(Vector2 vel0, float h, float g)
        {
            float accel = g * Physics2D.gravity.y;
            float disc = vel0.y * vel0.y - 2 * accel * h;
            float time = (-vel0.y + Mathf.Sqrt(disc)) / accel;
            float time2 = (-vel0.y - Mathf.Sqrt(disc)) / accel;
            if (time < 0) time = time2;
            return time * vel0.x;
        }

        private void Update()
        {
            if(_healthPool <= 0 && !_isDead)
            {
                Log("Victory");
                _isDead = true;
                preventDamage = true;
                _healthPool = FrenzyHP;
				EnemyHPBarImport.DisableHPBar(gameObject);
                if(onlyIsma)
                {
                    _hm.hp = 800;
                    _hm.isDead = false;
                    startedIsmaDeath = true;
                }
                else if(_isIsmaHitLast)
                {
                    startedOgrimRage = true;
                    StopAllCoroutines();
                    StartCoroutine(OgrimRage());
                    StartCoroutine(SpawnWalls());
                }
                else
                {
                    startedIsmaRage = true;
                    StopCoroutine(_wallsCoro);
                    StartCoroutine(IsmaRage());
                    StartCoroutine(SpawnWalls());
                }
            }
        }

        private bool startedOgrimRage;
        private bool startedIsmaRage;
        private bool startedIsmaDeath;
        private bool preventDamage;

        private IEnumerator OgrimRage()
        {
            Log("Started Ogrim rage");

            _attacking = true;
            _healthPool = _hm.hp = _hmDD.hp = MaxHPDuo;

            // Isma dies and leaves
            Destroy(whip);
            Destroy(agonyThorns);
            Destroy(tentArm);
            Destroy(arm);
            Destroy(spike);
            float dir = FaceHero(true);
            PlayDeathFor(gameObject);
            _bc.enabled = false;
            _rb.gravityScale = 1.5f;
            float ismaXSpd = dir * 5f;
            _rb.velocity = new Vector2(ismaXSpd, 28f);
            float side = Mathf.Sign(gameObject.transform.localScale.x);
            _anim.Play("LoneDeath");
            _anim.speed *= 0.7f;
            _anim.enabled = true;
            yield return null;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
            _anim.enabled = false;
            yield return new WaitWhile(() => transform.position.y > GroundY + 2.5f);
            transform.position = new Vector3(transform.position.x, GroundY + 2.25f, transform.position.z);
            var sc = transform.localScale;
            transform.localScale = new Vector3(sc.x * -1f, sc.y, sc.z);
            _anim.enabled = true;
            _rb.gravityScale = 0f;
            _rb.velocity = new Vector2(0f, 0f);
            yield return _anim.WaitToFrame(4);
            _anim.speed = 1f;
            _anim.PlayAt("IsmaTired", 0);
            yield return new WaitForSeconds(1f);
            transform.position = new Vector3(transform.position.x, GroundY + 2.35f, transform.position.z);
            sc = transform.localScale;
            transform.localScale = new Vector3(sc.x * -1f, sc.y, sc.z);
            _anim.PlayAt("LoneDeath", 5);
            _anim.speed = 1f;
            _anim.enabled = true;
            yield return _anim.WaitToFrame(7);
            _rb.velocity = new Vector2(-side * 25f, 25f);
            yield return new WaitForSeconds(0.2f);
            _sr.enabled = false;

            // FSM edits to start rage
            _ddFsm.GetAction<Wait>("AD In", 0).time.Value = 0.25f;
            _ddFsm.GetAction<BoolTestMulti>("RJ In Air", 8).Enabled = true;
            _ddFsm.GetAction<SetVelocity2d>("Air Dive", 4).Enabled = true;
            _ddFsm.GetAction<Tk2dPlayAnimationWithEvents>("Air Dive Antic", 1).Enabled = true;
            _ddFsm.GetAction<SetIntValue>("Set Rage", 1).intValue = 999;
            Coroutine waitToRage = null;
            foreach(string state in new string[] { "Idle", "Move Choice", "After Throw?", "After Evade" })
			{
                _ddFsm.InsertMethod(state, () =>
				{
                    IEnumerator WaitToRage()
					{
                        yield return new WaitWhile(() => dd.GetComponent<tk2dSpriteAnimator>().Playing);
                        _ddFsm.SetState("Rage Roar");
                    }
                    if(waitToRage == null) waitToRage = StartCoroutine(WaitToRage());
                }, 0);
            }
            _ddFsm.GetAction<Tk2dPlayAnimation>("Rage Roar", 1).Enabled = true;
            _ddFsm.InsertMethod("Rage Roar", () =>
            {
                _rbDD.velocity = Vector2.zero;
				_healthPool = _hm.hp = _hmDD.hp = FrenzyHP;
				EnemyHPBarImport.EnableHPBar(gameObject);
                preventDamage = false;
            }, 0);
            _ddFsm.InsertMethod("Set Rage", () => Destroy(_ddFsm.FsmVariables.FindFsmGameObject("Roar Emitter").Value), 0);

            // Wait until death and set variables
            yield return new WaitWhile(() => _healthPool > 0);
            float xSpd = _target.transform.GetPositionX() > dd.transform.GetPositionX() ? -20f : 20f;
            Destroy(_ddFsm.GetAction<FadeAudio>("Stun Recover", 2).gameObject.GameObject.Value);
            PlayDeathFor(dd);
            eliminateMinions = true;
            killAllMinions = true;
            _ddFsm.GetAction<SetVelocity2d>("Stun Launch", 0).y.Value = 45f;
            _ddFsm.GetAction<SetVelocity2d>("Stun Launch", 0).x.Value = xSpd;
            if(dd.transform.GetPositionY() < 9.1f) dd.transform.position = new Vector2(dd.transform.GetPositionX(), 9.1f);

            // Ogrim gets stunned and launched
            GGBossManager.Instance.PlayMusic(null, 1f);
            dd.layer = gameObject.layer = (int)GlobalEnums.PhysLayers.CORPSE;
            _ddFsm.SetState("Stun Set");
            yield return null;
            yield return new WaitWhile(() => _ddFsm.ActiveStateName == "Stun Set");
            PlayerData.instance.isInvincible = true;
            float x = CalculateTrajectory(new Vector2(xSpd, 45f), 5.1f - dd.transform.GetPositionY(), _rbDD.gravityScale) + dd.transform.GetPositionX();
            if(x < 68f) x = 68f;
            else if(x > 85f) x = 85f;
            yield return new WaitWhile(() => _ddFsm.ActiveStateName != "Stun In Air");
            yield return null;
            _ddFsm.enabled = false;

            // Isma starts moving to catch Ogrim
            yield return new WaitWhile(() => _rbDD.velocity.y > 0f);
            Log("Catching ogrim");
            ToggleIsma(true);
            _anim.Play("OgrimCatchIntro");
            float sign = Mathf.Sign(dd.transform.localScale.x);
            sc = gameObject.transform.localScale;
            transform.localScale = new Vector3(sign * Mathf.Abs(sc.x), sc.y, sc.z);
            transform.localScale *= 1.2f;
            transform.position = new Vector2(x + sign * 2f, GroundY + 2.05f);
            _rb.velocity = new Vector2(sign * -20f,0f);
            yield return null;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
            _rb.velocity = new Vector2(0f, 0f);
            IEnumerator OgrimCatchPos()
            {
                while(true)
                {
                    transform.position = new Vector2(dd.transform.GetPositionX(), transform.GetPositionY());
                    yield return null;
                }
            }
            Coroutine c = StartCoroutine(OgrimCatchPos());
            Log("After start coroutine");
            FiveKnights.journalEntries["Isma"].RecordJournalEntry();
            yield return new WaitWhile(() => !FastApproximately(transform.GetPositionY(), dd.transform.GetPositionY(), 1.6f) && dd.transform.GetPositionY() > 13f);
            Log("After wait while");
            if(c != null) StopCoroutine(c);
            if(dd.transform.GetPositionY() < 13f) //In case we don't catch ogrim
			{
				_anim.PlayAt("OgrimCatch", 2);
				transform.position = new Vector3(dd.transform.GetPositionX(), GroundY + 2.05f);
            }
			else
			{
                _anim.Play("OgrimCatch");
            }
            dd.SetActive(false);
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
            _anim.enabled = false;
            yield return new WaitForSeconds(0.65f);
            _anim.enabled = true;
            yield return null;

            // Jump after catching
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
            _rb.velocity = new Vector2(0f, 35f);
            GameCameras.instance.cameraShakeFSM.SendEvent("BigShake");
            yield return new WaitWhile(() => transform.position.y < 19f);
            PlayerData.instance.isInvincible = false;
            if (CustomWP.boss != CustomWP.Boss.All) yield return new WaitForSeconds(1f);
            CustomWP.wonLastFight = true;
            _ddFsm.enabled = false;
            Destroy(this);
        }

		private IEnumerator IsmaRage()
        {
            Log("Started Isma rage");

            _healthPool = _hm.hp = _hmDD.hp = MaxHPDuo;

            // Prevent music from cutting out
            void TransitionToAudioSnapshotOnEnter(On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.orig_OnEnter orig, TransitionToAudioSnapshot self)
            {
                if(self.State.Name == "Wake") return;
                else orig(self);
            }
            On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.OnEnter += TransitionToAudioSnapshotOnEnter;

            // Make Ogrim get stunned
            if(dd.transform.GetPositionY() < 9.1f) dd.transform.position = new Vector2(dd.transform.GetPositionX(), 9.1f);
            _ddFsm.SetState("Stun Set");

            // Disable burrow and pillars
            PlayMakerFSM burrow = GameObject.Find("Burrow Effect").LocateMyFSM("Burrow Effect");
            burrow.enabled = true;
            burrow.SendEvent("BURROW END");
            foreach(PlayMakerFSM pillar in dd.Find("Slam Pillars").GetComponentsInChildren<PlayMakerFSM>())
            {
                if(pillar.ActiveStateName == "Up" || pillar.ActiveStateName == "Hit")
                {
                    pillar.gameObject.GetComponent<MeshRenderer>().enabled = false;
                    pillar.SetState("Dormant");
                    pillar.FsmVariables.FindFsmGameObject("Chunks").Value.GetComponent<ParticleSystem>().Play();
                }
                else
                {
                    pillar.enabled = false;
                }
            }

            // Wait when he's down and make some changes
            yield return new WaitWhile(() => _ddFsm.ActiveStateName != "Stun Land");
            _ddFsm.enabled = false;
            burrow.enabled = false;
            yield return new WaitForSeconds(1f);
            foreach(FsmTransition i in _ddFsm.GetState("Idle").Transitions)
            {
                SFCore.Utils.FsmUtil.ChangeTransition(_ddFsm, "Idle", i.EventName, "Timer");
            }

            // Get up
            _ddFsm.enabled = true;
            _ddFsm.SetState("Stun Recover");

            // Reenable burrow effect and his fsm
            yield return new WaitForSeconds(0.5f);
            burrow.enabled = true;
            yield return new WaitWhile(() => !_ddFsm.ActiveStateName.Contains("Tunneling"));
            _ddFsm.enabled = false;

            // Start Agony
            yield return WaitToAttack();
            Coroutine c = StartCoroutine(LoopedAgony());
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);

            if(dd.transform.GetPositionX() < MiddleX) _rbDD.velocity = new Vector2(10f, 0f);
            else _rbDD.velocity = new Vector2(-10f, 0f);

            IEnumerator TrackIsmaPos()
            {
                yield return new WaitUntil(() => FastApproximately(dd.transform.GetPositionX(), MiddleX, 1f));
                _rbDD.velocity = new Vector2(0f, 0f);
			}
            StartCoroutine(TrackIsmaPos());

            // Wait for death
			_healthPool = _hm.hp = _hmDD.hp = FrenzyHP;
			EnemyHPBarImport.EnableHPBar(gameObject);
            preventDamage = false;
            yield return new WaitWhile(() => _healthPool > 0);

            eliminateMinions = true;
            killAllMinions = true;
            if (c != null) StopCoroutine(c);
            _anim.speed = 1f;
            foreach (SpriteRenderer i in gameObject.GetComponentsInChildren<SpriteRenderer>())
            {
                if (i.name.Contains("Isma")) continue;
                i.gameObject.SetActive(false);
            }
            Destroy(fakeIsma);
            float dir = FaceHero(true);
            _anim.enabled = true;
            yield return null;

            // Isma dies
            Destroy(_ddFsm.GetAction<FadeAudio>("Stun Recover", 2).gameObject.GameObject.Value);
			GGBossManager.Instance.PlayMusic(null, 1f);
            FiveKnights.journalEntries["Isma"].RecordJournalEntry();
            On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.OnEnter -= TransitionToAudioSnapshotOnEnter;
            PlayDeathFor(gameObject);
            _anim.Play("Falling");
            PlayerData.instance.isInvincible = true;

            _rb.gravityScale = 1.5f;
            float ismaXSpd = dir * 10f;
            _rb.velocity = new Vector2(ismaXSpd, 28f);

            // Ogrim starts tracking her again
            Vector3 scDD2 = dd.transform.localScale;
            float side2 = Mathf.Sign(gameObject.transform.localScale.x);

            dd.transform.localScale = new Vector3(side2 * Mathf.Abs(scDD2.x), scDD2.y, scDD2.z);
            
            while (_rb.velocity.y > -15f)
            {
                dd.transform.position = new Vector2(transform.GetPositionX(), dd.transform.GetPositionY());
                yield return new WaitForEndOfFrame();
            }
            _rb.velocity = new Vector2(0f, _rb.velocity.y);

            Vector3 scDD = dd.transform.localScale;
            float side = Mathf.Sign(gameObject.transform.localScale.x);

            dd.LocateMyFSM("Constrain X").FsmVariables.FindFsmFloat("Edge L").Value = 61.2f;
            dd.LocateMyFSM("Constrain X").FsmVariables.FindFsmFloat("Edge R").Value = 91f;
            _ddFsm.FsmVariables.FindFsmFloat("Dolphin Max X").Value = 87.18f;
            _ddFsm.FsmVariables.FindFsmFloat("Dolphin Min X").Value = 65.49f;
            _ddFsm.FsmVariables.FindFsmFloat("Max X").Value = 90.57f;
            _ddFsm.FsmVariables.FindFsmFloat("Min X").Value = 61.78f;

            // Ogrim jumps out to catch her
            _rbDD.velocity = new Vector2(5f, 0f);
            dd.transform.localScale = new Vector3(side * Mathf.Abs(scDD.x), scDD.y, scDD.z);
            _ddFsm.enabled = true;
            _ddFsm.FsmVariables.FindFsmBool("Intro Attack").Value = false;
            _ddFsm.SetState("First?");
            GameObject.Find("Burrow Effect").LocateMyFSM("Burrow Effect").SendEvent("BURROW END");
            yield return new WaitWhile(() => 
                !FastApproximately(transform.GetPositionY(), dd.transform.GetPositionY(), 0.9f));
            dd.GetComponent<MeshRenderer>().enabled = false;
            dd.SetActive(false);

            _sr.material = new Material(Shader.Find("Sprites/Default"));

            _anim.Play("Catch");
            gameObject.transform.localScale *= 1.25f;
            _rb.gravityScale = 0f;
            _rb.velocity = new Vector2(0f, 50f);
            yield return new WaitWhile(() => transform.position.y < 19f);
            PlayerData.instance.isInvincible = false;
            if (CustomWP.boss != CustomWP.Boss.All) yield return new WaitForSeconds(1f);
            CustomWP.wonLastFight = true;
            _ddFsm.enabled = false;
            Destroy(this);
        }

		private IEnumerator IsmaLoneDeath()
        {
            Log("Started Isma Lone Death");
            _healthPool = _hm.hp = FrenzyHP;
			EnemyHPBarImport.EnableHPBar(gameObject);
            preventDamage = false;
            Coroutine c = StartCoroutine(LoopedAgony());
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);

            // Wait for death
            while (_healthPool > 0)
            {
                _sr.enabled = true;
                _bc.enabled = true;
                yield return new WaitForEndOfFrame();
            }

            // Destroy objects and award achivement
            if(OWArenaFinder.IsInOverWorld) GameManager.instance.AwardAchievement("PALE_COURT_ISMA_ACH");
            FiveKnights.journalEntries["Isma"].RecordJournalEntry();

            eliminateMinions = true;
            killAllMinions = true;
            if (c != null) StopCoroutine(c);
            _anim.speed = 1f;
            foreach (SpriteRenderer i in gameObject.GetComponentsInChildren<SpriteRenderer>())
            {
                if (i.name.Contains("Isma")) continue;
                i.gameObject.SetActive(false);
            }
            Destroy(fakeIsma);

            // Actual death animation sequence
            float dir = FaceHero(true);
            _anim.enabled = true;
            yield return null;
            if (!OWArenaFinder.IsInOverWorld) GGBossManager.Instance.PlayMusic(null, 1f);
            else OWBossManager.PlayMusic(null);
            PlayDeathFor(gameObject);
            _bc.enabled = false;
            _rb.gravityScale = 1.5f;
            float ismaXSpd = dir * 10f;
            _rb.velocity = new Vector2(ismaXSpd, 28f);
            float side = Mathf.Sign(gameObject.transform.localScale.x);
            _anim.Play("LoneDeath");
            _anim.speed *= 0.7f;
            _anim.enabled = true;
            yield return null;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
            _anim.enabled = false;
            yield return new WaitWhile(() => transform.position.y > GroundY + 2.5f);
            transform.position = new Vector3(transform.position.x, GroundY + 2.25f, transform.position.z);
            var sc = transform.localScale;
            transform.localScale = new Vector3(sc.x * -1f, sc.y, sc.z);
            _anim.enabled = true;
            _rb.gravityScale = 0f;
            _rb.velocity = new Vector2(0f,0f);
            yield return _anim.WaitToFrame(4);
            _anim.speed = 1f;
            _anim.PlayAt("IsmaTired", 0);
            yield return new WaitForSeconds(1f);
            sc = transform.localScale;
            transform.localScale = new Vector3(sc.x * -1f, sc.y, sc.z);
            _anim.PlayAt("LoneDeath", 5);
            _anim.speed = 1f;
            _anim.enabled = true;
            yield return _anim.WaitToFrame(7);
            _rb.velocity = new Vector2(-side * 25f, 25f);
            yield return new WaitForSeconds(0.2f);
            _sr.enabled = false;
            yield return new WaitForSeconds(0.75f);
            if (!OWArenaFinder.IsInOverWorld) CustomWP.wonLastFight = true;
            Destroy(this);
        }
        
        private IEnumerator ChangeIntroText(GameObject area, string mainTxt, string subTxt, string supTxt, bool right)
        {
            area.SetActive(true);
            PlayMakerFSM fsm = area.LocateMyFSM("Area Title Control");
            fsm.FsmVariables.FindFsmBool("Visited").Value = true;
            fsm.FsmVariables.FindFsmBool("Display Right").Value = right;
            yield return null;
            GameObject parent = area.transform.Find("Title Small").gameObject;
            GameObject main = parent.transform.Find("Title Small Main").gameObject;
            GameObject super = parent.transform.Find("Title Small Super").gameObject;
            GameObject sub = parent.transform.Find("Title Small Sub").gameObject;
            main.GetComponent<TextMeshPro>().text = mainTxt;
            super.GetComponent<TextMeshPro>().text = supTxt;
            sub.GetComponent<TextMeshPro>().text = subTxt;
            Vector3 pos = parent.transform.position;
            parent.transform.position = new Vector3(pos.x, pos.y, -0.1f);
            yield return new WaitForSeconds(10f);
            if (area.name.Contains("Clone"))
            {
                Destroy(area);
            }
        }

        private void OnReceiveDreamImpact(On.EnemyDreamnailReaction.orig_RecieveDreamImpact orig, EnemyDreamnailReaction self)
        {
            if(self.name.Contains("Isma"))
            {
                Instantiate(_dnailEff, transform.position, Quaternion.identity);
            }
            if(self.name.Contains("White Defender"))
            {
                Mirror.SetField(self, "convoAmount", 5);
                self.SetConvoTitle("OGRIM_GG_DREAM");
            }
            orig(self);
        }
        
        private void SpellFlukeOnDoDamage(On.SpellFluke.orig_DoDamage orig, SpellFluke self, GameObject tar, int upwardrecursionamount, bool burst)
        {
            int damage = Mirror.GetField<SpellFluke, int>(self, "damage");
            DoTakeDamage(tar, damage, 0);
            orig(self, tar, upwardrecursionamount, burst);
        }

        private void HealthManagerTakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if(self.gameObject.name.Contains("Isma") && hitInstance.Source.name.Contains("Spike Ball"))
            {
                hitInstance.DamageDealt = GULKA_DAMAGE;
                if(_hm.hp - (int)(hitInstance.DamageDealt * hitInstance.Multiplier) <= 0)
                {
                    _hm.hp = 2;
                    hitInstance.DamageDealt = 1;
                    hitInstance.Multiplier = 1f;
                }
            }
            DoTakeDamage(self.gameObject, (int)(hitInstance.DamageDealt * hitInstance.Multiplier), hitInstance.Direction);
            if(self.gameObject.name.Contains("White Defender"))
			{
                if(_hmDD.hp - (int)(hitInstance.DamageDealt * hitInstance.Multiplier) <= 0)
				{
                    _hmDD.hp = 2;
                    hitInstance.DamageDealt = 1;
                    hitInstance.Multiplier = 1f;
				}
			}
            orig(self, hitInstance);
            _hmDD.hp = Math.Max(_healthPool, 1);
            _hm.hp = _healthPool;
        }

        private void HealthManagerDie(On.HealthManager.orig_Die orig, HealthManager self, float? attackDirection, AttackTypes attackType, bool ignoreEvasion)
        {
            if(self.gameObject.name.Contains("Isma")) return;
            orig(self, attackDirection, attackType, ignoreEvasion);
        }
        
        private void DoTakeDamage(GameObject tar, int damage, float dir)
        {
            if (tar.name.Contains("Isma"))
            {
                if (onlyIsma && _waitForHitStart)
                {
                    StopCoroutine("Start2");
                    _attacking = true;
                    _waitForHitStart = false;
                    StartCoroutine(BowWhipAttack());
                    StartCoroutine(SpawnWalls());
                    StartCoroutine(AttackChoice());
                }
                if(!preventDamage) _healthPool -= spawningWalls ? damage / 2 : damage;
                _hitEffects.RecieveHitEffect(dir);
                _isIsmaHitLast = true;
            }
            else if (tar.name.Contains("White Defender"))
            {
                if(!preventDamage) _healthPool -= spawningWalls ? damage / 2 : damage;
                _isIsmaHitLast = false;
            }
        }

        private void MarkDungBalls(On.HutongGames.PlayMaker.Actions.ReceivedDamage.orig_OnEnter orig, ReceivedDamage self)
        {
            orig(self);
			if(self.Fsm.Name.Contains("Nail Hit") && self.Fsm.GameObject.name.Contains("Dung Ball"))
			{
				Log("Player hit a ball, changing name to prevent Isma hitting it");
                self.Fsm.GameObject.AddComponent<DungTracker>();
			}
		}

        public class DungTracker : MonoBehaviour
		{
            private void OnDisable()
			{
                Destroy(this);
			}
		}

        private float FaceHero(bool opposite = false)
        {
            float heroSignX = Mathf.Sign(_target.transform.GetPositionX() - gameObject.transform.GetPositionX());
            heroSignX = opposite ? -1f * heroSignX : heroSignX;
            Vector3 pScale = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector3(Mathf.Abs(pScale.x) * heroSignX, pScale.y, 1f);
            return heroSignX;
        }

        private IEnumerator SilLeave()
        {
            SpriteRenderer sil = GameObject.Find("Silhouette Isma").GetComponent<SpriteRenderer>();
            sil.transform.localScale *= 1.2f;
            sil.sprite = ArenaFinder.Sprites["Sil_Isma_1"];
            yield return new WaitForSeconds(0.05f);
            sil.sprite = ArenaFinder.Sprites["Sil_Isma_2"];
            yield return new WaitForSeconds(0.05f);
            sil.sprite = ArenaFinder.Sprites["Sil_Isma_3"];
            yield return new WaitForSeconds(0.05f);
            Destroy(sil.gameObject);
        }

        private void ToggleIsma(bool visible)
        {
            _sr.enabled = visible;
            _bc.enabled = visible;
            _anim.PlayAt("Idle", 0);
        }

        private IEnumerator IdleTimer(float time)
        {
            yield return new WaitForSeconds(time);
            _attacking = false;
        }

        private void PositionIsma()
        {
            float xPos = 80f;
            float changeXPos = 0f;
            float ddX = dd.transform.GetPositionX();
            float heroX = _target.transform.GetPositionX();

            if (FastApproximately(xPos, ddX, 2f)) changeXPos += 4f;
            if (FastApproximately(xPos, heroX, 2f)) changeXPos -= 4f;
            xPos += changeXPos;
            gameObject.transform.position = new Vector3(xPos, GroundY, 1f);
        }

        private void PlayDeathFor(GameObject go)
        {
            GameObject eff1 = Instantiate(_ddDeathFx.uninfectedDeathPt);
            GameObject eff2 = Instantiate(_ddDeathFx.whiteWave);
            eff1.SetActive(true);
            eff2.SetActive(true);
            eff1.transform.position = eff2.transform.position = go.transform.position;
            _ddDeathFx.EmitSound();
            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
            if (go.name.Contains("Isma"))
            {
                this.PlayAudio(FiveKnights.Clips["IsmaAudDeath"], 1f);
            }
        }
        
        private void AssignFields(GameObject go)
        {
            HealthManager hm = go.GetComponent<HealthManager>();
            HealthManager hornHP = _hmDD;
            foreach (FieldInfo fi in typeof(HealthManager).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => x.Name.Contains("Prefab")))
            {
                fi.SetValue(hm, fi.GetValue(hornHP));
            }
            
            EnemyHitEffectsUninfected hitEff = go.GetComponent<EnemyHitEffectsUninfected>();
            EnemyHitEffectsUninfected ogrimHitEffects = dd.GetComponent<EnemyHitEffectsUninfected>();
            foreach (FieldInfo fi in typeof(EnemyHitEffectsUninfected).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                fi.SetValue(hitEff,
                    fi.Name.Contains("Origin") ? new Vector3(-0.2f, 1.3f, 0f) : fi.GetValue(ogrimHitEffects));
            }
            _ddDeathFx = _ddFsm.gameObject.GetComponent<EnemyDeathEffectsUninfected>();
        }

        private bool FastApproximately(float a, float b, float threshold)
        {
            return ((a - b) < 0 ? ((a - b) * -1) : (a - b)) <= threshold;
        }

        private void PlayVoice()
		{
            AudioClip voice = _randVoice[_rand.Next(0, _randVoice.Count)];
            this.PlayAudio(voice, 1f);
        }

        private IEnumerator WaitToAttack()
		{
            yield return new WaitWhile(() => _attacking);
            _attacking = true;
		}

        private void OnDestroy()
        {
            On.HealthManager.TakeDamage -= HealthManagerTakeDamage;
            On.HealthManager.Die -= HealthManagerDie;
            On.SpellFluke.DoDamage -= SpellFlukeOnDoDamage;
            On.EnemyDreamnailReaction.RecieveDreamImpact -= OnReceiveDreamImpact;
            On.HutongGames.PlayMaker.Actions.ReceivedDamage.OnEnter -= MarkDungBalls;

            // Revert acid texture to vanilla because it's shared
            //PlayMakerFSM noskFSM = FiveKnights.preloadedGO["Nosk"].LocateMyFSM("Mimic Spider");
            //GameObject acidOrig = noskFSM.GetAction<FlingObjectsFromGlobalPool>("Spit 1", 1).gameObject.Value;
            tk2dSpriteDefinition def = FiveKnights.preloadedGO["AcidSpit"].GetComponentInChildren<tk2dSprite>().GetCurrentSpriteDef();
            def.material.mainTexture = _acidTexture;
        }

        private void Log(object o)
        {
            if (!FiveKnights.isDebug) return;
            Modding.Logger.Log("[Isma] " + o);
        }
    }
}
