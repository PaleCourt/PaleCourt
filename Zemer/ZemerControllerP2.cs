using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FiveKnights.BossManagement;
using FiveKnights.Ogrim;
using FrogCore.Ext;
using HutongGames.PlayMaker.Actions;
using SFCore.Utils;
using UnityEngine;
using Logger = Modding.Logger;
using Random = System.Random;

namespace FiveKnights.Zemer
{
    public class ZemerControllerP2 : MonoBehaviour
    {
        private HealthManager _hm;
        private BoxCollider2D _bc;
        private SpriteRenderer _sr;
        private EnemyDreamnailReaction _dnailReac;
        private MusicPlayer _ap;
        private MusicPlayer _voice;
        private GameObject _dd;
        private GameObject[] traitorSlam;
        private int traitorSlamIndex;
        private GameObject _dnailEff;
        private Animator _anim;
        private Rigidbody2D _rb;
        private Random _rand;
        private EnemyHitEffectsUninfected _hitEffects;
        private EnemyDeathEffectsUninfected _deathEff;
        private GameObject _target;
        private string[] _commonAtt;

        private float GroundY = (CustomWP.boss == CustomWP.Boss.All) ? 9.4f : 29.4f;
        private readonly float LeftX = (OWArenaFinder.IsInOverWorld) ? 240.1f : (CustomWP.boss == CustomWP.Boss.All) ? 61.0f : 11.2f;
        private readonly float RightX = (OWArenaFinder.IsInOverWorld) ? 273.9f : (CustomWP.boss == CustomWP.Boss.All) ? 91.0f : 45.7f;

        private const int Phase2HP = 1500;
        private const int Phase3HP = 1000;

        private const float TurnDelay = 0.05f;
        private const float IdleDelay = 0.38f;
        private const float DashDelay = 0.18f;
        private const float MIDDLE = 29f;

        private const float SmallPillarSpd = 23.5f;
        private const float Att1CompAnticTime = 0.25f;

        private PlayMakerFSM _pvFsm;
        private Coroutine _counterRoutine;

        private bool _blockedHit;
        private bool _countering;

        private Func<IEnumerator> _lastAtt;
        private bool isHit;
        private int _spinType;

        public bool DoPhase;

        private readonly string[] _dnailDial =
        {
            "ZEM_DREAM_1",
            "ZEM_DREAM_2",
            "ZEM_DREAM_3"
        };

        private void Awake()
        {
            GroundY = OWArenaFinder.IsInOverWorld ? 108.8f : GroundY;
            
            OnDestroy();

            On.HealthManager.TakeDamage += HealthManager_TakeDamage;
            On.EnemyDreamnailReaction.RecieveDreamImpact += OnReceiveDreamImpact;

            _hm = GetComponent<HealthManager>();
            _anim = GetComponent<Animator>();

            _bc = GetComponent<BoxCollider2D>();

            _rb = GetComponent<Rigidbody2D>();
            _rb.isKinematic = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            _sr = GetComponent<SpriteRenderer>();

            GetComponent<DamageHero>().damageDealt = 1;

            _dd = FiveKnights.preloadedGO["WD"];

            _dnailEff = _dd.GetComponent<EnemyDreamnailReaction>().GetAttr<EnemyDreamnailReaction, GameObject>("dreamImpactPrefab");

            _rand = new Random();

            _dnailReac = GetComponent<EnemyDreamnailReaction>();
            _dnailReac.enabled = true;
            _dnailReac.SetConvoTitle(_dnailDial[_rand.Next(_dnailDial.Length)]);

            _hitEffects = gameObject.GetComponent<EnemyHitEffectsUninfected>();
            _hitEffects.enabled = true;

            _pvFsm = FiveKnights.preloadedGO["PV"].LocateMyFSM("Control");

            AssignFields();
        }

        private IEnumerator Start()
        {
            _hm.hp = Phase2HP;
            _spinType = 1;

            _deathEff = _dd.GetComponent<EnemyDeathEffectsUninfected>();
            _deathEff.SetJournalEntry(FiveKnights.journalentries["Zemer"]);
            _target = HeroController.instance.gameObject;
            
            yield return EndPhase1();
            StartCoroutine(Attacks());
        }

        private void Update()
        {
            if (_isKnockingOut)
            {
                _rb.isKinematic = false;
                _bc.enabled = false;
            }
            if (_bc == null || !_bc.enabled) return;

            if (transform.GetPositionX() > RightX - 1.3f && _rb.velocity.x > 0f)
            {
                _rb.velocity = new Vector2(0f, _rb.velocity.y);
            }

            if (transform.GetPositionX() < LeftX + 1.3f && _rb.velocity.x < 0f)
            {
                _rb.velocity = new Vector2(0f, _rb.velocity.y);
            }
        }

        private IEnumerator Attacks()
        {
            int counterCount = 0;
            Dictionary<Func<IEnumerator>, int> rep = new Dictionary<Func<IEnumerator>, int>
            {
                [Dash] = 0,
                [Attack1Base] = 0,
                [AerialAttack] = 0,
                [NailLaunch] = 0,
                [DoubleFancy] = 0,
                [SweepDash] = 0,
                [ZemerSlam] = 0,
            };
           
            while (true)
            {
                Log("[Waiting to start calculation]");
                _anim.Play("ZIdle");
                isHit = false;
                yield return new WaitSecWhile(() => !isHit, IdleDelay);
                Log("[End of Wait]");
                
                Vector2 posZem = transform.position;
                Vector2 posH = _target.transform.position;

                if (posH.y > GroundY + 9f && (posH.x <= LeftX || posH.x >= RightX))
                {
                    yield return SpinAttack();
                }
                else if (FastApproximately(posZem.x, posH.x, 5f))
                {
                    int r = _rand.Next(0, 4);
                    if (r == 0 && counterCount < 2)
                    {
                        counterCount++;
                        Log("Doing Counter");
                        ZemerCounter();
                        _countering = true;
                        yield return new WaitWhile(() => _countering);
                        Log("Done Counter");
                    }
                    else if (r < 2)
                    {
                        Log("Doing Dodge");
                        counterCount = 0;
                        yield return Dodge();
                        Log("Done Dodge");
                    }
                    else
                    {
                        counterCount = 0;
                    }
                }

                List<Func<IEnumerator>> attLst = new List<Func<IEnumerator>>
                {
                    Dash, Attack1Base, NailLaunch, 
                    AerialAttack, DoubleFancy, SweepDash, ZemerSlam
                };
                
                Func<IEnumerator> currAtt = attLst[_rand.Next(0, attLst.Count)];
                while (rep[currAtt] >= 2)
                {
                    attLst.Remove(currAtt);
                    rep[currAtt] = 0;
                    currAtt = attLst[_rand.Next(0, attLst.Count)];
                }

                rep[currAtt]++;
                Log("Doing " + currAtt.Method.Name);
                yield return currAtt();
                Log("Doing " + currAtt.Method.Name);

                if (currAtt == Attack1Base)
                {
                    List<Func<IEnumerator>> lst2 = new List<Func<IEnumerator>>
                    {
                        Attack1Complete, FancyAttack
                    };
                    currAtt = lst2[_rand.Next(0, lst2.Count)];
                    Log("Doing " + currAtt.Method.Name);
                    yield return currAtt();
                    Log("Done " + currAtt.Method.Name);
                    
                    if (currAtt == FancyAttack && _rand.Next(0,3) < 2)
                    {
                        Log("Doing Special Fancy Attack");
                        yield return Dodge();
                        yield return FancyAttack();
                        yield return Dash();
                        Log("Done Special Fancy Attack");
                    }
                }

                Log("[Done Setting Attacks]");

                _anim.Play("ZIdle");

                Log("[Restarting Calculations]");

                yield return new WaitForEndOfFrame();
            }
            // ReSharper disable once IteratorNeverReturns
        }
        
        private IEnumerator AerialAttack()
        {
            IEnumerator Attack()
            {
                if (!IsFacingPlayer())
                    yield return Turn();

                transform.Find("BladeAerialShadow").gameObject.SetActive(true);

                float xVel = FaceHero() * -1f;

                _anim.Play("ZAerial2");
                
                PlayAudioClip("ZAudAtt" + _rand.Next(1,7), _voice);
                
                yield return _anim.WaitToFrame(8);

                _rb.velocity = new Vector2(xVel * 35f, 18f);
                _rb.gravityScale = 1.3f;
                _rb.isKinematic = false;

                yield return new WaitForSeconds(0.1f);
                yield return _anim.WaitToFrame(10);
                PlayAudioClip("AudBigSlash2",_ap,0.85f,1.15f);
                yield return _anim.WaitToFrame(13);
                PlayAudioClip("AudBigSlash2",_ap,0.85f,1.15f);
                yield return new WaitWhile(() => transform.position.y > GroundY);

                _rb.velocity = Vector2.zero;
                _rb.gravityScale = 0f;
                _rb.isKinematic = true;

                transform.position = new Vector2(transform.position.x, GroundY);

                yield return _anim.PlayToEnd();

                _anim.Play("ZIdle");

                yield return new WaitForSeconds(0.2f);
            }

            _lastAtt = AerialAttack;
            
            yield return Attack();
        }

