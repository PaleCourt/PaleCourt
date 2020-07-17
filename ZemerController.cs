using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ModCommon;
using System.Collections;
using HutongGames.PlayMaker.Actions;
using ModCommon.Util;
using System.Reflection;
using TMPro;

namespace FiveKnights
{
    public class ZemerController : MonoBehaviour
    {
        private HealthManager _hm;
        private BoxCollider2D _bc;
        private SpriteRenderer _sr;
        private EnemyDreamnailReaction _dnailReac;
        private GameObject _dd;
        private GameObject _dnailEff;
        private Animator _anim;
        private Rigidbody2D _rb;
        private System.Random _rand;
        private EnemyHitEffectsUninfected _hitEffects;
        private GameObject _target;
        private const float GroundY = 9.75f;
        private const float LeftX = 61.5f;
        private const float RightX = 91.6f;
        private const int Phase2HP = 200;
        private const int MaxHP = 300 + Phase2HP;
        private bool doingIntro;
        private PlayMakerFSM _pvFsm;
        private GameObject[] traitorSlam;
        private int traitorSlamIndex;
        private Coroutine _counterRoutine;
        private bool _blockedHit;
        private const float TurnDelay = 0.05f;
        private bool _attacking;
        private MusicPlayer _ap;
        private List<Action> _moves;
        private Dictionary<Action, int> _maxRepeats;
        private Dictionary<Action, int> _repeats;

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
            _hm = gameObject.AddComponent<HealthManager>();
            _anim = gameObject.GetComponent<Animator>();
            _bc = gameObject.GetComponent<BoxCollider2D>();
            _rb = gameObject.GetComponent<Rigidbody2D>();
            _rb.isKinematic = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _sr = GetComponent<SpriteRenderer>();
            _dnailReac = gameObject.AddComponent<EnemyDreamnailReaction>();
            gameObject.AddComponent<AudioSource>();
            gameObject.AddComponent<DamageHero>().damageDealt = 1;
            _dd = FiveKnights.preloadedGO["WD"];
            _dnailEff = _dd.GetComponent<EnemyDreamnailReaction>().GetAttr<EnemyDreamnailReaction, GameObject>("dreamImpactPrefab");
            _dnailReac.enabled = true;
            _rand = new System.Random();
            _dnailReac.SetConvoTitle(_dnailDial[_rand.Next(_dnailDial.Length)]);
            _hitEffects = gameObject.AddComponent<EnemyHitEffectsUninfected>();
            _hitEffects.enabled = true;
            gameObject.AddComponent<Flash>();
            _pvFsm = FiveKnights.preloadedGO["PV"].LocateMyFSM("Control");
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
        }

