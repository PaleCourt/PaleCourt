using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Modding;
using ModCommon;
using System.Collections;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using HutongGames.Utility;
using ModCommon.Util;
using System.Reflection;
using TMPro;
using UnityEngine.UI;
using ActionData = On.HutongGames.PlayMaker.ActionData;
using Object = UnityEngine.Object;
using StartCoroutine = IL.HutongGames.PlayMaker.Actions.StartCoroutine;

namespace FiveKnights
{
    public class ZemerControllerP2 : MonoBehaviour
    {
        private HealthManager _hm;
        private BoxCollider2D _bc;
        private SpriteRenderer _sr;
        private float faderTimer;
        private const float faderTimerMax = 8f;
        private int faderIndex = 0;
        private EnemyDreamnailReaction _dnailReac;
        private AudioSource _aud;
        private GameObject _dd;
        private bool flashing;
        private GameObject _dnailEff;
        private Animator _anim;
        private Rigidbody2D _rb;
        private System.Random _rand;
        private EnemyHitEffectsUninfected _hitEffects;
        private EnemyDeathEffectsUninfected _deathEff;
        private GameObject _target;
        private const float GroundY = 9.75f;
        private const float LeftX = 61.5f;
        private const float RightX = 91.6f;
        private const int Phase2HP = 950;
        private PlayMakerFSM _pvFsm;
        private Coroutine _counterRoutine;
        private bool _blockedHit;
        private bool atPhase2;
        private bool _attacking;
        private List<Action> _moves;
        private GameObject[] afterImageFades;
        private Dictionary<Action, int> _maxRepeats;
        private Dictionary<Action, int> _repeats;
        private bool shouldFade;
        private bool isHit;
        public bool shouldNotDoPhase2;

        private readonly string[] _dnailDial =
        {
            "ISMA_DREAM_1",
            "ISMA_DREAM_2",
            "ISMA_DREAM_3"
        };

        private void Awake()
        {
            OnDestroy();
            On.HealthManager.TakeDamage += HealthManager_TakeDamage;
            On.EnemyDreamnailReaction.RecieveDreamImpact += OnReceiveDreamImpact;
            _hm = gameObject.GetComponent<HealthManager>();
            _anim = gameObject.GetComponent<Animator>();
            _bc = gameObject.GetComponent<BoxCollider2D>();
            _rb = gameObject.GetComponent<Rigidbody2D>();
            _rb.isKinematic = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _sr = GetComponent<SpriteRenderer>();
            _dnailReac = gameObject.GetComponent<EnemyDreamnailReaction>();
            _aud = gameObject.GetComponent<AudioSource>();
            gameObject.GetComponent<DamageHero>().damageDealt = 1;
            _dd = FiveKnights.preloadedGO["WD"];
            _dnailEff = _dd.GetComponent<EnemyDreamnailReaction>().GetAttr<EnemyDreamnailReaction, GameObject>("dreamImpactPrefab");
            _dnailReac.enabled = true;
            _rand = new System.Random();
            _dnailReac.SetConvoTitle(_dnailDial[_rand.Next(_dnailDial.Length)]);
            _hitEffects = gameObject.GetComponent<EnemyHitEffectsUninfected>();
            _hitEffects.enabled = true;
            gameObject.GetComponent<Flash>();
            _pvFsm = FiveKnights.preloadedGO["PV"].LocateMyFSM("Control");
            afterImageFades = new GameObject[10];
            for (int i = 0; i < afterImageFades.Length; i++)
            {
                afterImageFades[i] = new GameObject("afterimage");
                afterImageFades[i].AddComponent<SpriteRenderer>();
                afterImageFades[i].GetComponent<SpriteRenderer>().material = _sr.material;
                afterImageFades[i].AddComponent<AfterimageFader>();
            }

            shouldFade = false;
        }

        private void Start()
        {
            Log("Start2");
            _deathEff = _dd.GetComponent<EnemyDeathEffectsUninfected>();
            _target = HeroController.instance.gameObject;
            EndPhase1();
        }
        
