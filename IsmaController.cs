using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ModCommon.Util;
using ModCommon;
using System.Reflection;
using TMPro;

namespace FiveKnights
{
    public class IsmaController : MonoBehaviour
    {
        //Note: Dreamnail code was taken from Jngo's code :)
        public bool onlyIsma;
        private bool flashing;
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
        private const float LEFT_X = 60.3f;
        private const float RIGHT_X = 90.6f;
        private const float GROUND_Y = 5.9f;
        private const int MAX_HP = 1200;
        private const int WALL_HP = 900;
        private const int SPIKE_HP = 600;
        private const float IDLE_TIME = 0.1f;
        public static float offsetTime;
        public static bool killAllMinions;
        private bool isDead;
        public static bool eliminateMinions;
        private bool isIsmaHitLast;
        public static List<GameObject> PlantF { get; set; }
        public static List<GameObject> PlantG { get; set; }
        public bool introDone;
        private string[] _dnailDial =
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
        }

        private IEnumerator Start()
        {
            Log("Begin Isma");
            yield return null;
            offsetTime = 0f;
            GameObject actor = GameObject.Find("Audio Player Actor");
            _ap = new MusicPlayer
            {
                Volume = 1f,
                Player = actor,
                MaxPitch = 1f,
                MinPitch = 1f,
                Spawn = HeroController.instance.gameObject
            };
            if (CustomWP.boss == CustomWP.Boss.All)
            {
                StartCoroutine(SilLeave());
                yield return new WaitForSeconds(0.15f);
            }
            if (onlyIsma)
            {
                GameObject.Find("Burrow Effect").SetActive(false);
                StartCoroutine(FixArena());
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
            else transform.position = new Vector3(80f, GROUND_Y, 1f);
            float dir = FaceHero();
            _bc.enabled = false;
            _anim.Play("Apear"); //Yes I know it's "appear," I don't feel like changing the assetbundle buddo
            gameObject.transform.position = new Vector2(gameObject.transform.GetPositionX(), GROUND_Y + 8f);
            _rb.velocity = new Vector2(0f, -40f);
            yield return new WaitWhile(() => gameObject.transform.GetPositionY() > GROUND_Y);
            GameCameras.instance.cameraShakeFSM.SendEvent("SmallShake");
            _rb.velocity = new Vector2(0f, 0f);
            gameObject.transform.position = new Vector2(gameObject.transform.GetPositionX(), GROUND_Y);
            yield return new WaitWhile(() => _anim.IsPlaying());
            GameObject whip = transform.Find("Whip").gameObject;
            whip.layer = 11;
            PlantG = new List<GameObject>();
            PlantF = new List<GameObject>();
            StartCoroutine("Start2");
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
            StartCoroutine(ChangeIntroText(Instantiate(area), "Isma", "", "Kindly", false));
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
            _sr.sortingOrder = 20;
            EnemyPlantSpawn.isPhase2 = true;
            killAllMinions = true;
            yield return null;
            killAllMinions = false;
            _wallActive = true;
            if (!onlyIsma)
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

            if (onlyIsma)
            {
                GameObject tur1 = new GameObject();
                tur1.transform.position = new Vector2(70f, 19.8f);
                tur1.AddComponent<EnemyPlantSpawn>().isSpecialTurret = true;
                GameObject tur2 = new GameObject();
                tur2.transform.position = new Vector2(82f, 19.8f);
                tur2.AddComponent<EnemyPlantSpawn>().isSpecialTurret = true;
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
            if (hPos.x > 86f) _target.transform.position = new Vector2(86f,hPos.y);
            else if (hPos.x < 67f) _target.transform.position = new Vector2(67f,hPos.y);
            
            yield return new WaitWhile(() => _healthPool > SPIKE_HP);
            
            GameObject spike = wallR.transform.Find("Spike").gameObject;
            GameObject spike2 = wallL.transform.Find("Spike").gameObject;
            spike.transform.Find("Front").gameObject.layer = spike2.transform.Find("Front").gameObject.layer = 17;
            spike.transform.Find("Front").gameObject.AddComponent<DamageHero>().damageDealt = 1;
            spike2.transform.Find("Front").gameObject.AddComponent<DamageHero>().damageDealt = 1;
            spike.SetActive(true);
            spike2.SetActive(true);
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
                float ismaX = heroX - 75f > 0f ? 70f : 82.5f;
                if (_wallActive) ismaX = heroX - 75f > 0f ? 71f : 82f;
                transform.position = new Vector2(ismaX, GROUND_Y);
                dir = FaceHero();
                _rb.velocity = new Vector2(-20f * dir, 0f);
                ToggleIsma(true);
                _anim.Play("ThrowBomb");
                _ap.Clip = _randAud[_rand.Next(0, _randAud.Count)];
                _ap.DoPlayRandomClip();
                //_aud.PlayOneShot(_randAud[_rand.Next(0, _randAud.Count)]);
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
                seed.transform.localScale *= 1.75f;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
                bomb.SetActive(true);
                rb.gravityScale = 1.3f;
                StartCoroutine(AnimEnder());
                rb.velocity = new Vector2(dir * 30f, 40f);
                CollisionCheck cc = bomb.AddComponent<CollisionCheck>();
                rb.angularVelocity = dir * 900f;
                float xLim = dir > 0 ? 75f : 75f;
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
                    Vector3 scale = localSeed.transform.localScale * 1.4f;
                    localSeed.transform.localScale = new Vector3(scale.x * -1f, scale.y, scale.z);
                    localSeed.GetComponent<Rigidbody2D>().velocity = new Vector2(20f * Mathf.Cos(rot * Mathf.Deg2Rad), 20f * Mathf.Sin(rot * Mathf.Deg2Rad));
                    EnemyPlantSpawn eps = localSeed.AddComponent<EnemyPlantSpawn>();
                    eps.PlantG = PlantG;
                    eps.PlantF = PlantF;
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
                float ismaX = heroX - 75f > 0f ? heroX - distance : heroX + distance;
                if (_wallActive) ismaX = _rand.Next(67, 86);
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

        private void WhipAttack()
        {
            IEnumerator WhipAttack()
            {
                float heroX = _target.transform.GetPositionX();
                float ismaX = heroX - 75f > 0f ? 70f : 84f;
                if (_wallActive) ismaX = heroX - 75f > 0f ? 71f : 82f;
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
                GameObject fist = transform.Find("Arm").gameObject;
                GameObject whiporig = transform.Find("Whip").gameObject;
                GameObject whipPar = new GameObject();
                whipPar.transform.position = gameObject.transform.position;
                whipPar.transform.localScale = gameObject.transform.localScale;
                GameObject whip = Instantiate(whiporig);
                Vector3 orig = whiporig.transform.localScale;
                whip.transform.localScale = new Vector3(orig.x * gameObject.transform.localScale.x, orig.y,orig.z);
                whip.transform.parent = whipPar.transform;
                Animator anim = whip.GetComponent<Animator>();
                yield return null;
                yield return new WaitWhile(() => _anim.IsPlaying());
                whip.SetActive(true);
                anim.Play("Whip");
                yield return new WaitWhile(() => anim.GetCurrentFrame() < 1);
                _anim.Play("GFist");
                fist.SetActive(true);
                yield return new WaitWhile(() => anim.GetCurrentFrame() < 13);
                _anim.Play("GFist2");
                fist.SetActive(false);
                yield return new WaitWhile(() => anim.GetCurrentFrame() < 14);
                _anim.Play("GFist3");
                yield return new WaitWhile(() => anim.IsPlaying());
                anim.Play("Idle");
                whip.SetActive(false);
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
            GameObject fist = transform.Find("Arm").gameObject;
            GameObject whiporig = transform.Find("Whip").gameObject;
            GameObject whipPar = new GameObject();
            whipPar.transform.position = gameObject.transform.position;
            whipPar.transform.localScale = gameObject.transform.localScale;
            GameObject whip = Instantiate(whiporig);
            Vector3 orig = whiporig.transform.localScale;
            whip.transform.localScale = new Vector3(orig.x * gameObject.transform.localScale.x, orig.y, orig.z);
            whip.transform.parent = whipPar.transform;
            Animator anim = whip.GetComponent<Animator>();
            _anim.Play("GFistAntic2");
            yield return null;
            yield return new WaitWhile(() => _anim.IsPlaying());
            whip.SetActive(true);
            anim.Play("Whip");
            yield return new WaitWhile(() => anim.GetCurrentFrame() < 1);
            _anim.Play("GFist");
            fist.SetActive(true);
            yield return new WaitWhile(() => anim.GetCurrentFrame() < 13);
            _anim.Play("GFist2");
            fist.SetActive(false);
            yield return new WaitWhile(() => anim.GetCurrentFrame() < 14);
            _anim.Play("GFist3");
            yield return new WaitWhile(() => anim.IsPlaying());
            anim.Play("Idle");
            whip.SetActive(false);
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
            tk2dSpriteAnimator tk = dd.GetComponent<tk2dSpriteAnimator>();
            while (true)
            {
                int oldHp = _hm.hp - 150;
                if (onlyIsma)
                {
                    yield return new WaitWhile(() => _hm.hp > oldHp);
                    yield return new WaitWhile(() => _attacking);
                    _attacking = true;
                }
                else
                {
                    yield return new WaitWhile(() => _hm.hp > oldHp);
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
                        continue;
                    }
                }
                ToggleIsma(true);
                Vector3 scIs = gameObject.transform.localScale;
                gameObject.transform.localScale = new Vector3(Mathf.Abs(scIs.x), scIs.y, scIs.z);
                gameObject.transform.SetPosition2D(75f, 17.5f);
                
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
                _anim.Play("AgonyLoop");
                int j = onlyIsma ? 5 : 3;
                _anim.speed = 1.7f;
                do
                {
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
                        i.GetComponentInChildren<LineRenderer>(true).enabled = true;
                    }
                    for (int r = start - 1; r > start-off-1; r--)
                    {
                        Animator i = anims[ind++];
                        i.gameObject.transform.rotation = Quaternion.Euler(0f, 0f, r * 30f + UnityEngine.Random.Range(0, 5) * 6);
                        i.GetComponentInChildren<LineRenderer>(true).enabled = true;
                    }
                    yield return new WaitWhile(() => _anim.GetCurrentFrame() < 9);
                    foreach (Animator i in thorn.GetComponentsInChildren<Animator>(true))
                    {
                        LineRenderer lr = i.GetComponentInChildren<LineRenderer>(true);
                        if (!lr.enabled) continue;
                        lr.enabled = false;
                        i.gameObject.GetComponent<SpriteRenderer>().enabled = true;
                        i.Play("ThornShot");
                    }
                    yield return new WaitWhile(() => _anim.GetCurrentFrame() < 11);
                    _anim.enabled = false;
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
        }

        private GameObject fakeIsma;
        
        private IEnumerator LoopedAgony()
        {
            ToggleIsma(true);
            _attacking = true;
            gameObject.transform.SetPosition2D(75f, 17f);
            
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
            _anim.Play("AgonyLoop");
            _anim.speed = 1.6f;
            while (true)
            {
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
                    i.gameObject.transform.rotation = Quaternion.Euler(0f, 0f, r == start ? rot : r * 30f + UnityEngine.Random.Range(0, 5) * 6);
                    i.GetComponentInChildren<LineRenderer>(true).enabled = true;
                }
                for (int r = start - 1; r > start - 4; r--)
                {
                    Animator i = anims[ind++];
                    i.gameObject.transform.rotation = Quaternion.Euler(0f, 0f, r * 30f + UnityEngine.Random.Range(0, 5) * 6);
                    i.GetComponentInChildren<LineRenderer>(true).enabled = true;
                }
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 9);
                foreach (Animator i in thorn.GetComponentsInChildren<Animator>(true))
                {
                    i.GetComponentInChildren<LineRenderer>(true).enabled = false;
                    i.gameObject.GetComponent<SpriteRenderer>().enabled = true;
                    i.Play("ThornShot");
                }
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 11);
                _anim.enabled = false;
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
            PlayDeathFor(dd);
            eliminateMinions = true;
            killAllMinions = true;
            _ddFsm.GetAction<SetVelocity2d>("Stun Launch", 0).y.Value = 45f;
            _ddFsm.SetState("Stun Set");
            yield return null;
            yield return new WaitWhile(() => _ddFsm.ActiveStateName == "Stun Set");
            PlayerData.instance.isInvincible = true;
            HeroController.instance.RelinquishControl();
            GameManager.instance.playerData.disablePause = true;
            if (dd.transform.position.x > _target.transform.position.x) HeroController.instance.FaceRight();
            else HeroController.instance.FaceLeft();
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
            PlayerData.instance.isInvincible = false;
            HeroController.instance.RegainControl();
            GameManager.instance.playerData.disablePause = false;
            HeroController.instance.StartAnimationControl();
            yield return new WaitWhile(() => transform.position.y < 25f);
            //GameObject.Find("Main").GetComponent<AudioSource>().volume = 0f;
            WDController.CustomAudioPlayer.StopMusic();
            PlayMakerFSM fsm = GameObject.Find("Battle Scene").LocateMyFSM("Battle Scene");

            PlayMakerFSM pm = GameCameras.instance.tk2dCam.gameObject.LocateMyFSM("CameraFade");
            pm.SendEvent("FADE OUT INSTANT");
            PlayMakerFSM fsm2 = GameObject.Find("Blanker White").LocateMyFSM("Blanker Control");
            fsm2.FsmVariables.FindFsmFloat("Fade Time").Value = 0;
            fsm2.SendEvent("FADE IN");
            yield return null;
            HeroController.instance.MaxHealth();
            yield return null;
            GameCameras.instance.cameraFadeFSM.FsmVariables.FindFsmBool("No Fade").Value = true;
            yield return null;
            GameManager.instance.BeginSceneTransition(new GameManager.SceneLoadInfo
            {
                SceneName = "White_Palace_09",
                EntryGateName = "door_dreamReturnGGstatueStateIsma_GG_Statue_ElderHu(Clone)(Clone)",
                Visualization = GameManager.SceneLoadVisualizations.GodsAndGlory,
                WaitForSceneTransitionCameraFade = false,
                PreventCameraFadeOut = true,
                EntryDelay = 0

            });
            CustomWP.Instance.wonLastFight = true;
            Destroy(this);
        }

        private IEnumerator IsmaLoneDeath()
        {
            Log("Started Isma Lone Death");
            yield return new WaitWhile(() => _attacking);
            _attacking = true;
            _hm.hp = 200;
            _healthPool = 100;
            Coroutine c = StartCoroutine(LoopedAgony());
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            while (_healthPool > 0)
            {
                yield return new WaitForEndOfFrame();
            }
            _sr.sortingOrder = 0;
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
            PlayDeathFor(gameObject);
            if (WDController.CustomAudioPlayer != null) WDController.CustomAudioPlayer.StopMusic();
            //GameObject.Find("Main").GetComponent<AudioSource>().volume = 0f;
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
            yield return new WaitWhile(() => transform.position.y > GROUND_Y+2.5f);
            _anim.enabled = true;
            _rb.gravityScale = 0f;
            _rb.velocity = new Vector2(0f,0f);
            yield return new WaitForSeconds(1f);
            CustomWP.Instance.wonLastFight = true;
            Destroy(this);
        }

        private IEnumerator IsmaDeath()
        {
            Log("Started Isma Death");
            yield return new WaitWhile(() => _attacking);
            _attacking = true;

            foreach (FsmTransition i in _ddFsm.GetState("Idle").Transitions)
            {
                _ddFsm.ChangeTransition("Idle", i.EventName, "Timer");
            }
            _ddFsm.SetState("Idle");
            yield return new WaitWhile(() => _ddFsm.ActiveStateName != "Idle");
            yield return new WaitWhile(() => !_ddFsm.ActiveStateName.Contains("Tunneling"));
            tk2dSpriteAnimator tk = dd.GetComponent<tk2dSpriteAnimator>();
            _ddFsm.enabled = false;

            _sr.sortingOrder = 0;
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
            StopCoroutine(c);
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
            PlayDeathFor(gameObject);
            WDController.CustomAudioPlayer.StopMusic();
            //GameObject.Find("Main").GetComponent<AudioSource>().volume = 0f;

            _anim.Play("Falling");
            PlayerData.instance.isInvincible = true;
            HeroController.instance.RelinquishControl();
            GameManager.instance.playerData.disablePause = true;
            if (dd.transform.position.x > _target.transform.position.x) HeroController.instance.FaceRight();
            else HeroController.instance.FaceLeft();

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
            yield return new WaitWhile(() => gameObject.transform.position.y < 22f);
            PlayerData.instance.isInvincible = false;
            HeroController.instance.RegainControl();
            GameManager.instance.playerData.disablePause = false;
            HeroController.instance.StartAnimationControl();
            yield return new WaitWhile(() => transform.position.y < 25f);
            //Time.timeScale = 1f;
            yield return new WaitForSeconds(1f);
            CustomWP.Instance.wonLastFight = true;
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
            flashing = false;
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
            sil.transform.localScale *= 1.15f;
            sil.gameObject.AddComponent<Rigidbody2D>().velocity = new Vector2(0f, 80f);
            sil.gameObject.GetComponent<Rigidbody2D>().gravityScale = 0f;
            sil.sprite = ArenaFinder.Sprites["Sil_Isma_1"];
            yield return new WaitForSeconds(0.05f);
            sil.sprite = ArenaFinder.Sprites["Sil_Isma_2"];
            yield return new WaitForSeconds(0.05f);
            sil.sprite = ArenaFinder.Sprites["Sil_Isma_3"];
            yield return new WaitForSeconds(0.05f);
            sil.gameObject.SetActive(false);
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

        IEnumerator FixArena()
        {
            yield return null;
            foreach (var i in FindObjectsOfType<GameObject>().Where(x => x.name.Contains("dung")))
            {
                Destroy(i);
            }
            yield return null;
            GameObject go = Instantiate(FiveKnights.preloadedGO["ismaBG"]);
            foreach (SpriteRenderer i in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                i.gameObject.SetActive(true);
                i.material = new Material(Shader.Find("Sprites/Default"));
                if (i.name == "acid_plant_0020_root1" || i.name == "acid_plant_0000_root9 (2)")
                {
                    i.transform.position = i.transform.position - new Vector3(0f, 12.5f, 0f);
                }
                if (i.name.Contains("acid_root_floor"))
                {
                    i.transform.position = i.transform.position - new Vector3(0f,0.15f,0f);
                }
            }
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
                _ap.Clip = ArenaFinder.IsmaClips["IsmaAudDeath"];
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
                if (fi.Name.Contains("Origin"))
                {
                    hitEff.effectOrigin = new Vector3(0f, 0.5f, 0f);
                    continue;
                }
                fi.SetValue(hitEff, fi.GetValue(ogrimHitEffects));
            }
            _deathEff = _ddFsm.gameObject.GetComponent<EnemyDeathEffectsUninfected>();

            foreach (AudioClip i in ArenaFinder.IsmaClips.Values.Where(x=> !x.name.Contains("Death")))
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
            Modding.Logger.Log("[Isma] " + o);
        }
    }
}