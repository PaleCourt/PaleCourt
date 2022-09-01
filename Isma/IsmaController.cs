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
        private readonly float LEFT_X = (OWArenaFinder.IsInOverWorld) ? 105f : 60.3f;
        private readonly float RIGHT_X = (OWArenaFinder.IsInOverWorld) ? 135f : 90.6f;
        private readonly float MIDDDLE = (OWArenaFinder.IsInOverWorld) ? 120 : 75f;
        // I did this dumb b/c this is actually going to loop 3 times
        private readonly int NUM_AGONY_LOOPS = 1;
        private readonly float GROUND_Y = 5.9f;
        
        private const int MAX_HP = 1500;
        private const int WALL_HP = 1000;
        private const int SPIKE_HP = 600;
        
        private const float IDLE_TIME = 0f; //0.1f;
        private const int GulkaSpitEnemyDamage = 20;
        public static float offsetTime;
        public static bool killAllMinions;
        private bool isDead;
        public static bool eliminateMinions;
        private bool isIsmaHitLast;
        public bool introDone;
        private const int MaxDreamAmount = 3;

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
            _healthPool = MAX_HP;
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

            // Load/create missing objects and assets for Godhome arena
            if(!OWArenaFinder.IsInOverWorld)
            {
                // Acid spit
                #region
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
                FiveKnights.IsmaClips["AcidSpitSnd"] = clip;
                FiveKnights.preloadedGO["AcidSpit"] = acidOrig;
                FiveKnights.preloadedGO["AcidSpitPlayer"] = actorOrig;
                #endregion

                // Load thorn attack sound effects
                AssetBundle snd = ABManager.AssetBundles[ABManager.Bundle.Sound];
                FiveKnights.Clips["IsmaAudAgonyShoot"] = snd.LoadAsset<AudioClip>("IsmaAudAgonyShoot");
                FiveKnights.Clips["IsmaAudAgonyIntro"] = snd.LoadAsset<AudioClip>("IsmaAudAgonyIntro");

				// Seed columns
				#region
				GameObject sc = new GameObject();
                sc.name = "SeedCols";

                GameObject sf = new GameObject();
                sf.name = "SeedFloor";
                sf.transform.position = new Vector3(70.8f, 5.1f, 0f);
                BoxCollider2D sfcol = sf.AddComponent<BoxCollider2D>();
                sfcol.offset = new Vector2(3f, 0f);
                sfcol.size = new Vector2(19f, 1f);
                sf.transform.parent = sc.transform;

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
				#endregion
			}

			foreach(Transform sidecols in GameObject.Find("SeedCols").transform)
            {
                sidecols.gameObject.AddComponent<EnemyPlantSpawn>();
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
            _hm.hp = _hmDD.hp = MAX_HP + 200;
            gameObject.layer = 11;
            _target = HeroController.instance.gameObject;
            if (!onlyIsma) PositionIsma();
            else transform.position = new Vector3(LEFT_X + (RIGHT_X-LEFT_X)/1.5f, GROUND_Y, 1f);
            FaceHero();
            _bc.enabled = false;

            _anim.Play("Apear"); // Yes I know it's "appear," I don't feel like changing the assetbundle buddo
            gameObject.transform.position = new Vector2(gameObject.transform.GetPositionX(), GROUND_Y + 8f);
            _rb.velocity = new Vector2(0f, -40f);
            yield return new WaitWhile(() => gameObject.transform.GetPositionY() > GROUND_Y);
            GameCameras.instance.cameraShakeFSM.SendEvent("SmallShake");
            _rb.velocity = new Vector2(0f, 0f);
            gameObject.transform.position = new Vector2(gameObject.transform.GetPositionX(), GROUND_Y);
            yield return new WaitWhile(() => _anim.IsPlaying());

            //Log(HeroController.instance.GetComponent<tk2dSpriteAnimator>().CurrentClip.name);
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
            _bc.enabled = true;
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
            PlantPillar();
            StartCoroutine(SpawnWalls());
            if(onlyIsma)
            {
                StartCoroutine(Agony());
                StartCoroutine(AttackChoice());
            }
			else
			{
                StartCoroutine(AttackChoiceDuo());
            }
        }
        
        private void Update()
        {
            if (_healthPool <= 0 && !isDead)
            {
                Log("Victory");
                isDead = true;
                _healthPool = 100;
                if(onlyIsma) StartCoroutine(IsmaLoneDeath());
                else if(isIsmaHitLast) StartCoroutine(IsmaDeath());
                else StartCoroutine(OgrimDeath());
            }
        }

        private Action _prev;

        private IEnumerator AttackChoice()
        {
            int lastA = 3;
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
                        (EnemyPlantSpawn.MaxFool + EnemyPlantSpawn.MaxTurret)) * 100;
                }
                bool throwBomb = _rand.Next(100) < plantPercent;
                int r = onlyIsma ? UnityEngine.Random.Range(0, 10) : UnityEngine.Random.Range(0, 7);
                if (EnemyPlantSpawn.FoolCount == 0 && EnemyPlantSpawn.TurretCount == 0 
                                                   && lastA != 2
                                                   && UnityEngine.Random.Range(0, 2) == 0)
                {
                    lastA = 2;
                    _prev = Bomb;
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
                    _prev = Bomb;
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

        private IEnumerator AttackChoiceDuo()
		{
            EnemyPlantSpawn.isPhase2 = true;
            _ddFsm.FsmVariables.FindFsmInt("Damage").Value = 1;
            dd.GetComponent<DamageHero>().damageDealt = 1;

            // Limit to 1 throw
            _ddFsm.ChangeFsmTransition("Throw 2", "FINISHED", "After Throw?");

            // Add short delay after dung toss
            _ddFsm.GetAction<SendEventByName>("After Throw?", 0).delay = 0.3f;
            _ddFsm.InsertAction("After Throw?", _ddFsm.GetAction<Tk2dPlayAnimation>("Idle", 0), 0);

            // Increase delay after ground slam
            _ddFsm.GetAction<Wait>("G Slam Recover", 0).time = 1.2f;

            StartCoroutine(SmashBall());

            // Vine Whip - Burrowing
            _ddFsm.InsertMethod("Timer", () =>
            {
                IEnumerator WaitForWhip()
                {
                    yield return new WaitWhile(() => _attacking);
                    _attacking = true;
                    yield return new WaitForSeconds(0.15f);
                    WhipAttack();
                    _attacking = false;
                }
                StartCoroutine(WaitForWhip());
            }, 0);
            // Bomb - Bouncing
            _ddFsm.InsertMethod("RJ Launch", () =>
            {
                IEnumerator WaitForBomb()
                {
                    yield return new WaitWhile(() => _attacking);
                    _attacking = true;
                    yield return new WaitForSeconds(0.1f);
                    Bomb();
                    _attacking = false;
                }
                StartCoroutine(WaitForBomb());
            }, 0);

            yield return new WaitWhile(() => _healthPool > WALL_HP);

            // Acid/Air Fist - Spike slam
            _ddFsm.InsertMethod("G Slam", () =>
            {
                IEnumerator WaitForAirFist()
                {
                    yield return new WaitWhile(() => _attacking);
                    _attacking = true;
                    AirFist();
                    _attacking = false;
                }
                StartCoroutine(WaitForAirFist());
            }, 0);

            yield return new WaitWhile(() => _healthPool > SPIKE_HP);

            // Agony - Spike wave
            _ddFsm.InsertMethod("Under", () =>
            {
                IEnumerator WaitForAgony()
                {
                    yield return new WaitForSeconds(0.5f);
                    yield return new WaitWhile(() => _attacking);
                    _attacking = true;
                    yield return Agony();
                    _attacking = false;
                }
                StartCoroutine(WaitForAgony());
            }, 0);
        }

        private bool _wallActive;

        private IEnumerator SpawnWalls()
        {
            yield return new WaitWhile(() => _healthPool > WALL_HP);
            EnemyPlantSpawn.isPhase2 = true;
            killAllMinions = true;
            yield return new WaitForSeconds(0.1f);
            killAllMinions = false;
            _wallActive = true;
            if(onlyIsma)
            {
                EnemyPlantSpawn spawner = GameObject.Find("SeedFloor").GetComponent<EnemyPlantSpawn>();
                spawner.Phase2Spawn();
            }
            else
            {
                yield return new WaitWhile(() => _ddFsm.ActiveStateName != "Idle");
                dd.LocateMyFSM("Constrain X").FsmVariables.FindFsmFloat("Edge L").Value = 66.5f;
                dd.LocateMyFSM("Constrain X").FsmVariables.FindFsmFloat("Edge R").Value = 86f;
                _ddFsm.FsmVariables.FindFsmFloat("Dolphin Max X").Value = 86f;
                _ddFsm.FsmVariables.FindFsmFloat("Dolphin Min X").Value = 66.5f;
                _ddFsm.FsmVariables.FindFsmFloat("Max X").Value = 85f;
                _ddFsm.FsmVariables.FindFsmFloat("Min X").Value = 67f;
                _ddFsm.SetState("Timer");
            }

            GameObject wallR = Instantiate(FiveKnights.preloadedGO["Wall"]);
            wallR.transform.localScale = new Vector3(wallR.transform.localScale.x * -1f, wallR.transform.localScale.y, wallR.transform.localScale.z);
            GameObject wallL = Instantiate(FiveKnights.preloadedGO["Wall"]);
            GameObject frontWR = wallR.transform.Find("FrontW").gameObject;
            GameObject frontWL = wallL.transform.Find("FrontW").gameObject;
            frontWR.layer = 8;
            frontWL.layer = 8;
            wallR.transform.position = new Vector2(RIGHT_X - 1.5f, GROUND_Y);
            wallL.transform.position = new Vector2(LEFT_X + 3f, GROUND_Y);
            Animator anim = frontWR.GetComponent<Animator>();
            yield return new WaitWhile(() => anim.GetCurrentFrame() < 3);
            wallR.transform.Find("Petal").gameObject.SetActive(true);
            wallL.transform.Find("Petal").gameObject.SetActive(true);
            Vector2 hPos = _target.transform.position;
            if (hPos.x > RIGHT_X - 4.6f) _target.transform.position = new Vector2(RIGHT_X - 4.6f,hPos.y);
            else if (hPos.x < LEFT_X + 7.3f) _target.transform.position = new Vector2(LEFT_X + 7.3f,hPos.y);
            
            yield return new WaitWhile(() => _healthPool > SPIKE_HP);

            foreach (GameObject wall in new[] {wallR, wallL})
            {
                GameObject spike = wall.transform.Find("Spike").gameObject;
                GameObject spikeFront = spike.transform.Find("Front").gameObject;
                spikeFront.layer = 17;
                spikeFront.AddComponent<DamageHero>().damageDealt = 1;

                var newEff = spikeFront.AddComponent<TinkEffect>();
                var oldEff = FiveKnights.preloadedGO["TinkEff"].GetComponent<TinkEffect>();
                foreach (FieldInfo fi in typeof(TinkEffect).GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    fi.SetValue(newEff, fi.GetValue(oldEff));
                }

                spike.SetActive(true);
            }
            
            eliminateMinions = false;
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
            }
            
        }

        private void Bomb()
        {
            float dir = 0f;
            IEnumerator BombThrow()
            {
                float heroX = _target.transform.GetPositionX();
                float ismaX = heroX - MIDDDLE > 0f ? LEFT_X + 8f : RIGHT_X - 8f;
                if (_wallActive) ismaX = heroX - MIDDDLE > 0f ? LEFT_X + 11f : RIGHT_X - 11f;
                transform.position = new Vector2(ismaX, GROUND_Y);
                dir = FaceHero();
                _rb.velocity = new Vector2(-20f * dir, 0f);
                ToggleIsma(true);
                _anim.Play("ThrowBomb");
                _ap.Clip = _randAud[_rand.Next(0, _randAud.Count)];
                _ap.DoPlayRandomClip();
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
                //seed.transform.localScale *= 1.75f;
                seed.transform.localScale *= 1.15f;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
                bomb.SetActive(true);
                rb.gravityScale = 1.3f;
                StartCoroutine(AnimEnder());
                rb.velocity = new Vector2(dir * 30f, 40f);
                CollisionCheck cc = bomb.AddComponent<CollisionCheck>();
                rb.angularVelocity = dir * 900f;
                float xLim = MIDDDLE;
                yield return new WaitWhile(() => !cc.Hit && !FastApproximately(bomb.transform.GetPositionX(), xLim, 0.6f));
                anim.Play("Bomb");
                yield return new WaitWhile(() => anim.GetCurrentFrame() < 1);
                rb.angularVelocity = 0f;
                rb.gravityScale = 0f;
                rb.velocity = Vector2.zero;
                yield return new WaitWhile(() => anim.GetCurrentFrame() <= 2); //whip and boxcollider seed
                int incr = _wallActive ? 50 : 40;
                for (int i = 0; i <= 360; i += incr)
                {
                    GameObject localSeed = Instantiate(seed);
                    float rot = i + UnityEngine.Random.Range(0, 10) * (incr/10);
                    localSeed.SetActive(true);
                    localSeed.transform.rotation = Quaternion.Euler(0f, 0f, rot);
                    localSeed.transform.position = bomb.transform.position;
                    Vector3 scale = localSeed.transform.localScale * 1.7f;
                    localSeed.transform.localScale = new Vector3(scale.x * -1f, scale.y, scale.z);
                    float spd = 30;
                    
                    localSeed.layer = 11;
                    localSeed.name = "SeedEnemy";
                    
                    localSeed.GetComponent<Rigidbody2D>().velocity = new Vector2(spd * Mathf.Cos(rot * Mathf.Deg2Rad), spd * Mathf.Sin(rot * Mathf.Deg2Rad));
                    //EnemyPlantSpawn eps = localSeed.AddComponent<EnemyPlantSpawn>();
                    Destroy(localSeed.GetComponent<DamageHero>());
                    //eps.PlantG = PlantG;
                    //eps.PlantF = PlantF;
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

        private void PlantPillar()
        {
            List<float> PlantX = new List<float>();
            IEnumerator PlantChecker()
            {
                tk2dSpriteAnimator tk = dd.GetComponent<tk2dSpriteAnimator>();
                while (!_wallActive)
                {
                    yield return new WaitWhile(() => !tk.IsPlaying("Roll"));
                    Coroutine c = StartCoroutine(PlantPillar());
                    float time = 0f;
                    yield return new WaitWhile(() => tk.IsPlaying("Roll") && (time += Time.deltaTime) < 3.5f);
                    if (c != null) StopCoroutine(c);
                    yield return new WaitForEndOfFrame();
                }
            }

            IEnumerator PlantPillar()
            {
                while (!_wallActive)
                {
                    float posX = _target.transform.GetPositionX();
                    bool skip = false;
                    foreach (float i in PlantX.Where(x => FastApproximately(x, posX, 3f))) skip = true;
                    if (skip)
                    {
                        yield return new WaitForEndOfFrame();
                        continue;
                    }
                    PlantX.Add(posX);
                    GameObject plant = Instantiate(FiveKnights.preloadedGO["Plant"]);
                    plant.transform.position = new Vector2(posX, GROUND_Y);
                    plant.AddComponent<PlantCtrl>().PlantX = PlantX;
                    yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 2f));
                }
            }
            StartCoroutine(PlantChecker());
        }

        private void AirFist()
        {
            IEnumerator AirFist()
            {
                float heroX = _target.transform.GetPositionX();
                float distance = UnityEngine.Random.Range(10, 12);
                float ismaX = heroX - MIDDDLE > 0f ? heroX - distance : heroX + distance;

                if (_wallActive)
                {
                    float rightMost = RIGHT_X - 8;
                    float leftMost = LEFT_X + 10;
                    ismaX = heroX - MIDDDLE > 0f 
                        ? UnityEngine.Random.Range((int)leftMost, (int)heroX - 6) 
                        : UnityEngine.Random.Range((int)heroX + 6, (int)rightMost);
                }
                
                transform.position = new Vector2(ismaX, UnityEngine.Random.Range(13, 16));
                ToggleIsma(true);
                float dir = FaceHero();
                
                GameObject arm = transform.Find("Arm2").gameObject;
                _anim.Play("AFistAntic");
                _ap.Clip = _randAud[_rand.Next(0, _randAud.Count)];
                _ap.DoPlayRandomClip();
                _rb.velocity = new Vector2(dir * -20f, 0f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                _rb.velocity = new Vector2(0f, 0f);
                GameObject spike = transform.Find("SpikeArm").gameObject;
                yield return new WaitWhile(() => _anim.IsPlaying());

                _anim.Play("AFist");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 0);
                spike.SetActive(true);
                _anim.enabled = false;
                yield return new WaitForSeconds(0.3f);
                
                Vector2 heroVel = _target.GetComponent<Rigidbody2D>().velocity;
                float predTime = 0.4f;
                float yOff = 0.5f;
                float xOff = 0.8f;
                Vector3 predPos = _target.transform.position + new Vector3(heroVel.x * xOff, heroVel.y * yOff) * predTime;
                float rot = GetRot(arm, predPos, dir) is < -60f or > 50f
                    ? GetRot(arm, _target.transform.position, dir)
                    : GetRot(arm, predPos, dir);
                float rotD = rot * Mathf.Rad2Deg;

                if(rotD is < -60f or > 50f)
                {
                    int rnd = UnityEngine.Random.Range(0, 1);
                    if (rnd == 0)
                    {
                        _anim.enabled = true;
                        spike.SetActive(false);
                        yield return AcidThrow();
                    }
                    else
                    {
                        yield return EndAirFist(spike, new GameObject(), dir, 0.1f);
                        if (rnd == 1) this.AirFist();
                        else StartCoroutine(IdleTimer(IDLE_TIME));
                    }
                    yield break;
                }
                
                arm.transform.SetRotation2D(rot * Mathf.Rad2Deg);
                GameObject parArm = arm.transform.Find("TentArm").gameObject;
                parArm.SetActive(false);
                GameObject tentArm = Instantiate(parArm, parArm.transform.position, parArm.transform.rotation);
                tentArm.SetActive(false);
                Vector3 tentArmScale = tentArm.transform.localScale;
                tentArm.transform.localScale = new Vector3(dir * tentArmScale.x, tentArmScale.y, tentArmScale.z) * 1.35f;
                
                yield return new WaitForSeconds(0.1f);
                spike.SetActive(false);
                _anim.enabled = true;
                arm.SetActive(true);
                tentArm.SetActive(true);

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
                aud.PlayOneShot(FiveKnights.IsmaClips["AcidSpitSnd"], 1f);
                Destroy(this);
            }
        }

        private const float WhipYOffset = 2.21f;
        private const float WhipXOffset = 1.13f;
        
        private void WhipAttack()
        {
            IEnumerator WhipAttack()
            {
                float heroX = _target.transform.GetPositionX();
                float ismaX = heroX - MIDDDLE > 0f ? LEFT_X + 8f : RIGHT_X - 8f;
                //if (_wallActive) ismaX = heroX - MIDDDLE > 0f ? LEFT_X + 11f : RIGHT_X - 11f;
                if (_wallActive) ismaX = heroX - MIDDDLE > 0f ? LEFT_X + 11f : RIGHT_X - 9f;
                transform.position = new Vector2(ismaX, GROUND_Y);
                float dir = FaceHero();
                ToggleIsma(true);
                _rb.velocity = new Vector2(dir * -20f, 0f);
                _anim.Play("GFistAntic");
                _ap.Clip = _randAud[_rand.Next(0, _randAud.Count)];
                _ap.DoPlayRandomClip();
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                _rb.velocity = Vector2.zero;
                _anim.enabled = false;
                yield return new WaitForSeconds(0.7f);
                _anim.enabled = true;
                transform.position += new Vector3(WhipXOffset * Math.Sign(transform.localScale.x), WhipYOffset, 0f);
                var oldWhip = transform.Find("Whip").gameObject;
                var whip = Instantiate(oldWhip);
                whip.transform.position = oldWhip.transform.position;
                whip.transform.localScale = oldWhip.transform.lossyScale;
                whip.SetActive(true);
                
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
                //_anim.Play("GFistEnd");
                //yield return null;
                //yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                //_anim.enabled = false;
                //yield return new WaitForSeconds(0.5f);
                //_anim.enabled = true;
                //transform.position += new Vector3(1.21f * Math.Sign(transform.localScale.x), 0.4f);
                
                
                //yield return null;
                //yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                //_rb.velocity = new Vector2(dir * -20f, 0f);
                //yield return new WaitWhile(() => _anim.IsPlaying());
                //_rb.velocity = Vector2.zero;
                ToggleIsma(false);
                StartCoroutine(IdleTimer(IDLE_TIME));
            }

            StartCoroutine(WhipAttack());
        }

        private IEnumerator BowWhipAttack()
        {
            float dir = -1f * FaceHero(true);
            _ap.Clip = _randAud[_rand.Next(0, _randAud.Count)];
            _ap.DoPlayRandomClip();
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

        private IEnumerator SmashBall()
        {
            tk2dSpriteAnimator tk = dd.GetComponent<tk2dSpriteAnimator>();
            while (true)
            {
                yield return new WaitWhile(() => !tk.CurrentClip.name.Contains("Throw"));
                yield return new WaitWhile(() => _attacking);
                _attacking = true;
                while (tk.CurrentClip.name.Contains("Throw") || tk.CurrentClip.name.Contains("Erupt"))
                {
                    foreach (GameObject go in FindObjectsOfType<GameObject>().Where(x => x.name.Contains("Dung Ball") && x.activeSelf && x.transform.GetPositionY() > 15f
                            && x.transform.GetPositionY() < 16.5f && x.GetComponent<Rigidbody2D>().velocity.y > 0f))
                    {
                        Vector2 pos = go.transform.position;
                        ToggleIsma(true);
                        _ap.Clip = _randAud[_rand.Next(0, _randAud.Count)];
                        _ap.DoPlayRandomClip();
                        _attacking = true;
                        float side = go.GetComponent<Rigidbody2D>().velocity.x > 0f ? 1f : -1f;
                        gameObject.transform.position = new Vector2(pos.x + side * 1.77f, pos.y + 0.38f);
                        float dir = FaceHero();
                        GameObject squish = gameObject.transform.Find("Squish").gameObject;
                        GameObject ball = Instantiate(gameObject.transform.Find("Ball").gameObject);
                        ball.transform.localScale *= 1.4f;
                        ball.layer = 11;
                        ball.AddComponent<DamageHero>().damageDealt = 1;
                        ball.AddComponent<DungBall>();
                        _anim.Play("BallStrike");
                        yield return new WaitForSeconds(0.05f);
                        yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
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
                        if (diff.x > 0)
                        {
                            offset2 += 180f;
                        }
                        float rot = Mathf.Atan(diff.y / diff.x) + offset2 * Mathf.Deg2Rad;
                        ball.transform.SetRotation2D(rot * Mathf.Rad2Deg + 90f);
                        Vector2 vel = new Vector2(30f * Mathf.Cos(rot), 30f * Mathf.Sin(rot));
                        ball.GetComponent<Rigidbody2D>().velocity = vel;
                        yield return new WaitForSeconds(0.1f);
                        ballFx.GetComponent<Animator>().Play("FxEnd");
                        yield return new WaitForSeconds(0.1f);
                        Destroy(ballFx);
                        yield return new WaitWhile(() => _anim.GetCurrentFrame() < 6);
                        _rb.velocity = new Vector2(dir * 20f, 0f);
                        yield return new WaitWhile(() => _anim.IsPlaying());
                        _rb.velocity = new Vector2(0f, 0f);
                        ToggleIsma(false);
                        break;
                    }
                    yield return new WaitForEndOfFrame();
                }
                StartCoroutine(IdleTimer(IDLE_TIME));
            }
        }

        private IEnumerator Agony()
        {
            if(onlyIsma)
			{
                yield return new WaitWhile(() => _hm.hp > WALL_HP);
                yield return new WaitWhile(() => _attacking);
                _attacking = true;
            }

            ToggleIsma(true);
            Vector3 scIs = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector3(Mathf.Abs(scIs.x), scIs.y, scIs.z);
            gameObject.transform.SetPosition2D(MIDDDLE, GROUND_Y + 11.6f);

            GameObject fakeIsma = new GameObject();
            fakeIsma.transform.position = gameObject.transform.position;
            fakeIsma.transform.localScale = gameObject.transform.localScale;
            GameObject thornorig = transform.Find("Thorn").gameObject;
            GameObject thorn = Instantiate(thornorig);
            Vector3 orig = thornorig.transform.position;
            thorn.transform.position = new Vector3(orig.x - 1f, orig.y - 4f, orig.z);
            thorn.transform.parent = fakeIsma.transform;

            Animator tAnim = thorn.transform.Find("T1").gameObject.GetComponent<Animator>();

            _anim.Play("AgonyLoopIntro");
            yield return null;
            yield return new WaitWhile(() => _anim.IsPlaying());
            _ap.Clip = _randAud[_rand.Next(0, _randAud.Count)];
            _ap.DoPlayRandomClip();
            _anim.speed = 1.7f;

            yield return PerformAgony(thorn, tAnim, onlyIsma ? 3 : 1);

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
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 11);
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
            gameObject.transform.SetPosition2D(MIDDDLE, GROUND_Y + 11.1f);
            
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

        private float CalculateTrajector(Vector2 vel0, float h, float g)
        {
            float accel = g * Physics2D.gravity.y;
            float disc = vel0.y * vel0.y - 2 * accel * h;
            float time = (-vel0.y + Mathf.Sqrt(disc)) / accel;
            float time2 = (-vel0.y - Mathf.Sqrt(disc)) / accel;
            if (time < 0) time = time2;
            return time * vel0.x;
        }

        private IEnumerator OgrimDeath()
        {
            Log("Started Ogrim Death");
            yield return new WaitWhile(() => _attacking);
            ToggleIsma(false);
            _attacking = true;
            _hm.hp = _hmDD.hp = 200;
            _healthPool = 40;
            yield return new WaitWhile(() => _healthPool > 0);
            float xSpd = _target.transform.GetPositionX() > dd.transform.GetPositionX() ? -10f : 10f;
            GGBossManager.Instance.PlayMusic(null, 1f);
            Destroy(_ddFsm.GetAction<FadeAudio>("Stun Recover", 2).gameObject.GameObject.Value);
            //GameManager.instance.gameObject.GetComponent<GGBossManager>()._ap.StopMusic();
            //GameManager.instance.gameObject.GetComponent<GGBossManager>()._ap2.StopMusic();
            PlayDeathFor(dd);
            eliminateMinions = true;
            killAllMinions = true;
            _ddFsm.GetAction<SetVelocity2d>("Stun Launch", 0).y.Value = 45f;
            _ddFsm.SetState("Stun Set");
            yield return null;
            yield return new WaitWhile(() => _ddFsm.ActiveStateName == "Stun Set");
            PlayerData.instance.isInvincible = true;
            float x = CalculateTrajector(new Vector2(xSpd, 45f), 7.95f, dd.GetComponent<Rigidbody2D>().gravityScale) + dd.transform.GetPositionX();
            if (x < 68f) x = 68f;
            else if (x > 85f) x = 85f;
            yield return new WaitWhile(() => _ddFsm.ActiveStateName != "Stun In Air");
            yield return null;
            _ddFsm.enabled = false;
            Rigidbody2D ogrimRb = dd.GetComponent<Rigidbody2D>();
            yield return new WaitWhile(() => ogrimRb.velocity.y > 0f);
            ToggleIsma(true);
            _anim.Play("OgrimCatchIntro");
            float sign = Mathf.Sign(dd.transform.localScale.x);
            Vector3 sc = gameObject.transform.localScale;
            transform.localScale = new Vector3(sign * Mathf.Abs(sc.x), sc.y, sc.z);
            transform.localScale *= 1.2f;
            transform.position = new Vector2(x + sign * 2f, GROUND_Y + 2.05f);
            _rb.velocity = new Vector2(sign * -20f,0f);
            yield return null;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
            _rb.velocity = new Vector2(0f, 0f);
            _deathEff.RecordJournalEntry();
            yield return new WaitWhile(() => !FastApproximately(transform.GetPositionY(), dd.transform.GetPositionY(), 1.6f) && dd.transform.GetPositionY() > 4f);
            if (dd.transform.GetPositionY() < 4f) //In case we don't catch ogrim
            {
                _anim.PlayAt("OgrimCatch", 2);
                transform.position = new Vector3(75f, GROUND_Y - 3.5f);
                dd.SetActive(false);
            }
            else
            {
                _anim.Play("OgrimCatch");
                dd.SetActive(false);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                _anim.enabled = false;
                yield return new WaitForSeconds(0.65f);
                _anim.enabled = true;
            }
            yield return null;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
            _rb.velocity = new Vector2(0f, 35f);
            GameCameras.instance.cameraShakeFSM.SendEvent("BigShake");
            HeroController.instance.StartAnimationControl();
            yield return new WaitWhile(() => transform.position.y < 19f);
            PlayerData.instance.isInvincible = false;
            if (CustomWP.boss != CustomWP.Boss.All) yield return new WaitForSeconds(1f);
            CustomWP.wonLastFight = true;
            _ddFsm.enabled = false;
            Destroy(this);
        }

        private IEnumerator IsmaDeath()
        {
            Log("Started Isma Death");
            yield return new WaitWhile(() => _attacking);
            _attacking = true;

            foreach (FsmTransition i in _ddFsm.GetState("Idle").Transitions)
            {
                SFCore.Utils.FsmUtil.ChangeTransition(_ddFsm, "Idle", i.EventName, "Timer");
            }
            _ddFsm.SetState("Idle");
            yield return new WaitWhile(() => _ddFsm.ActiveStateName != "Idle");
            yield return new WaitWhile(() => !_ddFsm.ActiveStateName.Contains("Tunneling"));
            tk2dSpriteAnimator tk = dd.GetComponent<tk2dSpriteAnimator>();
            _ddFsm.enabled = false;

            //_sr.sortingOrder = 0;
            _hm.hp = _hmDD.hp = 500;
            _healthPool = 250;
            Coroutine c = StartCoroutine(LoopedAgony());
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            Rigidbody2D ogrimRB = dd.GetComponent<Rigidbody2D>();
            while (_healthPool > 0)
            {
                float ismaX = gameObject.transform.GetPositionX();
                float plX = _target.transform.GetPositionX();
                float ddX = dd.transform.GetPositionX();
                if (FastApproximately(plX, ddX, 0.35f))
                {
                    ogrimRB.velocity = Vector2.zero;
                    GameObject plant = Instantiate(FiveKnights.preloadedGO["Plant"]);
                    plant.transform.position = new Vector2(plX, GROUND_Y);
                    plant.AddComponent<PlantCtrl>().PlantX = new List<float>();
                    float t = 0f;
                    while (t < 5f)
                    {
                        if (_healthPool <= 0) break;
                        t += Time.fixedDeltaTime;
                        yield return new WaitForEndOfFrame();
                    }
                }
                else if (plX > ddX)
                {
                    ogrimRB.velocity = new Vector2(15f, 0f);
                }
                else if (plX < ddX)
                {
                    ogrimRB.velocity = new Vector2(-15f, 0f);
                }

                yield return new WaitForEndOfFrame();
            }
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
            Destroy(_ddFsm.GetAction<FadeAudio>("Stun Recover", 2).gameObject.GameObject.Value);
            Log("1 dada ");
            //GameManager.instance.gameObject.GetComponent<GGBossManager>()._ap.StopMusic();
            //GameManager.instance.gameObject.GetComponent<GGBossManager>()._ap2.StopMusic();
            Log("2 dada");
            GGBossManager.Instance.PlayMusic(null, 1f);
            PlayDeathFor(gameObject);
            _anim.Play("Falling");
            PlayerData.instance.isInvincible = true; ;

            _rb.gravityScale = 1.5f;
            float ismaXSpd = dir * 10f;
            _rb.velocity = new Vector2(ismaXSpd, 28f);
            bool once = false;

            //Time.timeScale = 0.5f;
            Vector3 scDD2 = dd.transform.localScale;
            float side2 = Mathf.Sign(gameObject.transform.localScale.x);
            _deathEff.RecordJournalEntry();

            dd.transform.localScale = new Vector3(side2 * Mathf.Abs(scDD2.x), scDD2.y, scDD2.z);
            
            while (_rb.velocity.y > -15f)
            {
                float ogrimSpd = transform.GetPositionX() > dd.transform.GetPositionX() ? 15f : -15f;
                if (!once && gameObject.transform.GetPositionX() > RIGHT_X || 
                    gameObject.transform.GetPositionX() < LEFT_X)
                {
                    _rb.velocity = new Vector2(0f, _rb.velocity.y);
                    once = true;
                }
                if (FastApproximately(transform.GetPositionX(), dd.transform.GetPositionX(), 0.2f))
                {
                    ogrimRB.velocity = new Vector2(0f, 0f);
                }
                else
                {
                    ogrimRB.velocity = new Vector2(ogrimSpd, 0f);
                }
                yield return new WaitForEndOfFrame();
            }
            _rb.velocity = new Vector2(_rb.velocity.x - dir * 5f, _rb.velocity.y);

            Vector3 scDD = dd.transform.localScale;
            float side = Mathf.Sign(gameObject.transform.localScale.x);

            ogrimRB.velocity = new Vector2(5f, 0f);
            dd.transform.localScale = new Vector3(side * Mathf.Abs(scDD.x), scDD.y, scDD.z);
            _ddFsm.enabled = true;
            _ddFsm.SetState("Erupt Out");
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
            //Time.timeScale = 1f;
            Log("isma dead end 1");
            if (CustomWP.boss != CustomWP.Boss.All) yield return new WaitForSeconds(1f);
            CustomWP.wonLastFight = true;
            Log("isma dead end 2");
            _ddFsm.enabled = false;
            Destroy(this);
        }

        private IEnumerator IsmaLoneDeath()
        {
            Log("Started Isma Lone Death");
            yield return new WaitWhile(() => _attacking);
            _attacking = true;
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
            if (self.gameObject.name.Contains("Isma") && hitInstance.Source.name.Contains("Spike Ball"))
            {
                hitInstance.DamageDealt = GulkaSpitEnemyDamage;
            }
            DoTakeDamage(self.gameObject, hitInstance.DamageDealt, hitInstance.Direction);
            orig(self, hitInstance);
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
                    PlantPillar();
                    if (!onlyIsma) StartCoroutine(SmashBall());
                    StartCoroutine(Agony());
                    StartCoroutine(AttackChoice());
                    StartCoroutine(SpawnWalls());
                }
                _healthPool -= damage;
                _hitEffects.RecieveHitEffect(dir);
                isIsmaHitLast = true;
            }
            else if (tar.name.Contains("White Defender"))
            {
                _healthPool -= damage;
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
            _anim.PlayAt("Idle", 0);
            _sr.enabled = visible;
            _bc.enabled = visible;
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