        private void Update()
        {
            if (transform.GetPositionX() > RightX && _rb.velocity.x > 0f)
            {
                _rb.velocity = new Vector2(0f, _rb.velocity.y);
            }
            if (transform.GetPositionX() < LeftX && _rb.velocity.x < 0f)
            {
                _rb.velocity = new Vector2(0f, _rb.velocity.y);
            }
            
            if (!shouldFade || !_sr.enabled) return;
            faderTimer += Time.deltaTime;
            if (faderTimer > faderTimerMax / 60)
            {
                faderTimer -= faderTimerMax / 60;
                afterImageFades[faderIndex].GetComponent<SpriteRenderer>().sprite = _sr.sprite;
                afterImageFades[faderIndex].transform.position = gameObject.transform.position;
                afterImageFades[faderIndex].transform.localScale = gameObject.transform.localScale;
                afterImageFades[faderIndex].GetComponent<AfterimageFader>().BeginFade();
                faderIndex++;
                faderIndex = faderIndex % afterImageFades.Length;
            }
        }

        //Add check where player pos is detected with velocity as well
        private void NailLaunch()
        {
            float dir = 0f;
            IEnumerator Throw()
            {
                dir = FaceHero();
                float rot = 0f;
                _anim.Play("ZThrow1");
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
                Vector2 hero = _target.transform.position;
                Vector2 zem = gameObject.transform.position;
                dir = FaceHero();
                rot = Mathf.Atan((hero.y - zem.y) / (hero.x - zem.x));
                float rotVel = (dir > 0) ? rot + 180 * Mathf.Deg2Rad : rot;
                GameObject arm = transform.Find("NailHand").gameObject;
                GameObject nailPar = Instantiate(transform.Find("ZNailB").gameObject);
                Rigidbody2D parRB = nailPar.GetComponent<Rigidbody2D>();
                arm.transform.SetRotation2D(rot * Mathf.Rad2Deg);
                nailPar.transform.SetRotation2D(rot * Mathf.Rad2Deg);
                nailPar.transform.position = transform.Find("ZNailB").position;
                nailPar.transform.localScale = new Vector3(dir * 1.6f,1.6f,1.6f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 5);
                nailPar.SetActive(true);
                parRB.velocity = new Vector2(Mathf.Cos(rotVel) * 25f, Mathf.Sin(rotVel) * 25f);
                yield return new WaitWhile(() => _anim.IsPlaying());
                StartCoroutine(LaunchSide(nailPar));
                //StartCoroutine(LaunchUp(rot, nailPar));
                //StartCoroutine(rot > 15f && rot < 165f ? LaunchUp(rot, nailPar) : LaunchSide(nailPar));
            }
            
            //In phase 1, have her just pick up the sword using reverse slam animation but in later phases add combos after attack
            
            IEnumerator LaunchUp(float rot, GameObject nail)
            {
                float rotVel = (dir > 0) ? rot + 180 * Mathf.Deg2Rad : rot;
                GameObject col = nail.transform.Find("ZNailC").gameObject;
                CollisionCheck cc = col.AddComponent<CollisionCheck>();
                Rigidbody2D rb = nail.GetComponent<Rigidbody2D>();
                if (rb.velocity.y < 0f)
                {
                    _attacking = false;
                    yield break;
                }
                _anim.speed *= 2f;
                
                _anim.Play("ZThrow2");
                _rb.velocity = new Vector2(-dir * 30f, 0f);
                yield return null;
                Spring(false, transform.position + new Vector3(-dir * 2.7f,0f,0f));
                yield return new WaitWhile(() => _anim.IsPlaying());
                cc.shouldStopForMe = true;
                ToggleZemer(false, false);

                _rb.velocity = Vector2.zero;
                cc.isHit = false;
                yield return new WaitWhile((() => nail.transform.position.y < 17f && !cc.isHit));
                transform.position = nail.transform.position + new Vector3(5f*Mathf.Cos(rotVel),5f*Mathf.Sin(rotVel),0f);
                _anim.Play("ZThrow3Air");
                ToggleZemer(true, false);
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
                nail.SetActive(false);
                _anim.speed /= 2f;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
                _anim.enabled = false;
                yield return new WaitForSeconds(0.25f);
                _anim.enabled = true;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 5);
                nail.SetActive(true);
                Vector2 hero = _target.transform.position;
                Vector2 zem = gameObject.transform.position;
                dir = FaceHero();
                rot = Mathf.Atan((hero.y - zem.y) / (hero.x - zem.x));
                rotVel = (dir > 0) ? rot + 180 * Mathf.Deg2Rad : rot;
                nail.transform.SetRotation2D(rot * Mathf.Rad2Deg + 180f);
                rb.velocity = new Vector2(Mathf.Cos(rotVel) * 60f, Mathf.Sin(rotVel) * 60f);
                nail.transform.position = transform.position;
                cc.shouldStopForMe = false;
                cc.action = () =>
                {
                    GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
                    nail.GetComponent<SpriteRenderer>().enabled = false;
                    nail.transform.Find("ZNailN").GetComponent<SpriteRenderer>().enabled = true;
                };
                yield return new WaitForSeconds(0.05f);
                cc.shouldStopForMe = true;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 8);
                rotVel = dir > 0 ? 10f: -45f;
                _rb.velocity = new Vector2(25f * Mathf.Cos(rotVel), 25f * Mathf.Sin(rotVel));
                yield return new WaitForSeconds(0.12f);
                Spring(false, transform.position + new Vector3(3.5f * Mathf.Cos(rotVel), 3.5f * Mathf.Sin(rotVel),0f));
                yield return new WaitWhile(() => _anim.IsPlaying());
                ToggleZemer(false, true);
                yield return new WaitForSeconds(1.5f);
                _rb.velocity = Vector2.zero;
                transform.position = new Vector3(80f, GroundY);
                StartCoroutine(LaunchSide(nail, false));
            }