        private IEnumerator NailLaunch()
        {
            float dir = 0f;
            bool doSlam = false;

            IEnumerator Throw()
            {
                Vector2 hero = _target.transform.position;
                Vector2 zem = gameObject.transform.position;
                if (FastApproximately(hero.x, zem.x, 10f) && hero.y < GroundY + 1.5f)
                {
                    Log("Failed NailLaunch");
                    yield break;
                }

                dir = FaceHero();
                float rot;
                _anim.Play("ZThrow1");
                PlayAudioClip("ZAudAtt" + _rand.Next(1,7), _voice);
                yield return _anim.WaitToFrame(2);
                _anim.enabled = false;
                yield return new WaitForSeconds(0.2f);
                _anim.enabled = true;
                yield return _anim.WaitToFrame(4);
                hero = _target.transform.position;
                zem = gameObject.transform.position;
                dir = FaceHero();
                rot = Mathf.Atan((hero.y - zem.y) / (hero.x - zem.x));
                float rotVel = dir > 0 ? rot + 180 * Mathf.Deg2Rad : rot;
                GameObject arm = transform.Find("NailHand").gameObject;
                GameObject nailPar = Instantiate(transform.Find("ZNailB").gameObject);
                Rigidbody2D parRB = nailPar.GetComponent<Rigidbody2D>();
                arm.transform.SetRotation2D(rot * Mathf.Rad2Deg);
                nailPar.transform.SetRotation2D(rot * Mathf.Rad2Deg);
                nailPar.transform.position = transform.Find("ZNailB").position;
                nailPar.transform.localScale = new Vector3(dir * 1.6f, 1.6f, 1.6f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 5);
                nailPar.SetActive(true);
                float velmag = hero.y < GroundY + 2f ? 70f : 28f;
                parRB.velocity = new Vector2(Mathf.Cos(rotVel) * velmag, Mathf.Sin(rotVel) * velmag);
                yield return new WaitForSeconds(0.02f);
                var cc = nailPar.transform.Find("ZNailC").gameObject.AddComponent<CollisionCheck>();
                cc.Freeze = true;
                cc.OnCollide += () =>
                {
                    if (cc.Hit) PlayAudioClip("AudLand",_ap);
                };
                yield return new WaitWhile(() => _anim.IsPlaying());
                bool isTooHigh = nailPar.transform.position.y > GroundY + 1f;
                yield return (!isTooHigh ? LaunchSide(nailPar) : LaunchUp(rot, nailPar));
            }

            //In phase 1, have her just pick up the sword using reverse slam animation but in later phases add combos after attack
            IEnumerator LaunchUp(float rot, GameObject nail)
            {
                float rotVel = dir > 0 ? rot + 180 * Mathf.Deg2Rad : rot;
                GameObject col = nail.transform.Find("ZNailC").gameObject;
                CollisionCheck cc = col.GetComponent<CollisionCheck>() ?? col.AddComponent<CollisionCheck>();
                Rigidbody2D rb = nail.GetComponent<Rigidbody2D>();

                _anim.speed *= 2f;
                _anim.Play("ZThrow2");
                _rb.velocity = new Vector2(-dir * 30f, 0f);
                yield return null;
                //Spring(false, transform.position + new Vector3(-dir * 2.7f, 0f, 0f));
                yield return new WaitWhile(() => _anim.IsPlaying());
                ToggleZemer(false);
                _rb.velocity = Vector2.zero;
                cc.Hit = rb.velocity == Vector2.zero;

                yield return new WaitWhile(() => nail.transform.position.y < 17f && !cc.Hit);

                transform.position = nail.transform.position + new Vector3(5f * Mathf.Cos(rotVel), 5f * Mathf.Sin(rotVel), 0f);
                _anim.Play("ZThrow3Air", -1, 0f);
                ToggleZemer(true);
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
                PlayAudioClip("ZAudAtt" + _rand.Next(1,7), _voice);
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
                float oldDir = dir;
                dir = FaceHero();
                rot = Mathf.Atan((hero.y - zem.y) / (hero.x - zem.x));
                rotVel = dir > 0 ? rot + Mathf.PI : rot;
                float offset = FastApproximately(dir, oldDir, 0.1f) ? 0f : 180f;
                nail.transform.SetRotation2D(rot * Mathf.Rad2Deg + offset);
                if (hero.y > GroundY + 1.5f)
                {
                    doSlam = true;
                    rot = Mathf.Atan((7.4f - zem.y) / (MIDDLE - zem.x));
                    rotVel = dir > 0 ? rot + Mathf.PI : rot;
                    offset = FastApproximately(dir, oldDir, 0.1f) ? 0f : 180f;
                    nail.transform.SetRotation2D(rot * Mathf.Rad2Deg + offset);
                }

                rb.velocity = new Vector2(Mathf.Cos(rotVel) * 70f, Mathf.Sin(rotVel) * 70f);
                nail.transform.position = transform.position;
                cc.Freeze = false;
                cc.OnCollide += () =>
                {
                    GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
                    nail.GetComponent<SpriteRenderer>().enabled = false;
                    nail.transform.Find("ZNailN").GetComponent<SpriteRenderer>().enabled = true;
                };
                yield return new WaitForSeconds(0.02f);
                cc.Freeze = true;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 8);
                rotVel = dir > 0 ? 10f : -45f;
                _rb.velocity = new Vector2(25f * Mathf.Cos(rotVel), 25f * Mathf.Sin(rotVel));
                yield return new WaitForSeconds(0.1f);
                //Spring(false, transform.position + new Vector3(3.5f * Mathf.Cos(rotVel), 3.5f * Mathf.Sin(rotVel),0f));
                yield return new WaitWhile(() => _anim.IsPlaying());
                ToggleZemer(false, true);
                yield return new WaitForSeconds(0.1f);
                _rb.velocity = Vector2.zero;
                transform.position = new Vector3(80f, GroundY);
                yield return new WaitWhile(() => !cc.Hit);
                yield return (LaunchSide(nail, false));
            }

            IEnumerator LaunchSide(GameObject nail, bool leave = true)
            {
                GameObject col = nail.transform.Find("ZNailC").gameObject;
                Rigidbody2D rbNail = nail.GetComponent<Rigidbody2D>();
                CollisionCheck cc = col.GetComponent<CollisionCheck>();
                if (cc == null) cc = col.AddComponent<CollisionCheck>();
                if (leave)
                {
                    _anim.Play("ZThrow2",-1,0f);
                    _rb.velocity = new Vector2(-dir * 30f, 0f);
                    cc.Hit = rbNail.velocity == Vector2.zero;
                    yield return null;
                    yield return new WaitWhile(() => !cc.Hit && _anim.IsPlaying());
                    ToggleZemer(false, true);
                    //Spring(false, transform.position + new Vector3(-dir * 3.5f, 0f, 0f));
                    yield return new WaitWhile(() => !cc.Hit);
                    PlayAudioClip("AudLand",_ap);
                    Log("Stop nail");
                    yield return new WaitForSeconds(0.02f);
                    nail.transform.Find("ZNailN").GetComponent<SpriteRenderer>().enabled = true;
                    rbNail.velocity = Vector2.zero;
                    yield return new WaitForSeconds(0.75f);
                }

                Vector2 zem = transform.position;
                Vector2 nl = nail.transform.Find("Point").position;
                Vector3 zemSc = transform.localScale;
                
                if (nl.x < MIDDLE)
                {
                    transform.localScale = new Vector3(Mathf.Abs(zemSc.x), zemSc.y, zemSc.z);
                    transform.position = new Vector2(nl.x + 5f, zem.y);
                    Spring(true, transform.position);
                    yield return new WaitForSeconds(0.15f);
                    ToggleZemer(true);
                    _rb.velocity = new Vector2(-40f, 0f);
                    _anim.Play("ZThrow3Gnd");
                    yield return null;
                    yield return new WaitWhile
                    (
                        () =>
                            _anim.GetCurrentFrame() < 1 && !FastApproximately(transform.position.x, nl.x, 0.3f)
                    );
                    _anim.enabled = false;
                    yield return new WaitWhile
                    (
                        () =>
                            !FastApproximately(transform.position.x, nl.x, 0.3f) && transform.position.x > LeftX + 1.3f
                    );
                    _anim.enabled = true;
                    yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                    _rb.velocity = new Vector2(0f, 0f);
                    Destroy(nail);
                }
                else
                {
                    transform.localScale = new Vector3(-Mathf.Abs(zemSc.x), zemSc.y, zemSc.z);
                    transform.position = new Vector2(nl.x - 5f, zem.y);
                    Spring(true, transform.position);
                    yield return new WaitForSeconds(0.15f);
                    ToggleZemer(true);
                    _rb.velocity = new Vector2(40f, 0f);
                    _anim.Play("ZThrow3Gnd");
                    yield return null;
                    yield return new WaitWhile
                    (
                        () =>
                            _anim.GetCurrentFrame() < 1 && !FastApproximately(transform.position.x, nl.x, 0.3f)
                    );
                    _anim.enabled = false;
                    yield return new WaitWhile
                    (
                        () =>
                            !FastApproximately(transform.position.x, nl.x, 0.3f) && transform.position.x < RightX - 1.3f
                    );
                    _anim.enabled = true;
                    yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                    _rb.velocity = new Vector2(0f, 0f);
                    Destroy(nail);
                }

                if (_hm.hp <= Phase3HP && _rand.Next(0, 5) < 3)
                {
                    _anim.PlayAt("Z2Crawl", 1);
                    yield return null;
                    yield return new WaitWhile(() => _anim.IsPlaying());
                    float sig = Mathf.Sign(transform.localScale.x);
                    yield return RageCombo(sig, true, _spinType);
                    yield break;
                }

                yield return new WaitWhile(() => _anim.IsPlaying());
                if (doSlam) yield return ZemerSlam();
            }

            _lastAtt = NailLaunch;
            yield return (Throw());
        }

