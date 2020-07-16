using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ModCommon;
using System.Collections;
using HutongGames.PlayMaker.Actions;
using ModCommon.Util;
using GlobalEnums;

namespace FiveKnights
{
    public class ZemerControllerP2 : MonoBehaviour
    {
        private HealthManager _hm;
        private BoxCollider2D _bc;
        private SpriteRenderer _sr;
        private EnemyDreamnailReaction _dnailReac;
        private MusicPlayer _ap;
        private GameObject _dd;
        private bool flashing;
        private GameObject[] traitorSlam;
        private int traitorSlamIndex;
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
        private const int Phase2HP = 1200;
        private const int Phase3HP = 800;
        private const float TurnDelay = 0.05f;
        private PlayMakerFSM _pvFsm;
        private Coroutine _counterRoutine;
        private bool _blockedHit;
        private bool atPhase2;
        private bool _attacking;
        private List<Action> _moves;
        private Dictionary<Action, int> _maxRepeats;
        private Dictionary<Action, int> _repeats;
        private Action _lastAtt;
        private bool isHit;
        public bool shouldNotDoPhase2;

        private readonly string[] _dnailDial =
        {
            "ZEM_DREAM_1",
            "ZEM_DREAM_2",
            "ZEM_DREAM_3"
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

            AssignFields();
        }

        private void Start()
        {
            Log("Start2");
            _hm.hp = Phase2HP;
            _deathEff = _dd.GetComponent<EnemyDeathEffectsUninfected>();
            _target = HeroController.instance.gameObject;
            _attacking = true;
            On.HeroController.TakeDamage += HeroControllerOnTakeDamage;
            StartCoroutine(Attacks());
            EndPhase1();
        }

        private bool _dontDmgKnigt;
        private void HeroControllerOnTakeDamage(On.HeroController.orig_TakeDamage orig, HeroController self,
            GameObject go, CollisionSide damageside, int damageamount, int hazardtype)
        {
            if (_dontDmgKnigt && go.name.Contains("Zemer")) return;
            orig(self, go, damageside, damageamount, hazardtype);
        }

        private void Update()
        {
            if (_bc.enabled)
            {
                if (transform.GetPositionX() > (RightX - 1.3f) && _rb.velocity.x > 0f)
                {
                    _rb.velocity = new Vector2(0f, _rb.velocity.y);
                }

                if (transform.GetPositionX() < (LeftX + 1.3f) && _rb.velocity.x < 0f)
                {
                    _rb.velocity = new Vector2(0f, _rb.velocity.y);
                }
            }
        }

        private void AerialAttack()
        {
            IEnumerator Attack()
            {
                if (!IsFacingPlayer())
                {
                    Turn();
                    yield return new WaitForSeconds(TurnDelay);
                }
                transform.Find("BladeAerialShadow").gameObject.SetActive(true);
                float xVel = FaceHero() * -1f;
                _anim.Play("ZAerial");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
                _rb.velocity = new Vector2(xVel * 35f, 18f);
                _rb.gravityScale = 1.3f;
                _rb.isKinematic = false;
                yield return new WaitForSeconds(0.1f);
                yield return new WaitWhile(() => transform.position.y > GroundY);
                _rb.velocity = Vector2.zero;
                _rb.gravityScale = 0f;
                _rb.isKinematic = true;
                transform.position = new Vector2(transform.position.x, GroundY);
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("ZIdle");
                yield return new WaitForSeconds(0.2f);
                _attacking = false;
            }

            _lastAtt = this.AerialAttack;
            StartCoroutine(Attack());
        }
        
        private IEnumerator Attacks()
        {
            Log("[Waiting to start calculation]");
            yield return new WaitWhile(()=>_attacking);
            _anim.Play("ZIdle");
            yield return new WaitForSeconds(0.3f);
            yield return new WaitWhile(()=>_attacking);
            Log("[End of Wait]");
            Log("[Setting Attacks]");
            Vector2 posZem = transform.position;
            Vector2 posH = _target.transform.position;                
            _attacking = true;
            List<Action> toDo = new List<Action>();
            //If the player is close
            if (posH.y > 19f && (posH.x <= 60.6 || posH.x >= 91.4f))
            {
                Action[] lst = { SpinAttack, NailLaunch };
                toDo.Add(lst[_rand.Next(0, lst.Length)]);
            }
            else if (FastApproximately(posZem.x, posH.x, 5f))
            {
                Action[] lst = {Dodge, Dodge, ZemerCounter, ZemerCounter, AerialAttack};
                Action curr = lst[_rand.Next(0, lst.Length)];
                while (_repeats[curr] >= _maxRepeats[curr])
                {
                    curr = lst[_rand.Next(0, lst.Length)];
                }
                toDo.Add(curr);
                Log("Added " + curr.Method.Name);
            }
            
            Action[] attLst = {Dash, Attack1Base, Attack1Base, NailLaunch, AerialAttack, DoubleFancy, SweepDash};
            Action currAtt = attLst[_rand.Next(0, attLst.Length)];
            while (_repeats[currAtt] >= _maxRepeats[currAtt])
            {
                currAtt = attLst[_rand.Next(0, attLst.Length)];
            }
            toDo.Add(currAtt);
            Log("Added " + currAtt.Method.Name);
            
            if (currAtt == Attack1Base)
            {
                Action[] lst2 =
                {
                    Dash, Dash, Attack1Complete, Attack1Complete, FancyAttack, FancyAttack, 
                    NailLaunch, NailLaunch
                };
                currAtt = lst2[_rand.Next(0, lst2.Length)];
                toDo.Add(currAtt);
                Log("Added " + currAtt.Method.Name);
                if (currAtt == Dash)
                {
                    lst2 = new Action[] {ZemerSlam, NailLaunch, null};
                    currAtt = lst2[_rand.Next(0, lst2.Length)];
                    if (currAtt != null)
                    {
                        toDo.Add(currAtt);
                        Log("Added " + currAtt.Method.Name);
                    }
                }
                else if (currAtt == FancyAttack)
                {
                    lst2 = new Action[] {Dodge, Dash, NailLaunch};
                    currAtt = lst2[_rand.Next(0, lst2.Length)];
                    if (currAtt == Dodge)
                    {
                        toDo.Add(Dodge);
                        toDo.Add(FancyAttack);
                        toDo.Add(Dash);   
                    }
                    Log("Added " + currAtt.Method.Name);
                }
            }
            else if (currAtt == Dash)
            {
                Action[] lst2 = {null, null, Dash, ZemerCounter};
                currAtt = lst2[_rand.Next(0, lst2.Length)];
                if (currAtt != null)
                {
                    toDo.Add(currAtt);
                    Log("Added " + currAtt.Method.Name);
                }
            }

            Log("[Done Setting Attacks]");
            foreach (Action act in _moves)
            {
                if (toDo.Contains(act)) _repeats[act]++;
                else _repeats[act] = 0;
            }

            Action prev = null;
            foreach (Action act in toDo)
            {
                Log("Doing [" + act.Method.Name + "]");
                if (prev != null) _repeats[prev] = 0;
                _attacking = true;
                yield return null;
                act.Invoke();
                yield return null;
                yield return new WaitWhile(() => _attacking);
                Log("Done [" + act.Method.Name + "]");
                prev = act;
                yield return null;
            }
            _anim.Play("ZIdle");
            Log("[Restarting Calculations]");
            yield return new WaitForEndOfFrame();
            StartCoroutine(Attacks());
        }
        