            //Be sure to never allow zemer to attack to a wall she is close to, otherwise this will fail!
            //Also, if player is too close and on ground, it will fail
            IEnumerator LaunchSide(GameObject nail, bool leave = true)
            {
                GameObject col = nail.transform.Find("ZNailC").gameObject;
                Rigidbody2D rbNail = nail.GetComponent<Rigidbody2D>();
                CollisionCheck cc = col.GetComponent<CollisionCheck>();
                if (cc == null) cc = col.AddComponent<CollisionCheck>();
                if (leave)
                {
                    _anim.Play("ZThrow2");
                    _rb.velocity = new Vector2(-dir * 30f, 0f);
                    cc.isHit = false;
                    yield return null;
                    yield return new WaitWhile(() => !cc.isHit && _anim.IsPlaying());
                    ToggleZemer(false, true);
                    Spring(false, transform.position + new Vector3(-dir * 3.5f, 0f, 0f));
                    yield return new WaitWhile(() => !cc.isHit);
                    Log("Stop nail");
                    rbNail.velocity = Vector2.zero;
                    nail.GetComponent<SpriteRenderer>().enabled = false;
                    nail.transform.Find("ZNailN").GetComponent<SpriteRenderer>().enabled = true;
                    yield return new WaitForSeconds(1f);
                }
                
                Vector2 zem = transform.position;
                Vector2 nl = nail.transform.Find("Point").position;
                Vector3 zemSc = transform.localScale;
                
                if (nl.x < 75f)
                {
                    transform.localScale = new Vector3(Mathf.Abs(zemSc.x), zemSc.y, zemSc.z);
                    transform.position = new Vector2(nl.x + 5f, zem.y);
                    Log("SCA1 " + nail.transform.GetRotation2D() + " | pos: " + nl.x);
                    Spring(true, transform.position);
                    yield return new WaitForSeconds(0.15f);
                    ToggleZemer(true, false);
                    _rb.velocity = new Vector2(-40f, 0f);
                    _anim.Play("ZThrow3Gnd");
                    yield return null;
                    yield return new WaitWhile(() => 
                        _anim.GetCurrentFrame() < 1 && !FastApproximately(transform.position.x, nl.x, 0.3f));
                    _anim.enabled = false;
                    yield return new WaitWhile(() => 
                        !FastApproximately(transform.position.x, nl.x, 0.3f));
                    _anim.enabled = true;
                    _rb.velocity = new Vector2(0f,0f);
                    yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                    Destroy(nail);
                }
                else
                {
                    transform.localScale = new Vector3(-Mathf.Abs(zemSc.x), zemSc.y, zemSc.z);
                    transform.position = new Vector2(nl.x - 5f, zem.y);
                    Log("SCA2 " + nail.transform.GetRotation2D() + " | pos: " + nl.x);
                    Spring(true, transform.position);
                    yield return new WaitForSeconds(0.15f);
                    ToggleZemer(true, false);
                    _rb.velocity = new Vector2(40f, 0f);
                    _anim.Play("ZThrow3Gnd");
                    yield return null;
                    yield return new WaitWhile(() => 
                        _anim.GetCurrentFrame() < 1 && !FastApproximately(transform.position.x, nl.x, 0.3f));
                    _anim.enabled = false;
                    yield return new WaitWhile(() => 
                        !FastApproximately(transform.position.x, nl.x, 0.3f));
                    _anim.enabled = true;
                    _rb.velocity = new Vector2(0f,0f);
                    yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                    Destroy(nail);
                }

                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
                _anim.enabled = false;
                yield return new WaitForSeconds(0.25f);
                _anim.enabled = true;
                _anim.speed *= 5.5f;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.speed /= 5.5f;
                SpawnShockwaves(2f,50f,1,transform.position);
                GameCameras.instance.cameraShakeFSM.SendEvent("BigShake");
                yield return new WaitForSeconds(1f);
                _attacking = false;
            }

