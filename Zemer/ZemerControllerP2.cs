using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FiveKnights.BossManagement;
using FiveKnights.Misc;
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
        private GameObject _dd;
        private GameObject[] traitorSlam;
        private int traitorSlamIndex;
        private GameObject _dnailEff;
        private Animator _anim;
        private Rigidbody2D _rb;
        private Random _rand;
        private DamageHero _dh;
        private EnemyHitEffectsUninfected _hitEffects;
        private EnemyDeathEffectsUninfected _deathEff;
        private GameObject _target;
        private ParticleSystem grass;
        private string[] _commonAtt;
        private List<GameObject> _destroyAtEnd;

        private readonly float deathGndOffset = (OWArenaFinder.IsInOverWorld) ? 1.18f : 0.7f;
        private readonly float GroundY = (OWArenaFinder.IsInOverWorld) ? 108.3f :   
            (CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim) ? 9f : 28.8f;
        private readonly float LeftX = (OWArenaFinder.IsInOverWorld) ? 240.1f :
            (CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim) ? 61.0f : 11.2f;
        private readonly float RightX = (OWArenaFinder.IsInOverWorld) ? 273.9f :
            (CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim) ? 91.0f : 45.7f;
        private readonly float SlamY = (OWArenaFinder.IsInOverWorld) ? 105f :
            (CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim) ? 6f : 25.9f;
        private readonly float NailHeightGrab = 
            (CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim) ? 10f : 19f;


        private const int Phase2HP = 1500;
        private int DoneFrenzyAtt;
        private const int Phase3HP = 1100;

        private readonly float NailMaxHeightStop = 
            (CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim) ? 18.5f : 39f;
        private readonly float NailMaxLeftStop = 
            (CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim) ? 62f : 13f;
        private readonly float NailMaxRightStop = 
            (CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim) ? 89f : 43f;
        private const float TurnDelay = 0.05f;
        private const float LandSlideTPInDelay = 0.07f;
        private const float LaserNutsEndDelay = 0.5f; //
        private const float IdleDelay = 0.1f;//0.19f; //0.38
        private const float DashDelay = 0.18f; // 0.18f
        private float MIDDLE; // 
        private const float ThrowDelay = 0.2f;
        private const float SwingOutToInDelay = 0.75f;
        private const float GenericReturnDelay = 0.75f;
        private const float RecoveryReturnFirstDelay = 3.5f;
        private const float RecoveryReturnRestDelay = 0.75f;
        private const float NailSize = 1.15f;
        private const float LeaveAnimSpeed = 2.75f;
        private const float AfterAirNailGrabDelay = 0.4f;
        private const float StunTimeFirst = 1.5f;
        private const float StunTimeRest = 1.2f;
        private const float DashXVel = 75f;

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

        public bool DoPhase;

        private void Awake()
        {
            DoneFrenzyAtt = 0;
            MIDDLE = (RightX + LeftX) / 2f;
            
            OnDestroy();

            On.HealthManager.TakeDamage += HealthManager_TakeDamage;
            On.EnemyDreamnailReaction.RecieveDreamImpact += OnReceiveDreamImpact;
            On.HealthManager.Die += HealthManagerOnDie;

            _hm = GetComponent<HealthManager>();
            _anim = GetComponent<Animator>();

            _bc = GetComponent<BoxCollider2D>();

            _rb = GetComponent<Rigidbody2D>();
            _rb.isKinematic = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            _sr = GetComponent<SpriteRenderer>();

            _dh = GetComponent<DamageHero>();
            _dh.damageDealt = 1;

            _dd = FiveKnights.preloadedGO["WD"];

            _dnailEff = _dd.GetComponent<EnemyDreamnailReaction>().GetAttr<EnemyDreamnailReaction, GameObject>("dreamImpactPrefab");

            _rand = new Random();

            _dnailReac = GetComponent<EnemyDreamnailReaction>();
            _dnailReac.enabled = true;

            _hitEffects = gameObject.GetComponent<EnemyHitEffectsUninfected>();
            _hitEffects.enabled = true;

            _pvFsm = FiveKnights.preloadedGO["PV"].LocateMyFSM("Control");
            
            GameObject grassGO = Instantiate(FiveKnights.preloadedGO["TraitorSlam"].transform.Find("Grass").gameObject, transform, true);
            grass = grassGO.GetComponent<ParticleSystem>();
            grass.gameObject.SetActive(true);
            grass.Stop();

            _destroyAtEnd = new List<GameObject>();

            AssignFields();
        }

        private void HealthManagerOnDie(On.HealthManager.orig_Die orig, HealthManager self, float? attackDirection, AttackTypes attackType, bool ignoreEvasion)
        {
            if(self.gameObject.name.Contains("Zemer")) return;
            orig(self, attackDirection, attackType, ignoreEvasion);
        }

        private IEnumerator Start()
        {
            _hm.hp = Phase2HP;
            _deathEff = _dd.GetComponent<EnemyDeathEffectsUninfected>();
            _deathEff.SetJournalEntry(FiveKnights.journalEntries["Zemer"]);
            _target = HeroController.instance.gameObject;
            
            foreach (var i in FindObjectsOfType<Rigidbody2D>(true))
            {
                if (i.name.Contains("Nail") && i.transform.parent == null)
                {
                    Destroy(i.gameObject);
                }
            }

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
                [FancyAttack] = 0,
                [Dash] = 0,
                [Attack1Base] = 0,
                [AerialAttack] = 0,
                [NailLaunch] = 0,
                [DoubleFancy] = 0,
                [SweepDash] = 0,
                [ZemerSlam] = 0,
                [Attack1Complete] = 0
            };
            
            Dictionary<Func<IEnumerator>, int> max = new Dictionary<Func<IEnumerator>, int>
            {
                [FancyAttack] = 3,
                [Dash] = 2,
                [Attack1Complete] = 2,
                [Attack1Base] = 2,
                [AerialAttack] = 2,
                [NailLaunch] = 2,
                [DoubleFancy] = 2,
                [SweepDash] = 1,
                [ZemerSlam] = 1,
            };

            while (true)
            {
                Log("[Waiting to start calculation]");
                _anim.Play("ZIdle");
                isHit = false;
                yield return new WaitSecWhile(() => !isHit, IdleDelay);
                
                /*float sig = Mathf.Sign(transform.localScale.x);
                yield return RageCombo(sig, true);
                yield return null;
                continue;*/

                Log("[End of Wait]");

                Vector2 posZem = transform.position;
                Vector2 posH = _target.transform.position;

                _dh.damageDealt = 1;

                if (posH.y > GroundY + 9f && (posH.x <= LeftX || posH.x >= RightX))
                {
                    yield return SpinAttack();
                }
                else if (FastApproximately(posZem.x, posH.x, 5f) && 
                         !((DoneFrenzyAtt == 0 && _hm.hp < 0.65f * Phase2HP) || (DoneFrenzyAtt == 1 && _hm.hp < 0.35f * Phase2HP)))
                {
                    int r = _rand.Next(0, 4); //0 1 2 3
                    if (r == 0 && counterCount < 2)
                    {
                        counterCount++;
                        Log("Doing Counter");
                        ZemerCounter();
                        _countering = true;
                        yield return new WaitWhile(() => _countering);
                        Log("Done Counter");
                    }
                    else
                    {
                        counterCount = 0;
                        Log("Dodge");
                        yield return Dodge();
                        Log("End Dodge");
                        var lst = new List<Func<IEnumerator>> {Dash, FancyAttack, NailLaunch, null};
                        var att = MiscMethods.ChooseAttack(lst, rep, max);
                        if (att != null)
                        {
                            Log("Doing " + att.Method.Name);
                            yield return att;
                            Log("Doing " + att.Method.Name);
                        }
                    }
                }

                List<Func<IEnumerator>> attLst = new List<Func<IEnumerator>> { NailLaunch, null, null };
                Func<IEnumerator> currAtt = null;
                
                if (!FastApproximately(posZem.x, posH.x, 10f)) currAtt = attLst[_rand.Next(0, attLst.Count)];

                if (currAtt != null)
                {
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
                    
                    currAtt = MiscMethods.ChooseAttack(attLst, rep, max);
                    
                    Log("Doing " + currAtt.Method.Name);
                    yield return currAtt();
                    Log("Done " + currAtt.Method.Name);

                    if (currAtt == Attack1Base)
                    {
                        List<Func<IEnumerator>> lst2 = FastApproximately(transform.position.x, _target.transform.position.x, 7f) ? 
                            new List<Func<IEnumerator>> {Attack1Complete} : 
                            new List<Func<IEnumerator>> {Attack1Complete, FancyAttack, FancyAttack};

                        currAtt = MiscMethods.ChooseAttack(lst2, rep, max);
                        Log("Doing " + currAtt.Method.Name);
                        yield return currAtt();
                        Log("Done " + currAtt.Method.Name);
                    
                        if (currAtt == FancyAttack && _rand.Next(0,3) < 2)
                        {
                            Log("Doing Special Fancy Attack");
                            yield return Dodge();
                            yield return new WaitForSeconds(TwoFancyDelay);
                            if (UnityEngine.Random.Range(0, 4) == 0)
                            {
                                Log("Doing Special Fancy Attack Nail Launch version");
                                yield return NailLaunch();
                            }
                            else
                            {
                                yield return FancyAttack();
                                yield return Dash();
                            }
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
                
                PlayAudioClip(ZemRandAudio.PickRandomZemAud(1, 6));
                
                yield return _anim.WaitToFrame(8);

                _rb.velocity = new Vector2(xVel * 35f, 18f);
                _rb.gravityScale = 1.3f;
                _rb.isKinematic = false;

                yield return new WaitForSeconds(0.1f);
                yield return _anim.WaitToFrame(10);
                PlayAudioClip("AudBigSlash2",0.15f);
                yield return _anim.WaitToFrame(13);
                PlayAudioClip("AudBigSlash2",0.15f);
                yield return new WaitWhile(() => transform.position.y > GroundY);

                _rb.velocity = Vector2.zero;
                _rb.gravityScale = 0f;
                _rb.isKinematic = true;

                transform.position = new Vector2(transform.position.x, GroundY);

                yield return _anim.PlayToEnd();

                _anim.Play("ZIdle");
            }

            _lastAtt = AerialAttack;
            
            yield return Attack();
        }
        
        private float GetAngleTo(Vector2 from, Vector2 to)
        {
            float num = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;
            while ((double) num < 0.0) num += 360f;
            return num;
        }
        
        private IEnumerator NailLaunch()
        {
            float dir = 0f;
            bool doSlam = false;

            IEnumerator Throw()
            {
                Vector2 hero = _target.transform.position;
                
                // If player is too close dodge back or if too close to wall as well dash forward
                if (hero.x.Within(transform.position.x, 8f))
                {
                    // Too close to wall
                    if (transform.position.x.Within(LeftX, 6f) || transform.position.x.Within(RightX, 6f))
                    {
                        yield return Dash();
                    }
                    else
                    {
                        yield return Dodge();
                    }
                }
                
                dir = FaceHero();
                float rot;
                _anim.Play("ZThrow1");
                PlayAudioClip(ZemRandAudio.PickRandomZemAud(1, 6));
                yield return _anim.WaitToFrame(2);
                _anim.enabled = false;
                yield return new WaitForSeconds(ThrowDelay / 2f);
                hero = _target.transform.position;
                dir = FaceHero();
                yield return FlashRepeat(hero, ThrowDelay / 2f, true);
                _anim.enabled = true;
                yield return _anim.WaitToFrame(3); 

                rot = GetAngleTo(transform.Find("ZNailB").position,  hero) * Mathf.Deg2Rad;
                float rotArm = rot + (dir > 0 ? Mathf.PI : 0f);

                GameObject arm = transform.Find("NailHand").gameObject;
                GameObject nailPar = Instantiate(transform.Find("ZNailB").gameObject);
                Rigidbody2D parRB = nailPar.GetComponent<Rigidbody2D>();
                arm.transform.SetRotation2D(rotArm * Mathf.Rad2Deg);
                nailPar.transform.SetRotation2D(rotArm * Mathf.Rad2Deg);
                nailPar.transform.position = transform.Find("ZNailB").position;
                nailPar.transform.localScale = new Vector3(Mathf.Sign(transform.localScale.x) * NailSize, NailSize, NailSize);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
                yield return new WaitForSeconds(0.07f);
                nailPar.SetActive(true);
                // TODO might want to readjust speed
                float velmag = 80f; //hero.y < GroundY + 2f ? 70f : 70f;
                parRB.velocity = new Vector2(Mathf.Cos(rot) * velmag, Mathf.Sin(rot) * velmag);
                nailPar.AddComponent<ExtraNailBndCheck>();
                yield return new WaitForSeconds(0.01f);

                var cc = nailPar.transform.Find("ZNailC").gameObject.AddComponent<CollisionCheck>();
                cc.Freeze = true;
                cc.OnCollide += () =>
                {
                    Log($"Collision debug 1: {nailPar.GetComponent<Rigidbody2D>().velocity}");
                    PlayAudioClip("AudLand");
                    GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
                    nailPar.GetComponent<SpriteRenderer>().enabled = false;
                    nailPar.transform.Find("ZNailN").GetComponent<SpriteRenderer>().enabled = true;
                    nailPar.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
                    Destroy(cc);
                };
                yield return new WaitWhile(() => _anim.IsPlaying());
                bool isTooHigh = nailPar.transform.position.y > GroundY + 1f;
                yield return isTooHigh ? LaunchUp(rot, nailPar, false, 0) : LaunchSide(nailPar);
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
            
            IEnumerator LaunchUp(float rot, GameObject nail, bool didFakeOut, int repeatCnt)
            {
                // float rotVel = dir > 0 ? rot + 180 * Mathf.Deg2Rad : rot;
                GameObject col = nail.transform.Find("ZNailC").gameObject;
                CollisionCheck cc = col.GetComponent<CollisionCheck>() ?? col.AddComponent<CollisionCheck>();
                Rigidbody2D rb = nail.GetComponent<Rigidbody2D>();

                if (repeatCnt == 0) yield return GndLeave(dir, true);

                cc.Hit = rb.velocity == Vector2.zero;
                yield return new WaitWhile(() => nail.transform.position.y < NailHeightGrab && !cc.Hit && rb.velocity != Vector2.zero);
                var pos = nail.transform.position + new Vector3(5f * Mathf.Cos(rot), 5f * Mathf.Sin(rot), 0f);
                if (pos.y > NailMaxHeightStop) pos.y = NailMaxHeightStop;
                if (pos.x > NailMaxRightStop) pos.x = NailMaxRightStop;
                if (pos.x < NailMaxLeftStop) pos.x = NailMaxLeftStop;
                transform.position = pos;
                float oldPlayerPosX = _target.transform.position.x;
                ToggleZemer(true);
                dir = FaceHero();
                _anim.speed = 2f;
                _anim.Play("ZThrow3Air", -1, 0f);
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                nail.SetActive(false);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                PlayAudioClip(ZemRandAudio.PickRandomZemAud(1, 6));
                _anim.speed = 1f;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
                _anim.enabled = false;
                yield return new WaitForSeconds(AfterAirNailGrabDelay / 2f);
                Vector2 hero = _target.transform.position;
                Vector2 zem = gameObject.transform.position;
                
                // Check if
                //      hasn't done this once already
                //      player changed sides from zem
                //      player is close to zemer
                if (!didFakeOut && 
                    ((oldPlayerPosX - zem.x) * (hero.x - zem.x) < 0f ||
                     zem.x.Within(hero.x, 6f)))
                {
                    // Teleport to the other side from the player
                    float tpToX = hero.x < MIDDLE ? (MIDDLE + RightX) / 2f : (LeftX + MIDDLE) / 2f;
                    didFakeOut = true;
                    repeatCnt+=10;
                    Spring(false, transform.position, 1.6f);
                    yield return new WaitForSeconds(0.1f);
                    ToggleZemer(false);
                    transform.position = new Vector3(tpToX, zem.y);
                    Spring(true, transform.position);
                    yield return new WaitForSeconds(0.15f);
                    dir = FaceHero();
                    hero = _target.transform.position - new Vector3(0f, 3f, 0f);
                    zem = gameObject.transform.position;
                    ToggleZemer(true);
                }
                
                // If we've already done air attack twice then just aim for the ground so we dont loop forever
                if (repeatCnt > 1)
                {
                    hero = new Vector2(hero.x, GroundY);
                }

                yield return FlashRepeat(hero, AfterAirNailGrabDelay / 3f, true);
                _anim.enabled = true;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 5);
                nail.SetActive(true);

                rot = GetAngleTo(zem, hero) * Mathf.Deg2Rad;
                float rotArm = rot + (dir > 0 ? Mathf.PI : 0f);
                Log($"Rot is {rot * Mathf.Rad2Deg} and rot vel is {rotArm * Mathf.Rad2Deg}");
                nail.transform.SetRotation2D(rotArm * Mathf.Rad2Deg);
                nail.transform.localScale = new Vector3(Mathf.Sign(transform.localScale.x) * NailSize, NailSize, NailSize);
                var vel = new Vector2(Mathf.Cos(rot) * 90f, Mathf.Sin(rot) * 90f);
                float deltaX = vel.x > 0 ? RightX - nail.transform.position.x : nail.transform.position.x - LeftX;
                float time = deltaX / Mathf.Abs(vel.x);
                float endY = vel.y * time + nail.transform.position.y;
                
                if (endY > NailMaxHeightStop - 7f && repeatCnt <= 1)
                {
                    repeatCnt++;
                    Log("Doing endY!!");
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
                        PlayAudioClip("AudLand");
                        GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
                        nail.GetComponent<SpriteRenderer>().enabled = false;
                        nail.transform.Find("ZNailN").GetComponent<SpriteRenderer>().enabled = true;
                        rb.velocity = Vector2.zero;
                        Destroy(cc);
                    };
                    yield return new WaitForSeconds(0.02f);
                    cc.Freeze = true;
                    yield return new WaitWhile(() => _anim.GetCurrentFrame() < 8);
                
                    _anim.speed = 2f;
                    Spring(false, transform.position + new Vector3(LeaveOffset.x * Mathf.Cos(rot), LeaveOffset.y * Mathf.Sin(rot),0f));
                    transform.position += new Vector3(LeaveOffset.x * Mathf.Cos(rot), LeaveOffset.y * Mathf.Sin(rot));
                    yield return _anim.PlayToEnd();
                    ToggleZemer(false);
                    cc.Freeze = true;
                    yield return LaunchUp(rot, nail, didFakeOut, repeatCnt);
                    yield break;
                }
                Log("did not do endy so slamming down");
                rb.velocity = new Vector2(Mathf.Cos(rot) * 90f, Mathf.Sin(rot) * 90f);
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
                    PlayAudioClip("AudLand");
                    GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
                    nail.GetComponent<SpriteRenderer>().enabled = false;
                    nail.transform.Find("ZNailN").GetComponent<SpriteRenderer>().enabled = true;
                    rb.velocity = Vector2.zero;
                    Destroy(cc);
                };
                yield return new WaitForSeconds(0.02f);
                cc.Freeze = true;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 8);
                //rotVel = dir > 0 ? 10f : -45f;
                
                _anim.speed = 2f;
                Spring(false, transform.position + new Vector3(LeaveOffset.x * Mathf.Cos(rot), LeaveOffset.y * Mathf.Sin(rot),0f));
                transform.position += new Vector3(LeaveOffset.x * Mathf.Cos(rot), LeaveOffset.y * Mathf.Sin(rot));
                yield return _anim.PlayToEnd();
                ToggleZemer(false);
                yield return new WaitForSeconds(0.1f);
                cc.Hit = rb.velocity == Vector2.zero;
                yield return new WaitWhile(() => !cc.Hit && rb.velocity != Vector2.zero);
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
                        PlayAudioClip("AudLand");
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
                    yield return new WaitWhile(() => !cc.Hit && rbNail.velocity != Vector2.zero);
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
                    yield return RageCombo(sig, false);
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
                PlayAudioClip("AudLand");
                
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

                PlayAudioClip("AudBigSlash", 0.15f);

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
                PlayAudioClip(ZemRandAudio.PickRandomZemAud(1, 6));
                yield return _anim.PlayToFrame("ZAtt1Intro", 1);
                
                _anim.enabled = false;

                yield return new WaitForSeconds(Att1BaseDelay);

                _anim.enabled = true;

                yield return _anim.WaitToFrame(2);

                PlayAudioClip("Slash",0.15f);

                yield return _anim.WaitToFrame(6);

                // If player has gone behind, do backward slash
                if ((int) -xVel != FaceHero(true))
                {
                    yield return RageCombo(-xVel, false); // changed so it only does dash
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
                        (0, ()=>PlayAudioClip("Slash", 0.15f))
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
                
                PlayAudioClip(ZemRandAudio.PickRandomZemAud(1, 6));
                yield return _anim.PlayToEndWithActions
                (
                    "ZAtt2",
                    (0, () => PlayAudioClip("AudBasicSlash1")),
                    (2, () => PlayAudioClip("AudBasicSlash2")),
                    (6, () => PlayAudioClip("AudBigSlash2")),
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
                PlayAudioClip("ZAudHoriz");
                
                _anim.enabled = true;
                
                yield return _anim.WaitToFrame(5);
                
                PlayAudioClip("AudDashIntro");
                
                yield return _anim.WaitToFrame(6);

                _anim.enabled = false;
                _rb.velocity = new Vector2(-dir * DashXVel, 0f);
                
                PlayAudioClip("AudDash");
                
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
                float dir = FaceHero();
                float x = heroX < MIDDLE ? heroX + 8 : heroX - 8;
                transform.position = new Vector3(x, GroundY + 9.5f);
                Spring(true, transform.position, 1.4f);
                yield return new WaitForSeconds(0.16f);
                ToggleZemer(true);

                const float offset = 3f;//1.5f;
                heroX = _target.transform.position.x;
                // positive means hero is in front of zem
                float heroSig = Mathf.Sign(_target.transform.position.x - transform.position.x);
                dir = FaceHero();
                if (heroSig > 0) heroX = Mathf.Max(heroX - offset, LeftX + 0.2f);
                else if (heroSig < 0) heroX = Mathf.Min(heroX + offset, RightX -0.2f);
                else heroX = dir > 0 ? heroX - offset : heroX + offset;

                _anim.Play("Z4AirSweep");
                PlayAudioClip(ZemRandAudio.PickRandomZemAud(1, 6));
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                PlayAudioClip("AudDash");
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);

                var diff = new Vector2(x - heroX, transform.position.y - GroundY - 0.95f);
                float rot = Mathf.Atan(diff.y / diff.x);
                rot = Mathf.Sin(rot) > 0 ? rot + Mathf.PI : rot;
                _rb.velocity = new Vector2(65f * Mathf.Cos(rot), 65f * Mathf.Sin(rot));
                
                yield return new WaitWhile(() => transform.position.y > GroundY + 3.5f && _anim.GetCurrentFrame() < 4 && CheckIfStuck());
                _anim.enabled = false;
                yield return new WaitWhile(() => transform.position.y > GroundY + 3.5f && CheckIfStuck());
                _anim.enabled = true;
                yield return new WaitWhile(() => transform.position.y > GroundY - 0.3 && _anim.GetCurrentFrame() < 6 && CheckIfStuck());
                _anim.enabled = false;
                yield return new WaitWhile(() => transform.position.y > GroundY - 0.3f && CheckIfStuck());
                _anim.enabled = true;
                transform.position = new Vector3(transform.position.x, GroundY - 0.3f);
                _rb.gravityScale = 0f;
                _rb.isKinematic = true;
                _anim.speed = 1f;
                _rb.velocity = new Vector2(Mathf.Sign(_rb.velocity.x) * 30f, 0f);

                //_rb.velocity = Vector2.zero;
                PlayAudioClip("AudLand");
                yield return new WaitWhile(() => _anim.IsPlaying());
                _rb.velocity = Vector2.zero;

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
                
                grass.Stop();
                grass.transform.position = transform.position;
                grass.transform.parent = transform;
                float baseDist = 11f;
                for (int i = 0; i < maxCount; i++)
                {
                    float heroX = _target.transform.position.x;
                    float zemY = transform.position.y;
                    float offsetRand = _rand.Next(-3, 3);

                    if (heroX.Within(LeftX, 7f))
                    {
                        transform.position = new Vector3(heroX + baseDist + offsetRand, zemY);
                    }
                    else if (heroX.Within(RightX, 7f))
                    {
                        transform.position = new Vector3(heroX - baseDist - offsetRand, zemY);
                    }
                    else
                    {
                        Vector3 pos = i % 2 == 0
                            ? new Vector3(heroX + baseDist + offsetRand, zemY)
                            : new Vector3(heroX - baseDist - offsetRand, zemY);
                        if (pos.x > RightX) pos.x = RightX;
                        if (pos.x < LeftX) pos.x = LeftX;
                        transform.position = pos;
                    }

                    yield return null;
                    
                    float signX = FaceHero();

                    Spring(true, transform.position, 1.5f);
                    ToggleZemer(true);

                    Log("Doing special dash");
                    PlayAudioClip("AudDash");
                    
                    transform.Find("HyperCut").gameObject.SetActive(false);
                    _anim.PlayAt("ZMultiDashAir", 1);
                    _anim.enabled = true;
                    _anim.speed = 0.5f;
                    yield return null;
                    // _anim.enabled = false;
                    grass.Play();
                    float velX = -signX * (XVel + i * 3f);
                    _rb.velocity = new Vector2(velX, 0f);
                    offsetRand = _rand.Next(1, 5);

                    // trying to add here
                    float oldDiff = _target.transform.position.x - transform.position.x;
                    bool doSpinInstead = false;

                    yield return new WaitWhile(() =>
                    {
                        // If player attempts cheese, spin instead
                        if ((_target.transform.position.x.Within(LeftX, 2.5f) ||
                             _target.transform.position.x.Within(RightX, 2.5f)) &&
                            _target.transform.position.y > GroundY + 3f)
                        {
                            doSpinInstead = true;
                            return false;
                        }
                        var newDiff = _target.transform.position.x - transform.position.x;
                        // if we moved past player by 7 units + random offset, tp out
                        if (newDiff * oldDiff <= 0 && Mathf.Abs(newDiff) > 7f + offsetRand) return false;
                        // if we have hit the wall, tp out
                        if (_rb.velocity.x.Within(0f, 0.1f)) return false;
                        // if we are near the wall by 3 units. tp out
                        if (-signX <= 0 && transform.position.x.Within( LeftX, 3f)) return false;
                        if (-signX > 0 && transform.position.x.Within(RightX, 3f)) return false;
                        return true;
                    });

                    if (doSpinInstead)
                    {
                        yield return AltZSpin();
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
                        grass.Stop();
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
                grass.Stop();
                _anim.speed = 1f;
                _anim.PlayAt("ZDash", 7);
                _anim.enabled = true;
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(() => _anim.IsPlaying());

            }

            IEnumerator AltZSpin()
            {
                _anim.enabled = true;
                float tarY = GroundY + 8f;
                float xVel = FaceHero() * -1f;
                float diffX = Mathf.Abs(_target.transform.GetPositionX() - transform.GetPositionX());
                float diffY = Mathf.Abs(tarY - transform.GetPositionY());
                float rot = Mathf.Atan(diffY / diffX);
                rot = xVel < 0 ? Mathf.PI - rot : rot;
                PlayAudioClip("AudDashIntro");
                grass.Stop();
                _anim.speed = 1f;
                _anim.PlayAt("ZSpin", 4);
                _rb.velocity = new Vector2(60f * Mathf.Cos(rot), 60f * Mathf.Sin(rot));
                yield return new WaitForSeconds(1 / 12f);
                _anim.enabled = false;
                yield return new WaitWhile(() => transform.position.x > LeftX + 5f && transform.position.x < RightX - 5f);
                _anim.enabled = true;
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 8);
                PlayAudioClip("AudBigSlash2");
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 11);
                _rb.isKinematic = false;
                _rb.gravityScale = 1.5f;
                yield return new WaitWhile(() => transform.position.y > GroundY);
                PlayAudioClip("AudLand");
                _rb.velocity = Vector2.zero;
                _rb.gravityScale = 0f;
                _rb.isKinematic = true;
                transform.position = new Vector3(transform.position.x, GroundY);
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("ZIdle");
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
            yield return _anim.WaitToFrame(1);
            PlayAudioClip("Slash",0.15f);
            yield return _anim.WaitToFrame(2);
            SpawnPillar(-dir, SmallPillarSize, SmallPillarSpd);
            yield return _anim.WaitToFrame(5);
            Spring(false, transform.position + new Vector3(dir * LeaveOffset.x, LeaveOffset.y,0f),1.5f);
            yield return _anim.WaitToFrame(6);
            _anim.speed = LeaveAnimSpeed;
            transform.position += new Vector3(dir * LeaveOffset.x, LeaveOffset.y);
            yield return _anim.PlayToEnd();
            ToggleZemer(false, false);
        }

        private IEnumerator RageCombo(float dir, bool special)
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
                float dir = FaceHero();
                float x = heroX < MIDDLE ? heroX + 8 : heroX - 8;
                transform.position = new Vector3(x, GroundY + 9.5f);
                Spring(true, transform.position, 1.4f);
                yield return new WaitForSeconds(0.16f);
                ToggleZemer(true);

                const float offset = 3f;//1.5f;
                heroX = _target.transform.position.x;
                // positive means hero is in front of zem
                float heroSig = Mathf.Sign(_target.transform.position.x - transform.position.x);
                dir = FaceHero();
                if (heroSig > 0) heroX = Mathf.Max(heroX - offset, LeftX + 0.2f);
                else if (heroSig < 0) heroX = Mathf.Min(heroX + offset, RightX -0.2f);
                else heroX = dir > 0 ? heroX - offset : heroX + offset;

                _anim.Play("Z4AirSweep");
                PlayAudioClip(ZemRandAudio.PickRandomZemAud(1, 6));
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                //PlayAudioClip("AudDash");
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
                PlayAudioClip("AudDash");

                var diff = new Vector2(x - heroX, transform.position.y - GroundY - 0.95f);
                float rot = Mathf.Atan(diff.y / diff.x);
                rot = Mathf.Sin(rot) > 0 ? rot + Mathf.PI : rot;
                _rb.velocity = new Vector2(65f * Mathf.Cos(rot), 65f * Mathf.Sin(rot));
                
                yield return new WaitWhile(() => transform.position.y > GroundY + 3.5f && _anim.GetCurrentFrame() < 4 && CheckIfStuck());
                _anim.enabled = false;
                yield return new WaitWhile(() => transform.position.y > GroundY + 3.5f && CheckIfStuck());
                _anim.enabled = true;
                yield return new WaitWhile(() => transform.position.y > GroundY - 0.3 && _anim.GetCurrentFrame() < 6 && CheckIfStuck());
                _anim.enabled = false;
                yield return new WaitWhile(() => transform.position.y > GroundY - 0.3f && CheckIfStuck());
                _anim.enabled = true;
                transform.position = new Vector3(transform.position.x, GroundY - 0.3f);
                _rb.gravityScale = 0f;
                _rb.isKinematic = true;
                _anim.speed = 1f;
                _rb.velocity = new Vector2(Mathf.Sign(_rb.velocity.x) * 30f, 0f);

                //_rb.velocity = Vector2.zero;
                PlayAudioClip("AudLand");
                yield return new WaitWhile(() => _anim.IsPlaying());
                _rb.velocity = Vector2.zero;

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
                    PlayAudioClip("AudDash");
                    transform.Find("HyperCut").gameObject.SetActive(false);
                    _anim.PlayAt("ZMultiDashAir", 1);
                    _anim.enabled = true;
                    _anim.speed = 0.5f;
                    yield return null;
                    float velX = -signX * DashXVel;
                    _rb.velocity = new Vector2(velX, 0f);
                    
                    yield return new WaitWhile
                    (
                        () => !FastApproximately(_rb.velocity.x, 0f, 0.1f) &&
                              ((-signX <= 0 && !FastApproximately(transform.position.x, LeftX, 10f)) ||
                               (-signX > 0 && !FastApproximately(transform.position.x, RightX, 10f)))
                    );
                    
                    _anim.speed = 1f;
                    _anim.PlayAt("ZDash", 7);
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
                float totalDelay = 0.3f;
                
                yield return new WaitForSeconds(0.3f); 
                PlayAudioClip("ZAudHoriz");
                
                _anim.enabled = true;
                
                yield return _anim.WaitToFrame(5);

                PlayAudioClip("AudDashIntro");
                
                yield return _anim.WaitToFrame(6);
                
                PlayAudioClip("AudDash");
                
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
                rbL.velocity = new Vector2(-dir * 50f, 0f);
                rbR.velocity = new Vector2(dir * 50f, 0f);
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
                PlayAudioClip("ZAudHoriz");
                
                _anim.enabled = true;
                
                yield return _anim.WaitToFrame(5);
                
                
                PlayAudioClip("AudDashIntro");
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 6);
                PlayAudioClip("AudDash");
                _anim.speed = 2f;
                _rb.velocity = new Vector2(-dir * DashXVel, 0f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 7);
                _anim.enabled = false;
                _anim.speed = 1f;
                yield return new WaitForSeconds(0.2f);
                _anim.enabled = true;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 9);
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("ZIdle");
                transform.position = new Vector3(transform.position.x, GroundY, transform.position.z);
            }

            _lastAtt = this.Dash;
            // TODO revert
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
                //PlayAudioClip("ZAudAtt" + _rand.Next(2,5));
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
                PlayAudioClip("ZAudCounter");
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
                PlayAudioClip("Counter");

                Vector2 fxPos = transform.position + new Vector3(1.7f * dir, 0.8f, -0.1f);
                Quaternion fxRot = Quaternion.Euler(0, 0, dir * 80f);
                GameObject counterFx = Instantiate(FiveKnights.preloadedGO["CounterFX"], fxPos, fxRot);
                counterFx.SetActive(true);
                yield return new WaitForSeconds(0.42f);
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
                PlayAudioClip(ZemRandAudio.PickRandomZemAud(1, 6));
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                PlayAudioClip("AudDashIntro");
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
                _rb.velocity = new Vector2(60f * Mathf.Cos(rot), 60f * Mathf.Sin(rot));
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
                _anim.enabled = false;
                yield return new WaitWhile(() => transform.position.x > LeftX + 4f && transform.position.x < RightX - 4f);
                _anim.enabled = true;
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 8);
                PlayAudioClip("AudBigSlash2");
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 11);
                _rb.isKinematic = false;
                _rb.gravityScale = 1.5f;
                yield return new WaitWhile(() => transform.position.y > GroundY);
                PlayAudioClip("AudLand");
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
                PlayAudioClip(ZemRandAudio.PickRandomZemAud(2, 4));
                yield return null;
                PlayAudioClip("AudBasicSlash1");
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                PlayAudioClip("AudBasicSlash2");
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 6);
                PlayAudioClip("AudBigSlash2");
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
                PlayAudioClip("ZAudP1Death");
                
                if (DoPhase)
                {
                    StartCoroutine(PlayDeathSound());
                }

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
                    // TODO This is breaking stuff, idk yall figure it out smh
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
                    yield return Recover(firstDeath);
                }
                
                IEnumerator PlayDeathSound()
                {
                    yield return new WaitForSeconds(FiveKnights.Clips["ZAudP2Death1"].length);
                    PlayAudioClip("ZAudP2Death2");
                }
            }

            IEnumerator Recover(bool firstDeath = false)
            {
                yield return _anim.PlayBlocking("ZRecover");
                
                float t = firstDeath ? RecoveryReturnFirstDelay : RecoveryReturnRestDelay;

                yield return LeaveTemp(dir, t, firstDeath);
                
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
                PlayAudioClip(ZemRandAudio.PickRandomZemAud(1, 6));
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
                yield return RageCombo(sig, true);
                StartCoroutine(Attacks());
            }
        }

        IEnumerator FlowerBloomer()
        {
            yield return GGBossManager.Instance.PlayFlowers(2);
            GameObject whiteflashOld = FiveKnights.preloadedGO["WhiteFlashZem"];
            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
            //List<Transform> children = GGBossManager.Instance.flowersAnim.SelectMany(i => i.transform.Cast<Transform>()).ToList();
            foreach (var glow in GGBossManager.Instance.flowersGlow)
            {
                glow.gameObject.SetActive(true); 
                glow.enabled = true;
                glow.Play("Glow", -1, 0f);
            }

            yield return null;
            foreach (var glow in GGBossManager.Instance.flowersGlow)
            {
                yield return glow.WaitToFrame(2);
            }
            foreach (var glow in GGBossManager.Instance.flowersGlow) glow.enabled = false;
            
            for (int i = 0; i < 5; i++)
            {
                GameObject whiteFlash2 = Instantiate(whiteflashOld);
                whiteFlash2.SetActive(true);
                whiteFlash2.transform.position = _target.transform.position;
            }

            yield return new WaitForSeconds(0.2f);
            foreach (var glow in GGBossManager.Instance.flowersGlow) glow.enabled = true;
        }
        
        IEnumerator LeaveTemp(float dir, float delay, bool firstDeath=false)
        {
            _anim.PlayAt("ZThrow2B", 0);

            //_anim.speed = LeaveAnimSpeed;
            yield return _anim.WaitToFrame(5);
            transform.position += new Vector3(dir * LeaveOffset.x / 2, LeaveOffset.y / 2);
            
            Spring(false, transform.position + new Vector3(dir * LeaveOffset.x, LeaveOffset.y,0f),1.5f);

            if (firstDeath && CustomWP.boss == CustomWP.Boss.All)
            {
                StartCoroutine(FlowerBloomer());
            }

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
            PlayAudioClip("TraitorPillar");
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
            
            grass.transform.parent = transform;
            
            var ciel = Instantiate(FiveKnights.preloadedGO["Ceiling Dust"].gameObject);
            ciel.SetActive(false);

            yield return PassageAcrossArena(3);
            yield return DoOneLastSpiral();
            
            bool continueWithSpiral;

            IEnumerator PassageAcrossArena(int numTimes)
            {
                while (numTimes > 0)
                {
                    numTimes--;
                    _anim.Play("Z6LaserSpin", -1, 0f);
                    PlayAudioClip(ZemRandAudio.PickRandomZemAud(1, 6));
                    _anim.speed = 1.75f;
                    yield return null;
                    _anim.enabled = false;
                    yield return new WaitForSeconds(0.1f);
                    _anim.enabled = true;
                    _rb.velocity = Vector2.zero;
                    transform.position = new Vector3(transform.position.x, GroundY - 1.2f);
                    float dir = FaceHero(false, true, MIDDLE);
                    yield return null;
                    yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                    PlayAudioClip("AudBasicSlash1");
                    yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                    _rb.velocity = new Vector2(dir * 60f, 0f);
                    _anim.speed = 1.75f;
                    grass.GetComponent<ParticleSystem>().Play();
                    yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
                    PlayAudioClip("AudDash");
                    yield return new WaitWhile(() => _anim.GetCurrentFrame() < 8);
                    _anim.enabled = false;
                    _anim.speed = 1f;

                    yield return WaitByVelocity(4f);
                
                    grass.GetComponent<ParticleSystem>().Stop();
                    _rb.velocity = new Vector2(dir * 50f, 0f);
                    _anim.enabled = true;
                    yield return new WaitWhile(() => _anim.GetCurrentFrame() < 9);

                    continueWithSpiral = false;
                    StartCoroutine(SpawnSpirals(_target.transform.position, 1.1f));

                    yield return new WaitForSeconds(0.1f);
                    
                    yield return _anim.PlayToEnd();

                    _anim.PlayAt("ZThrow2", 4);
                    Vector3 pScale = gameObject.transform.localScale;
                    gameObject.transform.localScale = new Vector3(-pScale.x, pScale.y, 1f);
                    Spring(false, transform.position + new Vector3(dir * LeaveOffset.x, 0f,0f), 1.5f);
                    transform.position += new Vector3(dir * LeaveOffset.x, 0f);
                    yield return _anim.PlayToEnd();
                    ToggleZemer(false);

                    yield return new WaitWhile(() => !continueWithSpiral);
                    
                    transform.position = dir > 0
                        ? new Vector3(RightX - 1.5f, transform.position.y)
                        : new Vector3(LeftX + 1.5f, transform.position.y);
                    Spring(true, transform.position);
                    yield return new WaitForSeconds(0.15f);
                    ToggleZemer(true);
                    _anim.enabled = true;
                }
            }

            IEnumerator DoOneLastSpiral()
            {
                _anim.Play("Z6LaserSpin", -1, 0f);
                _anim.speed = 1.75f;
                yield return null;
                _anim.enabled = false;
                yield return new WaitForSeconds(0.1f);
                PlayAudioClip(ZemRandAudio.PickRandomZemAud(1, 6));
                _anim.enabled = true;
                _rb.velocity = Vector2.zero;
                transform.position = new Vector3(transform.position.x, GroundY - 1.2f);
                float dir = FaceHero(false, true, MIDDLE);
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                PlayAudioClip("AudBasicSlash1");
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                _rb.velocity = new Vector2(dir * 60f, 0f);
                _anim.speed = 1.75f;
                grass.GetComponent<ParticleSystem>().Play();
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
                PlayAudioClip("AudDash");
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 8);
                _anim.enabled = false;
                _anim.speed = 1f;

                yield return WaitByVelocity(4f);
            
                grass.GetComponent<ParticleSystem>().Stop();
                _rb.velocity = new Vector2(dir * 50f, 0f);
                _anim.enabled = true;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 9);
                //_anim.enabled = false;

                StartCoroutine(Finish());
                yield return FlashAppear(new Vector2(MIDDLE, GroundY + 2.5f));
                
                IEnumerator Finish()
                {
                    yield return new WaitForSeconds(0.1f);
                    yield return _anim.PlayToEnd();
                    _anim.PlayAt("ZThrow2", 4);
                    Vector3 pScale = gameObject.transform.localScale;
                    gameObject.transform.localScale = new Vector3(-pScale.x, pScale.y, 1f);
                    Spring(false, transform.position + new Vector3(dir * LeaveOffset.x, 0f,0f), 1.5f);
                    transform.position += new Vector3(dir * LeaveOffset.x, 0f);
                    yield return _anim.PlayToEnd();
                    ToggleZemer(false);
                }
            }
            
            IEnumerator FlashAppear(Vector2 targ)
            {
                GameObject heartOld = FiveKnights.preloadedGO["Heart"];
                GameObject startCircle = heartOld.transform.Find("Appear Trail").gameObject;
                GameObject whiteflashOld = FiveKnights.preloadedGO["WhiteFlashZem"];

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
                
                GameObject startCircleNew = Instantiate(startCircle);
                startCircleNew.SetActive(true);
                startCircleNew.transform.position = targ;
                startCircleNew.transform.localScale *= 3f;
                startCircleNew.GetComponent<ParticleSystem>().Play();
                
                yield return new WaitForSeconds(0.5f); 

                yield return FlashRepeat(targ);

                yield return new WaitForSeconds(0.1f);

                Destroy(startCircleNew);

                GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
                for (int i = 0; i < 4; i++)
                {
                    GameObject whiteFlash = Instantiate(whiteflashOld);
                    whiteFlash.SetActive(true);
                    whiteFlash.transform.position = targ;
                }
                yield return new WaitForSeconds(0.1f);
                
                StartCoroutine(SpawnSpirals(targ - new Vector2(0f, 2f), 0.9f, true));
                
                GameObject controller = Instantiate(FiveKnights.preloadedGO["SlashRingController"]);
                _destroyAtEnd.Add(controller);
                controller.transform.localScale *= 0.5f;
                controller.SetActive(true);
                StartCoroutine(PlayExtendedSpiral(controller, 1.3f));
                
                float dir = FaceHero();
                transform.position = new Vector3(targ.x, GroundY + 2f);
                ToggleZemer(true);
                _anim.enabled = true;
                _anim.PlayAt("ZSpin", 15);
                yield return null;
                _anim.enabled = false;

                yield return new WaitForSeconds(0.2f);
                dir = FaceHero();
                yield return Falling();
                yield return DashWithSpiral(dir, controller);
            }

            IEnumerator Falling()
            {
                _rb.isKinematic = false;
                _rb.gravityScale = 0.3f;
                yield return new WaitWhile(() => transform.position.y > GroundY + 1.8f);
                _anim.enabled = true;
                yield return new WaitWhile(() => transform.position.y > GroundY - 0.3f);
                transform.position = new Vector3(transform.position.x, GroundY - 0.95f);
                _rb.gravityScale = 0f;
                _rb.isKinematic = true;
                _rb.velocity = Vector2.zero;
                yield return _anim.PlayToEnd();
                _anim.Play("ZIdle", -1, 0f);
                yield return new WaitForSeconds(0.5f);
            }

            IEnumerator DashWithSpiral(float dir, GameObject controller)
            {
                _anim.speed = 1;
                for (int i = 0; i < 2; i++)
                {
                    float signX = i == 0 ? dir : Mathf.Sign(gameObject.transform.GetPositionX() - MIDDLE);
                    float destination =
                        -signX < 0 ? LeftX + 0.8f * (MIDDLE - LeftX) : RightX - 0.8f * (RightX - MIDDLE);

                    Vector3 pScale = gameObject.transform.localScale;
                    gameObject.transform.localScale = new Vector3(Mathf.Abs(pScale.x) * signX, pScale.y, 1f);
                    _anim.PlayAt("ZAerial2", 0);
                    _anim.speed = 1f;
                    yield return null;
                    yield return _anim.WaitToFrame(4);
                    _anim.enabled = false;
                    yield return FlashRepeat(new Vector3(-signX < 0 ? LeftX + 1f : RightX - 1f, GroundY + 1f), 0.15f);

                    Vector2 p2 = new Vector2();
                    Vector2 p3 = new Vector2();
                    Vector3 tarPos = new Vector3(_target.transform.position.x, GroundY);
                    if (i == 1)
                    {
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
                        
                        StartCoroutine(DelayedFlash());
                        IEnumerator DelayedFlash()
                        {
                            yield return FlashRepeat(p2 - new Vector2(0f, 6f), 0.1f);
                            tarPos = new Vector3(_target.transform.position.x, GroundY);
                            yield return FlashRepeat(p3, 0.1f);
                            tarPos = new Vector3(_target.transform.position.x, GroundY);
                            yield return FlashRepeat(tarPos, 0.1f);
                        }
                    }
                    
                    _anim.enabled = true;
                    _anim.PlayAt("Z5LandSlide", 1);
                    PlayAudioClip("AudDash");
                    float spd = 60f;
                    _rb.velocity = new Vector2(-signX * spd, 0f);
                    grass.GetComponent<ParticleSystem>().Play();
                    yield return null;
                    _anim.enabled = false;
                    // going right
                    yield return -signX > 0 ? 
                        new WaitWhile(() => transform.position.x < destination) : 
                        new WaitWhile(() => transform.position.x > destination);
                    _anim.enabled = true;
                    _anim.PlayAt("Z4AirSweep", 6);
                    yield return new WaitForSeconds(0.05f);
                    _anim.PlayAt("ZSpin", 4);
                    _rb.velocity = new Vector2(_rb.velocity.x, 50f);
                    yield return new WaitForSeconds(0.1f);
                    _anim.enabled = false;
                    
                    yield return WaitByVelocity(0.3f);
                    ciel.SetActive(true);
                    ciel.GetComponent<ParticleSystem>().Play();
                    ciel.transform.position = new Vector3(MIDDLE, 39.5f);
                    PlayAudioClip(i % 2 == 0 ? "breakable_wall_hit_1" : "breakable_wall_hit_2");
                    GameCameras.instance.cameraShakeFSM.SendEvent("BigShake");
                    grass.GetComponent<ParticleSystem>().Stop();
                    
                    if (i == 1)
                    {
                        yield return DontStop(controller, p2, p3, tarPos);
                        yield break;
                    }
                    
                    _anim.PlayAt("ZSpin", 14);
                    _anim.enabled = true;
                    _rb.isKinematic = false;
                    _rb.gravityScale = 2f;
                    _rb.velocity = new Vector2(signX * 7f, 0f);
                    
                    yield return new WaitWhile(() => _anim.IsPlaying());
                    yield return new WaitWhile(() => transform.position.y > GroundY - 0.3f);
                    transform.position = new Vector3(transform.position.x, GroundY - 0.95f);
                    _rb.gravityScale = 0f;
                    _rb.isKinematic = true;
                    _rb.velocity = Vector2.zero;

                    _anim.PlayAt("Z5LandSlide",3);
                    yield return null;
                    yield return new WaitWhile(() => _anim.IsPlaying());
                }
                
                //yield return SpinDashInAir(controller);
            }

            IEnumerator DontStop(GameObject controller, Vector2 p2, Vector2 p3, Vector3 tarPos)
            {
                float dir = FaceHero(false, false, MIDDLE);
                _anim.PlayAt("ZSpin", 3);
                _anim.enabled = true;
                yield return null;
                
                Vector2 p1 = transform.position;

                float timePass = 0f;
                float duration = 0.5f;
                while (timePass < duration)
                {
                    transform.position = QuadraticBezierInterp(p1, p2, p3, timePass / duration);
                    timePass += Time.deltaTime;
                    if (timePass / duration > 0.55f &&  _anim.GetCurrentFrame() >= 4) _anim.enabled = false;

                    yield return null;
                }
                transform.position = p3;
                
                _anim.enabled = true;
                _rb.velocity = Vector2.zero;

                yield return LandToPlayer(controller, tarPos);
            }

            IEnumerator LandToPlayer(GameObject controller, Vector2 tarPos)
            {
                int side = 1;
                FaceHero();
                Vector2 p3 = transform.position;
                _anim.PlayAt("Z4AirSweep", 1);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
                PlayAudioClip("AudDash");
                _anim.speed = 2f;

                Vector2 diff = p3 - tarPos;
                float rot = Mathf.Atan(diff.y / diff.x);
                if (side > 0 && p3.x > tarPos.x) rot += Mathf.PI;
                _rb.velocity = new Vector2(70f * Mathf.Cos(rot), 70f * Mathf.Sin(rot));
                float velDir = Mathf.Sign(_rb.velocity.x);

                yield return new WaitWhile(() => transform.position.y > GroundY + 3.5f && _anim.GetCurrentFrame() < 4 && CheckIfStuck());
                _anim.enabled = false;
                yield return new WaitWhile(() => transform.position.y > GroundY + 3.5f && CheckIfStuck());
                _anim.enabled = true;
                yield return new WaitWhile(() => transform.position.y > GroundY - 0.3 && _anim.GetCurrentFrame() < 6 && CheckIfStuck());
                _anim.enabled = false;
                yield return new WaitWhile(() => transform.position.y > GroundY - 0.3f && CheckIfStuck());
                _anim.enabled = true;
                transform.position = new Vector3(transform.position.x, GroundY - 0.3f);
                _rb.gravityScale = 0f;
                _rb.isKinematic = true;
                _anim.speed = 1f;

                ciel.SetActive(true);
                ciel.GetComponent<ParticleSystem>().Play();
                ciel.transform.position = new Vector3(MIDDLE, 39.5f);
                PlayAudioClip("breakable_wall_hit_1");
                GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
                
                _rb.velocity = new Vector2(velDir * 60f, 0f);
                
                yield return new WaitWhile(() => _anim.IsPlaying());
                _rb.velocity = Vector2.zero;
                yield return new WaitForSeconds(0.1f);

                yield return Disperse(controller);
            }

            IEnumerator Disperse(GameObject controller)
            {
                if (!IsFacingPlayer())
                    yield return Turn();

                float dir = FaceHero();

                _anim.Play("ZAtt2");
                PlayAudioClip(ZemRandAudio.PickRandomZemAud(2, 4));
                _anim.speed = 2f;
                yield return null;
                PlayAudioClip("AudBasicSlash1");
                
                // Lerp small
                StartCoroutine(LerpSizeChange(controller.transform, 0.2f, 0.5f));

                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);

                PlayAudioClip("AudBasicSlash2");
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
                
                PlayAudioClip("AudBigSlash2");
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
                    if (trans == null) yield break;
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

            IEnumerator Preload(Vector2 targ, bool skipBallCircle = false)
            {
                GameObject heartOld = FiveKnights.preloadedGO["Heart"];
                GameObject startCircle = heartOld.transform.Find("Appear Trail").gameObject;
                GameObject whiteflashOld = FiveKnights.preloadedGO["WhiteFlashZem"];
                GameObject glowOld = heartOld.transform.Find("Get Anim").Find("Get Glow").gameObject;
                GameObject startCircleNew = Instantiate(startCircle);
                
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
                
                startCircleNew.transform.position = targ;
                startCircleNew.transform.localScale *= 2f;
                startCircleNew.GetComponent<ParticleSystem>().Play();
                startCircleNew.SetActive(!skipBallCircle);
                GameObject glowOne = Instantiate(glowOld);
                glowOne.SetActive(true);
                glowOne.transform.position = targ;
                glowOne.transform.SetRotation2D(UnityEngine.Random.Range(0,360));

                yield return new WaitForSeconds(0.3f);

                Destroy(startCircleNew);
                
                GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
                GameObject whiteFlash = Instantiate(whiteflashOld);
                whiteFlash.SetActive(true);
                whiteFlash.transform.position = targ;

                GameObject spawn = new GameObject();
                spawn.transform.position = targ;
                PlayAudioClip("NeedleSphere", 0.3f, 1f, spawn.transform);

                for (int i = 0; i < 5; i++)
                {
                    GameObject glow = Instantiate(glowOld);
                    glow.SetActive(true);
                    glow.transform.position = targ;
                    glow.transform.SetRotation2D(i * 90 + UnityEngine.Random.Range(20,70));
                }
                yield return new WaitForSeconds(0.15f);
            }

            // Spawn the spiral slash on top of the player
            IEnumerator SpawnSpirals(Vector2 targ, float scale, bool skipBallCircle = false)
            {
                yield return Preload(targ, skipBallCircle);

                GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");

                var slash = Instantiate(FiveKnights.preloadedGO["SlashRingControllerNew"]);
                _destroyAtEnd.Add(slash);
                slash.SetActive(true);
                slash.transform.position = targ;
                slash.transform.localScale /= (scale * 2.5f);
                float spd = 2.5f; //2.2f; // 2f
                StartCoroutine(LerpScale(slash.transform, 2.5f));

                for (int i = 0; i < 3; i++) // < 3
                {
                    GameObject spiral = slash.transform.Find($"SlashRing{i}").gameObject;
                    ActivateSpiral(spiral, 2f);
                }
                // Wait for first set to do non-hitbox part of animation
                Animator oldAnim = slash.transform.Find("SlashRing0").Find("1").gameObject.GetComponent<Animator>();
                yield return new WaitWhile(() => oldAnim.GetCurrentFrame() < 8 );
                
                StartCoroutine(MakeSureOuterSlashesDontEnd());
                for (int i = 3; i < 4; i++) // < 3
                {
                    GameObject spiral = slash.transform.Find($"SlashRing{i}").gameObject;
                    ActivateSpiral(spiral, spd * 1.2f);
                }
                
                int[] randSlashes = {4, 5, 6, 7};
                GameObject lastSpiral = null;
                foreach (int i in randSlashes)
                {
                    GameObject spiral = slash.transform.Find($"SlashRing{i}").gameObject;
                    ActivateSpiral(spiral, spd * 2f);
                    lastSpiral = spiral;
                    yield return new WaitForSeconds(0.05f);
                }
                oldAnim = lastSpiral.transform.Find("2").gameObject.GetComponent<Animator>();
                yield return new WaitWhile(() => oldAnim.GetCurrentFrame() < 8);
                Transform tBlast = FiveKnights.preloadedGO["Blast"].transform;
                var middle = Instantiate(tBlast.Find("Particle middle").gameObject);
                middle.transform.position = slash.transform.position;
                middle.SetActive(true);
                yield return new WaitWhile(() => oldAnim.GetCurrentFrame() < 10);
                yield return LerpScale2(slash.transform);
                //StartCoroutine(LerpOpacity(slash.transform));

                IEnumerator ForceDisableHitbox()
                {
                    while (slash != null)
                    {
                        foreach (Transform s in slash.transform)
                        {
                            foreach (PolygonCollider2D pc in s.GetComponentsInChildren<PolygonCollider2D>(true))
                            {
                                Log("DEASTEORIO(QAJOHD");
                                Destroy(pc);
                            }
                        }

                        yield return null;
                    }
                }
                
                IEnumerator MakeSureOuterSlashesDontEnd()
                {
                    while (slash != null)
                    {
                        for (int i = 0; i < 3; i++) // < 3
                        {
                            GameObject spiral = slash.transform.Find($"SlashRing{i}").gameObject;
                            Animator oldAnim1 = spiral.Find("1").gameObject.GetComponent<Animator>();
                            yield return oldAnim1.PlayToEnd();
                            Log("It ended a cycle");
                        }
                        for (int i = 0; i < 3; i++) // < 3
                        {
                            GameObject spiral = slash.transform.Find($"SlashRing{i}").gameObject;
                            ActivateSpiral(spiral, 2.5f);
                            Log("It started a cycle");
                        }

                        yield return null;
                    }
                }
                
                // Terrible way to check if animation is over
                
                foreach (Transform t in slash.transform)
                {
                    foreach (var anim in t.GetComponentsInChildren<Animator>())
                    {
                        Log($"Anim we check is {anim.name}");
                        yield return anim.WaitToFrame(10);
                        anim.gameObject.SetActive(false);
                    }
                }
                continueWithSpiral = true;
                //Destroy(slash);
                
                IEnumerator LerpScale(Transform trans, float scale)
                {
                    float lerpDuration = 0.15f;
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

                PlayAudioClip("NeedleSphere",0.3f);
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
                anim.PlayAt("NewSlash3", 0);
                anim.speed = 1f;
                yield return null;
                anim.enabled = true;
                yield return anim.WaitToFrame(3);
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

        private void Spring(bool isIn, Vector2 pos, float speedSca = 1f, bool parent=false)
        {
            string n = "VapeIn2";
            GameObject go = Instantiate(FiveKnights.preloadedGO[n]);
            PlayMakerFSM fsm = go.LocateMyFSM("FSM");
            go.GetComponent<tk2dSpriteAnimator>().GetClipByName("Plink").fps = 24 * speedSca;
            go.transform.localScale *= 1.7f; //1.3f
            fsm.GetAction<Wait>("State 1", 0).time = 0f;
            go.transform.position = pos;
            go.SetActive(true);

            if (parent)
            {
                go.transform.parent = transform;
            }
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
                (3, () => PlayAudioClip("Slash", 0.15f))
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
                _dnailReac.SetConvoTitle("ZEM_GG_DREAM");
            }

            orig(self);
        }

        private void HealthManager_TakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.name.Contains("Zemer"))
            {
                isHit = true;   
                _hitEffects.RecieveHitEffect(hitInstance.Direction);

                if ((DoneFrenzyAtt == 0 && _hm.hp < 0.65f * Phase2HP) || 
                    (DoneFrenzyAtt == 1 && _hm.hp < 0.35f * Phase2HP))
                {
                    Log($"Doing frenzy p{DoneFrenzyAtt}");
                    StopAllCoroutines();
                    foreach (var i in FindObjectsOfType<Rigidbody2D>(true))
                    {
                        if (i.name.Contains("Nail") && i.transform.parent == null)
                        {
                            Destroy(i.gameObject);
                        }
                    }
                    foreach (GameObject i in _destroyAtEnd.Where(x => x != null))
                    {
                        Destroy(i);
                    }

                    _destroyAtEnd = new List<GameObject>();
                    if (grass != null) grass.Stop();
                    FaceHero();
                    _bc.enabled = false;
                    _anim.speed = 1f;
                    _anim.enabled = true;
                    _rb.velocity = Vector2.zero;
                    _rb.gravityScale = 0f;
                    DoneFrenzyAtt++;
                    StartCoroutine(EndPhase1(false));
                }
                if (_hm.hp <= 200)
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

            foreach (GameObject i in _destroyAtEnd.Where(x => x != null))
            {
                Destroy(i);
            }
            
            grass.Stop();
            //Destroy(grass.gameObject);

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
                PlayAudioClip("ZAudP1Death");
                yield return new WaitForSeconds(FiveKnights.Clips["ZAudP2Death1"].length);
                PlayAudioClip("ZAudP2Death2");
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
        
        IEnumerator FlashRepeat(Vector3 targ, float timer = 0.2f, bool cringeVersion=false)
        {
            if (cringeVersion)
            {
                GameObject fxOrig = FiveKnights.preloadedGO["HornetSphere"].transform.Find("Flash Effect").gameObject;
                for (int i = 0; i < 5; i++)
                {
                    foreach (int j in new [] { 1 })
                    {
                        var fx = Instantiate(fxOrig);
                        float rot = (i * 90 + UnityEngine.Random.Range(30, 60)) * j;
                        fx.transform.SetRotationZ(rot);
                        fx.transform.position = targ;
                        fx.SetActive(true);
                        var fsm = fx.LocateMyFSM("FSM");
                        fsm.enabled = true;
                        fsm.FsmVariables.FindFsmFloat("Pause").Value = 1f;
                        fsm.FsmVariables.FindFsmFloat("Rotation").Value = rot;
                        fsm.FsmVariables.FindFsmBool("Reset Rotation").Value = false;
                        fsm.FsmVariables.FindFsmVector3("Init Pos").Value = targ;
                        fsm.SetState("Init");
                        fsm.transform.localScale *= 0.65f;
                        PlayAudioClip(i % 2 == 0 ? "AudBasicSlash1" : "AudBasicSlash2", 0.5f, 1f, fx.transform);   
                    }
                    yield return new WaitForSeconds(timer);
                    timer /= 1.5f;
                }
            }
            else
            {
                GameObject heartOld = FiveKnights.preloadedGO["Heart"];
                GameObject glowOld = heartOld.transform.Find("Get Anim").Find("Get Glow").gameObject;
                for (int i = 0; i < 5; i++)
                {
                    GameObject glow = Instantiate(glowOld);
                    glow.SetActive(true);
                    glow.transform.position = targ;
                    float rot = i * 90 + UnityEngine.Random.Range(20, 70);
                    glow.transform.SetRotation2D(rot);
                    glow.transform.SetRotation2D(-rot);

                    PlayAudioClip(i % 2 == 0 ? "AudBasicSlash1" : "AudBasicSlash2", 0.5f, 1f, glow.transform);

                    yield return new WaitForSeconds(timer);
                    timer /= 1.5f;
                }
            }
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

        public void PlayAudioClip(string clipName, float pitchVar = 0f, float volume = 1f, Transform posOverride = null)
        {
            var clip = clipName switch
            {
                "Counter" => (AudioClip) _pvFsm.GetAction<AudioPlayerOneShotSingle>("Counter Stance", 1).audioClip.Value,
                "Slash" => (AudioClip) _pvFsm.GetAction<AudioPlayerOneShotSingle>("Slash1", 1).audioClip.Value,
                "TraitorPillar" => FiveKnights.Clips["TraitorSlam"],
                _ => FiveKnights.Clips[clipName]
            };
            this.PlayAudio(clip, volume, pitchVar, posOverride);
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

        private void AssignFields()
        {
            Transform slash = transform.Find("NewSlash3");
            slash.localPosition = new Vector3(-16f,3.5f,-0.5f);
            slash.localScale = new Vector3(0.6f,0.6f,0.5f);


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
                dh.damageDealt = 1;
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
        
        bool CheckIfStuck()
        {
            if (_rb.velocity.x.Within(0f, 0.5f))
            {
                _rb.gravityScale = 2f;
                _rb.isKinematic = false;
            }
            return true;
        }
        
        private void Log(object o)
        {
            Logger.Log("[Zemer2] " + o);
        }

        private void OnDestroy()
        {
            On.HealthManager.Hit -= OnBlockedHit;
            On.HealthManager.Die -= HealthManagerOnDie;
            On.HealthManager.TakeDamage -= HealthManager_TakeDamage;
            On.EnemyDreamnailReaction.RecieveDreamImpact -= OnReceiveDreamImpact;
        }
    }
}