        private void NailLaunch()
        {
            float dir = 0f;
            bool doSlam = false;
            IEnumerator Throw()
            {
                Vector2 hero = _target.transform.position;
                Vector2 zem = gameObject.transform.position;
                if (FastApproximately(hero.x, zem.x, 10f) && 
                    hero.y < GroundY + 1)
                {
                    _attacking = false;
                    yield break;
                }
                dir = FaceHero();
                float rot = 0f;
                _anim.Play("ZThrow1");
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
                hero = _target.transform.position;
                zem = gameObject.transform.position;
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
                float velmag = (hero.y < GroundY + 2f) ? 70f : 25f;
                parRB.velocity = new Vector2(Mathf.Cos(rotVel) * velmag, Mathf.Sin(rotVel) * velmag);
                yield return new WaitForSeconds(0.02f);
                CollisionCheck cc = nailPar.transform.Find("ZNailC").gameObject.AddComponent<CollisionCheck>();
                cc.Freeze = true;
                cc.OnCollide += () =>
                {
                    if (cc.Hit) PlayAudioClip("AudLand");
                };
                yield return new WaitWhile(() => _anim.IsPlaying());
                bool isTooHigh = (nailPar.transform.position.y > GroundY + 1);
                StartCoroutine(!isTooHigh ? LaunchSide(nailPar) : LaunchUp(rot, nailPar));
            }
            
            //In phase 1, have her just pick up the sword using reverse slam animation but in later phases add combos after attack
            IEnumerator LaunchUp(float rot, GameObject nail)
            {
                float rotVel = (dir > 0) ? rot + 180 * Mathf.Deg2Rad : rot;
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
                
                yield return new WaitWhile((() => nail.transform.position.y < 17f && !cc.Hit));
                
                transform.position = nail.transform.position + new Vector3(5f*Mathf.Cos(rotVel),5f*Mathf.Sin(rotVel),0f);
                _anim.Play("ZThrow3Air", -1, 0f);
                ToggleZemer(true);
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
                float oldDir = dir;
                dir = FaceHero();
                rot = Mathf.Atan((hero.y - zem.y) / (hero.x - zem.x));
                rotVel = (dir > 0) ? rot + Mathf.PI : rot;
                float offset = FastApproximately(dir, oldDir, 0.1f) ? 0f : 180f;
                nail.transform.SetRotation2D(rot * Mathf.Rad2Deg + offset);
                if (hero.y > GroundY + 1.5f)
                {
                    doSlam = true;
                    rot = Mathf.Atan((7.4f - zem.y) / (75f - zem.x));
                    rotVel = (dir > 0) ? rot + Mathf.PI : rot;
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
                rotVel = dir > 0 ? 10f: -45f;
                _rb.velocity = new Vector2(25f * Mathf.Cos(rotVel), 25f * Mathf.Sin(rotVel));
                yield return new WaitForSeconds(0.1f);
                //Spring(false, transform.position + new Vector3(3.5f * Mathf.Cos(rotVel), 3.5f * Mathf.Sin(rotVel),0f));
                yield return new WaitWhile(() => _anim.IsPlaying());
                ToggleZemer(false, true);
                yield return new WaitForSeconds(0.1f);
                _rb.velocity = Vector2.zero;
                transform.position = new Vector3(80f, GroundY);
                yield return new WaitWhile(() => !cc.Hit);
                StartCoroutine(LaunchSide(nail, false));
            }
            
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
                    cc.Hit = rbNail.velocity == Vector2.zero;
                    _bc.enabled = false;
                    _dontDmgKnigt = true;
                    yield return null;
                    yield return new WaitWhile(() => !cc.Hit && _anim.IsPlaying());
                    ToggleZemer(false);
                    _bc.enabled = false;
                    //Spring(false, transform.position + new Vector3(-dir * 3.5f, 0f, 0f));
                    yield return new WaitWhile(() => !cc.Hit);
                    PlayAudioClip("AudLand");
                    Log("Stop nail");
                    yield return new WaitForSeconds(0.02f);
                    _bc.enabled = false;
                    rbNail.velocity = Vector2.zero;
                    nail.GetComponent<SpriteRenderer>().enabled = false;
                    nail.transform.Find("ZNailN").GetComponent<SpriteRenderer>().enabled = true;
                    _bc.enabled = false;
                    yield return new WaitForSeconds(0.75f);
                }
                
                Vector2 zem = transform.position;
                Vector2 nl = nail.transform.Find("Point").position;
                Vector3 zemSc = transform.localScale;
                nail.GetComponent<DamageHero>().enabled = false;
                nail.GetComponent<BoxCollider2D>().enabled = false;
                if (nl.x < 75f)
                {
                    transform.localScale = new Vector3(Mathf.Abs(zemSc.x), zemSc.y, zemSc.z);
                    transform.position = new Vector2(nl.x + 5f, zem.y);
                    Spring(true, transform.position);
                    _bc.enabled = false;
                    yield return new WaitForSeconds(0.15f);
                    _dontDmgKnigt = false;
                    ToggleZemer(true);
                    _bc.enabled = false;
                    _rb.velocity = new Vector2(-40f, 0f);
                    _anim.Play("ZThrow3Gnd");
                    yield return null;
                    yield return new WaitWhile(() => 
                        _anim.GetCurrentFrame() < 1 && !FastApproximately(transform.position.x, nl.x, 0.3f));
                    _anim.enabled = false;
                    yield return new WaitWhile(() => 
                        !FastApproximately(transform.position.x, nl.x, 0.3f) &&
                        transform.position.x > LeftX + 1.3f);
                    _anim.enabled = true;
                    yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                    _rb.velocity = new Vector2(0f,0f);
                    Destroy(nail);
                }
                else
                {
                    transform.localScale = new Vector3(-Mathf.Abs(zemSc.x), zemSc.y, zemSc.z);
                    transform.position = new Vector2(nl.x - 5f, zem.y);
                    Spring(true, transform.position);
                    _bc.enabled = false;
                    yield return new WaitForSeconds(0.15f);
                    ToggleZemer(true);
                    _bc.enabled = false;
                    _rb.velocity = new Vector2(40f, 0f);
                    _anim.Play("ZThrow3Gnd");
                    yield return null;
                    yield return new WaitWhile(() => 
                        _anim.GetCurrentFrame() < 1 && !FastApproximately(transform.position.x, nl.x, 0.3f));
                    _anim.enabled = false;
                    yield return new WaitWhile(() => 
                        !FastApproximately(transform.position.x, nl.x, 0.3f)&&
                        transform.position.x < RightX - 1.3f);
                    _anim.enabled = true;
                    yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                    _rb.velocity = new Vector2(0f,0f);
                    Destroy(nail);
                }
                
                if (_hm.hp <= Phase3HP && _rand.Next(0, 5) < 3)
                {
                    _anim.PlayAt("Z2Crawl", 1);
                    yield return null;
                    yield return new WaitWhile(() => _anim.IsPlaying());
                    RageCombo(dir, true, 3);
                    yield break;
                }
                yield return new WaitWhile(() => _anim.IsPlaying());
                if (doSlam) ZemerSlam();
                else _attacking = false;
            }