            StartCoroutine(Throw());
        }
        
        private void Dash()
        {
            IEnumerator Dash()
            {
                float dir = FaceHero();
                transform.Find("HyperCut").Find("Hyper1").GetComponent<SpriteRenderer>().enabled = false;
                transform.Find("HyperCut").Find("Hyper2").GetComponent<SpriteRenderer>().enabled = false;
                
                _anim.Play("ZDash");
                yield return null;
                yield return new WaitWhile(()=> _anim.GetCurrentFrame() < 5);
                if (FastApproximately(_target.transform.position.x, transform.position.x, 5f))
                {
                    StartCoroutine(StrikeAlternate());
                    yield break;
                }
                yield return new WaitWhile(()=> _anim.GetCurrentFrame() < 6);
                _rb.velocity = new Vector2(-dir * 60f, 0f);
                yield return new WaitWhile(()=> _anim.GetCurrentFrame() < 7);
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(()=> _anim.GetCurrentFrame() < 9);
                Transform par = transform.Find("HyperCut");
                GameObject slashR = Instantiate(par.Find("Hyper3R").gameObject);
                GameObject slashL = Instantiate(par.Find("Hyper3L").gameObject);
                Rigidbody2D rbR = slashR.GetComponent<Rigidbody2D>();
                Rigidbody2D rbL = slashL.GetComponent<Rigidbody2D>();
                slashR.SetActive(true);
                slashL.SetActive(true);
                slashR.transform.position = par.Find("Hyper3R").position;
                slashL.transform.position = par.Find("Hyper3L").position;
                slashR.transform.localScale = Product(par.transform.localScale, new Vector3(dir,1f,1f));
                slashL.transform.localScale =  Product(par.transform.localScale, new Vector3(dir,1f,1f));
                rbL.velocity = new Vector2(-dir * 45f,0f);
                rbR.velocity = new Vector2(dir * 45f,0f);
                yield return new WaitWhile(()=> _anim.IsPlaying());
                _anim.Play("ZIdle");
                _attacking = false;
            }

            IEnumerator StrikeAlternate()
            {
                float dir = FaceHero();
                
                _anim.Play("DashCounter");
                yield return null;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("ZIdle");
                _attacking = false;
            }

            StartCoroutine(Dash());
        }
        
        private void EndPhase1()
        {
            float dir;

            IEnumerator KnockedOut()
            {
                dir = -FaceHero();
                PlayDeathFor(gameObject);
                _bc.enabled = false;
                _anim.enabled = true;
                _rb.velocity = Vector2.zero;
                _anim.Play("ZKnocked");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                transform.position = new Vector3(transform.position.x, GroundY-0.95f);
                yield return new WaitWhile(() => _anim.IsPlaying());
                yield return new WaitForSeconds(1.75f);
                if (shouldNotDoPhase2)
                {
                    CustomWP.Instance.wonLastFight = true;
                    // Stop music here.
                    Destroy(this);
                    yield break;
                }

                isHit = false;
                yield return new WaitWhile(() => !isHit);
                StartCoroutine(Recover());
            }

            IEnumerator Recover()
            {
                _anim.Play("ZRecover");
                yield return null;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("ZThrow2B");
                _rb.velocity = new Vector2(dir * 25f, 25f);
                yield return new WaitForSeconds(0.13f);
                Spring(false, transform.position + new Vector3(dir * 4f, 4,0f));
                yield return new WaitWhile(() => _anim.IsPlaying());
                ToggleZemer(false, true);
                _rb.velocity = Vector2.zero;
                yield return new WaitForSeconds(0.6f);
                StartCoroutine(TransitionIn());
            }
            IEnumerator TransitionIn()
            {
                float heroX = _target.transform.GetPositionX();
                float x = (heroX < 75f) ? heroX + _rand.Next(8,11) : heroX - _rand.Next(8, 11);
                float xOff = (heroX < 75f) ? -4f : 4f;
                transform.position = new Vector3(x, GroundY + 6f);
                dir = FaceHero();
                Spring(true, transform.position, 1.8f);
                yield return new WaitForSeconds(0.05f);
                ToggleZemer(true, false);
                var diff = new Vector2(x - heroX + xOff, transform.position.y - GroundY-0.95f);
                float rot = Mathf.Atan(diff.y / diff.x);
                rot = (heroX < 75f) ? rot + Mathf.PI : rot;
                _rb.velocity = new Vector2(55f*Mathf.Cos(rot), 55f*Mathf.Sin(rot));
                _anim.Play("Z1ZipIn");
                yield return null;
                yield return new WaitWhile(() => transform.position.y > GroundY-0.95f);
                transform.position = new Vector3(transform.position.x, GroundY-0.95f);
                _rb.velocity = new Vector2(-dir * 40f, 0f);
                _anim.Play("Z2Crawl");
                yield return null;
                yield return new WaitForSeconds(0.08f);
                //yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                _rb.velocity = new Vector2(0f, 0f);
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("Z3Swing");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 5);
                _rb.velocity = new Vector2(dir * 40f, 40f);
                yield return new WaitForSeconds(0.08f);
                Spring(false, transform.position + new Vector3(dir * 4f, 4,0f),1.85f);
                yield return new WaitWhile(() => _anim.IsPlaying());
                ToggleZemer(false, true);
                yield return new WaitForSeconds(0.11f);
                _rb.velocity = Vector2.zero;
                StartCoroutine(FlyStrike());
            }

            IEnumerator FlyStrike()
            {
                float heroX = _target.transform.position.x;
                float x = (heroX < 75f) ? heroX + _rand.Next(6,7) : heroX - _rand.Next(6,7);
                transform.position = new Vector3(x, GroundY + 9.5f);
                dir = FaceHero();
                Spring(true, transform.position, 1.85f);
                yield return new WaitForSeconds(0.11f);
                ToggleZemer(true, false);
                var diff = new Vector2(x - heroX, transform.position.y - GroundY-0.95f);
                float rot = Mathf.Atan(diff.y / diff.x);
                rot = (heroX < 75f) ? rot + Mathf.PI : rot;
                _anim.Play("Z4AirSweep");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                _anim.speed *= 1.5f;
                _rb.velocity = new Vector2(45f*Mathf.Cos(rot), 45f*Mathf.Sin(rot));
                yield return new WaitWhile(() => transform.position.y > GroundY-0.95f);
                _anim.speed /= 1.5f;
                transform.position = new Vector3(transform.position.x, GroundY-0.95f);
                _rb.velocity = Vector2.zero;
                StartCoroutine(LandSlide());
            }

            IEnumerator LandSlide()
            {
                dir = FaceHero();
                _anim.Play("Z5LandSlide");
                _rb.velocity = new Vector2(-dir * 40f, 0f);
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(() => _anim.IsPlaying());
                StartCoroutine(LaserNuts());
            }

            IEnumerator LaserNuts()
            {
                _anim.Play("Z6LaserSpin");
                transform.position = new Vector3(transform.position.x, GroundY - 1.2f);
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                Coroutine slashes = StartCoroutine(SpawnSlashes());
                yield return new WaitWhile(() => _anim.IsPlaying());
                if (slashes != null) StopCoroutine(slashes);
                transform.position = new Vector3(transform.position.x, GroundY);
                _anim.Play("ZIdle");
                FaceHero();
            }

            IEnumerator SpawnSlashes()
            {
                int zemX = (int) transform.position.x;
                List<int> lstRight = new List<int>();
                List<int> lstLeft = new List<int>();
                for (int i = zemX+5; i < RightX + 3; i += _rand.Next(3,6)) lstRight.Add(i);
                for (int i = zemX-5; i > LeftX -3; i -= _rand.Next(3,6)) lstLeft.Add(i);
                while (lstRight.Count != 0 || lstLeft.Count != 0)
                {
                    if (lstLeft.Count > 0)
                    {
                        int ind = _rand.Next(0, lstLeft.Count);
                        int rot = _rand.Next(-10, 10);
                        StartCoroutine(SingleSlashControl(lstLeft[ind], -1f, rot));
                        lstLeft.RemoveAt(ind);
                    }
                    if (lstRight.Count > 0)
                    {
                        int ind = _rand.Next(0, lstRight.Count);
                        int rot = _rand.Next(-10, 10);
                        StartCoroutine(SingleSlashControl(lstRight[ind], 1f, rot));
                        lstRight.RemoveAt(ind);
                    }

                    yield return new WaitForSeconds(0.05f);
                }

                IEnumerator SingleSlashControl(float posX, float scaleSig, float angle)
                {
                    yield return new WaitForSeconds(_rand.Next(12,25)/100f);
                    GameObject slash = Instantiate(FiveKnights.preloadedGO["SlashBeam"]);
                    Animator anim = slash.GetComponent<Animator>();
                    slash.transform.localScale *= 1.43f;
                    slash.transform.position = new Vector3(posX, GroundY-1.3f);
                    slash.transform.SetRotation2D(angle);
                    slash.SetActive(true);
                    anim.enabled = true;
                    anim.Play("Laser");
                    Vector3 vec = slash.transform.localScale;
                    slash.transform.localScale = new Vector3(scaleSig *vec.x, vec.y, vec.z);
                }
            }
            
            StartCoroutine(KnockedOut());
        }
        
        private void Turn()
        {
            IEnumerator Turn()
            {
                _anim.Play("ZTurn");
                yield return new WaitForSeconds(0.05f);
                FaceHero();
                _anim.Play("Idle");
                _attacking = false;
            }
            StartCoroutine(Turn());
        }
        
        private void Spring(bool isIn, Vector2 pos, float speedSca = 1f)
        {
            string n =  "VapeIn2";
            GameObject go = Instantiate(FiveKnights.preloadedGO[n]);
            PlayMakerFSM fsm = go.LocateMyFSM("FSM");
            go.GetComponent<tk2dSpriteAnimator>().GetClipByName("Plink").fps = 24 * speedSca;
            go.transform.localScale *= 1.3f;
            fsm.GetAction<Wait>("State 1", 0).time = 0f;
            go.transform.position = pos;
            go.SetActive(true);
        }
        
        private void OnBlockedHit(On.HealthManager.orig_Hit orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.name.Contains("Zemer"))
            {
                StartCoroutine(GameManager.instance.FreezeMoment(0.04f, 0.35f, 0.04f, 0f));
                // Prevent code block from running every frame
                if (!_blockedHit)
                {
                    _blockedHit = true;
                    Log("Blocked Hit");
                    StopCoroutine(_counterRoutine);

                    //StartCoroutine(Countered());
                }
            }

            orig(self, hitInstance);
        }
        
        private void OnReceiveDreamImpact(On.EnemyDreamnailReaction.orig_RecieveDreamImpact orig, EnemyDreamnailReaction self)
        {
            if (self.name.Contains("Zemer"))
            {
                StartCoroutine(FlashWhite());
                Instantiate(_dnailEff, transform.position, Quaternion.identity);
                _dnailReac.SetConvoTitle(_dnailDial[_rand.Next(_dnailDial.Length)]);
            }
            orig(self);
        }

        private void HealthManager_TakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.name.Contains("Zemer"))
            {
                isHit = true;
                _hitEffects.RecieveHitEffect(hitInstance.Direction);
            }
            orig(self, hitInstance);
        }
        
        private float FaceHero()
        {
            float currSign = Mathf.Sign(transform.GetScaleX());
            float heroSignX = Mathf.Sign(gameObject.transform.GetPositionX() - _target.transform.GetPositionX());
            Vector3 pScale = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector3(Mathf.Abs(pScale.x) * heroSignX, pScale.y, 1f);
            return heroSignX;
        }
        
        private Vector3 Product(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x * b.x, a.y*b.y, a.z * b.z);
        }

        IEnumerator FlashWhite()
        {
            _sr.material.SetFloat("_FlashAmount", 1f);
            yield return null;
            for (float i = 1f; i >= 0f; i -= 0.05f)
            {
                _sr.material.SetFloat("_FlashAmount", i);
                yield return new WaitForSeconds(0.02f);
            }
            yield return null;
            flashing = false;
        }
        
        private void SpawnShockwaves(float vertScale, float speed, int damage, Vector2 pos)
        {
            bool[] facingRightBools = {false, true};
            PlayMakerFSM fsm = FiveKnights.preloadedGO["Mage"].LocateMyFSM("Mage Lord");
            foreach (bool facingRight in facingRightBools)
            {
                GameObject shockwave =
                    Instantiate(fsm.GetAction<SpawnObjectFromGlobalPool>("Quake Waves", 0).gameObject.Value); ;
                PlayMakerFSM shockFSM = shockwave.LocateMyFSM("shockwave");
                shockFSM.FsmVariables.FindFsmBool("Facing Right").Value = facingRight;
                shockFSM.FsmVariables.FindFsmFloat("Speed").Value = speed;
                shockwave.AddComponent<DamageHero>().damageDealt = damage;
                shockwave.SetActive(true);
                shockwave.transform.SetPosition2D(new Vector2(pos.x + (facingRight ? 0.5f : -0.5f), 6f));
                shockwave.transform.SetScaleX(vertScale);
            }
        }
        
        private bool IsPlayAboveHead()
        {
            return FastApproximately(transform.position.x, _target.transform.GetPositionX(), 1.2f);
        }

        public void PlayAudioClip(string clipName, float pitchMin = 1.0f, float pitchMax = 1.0f, float time = 0.0f)
        {
            AudioClip GetAudioClip()
            {
                switch (clipName)
                {
                    case "Counter":
                        return (AudioClip) _pvFsm.GetAction<AudioPlayerOneShotSingle>("Counter Stance", 1).audioClip
                            .Value;
                    case "Slash":
                        return (AudioClip) _pvFsm.GetAction<AudioPlayerOneShotSingle>("Slash1", 1).audioClip.Value;
                    default:
                        return null;
                }
            }

            _aud.pitch = (float) (_rand.NextDouble() * pitchMax) + pitchMin;
            _aud.time = time; 
            _aud.PlayOneShot(GetAudioClip());
            _aud.time = 0.0f;
        }
        
        private static bool FastApproximately(float a, float b, float threshold)
        {
            return ((a - b) < 0 ? ((a - b) * -1) : (a - b)) <= threshold;
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
        }
        
        private void ToggleZemer(bool visible, bool fade = false)
        {
            IEnumerator Fade()
            {
                _bc.enabled = false;
                Color col = _sr.color;
                _sr.enabled = true;
                if (visible)
                {
                    _sr.color = new Color(col.r, col.g, col.b, 0f);
                    for (float i = 0; i <= 1f; i += 0.1f)
                    {
                        _sr.color = new Color(col.r, col.g, col.b, i);
                        yield return new WaitForSeconds(0.01f);
                    }
                }
                else
                {
                    _sr.color = new Color(col.r, col.g, col.b, 1f);
                    for (float i = col.a; i >= 0f; i -= 0.1f)
                    {
                        _sr.color = new Color(col.r, col.g, col.b, i);
                        yield return new WaitForSeconds(0.01f);
                    }
                }

                Instant();
            }

            void Instant()
            {
                _sr.enabled = visible;
                _bc.enabled = visible;
                _rb.gravityScale = 0f;
                Color col = _sr.color;
                _sr.color = new Color(col.r, col.g, col.b, visible ? 1f : 0f);
            }

            if (fade) StartCoroutine(Fade());
            else Instant();
        }
        
        private void Log(object o)
        {
            Modding.Logger.Log("[Zemer2] " + o);
        }

        private void OnDestroy()
        {
            On.HealthManager.TakeDamage -= HealthManager_TakeDamage;
            On.EnemyDreamnailReaction.RecieveDreamImpact -= OnReceiveDreamImpact;
        }
    }
}
