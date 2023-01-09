using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FiveKnights.BossManagement;
using FiveKnights.Ogrim;
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
        Rigidbody2D _rb;
        private EnemyDreamnailReaction _dnailReac;
        private EnemyHitEffectsUninfected _hitEffects;
        private EnemyDeathEffectsUninfected _deathEff;
        private MusicPlayer _ap;
        private MusicPlayer _voice;
        private GameObject _dnailEff;
        private SpriteRenderer _sr;
        private GameObject _target;
        private GameObject dd;
        private PlayMakerFSM _ddFsm;
        private Animator _anim;
        private List<AudioClip> _randAud;
        private System.Random _rand;
        private int _healthPool;
        private bool waitForHitStart;
        private readonly float LEFT_X = OWArenaFinder.IsInOverWorld ? 105f : 60.3f;
        private readonly float RIGHT_X = OWArenaFinder.IsInOverWorld ? 135f : 91.7f;
        private readonly float MIDDLE = OWArenaFinder.IsInOverWorld ? 120f : 76f;
        private readonly float GROUND_Y = 5.9f;
        
        private const int MAX_HP = 1500;
        private const int WALL_HP = 1000;
        private const int SPIKE_HP = 600;
        private const int MAX_HP_DUO = 1700;
        private const int WALL_HP_DUO = 1300;
        
        private const float IDLE_TIME = 0f; //0.1f;
        private const int GulkaSpitEnemyDamage = 20;
        public static float offsetTime;
        public static bool killAllMinions;
        private bool isDead;
        public static bool eliminateMinions;
        private bool isIsmaHitLast;
        public bool introDone;
        private const int MaxDreamAmount = 3;
        private bool usingThornPillars;
        private bool ogrimEvaded;

        private void Awake()
        {
            _hm = gameObject.AddComponent<HealthManager>();
            _sr = gameObject.GetComponent<SpriteRenderer>();
            _anim = gameObject.GetComponent<Animator>();
            _bc = gameObject.GetComponent<BoxCollider2D>();
            _dnailReac = gameObject.AddComponent<EnemyDreamnailReaction>();
            gameObject.AddComponent<AudioSource>();
            _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            dd = FiveKnights.preloadedGO["WD"];
            _hmDD = dd.GetComponent<HealthManager>();
            _ddFsm = dd.LocateMyFSM("Dung Defender");
            _dnailEff = dd.GetComponent<EnemyDreamnailReaction>().GetAttr<EnemyDreamnailReaction, GameObject>("dreamImpactPrefab");
            gameObject.AddComponent<Flash>();
            _dnailReac.enabled = true;
            Mirror.SetField(_dnailReac, "convoAmount", MaxDreamAmount);
            _rand = new System.Random();
            _randAud = new List<AudioClip>();
            _healthPool = onlyIsma? MAX_HP: MAX_HP_DUO;
            _hitEffects = gameObject.AddComponent<EnemyHitEffectsUninfected>();
            _hitEffects.enabled = true;
            _deathEff = gameObject.AddComponent<EnemyDeathEffectsUninfected>();
            _deathEff.SetJournalEntry(FiveKnights.journalentries["Isma"]);
            EnemyPlantSpawn.isPhase2 = false;
            EnemyPlantSpawn.FoolCount = EnemyPlantSpawn.PillarCount = EnemyPlantSpawn.TurretCount = 0;
            killAllMinions = eliminateMinions = false;
        }

        private IEnumerator Start()
        {
            Log("Begin Isma");
            yield return null;
            offsetTime = 0f;
            PlayMakerFSM spellControl = HeroController.instance.gameObject.LocateMyFSM("Spell Control");
            GameObject fireballParent = spellControl.GetAction<SpawnObjectFromGlobalPool>("Fireball 2", 3).gameObject.Value;
            PlayMakerFSM fireballCast = fireballParent.LocateMyFSM("Fireball Cast");
            GameObject actor = fireballCast.GetAction<AudioPlayerOneShotSingle>("Cast Right", 3).audioPlayer.Value;

            if (actor == null)
            {
                Log("ERROR: Actor not found.");
                yield break;
            }
            _ap = new MusicPlayer
            {
                Volume = 1f,
                Player = actor,
                MaxPitch = 1f,
                MinPitch = 1f,
                Spawn = gameObject//HeroController.instance.gameObject
            };
            _voice = new MusicPlayer
            {
                Volume = 1f,
                Player = actor,
                MaxPitch = 1f,
                MinPitch = 1f,
                Spawn = gameObject//HeroController.instance.gameObject
            };
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

            // Create missing objects for Godhome arena
            if(!OWArenaFinder.IsInOverWorld)
            {
                #region Acid Spit
                var noskFSM = FiveKnights.preloadedGO["Nosk"].LocateMyFSM("Mimic Spider");
                var acidOrig = noskFSM.GetAction<FlingObjectsFromGlobalPool>("Spit 1", 1).gameObject.Value;
                acidOrig = Instantiate(acidOrig);
                acidOrig.SetActive(false);

                // Change particle color to green
                var stmain = acidOrig.transform.Find("Steam").GetComponent<ParticleSystem>().main;
                var stamain = acidOrig.transform.Find("Air Steam").GetComponent<ParticleSystem>().main;
                stmain.startColor = new ParticleSystem.MinMaxGradient(new Color(128 / 255f, 226 / 255f, 169 / 255f, 217 / 255f));
                stamain.startColor = new ParticleSystem.MinMaxGradient(new Color(128 / 255f, 226 / 255f, 169 / 255f, 217 / 255f));
                // Get audio actor and audio clip
                var actorOrig = FiveKnights.preloadedGO["Nosk"].LocateMyFSM("Glob Audio")
                    .GetAction<AudioPlayerOneShotSingle>("SFX", 0).audioPlayer.Value;
                actorOrig.SetActive(false);
                var clip = FiveKnights.preloadedGO["Nosk"].LocateMyFSM("Glob Audio")
                    .GetAction<AudioPlayerOneShotSingle>("SFX", 0).audioClip.Value as AudioClip;
                // Change texture
                tk2dSpriteDefinition def = acidOrig.GetComponentInChildren<tk2dSprite>().GetCurrentSpriteDef();
                //acidOldTex = def.material.mainTexture;
                def.material.mainTexture = FiveKnights.SPRITES["acid_b"].texture;
                // Store values
                FiveKnights.Clips["AcidSpitSnd"] = clip;
                FiveKnights.preloadedGO["AcidSpit"] = acidOrig;
                FiveKnights.preloadedGO["AcidSpitPlayer"] = actorOrig;
                #endregion

                #region Seed columns
                GameObject sc = new GameObject();
                sc.name = "SeedCols";

                GameObject sf = new GameObject();
                sf.name = "SeedFloor";
                sf.transform.position = new Vector3(70.8f, 5.1f, 0f);
                BoxCollider2D sfcol = sf.AddComponent<BoxCollider2D>();
                sfcol.offset = new Vector2(3f, 0f);
                sfcol.size = new Vector2(19f, 1f);
                sf.transform.parent = sc.transform;

                if(onlyIsma)
				{
                    GameObject sr = new GameObject();
                    sr.name = "SeedSideR";
                    sr.transform.position = new Vector3(92.4f, 12.7f, 0f);
                    BoxCollider2D srcol = sr.AddComponent<BoxCollider2D>();
                    srcol.size = new Vector2(1f, 7f);
                    sr.transform.parent = sc.transform;

                    GameObject sl = new GameObject();
                    sl.name = "SeedSideL";
                    sl.transform.position = new Vector3(59.4f, 12.7f, 0f);
                    BoxCollider2D slcol = sl.AddComponent<BoxCollider2D>();
                    slcol.size = new Vector2(1f, 7f);
                    sl.transform.parent = sc.transform;
                }
				#endregion
			}

            if(!OWArenaFinder.IsInOverWorld && onlyIsma) Destroy(GameObject.Find("black_fader_GG"));

			foreach(Transform sidecols in GameObject.Find("SeedCols").transform)
            {
                sidecols.gameObject.AddComponent<EnemyPlantSpawn>();
                sidecols.gameObject.layer = (int)GlobalEnums.PhysLayers.ENEMY_DETECTOR;
            }

            if(OWArenaFinder.IsInOverWorld)
            {
                HeroController.instance.GetComponent<tk2dSpriteAnimator>().Play("Roar Lock");
                HeroController.instance.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
                HeroController.instance.RelinquishControl();
                HeroController.instance.StopAnimationControl();
                HeroController.instance.GetComponent<Rigidbody2D>().Sleep();
            }

            On.HealthManager.TakeDamage += HealthManager_TakeDamage;
            On.SpellFluke.DoDamage += SpellFlukeOnDoDamage;
            On.EnemyDreamnailReaction.RecieveDreamImpact += OnReceiveDreamImpact;
            AssignFields(gameObject);
            _ddFsm.FsmVariables.FindFsmInt("Rage HP").Value = 801;
            _hm.hp = _hmDD.hp = (onlyIsma ? MAX_HP : MAX_HP_DUO) + 200;
            gameObject.layer = 11;
            _target = HeroController.instance.gameObject;
            if (!onlyIsma) PositionIsma();
            else transform.position = new Vector3(LEFT_X + (RIGHT_X-LEFT_X)/1.5f, GROUND_Y, 1f);
            FaceHero();
            _bc.enabled = false;

            // Wait for a bit while Ogrim is down
            if(!OWArenaFinder.IsInOverWorld && !onlyIsma)
            {
                ToggleIsma(false);
                yield return new WaitForSeconds(1f);
                ToggleIsma(true);
            }

            _anim.Play("Apear"); // Yes I know it's "appear," I don't feel like changing the assetbundle buddo
            gameObject.transform.position = new Vector2(gameObject.transform.GetPositionX(), GROUND_Y + 8f);
            _rb.velocity = new Vector2(0f, -40f);
            yield return new WaitWhile(() => gameObject.transform.GetPositionY() > GROUND_Y);
            GameCameras.instance.cameraShakeFSM.SendEvent("SmallShake");
            _rb.velocity = new Vector2(0f, 0f);
            gameObject.transform.position = new Vector2(gameObject.transform.GetPositionX(), GROUND_Y);
            yield return new WaitWhile(() => _anim.IsPlaying());

            if(OWArenaFinder.IsInOverWorld)
            {
                yield return new WaitForSeconds(0.75f);
                HeroController.instance.RegainControl();
                HeroController.instance.StartAnimationControl();
            }

			StartCoroutine("Start2");
            if(OWArenaFinder.IsInOverWorld)
            {
                OWBossManager.PlayMusic(FiveKnights.Clips["LoneIsmaIntro"]);
                yield return new WaitForSeconds(FiveKnights.Clips["LoneIsmaIntro"].length);
                OWBossManager.PlayMusic(FiveKnights.Clips["LoneIsmaLoop"]);
            }
            else if(onlyIsma)
            {
                GGBossManager.Instance.PlayMusic(FiveKnights.Clips["LoneIsmaIntro"]);
                yield return new WaitForSeconds(FiveKnights.Clips["LoneIsmaIntro"].length);
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
            if (!onlyIsma) StartCoroutine(ChangeIntroText(area, "Ogrim", "", "Loyal", true));
            
            GameObject area2 = Instantiate(area);
            area2.SetActive(true);
            AreaTitleCtrl.ShowBossTitle(
                this, area2, 2f, 
                "","","",
                "Isma","Kindly");
            if(onlyIsma) _bc.enabled = true;
            waitForHitStart = true;
            yield return new WaitForSeconds(0.7f);
            _anim.Play("Bow");
            yield return new WaitForSeconds(0.05f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            yield return new WaitForSeconds(1f);
            _bc.enabled = false;
            _anim.Play("EndBow");
            yield return null;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
            waitForHitStart = false;
            _rb.velocity = new Vector2(dir * 20f, 10f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            _rb.velocity = Vector2.zero;
            ToggleIsma(false);
            _attacking = false;
			StartCoroutine(SpawnWalls());

            if(onlyIsma)
            {
                StartCoroutine(Agony());
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
            int lastA = 2;
            SeedBomb();

            while (true)
            {
                yield return new WaitWhile(() => _attacking);
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 0.8f) + offsetTime);
                yield return new WaitWhile(() => _attacking);
                _attacking = true;
                //= _rand.Next(100)
                float plantPercent = 100f;
                if (_healthPool > WALL_HP)
                {
                    plantPercent = (1f - (float) (EnemyPlantSpawn.FoolCount + EnemyPlantSpawn.TurretCount) / 
                        (EnemyPlantSpawn.MAX_FOOL + EnemyPlantSpawn.MAX_TURRET)) * 100;
                }
                bool throwBomb = _rand.Next(100) < plantPercent;
                int r = onlyIsma ? UnityEngine.Random.Range(0, 10) : UnityEngine.Random.Range(0, 7);
                if(spawningWalls || (EnemyPlantSpawn.FoolCount == 0 && EnemyPlantSpawn.TurretCount == 0 
                    && lastA != 2 && UnityEngine.Random.Range(0, 2) == 0))
                {
                    lastA = 2;
                    _prev = SeedBomb;
                }
                else if (r < 4 && lastA != 0)
                {
                    lastA = 0;
                    _prev = AirFist;
                }
                else if ((r < 8 && lastA != 1) || lastA == 0)
                {
                    lastA = 1;
                    _prev = WhipAttack;
                }
                else if (throwBomb && onlyIsma && lastA != 2)
                {
                    lastA = 2;
                    _prev = SeedBomb;
                }
                else
                {
                    lastA = 3;
                    _prev = AirFist;
                }

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

            // Increase delay after ground slam
            _ddFsm.GetAction<Wait>("G Slam Recover", 0).time = 1.2f;

            // WD rolls before using Ground Slam if in the middle of the arena
            _ddFsm.InsertMethod("G Slam Antic", () =>
            {
                if(!ogrimEvaded && (FastApproximately(dd.transform.GetPositionX(), 76f, 4f) || 
                    (dd.transform.GetPositionX() < 72f && dd.transform.GetPositionX() - _target.transform.GetPositionX() > 0f) || 
                    (dd.transform.GetPositionX() > 80f && dd.transform.GetPositionX() - _target.transform.GetPositionX() < 0f)))
				{
                    _ddFsm.SetState("Evade Dir");
                    _ddFsm.GetAction<SendRandomEvent>("After Evade", 0).weights[0].Value = 0f;
                    ogrimEvaded = true;
                }
				else
				{
                    _ddFsm.GetAction<SendRandomEvent>("After Evade", 0).weights[0].Value = 0.5f;
                    ogrimEvaded = false;
                }
            }, 0);

			// WD burrows for longer
			_ddFsm.GetAction<RandomFloat>("Timer", 1).min.Value = 2f;
			_ddFsm.GetAction<RandomFloat>("Timer", 1).max.Value = 2f;

            // Make WD bounce around the arena at a consistent speed
            _ddFsm.InsertMethod("RJ Launch", 6, () =>
            {
                _ddFsm.FsmVariables.FindFsmFloat("Throw Speed Crt").Value = 12f;
            });

            // Add anticheese to bounce by adding to counter when knight hits WD
            _ddFsm.InsertMethod("Ball Hit Up", () =>
            {
                _ddFsm.FsmVariables.FindFsmInt("Bounces").Value--;
            }, 0);
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
                usingThornPillars = false;
				IEnumerator WaitForThornPillars()
				{
                    yield return WaitToAttack();
                    ThornPillars();
				}
				StartCoroutine(WaitForThornPillars());
			}, 0);
            _ddFsm.InsertMethod("RJ Speed Adjust", () => usingThornPillars = true, 0);

            // Ogrim Strike - After bouncing
            StartCoroutine(OgrimStrike());

            yield return new WaitWhile(() => _healthPool > SPIKE_HP);

            // Agony - Spike wave
            _ddFsm.InsertMethod("Under", () =>
            {
                IEnumerator WaitForAgony()
                {
                    yield return new WaitForSeconds(0.5f);
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
                yield return new WaitWhile(() => _healthPool > (onlyIsma ? WALL_HP : WALL_HP_DUO));
                EnemyPlantSpawn.isPhase2 = true;
                killAllMinions = true;
                yield return new WaitForSeconds(0.1f);
                killAllMinions = false;
                preventDamage = true;
                spawningWalls = true;
                if(onlyIsma)
                {
                    yield return new WaitWhile(() => spawningWalls);
                    yield return new WaitForSeconds(1f);
                    EnemyPlantSpawn spawner = GameObject.Find("SeedFloor").GetComponent<EnemyPlantSpawn>();
                    spawner.Phase2Spawn();
                }
                else
                {
                    yield return new WaitUntil(() => _ddFsm.ActiveStateName == "Idle");
                    _ddFsm.InsertMethod("Rage?", () => _ddFsm.SetState("TD Set"), 0);
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
                preventDamage = false;
                _wallActive = true;

                wallR = Instantiate(FiveKnights.preloadedGO["Wall"]);
                wallR.transform.localScale = new Vector3(wallR.transform.localScale.x * -1f, wallR.transform.localScale.y, wallR.transform.localScale.z);
                wallL = Instantiate(FiveKnights.preloadedGO["Wall"]);
                GameObject frontWR = wallR.transform.Find("FrontW").gameObject;
                GameObject frontWL = wallL.transform.Find("FrontW").gameObject;
                frontWR.layer = 8;
                frontWL.layer = 8;
                wallR.transform.position = new Vector2(RIGHT_X - 2f, GROUND_Y);
                wallL.transform.position = new Vector2(LEFT_X + 2f, GROUND_Y);
                Animator anim = frontWR.GetComponent<Animator>();
                yield return new WaitWhile(() => anim.GetCurrentFrame() < 3);
                wallR.transform.Find("Petal").gameObject.SetActive(true);
                wallL.transform.Find("Petal").gameObject.SetActive(true);
                Vector2 hPos = _target.transform.position;
                if(hPos.x > RIGHT_X - 6f) _target.transform.position = new Vector2(RIGHT_X - 6f, hPos.y);
                else if(hPos.x < LEFT_X + 6f) _target.transform.position = new Vector2(LEFT_X + 6f, hPos.y);

                yield return new WaitWhile(() => _healthPool > SPIKE_HP);

                preventDamage = true;
                spawningWalls = true;
                if(onlyIsma)
                {
                    yield return new WaitWhile(() => spawningWalls);
                    yield return new WaitForSeconds(1f);
                }
                else
				{
                    yield return new WaitUntil(() => _ddFsm.ActiveStateName == "Idle");
                    _ddFsm.InsertMethod("Rage?", () => _ddFsm.SetState("TD Set"), 0);
                    yield return new WaitUntil(() => _ddFsm.ActiveStateName.Contains("Tunneling"));
                    _ddFsm.RemoveAction("Rage?", 0);
                    yield return new WaitForSeconds(0.3f);
                    yield return new WaitWhile(() => spawningWalls);
                    yield return new WaitForSeconds(1f);
                }
                preventDamage = false;

                foreach(GameObject wall in new[] { wallR, wallL })
                {
                    GameObject spike = wall.transform.Find("Spike").gameObject;
                    GameObject spikeFront = spike.transform.Find("Front").gameObject;
                    spikeFront.layer = 17;
                    spikeFront.AddComponent<DamageHero>().damageDealt = 1;

                    spikeFront.AddComponent<ThornSplat>();
                    spikeFront.AddComponent<NonBouncer>();
                    spike.SetActive(true);
                }

                eliminateMinions = false;
            }
            yield return new WaitWhile(() => !eliminateMinions);
            
            foreach (GameObject wall in new[] {wallL, wallR})
            {
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
                float ismaX = heroX - MIDDLE > 0f ? LEFT_X + 8f : RIGHT_X - 8f;
                if (_wallActive) ismaX = heroX - MIDDLE > 0f ? LEFT_X + 11f : RIGHT_X - 11f;
                transform.position = new Vector2(ismaX, GROUND_Y);
                dir = FaceHero();
                _rb.velocity = new Vector2(-20f * dir, 0f);
                ToggleIsma(true);
                _anim.Play("ThrowBomb");
                _voice.Clip = _randAud[_rand.Next(0, _randAud.Count)];
                _voice.DoPlayRandomClip();
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
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
                Vector3 scale = seed.transform.localScale;
                scale *= 2f;
                scale.x *= -1f;
                seed.transform.localScale = scale;
                seed.layer = (int)GlobalEnums.PhysLayers.ENEMIES;
                Destroy(seed.GetComponent<DamageHero>());

                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
                bomb.SetActive(true);
                rb.gravityScale = 1.3f;
                StartCoroutine(AnimEnder());
                rb.velocity = new Vector2(dir * 30f, 40f);
                CollisionCheck cc = bomb.AddComponent<CollisionCheck>();
                rb.angularVelocity = dir * 900f;
                yield return new WaitWhile(() => !cc.Hit && !FastApproximately(bomb.transform.GetPositionX(), MIDDLE, 0.2f));
                anim.Play("Bomb");
                yield return new WaitWhile(() => anim.GetCurrentFrame() < 1);
                rb.angularVelocity = 0f;
                rb.gravityScale = 0f;
                rb.velocity = Vector2.zero;
                _ap.Clip = FiveKnights.Clips["IsmaAudSeedBomb"];
                _ap.DoPlayRandomClip();
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
                    }
                    firstBomb = false;
                }
                else if(spawningWalls)
                {
                    for(int i = 0; i < 8; i++)
                    {
                        Vector2 targetPos = new Vector2(i < 4 ? LEFT_X + 2f * (i + 1) : RIGHT_X + 2f * (i - 8), 7.3f);
                        Vector2 path = targetPos - (Vector2)bomb.transform.position;
                        float rot = Mathf.Atan2(path.y, path.x);

                        GameObject localSeed = Instantiate(seed, bomb.transform.position, Quaternion.Euler(0f, 0f, rot));
                        localSeed.name = "VineWallSeed";
                        localSeed.SetActive(true);
                        localSeed.GetComponent<Rigidbody2D>().velocity =
                            new Vector2(30f * Mathf.Cos(rot), 30f * Mathf.Sin(rot));
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
                    }
                }
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
                yield return _anim.PlayToFrame("ThrowBomb", 8);
                yield return new WaitForSeconds(0.08f);
                transform.position += new Vector3(0f, 0.2f);
                yield return _anim.PlayToEnd();
                ToggleIsma(false);
                StartCoroutine(IdleTimer(IDLE_TIME));
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
                float ismaX = heroX - MIDDLE > 0f ? heroX - distance : heroX + distance;

                if (_wallActive)
                {
                    float rightMost = RIGHT_X - 8;
                    float leftMost = LEFT_X + 10;
                    ismaX = heroX - MIDDLE > 0f 
                        ? UnityEngine.Random.Range((int)leftMost, (int)heroX - 6) 
                        : UnityEngine.Random.Range((int)heroX + 6, (int)rightMost);
                }
                
                transform.position = new Vector2(ismaX, UnityEngine.Random.Range(13, 16));
                ToggleIsma(true);
                float dir = FaceHero();
                
                arm = transform.Find("Arm2").gameObject;
                _anim.Play("AFistAntic");
                _voice.Clip = _randAud[_rand.Next(0, _randAud.Count)];
                _voice.DoPlayRandomClip();
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
                
                Vector2 heroVel = _target.GetComponent<Rigidbody2D>().velocity;
                //float predTime = 0.4f;
                //float yOff = 0.5f;
                //float xOff = 0.8f;
                //Vector3 predPos = _target.transform.position + new Vector3(heroVel.x * xOff, heroVel.y * yOff) * predTime;
                float rot = GetRot(arm, _target.transform.position, dir);
                float rotD = rot * Mathf.Rad2Deg;

                if(rotD is < -60f or > 50f)
                {
                    _anim.enabled = true;
                    spike.SetActive(false);
                    yield return AcidThrow();
                    yield break;
                }
                
                arm.transform.SetRotation2D(rot * Mathf.Rad2Deg);
                GameObject parArm = arm.transform.Find("TentArm").gameObject;
                parArm.SetActive(false);
                tentArm = Instantiate(parArm, parArm.transform.position, parArm.transform.rotation);
                tentArm.SetActive(false);
                Vector3 tentArmScale = tentArm.transform.localScale;
                tentArm.transform.localScale = new Vector3(dir * tentArmScale.x, tentArmScale.y, tentArmScale.z) * 1.35f;
                
                yield return new WaitForSeconds(0.1f);
                spike.SetActive(false);
                _anim.enabled = true;
                arm.SetActive(true);
                tentArm.SetActive(true);
                tentArm.AddComponent<AFistFlash>();

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
                //_anim.Play("AFistEnd");
                //yield return null;
                yield return _anim.PlayBlocking("AFistEnd");
                //transform.position += new Vector3(0.81f * Math.Sign(transform.localScale.x), 0.27f);
                //_anim.enabled = false;
                //yield return new WaitForSeconds(spd);
                //_anim.enabled = true;
                //_rb.velocity = new Vector2(dir * -20f, 0f);
                //yield return new WaitWhile(() => _anim.IsPlaying());
                //_rb.velocity = Vector2.zero;
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
            
            yield return _anim.PlayToFrame("AcidSwipe", 6);
            
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
                    PlayGndSnd();
                    var pos = transform.position;
                    transform.position = new Vector3(pos.x, SlimeY, pos.z);
                    _fsm.SetState("Land");
                }
            }

            void PlayGndSnd()
            {
                var actor = Instantiate(FiveKnights.preloadedGO["AcidSpitPlayer"]);
                actor.transform.position = HeroController.instance.transform.position;
                var aud = actor.GetComponent<AudioSource>();
                actor.SetActive(true);
                aud.enabled = true;
                aud.clip = null;
                aud.pitch = aud.volume = 1f;
                aud.PlayOneShot(FiveKnights.Clips["AcidSpitSnd"], 1f);
                Destroy(this);
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
                float ismaX = heroX - MIDDLE > 0f ? LEFT_X + 8f : RIGHT_X - 8f;
                if (_wallActive) ismaX = heroX - MIDDLE > 0f ? LEFT_X + 11f : RIGHT_X - 11f;
                transform.position = new Vector2(ismaX, GROUND_Y);
                float dir = FaceHero();
                ToggleIsma(true);
                _rb.velocity = new Vector2(dir * -20f, 0f);
                _anim.Play("GFistAntic");
                _voice.Clip = _randAud[_rand.Next(0, _randAud.Count)];
                _voice.DoPlayRandomClip();
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                _rb.velocity = Vector2.zero;
                _anim.enabled = false;
                yield return new WaitForSeconds(0.7f);
                _anim.enabled = true;
                transform.position += new Vector3(WhipXOffset * Math.Sign(transform.localScale.x), WhipYOffset, 0f);
                var oldWhip = transform.Find("Whip").gameObject;
                whip = Instantiate(oldWhip);
                whip.transform.position = oldWhip.transform.position;
                whip.transform.localScale = oldWhip.transform.lossyScale;
                whip.SetActive(true);
                whip.AddComponent<WhipFlash>();
                _ap.Clip = FiveKnights.Clips["IsmaAudGroundWhip"];
                _ap.DoPlayRandomClip();
                
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
                
                transform.position -= new Vector3(WhipXOffset * Math.Sign(transform.localScale.x), WhipYOffset, 0f);
                yield return _anim.PlayBlocking("GFistEnd");
                ToggleIsma(false);
                Destroy(whip);
                StartCoroutine(IdleTimer(IDLE_TIME));
            }

            StartCoroutine(WhipAttack());
        }

        private IEnumerator BowWhipAttack()
        {
            float dir = -1f * FaceHero(true);
            _voice.Clip = _randAud[_rand.Next(0, _randAud.Count)];
            _voice.DoPlayRandomClip();
            _anim.Play("LoneDeath");
            _rb.velocity = new Vector2(-dir * 17f, 32f);
            _rb.gravityScale = 1.5f;
            _anim.speed *= 0.9f;
            yield return null;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
            _anim.enabled = false;
            yield return new WaitWhile(() => transform.position.y > GROUND_Y + 2.5f);
            transform.position = new Vector3(transform.position.x, GROUND_Y + 2.5f, transform.position.z);
            _anim.enabled = true;
            _rb.gravityScale = 0f;
            _rb.velocity = new Vector2(0f, 0f);
            yield return new WaitForSeconds(0.1f);
            dir = FaceHero();
            transform.position = new Vector3(transform.position.x, GROUND_Y, transform.position.z);
            Log("Start play");
            transform.position += new Vector3(WhipXOffset * Math.Sign(transform.localScale.x), WhipYOffset, 0f);
            
            var oldWhip = transform.Find("Whip").gameObject;
            var whip = Instantiate(oldWhip);
            whip.transform.position = oldWhip.transform.position;
            whip.transform.localScale = oldWhip.transform.lossyScale;
            whip.SetActive(true);
            whip.AddComponent<WhipFlash>();
            _ap.Clip = FiveKnights.Clips["IsmaAudGroundWhip"];
            _ap.DoPlayRandomClip();

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
           
            //_anim.Play("GFistEnd");
            transform.position -= new Vector3(WhipXOffset * Math.Sign(transform.localScale.x), WhipYOffset, 0f);
            yield return _anim.PlayBlocking("GFistEnd");
            //yield return null;
            //yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
            //_anim.enabled = false;
            //yield return new WaitForSeconds(0.5f);
            //_anim.enabled = true;
            //yield return null;
            //yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
            //_rb.velocity = new Vector2(dir * -20f, 0f);
            //yield return new WaitWhile(() => _anim.IsPlaying());
            //_rb.velocity = Vector2.zero;
            ToggleIsma(false);
            StartCoroutine(IdleTimer(IDLE_TIME));
        }

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
                    GameObject go = LocateBall();
                    if(go == null)
					{
                        if(_ddFsm.FsmVariables.FindFsmInt("Rages").Value % 2 == 1) prevRageBallMissed = true;
                        yield return new WaitForEndOfFrame();
                        continue;
                    }
                    prevRageBallMissed = false;

                    Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
                    float xPos = CalculateTrajectory(rb.velocity, 16f - go.transform.GetPositionY(), rb.gravityScale) + 
                        rb.velocity.x * 0.05f + go.transform.GetPositionX();
                    Vector2 pos = new Vector2(xPos, go.transform.GetPositionY());

                    ToggleIsma(true);
                    _voice.Clip = _randAud[_rand.Next(0, _randAud.Count)];
                    _voice.DoPlayRandomClip();
                    float side = go.GetComponent<Rigidbody2D>().velocity.x > 0f ? 1f : -1f;
                    gameObject.transform.position = new Vector2(pos.x + side * 1.77f, pos.y + 0.38f);
                    float dir = FaceHero();
                    GameObject squish = gameObject.transform.Find("Squish").gameObject;
                    GameObject ball = Instantiate(gameObject.transform.Find("Ball").gameObject);
                    GameObject particles = Instantiate(go.LocateMyFSM("Ball Control").FsmVariables.FindFsmGameObject("Break Chunks").Value);
                    ball.name = "IsmaHitBall";
                    ball.transform.localScale *= 1.4f;
                    ball.layer = 11;
                    ball.AddComponent<DamageHero>().damageDealt = 1;
                    DungBall db = ball.AddComponent<DungBall>();
                    db.particles = particles;
                    db.usingThornPillars = usingThornPillars;

                    _anim.Play("BallStrike");
                    yield return new WaitForSeconds(0.05f);
                    yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                    // Check if ball has been destroyed before hitting it
                    if(go != null)
                    {
                        Destroy(go);
                        squish.SetActive(true);
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
                    yield return new WaitWhile(() => _anim.GetCurrentFrame() < 6);
                    _rb.velocity = new Vector2(dir * 20f, 0f);
                    yield return new WaitWhile(() => _anim.IsPlaying());
                    _rb.velocity = new Vector2(0f, 0f);
                    ToggleIsma(false);
                    yield return new WaitForEndOfFrame();
                }
                StartCoroutine(IdleTimer(IDLE_TIME));
                yield return new WaitForSeconds(0.75f);
            }
        }

        private GameObject LocateBall()
		{
            tk2dSpriteAnimator tk = dd.GetComponent<tk2dSpriteAnimator>();
            GameObject[] balls = FindObjectsOfType<GameObject>().Where(x =>
                x.name.Contains("Dung Ball") && x.activeSelf &&
                x.transform.GetPositionY() > 16f &&
				x.GetComponent<Rigidbody2D>().velocity.y > 0f &&
				(tk.CurrentClip.name.Contains("Throw") || FastApproximately(x.transform.GetPositionX(), _target.transform.GetPositionX(), 7.5f))).ToArray();
            if(balls.Length > 0) return balls[_rand.Next(0, balls.Length)];
            return null;
        }

        private IEnumerator OgrimStrike()
		{
            tk2dSpriteAnimator tk = dd.GetComponent<tk2dSpriteAnimator>();
            Rigidbody2D rb = dd.GetComponent<Rigidbody2D>();
            _ddFsm.GetAction<BoolTestMulti>("RJ In Air", 8).Enabled = false;
            _ddFsm.GetAction<SetVelocity2d>("Air Dive", 4).Enabled = false;
            _ddFsm.InsertMethod("AD In", () =>
            {
                rb.velocity = new Vector2(0f, rb.velocity.y);
            }, 1);
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
                yield return new WaitUntil(() => (!_ddFsm.FsmVariables.FindFsmBool("Still Bouncing").Value
                    && _ddFsm.FsmVariables.FindFsmBool("Air Dive Height").Value) ||
                    (FastApproximately(rb.velocity.x, 0f, 1f) && _ddFsm.FsmVariables.FindFsmInt("Bounces").Value < -3) ||
                    startedIsmaRage);
                if(startedIsmaRage)
                {
                    _attacking = false;
                    break;
                }

                _ddFsm.SetState("Air Dive Antic");

                Vector2 pos = dd.transform.position;

                _voice.Clip = _randAud[_rand.Next(0, _randAud.Count)];
                _voice.DoPlayRandomClip();
                float side = rb.velocity.x > 0f ? 1f : -1f;
                gameObject.transform.position = new Vector2(pos.x, pos.y);
                float dir = FaceHero();

                Vector2 diff = dd.transform.position - new Vector3(_target.transform.GetPositionX(), GROUND_Y);
                float offset2 = 0f;
                if(diff.x > 0)
                {
                    offset2 += 180f;
                }
                float rot = Mathf.Atan(diff.y / diff.x) + offset2 * Mathf.Deg2Rad;
                Vector2 vel = new Vector2(40f * Mathf.Cos(rot), 35f * Mathf.Sin(rot));
                bool setVel = false;
                _ddFsm.InsertMethod("Air Dive", () =>
                {
                    dd.transform.SetRotation2D(rot * Mathf.Rad2Deg + 90f);
                    rb.velocity = vel;
                    setVel = true;
                }, 2);

                yield return new WaitForSeconds(0.2f);
                ToggleIsma(true);
                _anim.Play("BallStrike");
                yield return new WaitForSeconds(0.05f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() <= 2);
                yield return new WaitUntil(() => setVel);
                _ddFsm.RemoveAction("Air Dive", 2);
                yield return new WaitForSeconds(0.2f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 6);
                _rb.velocity = new Vector2(dir * 20f, 0f);
                yield return new WaitWhile(() => _anim.IsPlaying());
                _rb.velocity = new Vector2(0f, 0f);
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
                float ismaX = heroX - MIDDLE > 0f ? LEFT_X + 11f : RIGHT_X - 9f;
                transform.position = new Vector2(ismaX, GROUND_Y);
                float dir = FaceHero();
                ToggleIsma(true);
                _rb.velocity = new Vector2(dir * -20f, 0f);
                _anim.Play("ThornPillarsAntic");
                _voice.Clip = _randAud[_rand.Next(0, _randAud.Count)];
                _voice.DoPlayRandomClip();
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

                float center = MIDDLE + UnityEngine.Random.Range(-1f, 1f);
                for(int i = -2; i < 5; i++)
				{
                    GameObject pillar = Instantiate(FiveKnights.preloadedGO["ThornPlant"]);
                    pillar.AddComponent<ThornPlantCtrl>();
                    pillar.GetComponent<BoxCollider2D>().enabled = false;
                    pillar.layer = (int)GlobalEnums.PhysLayers.ENEMY_ATTACK;
                    pillar.transform.position = new Vector2(MIDDLE + 4f * i * dir, 13.4f);
                    yield return new WaitForSeconds(0.1f);
                }
                yield return new WaitForSeconds(1f);
                for(int i = -2; i < 5; i++)
                {
                    GameObject pillar = Instantiate(FiveKnights.preloadedGO["ThornPlant"]);
                    pillar.AddComponent<ThornPlantCtrl>().secondWave = true;
                    pillar.GetComponent<BoxCollider2D>().enabled = false;
                    pillar.layer = (int)GlobalEnums.PhysLayers.ENEMY_ATTACK;
                    pillar.transform.position = new Vector2(MIDDLE + 4f * i * dir + 2f * dir, 13.4f);
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

        private GameObject agonyThorns;

        private IEnumerator Agony()
        {
            if(onlyIsma)
			{
                yield return new WaitUntil(() => _wallActive);
                yield return WaitToAttack();
            }

            ToggleIsma(true);
            Vector3 scIs = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector3(Mathf.Abs(scIs.x), scIs.y, scIs.z);
            gameObject.transform.SetPosition2D(MIDDLE, GROUND_Y + 11.6f);

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
            _voice.Clip = _randAud[_rand.Next(0, _randAud.Count)];
            _voice.DoPlayRandomClip();
            _anim.speed = 1.7f;

            yield return PerformAgony(agonyThorns, tAnim, onlyIsma ? 3 : 1);

            _anim.Play("AgonyLoopEnd");
            yield return null;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
            _rb.velocity = new Vector2(20f, 0f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            _anim.speed = 1f;
            _rb.velocity = Vector2.zero;
            ToggleIsma(false);
            StartCoroutine(IdleTimer(IDLE_TIME));
        }

        private IEnumerator PerformAgony(GameObject thorn, Animator tAnim, int loops = 0)
		{
            bool repeat = loops == 0;
            for(int j = 0; j < loops || repeat; j++)
            {
                _anim.PlayAt("AgonyLoop", 0);

                _ap.Clip = FiveKnights.Clips["IsmaAudAgonyIntro"];
                _ap.DoPlayRandomClip();

                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
                thorn.SetActive(true);

                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 9);


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
                    t1.gameObject.transform.rotation = Quaternion.Euler(0f, 0f, currRot - smallOff);
                    t2.gameObject.transform.rotation = Quaternion.Euler(0f, 0f, currRot + smallOff);
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

                yield return new WaitWhile(() => tAnim.GetCurrentFrame() < 5);
                _ap.Clip = FiveKnights.Clips["IsmaAudAgonyShoot"];
                _ap.DoPlayRandomClip();


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

        private GameObject fakeIsma;
        private float agonyAnimSpd = 1.2f;
        private Vector2 agonySpread = new Vector2(30f, 40f);

        private IEnumerator LoopedAgony()
        {
            ToggleIsma(true);
            _attacking = true;
            gameObject.transform.SetPosition2D(MIDDLE, GROUND_Y + 11.1f);
            
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
            if(_healthPool <= 0 && !isDead)
            {
                Log("Victory");
                isDead = true;
                _healthPool = 100;
                if(onlyIsma)
                {
                    StartCoroutine(IsmaLoneDeath());
                }
                else if(isIsmaHitLast)
                {
                    startedOgrimRage = true;
                    preventDamage = true;
                    StopAllCoroutines();
                    StartCoroutine(OgrimRage());
                    StartCoroutine(SpawnWalls());
                }
                else
                {
                    startedIsmaRage = true;
                    preventDamage = true;
                    StartCoroutine(IsmaRage());
                }
            }
        }

        private bool startedOgrimRage;
        private bool startedIsmaRage;
        private bool preventDamage;

        private IEnumerator OgrimRage()
        {
            Log("Started Ogrim rage");

            _attacking = true;
            _healthPool = _hm.hp = _hmDD.hp = MAX_HP_DUO;

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
            yield return new WaitWhile(() => transform.position.y > GROUND_Y + 2.5f);
            transform.position = new Vector3(transform.position.x, GROUND_Y + 2.25f, transform.position.z);
            var sc = transform.localScale;
            transform.localScale = new Vector3(sc.x * -1f, sc.y, sc.z);
            _anim.enabled = true;
            _rb.gravityScale = 0f;
            _rb.velocity = new Vector2(0f, 0f);
            yield return _anim.WaitToFrame(4);
            _anim.speed = 1f;
            _anim.PlayAt("IsmaTired", 0);
            yield return new WaitForSeconds(1f);
            transform.position = new Vector3(transform.position.x, GROUND_Y + 2.35f, transform.position.z);
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

            // FSM edits to start rage
            _ddFsm.GetAction<Wait>("AD In", 0).time.Value = 0.25f;
            _ddFsm.GetAction<BoolTestMulti>("RJ In Air", 8).Enabled = true;
            _ddFsm.GetAction<SetVelocity2d>("Air Dive", 4).Enabled = true;
            _ddFsm.GetAction<Tk2dPlayAnimationWithEvents>("Air Dive Antic", 1).Enabled = true;
            _ddFsm.GetAction<SetIntValue>("Set Rage", 1).intValue = 999;
            foreach(string state in new string[] { "Idle", "Move Choice", "After Throw?", "After Evade" })
			{
                _ddFsm.InsertMethod(state, () =>
				{
                    IEnumerator WaitToRage()
					{
                        yield return new WaitWhile(() => dd.GetComponent<tk2dSpriteAnimator>().Playing);
                        _ddFsm.SetState("Rage Roar");
                    }
                    StartCoroutine(WaitToRage());
                }, 0);
            }
            _ddFsm.GetAction<Tk2dPlayAnimation>("Rage Roar", 1).Enabled = true;
            _ddFsm.InsertMethod("Rage Roar", () =>
            {
                dd.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            }, 0);

            // Wait until death and set variables
            _healthPool = 180;
            preventDamage = false;
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
            float x = CalculateTrajectory(new Vector2(xSpd, 45f), 5.1f - dd.transform.GetPositionY(), dd.GetComponent<Rigidbody2D>().gravityScale) + dd.transform.GetPositionX();
            if(x < 68f) x = 68f;
            else if(x > 85f) x = 85f;
            yield return new WaitWhile(() => _ddFsm.ActiveStateName != "Stun In Air");
            yield return null;
            _ddFsm.enabled = false;
            Rigidbody2D ogrimRb = dd.GetComponent<Rigidbody2D>();

            // Isma starts moving to catch Ogrim
            yield return new WaitWhile(() => ogrimRb.velocity.y > 0f);
            Destroy(_ddFsm.GetAction<FadeAudio>("Stun Recover", 2).gameObject.GameObject.Value);
            Log("Catching ogrim");
            ToggleIsma(true);
            _anim.Play("OgrimCatchIntro");
            float sign = Mathf.Sign(dd.transform.localScale.x);
            sc = gameObject.transform.localScale;
            transform.localScale = new Vector3(sign * Mathf.Abs(sc.x), sc.y, sc.z);
            transform.localScale *= 1.2f;
            transform.position = new Vector2(x + sign * 2f, GROUND_Y + 2.05f);
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
            _deathEff.RecordJournalEntry();
            yield return new WaitWhile(() => !FastApproximately(transform.GetPositionY(), dd.transform.GetPositionY(), 1.6f) && dd.transform.GetPositionY() > 13f);
            Log("After wait while");
            if(c != null) StopCoroutine(c);
            if(dd.transform.GetPositionY() < 13f) //In case we don't catch ogrim
			{
				_anim.PlayAt("OgrimCatch", 2);
				transform.position = new Vector3(dd.transform.GetPositionX(), GROUND_Y + 2.05f);
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

            _healthPool = _hm.hp = _hmDD.hp = MAX_HP_DUO;

            void TransitionToAudioSnapshotOnEnter(On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.orig_OnEnter orig, TransitionToAudioSnapshot self)
            {
                if(self.State.Name == "Wake") return;
                else orig(self);
            }
            On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.OnEnter += TransitionToAudioSnapshotOnEnter;

            //Make Ogrim get stunned
            _ddFsm.SetState("Stun Set");
            yield return new WaitWhile(() => _ddFsm.ActiveStateName != "Stun Land");
            _ddFsm.enabled = false;
            yield return new WaitForSeconds(1f);
            foreach(FsmTransition i in _ddFsm.GetState("Idle").Transitions)
            {
                SFCore.Utils.FsmUtil.ChangeTransition(_ddFsm, "Idle", i.EventName, "Timer");
            }
            _ddFsm.enabled = true;
            _ddFsm.SetState("Stun Recover");
            yield return new WaitForSeconds(1f);
            yield return new WaitWhile(() => !_ddFsm.ActiveStateName.Contains("Tunneling"));
            _ddFsm.enabled = false;

            // Start Agony
            _healthPool = 180;
            yield return WaitToAttack();
            Coroutine c = StartCoroutine(LoopedAgony());
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            Rigidbody2D ogrimRB = dd.GetComponent<Rigidbody2D>();

            if(dd.transform.GetPositionX() < MIDDLE) ogrimRB.velocity = new Vector2(10f, 0f);
            else ogrimRB.velocity = new Vector2(-10f, 0f);

            IEnumerator TrackIsmaPos()
            {
                yield return new WaitUntil(() => FastApproximately(dd.transform.GetPositionX(), MIDDLE, 1f));
                ogrimRB.velocity = new Vector2(0f, 0f);
			}
            StartCoroutine(TrackIsmaPos());

            // Wait for death
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
            _deathEff.RecordJournalEntry();

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
            ogrimRB.velocity = new Vector2(5f, 0f);
            dd.transform.localScale = new Vector3(side * Mathf.Abs(scDD.x), scDD.y, scDD.z);
            _ddFsm.enabled = true;
            _ddFsm.SetState("Erupt Out First");
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
            yield return WaitToAttack();
            _hm.hp = 800;
            _hm.isDead = false;
            _healthPool = 300;
            Coroutine c = StartCoroutine(LoopedAgony());
            Log("Test");
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            while (_healthPool > 0)
            {
                _sr.enabled = true;
                _bc.enabled = true;
                yield return new WaitForEndOfFrame();
            }
            Log("Test1");
            //_sr.sortingOrder = 0;
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
            Log("Test2");
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
            yield return new WaitWhile(() => transform.position.y > GROUND_Y + 2.5f);
            transform.position = new Vector3(transform.position.x, GROUND_Y + 2.25f, transform.position.z);
            var sc = transform.localScale;
            transform.localScale = new Vector3(sc.x * -1f, sc.y, sc.z);
            _anim.enabled = true;
            _rb.gravityScale = 0f;
            _rb.velocity = new Vector2(0f,0f);
            yield return _anim.WaitToFrame(4);
            _anim.speed = 1f;
            _anim.PlayAt("IsmaTired", 0);
            yield return new WaitForSeconds(1f);
            transform.position = new Vector3(transform.position.x, GROUND_Y + 2.35f, transform.position.z);
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
            if (self.name.Contains("Isma"))
            {
                StartCoroutine(FlashWhite());
                Instantiate(_dnailEff, transform.position, Quaternion.identity); 
                _dnailReac.SetConvoTitle("ISMA_DREAM");
            }
            orig(self);
        }
        
        private void SpellFlukeOnDoDamage(On.SpellFluke.orig_DoDamage orig, SpellFluke self, GameObject tar, 
            int upwardrecursionamount, bool burst)
        {
            int damage = Mirror.GetField<SpellFluke, int>(self, "damage");
            DoTakeDamage(tar, damage, 0);
            orig(self, tar, upwardrecursionamount, burst);
        }

        private void HealthManager_TakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if(self.gameObject.name.Contains("Isma") && hitInstance.Source.name.Contains("Spike Ball"))
            {
                hitInstance.DamageDealt = GulkaSpitEnemyDamage;
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
            if(self.gameObject.name.Contains("White Defender")) _hmDD.hp = _healthPool;
            _hm.hp = _healthPool;
        }
        
        private void DoTakeDamage(GameObject tar, int damage, float dir)
        {
            if (tar.name.Contains("Isma"))
            {
                if (onlyIsma && waitForHitStart)
                {
                    StopCoroutine("Start2");
                    _attacking = true;
                    waitForHitStart = false;
                    StartCoroutine(BowWhipAttack());
                    StartCoroutine(SpawnWalls());
                    StartCoroutine(Agony());
                    StartCoroutine(AttackChoice());
                }
                if(!preventDamage) _healthPool -= damage;
                _hitEffects.RecieveHitEffect(dir);
                isIsmaHitLast = true;
            }
            else if (tar.name.Contains("White Defender"))
            {
                if(!preventDamage) _healthPool -= damage;
                isIsmaHitLast = false;
            }
        }

        IEnumerator FlashWhite()
        {
            _sr.material.SetFloat("_FlashAmount", 1f);
            yield return null;
            for (float i = 1f; i >= 0f; i -= 0.05f)
            {
                _sr.material.SetFloat("_FlashAmount", i);
                yield return null;
            }
            yield return null;
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

        IEnumerator IdleTimer(float time)
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
            gameObject.transform.position = new Vector3(xPos, GROUND_Y, 1f);
        }

        private void PlayDeathFor(GameObject go)
        {
            GameObject eff1 = Instantiate(_deathEff.uninfectedDeathPt);
            GameObject eff2 = Instantiate(_deathEff.whiteWave);
            eff1.SetActive(true);
            eff2.SetActive(true);
            eff1.transform.position = eff2.transform.position = go.transform.position;
            _deathEff.EmitSound();
            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
            if (go.name.Contains("Isma"))
            {
                _ap.Clip = FiveKnights.IsmaClips["IsmaAudDeath"];
                _ap.DoPlayRandomClip();
                //_aud.PlayOneShot(ArenaFinder.ismaAudioClips["IsmaAudDeath"]);
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
            _deathEff = _ddFsm.gameObject.GetComponent<EnemyDeathEffectsUninfected>();

            foreach (AudioClip i in FiveKnights.IsmaClips.Values.Where(x => x != null && !x.name.Contains("Death")))
            {
                _randAud.Add(i);
            }
        }

        private bool FastApproximately(float a, float b, float threshold)
        {
            return ((a - b) < 0 ? ((a - b) * -1) : (a - b)) <= threshold;
        }

        private IEnumerator WaitToAttack()
		{
            yield return new WaitWhile(() => _attacking);
            _attacking = true;
		}

        private void OnDestroy()
        {
            On.HealthManager.TakeDamage -= HealthManager_TakeDamage;
            On.EnemyDreamnailReaction.RecieveDreamImpact -= OnReceiveDreamImpact;
        }

        private void Log(object o)
        {
            if (!FiveKnights.isDebug) return;
            Modding.Logger.Log("[Isma] " + o);
        }
    }
}