            _lastAtt = this.NailLaunch;
            StartCoroutine(Throw());
        }

        private void ZemerSlam()
        {
            IEnumerator Slam()
            {
                _anim.Play("ZSlam");
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
                _anim.enabled = false;
                yield return new WaitForSeconds(0.25f);
                _anim.enabled = true;
                _anim.speed *= 5.5f;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.speed /= 5.5f;
                SpawnShockwaves(2f,50f,1, transform.position);
                GameCameras.instance.cameraShakeFSM.SendEvent("BigShake");
                _attacking = false;
            }

            _lastAtt = this.ZemerSlam;
            StartCoroutine(Slam());
        }
        
        private void Attack1Complete()
        {
            IEnumerator Attack1Complete()
            {
                _anim.Play("ZAtt1");
                float xVel = FaceHero() * -1;
                _anim.enabled = false;
                yield return new WaitForSeconds(0.1f);
                _anim.enabled = true;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
                PlayAudioClip("Slash", 0.85f, 1.15f);
                _rb.velocity = new Vector2(40f * xVel, 0f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("ZIdle");
                _attacking = false;
            }

            if (_lastAtt != Attack1Base)
            {
                _attacking = false;
                return;
            }

            _lastAtt = this.Attack1Complete;
            StartCoroutine(Attack1Complete());
        }
        
        private void Attack1Base()
        {
            IEnumerator Attack1Base()
            {
                if (!IsFacingPlayer())
                {
                    Turn();
                    yield return new WaitForSeconds(TurnDelay);
                }
                float xVel = FaceHero() * -1f;
                _anim.Play("ZAtt1Base");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                _anim.enabled = false;
                yield return new WaitForSeconds(0.2f);
                _anim.enabled = true;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                PlayAudioClip("Slash", 0.85f, 1.15f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 6);
                // If player has gone behind, do backward slash
                if (_hm.hp <= Phase3HP && (int)-xVel != (int)FaceHero(true))
                {
                    RageCombo(-xVel, _rand.Next(0, 5) >= 3, _rand.Next(1,3));
                    _lastAtt = null;
                    yield break;
                }
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 7);
                PlayAudioClip("Slash", 0.85f, 1.15f);
                _rb.velocity = new Vector2(23f * xVel, 0f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 11);
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _attacking = false;
            }

            _lastAtt = this.Attack1Base;
            StartCoroutine(Attack1Base());
        }

        //Only in phase 3
        private void DoubleFancy()
        {
            int i = 0;
            
            void Position()
            {
                float distL = transform.position.x - LeftX;
                float distR = RightX - transform.position.x;

                if (distL > 7f && distR > 7f)
                {
                    float pos = (_rand.Next(0, 2) == 0) ? LeftX : RightX;
                    float distSigX = Mathf.Sign(pos-transform.GetPositionX());
                    float signX = Mathf.Sign(transform.localScale.x);
                    if (FastApproximately(pos, transform.position.x, 15f))
                    {
                        if (!FastApproximately(distSigX, signX, 0.1f))
                        {
                            Vector3 tmp = transform.localScale;
                            transform.localScale = new Vector3(tmp.x * -1f,tmp.y, tmp.z);
                        }

                        StartCoroutine(Dodge(distSigX));
                    }
                    else
                    {
                        if (FastApproximately(distSigX, signX, 0.1f))
                        {
                            Vector3 tmp = transform.localScale;
                            transform.localScale = new Vector3(tmp.x * -1f,tmp.y, tmp.z);
                        }

                        StartCoroutine(Dash(distSigX));
                    }
                }
                else
                {
                    StartCoroutine(FancyOne());   
                }
            }
            
            IEnumerator Dodge(float xVel)
            {
                _anim.Play("ZDodge");
                _rb.velocity = new Vector2(xVel * 40f,0f);
                yield return null;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _rb.velocity = new Vector2(0f, 0f);
                _anim.Play("ZIdle");
                StartCoroutine(FancyOne());
            }
            
            IEnumerator Dash(float dir)
            {
                transform.Find("HyperCut").gameObject.SetActive(false);
                _anim.Play("ZDash");
                yield return null;
                yield return new WaitWhile(()=> _anim.GetCurrentFrame() < 5);
                PlayAudioClip("AudDashIntro");
                yield return new WaitWhile(()=> _anim.GetCurrentFrame() < 6);
                PlayAudioClip("AudDash");
                _rb.velocity = new Vector2(dir * 60f, 0f);
                yield return new WaitWhile(()=> _anim.GetCurrentFrame() < 9);
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(()=> _anim.IsPlaying());
                _anim.Play("ZIdle");
                transform.position = new Vector3(transform.position.x, GroundY, transform.position.z);
                StartCoroutine(FancyOne());
            }

            IEnumerator Leave(float dir)
            {
                _anim.Play("ZThrow2B");
                _rb.velocity = new Vector2(-dir * 40f, 40f);
                yield return new WaitForSeconds(0.05f);
                //Spring(false, transform.position + new Vector3(dir * 4f, 4,0f));
                yield return new WaitWhile(() => _anim.IsPlaying());
                ToggleZemer(false);
                _rb.velocity = Vector2.zero;
                StartCoroutine(BackIn(dir));
            }

            IEnumerator BackIn(float dir)
            {
                float x = (dir > 0) ? LeftX + 9f : RightX - 9f;
                float tarX = (dir > 0) ? LeftX + 4f : RightX - 4f;
                transform.position = new Vector3(x, GroundY + 6f);
                Spring(true, transform.position, 1.8f);
                yield return new WaitForSeconds(0.15f);
                ToggleZemer(true);
                var diff = new Vector2(x - tarX, transform.position.y - GroundY-0.95f);
                float rot = Mathf.Atan(diff.y / diff.x);
                rot = (tarX < 75f) ? rot + Mathf.PI : rot;
                _rb.velocity = new Vector2(55f*Mathf.Cos(rot), 55f*Mathf.Sin(rot));
                _anim.Play("Z1ZipIn");
                yield return null;
                yield return new WaitWhile(() => transform.position.y > GroundY-0.95f);
                transform.position = new Vector3(transform.position.x, GroundY-0.95f);
                _rb.velocity = new Vector2(-dir * 40f, 0f);
                _anim.Play("Z2Crawl");
                transform.position = new Vector3(transform.position.x, GroundY);
                yield return new WaitForSeconds(0.05f);
                _rb.velocity = new Vector2(0f, 0f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                _anim.Play("ZIdle");
                StartCoroutine(FancyOne());
            }
            
            IEnumerator FancyOne()
            {
                if (!IsFacingPlayer())
                {
                    Turn();
                    yield return new WaitForSeconds(TurnDelay);
                }
                float dir = FaceHero();
                
                _anim.Play("ZAtt2");
                yield return null;
                PlayAudioClip("AudBasicSlash1");
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                PlayAudioClip("AudBasicSlash2");
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 6);
                PlayAudioClip("AudBigSlash2");
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 7);
                SpawnPillar(dir, Vector2.one, 20f);
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("ZIdle");
                yield return new WaitForSeconds(0.2f);
                if (i == 0) StartCoroutine(Leave(dir));
                else _attacking = false;
                i++;
            }
            