        private IEnumerator ZemerSlam()
        {
            IEnumerator Slam()
            {
                yield return _anim.PlayToFrame("ZSlam", 3);
                _anim.enabled = false;

                yield return new WaitForSeconds(0.25f);

                _anim.enabled = true;
                _anim.speed *= 5.5f;

                yield return _anim.PlayToEnd();

                _anim.speed /= 5.5f;

                SpawnShockwaves(2f, 50f, 1, transform.position);

                GameCameras.instance.cameraShakeFSM.SendEvent("BigShake");
            }

            _lastAtt = ZemerSlam;

            yield return (Slam());
        }

        private IEnumerator Attack1Complete()
        {
            IEnumerator Attack1Complete()
            {
                _anim.Play("ZAtt1");
                float xVel = FaceHero() * -1;

                _anim.enabled = false;

                yield return new WaitForSeconds(Att1CompAnticTime);

                _anim.enabled = true;

                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);

                PlayAudioClip("AudBigSlash", _ap,0.85f, 1.15f);

                _rb.velocity = new Vector2(40f * xVel, 0f);

                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);

                _rb.velocity = Vector2.zero;

                yield return new WaitWhile(() => _anim.IsPlaying());

                _anim.Play("ZIdle");
            }

            if (_lastAtt != Attack1Base)
            {
                yield break;
            }

            _lastAtt = this.Attack1Complete;
            yield return (Attack1Complete());
        }

        private IEnumerator Attack1Base()
        {
            IEnumerator Attack1Base()
            {
                if (!IsFacingPlayer())
                {
                    yield return Turn();
                }

                float xVel = FaceHero() * -1f;
                PlayAudioClip("ZAudAtt" + _rand.Next(1,7), _voice);
                yield return _anim.PlayToFrame("ZAtt1Intro", 1);
                
                _anim.enabled = false;

                yield return new WaitForSeconds(0.2f);

                _anim.enabled = true;

                yield return _anim.WaitToFrame(2);

                PlayAudioClip("Slash",_ap, 0.85f, 1.15f);

                yield return _anim.WaitToFrame(6);

                // If player has gone behind, do backward slash
                if ((int) -xVel != FaceHero(true))
                {
                    yield return RageCombo(-xVel, _rand.Next(0, 5) >= 3, _spinType);
                    _lastAtt = null;
                    yield break;
                }

                yield return _anim.PlayToEnd();
                
                _rb.velocity = new Vector2(23f * xVel, 0f);

                _anim.speed = 1.5f;
                
                while ((xVel > 0 && transform.position.x < RightX - 10f) ||
                       (xVel < 0 && transform.position.x > LeftX + 10f))
                {
                    yield return _anim.PlayToEndWithActions("ZAtt1Loop",
                        (0, ()=>PlayAudioClip("Slash", _ap,0.85f, 1.15f))
                    );
                }

                _anim.speed = 1f;

                _anim.Play("ZAtt1End");
                _rb.velocity = Vector2.zero;
                
                yield return _anim.PlayToEnd();
            }

            _lastAtt = this.Attack1Base;

            yield return (Attack1Base());
        }

        //Only in phase 3
        private IEnumerator DoubleFancy()
        {
            IEnumerator Leave(float dir)
            {
                _rb.velocity = new Vector2(-dir * 40f, 40f);

                _anim.Play("ZThrow2B");

                yield return _anim.PlayBlocking("ZThrow2B");

                ToggleZemer(false);

                _rb.velocity = Vector2.zero;
            }

            IEnumerator BackIn(float dir)
            {
                float x = dir > 0 ? LeftX + 9f : RightX - 9f;
                float tarX = dir > 0 ? LeftX + 4f : RightX - 4f;

                transform.position = new Vector3(x, GroundY + 6f);
                Vector3 tmp = transform.localScale;
                transform.localScale = new Vector3(dir * Mathf.Abs(tmp.x), tmp.y, tmp.z);
                
                Spring(true, transform.position, 1.8f);

                yield return new WaitForSeconds(0.15f);

                ToggleZemer(true);

                var diff = new Vector2(x - tarX, transform.position.y - GroundY - 0.95f);

                float rot = Mathf.Atan(diff.y / diff.x);

                rot = tarX < MIDDLE ? rot + Mathf.PI : rot;

                _rb.velocity = new Vector2(55f * Mathf.Cos(rot), 55f * Mathf.Sin(rot));

                yield return _anim.PlayBlockingWhile("Z1ZipIn", () => transform.position.y > GroundY - 0.95f);

                transform.position = new Vector3(transform.position.x, GroundY - 0.95f);

                _rb.velocity = new Vector2(-dir * 40f, 0f);

                _anim.Play("Z2Crawl");

                transform.position = new Vector3(transform.position.x, GroundY);

                yield return new WaitForSeconds(0.05f);

                _rb.velocity = new Vector2(0f, 0f);

                yield return _anim.WaitToFrame(1);

                _anim.Play("ZIdle");

            }

            IEnumerator FancyOne()
            {
                if (!IsFacingPlayer())
                    yield return Turn();

                float dir = FaceHero();
                
                PlayAudioClip("ZAudAtt" + _rand.Next(1,7), _voice);
                yield return _anim.PlayToEndWithActions
                (
                    "ZAtt2",
                    (0, () => PlayAudioClip("AudBasicSlash1",_ap)),
                    (2, () => PlayAudioClip("AudBasicSlash2",_ap)),
                    (6, () => PlayAudioClip("AudBigSlash2",_ap)),
                    (7, () => SpawnPillar(dir, Vector2.one, 20f))
                );

                _anim.Play("ZIdle");

                yield return new WaitForSeconds(0.2f);
            }
            
            IEnumerator DashOtherSide()
            {
                float dir = Mathf.Sign(transform.localScale.x);
                transform.Find("HyperCut").gameObject.SetActive(false);
                _anim.Play("ZDash");
                transform.position = new Vector3(transform.position.x, GroundY-0.3f, transform.position.z);

                
                yield return _anim.WaitToFrame(4);
                
                _anim.enabled = false;
                
                yield return new WaitForSeconds(DashDelay-0.8f);
                PlayAudioClip("ZAudHoriz",_voice);
                
                _anim.enabled = true;
                
                yield return _anim.WaitToFrame(5);
                
                PlayAudioClip("AudDashIntro",_ap);
                
                yield return _anim.WaitToFrame(6);

                _anim.enabled = false;
                _rb.velocity = new Vector2(-dir * 60f, 0f);
                
                PlayAudioClip("AudDash",_ap);
                
                if (-dir > 0)
                {
                    yield return new WaitWhile(() => 
                        transform.GetPositionX() < RightX - 12f);
                }
                else
                {
                    yield return new WaitWhile(() => 
                        transform.GetPositionX() > LeftX + 12f);
                }
                _anim.enabled = true;
                yield return _anim.WaitToFrame(7);
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("ZIdle");
                transform.position = new Vector3(transform.position.x, GroundY, transform.position.z);
            }

            float heroFromMid = Mathf.Sign(_target.transform.position.x - MIDDLE);

            yield return Leave(FaceHero());
            yield return BackIn(heroFromMid);
            yield return FancyOne();
            yield return DashOtherSide();
            yield return FancyOne();
        }

        private IEnumerator SweepDash()
        {
            IEnumerator Leave()
            {
                float dir = FaceHero();

                _rb.velocity = new Vector2(-dir * 40f, 40f);

                yield return _anim.PlayBlocking("ZThrow2B");

                ToggleZemer(false);

                _rb.velocity = Vector2.zero;

                yield return FlyStrike();
            }

            IEnumerator FlyStrike()
            {
                float heroX = _target.transform.position.x;
                float hVelX = _target.GetComponent<Rigidbody2D>().velocity.x;

                const float offset = 1.5f;

                int dir = FaceHero();

                float target_pos_x = heroX;

                // Moving left, close to right wall
                if (hVelX > 0 && heroX.Within(RightX, offset))
                    target_pos_x += offset;
                // Moving right, close to left wall
                else if (hVelX <= 0 && heroX.Within(LeftX, offset))
                    target_pos_x -= offset;
                // Neither
                else
                    target_pos_x = dir > 0 ? heroX - 3f : heroX + 3f;

                target_pos_x = target_pos_x < MIDDLE
                    ? target_pos_x + 5
                    : target_pos_x - 5;

                transform.position = new Vector3(target_pos_x, GroundY + 9.5f);

                Spring(true, transform.position, 1.4f);

                yield return new WaitForSeconds(0.16f);

                ToggleZemer(true);

                dir = FaceHero();

                hVelX = _target.GetComponent<Rigidbody2D>().velocity.x;

                if (hVelX > 0 && heroX + offset < RightX) heroX += offset;
                else if (hVelX <= 0 && heroX - offset > LeftX) heroX -= offset;
                else heroX = dir > 0 ? heroX - 3f : heroX + 3f;

                _anim.Play("Z4AirSweep");
                PlayAudioClip("ZAudAtt1", _voice);
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                PlayAudioClip("AudDash",_ap);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);

                if (FaceHero(true) == dir)
                {
                    heroX = _target.transform.position.x;
                    hVelX = _target.GetComponent<Rigidbody2D>().velocity.x;
                    if (hVelX > 0 && heroX + offset < RightX) heroX += offset;
                    else if (hVelX <= 0 && heroX - offset > LeftX) heroX -= offset;
                    else heroX = dir > 0 ? heroX - 3f : heroX + 3f;
                }

                if (FastApproximately(heroX, transform.GetPositionX(), 2.5f))
                {
                    heroX = dir > 0 ? heroX - 3f : heroX + 3f;
                }

                var diff = new Vector2(target_pos_x - heroX, transform.position.y - GroundY - 0.95f);

                float rot = Mathf.Atan(diff.y / diff.x);

                rot = Mathf.Sin(rot) > 0 ? rot + Mathf.PI : rot;

                _rb.velocity = new Vector2(65f * Mathf.Cos(rot), 65f * Mathf.Sin(rot));

                yield return new WaitWhile(() => transform.position.y > GroundY + 2.5f && _anim.GetCurrentFrame() < 4);

                _anim.enabled = false;

                yield return new WaitWhile(() => transform.position.y > GroundY + 2.5f);

                _anim.enabled = true;

                yield return new WaitWhile(() => transform.position.y > GroundY - 0.3 && _anim.GetCurrentFrame() < 6);

                _anim.enabled = false;

                yield return new WaitWhile(() => transform.position.y > GroundY - 0.3f);

                _anim.enabled = true;

                transform.position = new Vector3(transform.position.x, GroundY - 0.3f);

                _rb.velocity = Vector2.zero;
                PlayAudioClip("AudLand",_ap);

                yield return new WaitWhile(() => _anim.IsPlaying());
                yield return new WaitForSeconds(0.25f);
                
                yield return (_rand.Next(0,5) < 2 ? LandSlide() : Dash());
            }
            
            IEnumerator LandSlide()
            {
                float signX = Mathf.Sign(gameObject.transform.GetPositionX() - MIDDLE);
                Vector3 pScale = gameObject.transform.localScale;
                gameObject.transform.localScale = new Vector3(Mathf.Abs(pScale.x) * signX, pScale.y, 1f);
                _anim.Play("Z5LandSlide");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                _rb.velocity = new Vector2(-signX * 45f, 0f);
                _anim.enabled = false; 
                yield return null;
                yield return new WaitWhile
                (
                    () => !FastApproximately(_rb.velocity.x, 0f, 0.1f) && !FastApproximately(transform.position.x, MIDDLE, 0.25f)
                );
                _anim.enabled = true;
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                yield return new WaitWhile(() => _anim.IsPlaying());
                yield return  (LaserNuts(1, _spinType));
            }

            IEnumerator LaserNuts(int i, int type)
            {
                if (i == 1)
                {
                    _anim.Play("Z6LaserSpin", -1, 0f);
                    yield return null;
                    _anim.enabled = false;
                    yield return new WaitForSeconds(0.15f);
                    PlayAudioClip("ZAudLaser", _voice);
                    _anim.enabled = true;
                }
                else _anim.PlayAt("Z6LaserSpin", 1);

                transform.position = new Vector3(transform.position.x, GroundY - 1.2f);
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);

                if (type == 3)
                {
                    GameObject wav = Instantiate(FiveKnights.preloadedGO["WaveShad"]);
                    wav.GetComponent<SpriteRenderer>().material = FiveKnights.Materials["TestDist"];
                    wav.transform.position = new Vector3(MIDDLE, GroundY);
                    wav.SetActive(true);
                    wav.AddComponent<WaveIncrease>();
                }

                SpawnSlashes(type);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 9);

                if (i > 3)
                {
                    yield return _anim.PlayToEnd();

                    transform.position = new Vector3(transform.position.x, GroundY);

                    FaceHero();

                    _anim.Play("ZIdle");
                    _anim.enabled = false;

                    yield return new WaitForSeconds(0.1f);
                    _anim.enabled = true;

                    yield break;
                }
                
                yield return LaserNuts(++i, type);
            }
            
            IEnumerator Dash()
            {
                float dir = FaceHero();

                transform.Find("HyperCut").gameObject.SetActive(false);

                PlayAudioClip("AudDash",_ap);

                _rb.velocity = new Vector2(-dir * 60f, 0f);

                yield return _anim.PlayToFrameAt("ZDash", 4, 9);

                _rb.velocity = Vector2.zero;

                yield return _anim.PlayToEnd();

                _anim.Play("ZIdle");

            }

            yield return (Leave());
        }

        private IEnumerator RageCombo(float dir, bool dashes, int spinType)
        {
            IEnumerator Swing()
            {
                _lastAtt = null;

                _anim.Play("Z3Swing");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                PlayAudioClip("ZAudAtt" + _rand.Next(1,7), _voice);
                SpawnPillar(-dir, new Vector2(1.6f, 0.5f), SmallPillarSpd);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 5);
                _rb.velocity = new Vector2(dir * 40f, 40f);
                yield return new WaitForSeconds(0.08f);

                _bc.enabled = false;

                //Spring(false, transform.position + new Vector3(dir * 4f, 4f,0f),1.5f);
                yield return new WaitWhile(() => _anim.IsPlaying());
                ToggleZemer(false, true);
                _rb.velocity = Vector2.zero;
                yield return new WaitForSeconds(0.1f); //0.15f
                yield return (FlyStrike());
            }

            IEnumerator FlyStrike()
            {
                float heroX = _target.transform.position.x;
                float hVelX = _target.GetComponent<Rigidbody2D>().velocity.x;
                const float offset = 1.5f;
                float dir = FaceHero();
                if (hVelX > 0 && heroX + offset < RightX) heroX += offset;
                else if (hVelX <= 0 && heroX - offset > LeftX) heroX -= offset;
                else heroX = dir > 0 ? heroX - 3f : heroX + 3f;
                float x = heroX < MIDDLE ? heroX + 5 : heroX - 5;
                transform.position = new Vector3(x, GroundY + 9.5f);
                Spring(true, transform.position, 1.4f);
                yield return new WaitForSeconds(0.16f);
                ToggleZemer(true);

                heroX = _target.transform.position.x;
                dir = FaceHero();
                hVelX = _target.GetComponent<Rigidbody2D>().velocity.x;
                if (hVelX > 0 && heroX + offset < RightX) heroX += offset;
                else if (hVelX <= 0 && heroX - offset > LeftX) heroX -= offset;
                else heroX = dir > 0 ? heroX - 3f : heroX + 3f;

                _anim.Play("Z4AirSweep");
                PlayAudioClip("ZAudAtt" + _rand.Next(1,7), _voice);
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                PlayAudioClip("AudDash",_ap);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
                if (FaceHero(true) == (int) dir)
                {
                    heroX = _target.transform.position.x;
                    hVelX = _target.GetComponent<Rigidbody2D>().velocity.x;
                    if (hVelX > 0 && heroX + offset < RightX) heroX += offset;
                    else if (hVelX <= 0 && heroX - offset > LeftX) heroX -= offset;
                    else heroX = dir > 0 ? heroX - 3f : heroX + 3f;
                }

                if (FastApproximately(heroX, transform.GetPositionX(), 2.5f))
                {
                    heroX = dir > 0 ? heroX - 3f : heroX + 3f;
                }

                var diff = new Vector2(x - heroX, transform.position.y - GroundY - 0.95f);
                float rot = Mathf.Atan(diff.y / diff.x);
                rot = Mathf.Sin(rot) > 0 ? rot + Mathf.PI : rot;
                _rb.velocity = new Vector2(65f * Mathf.Cos(rot), 65f * Mathf.Sin(rot));
                yield return new WaitWhile(() => transform.position.y > GroundY + 2.5f && _anim.GetCurrentFrame() < 4);
                _anim.enabled = false;
                yield return new WaitWhile(() => transform.position.y > GroundY + 2.5f);
                _anim.enabled = true;
                yield return null;
                yield return new WaitWhile(() => transform.position.y > GroundY - 0.3 && _anim.GetCurrentFrame() < 6);
                _anim.enabled = false;
                yield return new WaitWhile(() => transform.position.y > GroundY - 0.3f);
                _anim.enabled = true;
                transform.position = new Vector3(transform.position.x, GroundY - 0.3f);
                _rb.velocity = Vector2.zero;
                PlayAudioClip("AudLand",_ap);
                yield return new WaitWhile(() => _anim.IsPlaying());

                yield return (dashes ? LandSlide() : Dash()); //if far
            }

            IEnumerator Dash()
            {
                float dir = FaceHero();
                transform.Find("HyperCut").gameObject.SetActive(false);
                PlayAudioClip("ZAudHoriz",_voice);
                _anim.PlayAt("ZDash", 4);
                PlayAudioClip("AudDash",_ap);
                _rb.velocity = new Vector2(-dir * 60f, 0f);
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 9);
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("ZIdle");

            }

            IEnumerator LandSlide()
            {
                float signX = Mathf.Sign(gameObject.transform.GetPositionX() - MIDDLE);
                Vector3 pScale = gameObject.transform.localScale;
                gameObject.transform.localScale = new Vector3(Mathf.Abs(pScale.x) * signX, pScale.y, 1f);
                _anim.Play("Z5LandSlide");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                _rb.velocity = new Vector2(-signX * 45f, 0f);
                _anim.enabled = false; 
                yield return null;
                yield return new WaitWhile
                (
                    () => !FastApproximately(_rb.velocity.x, 0f, 0.1f) && !FastApproximately(transform.position.x, MIDDLE, 0.25f)
                );
                _anim.enabled = true;
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                yield return new WaitWhile(() => _anim.IsPlaying());
                yield return (LaserNuts(1, spinType));
            }

            IEnumerator LaserNuts(int i, int type)
            {
                if (i == 1)
                {
                    _anim.Play("Z6LaserSpin", -1, 0f);
                    yield return null;
                    _anim.enabled = false;
                    yield return new WaitForSeconds(0.15f);
                    PlayAudioClip("ZAudLaser", _voice);
                    _anim.enabled = true;
                }
                else _anim.PlayAt("Z6LaserSpin", 1);

                transform.position = new Vector3(transform.position.x, GroundY - 1.2f);
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);

                if (type == 3)
                {
                    GameObject wav = Instantiate(FiveKnights.preloadedGO["WaveShad"]);
                    wav.GetComponent<SpriteRenderer>().material = FiveKnights.Materials["TestDist"];
                    wav.transform.position = new Vector3(MIDDLE, GroundY);
                    wav.SetActive(true);
                    wav.AddComponent<WaveIncrease>();
                }

                SpawnSlashes(type);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 9);

                if (i > 4)
                {
                    yield return _anim.PlayToEnd();

                    transform.position = new Vector3(transform.position.x, GroundY);

                    FaceHero();

                    _anim.Play("ZIdle");
                    _anim.enabled = false;

                    yield return new WaitForSeconds(0.1f);
                    _anim.enabled = true;

                    yield break;
                }
                
                yield return LaserNuts(++i, type);
            }

            yield return (Swing());
        }

        private IEnumerator Dash()
        {
            IEnumerator Dash()
            {
                float dir = FaceHero();
                transform.Find("HyperCut").gameObject.SetActive(true);
                _anim.Play("ZDash");
                transform.position = new Vector3(transform.position.x, GroundY-0.3f, transform.position.z);

                
                yield return _anim.WaitToFrame(4);
                
                _anim.enabled = false;
                
                yield return new WaitForSeconds(DashDelay);
                PlayAudioClip("ZAudHoriz",_voice);
                
                _anim.enabled = true;
                
                yield return _anim.WaitToFrame(5);
                
                if (FastApproximately(_target.transform.position.x, transform.position.x, 5f))
                {
                    transform.position = new Vector3(transform.position.x, GroundY, transform.position.z);
                    yield return (StrikeAlternate());
                    yield break;
                }
                PlayAudioClip("AudDashIntro",_ap);
                
                yield return _anim.WaitToFrame(6);
                
                PlayAudioClip("AudDash",_ap);
                
                if (_hm.hp <= Phase3HP && _rand.Next(0, 5) < 2)
                {
                    transform.position = new Vector3(transform.position.x, GroundY, transform.position.z);
                    yield return LandSlide();
                    yield break;
                }
                //yield return _anim.WaitToFrame(7);
                //_rb.velocity = Vector2.zero;
                yield return _anim.WaitToFrame(7);

                Animator an = transform.Find("HorizSlashFX").GetComponent<Animator>();
                an.gameObject.SetActive(true);
                an.enabled = true;
                an.PlayAt("HorizFX",0);
                an.speed = 2f;
                
                yield return _anim.WaitToFrame(9);
                
                Transform par = transform.Find("HyperCut");
                GameObject slashR = Instantiate(par.Find("Hyper4R").gameObject);
                GameObject slashL = Instantiate(par.Find("Hyper4L").gameObject);
                Rigidbody2D rbR = slashR.GetComponent<Rigidbody2D>();
                Rigidbody2D rbL = slashL.GetComponent<Rigidbody2D>();
                slashR.SetActive(true);
                slashL.SetActive(true);
                slashR.transform.position = par.Find("Hyper4R").position;
                slashL.transform.position = par.Find("Hyper4L").position;
                slashR.transform.localScale = Product(par.transform.localScale, new Vector3(dir, 1f, 1f));
                slashL.transform.localScale = Product(par.transform.localScale, new Vector3(dir, 1f, 1f));
                rbL.velocity = new Vector2(-dir * 45f, 0f);
                rbR.velocity = new Vector2(dir * 45f, 0f);
                yield return new WaitWhile(() => _anim.IsPlaying());
                an.enabled = false;
                an.gameObject.SetActive(false);
                _anim.Play("ZIdle");
                transform.position = new Vector3(transform.position.x, GroundY, transform.position.z);
            }

            IEnumerator LandSlide()
            {
                float signX = Mathf.Sign(gameObject.transform.GetPositionX() - MIDDLE);
                Vector3 pScale = gameObject.transform.localScale;
                gameObject.transform.localScale = new Vector3(Mathf.Abs(pScale.x) * signX, pScale.y, 1f);
                _anim.Play("Z5LandSlide");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                _rb.velocity = new Vector2(-signX * 45f, 0f);
                _anim.enabled = false; 
                yield return null;
                yield return new WaitWhile
                (
                    () => !FastApproximately(_rb.velocity.x, 0f, 0.1f) && !FastApproximately(transform.position.x, MIDDLE, 0.25f)
                );
                _anim.enabled = true;
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                yield return new WaitWhile(() => _anim.IsPlaying());
                yield return (LaserNuts(1, _spinType));
            }

            IEnumerator LaserNuts(int i, int type)
            {
                if (i == 1)
                {
                    _anim.Play("Z6LaserSpin", -1, 0f);
                    yield return null;
                    _anim.enabled = false;
                    yield return new WaitForSeconds(0.3f);
                    PlayAudioClip("ZAudLaser", _voice);
                    _anim.enabled = true;
                }
                else _anim.PlayAt("Z6LaserSpin", 1);

                transform.position = new Vector3(transform.position.x, GroundY - 1.2f);
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);

                if (type == 3)
                {
                    GameObject wav = Instantiate(FiveKnights.preloadedGO["WaveShad"]);
                    wav.GetComponent<SpriteRenderer>().material = FiveKnights.Materials["TestDist"];
                    wav.transform.position = new Vector3(MIDDLE, GroundY);
                    wav.SetActive(true);
                    wav.AddComponent<WaveIncrease>();
                }

                SpawnSlashes(type);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 9);

                if (i > 2)
                {
                    yield return new WaitWhile(() => _anim.IsPlaying());
                    transform.position = new Vector3(transform.position.x, GroundY);
                    FaceHero();
                    _anim.Play("ZIdle");
                    yield break;
                }

                yield return (LaserNuts(++i, type));
            }

            IEnumerator StrikeAlternate()
            {
                FaceHero();

                _anim.Play("DashCounter");
                PlayAudioClip("Slash",_ap, 0.85f, 1.15f);
                yield return null;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("ZIdle");
            }

            _lastAtt = this.Dash;
            yield return (Dash());
        }

        private IEnumerator Dodge()
        {
            IEnumerator Dodge()
            {
                if (!IsFacingPlayer())
                {
                    yield return Turn();
                }

                float xVel = FaceHero() * -1f;

                _anim.Play("ZDodge");
                PlayAudioClip("ZAudAtt" + _rand.Next(2,5), _voice);
                _rb.velocity = new Vector2(-xVel * 40f, 0f);
                yield return null;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _rb.velocity = new Vector2(0f, 0f);
                _anim.Play("ZIdle");
            }

            _lastAtt = this.Dodge;
            yield return (Dodge());
        }

        private void ZemerCounter()
        {
            float dir = 0f;

            IEnumerator CounterAntic()
            {
                if (!IsFacingPlayer())
                    yield return Turn();

                dir = FaceHero() * -1f;
                _anim.Play("ZCInit");
                PlayAudioClip("ZAudCounter",_voice);
                yield return new WaitWhile(() => _anim.IsPlaying());

                _counterRoutine = StartCoroutine(Countering());
            }

            IEnumerator Countering()
            {
                Parryable.ParryFlag = true;
                _hm.IsInvincible = true;
                _anim.Play("ZCIdle");
                _blockedHit = false;
                On.HealthManager.Hit -= OnBlockedHit;
                On.HealthManager.Hit += OnBlockedHit;
                PlayAudioClip("Counter",_ap);
                StartCoroutine(FlashWhite());


                Vector2 fxPos = transform.position + Vector3.right * (1.7f * dir) + Vector3.up * 0.8f;
                Quaternion fxRot = Quaternion.Euler(0, 0, dir * 80f);
                GameObject counterFx = Instantiate(FiveKnights.preloadedGO["CounterFX"], fxPos, fxRot);
                counterFx.SetActive(true);
                yield return new WaitForSeconds(0.35f);
                On.HealthManager.Hit -= OnBlockedHit;
                Parryable.ParryFlag = false;
                _counterRoutine = StartCoroutine(CounterEnd());
            }

            IEnumerator CounterEnd()
            {
                _hm.IsInvincible = false;
                _anim.Play("ZCCancel");
                yield return new WaitWhile(() => _anim.IsPlaying());
                _countering = false;
            }

            _lastAtt = null;
            _counterRoutine = StartCoroutine(CounterAntic());
        }

        private IEnumerator SpinAttack()
        {
            IEnumerator SpinAttack()
            {
                if (!IsFacingPlayer())
                    yield return Turn();

                float xVel = FaceHero() * -1f;
                float diffX = Mathf.Abs(_target.transform.GetPositionX() - transform.GetPositionX());
                float diffY = Mathf.Abs(_target.transform.GetPositionY() - transform.GetPositionY());
                float rot = Mathf.Atan(diffY / diffX);
                rot = xVel < 0 ? Mathf.PI - rot : rot;
                _anim.Play("ZSpin");
                PlayAudioClip("ZAudAtt" + _rand.Next(1,7), _voice);
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                PlayAudioClip("AudDashIntro",_ap);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
                _rb.velocity = new Vector2(60f * Mathf.Cos(rot), 60f * Mathf.Sin(rot));
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
                _anim.enabled = false;
                yield return new WaitWhile(() => transform.position.x > LeftX + 4f && transform.position.x < RightX - 4f);
                _anim.enabled = true;
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 8);
                PlayAudioClip("AudBigSlash2",_ap);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 11);
                _rb.isKinematic = false;
                _rb.gravityScale = 1.5f;
                yield return new WaitWhile(() => transform.position.y > GroundY);
                PlayAudioClip("AudLand",_ap);
                _rb.velocity = Vector2.zero;
                _rb.gravityScale = 0f;
                _rb.isKinematic = true;
                transform.position = new Vector3(transform.position.x, GroundY);
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("ZIdle");
            }

            _lastAtt = this.SpinAttack;
            yield return (SpinAttack());
        }

        private IEnumerator FancyAttack()
        {
            IEnumerator FancyAttack()
            {
                if (!IsFacingPlayer())
                    yield return Turn();

                float dir = FaceHero();

                _anim.Play("ZAtt2");
                PlayAudioClip("ZAudAtt" + _rand.Next(2,5), _voice);
                yield return null;
                PlayAudioClip("AudBasicSlash1",_ap);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                PlayAudioClip("AudBasicSlash2",_ap);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 6);
                PlayAudioClip("AudBigSlash2",_ap);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 7);
                dir = FaceHero();
                SpawnPillar(dir, Vector2.one, 15f);
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("ZIdle");
            }

            _lastAtt = this.FancyAttack;
            yield return (FancyAttack());
        }

        private IEnumerator MusicControl()
        {
            if (OWArenaFinder.IsInOverWorld)
            {
                OWBossManager.PlayMusic(FiveKnights.Clips["ZP2Intro"]);
                yield return new WaitForSecondsRealtime(14.12f);
                OWBossManager.PlayMusic(FiveKnights.Clips["ZP2Loop"]);
            }
            else
            {
                GGBossManager.Instance.PlayMusic(FiveKnights.Clips["ZP2Intro"], 1f);
                yield return new WaitForSecondsRealtime(14.12f);
                GGBossManager.Instance.PlayMusic(FiveKnights.Clips["ZP2Loop"], 1f);
            }
            
        }

        private bool _isKnockingOut;
        
        private IEnumerator EndPhase1()
        {
            float dir;
            yield return KnockedOut();
            
            IEnumerator KnockedOut()
            {
                _isKnockingOut = true;
                float knockDir = Math.Sign(transform.position.x - HeroController.instance.transform.position.x);
                dir = -FaceHero();
                _rb.gravityScale = 1.5f;
                _rb.velocity = new Vector2(knockDir * 12f, 10f);
                PlayDeathFor(gameObject);
                _anim.enabled = true;
                _anim.Play("ZKnocked");
                PlayAudioClip("ZAudP1Death",_voice);
                _anim.speed = 1f;
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                _anim.enabled = false;
                yield return new WaitWhile(() => transform.position.y > GroundY - 0.99f);
                _rb.velocity = Vector2.zero;
                _anim.enabled = true;
                _rb.gravityScale = 0f;
                transform.position = new Vector3(transform.position.x, GroundY - 1.18f);
                yield return new WaitWhile(() => _anim.IsPlaying());
                _isKnockingOut = false;

                if (DoPhase)
                {
                    _anim.enabled = false;
                    // This is breaking stuff, idk yall figure it out smh
                    // _deathEff.RecordWithoutNotes();
                    yield return new WaitForSeconds(1.75f);
                    CustomWP.wonLastFight = true;
                    // Stop music here.
                    Destroy(this);
                }
                else
                {
                    _bc.enabled = true;
                    _rb.isKinematic = true;
                    isHit = false;
                    yield return new WaitSecWhile(() => !isHit, 8f);
                    yield return Recover();
                }
            }

            IEnumerator Recover()
            {
                yield return _anim.PlayBlocking("ZRecover");

                _anim.Play("ZThrow2B");

                _rb.velocity = new Vector2(dir * 35f, 35f);

                yield return _anim.PlayToEnd();
                
                ToggleZemer(false, true);

                _rb.velocity = Vector2.zero;

                yield return new WaitForSeconds(3.5f);
                
                StartCoroutine(MusicControl());

                yield return (TransitionIn());
            }

            IEnumerator TransitionIn()
            {
                float heroX = _target.transform.GetPositionX();
                float x = heroX < MIDDLE ? heroX + _rand.Next(8, 11) : heroX - _rand.Next(8, 11);
                float xOff = heroX < MIDDLE ? -4f : 4f;
                transform.position = new Vector3(x, GroundY + 6f);
                dir = FaceHero();
                Spring(true, transform.position, 1.8f);
                yield return new WaitForSeconds(0.15f);
                ToggleZemer(true);
                var diff = new Vector2(x - heroX + xOff, transform.position.y - GroundY - 0.95f);
                float rot = Mathf.Atan(diff.y / diff.x);
                rot = heroX < MIDDLE ? rot + Mathf.PI : rot;
                _rb.velocity = new Vector2(55f * Mathf.Cos(rot), 55f * Mathf.Sin(rot));
                _anim.Play("Z1ZipIn");
                PlayAudioClip("ZAudAtt" + _rand.Next(1,7), _voice);
                yield return null;
                yield return new WaitWhile(() => transform.position.y > GroundY - 0.95f);
                transform.position = new Vector3(transform.position.x, GroundY - 0.95f);
                _rb.velocity = new Vector2(-dir * 40f, 0f);
                _anim.Play("Z2Crawl");
                yield return null;
                yield return new WaitForSeconds(0.08f);
                _rb.velocity = new Vector2(0f, 0f);
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("Z3Swing");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                SpawnPillar(-dir, new Vector2(1.4f, 0.48f), SmallPillarSpd);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 5);
                _rb.velocity = new Vector2(dir * 40f, 40f);
                yield return new WaitForSeconds(0.08f);
                //Spring(false, transform.position + new Vector3(dir * 4f, 4f,0f),1.5f);
                yield return new WaitWhile(() => _anim.IsPlaying());
                ToggleZemer(false, true);
                _rb.velocity = Vector2.zero;
                yield return new WaitForSeconds(0.1f); //0.15f
                yield return (FlyStrike());
            }

            IEnumerator FlyStrike()
            {
                float heroX = _target.transform.position.x;
                float hVelX = _target.GetComponent<Rigidbody2D>().velocity.x;
                const float offset = 1.5f;
                float dir = FaceHero();
                if (hVelX > 0 && heroX + offset < RightX) heroX += offset;
                else if (hVelX <= 0 && heroX - offset > LeftX) heroX -= offset;
                else heroX = dir > 0 ? heroX - 3f : heroX + 3f;
                float x = heroX < MIDDLE ? heroX + 5 : heroX - 5;
                transform.position = new Vector3(x, GroundY + 9.5f);
                Spring(true, transform.position, 1.4f);
                yield return new WaitForSeconds(0.16f);
                ToggleZemer(true);

                heroX = _target.transform.position.x;
                dir = FaceHero();
                hVelX = _target.GetComponent<Rigidbody2D>().velocity.x;
                if (hVelX > 0 && heroX + offset < RightX) heroX += offset;
                else if (hVelX <= 0 && heroX - offset > LeftX) heroX -= offset;
                else heroX = dir > 0 ? heroX - 3f : heroX + 3f;

                _anim.Play("Z4AirSweep");
                PlayAudioClip("ZAudAtt" + _rand.Next(1,7), _voice);
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                PlayAudioClip("AudDash",_ap);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
                if (FaceHero(true) == (int) dir)
                {
                    heroX = _target.transform.position.x;
                    hVelX = _target.GetComponent<Rigidbody2D>().velocity.x;
                    if (hVelX > 0 && heroX + offset < RightX) heroX += offset;
                    else if (hVelX <= 0 && heroX - offset > LeftX) heroX -= offset;
                    else heroX = dir > 0 ? heroX - 3f : heroX + 3f;
                }

                if (FastApproximately(heroX, transform.GetPositionX(), 2.5f))
                {
                    heroX = dir > 0 ? heroX - 3f : heroX + 3f;
                }

                var diff = new Vector2(x - heroX, transform.position.y - GroundY - 0.95f);
                float rot = Mathf.Atan(diff.y / diff.x);
                rot = Mathf.Sin(rot) > 0 ? rot + Mathf.PI : rot;
                _rb.velocity = new Vector2(65f * Mathf.Cos(rot), 65f * Mathf.Sin(rot));
                yield return new WaitWhile
                (
                    () =>
                        transform.position.y > GroundY + 2.5f && _anim.GetCurrentFrame() < 4
                );
                _anim.enabled = false;
                yield return new WaitWhile(() => transform.position.y > GroundY + 2.5f);
                _anim.enabled = true;
                yield return null;
                yield return new WaitWhile
                (
                    () =>
                        transform.position.y > GroundY - 0.3 && _anim.GetCurrentFrame() < 6
                );
                _anim.enabled = false;
                yield return new WaitWhile(() => transform.position.y > GroundY - 0.3f);
                _anim.enabled = true;
                transform.position = new Vector3(transform.position.x, GroundY - 0.3f);
                _rb.velocity = Vector2.zero;
                PlayAudioClip("AudLand",_ap);
                yield return new WaitWhile(() => _anim.IsPlaying());
                yield return new WaitForSeconds(0.25f);
                yield return (LandSlide());
            }

            IEnumerator LandSlide()
            {
                float signX = Mathf.Sign(gameObject.transform.GetPositionX() - MIDDLE);
                Vector3 pScale = gameObject.transform.localScale;
                gameObject.transform.localScale = new Vector3(Mathf.Abs(pScale.x) * signX, pScale.y, 1f);
                _anim.Play("Z5LandSlide");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                _rb.velocity = new Vector2(-signX * 45f, 0f);
                _anim.enabled = false; 
                yield return null;
                yield return new WaitWhile
                (
                    () => !FastApproximately(_rb.velocity.x, 0f, 0.1f) && !FastApproximately(transform.position.x, MIDDLE, 0.25f)
                );
                _anim.enabled = true;
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                yield return new WaitWhile(() => _anim.IsPlaying());
                yield return (LaserNuts());
            }

            IEnumerator LaserNuts()
            {
                _anim.Play("Z6LaserSpin");
                transform.position = new Vector3(transform.position.x, GroundY - 1.2f);
                yield return null;
                _anim.enabled = false;
                yield return new WaitForSeconds(0.15f);
                _anim.enabled = true;
                PlayAudioClip("ZAudLaser", _voice);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                SpawnSlashes(_spinType);
                yield return new WaitWhile(() => _anim.IsPlaying());
                transform.position = new Vector3(transform.position.x, GroundY);
                _anim.Play("ZIdle");
                FaceHero();
                yield return new WaitForSeconds(0.3f);
            }
        }

        void SpawnPillar(float dir, Vector2 size, float xSpd)
        {
            GameObject slam = traitorSlam[traitorSlamIndex++ % 2];
            Animator anim = slam.transform.Find("slash_core").GetComponent<Animator>();
            slam.SetActive(true);
            anim.enabled = true;
            anim.Play("mega_mantis_slash_big", -1, 0f);
            PlayAudioClip("TraitorPillar",_ap);
            Rigidbody2D rb = slam.GetComponent<Rigidbody2D>();
            rb.velocity = new Vector2(-dir * xSpd, 0f);
            Vector3 pos = transform.position;
            slam.transform.position = new Vector3(-dir * 2.15f + pos.x, GroundY - 3.2f, 6.4f);
            slam.transform.localScale = new Vector3(-dir * size.x, size.y, 1f);
        }

        private bool offsetAngle;
        private bool scaleOffset;
        
        private void SpawnSlashes(int type)
        {
            Log("Laser Pattern " + type);

            IEnumerator Pattern1()
            {
                Transform slash = transform.Find("NewSlash3");
                int sc = scaleOffset ? 1 : -1;
                slash.localScale = new Vector3(sc*Mathf.Abs(slash.localScale.x),slash.localScale.y,slash.localScale.z);
                slash.localPosition = new Vector3(sc < 0 ? 18.2f : -16f, 3.5f,
                    slash.localPosition.z);
                scaleOffset = !scaleOffset;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                slash.Find("1").gameObject.SetActive(true);
                slash.Find("2").gameObject.SetActive(true);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                slash.Find("4").gameObject.SetActive(true);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
                slash.Find("3").gameObject.SetActive(true);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 7);
                slash.Find("5").gameObject.SetActive(true);
                slash.Find("6").gameObject.SetActive(true);
                Animator anim = slash.Find("6").GetComponent<Animator>();
                yield return anim.PlayToEnd();
                foreach (Transform i in slash)
                {
                    i.gameObject.SetActive(false);
                }
            }
            
            IEnumerator Pattern3()
            {
                string typ = "SlashBeam2";
                GameObject origSlashA = FiveKnights.preloadedGO[typ].transform.Find("SlashA").gameObject;
                GameObject origSlashB = FiveKnights.preloadedGO[typ].transform.Find("SlashB").gameObject;
                IList<GameObject> SlashA = new List<GameObject>();
                IList<GameObject> SlashB = new List<GameObject>();
                int origA = (int) origSlashA.transform.GetRotation2D();
                int origB = (int) origSlashB.transform.GetRotation2D();
                for (int i = origA, j = origB; i < 270 + origA; i += 35, j -= 35)
                {
                    GameObject slashA = Instantiate(origSlashA);
                    GameObject slashB = Instantiate(origSlashB);
                    slashA.SetActive(false);
                    slashB.SetActive(false);
                    slashA.transform.SetRotation2D(i + (offsetAngle ? 20f : 0f));
                    slashB.transform.SetRotation2D(j - (offsetAngle ? 0f : 20f));
                    slashA.transform.position = new Vector3(transform.position.x, 7.4f);
                    slashB.transform.position = new Vector3(transform.position.x, 7.4f);
                    slashA.transform.localScale = origSlashA.transform.lossyScale;
                    slashB.transform.localScale = origSlashB.transform.lossyScale;
                    slashA.transform.localScale *= 0.68f;
                    slashB.transform.localScale *= 0.68f;
                    slashA.GetComponent<Animator>().speed = 1.5f;
                    slashB.GetComponent<Animator>().speed = 1.5f;
                    SlashA.Add(slashA);
                    SlashB.Add(slashB);
                }

                offsetAngle = !offsetAngle;

                SlashA = SlashA.OrderBy(a => Guid.NewGuid()).ToList();
                SlashB = SlashB.OrderBy(a => Guid.NewGuid()).ToList();
                for (int i = 0; i < SlashA.Count; i++)
                {
                    SlashA[i].SetActive(true);
                    SlashB[i].SetActive(true);
                    yield return new WaitForSeconds(0.05f);
                }

                Animator anim2 = SlashA[SlashA.Count - 1].GetComponent<Animator>();
                yield return new WaitWhile(() => anim2.IsPlaying());

                for (int i = 0; i < SlashA.Count; i++)
                {
                    Destroy(SlashA[i]);
                    Destroy(SlashB[i]);
                }
            }

            /*IEnumerator Pattern1()
            {
                List<float> lstRight = new List<float>();
                List<float> lstLeft = new List<float>();
                for (float i = LeftX + 3; i < RightX + 3; i += UnityEngine.Random.Range(7.5f, 8.2f)) lstRight.Add(i);
                for (float i = RightX - 3; i > LeftX - 3; i -= UnityEngine.Random.Range(7.5f, 8.2f)) lstLeft.Add(i);
                while (lstRight.Count != 0 || lstLeft.Count != 0)
                {
                    if (lstLeft.Count > 0)
                    {
                        int rot = -24;
                        int ind = _rand.Next(0, lstLeft.Count);
                        StartCoroutine(SingleSlashControl(lstLeft[ind], -1, rot));
                        lstLeft.RemoveAt(ind);
                    }

                    if (lstRight.Count > 0)
                    {
                        int rot = 24;
                        int ind = _rand.Next(0, lstRight.Count);
                        StartCoroutine(SingleSlashControl(lstRight[ind], 1, rot));
                        lstRight.RemoveAt(ind);
                    }

                    yield return new WaitForSeconds(0.05f);
                }
            }

            IEnumerator Pattern2()
            {
                List<float> lstRight = new List<float>();
                List<float> lstLeft = new List<float>();
                for (float i = LeftX + 3; i < RightX + 3; i += UnityEngine.Random.Range(7.5f, 8.2f)) lstRight.Add(i);
                for (float i = RightX - 3; i > LeftX - 3; i -= UnityEngine.Random.Range(7.5f, 8.2f)) lstLeft.Add(i);
                while (lstRight.Count != 0 || lstLeft.Count != 0)
                {
                    if (lstLeft.Count > 0)
                    {
                        int rot = 110;
                        int ind = _rand.Next(0, lstLeft.Count);
                        StartCoroutine(SingleSlashControl(lstLeft[ind], 1, rot));
                        lstLeft.RemoveAt(ind);
                    }

                    if (lstRight.Count > 0)
                    {
                        int rot = 260;
                        int ind = _rand.Next(0, lstRight.Count);
                        StartCoroutine(SingleSlashControl(lstRight[ind], -1, rot));
                        lstRight.RemoveAt(ind);
                    }

                    yield return new WaitForSeconds(0.05f);
                }
            }*/

            IEnumerator Randomized()
            {
                int zemX = (int) transform.position.x;
                List<int> lstRight = new List<int>();
                List<int> lstLeft = new List<int>();
                for (int i = zemX + 5; i < RightX + 3; i += _rand.Next(3, 6)) lstRight.Add(i);
                for (int i = zemX - 5; i > LeftX - 3; i -= _rand.Next(3, 6)) lstLeft.Add(i);
                while (lstRight.Count != 0 || lstLeft.Count != 0)
                {
                    int scale = _rand.Next(0, 2) == 0 ? -1 : 1;
                    int rot = _rand.Next(-10, 10);
                    if (lstLeft.Count > 0)
                    {
                        int ind = _rand.Next(0, lstLeft.Count);
                        StartCoroutine(SingleSlashControl(lstLeft[ind], scale, rot));
                        lstLeft.RemoveAt(ind);
                    }

                    if (lstRight.Count > 0)
                    {
                        int ind = _rand.Next(0, lstRight.Count);
                        StartCoroutine(SingleSlashControl(lstRight[ind], scale, rot));
                        lstRight.RemoveAt(ind);
                    }

                    yield return new WaitForSeconds(0.05f);
                }
            }

            IEnumerator SingleSlashControl(float posX, float scaleSig, float angle)
            {
                yield return new WaitForSeconds(_rand.Next(12, 25) / 100f);
                GameObject slash = Instantiate(FiveKnights.preloadedGO["SlashBeam"]);
                Animator anim = slash.GetComponent<Animator>();
                //slash.transform.localScale *= 1.43f;
                slash.transform.position = new Vector3(posX, GroundY + 4.5f); //GroundY - 1.3f
                slash.transform.SetRotation2D(angle);
                slash.SetActive(true);
                anim.enabled = true;
                anim.speed /= 1.5f;
                Vector3 vec = slash.transform.localScale;
                slash.transform.localScale = new Vector3(scaleSig * vec.x, vec.y, vec.z);
            }

            IEnumerator run = type switch
            {
                1 => Pattern1(),
                3 => Pattern3(),
                _ => Randomized()
            };
            StartCoroutine(run);
        }

        private bool IsFacingPlayer()
        {
            int sigZem = (int) Mathf.Sign(transform.localScale.x);
            int sigDiff = (int) Mathf.Sign(transform.position.x - _target.transform.position.x);
            return sigZem == sigDiff;
        }

        private IEnumerator Turn()
        {
            _anim.Play("ZTurn");
            yield return new WaitForSeconds(TurnDelay);
        }

        private void Spring(bool isIn, Vector2 pos, float speedSca = 1f)
        {
            string n = "VapeIn2";
            GameObject go = Instantiate(FiveKnights.preloadedGO[n]);
            PlayMakerFSM fsm = go.LocateMyFSM("FSM");
            go.GetComponent<tk2dSpriteAnimator>().GetClipByName("Plink").fps = 24 * speedSca;
            go.transform.localScale *= 1.7f; //1.3f
            fsm.GetAction<Wait>("State 1", 0).time = 0f;
            go.transform.position = pos;
            go.SetActive(true);
        }

        private void OnBlockedHit(On.HealthManager.orig_Hit orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.name.Contains("Zemer"))
            {
                // Prevent code block from running every frame
                if (!_blockedHit)
                {
                    _blockedHit = true;
                    //(0.04f, 0.2f, 0.04f, 0f)
                    GameManager.instance.StartCoroutine(GameManager.instance.FreezeMoment(0.01f, 0.35f, 0.1f, 0.0f));
                    Log("Blocked Hit");
                    Log("Closing " + _counterRoutine);
                    StopCoroutine(_counterRoutine);
                    StartCoroutine(Countered());
                    return;
                }
            }

            orig(self, hitInstance);
        }

        private IEnumerator Countered()
        {
            On.HealthManager.Hit -= OnBlockedHit;
            _anim.Play("ZCAtt");
            _hm.IsInvincible = false;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 15);
            PlayAudioClip("Slash",_ap,0.85f, 1.15f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            Parryable.ParryFlag = false;
            _anim.Play("ZIdle");
            yield return new WaitForSeconds(0.25f);
            _countering = false;
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
                if (_spinType == 1 && _hm.hp <= Phase3HP)
                {
                    _spinType = 3;
                }
                
                if (_hm.hp <= 50)
                {
                    Log("Going to die :(");
                    StopAllCoroutines();
                    StartCoroutine(Death());
                }
            }

            orig(self, hitInstance);
        }

        IEnumerator Death()
        {
            // TODO: This doesn't seem to take into account if Zem dies in the air??
            GameObject extraNail = GameObject.Find("ZNailB");
            if (extraNail != null && extraNail.transform.parent == null)
            {
                Destroy(extraNail);
            }

            FaceHero();
            if (OWArenaFinder.IsInOverWorld ) OWBossManager.PlayMusic(null);
            else GGBossManager.Instance.PlayMusic(null, 1f);
            PlayDeathFor(gameObject);
            _bc.enabled = false;
            _anim.enabled = true;
            _rb.velocity = Vector2.zero;
            _rb.gravityScale = 0f;
            _deathEff.RecordJournalEntry();
            _anim.Play("ZKnocked");
            PlayAudioClip("ZAudP2Death1",_voice);
            yield return null;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
            transform.position = new Vector3(transform.position.x, GroundY - 0.95f);
            yield return _anim.PlayToEnd();
            yield return new WaitForSeconds(FiveKnights.Clips["ZAudP2Death1"].length);
            PlayAudioClip("ZAudP2Death2",_voice);
            yield return new WaitForSeconds(1.75f);
            CustomWP.wonLastFight = true;
            // Stop music here.
            Destroy(this);
        }

        private int FaceHero(bool onlyCalc = false, bool opposite = false)
        {
            int sign = (int) Mathf.Sign(gameObject.transform.GetPositionX() - _target.transform.GetPositionX());
            sign = opposite ? -sign : sign;
            if (onlyCalc)
                return sign;

            Vector3 pScale = gameObject.transform.localScale;

            gameObject.transform.localScale = new Vector3(Mathf.Abs(pScale.x) * sign, pScale.y, 1f);

            return sign;
        }

        private Vector3 Product(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        }

        private static readonly int FLASH_AMOUNT = Shader.PropertyToID("_FlashAmount");

        private IEnumerator FlashWhite()
        {
            _sr.material.SetFloat(FLASH_AMOUNT, 1f);

            yield return null;

            for (float i = 1f; i >= 0f; i -= 0.05f)
            {
                _sr.material.SetFloat(FLASH_AMOUNT, i);
                yield return new WaitForSeconds(0.02f);
            }

            yield return null;
        }

        private static void SpawnShockwaves(float vertScale, float speed, int damage, Vector2 pos)
        {
            bool[] facingRightBools = {false, true};

            PlayMakerFSM fsm = FiveKnights.preloadedGO["Mage"].LocateMyFSM("Mage Lord");

            foreach (bool facingRight in facingRightBools)
            {
                GameObject shockwave = Instantiate
                (
                    fsm.GetAction<SpawnObjectFromGlobalPool>("Quake Waves", 0).gameObject.Value
                );

                PlayMakerFSM shockFSM = shockwave.LocateMyFSM("shockwave");

                shockFSM.FsmVariables.FindFsmBool("Facing Right").Value = facingRight;
                shockFSM.FsmVariables.FindFsmFloat("Speed").Value = speed;

                shockwave.AddComponent<DamageHero>().damageDealt = damage;

                shockwave.SetActive(true);

                shockwave.transform.SetPosition2D(new Vector2(pos.x + (facingRight ? 0.5f : -0.5f), 6f));
                shockwave.transform.SetScaleX(vertScale);
            }
        }

        private void PlayAudioClip(
            string clipName, MusicPlayer ap, 
            float pitchMin = 1.0f, float pitchMax = 1.0f, float time = 0.0f)
        {
            ap.MaxPitch = pitchMax;
            ap.MinPitch = pitchMin;

            ap.Clip = clipName switch
            {
                "Counter" => (AudioClip) _pvFsm.GetAction<AudioPlayerOneShotSingle>("Counter Stance", 1).audioClip.Value,
                "Slash" => (AudioClip) _pvFsm.GetAction<AudioPlayerOneShotSingle>("Slash1", 1).audioClip.Value,
                "TraitorPillar" => FiveKnights.Clips["TraitorSlam"],
                _ => FiveKnights.Clips[clipName]
            };

            ap.DoPlayRandomClip();
        }

        private static bool FastApproximately(float a, float b, float threshold)
        {
            return Math.Abs(a - b) <= threshold;
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
                Color col = _sr.color;

                _sr.enabled = true;

                if (visible)
                {
                    _sr.color = new Color(col.r, col.g, col.b, 0f);

                    for (float i = 0; i <= 1f; i += 0.2f)
                    {
                        _sr.color = new Color(col.r, col.g, col.b, i);

                        yield return new WaitForSeconds(0.01f);
                    }
                }
                else
                {
                    _sr.color = new Color(col.r, col.g, col.b, 1f);

                    for (float i = col.a; i >= 0f; i -= 0.2f)
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

                _rb.gravityScale = 0f;

                _bc.enabled = visible;
                
                Color col = _sr.color;

                _sr.color = new Color(col.r, col.g, col.b, visible ? 1f : 0f);
            }

            _bc.enabled = false;

            if (fade)
                StartCoroutine(Fade());
            else
                Instant();
        }

        private void AssignFields()
        {
            Transform slash = transform.Find("NewSlash3");
            slash.localPosition = new Vector3(-16f,3.5f,-0.5f);
            slash.localScale = new Vector3(0.6f,0.6f,0.5f);
            
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
            
            _voice = new MusicPlayer
            {
                Volume = 1f,
                Player = actor,
                MaxPitch = 1f,
                MinPitch = 1f,
                Spawn = gameObject
            };

            traitorSlam = new GameObject[2];
            traitorSlamIndex = 0;

            for (int i = 0; i < traitorSlam.Length; i++)
            {
                traitorSlam[i] = Instantiate(FiveKnights.preloadedGO["TraitorSlam"]);
                Destroy(traitorSlam[i].GetComponent<AutoRecycleSelf>());
                traitorSlam[i].transform.Find("slash_core").Find("hurtbox").GetComponent<DamageHero>().damageDealt = 1;
                traitorSlam[i].SetActive(false);
                Log("FOR SURE");
            }

            foreach (DamageHero dh in transform.Find("Ki").GetComponentsInChildren<DamageHero>(true))
            {
                dh.damageDealt = 2;
            }

            foreach (DamageHero dh in transform.Find("HyperCut").GetComponentsInChildren<DamageHero>(true))
            {
                dh.damageDealt = 2;
            }

            foreach (DamageHero dh in transform.Find("BladeAerialShadow").GetComponentsInChildren<DamageHero>(true))
            {
                dh.damageDealt = 2;
            }
        }

        private void Log(object o)
        {
            Logger.Log("[Zemer2] " + o);
        }

        private void OnDestroy()
        {
            On.HealthManager.Hit -= OnBlockedHit;
            On.HealthManager.TakeDamage -= HealthManager_TakeDamage;
            On.EnemyDreamnailReaction.RecieveDreamImpact -= OnReceiveDreamImpact;
        }
    }
}