        private IEnumerator Start()
        {
            Log("Start");
            GameCameras.instance.tk2dCam.ZoomFactor = 0.75f;
            _hm.hp = MaxHP;
            _bc.enabled = doingIntro = false;
            gameObject.transform.localScale *= 0.9f;
            gameObject.layer = 11;
            yield return new WaitWhile(() => !(_target = HeroController.instance.gameObject));
            Destroy(GameObject.Find("Bounds Cage"));
            Destroy(GameObject.Find("World Edge v2"));
            if (!WDController.alone) StartCoroutine(SilLeave());
            else yield return new WaitForSeconds(1.7f);
            gameObject.SetActive(true);
            gameObject.transform.position = new Vector2(80f, GroundY + 0.5f);
            FaceHero();
            AssignFields();
            _bc.enabled = false;
            _sr.enabled = false;
            //Spring(true, gameObject.transform.position);
            yield return new WaitForSeconds(0.2f);
            _anim.Play("ZIntro");
            _sr.enabled = true;
            yield return null;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
            gameObject.transform.position = new Vector2(80f, GroundY);
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
            _anim.enabled = false;
            GameObject area = null;
            foreach (GameObject i in FindObjectsOfType<GameObject>().Where(x => x.name.Contains("Area Title Holder")))
            {
                area = i.transform.Find("Area Title").gameObject;
            }

            yield return new WaitForSeconds(0.3f);
            StartCoroutine(ChangeIntroText(Instantiate(area), "Zemer", "", "Mysterious", false));
            _bc.enabled = doingIntro = true;
            _anim.enabled = true;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 12);
            _anim.enabled = false;
            yield return new WaitForSeconds(0.8f);
            _anim.enabled = true;
            yield return new WaitWhile(() => _anim.IsPlaying());
            _anim.Play("ZWalk");
            _rb.velocity = new Vector2(7f, 0f);
            yield return new WaitWhile(() => transform.GetPositionX() < RightX - 5f);
            _rb.velocity = Vector2.zero;
            doingIntro = false;
            _anim.Play("ZIdle");
            StartCoroutine(Attacks());
            Log("Done Intro");
        }

        private void Update()
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

        private IEnumerator Attacks()
        {
            Log("[Waiting to start calculation]");
            yield return new WaitWhile(() => _attacking);
            yield return new WaitForSeconds(0.2f);
            Log("[End of Wait]");
            _attacking = true;
            Log("[Setting Attacks]");
            Vector2 posZem = transform.position;
            Vector2 posH = _target.transform.position;
            List<Action> toDo = new List<Action>();

            //If the player is close
            if (posH.y > 19f && (posH.x <= 60.4 || posH.x >= 91.6f))
            {
                Action[] lst = {SpinAttack};
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

            Action[] attLst = {Dash, Attack1Base, Attack1Base, AerialAttack};
            Action currAtt = attLst[_rand.Next(0, attLst.Length)];
            while (_repeats[currAtt] >= _maxRepeats[currAtt])
            {
                currAtt = attLst[_rand.Next(0, attLst.Length)];
            }

            toDo.Add(currAtt);
            Log("Added " + currAtt.Method.Name);

            if (currAtt == Attack1Base)
            {
                //REM
                Action[] lst2 = {Dash, Dash, FancyAttack, FancyAttack, AerialAttack, null};
                currAtt = lst2[_rand.Next(0, lst2.Length)];
                if (currAtt != null)
                {
                    toDo.Add(currAtt);
                    Log("Added " + currAtt.Method.Name);
                }

                if (currAtt == FancyAttack && _rand.Next(0, 3) == 0)
                {
                    toDo.Add(Dodge);
                    toDo.Add(currAtt);
                    toDo.Add(Dash);
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
                act.Invoke();
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

        private void AerialAttack()
        {
            IEnumerator Attack()
            {
                if (!IsFacingPlayer())
                {
                    Turn();
                    yield return new WaitForSeconds(TurnDelay);
                }

                float xVel = FaceHero() * -1f;
                transform.Find("BladeAerialShadow").gameObject.SetActive(false);
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
                _attacking = false;
            }

            StartCoroutine(Attack());
        }

        private void Walk(float displacement)
        {
            IEnumerator Walk()
            {
                float xPos = transform.position.x;
                string animName = (displacement < 0) ? "ZWalk" : "ZWalkLeft";

                _anim.Play(animName);
                yield return null;
                _rb.velocity = new Vector2(Mathf.Sign(displacement) * 7f, 0f);
                yield return new WaitWhile
                (
                    () =>
                        !FastApproximately(_rb.velocity.x, 0f, 0.1f) && !FastApproximately(xPos + displacement, transform.position.x, 0.1f)
                );
                _rb.velocity = Vector2.zero;
                _anim.Play("ZIdle");
                yield return new WaitForSeconds(0.5f);
                _attacking = false;
            }

            StartCoroutine(Walk());
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

        //Stolen from Jngo
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
                _hm.IsInvincible = true;
                _anim.Play("ZCIdle");
                _blockedHit = false;
                On.HealthManager.Hit += OnBlockedHit;
                PlayAudioClip("Counter");
                StartCoroutine(FlashWhite());


                Vector2 fxPos = transform.position + Vector3.right * (1.7f * dir) + Vector3.up * 0.8f;
                Quaternion fxRot = Quaternion.Euler(0, 0, dir * 80f);
                GameObject counterFx = Instantiate(FiveKnights.preloadedGO["CounterFX"], fxPos, fxRot);
                counterFx.SetActive(true);
                yield return new WaitForSeconds(0.35f);

                _counterRoutine = StartCoroutine(CounterEnd());
            }

            IEnumerator CounterEnd()
            {
                _hm.IsInvincible = false;
                On.HealthManager.Hit -= OnBlockedHit;
                _anim.Play("ZCCancel");
                yield return new WaitWhile(() => _anim.IsPlaying());
                _attacking = false;
            }

            Log("DO1");
            _counterRoutine = StartCoroutine(CounterAntic());
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
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 8);
                SpawnPillar(dir);
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("ZIdle");
                _attacking = false;
            }

            void SpawnPillar(float dir)
            {
                GameObject slam = traitorSlam[traitorSlamIndex++ % 2];
                Animator anim = slam.transform.Find("slash_core").GetComponent<Animator>();
                slam.SetActive(true);
                anim.enabled = true;
                anim.Play("mega_mantis_slash_big", -1, 0f);
                PlayAudioClip("TraitorPillar");
                Rigidbody2D rb = slam.GetComponent<Rigidbody2D>();
                rb.velocity = new Vector2(-dir * 15f, 0f);
                Vector3 pos = transform.position;
                slam.transform.position = new Vector3(-dir * 4.4f + pos.x, GroundY - 3.2f, 6.4f);
                slam.transform.localScale = new Vector3(-dir, 1f, 1f);
            }

            StartCoroutine(FancyAttack());
        }

        // Put these IEnumerators outside so that they can be started in OnBlockedHit
        private IEnumerator Countered()
        {
            _anim.Play("ZCAtt");
            _hm.IsInvincible = false;
            On.HealthManager.Hit -= OnBlockedHit;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 15);
            PlayAudioClip("Slash", 0.85f, 1.15f);
            yield return new WaitWhile(() => _anim.IsPlaying());

            _anim.Play("ZIdle");
            yield return new WaitForSeconds(0.4f);
            _attacking = false;
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
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 7);
                PlayAudioClip("Slash", 0.85f, 1.15f);
                _rb.velocity = new Vector2(23f * xVel, 0f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 11);
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _attacking = false;
            }

            StartCoroutine(Attack1Base());
        }

        private void Dash()
        {
            IEnumerator Dash()
            {
                if (!IsFacingPlayer())
                {
                    Turn();
                    yield return new WaitForSeconds(TurnDelay);
                }

                float dir = FaceHero();
                transform.Find("HyperCut").gameObject.SetActive(false);

                _anim.Play("ZDash");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 5);
                PlayAudioClip("AudDashIntro");
                if (FastApproximately(_target.transform.position.x, transform.position.x, 5f))
                {
                    StartCoroutine(StrikeAlternate());
                    yield break;
                }

                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 6);
                PlayAudioClip("AudDash");
                _rb.velocity = new Vector2(-dir * 60f, 0f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 9);
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("ZIdle");
                transform.position = new Vector3(transform.position.x, GroundY, transform.position.z);
                _attacking = false;
            }

            IEnumerator StrikeAlternate()
            {
                if (!IsFacingPlayer())
                {
                    Turn();
                    yield return new WaitForSeconds(TurnDelay);
                }

                float dir = FaceHero();
                _anim.Play("DashCounter");
                PlayAudioClip("Slash", 0.85f, 1.15f);
                yield return null;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("ZIdle");
                _attacking = false;
            }

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
                _rb.velocity = new Vector2(-xVel * 40f, 0f);
                yield return null;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _rb.velocity = new Vector2(0f, 0f);
                _anim.Play("ZIdle");
                _attacking = false;
            }

            StartCoroutine(Dodge());
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
                _attacking = false;
            }

            StartCoroutine(SpinAttack());
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

                    StartCoroutine(Countered());
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
                _hitEffects.RecieveHitEffect(hitInstance.Direction);

                if (doingIntro)
                {
                    StopCoroutine(nameof(Start));
                    _rb.velocity = new Vector2(0f, 0f);
                    _attacking = true;
                    doingIntro = false;
                    _bc.enabled = true;
                    _anim.enabled = true;
                    ZemerCounter();
                    StartCoroutine(Attacks());
                }

                if (_hm.hp <= Phase2HP)
                {
                    Log("Going to phase 2");
                    _bc.enabled = false;
                    GameObject extraNail = GameObject.Find("ZNailB");
                    if (extraNail != null && extraNail.transform.parent == null)
                    {
                        Destroy(extraNail);
                    }

                    OnDestroy();
                    gameObject.AddComponent<ZemerControllerP2>().DoPhase =
                        CustomWP.boss == CustomWP.Boss.Zemer;
                    Destroy(this);
                }
            }

            orig(self, hitInstance);
        }

        private IEnumerator SilLeave()
        {
            SpriteRenderer sil = GameObject.Find("Silhouette Zemer").GetComponent<SpriteRenderer>();
            sil.transform.localScale *= 1.15f;
            sil.gameObject.AddComponent<Rigidbody2D>().velocity = new Vector2(0f, 10f);
            sil.gameObject.GetComponent<Rigidbody2D>().gravityScale = 0f;
            sil.sprite = ArenaFinder.Sprites["Zem_Sil_1"];
            yield return new WaitForSeconds(0.05f);
            sil.sprite = ArenaFinder.Sprites["Zem_Sil_2"];
            yield return new WaitForSeconds(0.05f);
            sil.sprite = ArenaFinder.Sprites["Zem_Sil_3"];
            yield return new WaitForSeconds(0.05f);
            sil.gameObject.SetActive(false);
        }

        private float FaceHero(bool shouldRev = false)
        {
            float currSign = Mathf.Sign(transform.GetScaleX());
            float heroSignX = Mathf.Sign(gameObject.transform.GetPositionX() - _target.transform.GetPositionX());
            heroSignX = shouldRev ? -1 * heroSignX : heroSignX;
            Vector3 pScale = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector3(Mathf.Abs(pScale.x) * heroSignX, pScale.y, 1f);
            return heroSignX;
        }

        private bool IsFacingPlayer()
        {
            int sigZem = (int) Mathf.Sign(transform.localScale.x);
            int sigDiff = (int) Mathf.Sign(transform.position.x - _target.transform.position.x);
            return sigZem == sigDiff;
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
        }

        private void AssignFields()
        {
            HealthManager hornHP = _dd.GetComponent<HealthManager>();
            foreach (FieldInfo fi in typeof(HealthManager).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                                                          .Where(x => x.Name.Contains("Prefab")))
            {
                fi.SetValue(_hm, fi.GetValue(hornHP));
            }

            EnemyHitEffectsUninfected ogrimHitEffects = _dd.GetComponent<EnemyHitEffectsUninfected>();

            foreach (FieldInfo fi in typeof(EnemyHitEffectsUninfected).GetFields())
            {
                if (fi.Name.Contains("Origin"))
                {
                    _hitEffects.effectOrigin = new Vector3(0f, 0.5f, 0f);
                    continue;
                }

                fi.SetValue(_hitEffects, fi.GetValue(ogrimHitEffects));
            }

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

            _moves = new List<Action>
            {
                ZemerCounter,
                Attack1Base,
                Attack1Complete,
                Dash,
                Dodge,
                SpinAttack,
                AerialAttack,
                FancyAttack,
            };

            _repeats = new Dictionary<Action, int>
            {
                [ZemerCounter] = 0,
                [Attack1Base] = 0,
                [Attack1Complete] = 0,
                [Dash] = 0,
                [Dodge] = 0,
                [SpinAttack] = 0,
                [AerialAttack] = 0,
                [FancyAttack] = 0
            };

            _maxRepeats = new Dictionary<Action, int>
            {
                [ZemerCounter] = 1,
                [Attack1Base] = 2,
                [Attack1Complete] = 2,
                [Dash] = 2,
                [Dodge] = 2,
                [SpinAttack] = 2,
                [AerialAttack] = 2,
                [FancyAttack] = 2
            };
        }

        private void PlayAudioClip(string clipName, float pitchMin = 1.0f, float pitchMax = 1.0f, float time = 0.0f)
        {
            AudioClip GetAudioClip()
            {
                switch (clipName)
                {
                    case "Counter":
                        return (AudioClip) _pvFsm.GetAction<AudioPlayerOneShotSingle>("Counter Stance", 1)
                                                 .audioClip
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

        private void Log(object o)
        {
            Modding.Logger.Log("[Zemer] " + o);
        }

        private void OnDestroy()
        {
            On.HealthManager.Hit -= OnBlockedHit;
            On.HealthManager.TakeDamage -= HealthManager_TakeDamage;
            On.EnemyDreamnailReaction.RecieveDreamImpact -= OnReceiveDreamImpact;
        }
    }
}