            Position();
        }

        private void SweepDash()
        {
            IEnumerator Leave()
            {
                float dir = FaceHero();
                _anim.Play("ZThrow2B");
                _rb.velocity = new Vector2(-dir * 40f, 40f);
                yield return new WaitForSeconds(0.05f);
                //Spring(false, transform.position + new Vector3(dir * 4f, 4,0f));
                yield return new WaitWhile(() => _anim.IsPlaying());
                ToggleZemer(false);
                _rb.velocity = Vector2.zero;
                StartCoroutine(FlyStrike());
            }
            
            IEnumerator FlyStrike()
            {
                float heroX = _target.transform.position.x;
                float hVelX = _target.GetComponent<Rigidbody2D>().velocity.x;
                const float offset = 1.5f;
                float dir = FaceHero();
                if (hVelX > 0 && heroX + offset < RightX) heroX += offset;
                else if (hVelX <= 0 && heroX - offset > LeftX) heroX -= offset;
                else heroX = (dir > 0) ? heroX - 3f : heroX + 3f;
                float x = (heroX < 75f) ? heroX + 5 : heroX - 5;
                transform.position = new Vector3(x, GroundY + 9.5f);
                Spring(true, transform.position, 1.4f);
                yield return new WaitForSeconds(0.16f);
                ToggleZemer(true);
                
                heroX = _target.transform.position.x;
                dir = FaceHero();
                hVelX = _target.GetComponent<Rigidbody2D>().velocity.x;
                if (hVelX > 0 && heroX + offset < RightX) heroX += offset;
                else if (hVelX <= 0 && heroX - offset > LeftX) heroX -= offset;
                else heroX = (dir > 0) ? heroX - 3f : heroX + 3f;
                
                _anim.Play("Z4AirSweep");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);

                if ((int) FaceHero(true) == (int) dir)
                {
                    heroX = _target.transform.position.x;
                    hVelX = _target.GetComponent<Rigidbody2D>().velocity.x;
                    if (hVelX > 0 && heroX + offset < RightX) heroX += offset;
                    else if (hVelX <= 0 && heroX - offset > LeftX) heroX -= offset;
                    else heroX = (dir > 0) ? heroX - 3f : heroX + 3f;
                }

                if (FastApproximately(heroX, transform.GetPositionX(), 2.5f))
                {
                    heroX = (dir > 0) ? heroX - 3f : heroX + 3f;
                }
                var diff = new Vector2(x - heroX, transform.position.y - GroundY-0.95f);
                float rot = Mathf.Atan(diff.y / diff.x);
                rot = (Mathf.Sin(rot) > 0) ? rot + Mathf.PI : rot;
                _rb.velocity = new Vector2(65f*Mathf.Cos(rot), 65f*Mathf.Sin(rot));
                yield return new WaitWhile(() => 
                    transform.position.y > GroundY+2.5f && _anim.GetCurrentFrame() < 4);
                _anim.enabled = false;
                yield return new WaitWhile(() => transform.position.y > GroundY + 2.5f);
                _anim.enabled = true;
                yield return null;
                yield return new WaitWhile(() => 
                    transform.position.y > GroundY-0.3 && _anim.GetCurrentFrame() < 6);
                _anim.enabled = false;
                yield return new WaitWhile(() => transform.position.y > GroundY -0.3f);
                _anim.enabled = true;
                transform.position = new Vector3(transform.position.x, GroundY-0.3f);
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(()=> _anim.IsPlaying());
                
                StartCoroutine(Dash());
            }

            IEnumerator Dash()
            {
                float dir = FaceHero();
                transform.Find("HyperCut").gameObject.SetActive(false);
                _anim.PlayAt("ZDash", 4);
                PlayAudioClip("AudDash");
                _rb.velocity = new Vector2(-dir * 60f, 0f);
                yield return null;
                yield return new WaitWhile(()=> _anim.GetCurrentFrame() < 9);
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(()=> _anim.IsPlaying());
                _anim.Play("ZIdle");
                
                _attacking = false;
            }

            StartCoroutine(Leave());
        }
        
        private void RageCombo(float dir, bool dashes, int spinType)
        {
            IEnumerator Swing()
            {
                _lastAtt = null;
                _anim.Play("Z3Swing");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                SpawnPillar(dir, new Vector2(1.6f, 0.7f), 30f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 5);
                _rb.velocity = new Vector2(dir * 40f, 40f);
                yield return new WaitForSeconds(0.08f);

                _bc.enabled = false;
                
                //Spring(false, transform.position + new Vector3(dir * 4f, 4f,0f),1.5f);
                yield return new WaitWhile(() => _anim.IsPlaying());
                ToggleZemer(false, true);
                _rb.velocity = Vector2.zero;
                yield return new WaitForSeconds(0.1f); //0.15f
                StartCoroutine(FlyStrike());
            }

            IEnumerator FlyStrike()
            {
                float heroX = _target.transform.position.x;
                float hVelX = _target.GetComponent<Rigidbody2D>().velocity.x;
                const float offset = 1.5f;
                float dir = FaceHero();
                if (hVelX > 0 && heroX + offset < RightX) heroX += offset;
                else if (hVelX <= 0 && heroX - offset > LeftX) heroX -= offset;
                else heroX = (dir > 0) ? heroX - 3f : heroX + 3f;
                float x = (heroX < 75f) ? heroX + 5 : heroX - 5;
                transform.position = new Vector3(x, GroundY + 9.5f);
                Spring(true, transform.position, 1.4f);
                yield return new WaitForSeconds(0.16f);
                ToggleZemer(true);
                
                heroX = _target.transform.position.x;
                dir = FaceHero();
                hVelX = _target.GetComponent<Rigidbody2D>().velocity.x;
                if (hVelX > 0 && heroX + offset < RightX) heroX += offset;
                else if (hVelX <= 0 && heroX - offset > LeftX) heroX -= offset;
                else heroX = (dir > 0) ? heroX - 3f : heroX + 3f;
                
                _anim.Play("Z4AirSweep");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);

                if ((int) FaceHero(true) == (int) dir)
                {
                    heroX = _target.transform.position.x;
                    hVelX = _target.GetComponent<Rigidbody2D>().velocity.x;
                    if (hVelX > 0 && heroX + offset < RightX) heroX += offset;
                    else if (hVelX <= 0 && heroX - offset > LeftX) heroX -= offset;
                    else heroX = (dir > 0) ? heroX - 3f : heroX + 3f;
                }

                if (FastApproximately(heroX, transform.GetPositionX(), 2.5f))
                {
                    heroX = (dir > 0) ? heroX - 3f : heroX + 3f;
                }
                var diff = new Vector2(x - heroX, transform.position.y - GroundY-0.95f);
                float rot = Mathf.Atan(diff.y / diff.x);
                rot = (Mathf.Sin(rot) > 0) ? rot + Mathf.PI : rot;
                _rb.velocity = new Vector2(65f*Mathf.Cos(rot), 65f*Mathf.Sin(rot));
                yield return new WaitWhile(() => 
                    transform.position.y > GroundY+2.5f && _anim.GetCurrentFrame() < 4);
                _anim.enabled = false;
                yield return new WaitWhile(() => transform.position.y > GroundY + 2.5f);
                _anim.enabled = true;
                yield return null;
                yield return new WaitWhile(() => 
                    transform.position.y > GroundY-0.3 && _anim.GetCurrentFrame() < 6);
                _anim.enabled = false;
                yield return new WaitWhile(() => transform.position.y > GroundY -0.3f);
                _anim.enabled = true;
                transform.position = new Vector3(transform.position.x, GroundY-0.3f);
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(()=> _anim.IsPlaying());
                
                StartCoroutine(dashes ? LandSlide() : Dash()); //if far
            }

            IEnumerator Dash()
            {
                float dir = FaceHero();
                transform.Find("HyperCut").gameObject.SetActive(false);
                _anim.PlayAt("ZDash", 4);
                PlayAudioClip("AudDash");
                _rb.velocity = new Vector2(-dir * 60f, 0f);
                yield return null;
                yield return new WaitWhile(()=> _anim.GetCurrentFrame() < 9);
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(()=> _anim.IsPlaying());
                _anim.Play("ZIdle");
                
                _attacking = false;
            }

            IEnumerator LandSlide()
            {
                float signX = Mathf.Sign(gameObject.transform.GetPositionX() - 75f);
                Vector3 pScale = gameObject.transform.localScale;
                gameObject.transform.localScale = new Vector3(Mathf.Abs(pScale.x) * signX, pScale.y, 1f);
                _anim.Play("Z5LandSlide");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                _rb.velocity = new Vector2(-signX * 40f, 0f);
                _anim.enabled = false;
                yield return null;
                yield return new WaitWhile(
                    ()=>!FastApproximately(transform.position.x, 75.1f, 0.1f));
                _anim.enabled = true;
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                yield return new WaitWhile(() => _anim.IsPlaying());
                StartCoroutine(LaserNuts(1, spinType));
            }

            IEnumerator LaserNuts(int i, int type)
            {
                if (i == 1)
                {
                    _anim.Play("Z6LaserSpin", -1, 0f);
                    yield return null;
                    _anim.enabled = false;
                    yield return new WaitForSeconds(0.15f);
                    _anim.enabled = true;
                }
                else _anim.PlayAt("Z6LaserSpin", 1);
                transform.position = new Vector3(transform.position.x, GroundY - 1.2f);
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);

                if (type == 3)
                {
                    GameObject wav = Instantiate(FiveKnights.preloadedGO["WaveShad"]);
                    wav.GetComponent<SpriteRenderer>().material = ArenaFinder.Materials["TestDist"];
                    wav.transform.position = new Vector3(75.1f, 7.4f);
                    wav.SetActive(true);
                    wav.AddComponent<WaveIncrease>();
                }

                SpawnSlashes(type);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 9);
                
                if (i > 4)
                {
                    yield return new WaitWhile(() => _anim.IsPlaying());
                    transform.position = new Vector3(transform.position.x, GroundY);
                    FaceHero();
                    _anim.Play("ZIdle");
                    yield return null;
                    _anim.enabled = false;
                    yield return new WaitForSeconds(0.1f);
                    _anim.enabled = true;
                    _attacking = false;
                    yield break;
                }

                if (type != 3) type = (type % 2) + 1;
                StartCoroutine(LaserNuts(++i, type));
            }

            StartCoroutine(Swing());
        }
        
        private void Dash()
        {
            IEnumerator Dash()
            {
                float dir = FaceHero();
                transform.Find("HyperCut").gameObject.SetActive(true);
                _anim.Play("ZDash");
                yield return null;
                yield return new WaitWhile(()=> _anim.GetCurrentFrame() < 5);
                if (FastApproximately(_target.transform.position.x, transform.position.x, 5f))
                {
                    StartCoroutine(StrikeAlternate());
                    yield break;
                }
                PlayAudioClip("AudDashIntro");
                yield return new WaitWhile(()=> _anim.GetCurrentFrame() < 6);
                PlayAudioClip("AudDash");
                _rb.velocity = new Vector2(-dir * 60f, 0f);
                
                if (_hm.hp <= Phase3HP && _rand.Next(0, 5) < 2)
                {
                    StartCoroutine(LandSlide());
                    yield break;
                }
                
                yield return new WaitWhile(()=> _anim.GetCurrentFrame() < 7);
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(()=> _anim.GetCurrentFrame() < 9);
                Transform par = transform.Find("HyperCut");
                GameObject slashR = Instantiate(par.Find("Hyper4R").gameObject);
                GameObject slashL = Instantiate(par.Find("Hyper4L").gameObject);
                Rigidbody2D rbR = slashR.GetComponent<Rigidbody2D>();
                Rigidbody2D rbL = slashL.GetComponent<Rigidbody2D>();
                slashR.SetActive(true);
                slashL.SetActive(true);
                slashR.transform.position = par.Find("Hyper4R").position;
                slashL.transform.position = par.Find("Hyper4L").position;
                slashR.transform.localScale = Product(par.transform.localScale, new Vector3(dir,1f,1f));
                slashL.transform.localScale =  Product(par.transform.localScale, new Vector3(dir,1f,1f));
                rbL.velocity = new Vector2(-dir * 45f,0f);
                rbR.velocity = new Vector2(dir * 45f,0f);
                yield return new WaitWhile(()=> _anim.IsPlaying());
                _anim.Play("ZIdle");
                _attacking = false;
            }
            
            IEnumerator LandSlide()
            {
                int xSig = (int) Mathf.Sign(75f - transform.GetPositionX());
                int velSig = (int) Mathf.Sign(_rb.velocity.x);
                int typeMax = xSig == velSig ? 4 : 3;
                _anim.PlayAt("Z5LandSlide",1);
                _anim.enabled = false;
                yield return null;
                if (typeMax == 4) 
                    yield return new WaitWhile(
                    ()=>!FastApproximately(_rb.velocity.x, 0f, 0.1f) &&
                        !FastApproximately(transform.position.x, 75.1f, 0.25f));
                else yield return new WaitSecWhile(
                    ()=>!FastApproximately(_rb.velocity.x, 0f, 0.1f), 0.3f);
                _anim.enabled = true;
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                yield return new WaitWhile(() => _anim.IsPlaying());
                StartCoroutine(LaserNuts(1, _rand.Next(1,typeMax)));
            }

            IEnumerator LaserNuts(int i, int type)
            {
                if (i == 1)
                {
                    _anim.Play("Z6LaserSpin", -1, 0f);
                    yield return null;
                    _anim.enabled = false;
                    yield return new WaitForSeconds(0.15f);
                    _anim.enabled = true;
                }
                else _anim.PlayAt("Z6LaserSpin", 1);
                transform.position = new Vector3(transform.position.x, GroundY - 1.2f);
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);

                if (type == 3)
                {
                    GameObject wav = Instantiate(FiveKnights.preloadedGO["WaveShad"]);
                    wav.GetComponent<SpriteRenderer>().material = ArenaFinder.Materials["TestDist"];
                    wav.transform.position = new Vector3(75.1f, 7.4f);
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
                    yield return null;
                    _anim.enabled = false;
                    yield return new WaitForSeconds(0.1f);
                    _anim.enabled = true;
                    _attacking = false;
                    yield break;
                }

                if (type != 3) type = (type%2) +1;
                StartCoroutine(LaserNuts(++i, type));
            }
            
            IEnumerator StrikeAlternate()
            {
                float dir = FaceHero();
                
                _anim.Play("DashCounter");
                PlayAudioClip("Slash", 0.85f, 1.15f);
                yield return null;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("ZIdle");
                _attacking = false;
            }

            _lastAtt = this.Dash;
            StartCoroutine(Dash());
        }
        
        private void Dodge()
        {
            IEnumerator Dodge()
            {
                if (!IsFacingPlayer())
                {
                    Turn();
                    yield return new WaitForSeconds(TurnDelay);
                }
                float xVel = FaceHero() * -1f;

                _anim.Play("ZDodge");
                _rb.velocity = new Vector2(-xVel * 40f,0f);
                yield return null;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _rb.velocity = new Vector2(0f, 0f);
                _anim.Play("ZIdle");
                _attacking = false;
            }

            _lastAtt = this.Dodge;
            StartCoroutine(Dodge());
        }
        
        private void ZemerCounter()
        {
            float dir = 0f;
            IEnumerator CounterAntic()
            {
                if (!IsFacingPlayer())
                {
                    Turn();
                    yield return new WaitForSeconds(TurnDelay);
                }
                dir = FaceHero() * -1f;
                _anim.Play("ZCInit");
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
                yield return new WaitWhile(()=>_anim.IsPlaying());
                _attacking = false;
            }

            _lastAtt = this.ZemerCounter;
            _counterRoutine = StartCoroutine(CounterAntic());
        }
        
        private void SpinAttack()
        {
            IEnumerator SpinAttack()
            {
                if (!IsFacingPlayer())
                {
                    Turn();
                    yield return new WaitForSeconds(TurnDelay);
                }
                float xVel = FaceHero() * -1f;
                float diffX = Mathf.Abs(_target.transform.GetPositionX() - transform.GetPositionX());
                float diffY = Mathf.Abs(_target.transform.GetPositionY() - transform.GetPositionY());
                float rot = Mathf.Atan(diffY / diffX);
                rot = (xVel < 0) ? Mathf.PI - rot : rot; 
                _anim.Play("ZSpin");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                PlayAudioClip("AudDashIntro");
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
                _rb.velocity = new Vector2(60f * Mathf.Cos(rot), 60f * Mathf.Sin(rot));
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
                _anim.enabled = false;
                yield return new WaitWhile(() => transform.position.x > LeftX+4f && transform.position.x < RightX-4f);
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
                _attacking = false;
            }

            _lastAtt = this.SpinAttack;
            StartCoroutine(SpinAttack());
        }
        
        private void FancyAttack()
        {
            IEnumerator FancyAttack()
            {
                if (!IsFacingPlayer())
                {
                    Turn();
                    yield return new WaitForSeconds(TurnDelay);
                }
                float dir = FaceHero();
                
                _anim.Play("ZAtt2");
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
                _attacking = false;
            }

            _lastAtt = this.FancyAttack;
            StartCoroutine(FancyAttack());
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
                if (shouldNotDoPhase2)
                {
                    yield return new WaitForSeconds(1.75f);
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
                _rb.velocity = new Vector2(dir * 35f, 35f);
                yield return new WaitForSeconds(0.1f);
                //Spring(false, transform.position + new Vector3(dir * 4f, 4,0f));
                yield return new WaitWhile(() => _anim.IsPlaying());
                ToggleZemer(false, true);
                _rb.velocity = Vector2.zero;
                yield return new WaitForSeconds(1.5f);
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
                yield return new WaitForSeconds(0.15f);
                ToggleZemer(true);
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
                _rb.velocity = new Vector2(0f, 0f);
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("Z3Swing");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                SpawnPillar(-dir, new Vector2(1.4f, 0.65f), 25f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 5);
                _rb.velocity = new Vector2(dir * 40f, 40f);
                yield return new WaitForSeconds(0.08f);
                //Spring(false, transform.position + new Vector3(dir * 4f, 4f,0f),1.5f);
                yield return new WaitWhile(() => _anim.IsPlaying());
                ToggleZemer(false, true);
                _rb.velocity = Vector2.zero;
                yield return new WaitForSeconds(0.1f); //0.15f
                StartCoroutine(FlyStrike());
            }

            IEnumerator FlyStrike()
            {
                float heroX = _target.transform.position.x;
                float hVelX = _target.GetComponent<Rigidbody2D>().velocity.x;
                const float offset = 1.5f;
                float dir = FaceHero();
                if (hVelX > 0 && heroX + offset < RightX) heroX += offset;
                else if (hVelX <= 0 && heroX - offset > LeftX) heroX -= offset;
                else heroX = (dir > 0) ? heroX - 3f : heroX + 3f;
                float x = (heroX < 75f) ? heroX + 5 : heroX - 5;
                transform.position = new Vector3(x, GroundY + 9.5f);
                Spring(true, transform.position, 1.4f);
                yield return new WaitForSeconds(0.16f);
                ToggleZemer(true);
                
                heroX = _target.transform.position.x;
                dir = FaceHero();
                hVelX = _target.GetComponent<Rigidbody2D>().velocity.x;
                if (hVelX > 0 && heroX + offset < RightX) heroX += offset;
                else if (hVelX <= 0 && heroX - offset > LeftX) heroX -= offset;
                else heroX = (dir > 0) ? heroX - 3f : heroX + 3f;
                
                _anim.Play("Z4AirSweep");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);

                if ((int) FaceHero(true) == (int) dir)
                {
                    heroX = _target.transform.position.x;
                    hVelX = _target.GetComponent<Rigidbody2D>().velocity.x;
                    if (hVelX > 0 && heroX + offset < RightX) heroX += offset;
                    else if (hVelX <= 0 && heroX - offset > LeftX) heroX -= offset;
                    else heroX = (dir > 0) ? heroX - 3f : heroX + 3f;
                }

                if (FastApproximately(heroX, transform.GetPositionX(), 2.5f))
                {
                    heroX = (dir > 0) ? heroX - 3f : heroX + 3f;
                }
                var diff = new Vector2(x - heroX, transform.position.y - GroundY-0.95f);
                float rot = Mathf.Atan(diff.y / diff.x);
                rot = (Mathf.Sin(rot) > 0) ? rot + Mathf.PI : rot;
                _rb.velocity = new Vector2(65f*Mathf.Cos(rot), 65f*Mathf.Sin(rot));
                yield return new WaitWhile(() => 
                    transform.position.y > GroundY+2.5f && _anim.GetCurrentFrame() < 4);
                _anim.enabled = false;
                yield return new WaitWhile(() => transform.position.y > GroundY + 2.5f);
                _anim.enabled = true;
                yield return null;
                yield return new WaitWhile(() => 
                    transform.position.y > GroundY-0.3 && _anim.GetCurrentFrame() < 6);
                _anim.enabled = false;
                yield return new WaitWhile(() => transform.position.y > GroundY -0.3f);
                _anim.enabled = true;
                transform.position = new Vector3(transform.position.x, GroundY-0.3f);
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(()=> _anim.IsPlaying());
                StartCoroutine(LandSlide());
            }

            IEnumerator LandSlide()
            {
                dir = FaceHero();
                _anim.Play("Z5LandSlide");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                _rb.velocity = new Vector2(-dir * 40f, 0f);
                _anim.enabled = false;
                yield return null;
                yield return new WaitSecWhile(
                    ()=>!FastApproximately(_rb.velocity.x, 0f, 0.1f), 0.15f);
                _anim.enabled = true;
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
                _anim.enabled = false;
                yield return new WaitForSeconds(0.15f);
                _anim.enabled = true;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                SpawnSlashes(_rand.Next(1,3));
                yield return new WaitWhile(() => _anim.IsPlaying());
                transform.position = new Vector3(transform.position.x, GroundY);
                _anim.Play("ZIdle");
                _attacking = false;
                FaceHero();
            }
            
            StartCoroutine(KnockedOut());
        }
        
        void SpawnPillar(float dir, Vector2 size, float xSpd)
        {
            GameObject slam = traitorSlam[traitorSlamIndex++ % 2];
            Animator anim = slam.transform.Find("slash_core").GetComponent<Animator>();
            slam.SetActive(true);
            anim.enabled = true;
            anim.Play("mega_mantis_slash_big",-1,0f);
            PlayAudioClip("TraitorPillar");
            Rigidbody2D rb = slam.GetComponent<Rigidbody2D>();
            rb.velocity = new Vector2(-dir * xSpd,0f);
            Vector3 pos = transform.position;
            slam.transform.position = new Vector3(-dir*2.15f + pos.x, GroundY - 3.2f, 6.4f);
            slam.transform.localScale = new Vector3(-dir * size.x, size.y, 1f);
        }

        private bool offsetAngle;
        private void SpawnSlashes(int type = 0)
        {
            Log("Laser Pattern " + type);
            IEnumerator Pattern3()
            {
                string typ = "SlashBeam2";
                GameObject origSlashA = FiveKnights.preloadedGO[typ].transform.Find("SlashA").gameObject;
                GameObject origSlashB = FiveKnights.preloadedGO[typ].transform.Find("SlashB").gameObject;
                IList<GameObject> SlashA = new List<GameObject>();
                IList<GameObject> SlashB = new List<GameObject>();
                int origA = (int) origSlashA.transform.GetRotation2D();
                int origB = (int) origSlashB.transform.GetRotation2D();
                for (int i = origA, j = origB; i < 270+origA; i += 35, j -= 35)
                {
                    GameObject slashA = Instantiate(origSlashA);
                    GameObject slashB = Instantiate(origSlashB);
                    slashA.SetActive(false);
                    slashB.SetActive(false);
                    slashA.transform.SetRotation2D(i + (offsetAngle ? 20f : 0f));
                    slashB.transform.SetRotation2D(j - (offsetAngle ? 0f : 20f));
                    slashA.transform.position = new Vector3(transform.position.x,7.4f);
                    slashB.transform.position = new Vector3(transform.position.x,7.4f);
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

                Animator anim2 = SlashA[SlashA.Count-1].GetComponent<Animator>();
                yield return new WaitWhile(() => anim2.IsPlaying());

                for (int i = 0; i < SlashA.Count; i++)
                {
                    Destroy(SlashA[i]);
                    Destroy(SlashB[i]);
                }
            }
            
            IEnumerator Pattern1()
            {
                List<float> lstRight = new List<float>();
                List<float> lstLeft = new List<float>();
                for (float i = LeftX+3; i < RightX + 3; i += UnityEngine.Random.Range(7.5f,8.2f)) lstRight.Add(i);
                for (float i = RightX-3; i > LeftX - 3; i -= UnityEngine.Random.Range(7.5f,8.2f)) lstLeft.Add(i);
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
                for (float i = LeftX+3; i < RightX + 3; i += UnityEngine.Random.Range(7.5f,8.2f)) lstRight.Add(i);
                for (float i = RightX-3; i > LeftX - 3; i -= UnityEngine.Random.Range(7.5f,8.2f)) lstLeft.Add(i);
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
            }
            
            IEnumerator Randomized()
            {
                int zemX = (int) transform.position.x;
                List<int> lstRight = new List<int>();
                List<int> lstLeft = new List<int>();
                for (int i = zemX+5; i < RightX + 3; i += _rand.Next(3,6)) lstRight.Add(i);
                for (int i = zemX-5; i > LeftX -3; i -= _rand.Next(3,6)) lstLeft.Add(i);
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
                yield return new WaitForSeconds(_rand.Next(12,25)/100f);
                GameObject slash = Instantiate(FiveKnights.preloadedGO["SlashBeam"]);
                Animator anim = slash.GetComponent<Animator>();
                //slash.transform.localScale *= 1.43f;
                slash.transform.position = new Vector3(posX, GroundY+4.5f); //GroundY - 1.3f
                slash.transform.SetRotation2D(angle);
                slash.SetActive(true);
                anim.enabled = true;
                anim.speed /= 1.5f;
                Vector3 vec = slash.transform.localScale;
                slash.transform.localScale = new Vector3(scaleSig *vec.x, vec.y, vec.z);
            }

            IEnumerator run = type switch
            {
                1 => Pattern1(),
                2 => Pattern2(),
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
        
        private void Turn()
        {
            IEnumerator Turn()
            {
                _anim.Play("ZTurn");
                yield return new WaitForSeconds(TurnDelay);
            }
            StartCoroutine(Turn());
        }
        
        private void Spring(bool isIn, Vector2 pos, float speedSca = 1f)
        {
            string n =  "VapeIn2";
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
                    GameManager.instance.StartCoroutine(GameManager.instance.FreezeMoment(0.04f, 0.2f, 0.04f, 0f));
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
            yield return new WaitWhile(() => _anim.GetCurrentFrame()<15);
            PlayAudioClip("Slash", 0.85f, 1.15f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            Parryable.ParryFlag = false;
            _anim.Play("ZIdle");
            yield return new WaitForSeconds(0.4f);
            _attacking = false;
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
                Log("IN1");
                if (_hm.hp <= 50)
                {
                    Log("IN2");
                    Log("Going to die :(");
                    StopAllCoroutines();
                    StartCoroutine(Death());
                }
            }
            orig(self, hitInstance);
        }

        IEnumerator Death()
        {
            GameObject extraNail = GameObject.Find("ZNailB");
            if (extraNail != null && extraNail.transform.parent == null)
            {
                Destroy(extraNail);
            }
            float dir = -FaceHero();
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
            CustomWP.Instance.wonLastFight = true;
            // Stop music here.
            Destroy(this);
        }
        
        private float FaceHero(bool onlyCalc = false)
        {
            float heroSignX = Mathf.Sign(gameObject.transform.GetPositionX() - _target.transform.GetPositionX());
            if (onlyCalc) return heroSignX;
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
        
        private void PlayAudioClip(string clipName, float pitchMin = 1.0f, float pitchMax = 1.0f, float time = 0.0f)
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
                    case "TraitorPillar":
                        return ArenaFinder.Clips["TraitorSlam"];
                    default:
                        return ArenaFinder.Clips[clipName];
                }
            }

            _ap.MaxPitch = pitchMax;
            _ap.MinPitch = pitchMin;
            _ap.Clip = GetAudioClip();
            _ap.DoPlayRandomClip();
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
                _rb.gravityScale = 0f;
                Color col = _sr.color;
                _sr.color = new Color(col.r, col.g, col.b, visible ? 1f : 0f);
            }
            
            _bc.enabled = false;
            if (fade) StartCoroutine(Fade());
            else Instant();
        }
        
        private void AssignFields()
        {
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
                Spawn = HeroController.instance.gameObject
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
            
            _moves = new List<Action>
            {
                ZemerCounter,
                AerialAttack,
                Attack1Base,
                Attack1Complete,
                NailLaunch,
                Dash,
                Dodge,
                SpinAttack,
                FancyAttack,
                DoubleFancy,
                SweepDash
            };

            _repeats = new Dictionary<Action, int>
            {
                [ZemerCounter] = 0,
                [Attack1Base] = 0,
                [AerialAttack] = 0,
                [Attack1Complete] = 0,
                [NailLaunch] = 0,
                [Dash] = 0,
                [Dodge] = 0,
                [SpinAttack] = 0,
                [FancyAttack] = 0,
                [DoubleFancy] = 0,
                [SweepDash] = 0
            };

            _maxRepeats = new Dictionary<Action, int>
            {
                [ZemerCounter] = 1,
                [Attack1Base] = 2,
                [Attack1Complete] = 2,
                [AerialAttack] = 2,
                [NailLaunch] = 2,
                [Dash] = 2,
                [Dodge] = 2,
                [SpinAttack] = 2,
                [FancyAttack] = 2,
                [DoubleFancy] = 1,
                [SweepDash] = 1
            };
        }

        private void Log(object o)
        {
            Modding.Logger.Log("[Zemer2] " + o);
        }

        private void OnDestroy()
        {
            On.HealthManager.Hit -= OnBlockedHit;
            On.HealthManager.TakeDamage -= HealthManager_TakeDamage;
            On.EnemyDreamnailReaction.RecieveDreamImpact -= OnReceiveDreamImpact;
        }
    }
}
