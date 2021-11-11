using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FiveKnights.BossManagement;
using FiveKnights.Ogrim;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using SFCore.Utils;
using TMPro;
using UnityEngine;
using Vasi;

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
        private readonly int NUM_AGONY_LOOPS = 4;
        private readonly float GROUND_Y = 5.9f;
        private const int MAX_HP = 1700;
        private const int WALL_HP = 1200;
        private const int SPIKE_HP = 800;
        private const float IDLE_TIME = 0.1f;
        public static float offsetTime;
        public static bool killAllMinions;
        private bool isDead;
        public static bool eliminateMinions;
        private bool isIsmaHitLast;
        public bool introDone;
        
        private readonly string[] _dnailDial =
        {
            "ISMA_DREAM_1",
            "ISMA_DREAM_2",
            "ISMA_DREAM_3"
        };

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
            _rand = new System.Random();
            _randAud = new List<AudioClip>();
            _dnailReac.SetConvoTitle(_dnailDial[_rand.Next(_dnailDial.Length)]);
            _healthPool = MAX_HP;
            _hitEffects = gameObject.AddComponent<EnemyHitEffectsUninfected>();
            _hitEffects.enabled = true;
            _deathEff = gameObject.AddComponent<EnemyDeathEffectsUninfected>();
            EnemyPlantSpawn.isPhase2 = false;

            GameObject seedFloor = new GameObject("SeedFloor");
            seedFloor.SetActive(true);
            seedFloor.transform.position = new Vector3(116.1f, 5f, 0f);
            var bc = seedFloor.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.offset = new Vector2(3.949066f, 0f);
            bc.size = new Vector2(24.56163f, 1f);
            seedFloor.AddComponent<EnemyPlantSpawn>();
            seedFloor.layer = 8;
            
            GameObject seedSideL = new GameObject("SeedSideL");
            seedSideL.SetActive(true);
            seedSideL.transform.position = new Vector3(104.7f, 12.6f, 0f);
            var bcL = seedSideL.AddComponent<BoxCollider2D>();
            bcL.isTrigger = true;
            bcL.offset = new Vector2(0, 0.4677944f);
            bcL.size = new Vector2(1, 7.96706f);
            //seedSideL.AddComponent<DebugColliders>();
            seedSideL.AddComponent<EnemyPlantSpawn>();
            seedSideL.layer = 8;
            
            GameObject seedSideR = new GameObject("SeedSideR");
            seedSideR.SetActive(true);
            seedSideR.transform.position = new Vector3(137.7f, 12.6f, 0f); //5.4
            var bcR = seedSideR.AddComponent<BoxCollider2D>();
            bcR.isTrigger = true;
            bcR.offset = new Vector2(0, 0f);
            bcR.size = new Vector2(1, 7.031467f);
            //seedSideR.AddComponent<DebugColliders>();
            seedSideR.AddComponent<EnemyPlantSpawn>();
            seedSideR.layer = 8;
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

            On.HealthManager.TakeDamage += HealthManager_TakeDamage;
            On.EnemyDreamnailReaction.RecieveDreamImpact += OnReceiveDreamImpact;
            AssignFields(gameObject);
            _ddFsm.FsmVariables.FindFsmInt("Rage HP").Value = 801;
            _hm.hp = _hmDD.hp = MAX_HP + 200;
            gameObject.layer = 11;
            _target = HeroController.instance.gameObject;
            if (!onlyIsma) PositionIsma();
            else transform.position = new Vector3(LEFT_X + (RIGHT_X-LEFT_X)/1.5f, GROUND_Y, 1f);
            float dir = FaceHero();
            _bc.enabled = false;
            _anim.Play("Apear"); // Yes I know it's "appear," I don't feel like changing the assetbundle buddo
            gameObject.transform.position = new Vector2(gameObject.transform.GetPositionX(), GROUND_Y + 8f);
            _rb.velocity = new Vector2(0f, -40f);
            yield return new WaitWhile(() => gameObject.transform.GetPositionY() > GROUND_Y);
            GameCameras.instance.cameraShakeFSM.SendEvent("SmallShake");
            _rb.velocity = new Vector2(0f, 0f);
            gameObject.transform.position = new Vector2(gameObject.transform.GetPositionX(), GROUND_Y);
            yield return new WaitWhile(() => _anim.IsPlaying());
            //GameObject whip = transform.Find("Whip").gameObject;
            //whip.layer = 11;
            if (onlyIsma && !OWArenaFinder.IsInOverWorld) MusicControl();
            StartCoroutine("Start2");
        }

        private void MusicControl()
        {
            Log("Start music");
            WDController.Instance.PlayMusic(FiveKnights.Clips["LoneIsmaMusic"], 1f);
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
            if (!onlyIsma) StartCoroutine(SmashBall());
            StartCoroutine(Agony());
            StartCoroutine(AttackChoice());
            StartCoroutine(SpawnWalls());
            _ddFsm.FsmVariables.FindFsmInt("Damage").Value = 1;
            dd.GetComponent<DamageHero>().damageDealt = 1;
        }
        
        
        private void Update()
        {
            if (_healthPool <= 0 && !isDead)
            {
                Log("Victory");
                isDead = true;
                _healthPool = 100;
                if (isIsmaHitLast && !onlyIsma) StartCoroutine(IsmaDeath());
                else if (onlyIsma) StartCoroutine(IsmaLoneDeath());
                else StartCoroutine(OgrimDeath());
            }
        }

        private Action _prev;

        private IEnumerator AttackChoice()
        {
            int lastA = 3;
            while (true)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 0.8f) + offsetTime);
                yield return new WaitWhile(() => _attacking);
                _attacking = true;
                int r = onlyIsma ? UnityEngine.Random.Range(0, 10) : UnityEngine.Random.Range(0, 7);
                if (r < 4 && lastA != 0)
                {
                    lastA = 0;
                    _prev = AirFist;
                }
                else if ((r < 7 && lastA != 1) || lastA == 0)
                {
                    lastA = 1;
                    _prev = WhipAttack;
                }
                else if (onlyIsma && (lastA != 2 || lastA == 1))
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

        private bool _wallActive;

        private IEnumerator SpawnWalls()
        {
            yield return new WaitWhile(() => _healthPool > WALL_HP);
            EnemyPlantSpawn.isPhase2 = true;
            killAllMinions = true;
            yield return new WaitForSeconds(0.1f);
            killAllMinions = false;
            _wallActive = true;
            if (onlyIsma)
            {
                EnemyPlantSpawn spawner = GameObject.Find("SeedFloor").GetComponent<EnemyPlantSpawn>();
                GameObject tur1 = new GameObject();
                tur1.transform.position = new Vector2(LEFT_X + 10f, GROUND_Y + 14f);
                StartCoroutine(spawner.Phase2Spawn(tur1));
                //tur1.AddComponent<EnemyPlantSpawn>().isSpecialTurret = true;
                GameObject tur2 = new GameObject();
                tur2.transform.position = new Vector2(RIGHT_X - 10f, GROUND_Y + 14f);
                StartCoroutine(spawner.Phase2Spawn(tur2));
                //tur2.AddComponent<EnemyPlantSpawn>().isSpecialTurret = true;
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
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 6);
                _anim.enabled = false;
                yield return new WaitForSeconds(0.1f);
                _anim.enabled = true;
                _rb.velocity = new Vector2(-20f * dir, 0f);
                yield return new WaitWhile(() => _anim.IsPlaying());
                _rb.velocity = new Vector2(0f, 0f);
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
                    yield return new WaitForSeconds(UnityEngine.Random.Range(1, 2));
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
                if (_wallActive) ismaX = _rand.Next((int)LEFT_X + 10, (int)RIGHT_X - 8);
                // if (_wallActive) ismaX = _rand.Next((int)LEFT_X + 7, (int)RIGHT_X - 5);
                transform.position = new Vector2(ismaX, UnityEngine.Random.Range(10, 16));
                ToggleIsma(true);
                float dir = FaceHero();
                _anim.Play("AFistAntic");
                _ap.Clip = _randAud[_rand.Next(0, _randAud.Count)];
                _ap.DoPlayRandomClip();
                _rb.velocity = new Vector2(dir * -20f, 0f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                _rb.velocity = new Vector2(0f, 0f);
                GameObject spike = transform.Find("SpikeArm").gameObject;
                yield return new WaitWhile(() => _anim.IsPlaying());
                GameObject arm = transform.Find("Arm2").gameObject;
                Vector2 diff = arm.transform.position - _target.transform.position;
                float offset2 = 0f;
                if ((dir > 0 && diff.x > 0) || (dir < 0 && diff.x < 0)) offset2 += 180f;
                float rot = Mathf.Atan(diff.y / diff.x) + offset2 * Mathf.Deg2Rad;
                arm.transform.SetRotation2D(rot * Mathf.Rad2Deg);
                SpriteRenderer armSpr = arm.GetComponent<SpriteRenderer>();
                GameObject stripStr = arm.transform.Find("StripStart").gameObject;
                stripStr.SetActive(false);
                GameObject rstrip = arm.transform.Find("StripEnd").gameObject;
                GameObject rfist = arm.transform.Find("SpikeFistB").gameObject;
                GameObject strip = Instantiate(rstrip, rstrip.transform.position, rstrip.transform.rotation);
                GameObject fist = Instantiate(rfist, rfist.transform.position, rfist.transform.rotation);
                SpriteRenderer stpSR = strip.GetComponent<SpriteRenderer>();
                strip.SetActive(false);
                fist.SetActive(false);
                CollisionCheck afc = fist.AddComponent<CollisionCheck>();
                Vector3 strSC = strip.transform.localScale;
                Vector3 fstSc = fist.transform.localScale;
                strip.transform.localScale = new Vector3(dir * strSC.x, strSC.y, strSC.z) * 1.6f;
                fist.transform.localScale = new Vector3(dir * fstSc.x, fstSc.y, fstSc.z) * 1.7f;

                _anim.Play("AFist");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 0);
                spike.SetActive(true);
                _anim.enabled = false;
                yield return new WaitForSeconds(0.5f);
                spike.SetActive(false);
                _anim.enabled = true;

                arm.SetActive(true);
                armSpr.enabled = true;
                stripStr.SetActive(true);
                strip.SetActive(true);
                fist.SetActive(true);
                int i = 0;
                float stp = 0.35f;
                while (!afc.Hit)
                {
                    stpSR.size = new Vector2(stpSR.size.x + stp, 0.42f);
                    Vector2 pos = strip.transform.position;
                    float offset = stpSR.size.x * strip.transform.localScale.x - dir * 0.5f;
                    fist.transform.position = new Vector3(pos.x + offset * Mathf.Cos(rot),
                        pos.y + offset * Mathf.Sin(rot), -0.2f);
                    i++;
                    yield return new WaitForSeconds(0.01f);
                }

                while (i > 0)
                {
                    stpSR.size = new Vector2(stpSR.size.x - stp, 0.42f);
                    Vector2 pos = strip.transform.position;
                    float offset = stpSR.size.x * strip.transform.localScale.x - dir * 0.5f;
                    fist.transform.position = new Vector3(pos.x + offset * Mathf.Cos(rot),
                        pos.y + offset * Mathf.Sin(rot), -0.2f);
                    i--;
                    yield return new WaitForSeconds(0.01f);
                }

                armSpr.enabled = false;
                stripStr.SetActive(false);
                Destroy(strip);
                Destroy(fist);
                spike.SetActive(true);
                _anim.Play("AFist2");
                yield return new WaitForSeconds(0.05f);
                spike.SetActive(false);
                _anim.Play("AFistEnd");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                _anim.enabled = false;
                yield return new WaitForSeconds(0.4f);
                _anim.enabled = true;
                _rb.velocity = new Vector2(dir * -20f, 0f);
                yield return new WaitWhile(() => _anim.IsPlaying());
                _rb.velocity = Vector2.zero;
                ToggleIsma(false);
                StartCoroutine(IdleTimer(IDLE_TIME));
            }

            StartCoroutine(AirFist());
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
                _anim.Play("GFistEnd");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                _anim.enabled = false;
                yield return new WaitForSeconds(0.5f);
                _anim.enabled = true;
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                _rb.velocity = new Vector2(dir * -20f, 0f);
                yield return new WaitWhile(() => _anim.IsPlaying());
                _rb.velocity = Vector2.zero;
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
           
            _anim.Play("GFistEnd");
            transform.position -= new Vector3(WhipXOffset * Math.Sign(transform.localScale.x), WhipYOffset, 0f);
            yield return null;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
            _anim.enabled = false;
            yield return new WaitForSeconds(0.5f);
            _anim.enabled = true;
            yield return null;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
            _rb.velocity = new Vector2(dir * -20f, 0f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            _rb.velocity = Vector2.zero;
            ToggleIsma(false);
            StartCoroutine(IdleTimer(IDLE_TIME));
        }

        private bool ddIsThrowing;

        private IEnumerator SmashBall()
        {
            tk2dSpriteAnimator tk = dd.GetComponent<tk2dSpriteAnimator>();
            while (true)
            {
                yield return new WaitWhile(() => !tk.CurrentClip.name.Contains("Throw"));
                ddIsThrowing = true;
                yield return new WaitWhile(() => _attacking);
                _attacking = true;
                while (tk.CurrentClip.name.Contains("Throw"))
                {
                    foreach (GameObject go in FindObjectsOfType<GameObject>().Where(x => x.name.Contains("Dung Ball Large W") && x.activeSelf && x.transform.GetPositionY() > 15f
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
                ddIsThrowing = false;
                StartCoroutine(IdleTimer(IDLE_TIME));
            }
        }

        IEnumerator Agony()
        {
            yield return new WaitWhile(() => _hm.hp > WALL_HP);
            if (onlyIsma)
            {
                yield return new WaitWhile(() => _attacking);
                _attacking = true;
            }
            else
            {
                yield return new WaitWhile(() => 
                    !FastApproximately(dd.transform.GetPositionY(), -3, 0.2f));//!tk.IsPlaying("Dive In 2"));
                yield return new WaitWhile(() => _attacking);
                _attacking = true;
                float time = 1f;
                yield return new WaitWhile(() => 
                    !FastApproximately(dd.transform.GetPositionY(), -3, 0.2f) && 
                    (time -= Time.deltaTime) > 0f);
                if (time <= 0f)
                {
                    _attacking = false;
                    yield return null;
                    Log("Restarting agony but I really don't know why lmao");
                    StartCoroutine(Agony());
                }
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
            thorn.transform.position = new Vector3(orig.x-1f,orig.y-4f,orig.z);
            thorn.transform.parent = fakeIsma.transform;

            Animator tAnim = thorn.transform.Find("T1").gameObject.GetComponent<Animator>();
            _anim.Play("AgonyLoopIntro");
            yield return null;
            yield return new WaitWhile(() => _anim.IsPlaying());
            _ap.Clip = _randAud[_rand.Next(0, _randAud.Count)];
            _ap.DoPlayRandomClip();
            int j = NUM_AGONY_LOOPS;
            _anim.speed = 1.7f;
            do
            {
                _anim.PlayAt("AgonyLoop", 0);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
                thorn.SetActive(true);
                Vector2 diff = tAnim.transform.position - _target.transform.position;
                float rot = Mathf.Atan(diff.y / diff.x) * Mathf.Rad2Deg + (diff.x < 0 ? 180f : 0f);
                int start = (int)(rot / 30f);
                Animator[] anims = thorn.GetComponentsInChildren<Animator>(true);
                int ind = 0;
                int off = !onlyIsma && _wallActive ? 2 : 3; //1,3
                for (int r = start; r < start + off; r++)
                {
                    Animator i = anims[ind++];
                    i.gameObject.layer = 17;
                    i.gameObject.transform.rotation = Quaternion.Euler(0f, 0f, r == start ? rot : r * 30f + UnityEngine.Random.Range(0, 5) * 6);
                }
                for (int r = start - 1; r > start-off-1; r--)
                {
                    Animator i = anims[ind++];
                    i.gameObject.transform.rotation = Quaternion.Euler(0f, 0f, r * 30f + UnityEngine.Random.Range(0, 5) * 6);
                }
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 9);
                foreach (Animator i in thorn.GetComponentsInChildren<Animator>(true))
                {
                    i.gameObject.GetComponent<SpriteRenderer>().enabled = true;
                    i.Play("NewAThornAnim");
                }
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 11);
                _anim.enabled = false;
                
                yield return new WaitWhile(() => tAnim.GetCurrentFrame() < 4);
                foreach (Animator i in thorn.GetComponentsInChildren<Animator>(true))
                {
                    i.enabled = false;
                }
                yield return new WaitForSeconds(0.2f);
                foreach (Animator i in thorn.GetComponentsInChildren<Animator>(true))
                {
                    i.enabled = true;
                }
                
                yield return new WaitWhile(() => tAnim.GetCurrentFrame() < 6);
                _anim.enabled = true;
                yield return new WaitWhile(() => tAnim.IsPlaying());
                foreach (Animator i in thorn.GetComponentsInChildren<Animator>(true))
                {
                    i.gameObject.GetComponent<SpriteRenderer>().enabled = false;
                    i.Play("IdleThorn");
                }
                thorn.SetActive(false);
                yield return new WaitWhile(() => _anim.IsPlaying());
            }
            while (j-- >= 0 && !ddIsThrowing);

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

        private GameObject fakeIsma;
        
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
            while (true)
            {
                _anim.PlayAt("AgonyLoop", 0);
                _anim.enabled = true;
                _ap.Clip = _randAud[_rand.Next(0, _randAud.Count)];
                _ap.DoPlayRandomClip();
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
                thorn.SetActive(true);
                Vector2 diff = tAnim.transform.position - _target.transform.position;
                float rot = Mathf.Atan(diff.y / diff.x) * Mathf.Rad2Deg + (diff.x < 0 ? 180f : 0f);
                int start = (int)(rot / 30f);
                //float rot = _wallActive ? 60f : -30f;
                Animator[] anims = thorn.GetComponentsInChildren<Animator>(true);
                int ind = 0;
                for (int r = start; r < start + 3; r++)
                {
                    Animator i = anims[ind++];
                    i.gameObject.layer = 17;
                    i.gameObject.transform.rotation = Quaternion.Euler(0f, 0f, r == start ? rot : r * 30f + UnityEngine.Random.Range(0, 5) * 6);
                    //i.GetComponentInChildren<LineRenderer>(true).enabled = true;
                }
                for (int r = start - 1; r > start - 4; r--)
                {
                    Animator i = anims[ind++];
                    i.gameObject.transform.rotation = Quaternion.Euler(0f, 0f, r * 30f + UnityEngine.Random.Range(0, 5) * 6);
                    //i.GetComponentInChildren<LineRenderer>(true).enabled = true;
                }
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 9);
                foreach (Animator i in thorn.GetComponentsInChildren<Animator>(true))
                {
                    i.gameObject.GetComponent<SpriteRenderer>().enabled = true;
                    i.Play("NewAThornAnim");
                }
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 11);
                _anim.enabled = false;
                
                yield return new WaitWhile(() => tAnim.GetCurrentFrame() < 4);
                foreach (Animator i in thorn.GetComponentsInChildren<Animator>(true))
                {
                    i.enabled = false;
                }
                yield return new WaitForSeconds(0.2f);
                foreach (Animator i in thorn.GetComponentsInChildren<Animator>(true))
                {
                    i.enabled = true;
                }
                
                yield return new WaitWhile(() => tAnim.GetCurrentFrame() < 6);
                _anim.enabled = true;
                yield return new WaitWhile(() => tAnim.IsPlaying());
                foreach (Animator i in thorn.GetComponentsInChildren<Animator>(true))
                {
                    i.gameObject.GetComponent<SpriteRenderer>().enabled = false;
                    i.Play("IdleThorn");
                }

                thorn.SetActive(false);
                yield return new WaitWhile(() => _anim.IsPlaying());
            }
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
            WDController.Instance.PlayMusic(null, 1f);
            Destroy(_ddFsm.GetAction<FadeAudio>("Stun Recover", 2).gameObject.GameObject.Value);
            GameManager.instance.gameObject.GetComponent<WDController>()._ap.StopMusic();
            GameManager.instance.gameObject.GetComponent<WDController>()._ap2.StopMusic();
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
            Log("Death ogrim end0");
            if (CustomWP.boss != CustomWP.Boss.All) yield return new WaitForSeconds(1f);
            CustomWP.wonLastFight = true;
            Log("Death ogrim end");
            _ddFsm.enabled = false;
            Destroy(this);
        }

        private IEnumerator IsmaLoneDeath()
        {
            Log("Started Isma Lone Death");
            yield return new WaitWhile(() => _attacking);
            _attacking = true;
            _hm.hp = 300;
            _healthPool = 100;
            Coroutine c = StartCoroutine(LoopedAgony());
            Log("Test");
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            while (_healthPool > 0)
            {
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
            if (!OWArenaFinder.IsInOverWorld) WDController.Instance.PlayMusic(null, 1f);
            else OWBossManager.Instance.PlayMusic(null);
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
            _anim.enabled = true;
            _rb.gravityScale = 0f;
            _rb.velocity = new Vector2(0f,0f);
            yield return _anim.WaitToFrame(5);
            _anim.enabled = false;
            yield return new WaitForSeconds(1f);
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
            GameManager.instance.gameObject.GetComponent<WDController>()._ap.StopMusic();
            GameManager.instance.gameObject.GetComponent<WDController>()._ap2.StopMusic();
            Log("2 dada");
            WDController.Instance.PlayMusic(null, 1f);
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
                _dnailReac.SetConvoTitle(_dnailDial[_rand.Next(_dnailDial.Length)]);
            }
            orig(self);
        }

        private void HealthManager_TakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.name.Contains("Isma"))
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
                _healthPool -= hitInstance.DamageDealt;
                _hitEffects.RecieveHitEffect(hitInstance.Direction);
                isIsmaHitLast = true;
            }
            else if (self.name.Contains("White Defender"))
            {
                _healthPool -= hitInstance.DamageDealt;
                isIsmaHitLast = false;
            }
            orig(self, hitInstance);
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
            _anim.Play("Idle");
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

            foreach (AudioClip i in FiveKnights.IsmaClips.Values.Where(x=> !x.name.Contains("Death")))
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