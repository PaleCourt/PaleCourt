﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FiveKnights.BossManagement;
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

        private readonly float PlayerGndY = CustomWP.boss == CustomWP.Boss.All ? 23.919f : 23.919f;
        private readonly float deathGndOffset = (OWArenaFinder.IsInOverWorld) ? 1.18f : 0.7f;
        private readonly float GroundY = (OWArenaFinder.IsInOverWorld) ? 108.3f :   
            (CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim) ? 9f : 28.8f;
        private readonly float LeftX = (OWArenaFinder.IsInOverWorld) ? 240.1f :
            (CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim) ? 61.0f : 11.2f;
        private readonly float RightX = (OWArenaFinder.IsInOverWorld) ? 273.9f :
            (CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim) ? 91.0f : 45.7f;
        private readonly float SlamY = (OWArenaFinder.IsInOverWorld) ? 105f :
            (CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim) ? 6f : 25.9f;
        private readonly float NailHeightGrab = 19f;
        
        private const int Phase2HP = 1500;
        private int DoneFrenzyAtt;
        private const int Phase3HP = 1000;

        private const float NailMaxHeightStop = 39f;
        private const float NailMaxLeftStop = 13f;
        private const float NailMaxRightStop = 43f;
        private const float TurnDelay = 0.05f;
        private const float LandSlideTPInDelay = 0.07f;
        private const float LaserNutsEndDelay = 0.5f;
        private const float IdleDelay = 0.19f; //0.38
        private const float DashDelay = 0.18f;
        private float MIDDLE;
        private const float ThrowDelay = 0.2f;
        private const float SwingOutToInDelay = 0.75f;
        private const float GenericReturnDelay = 0.75f;
        private const float RecoveryReturnFirstDelay = 3.5f;
        private const float RecoveryReturnRestDelay = 0.75f;
        private const float NailSize = 1.15f;
        private const float LeaveAnimSpeed = 2.75f;
        private const float AfterAirNailGrabDelay = 0.4f;
        private const float StunTimeFirst = 1.5f;
        private const float StunTimeRest = 0.5f;
        private const float DashXVel = 70f;

        private const float SmallPillarSpd = 23.5f;
        private readonly Vector2 SmallPillarSize = new Vector2(1.1f, 0.45f);
        private const float Att1CompAnticTime = 0.25f;
        private float Att1BaseDelay = 0.4f;
        private float TwoFancyDelay = 0.25f;
        private readonly Vector3 LeaveOffset = new Vector3(1.5f, 1.5f);

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
            DoneFrenzyAtt = 0;
            MIDDLE = (RightX + LeftX) / 2f;
            
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

            yield return EndPhase1(true);
        }
        
        private void Update()
        {
            if (_isKnockingOut)
            {
                _rb.isKinematic = false;
                _bc.enabled = false;
            }

            if ((_bc == null || !_bc.enabled) 
                && !_anim.GetCurrentAnimatorStateInfo(0).IsName("ZKnocked") 
                && !_anim.GetCurrentAnimatorStateInfo(0).IsName("ZThrow2B")) return;

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

                yield return SweepDash();
                yield return new WaitForSeconds(0.5f);
                continue;
                
                
                if (posH.y > GroundY + 9f && (posH.x <= LeftX || posH.x >= RightX))
                {
                    yield return SpinAttack();
                }
                else if (FastApproximately(posZem.x, posH.x, 5f) && 
                         !((DoneFrenzyAtt == 0 && _hm.hp < 0.65f * Phase2HP) || (DoneFrenzyAtt == 1 && _hm.hp < 0.35f * Phase2HP)))
                {
                    int r = _rand.Next(0, 6);
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
                    else if (r < 3)
                    {
                        counterCount = 0;
                        Log("Doing NailLaunch");
                        yield return NailLaunch();
                        Log("Done NailLaunch");
                    }
                    else
                    {
                        counterCount = 0;
                    }
                }

                List<Func<IEnumerator>> attLst = new List<Func<IEnumerator>> { NailLaunch, null, null, null };
                Func<IEnumerator> currAtt = null;
                
                if (!FastApproximately(posZem.x, posH.x, 10f)) currAtt = attLst[_rand.Next(0, attLst.Count)];

                if (currAtt != null)
                {
                    rep[currAtt]++;
                    Log("Doing " + currAtt.Method.Name);
                    yield return currAtt();
                    Log("Done " + currAtt.Method.Name);
                }
                else
                {
                    attLst = new List<Func<IEnumerator>>
                    {
                        Dash, Attack1Base, NailLaunch, 
                        AerialAttack, DoubleFancy, SweepDash, ZemerSlam
                    };

                    currAtt = attLst[_rand.Next(0, attLst.Count)];

                    while (rep[currAtt] >= 2)
                    {
                        attLst.Remove(currAtt);
                        rep[currAtt] = 0;
                        currAtt = attLst[_rand.Next(0, attLst.Count)];
                    }

                    rep[currAtt]++;
                    Log("Doing " + currAtt.Method.Name);
                    yield return currAtt();
                    Log("Done " + currAtt.Method.Name);

                    if (currAtt == Attack1Base)
                    {
                        List<Func<IEnumerator>> lst2 = FastApproximately(transform.position.x, _target.transform.position.x, 7f) ? 
                            new List<Func<IEnumerator>> {Attack1Complete} : 
                            new List<Func<IEnumerator>> {Attack1Complete, FancyAttack, FancyAttack};

                        currAtt = lst2[_rand.Next(0, lst2.Count)];
                        Log("Doing " + currAtt.Method.Name);
                        yield return currAtt();
                        Log("Done " + currAtt.Method.Name);
                    
                        if (currAtt == FancyAttack && _rand.Next(0,3) < 2)
                        {
                            Log("Doing Special Fancy Attack");
                            yield return Dodge();
                            yield return new WaitForSeconds(TwoFancyDelay);
                            yield return FancyAttack();
                            yield return Dash();
                            Log("Done Special Fancy Attack");
                        }
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
                //if (hero.x.Within(zem.x, 10f) && hero.y < GroundY + 1.5f)
                if (hero.y < GroundY + 1.5f)
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
                yield return new WaitForSeconds(ThrowDelay);
                _anim.enabled = true;
                hero = _target.transform.position;
                zem = gameObject.transform.position;
                dir = FaceHero();
                yield return _anim.WaitToFrame(4);
                rot = Mathf.Atan((hero.y - zem.y) / (hero.x - zem.x));
                float rotVel = dir > 0 ? rot + 180 * Mathf.Deg2Rad : rot;
                GameObject arm = transform.Find("NailHand").gameObject;
                GameObject nailPar = Instantiate(transform.Find("ZNailB").gameObject);
                Rigidbody2D parRB = nailPar.GetComponent<Rigidbody2D>();
                arm.transform.SetRotation2D(rot * Mathf.Rad2Deg);
                nailPar.transform.SetRotation2D(rot * Mathf.Rad2Deg);
                nailPar.transform.position = transform.Find("ZNailB").position;
                nailPar.transform.localScale = new Vector3(dir * NailSize, NailSize, NailSize);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 5);
                nailPar.SetActive(true);
                // TODO might want to readjust speed
                float velmag = hero.y < GroundY + 2f ? 70f : 50f;
                parRB.velocity = new Vector2(Mathf.Cos(rotVel) * velmag, Mathf.Sin(rotVel) * velmag);
                yield return new WaitForSeconds(0.02f);
                var cc = nailPar.transform.Find("ZNailC").gameObject.AddComponent<CollisionCheck>();
                cc.Freeze = true;
                cc.OnCollide += () =>
                {
                    Log($"Collision debug 1: {nailPar.GetComponent<Rigidbody2D>().velocity}");
                    PlayAudioClip("AudLand",_ap);
                    GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
                    nailPar.GetComponent<SpriteRenderer>().enabled = false;
                    nailPar.transform.Find("ZNailN").GetComponent<SpriteRenderer>().enabled = true;
                    nailPar.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
                    Destroy(cc);
                };
                yield return new WaitWhile(() => _anim.IsPlaying());
                bool isTooHigh = nailPar.transform.position.y > GroundY + 1f;
                yield return isTooHigh ? LaunchUp(rot, nailPar) : LaunchSide(nailPar);
            }

            IEnumerator GndLeave(float dir, bool isSpedup=false)
            {
                if (isSpedup)
                {
                    _anim.speed = 2f;
                    _anim.PlayAt("ZThrow2", 3);
                    Spring(false, transform.position + new Vector3(-dir * LeaveOffset.x, 0f,0f), 1.6f);
                    yield return null;
                    yield return _anim.WaitToFrame(4);
                    transform.position += new Vector3(-dir * LeaveOffset.x, 0f);
                    yield return _anim.PlayToEnd();
                    ToggleZemer(false);
                }
                else
                {
                    _anim.speed = 2f;
                    _anim.Play("ZThrow2");
                    yield return null;
                    yield return _anim.WaitToFrame(2);
                    Spring(false, transform.position + new Vector3(-dir * LeaveOffset.x, 0f,0f), 1.5f);
                    yield return _anim.WaitToFrame(4);
                    transform.position += new Vector3(-dir * LeaveOffset.x, 0f);
                    yield return _anim.PlayToEnd();
                    ToggleZemer(false);   
                }
            }
            
            IEnumerator LaunchUp(float rot, GameObject nail, bool isRepeat=false)
            {
                float rotVel = dir > 0 ? rot + 180 * Mathf.Deg2Rad : rot;
                GameObject col = nail.transform.Find("ZNailC").gameObject;
                CollisionCheck cc = col.GetComponent<CollisionCheck>() ?? col.AddComponent<CollisionCheck>();
                Rigidbody2D rb = nail.GetComponent<Rigidbody2D>();

                if (!isRepeat) yield return GndLeave(dir, true);

                cc.Hit = rb.velocity == Vector2.zero;
                Log("Waiting to grab nail");
                yield return new WaitWhile(() => nail.transform.position.y < NailHeightGrab && !cc.Hit);
                Log("Went to get nail!!");
                var pos = nail.transform.position + new Vector3(5f * Mathf.Cos(rotVel), 5f * Mathf.Sin(rotVel), 0f);
                if (pos.y > NailMaxHeightStop) pos.y = NailMaxHeightStop;
                if (pos.x > NailMaxRightStop) pos.x = NailMaxRightStop;
                if (pos.x < NailMaxLeftStop) pos.x = NailMaxLeftStop;
                transform.position = pos;
                ToggleZemer(true);
                _anim.speed = 2f;
                _anim.Play("ZThrow3Air", -1, 0f);
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                nail.SetActive(false);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                PlayAudioClip("ZAudAtt" + _rand.Next(1,7), _voice);
                _anim.speed = 1f;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
                _anim.enabled = false;
                yield return new WaitForSeconds(AfterAirNailGrabDelay);
                _anim.enabled = true;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 5);
                nail.SetActive(true);
                Vector2 hero = _target.transform.position;
                Vector2 zem = gameObject.transform.position;
                float oldDir = dir;
                dir = FaceHero();
                rot = Mathf.Atan((hero.y - zem.y) / (hero.x - zem.x));
                rotVel = dir > 0 ? rot + Mathf.PI : rot;
                float offset = dir.Within(oldDir, 0.1f) ? 0f : 180f;
                float tRot = rot * Mathf.Rad2Deg + offset;
                nail.transform.SetRotation2D(rot * Mathf.Rad2Deg);//offset);
                var vel = new Vector2(Mathf.Cos(rotVel) * 70f, Mathf.Sin(rotVel) * 70f);
                float deltaX = vel.x > 0 ? RightX - nail.transform.position.x : LeftX - nail.transform.position.x;
                float time = deltaX / vel.x;
                float endY = vel.y * time + nail.transform.position.y;

                if (endY > 31f)//GroundY + 2f)//rotVel * Mathf.Rad2Deg is > -20f and < 200f)
                {
                    var t = Mathf.Sign(nail.transform.localScale.x) * Mathf.Sign(transform.localScale.x);
                    nail.transform.SetRotation2D(t < 0 ? rot * Mathf.Rad2Deg + 180f : rot * Mathf.Rad2Deg);
                    Log("Going to repeat!!");
                    rb.velocity = vel;
                    nail.transform.position = transform.position;
                    if (cc != null) Destroy(cc);
                    cc = col.AddComponent<CollisionCheck>();
                    nail.GetComponent<SpriteRenderer>().enabled = true;
                    nail.transform.Find("ZNailN").GetComponent<SpriteRenderer>().enabled = false;
                    cc.Freeze = false;
                    cc.Hit = false;
                    cc.OnCollide += () =>
                    {
                        if (nail.transform.position.y > GroundY + 5f) return;
                        PlayAudioClip("AudLand",_ap);
                        GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
                        nail.GetComponent<SpriteRenderer>().enabled = false;
                        nail.transform.Find("ZNailN").GetComponent<SpriteRenderer>().enabled = true;
                        rb.velocity = Vector2.zero;
                        Destroy(cc);
                    };
                    yield return new WaitForSeconds(0.02f);
                    cc.Freeze = true;
                    yield return new WaitWhile(() => _anim.GetCurrentFrame() < 8);
                    rotVel = dir > 0 ? 10f : -45f;
                
                    _anim.speed = 2f;
                    Spring(false, transform.position + new Vector3(LeaveOffset.x * Mathf.Cos(rotVel), LeaveOffset.y * Mathf.Sin(rotVel),0f));
                    transform.position += new Vector3(LeaveOffset.x * Mathf.Cos(rotVel), LeaveOffset.y * Mathf.Sin(rotVel));
                    yield return _anim.PlayToEnd();
                    ToggleZemer(false);
                    cc.Freeze = true;
                    yield return LaunchUp(rot, nail, true);
                    yield break;

                    /*hero = new Vector2(hero.x, GroundY);
                    rot = Mathf.Atan((hero.y - zem.y) / (hero.x - zem.x));
                    rotVel = dir > 0 ? rot + Mathf.PI : rot;
                    offset = dir.Within(oldDir, 0.1f) ? 0f : 180f;
                    tRot = rot * Mathf.Rad2Deg + offset;
                    nail.transform.SetRotation2D(rot * Mathf.Rad2Deg + offset);*/
                }
                
                var t2 = Mathf.Sign(nail.transform.localScale.x) * Mathf.Sign(transform.localScale.x);
                nail.transform.SetRotation2D(t2 < 0 ? rot * Mathf.Rad2Deg + 180f : rot * Mathf.Rad2Deg);
                rb.velocity = new Vector2(Mathf.Cos(rotVel) * 90f, Mathf.Sin(rotVel) * 90f);
                col.GetComponent<BoxCollider2D>().enabled = true;
                col.SetActive(true);
                nail.transform.position = transform.position;
                if (cc != null) Destroy(cc);
                cc = col.AddComponent<CollisionCheck>();
                cc.Hit = false;
                cc.Freeze = false;
                nail.GetComponent<SpriteRenderer>().enabled = true;
                nail.transform.Find("ZNailN").GetComponent<SpriteRenderer>().enabled = false;
                cc.OnCollide += () =>
                {
                    if (nail.transform.position.y > GroundY + 5f) return;
                    PlayAudioClip("AudLand",_ap);
                    GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
                    nail.GetComponent<SpriteRenderer>().enabled = false;
                    nail.transform.Find("ZNailN").GetComponent<SpriteRenderer>().enabled = true;
                    rb.velocity = Vector2.zero;
                    Destroy(cc);
                };
                yield return new WaitForSeconds(0.02f);
                cc.Freeze = true;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 8);
                rotVel = dir > 0 ? 10f : -45f;
                
                _anim.speed = 2f;
                Spring(false, transform.position + new Vector3(LeaveOffset.x * Mathf.Cos(rotVel), LeaveOffset.y * Mathf.Sin(rotVel),0f));
                transform.position += new Vector3(LeaveOffset.x * Mathf.Cos(rotVel), LeaveOffset.y * Mathf.Sin(rotVel));
                yield return _anim.PlayToEnd();
                ToggleZemer(false);
                yield return new WaitForSeconds(0.1f);
                yield return new WaitWhile(() => !cc.Hit);
                transform.position = new Vector3(80f, GroundY);
                yield return new WaitForSeconds(0.75f);
                yield return LaunchSide(nail, false);
            }

            IEnumerator LaunchSide(GameObject nail, bool leave = true)
            {
                GameObject col = nail.transform.Find("ZNailC").gameObject;
                Rigidbody2D rbNail = nail.GetComponent<Rigidbody2D>();
                CollisionCheck cc = col.GetComponent<CollisionCheck>();
                Log("Doing launch side");
                if (cc == null)
                {
                    cc = col.AddComponent<CollisionCheck>();
                    cc.OnCollide += () =>
                    {
                        PlayAudioClip("AudLand",_ap);
                        GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
                        nail.GetComponent<SpriteRenderer>().enabled = false;
                        nail.transform.Find("ZNailN").GetComponent<SpriteRenderer>().enabled = true;
                        rbNail.velocity = Vector2.zero;
                        Destroy(cc);
                    };
                }
                
                if (leave)
                {
                    cc.Hit = rbNail.velocity == Vector2.zero;
                    yield return GndLeave(dir);
                    yield return new WaitWhile(() => !cc.Hit);
                    yield return new WaitForSeconds(0.75f);
                }

                Vector2 zem = transform.position;
                Vector2 nl = nail.transform.Find("Point").position;
                Vector3 zemSc = transform.localScale;
                
                if (nl.x < MIDDLE)
                {
                    transform.localScale = new Vector3(Mathf.Abs(zemSc.x), zemSc.y, zemSc.z);
                    transform.position = new Vector2(nl.x + LeaveOffset.x, zem.y);
                    Spring(true, transform.position);
                    yield return new WaitForSeconds(0.15f);
                    ToggleZemer(true);
                    _anim.speed = 2f;
                    _anim.Play("ZThrow3Gnd");
                    yield return null;
                    yield return _anim.WaitToFrame(1);
                    transform.position -= new Vector3(LeaveOffset.x, 0f);
                    yield return _anim.WaitToFrame(2);
                    _anim.speed = 1f;
                    Destroy(nail);
                }
                else
                {
                    transform.localScale = new Vector3(-Mathf.Abs(zemSc.x), zemSc.y, zemSc.z);
                    transform.position = new Vector2(nl.x - LeaveOffset.x, zem.y);
                    Spring(true, transform.position);
                    yield return new WaitForSeconds(0.15f);
                    ToggleZemer(true);
                    _anim.speed = 2f;
                    _anim.Play("ZThrow3Gnd");
                    yield return null;
                    yield return _anim.WaitToFrame(1);
                    transform.position += new Vector3(LeaveOffset.x, 0f);
                    yield return _anim.WaitToFrame(2);
                    _anim.speed = 1f;
                    Destroy(nail);
                }

                if (_hm.hp <= Phase3HP && _rand.Next(0, 5) < 3)
                {
                    _anim.PlayAt("Z2Crawl", 1);
                    yield return null;
                    yield return new WaitWhile(() => _anim.IsPlaying());
                    float sig = Mathf.Sign(transform.localScale.x);
                    yield return RageCombo(sig, false, _spinType);
                    yield break;
                }

                yield return _anim.PlayToEnd();
                if (doSlam) yield return ZemerSlam();
            }

            _lastAtt = NailLaunch;
            yield return Throw();
        }

        private IEnumerator ZemerSlam()
        {
            IEnumerator Slam()
            {
                transform.position += new Vector3(0f, 1.32f);
                yield return _anim.PlayToFrame("ZSlamNew", 7);
                
                SpawnShockwaves(2f, 50f, 1, transform.position);

                GameCameras.instance.cameraShakeFSM.SendEvent("BigShake");
                PlayAudioClip("AudLand", _ap);
                
                yield return _anim.PlayToEnd();
                transform.position -= new Vector3(0f, 1.32f);
            }
            
            void SpawnShockwaves(float vertScale, float speed, int damage, Vector2 pos)
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
        
                    shockwave.transform.SetPosition2D(new Vector2(pos.x + (facingRight ? 0.5f : -0.5f), SlamY));
                    shockwave.transform.SetScaleX(vertScale);
                }
            }

            _lastAtt = ZemerSlam;
            yield return Slam();
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

                yield return new WaitForSeconds(Att1BaseDelay);

                _anim.enabled = true;

                yield return _anim.WaitToFrame(2);

                PlayAudioClip("Slash",_ap, 0.85f, 1.15f);

                yield return _anim.WaitToFrame(6);

                // If player has gone behind, do backward slash
                if ((int) -xVel != FaceHero(true))
                {
                    yield return RageCombo(-xVel, false, _spinType); // changed so it only does dash
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
            IEnumerator BackIn(float dir)
            {
                float x = dir > 0 ? LeftX + 11f : RightX - 11f;
                float tarX = dir > 0 ? LeftX + 6f : RightX - 6f;

                transform.position = new Vector3(x, GroundY + 6f);
                Vector3 tmp = transform.localScale;
                transform.localScale = new Vector3(dir * Mathf.Abs(tmp.x), tmp.y, tmp.z);
                
                Spring(true, transform.position, 1.8f);

                yield return new WaitForSeconds(0.15f);

                ToggleZemer(true);

                var diff = new Vector2(x - tarX, transform.position.y - GroundY - 0.95f);

                float rot = Mathf.Atan(diff.y / diff.x);

                rot = tarX < MIDDLE ? rot + Mathf.PI : rot;

                // Scuffed workaround for CC just to make it work for now
                if(CustomWP.boss != CustomWP.Boss.All && CustomWP.boss != CustomWP.Boss.Ogrim)
				{
                    _rb.velocity = new Vector2(55f * Mathf.Cos(rot), 55f * Mathf.Sin(rot));
                }
                else _rb.velocity = new Vector2(55f * Mathf.Cos(rot), -Mathf.Abs(55f * Mathf.Sin(rot)));

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
                _rb.velocity = new Vector2(-dir * DashXVel, 0f);
                
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

            yield return LeaveTemp(-FaceHero(), GenericReturnDelay);
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

                yield return LeaveTemp(-dir, GenericReturnDelay);
                yield return FlyStrike();
            }

            IEnumerator FlyStrike()
            {
                float heroX = _target.transform.position.x;
                float hVelX = _target.GetComponent<Rigidbody2D>().velocity.x;

                const float offset = 1.5f;

                int dir = FaceHero();

                float target_pos_x = heroX;

                target_pos_x = target_pos_x < MIDDLE
                    ? target_pos_x + 8
                    : target_pos_x - 8;

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

                if (heroX.Within(transform.position.x, 2.5f))
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
                // Removed landslide here so that laser attack only happens in strict intervals
                // yield return (_rand.Next(0,5) < 2 ? LandSlide() : Dash());
                yield return LandSlide();
            }
            
            IEnumerator LandSlide()
            {

                float dir = Mathf.Sign(transform.localScale.x);
                float XVel = 75f;

                //_anim.speed = 2f;
                _anim.PlayAt("ZThrow2", 4);
                Spring(false, transform.position + new Vector3(-dir * LeaveOffset.x, 0f, 0f), 1.5f);
                transform.position += new Vector3(-dir * LeaveOffset.x, 0f);
                yield return _anim.PlayToEnd();
                ToggleZemer(false);
                int maxCount = 6;
                
                yield return new WaitForSeconds(0.2f);
                
                GameObject grass = Instantiate(FiveKnights.preloadedGO["TraitorSlam"].transform.Find("Grass").gameObject, transform, true);
                ParticleSystem psGrass = grass.GetComponent<ParticleSystem>();
                grass.SetActive(true);
                psGrass.Stop();
                grass.transform.position = transform.position;
                grass.transform.parent = transform;
                
                for (int i = 0; i < maxCount; i++)
                {
                    float heroX = _target.transform.position.x;
                    float zemY = transform.position.y;
                    float offsetRand = _rand.Next(0, 3);

                    if (heroX.Within(LeftX, 7f))
                    {
                        transform.position = new Vector3(heroX + 10f + offsetRand, zemY);
                    }
                    else if (heroX.Within(RightX, 7f))
                    {
                        transform.position = new Vector3(heroX - 10f - offsetRand, zemY);
                    }
                    else
                    {
                        Vector3 pos = i % 2 == 0
                            ? new Vector3(heroX + 10f + offsetRand, zemY)
                            : new Vector3(heroX - 10f - offsetRand, zemY);
                        if (pos.x > RightX) pos.x = RightX;
                        if (pos.x < LeftX) pos.x = LeftX;
                        transform.position = pos;
                    }

                    yield return null;
                    
                    float signX = FaceHero();

                    Spring(true, transform.position, 1.5f);
                    ToggleZemer(true);

                    Log("Doing special dash");
                    PlayAudioClip("AudDash",_ap);
                    
                    transform.Find("HyperCut").gameObject.SetActive(false);
                    _anim.PlayAt("ZMultiDashAir", 1);
                    _anim.enabled = true;
                    _anim.speed = 0.5f;
                    yield return null;
                    // _anim.enabled = false;
                    psGrass.Play();
                    float velX = -signX * (XVel + i * 3f);
                    _rb.velocity = new Vector2(velX, 0f);
                    offsetRand = _rand.Next(1, 5);

                    // trying to add here
                    float oldDiff = _target.transform.position.x - transform.position.x;

                    yield return new WaitWhile(() =>
                    {
                        var newDiff = _target.transform.position.x - transform.position.x;
                        if (newDiff * oldDiff <= 0 && Mathf.Abs(newDiff) > 7f + offsetRand) return false;
                        if (_rb.velocity.x.Within(0f, 0.1f)) return false;
                        if (-signX <= 0 && transform.position.x.Within( LeftX, 3f)) return false;
                        if (-signX > 0 && transform.position.x.Within(RightX, 3f)) return false;
                        return true;
                    });

                    if ((_target.transform.position.x.Within(LeftX, 2.5f) ||
                         _target.transform.position.x.Within(RightX, 2.5f)) &&
                        _target.transform.position.y > GroundY + 3f)
                    {
                        _anim.enabled = true;
                        float xVel = FaceHero() * -1f;
                        float diffX = Mathf.Abs(_target.transform.GetPositionX() - transform.GetPositionX());
                        float diffY = Mathf.Abs(_target.transform.GetPositionY() - transform.GetPositionY());
                        float rot = Mathf.Atan(diffY / diffX);
                        rot = xVel < 0 ? Mathf.PI - rot : rot;
                        PlayAudioClip("AudDashIntro",_ap);
                        _anim.speed = 3f;
                        psGrass.Stop();
                        yield return _anim.PlayToEnd();
                        _anim.speed = 1f;
                        _anim.PlayAt("ZSpin", 4);
                        _rb.velocity = new Vector2(60f * Mathf.Cos(rot), 60f * Mathf.Sin(rot));
                        yield return new WaitForSeconds(1 / 12f);
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

                        yield break;
                    }
                    
                    if (i != maxCount - 1)
                    {
                        float xNext = velX * 0.1f;
                        Vector3 newPos = transform.position + new Vector3(xNext, 0f, 0f);
                        if (newPos.x > RightX) newPos.x = RightX - 1f;
                        if (newPos.x < LeftX) newPos.x = LeftX + 1f;
                        Spring(false, newPos, 1.5f);
                        _anim.speed = 1f;
                        psGrass.Stop();
                        yield return new WaitWhile(() =>
                        {
                            if (_rb.velocity.x.Within(0f, 0.1f)) return false;
                            if (-signX <= 0)
                            {
                                if (transform.position.x.Within(LeftX, 1.2f)) return false;
                                if (transform.position.x < newPos.x) return false;
                            }
                            if (-signX > 0)
                            {
                                if (-signX > 0 && transform.position.x.Within(RightX, 1.2f)) return false;
                                if (transform.position.x > newPos.x) return false;
                            }
                            return true;
                        });
                        _rb.velocity = Vector2.zero;
                        ToggleZemer(false);
                        yield return new WaitForSeconds(0.2f);
                    }

                }
                psGrass.Stop();
                _anim.speed = 1f;
                _anim.PlayAt("ZDash", 7);
                _anim.enabled = true;
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(() => _anim.IsPlaying());

            }

            yield return (Leave());
        }

        private IEnumerator SwingTemp(float dir, float delay)
        {
            _anim.Play("Z3Swing");
            yield return null;
            yield return _anim.WaitToFrame(0);
            _anim.enabled = false;
            yield return new WaitForSeconds(delay);
            _anim.enabled = true;
            yield return _anim.WaitToFrame(2);
            PlayAudioClip("ZAudAtt" + _rand.Next(1,7), _voice);
            SpawnPillar(-dir, SmallPillarSize, SmallPillarSpd);
            yield return _anim.WaitToFrame(5);
            Spring(false, transform.position + new Vector3(dir * LeaveOffset.x, LeaveOffset.y,0f),1.5f);
            yield return _anim.WaitToFrame(6);
            _anim.speed = LeaveAnimSpeed;
            transform.position += new Vector3(dir * LeaveOffset.x, LeaveOffset.y);
            yield return _anim.PlayToEnd();
            ToggleZemer(false, false);
        }

        private IEnumerator RageCombo(float dir, bool special, int spinType)
        {
            IEnumerator Swing()
            {
                _lastAtt = null;

                yield return SwingTemp(dir, special ? 0.2f : 0.4f);
                yield return new WaitForSeconds(SwingOutToInDelay); //0.15f
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

                yield return LandSlide();
            }

            IEnumerator LandSlide()
            {
                float dir = Mathf.Sign(transform.localScale.x);
                
                //_anim.speed = 2f;
                _anim.PlayAt("ZThrow2", 4);
                Spring(false, transform.position + new Vector3(-dir * LeaveOffset.x, 0f,0f), 1.5f);
                transform.position += new Vector3(-dir * LeaveOffset.x, 0f);
                yield return _anim.PlayToEnd();
                ToggleZemer(false);

                yield return new WaitForSeconds(LandSlideTPInDelay);

                transform.position = dir > 0
                    ? new Vector3(RightX - 1.5f, transform.position.y)
                    : new Vector3(LeftX + 1.5f, transform.position.y);
                
                Spring(true, transform.position);
                yield return new WaitForSeconds(0.15f);
                ToggleZemer(true);
                
                if (special)
                {
                    // Changed this so instead of going towards middle, we stay at opposite end of arena
                    float signX = Mathf.Sign(gameObject.transform.GetPositionX() - MIDDLE);
                    Vector3 pScale = gameObject.transform.localScale;
                    gameObject.transform.localScale = new Vector3(Mathf.Abs(pScale.x) * signX, pScale.y, 1f);
                    _anim.enabled = true;
                    _anim.Play("ZIdle");
                    yield return DoSpiralPassage();
                }
                else
                {
                    float signX = Mathf.Sign(gameObject.transform.GetPositionX() - MIDDLE);
                    Vector3 pScale = gameObject.transform.localScale;
                    gameObject.transform.localScale = new Vector3(Mathf.Abs(pScale.x) * signX, pScale.y, 1f);
                    
                    Log("Doing special dash");
                    transform.Find("HyperCut").gameObject.SetActive(false);
                    _anim.PlayAt("ZDash", 6);
                    yield return null;
                    _anim.enabled = false;
                    _rb.velocity = new Vector2(-signX * DashXVel, 0f);
                    yield return new WaitWhile
                    (
                        () => !FastApproximately(_rb.velocity.x, 0f, 0.1f) &&
                              ((-signX <= 0 && !FastApproximately(transform.position.x, LeftX, 10f)) ||
                               (-signX > 0 && !FastApproximately(transform.position.x, RightX, 10f)))
                    );
                    _anim.enabled = true;
                    _rb.velocity = Vector2.zero;
                    yield return new WaitWhile(() => _anim.IsPlaying());

                }
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
                
                // Removed this so laser attack only happens in strict intervals
                /*if (_hm.hp <= Phase3HP && _rand.Next(0, 5) < 2)
                {
                    transform.position = new Vector3(transform.position.x, GroundY, transform.position.z);
                    yield return LandSlide();
                    yield break;
                }*/
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

            IEnumerator RegularDash()
            {
                if (!IsFacingPlayer())
                {
                    yield return Turn();
                }

                float dir = FaceHero();
                transform.Find("HyperCut").gameObject.SetActive(false);

                _anim.Play("ZDash");
                transform.position = new Vector3(transform.position.x, GroundY-0.3f, transform.position.z);
                yield return _anim.WaitToFrame(4);
                
                _anim.enabled = false;
                
                yield return new WaitForSeconds(DashDelay);
                PlayAudioClip("ZAudHoriz", _voice);
                
                _anim.enabled = true;
                
                yield return _anim.WaitToFrame(5);
    
                PlayAudioClip("AudDashIntro", _ap);
                if (FastApproximately(_target.transform.position.x, transform.position.x, 5f))
                {
                    yield return StrikeAlternate();
                    transform.position = new Vector3(transform.position.x, GroundY);
                    yield break;
                }

                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 6);
                PlayAudioClip("AudDash", _ap);
                _rb.velocity = new Vector2(-dir * DashXVel, 0f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 9);
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("ZIdle");
                transform.position = new Vector3(transform.position.x, GroundY, transform.position.z);
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
            yield return FastApproximately(MIDDLE, transform.position.x, 8.5f) ? Dash() : RegularDash();
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
                OWBossManager.PlayMusic(null);
                OWBossManager.PlayMusic(FiveKnights.Clips["ZP2Intro"]);
                yield return new WaitForSecondsRealtime(14.12f);
                OWBossManager.PlayMusic(FiveKnights.Clips["ZP2Loop"]);
            }
            else
            {
                GGBossManager.Instance.PlayMusic(null);
                GGBossManager.Instance.PlayMusic(FiveKnights.Clips["ZP2Intro"], 1f);
                yield return new WaitForSecondsRealtime(14.12f);
                GGBossManager.Instance.PlayMusic(FiveKnights.Clips["ZP2Loop"], 1f);
            }
            
        }

        private bool _isKnockingOut;

        private IEnumerator EndPhase1(bool firstDeath)
        {
            float dir;
            yield return KnockedOut();

            IEnumerator KnockedOut()
            {
                _isKnockingOut = true;
                float knockDir = Math.Sign(transform.position.x - HeroController.instance.transform.position.x);
                dir = -FaceHero();
                _rb.gravityScale = 1.5f;
                _rb.velocity = new Vector2(knockDir * 15f, 20f);
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
                transform.position = new Vector3(transform.position.x, GroundY - deathGndOffset);
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
                    _anim.enabled = false;
                    _bc.enabled = true;
                    _rb.isKinematic = true;
                    isHit = false;
                    float t = firstDeath ? StunTimeFirst : StunTimeRest;
                    yield return new WaitSecWhile(() => !isHit, t);
                    _anim.enabled = true;
                    yield return Recover();
                }
            }

            IEnumerator Recover()
            {
                yield return _anim.PlayBlocking("ZRecover");
                
                float t = firstDeath ? RecoveryReturnFirstDelay : RecoveryReturnRestDelay;

                yield return LeaveTemp(dir, t);
                
                if (firstDeath) StartCoroutine(MusicControl());

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
                float sig = Mathf.Sign(transform.localScale.x);
                yield return RageCombo(sig, true, _spinType);
                StartCoroutine(Attacks());
            }
        }
        
        IEnumerator LeaveTemp(float dir, float delay)
        {
            _anim.PlayAt("ZThrow2B", 0);

            //_anim.speed = LeaveAnimSpeed;
            yield return _anim.WaitToFrame(5);
            transform.position += new Vector3(dir * LeaveOffset.x / 2, LeaveOffset.y / 2);
            
            Spring(false, transform.position + new Vector3(dir * LeaveOffset.x, LeaveOffset.y,0f),1.5f);
            
            yield return _anim.PlayToEnd();
            //_anim.speed = 1f;
                
            ToggleZemer(false, false);
            
            yield return new WaitForSeconds(delay);
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
            if (slam.transform.Find("slash_core").Find("hurtbox") != null)
            {
                slam.transform.Find("slash_core").Find("hurtbox").gameObject.SetActive(false);
            }

            var pc = slam.transform.Find("slash_core").Find("Test2").GetComponent<PolygonCollider2D>();
            var off = pc.offset;
            off.y = slam.transform.Find("slash_core").Find("hurtbox").GetComponent<PolygonCollider2D>().offset.y - 10f;
        }

        private IEnumerator DoSpiralPassage()
        {
            // We want to spawn Zemer on an opposite end of the arena => Done by caller
            // We then play her Z6LaserSpin animation while she moves across the arena
            // When this ends, we pause on frame 10 and spawn slashes on player (SpawnSlashes)
            // Repeat this 3 times
            //      Potentially overlap the occurrence so multiple spirals appear at once?
            // The third iteration will not track player, and will be huge at the center of arena
            
            // After the last spiral starts, Zemer begins charging by going through Z6LaserSpin in place
            // This results in spirals spawning on her
            // She then proceeds to play ZSpin (waiting a little on frame 2)
            // Jumps in the air in an arc then descends linearly to where the player was at the start of jump
            
            // Next, have her disperse spiral with a traitor lord slash
            
            GameObject grass = Instantiate(FiveKnights.preloadedGO["TraitorSlam"].transform.Find("Grass").gameObject, transform, true);
            grass.SetActive(false);
            grass.transform.parent = transform;
            
            var ciel = Instantiate(FiveKnights.preloadedGO["Ceiling Dust"].gameObject);
            ciel.SetActive(false);

            yield return PassageAcrossArena(4);

            yield return SomersaultAntic();
            
            IEnumerator PassageAcrossArena(int numTimes)
            {
                while (numTimes > 0)
                {
                    numTimes--;
                    _anim.Play("Z6LaserSpin", -1, 0f);
                    _anim.speed = 1.75f;
                    yield return null;
                    _anim.enabled = false;
                    yield return new WaitForSeconds(0.1f);
                    PlayAudioClip("ZAudAtt" + _rand.Next(1,7), _voice);
                    _anim.enabled = true;
                    _rb.velocity = Vector2.zero;
                    transform.position = new Vector3(transform.position.x, GroundY - 1.2f);
                    float dir = FaceHero(false, true, MIDDLE);
                    yield return null;
                    yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                    PlayAudioClip("AudBasicSlash1",_ap);
                    yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                    _rb.velocity = new Vector2(dir * 80f, 0f);
                    _anim.speed = 1.75f;
                    grass.SetActive(true);
                    grass.GetComponent<ParticleSystem>().Play();
                    yield return new WaitWhile(() => _anim.GetCurrentFrame() < 8);
                    _anim.enabled = false;
                    _anim.speed = 1f;

                    yield return WaitByVelocity(2.5f);
                
                    grass.GetComponent<ParticleSystem>().Stop();
                    _rb.velocity = Vector2.zero;
                    _anim.enabled = true;
                    yield return new WaitWhile(() => _anim.GetCurrentFrame() < 10);
                    _anim.enabled = false;
                    yield return new WaitForSeconds(0.2f);
                    
                    StartCoroutine(numTimes == 0
                        ? SpawnSpirals(new Vector2(MIDDLE, GroundY), 0.9f)
                        : SpawnSpirals(_target.transform.position, 1.2f));

                    yield return new WaitForSeconds(0.05f);
                    _anim.enabled = true;
                    yield return _anim.PlayToEnd();    
                }
            }

            IEnumerator SomersaultAntic()
            {
                GameObject controller = Instantiate(FiveKnights.preloadedGO["SlashRingController"]);
                controller.transform.localScale *= 0.65f;
                controller.SetActive(true);
                
                _anim.speed = 1f;
                transform.position = new Vector3(transform.position.x, GroundY - 1.2f);
                GameCameras.instance.cameraShakeFSM.FsmVariables.FindFsmBool("RumblingMed").Value = true;
                _anim.Play("Z6LaserSpin", -1, 0f);
                yield return null;
                _anim.enabled = false;
                yield return new WaitForSeconds(0.7f);
                PlayAudioClip("ZAudLaser", _voice);
                FaceHero(false, false, MIDDLE);

                GameCameras.instance.cameraShakeFSM.FsmVariables.FindFsmBool("RumblingMed").Value = false;
                _anim.enabled = true;
                
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                //_anim.speed = 1.5f;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
                StartCoroutine(PlayExtendedSpiral(controller, 1.3f));
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 10);
                _anim.enabled = false;
                FaceHero(false, true, MIDDLE);
                yield return new WaitForSeconds(0.6f);
                _anim.enabled = true;
                yield return _anim.PlayToEnd();

                for (int i = 0; i < 2; i++)
                {
                    float signX = Mathf.Sign(gameObject.transform.GetPositionX() - MIDDLE);
                    Vector3 pScale = gameObject.transform.localScale;
                    gameObject.transform.localScale = new Vector3(Mathf.Abs(pScale.x) * signX, pScale.y, 1f);
                    _anim.Play("Z5LandSlide", -1, 0f);
                    yield return null;
                    yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                    _rb.velocity = new Vector2(-signX * 80f, 0f);
                    _anim.enabled = false;
                    grass.SetActive(true);
                    grass.GetComponent<ParticleSystem>().Play();
                    yield return null;

                    // going right
                    if (-signX > 0)
                    {
                        float xStop = 0.7f * (MIDDLE - LeftX) + LeftX;

                        yield return new WaitWhile(() => transform.position.x < xStop);
                        _anim.enabled = true;
                        _anim.PlayAt("Z4AirSweep", 6);
                        yield return new WaitForSeconds(0.1f);
                        _anim.PlayAt("ZSpin", 4);
                        _rb.velocity = new Vector2(_rb.velocity.x, 30f);
                        yield return new WaitForSeconds(0.1f);
                        _anim.enabled = false;
                    }
                    else
                    {
                        float xStop = 0.7f * (RightX - MIDDLE) + MIDDLE;

                        yield return new WaitWhile(() => transform.position.x > xStop);
                        _anim.enabled = true;
                        _anim.PlayAt("Z4AirSweep", 6);
                        yield return new WaitForSeconds(0.1f);
                        _anim.PlayAt("ZSpin", 4);
                        _rb.velocity = new Vector2(_rb.velocity.x, 30f);
                        yield return new WaitForSeconds(0.1f);
                        _anim.enabled = false;
                    }
                    
                    yield return WaitByVelocity(0.5f);
                    ciel.SetActive(true);
                    ciel.GetComponent<ParticleSystem>().Play();
                    ciel.transform.position = new Vector3(MIDDLE, 39.5f);
                    PlayAudioClip(i % 2 == 0 ? "breakable_wall_hit_1" : "breakable_wall_hit_2", _ap);
                    GameCameras.instance.cameraShakeFSM.SendEvent("BigShake");
                    grass.GetComponent<ParticleSystem>().Stop();

                    _anim.PlayAt("ZSpin", 14);
                    _anim.enabled = true;
                    _rb.isKinematic = false;
                    _rb.gravityScale = 2f;
                    _rb.velocity = new Vector2(signX * 8f, 0f);
                    
                    yield return new WaitWhile(() => _anim.IsPlaying());
                    yield return new WaitWhile(() => transform.position.y > GroundY - 0.3f);
                    transform.position = new Vector3(transform.position.x, GroundY - 0.95f);
                    _rb.gravityScale = 0f;
                    _rb.isKinematic = true;
                    _rb.velocity = Vector2.zero;

                    _anim.PlayAt("Z5LandSlide",3);
                    yield return null;
                    yield return new WaitWhile(() => _anim.IsPlaying());
                    yield return new WaitForSeconds(0.25f);
                }
                
                yield return SpinDashInAir(controller);
            }
            
            IEnumerator SpinDashInAir(GameObject controller)
            {
                float dir = FaceHero(false, false, MIDDLE);
                _anim.Play("ZSpin", -1, 0f);
                transform.position = new Vector3(transform.position.x, GroundY);
                PlayAudioClip("ZAudAtt" + _rand.Next(1,7), _voice);
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                _anim.enabled = false;
                yield return new WaitForSeconds(0.2f);
                _anim.enabled = true;
                PlayAudioClip("AudDashIntro",_ap);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
                
                Vector2 tarPos = _target.transform.position;
                Vector2 p1 = transform.position;
                Vector2 p2;
                Vector2 p3;
                
                if (dir > 0)
                {
                    p2 = new Vector2(RightX - (RightX - LeftX) * 0.49f, GroundY + 16.2f);
                    p3 = new Vector2(LeftX + (RightX - LeftX) * 0.15f, GroundY + 8.2f);
                }
                else
                {
                    p2 = new Vector2(LeftX + (RightX - LeftX) * 0.49f, GroundY + 16.2f);
                    p3 = new Vector2(RightX - (RightX - LeftX) * 0.15f, GroundY + 8.2f);
                }

                float timePass = 0f;
                float duration = 0.5f;
                while (timePass < duration)
                {
                    transform.position = QuadraticBezierInterp(p1, p2, p3, timePass / duration);
                    timePass += Time.deltaTime;
                    if (timePass / duration < 0.5f &&  _anim.GetCurrentFrame() >= 4) _anim.enabled = false;
                    if (timePass / duration > 0.55f &&  _anim.GetCurrentFrame() < 5) _anim.enabled = true;
                    if (timePass / duration > 0.55f &&  _anim.GetCurrentFrame() >= 5) _anim.enabled = false;
                    yield return null;
                }
                transform.position = p3;
                
                _anim.enabled = true;
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 6);

                yield return LandToPlayer(controller, tarPos);
            }

            IEnumerator LandToPlayer(GameObject controller, Vector2 tarPos)
            {
                int side = 1;
                FaceHero();
                Vector2 p3 = transform.position;
                _anim.PlayAt("Z4AirSweep", 1);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
               
                
                Vector2 diff = p3 - tarPos;
                float rot = Mathf.Atan(diff.y / diff.x);
                if (side > 0 && p3.x > tarPos.x) rot += Mathf.PI;
                Debug.Log($"Rot is {rot * Mathf.Rad2Deg}");
                _rb.velocity = new Vector2(70f * Mathf.Cos(rot), 70f * Mathf.Sin(rot));

                yield return new WaitWhile(() => transform.position.y > GroundY + 2.5f && _anim.GetCurrentFrame() < 4 && CheckIfStuck());
                _anim.enabled = false;
                yield return new WaitWhile(() => transform.position.y > GroundY + 2.5f && CheckIfStuck());
                _anim.enabled = true;
                yield return new WaitWhile(() => transform.position.y > GroundY - 0.3 && _anim.GetCurrentFrame() < 6 && CheckIfStuck());
                _anim.enabled = false;
                yield return new WaitWhile(() => transform.position.y > GroundY - 0.3f && CheckIfStuck());
                _anim.enabled = true;
                transform.position = new Vector3(transform.position.x, GroundY - 0.3f);
                _rb.gravityScale = 0f;
                _rb.isKinematic = true;
                _rb.velocity = Vector2.zero;

                bool CheckIfStuck()
                {
                    if (_rb.velocity.x.Within(0f, 0.5f))
                    {
                        _rb.gravityScale = 2f;
                        _rb.isKinematic = false;
                    }
                    return true;
                }
                
                ciel.SetActive(true);
                ciel.GetComponent<ParticleSystem>().Play();
                ciel.transform.position = new Vector3(MIDDLE, 39.5f);
                PlayAudioClip("breakable_wall_hit_1", _ap);
                GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
                
                yield return new WaitWhile(() => _anim.IsPlaying());
                yield return new WaitForSeconds(0.1f);

                yield return Disperse(controller);
            }

            IEnumerator Disperse(GameObject controller)
            {
                if (!IsFacingPlayer())
                    yield return Turn();

                float dir = FaceHero();

                _anim.Play("ZAtt2");
                PlayAudioClip("ZAudAtt" + _rand.Next(2,5), _voice);
                _anim.speed = 2f;
                yield return null;
                PlayAudioClip("AudBasicSlash1",_ap);
                
                // Lerp small
                StartCoroutine(LerpSizeChange(controller.transform, 0.2f, 0.5f));

                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);

                PlayAudioClip("AudBasicSlash2",_ap);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
                
                // Lerp big
                StartCoroutine(LerpSizeChange(controller.transform, 0.1f, 2.2f));
                // 4 way flash
                GameObject fxOrig = FiveKnights.preloadedGO["HornetSphere"].transform.Find("Flash Effect").gameObject;
                foreach (float i in new [] {0f, 90f, 180f, 270f})
                {
                    var fx = Instantiate(fxOrig);
                    fx.transform.SetRotationZ(i + UnityEngine.Random.Range(10, 30));
                    fx.transform.position = controller.transform.position;
                    fx.transform.parent = controller.transform;
                    fx.SetActive(true);
                    var fsm = fx.LocateMyFSM("FSM");
                    fsm.enabled = true;
                    fsm.FsmVariables.FindFsmFloat("Pause").Value = 1f;
                    fsm.FsmVariables.FindFsmFloat("Rotation").Value = i + UnityEngine.Random.Range(10, 30);
                    fsm.FsmVariables.FindFsmBool("Reset Rotation").Value = false;
                    fsm.SetState("Init");
                }
                GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
                
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 6);
                
                Destroy(controller);
                
                PlayAudioClip("AudBigSlash2",_ap);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 7);
                dir = FaceHero();
                SpawnPillar(dir, Vector2.one, 30f);
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("ZIdle");
                _anim.speed = 1f;
            }
            
            IEnumerator LerpSizeChange(Transform trans, float dur, float scale)
            {
                float lerpDuration = dur;
                Vector2 startValue = trans.localScale;
                Vector2 endValue = trans.localScale * scale;
                float timeElapsed = 0;
                while (timeElapsed < lerpDuration)
                {
                    trans.localScale = Vector2.Lerp(startValue, endValue, timeElapsed / lerpDuration);
                    timeElapsed += Time.deltaTime;
                    yield return null;
                }
                trans.localScale = endValue;
            }

            Vector2 QuadraticBezierInterp(Vector2 p1, Vector2 p2, Vector2 p3, float t)
            {
                Vector2 a = LineLerp(p1, p2, t);
                Vector2 b = LineLerp(p2, p3, t);

                return LineLerp(a, b, t);
        
                Vector2 LineLerp(Vector2 p1, Vector2 p2, float t)
                {
                    float x = Mathf.SmoothStep(p1.x, p2.x, t);
                    float y = Mathf.SmoothStep(p1.y, p2.y, t);

                    return new Vector2(x, y);
                }
            }

            // Spawn the spiral slash on top of the player
            IEnumerator SpawnSpirals(Vector2 targ, float scale)
            {
                PlayAudioClip("NeedleSphere",_ap);
                GameObject fxOrig = FiveKnights.preloadedGO["HornetSphere"].transform.Find("Flash Effect").gameObject;
                foreach (float i in new [] {0f, 90f, 180f, 270f})
                {
                    var fx = Instantiate(fxOrig);
                    fx.transform.SetRotationZ(i + UnityEngine.Random.Range(10, 30));
                    fx.transform.position = _target.transform.position;
                    fx.transform.parent = _target.transform;
                    fx.SetActive(true);
                    var fsm = fx.LocateMyFSM("FSM");
                    fsm.enabled = true;
                    fsm.FsmVariables.FindFsmFloat("Pause").Value = 1f;
                    fsm.FsmVariables.FindFsmFloat("Rotation").Value = i + UnityEngine.Random.Range(10, 30);
                    fsm.FsmVariables.FindFsmBool("Reset Rotation").Value = false;
                    fsm.SetState("Init");
                }
                GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
                
                var slash = Instantiate(FiveKnights.preloadedGO["SlashRingControllerNew"]);
                slash.SetActive(true);
                slash.transform.position = targ;
                slash.transform.localScale /= (scale * 2.5f);
                float spd = 2f; // 2f
                StartCoroutine(LerpScale(slash.transform, 2.5f));

                for (int i = 0; i < 3; i++)
                {
                    GameObject spiral = slash.transform.Find($"SlashRing{i}").gameObject;
                    ActivateSpiral(spiral, spd);
                }
        
                // Wait for first set to do non-hitbox part of animation
                Animator oldAnim = slash.transform.Find("SlashRing0").Find("1").gameObject.GetComponent<Animator>();
                yield return new WaitWhile(() => oldAnim.GetCurrentFrame() < 7);


                for (int i = 3; i < 5; i++)
                {
                    GameObject spiral = slash.transform.Find($"SlashRing{i}").gameObject;
                    ActivateSpiral(spiral, spd * 1.8f);
                }

                System.Random rnd = new System.Random();
                int[] randSlashes = new [] {5, 6, 7}.OrderBy(x => rnd.Next()).ToArray();
                GameObject lastSpiral = null;
                
                foreach (int i in randSlashes)
                {
                    GameObject spiral = slash.transform.Find($"SlashRing{i}").gameObject;
                    ActivateSpiral(spiral, spd * 2f);
                    lastSpiral = spiral;
                    yield return new WaitForSeconds(rnd.Next(5, 10) * 0.01f);
                }

                oldAnim = lastSpiral.Find("1").gameObject.GetComponent<Animator>();
                yield return new WaitWhile(() => oldAnim.GetCurrentFrame() < 8);

                Transform tBlast = FiveKnights.preloadedGO["Blast"].transform;
                var middle = Instantiate(tBlast.Find("Particle middle").gameObject);
                middle.transform.position = slash.transform.position;
                middle.SetActive(true);
                //middle.transform.localScale *= 2f;

                yield return new WaitWhile(() => oldAnim.GetCurrentFrame() < 10);
                yield return LerpScale2(slash.transform);
                StartCoroutine(LerpOpacity(slash.transform));

                // Terrible way to check if animation is over
                foreach (Transform t in slash.transform)
                {
                    foreach (var anim in t.GetComponentsInChildren<Animator>(true))
                    {
                        yield return anim.PlayToEnd();
                    }
                }


                Destroy(slash);

                IEnumerator LerpScale(Transform trans, float scale)
                {
                    float lerpDuration = (5f / 12f) / 1.8f;
                    Vector2 startValue = trans.localScale;
                    Vector2 endValue = trans.localScale * scale;
                    float timeElapsed = 0;
                    while (timeElapsed < lerpDuration)
                    {
                        trans.localScale = Vector2.Lerp(startValue, endValue, timeElapsed / lerpDuration);
                        timeElapsed += Time.deltaTime;
                        yield return null;
                    }
                    trans.localScale = endValue;
                }
                
                IEnumerator LerpScale2(Transform trans)
                {
                    float lerpDuration = 0.1f;
                    Vector2 startValue = trans.localScale;
                    Vector2 endValue = trans.localScale * 0.7f;
                    float timeElapsed = 0;
                    while (timeElapsed < lerpDuration)
                    {
                        trans.localScale = Vector2.Lerp(startValue, endValue, timeElapsed / lerpDuration);
                        timeElapsed += Time.deltaTime;
                        yield return null;
                    }
                    trans.localScale = endValue;
                }
                
                IEnumerator LerpOpacity(Transform trans)
                {
                    float lerpDuration = 0.1f;
                    float startValue = 1f;
                    float endValue = 0f;
                    float timeElapsed = 0;
                    while (timeElapsed < lerpDuration)
                    {
                        float a = Mathf.Lerp(startValue, endValue, timeElapsed / lerpDuration);
                        foreach (Transform t in trans)
                        {
                            foreach (var sr in t.GetComponentsInChildren<SpriteRenderer>(true))
                            {
                                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, a);
                            }
                        }
                        timeElapsed += Time.deltaTime;
                        yield return null;
                    }
                    foreach (SpriteRenderer sr in trans.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0f);
                    }
                }
                
                void ActivateSpiral(GameObject spiral, float spd)
                {
                    spiral.SetActive(true);
                    var animOrig = spiral.GetComponent<Animator>();
                    animOrig.speed = spd;
                    animOrig.Rebind();
                    animOrig.Update(0f);
                    foreach (var anim in spiral.GetComponentsInChildren<Animator>(true))
                    {
                        anim.Rebind();
                        anim.Update(0f);
                        anim.speed = spd;
                    }
                }
            }

            IEnumerator PlayExtendedSpiral(GameObject controller, float spd)
            {
                Random rnd = new Random();
                int[] randSlashes = new [] {0, 1, 2, 3, 4}.OrderBy(x => rnd.Next()).ToArray();

                PlayAudioClip("NeedleSphere",_ap);
                foreach (var i in randSlashes)
                {
                    GameObject ring = controller.Find($"SlashRing{i}").gameObject;
                    ring.SetActive(false);
                }
                
                GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
                controller.transform.localScale /= 2.5f;
                StartCoroutine(LerpScale(controller.transform, 2.5f));
                
                GameObject fxOrig = FiveKnights.preloadedGO["HornetSphere"].transform.Find("Flash Effect").gameObject;
                foreach (float i in new [] {0f, 90f, 180f, 270f})
                {
                    var fx = Instantiate(fxOrig);
                    fx.transform.SetRotationZ(i + UnityEngine.Random.Range(10, 30));
                    fx.transform.position = transform.position;
                    fx.transform.parent = transform;
                    fx.SetActive(true);
                    var fsm = fx.LocateMyFSM("FSM");
                    fsm.enabled = true;
                    fsm.FsmVariables.FindFsmFloat("Pause").Value = 1f;
                    fsm.FsmVariables.FindFsmFloat("Rotation").Value = i + UnityEngine.Random.Range(10, 30);
                    fsm.FsmVariables.FindFsmBool("Reset Rotation").Value = false;
                    fsm.SetState("Init");
                }
 
                foreach (var i in randSlashes)
                {
                    GameObject ring = controller.Find($"SlashRing{i}").gameObject;
                    ring.SetActive(true);
                    Animator arc1 = ring.transform.Find("1").GetComponent<Animator>();
                    Animator arc2 = ring.transform.Find("2").GetComponent<Animator>();
                    StartCoroutine(ActivateRing(ring, arc1, arc2));
                    StartCoroutine(FollowZemer(controller.transform));
                    yield return new WaitForSeconds(rnd.Next(15, 30) * 0.01f); //0.15f
                }
                
                IEnumerator LerpScale(Transform trans, float scale)
                {
                    float lerpDuration = 0.4f;
                    Vector2 startValue = trans.localScale;
                    Vector2 endValue = trans.localScale * scale;
                    float timeElapsed = 0;
                    while (timeElapsed < lerpDuration)
                    {
                        trans.localScale = Vector2.Lerp(startValue, endValue, timeElapsed / lerpDuration);
                        timeElapsed += Time.deltaTime;
                        yield return null;
                    }
                    trans.localScale = endValue;
                }

                IEnumerator FollowZemer(Transform spiral)
                {
                    while (spiral != null)
                    {
                        spiral.position = transform.position;
                        yield return null;
                    }
                }
        
                IEnumerator ActivateRing(GameObject ring, Animator a1, Animator a2)
                {
                    ring.SetActive(true);
                    a1.enabled = true;
                    a1.gameObject.SetActive(true);
                    a1.Play("NewSlash3Antic", -1, 0f);
                    a1.speed = spd;
                    a2.enabled = false;
                    a2.gameObject.SetActive(false);
                    
                    yield return null;
                    yield return new WaitWhile(() => a1.GetCurrentFrame() < 4);

                    StartCoroutine(LoopAnimation(a1, "NewSlash3Loop"));
                    a2.enabled = true;
                    a2.speed = spd;
                    a2.gameObject.SetActive(true);
                    a2.Play("NewSlash3Antic", -1, 0f);
                    
                    yield return null;
                    yield return new WaitWhile(() => a2.GetCurrentFrame() < 4);

                    StartCoroutine(LoopAnimation(a2, "NewSlash3Loop"));

                    IEnumerator LoopAnimation(Animator anim, string name)
                    {
                        while (anim != null)
                        {
                            anim.Play(name, -1, 0f);
                            anim.speed = spd;
                            yield return null;
                            yield return new WaitWhile(() => anim != null && anim.GetCurrentFrame() < 5);
                        }
                    }
                }
            }
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

            IEnumerator Pattern4()
            {
                Transform slash = transform.Find("NewSlash3");
                slash.gameObject.SetActive(true);
                int sc = scaleOffset ? 1 : -1;
                slash.localScale = new Vector3(sc * Mathf.Abs(slash.localScale.x),slash.localScale.y,slash.localScale.z);
                slash.localPosition = new Vector3(sc < 0 ? 18.2f : -16f, 3.5f,
                    slash.localPosition.z);
                scaleOffset = !scaleOffset;
                
                yield return _anim.WaitToFrame(3);
                
                foreach (Transform i in slash)
                {
                    StartCoroutine(Test(i.GetComponent<Animator>()));
                }
	
                Log($"Wait to frame 6, is curr at frame {_anim.GetCurrentFrame()}");
                yield return _anim.WaitToFrame(10);
                Log("Wait to frame 7");

                foreach (Transform i in slash)
                {
                    var anim = i.GetComponent<Animator>();
                    anim.speed = 1.2f;
                    anim.enabled = true;
                }
                Log("Wait to finished");
                foreach (Transform i in slash)
                {
                    var anim = i.GetComponent<Animator>();
                    yield return anim.PlayToEnd();
                }
                Log("Finished");
                
                foreach (Transform i in slash)
                {
                   i.gameObject.SetActive(false);
                }
	
                slash.gameObject.SetActive(false);
            }

            IEnumerator Test(Animator anim)
            {
                anim.gameObject.SetActive(true);
                Log($"Wait to frame 3:");
                anim.PlayAt("NewSlash3", 0);
                Log("Wait to frame 4");
                anim.speed = 1f;
                yield return null;
                anim.enabled = true;
                yield return anim.WaitToFrame(3);
                Log("Wait to frame 5");
                anim.enabled = false;
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
            StartCoroutine(Pattern4());
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
                    StopCoroutine(_counterRoutine);
                    StartCoroutine(Countered());
                    return;
                }
            }

            orig(self, hitInstance);
        }

        private IEnumerator Countered()
        {
            _hm.IsInvincible = false;
            On.HealthManager.Hit -= OnBlockedHit;
            yield return _anim.PlayToEndWithActions("ZCAtt",
                (3, () => PlayAudioClip("Slash", _ap,0.85f, 1.15f))
            );
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
                // TODO Disabled second spin type
                /*if (_spinType == 1 && _hm.hp <= Phase3HP)
                {
                    _spinType = 3;
                }*/
                
                if ((DoneFrenzyAtt == 0 && _hm.hp < 0.65f * Phase2HP) || 
                    (DoneFrenzyAtt == 1 && _hm.hp < 0.35f * Phase2HP))
                {
                    Log($"Doing frenzy p{DoneFrenzyAtt}");
                    StopAllCoroutines();
                    
                    
                    foreach (var i in FindObjectsOfType<Rigidbody2D>(true))
                    {
                        if (i.name.Contains("Nail") && i.transform.parent == null)
                        {
                            Log("Destroyed nail!!!!");
                            Destroy(i.gameObject);
                        }
                    }

                    FaceHero();
                    _bc.enabled = false;
                    _anim.enabled = true;
                    _rb.velocity = Vector2.zero;
                    _rb.gravityScale = 0f;
                    DoneFrenzyAtt++;
                    
                    StartCoroutine(EndPhase1(false));
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
            // wtf u talking about old me???
            foreach (var i in FindObjectsOfType<Rigidbody2D>(true))
            {
                if (i.name.Contains("Nail") && i.transform.parent == null)
                {
                    Log("Destroyed nail!!!!");
                    Destroy(i.gameObject);
                }
            }

            if (OWArenaFinder.IsInOverWorld ) OWBossManager.PlayMusic(null);
            else GGBossManager.Instance.PlayMusic(null, 1f);
            _deathEff.RecordJournalEntry();

            _isKnockingOut = true;
            float knockDir = Math.Sign(transform.position.x - HeroController.instance.transform.position.x);
            FaceHero();
            _rb.gravityScale = 1.5f;
            _rb.velocity = new Vector2(knockDir * 15f, 20f);
            PlayDeathFor(gameObject);
            _anim.enabled = true;
            _anim.Play("ZKnocked");
            StartCoroutine(PlayDeathSound());
            _anim.speed = 1f;
            yield return null;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
            _anim.enabled = false;
            yield return new WaitWhile(() => transform.position.y > GroundY - 0.99f);
            _rb.velocity = Vector2.zero;
            _anim.enabled = true;
            _rb.gravityScale = 0f;
            transform.position = new Vector3(transform.position.x, GroundY - deathGndOffset);
            yield return new WaitWhile(() => _anim.IsPlaying());
            _isKnockingOut = false;
            yield return new WaitForSeconds(1.75f);
            CustomWP.wonLastFight = true;
            // Stop music here.
            Log("here");
            Destroy(this);

            IEnumerator PlayDeathSound()
            {
                PlayAudioClip("ZAudP1Death",_voice);
                yield return new WaitForSeconds(FiveKnights.Clips["ZAudP2Death1"].length);
                PlayAudioClip("ZAudP2Death2",_voice);
            }
        }

        private int FaceHero(bool onlyCalc = false, bool opposite = false, float? tarX = null)
        {
            tarX ??= _target.transform.position.x;
            int sign = (int) Mathf.Sign(gameObject.transform.GetPositionX() - tarX.Value);
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

        public void PlayAudioClip(
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
                _anim.enabled = visible;
                _rb.gravityScale = 0f;
                _bc.enabled = visible;
                Color col = _sr.color;
                _sr.color = new Color(col.r, col.g, col.b, visible ? 1f : 0f);
            }

            _bc.enabled = false;
            _anim.enabled = false;
            _anim.speed = 1f;

            if (fade)
                StartCoroutine(Fade());
            else
                Instant();
        }

        public static void SaveTextureAsPNG(Texture2D _texture, string _fullPath)
        {
            byte[] _bytes = _texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(_fullPath, _bytes);
            Debug.Log(_bytes.Length / 1024 + "Kb was saved as: " + _fullPath);
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
                var old = traitorSlam[i].transform.Find("slash_core").Find("hurtbox");
                var cp = Instantiate(old);
                cp.name = "Test2";
                //Destroy(old);
                cp.parent = traitorSlam[i].transform.Find("slash_core").transform;
                cp.transform.position = old.transform.position;
                cp.transform.localScale = old.transform.localScale;
                old.gameObject.SetActive(false);
                traitorSlam[i].SetActive(false);
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

        private IEnumerator WaitByVelocity(float offset)
        {
            yield return null;

            if (_rb.velocity.x < 0)
            {
                yield return new WaitWhile (() => !_rb.velocity.x.Within(0f,0.5f) && transform.position.x > LeftX + offset);
            }
            else 
            {
                yield return new WaitWhile (() => !_rb.velocity.x.Within(0f,0.5f) && transform.position.x < RightX - offset);
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
