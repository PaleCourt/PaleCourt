using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FiveKnights.BossManagement;
using HutongGames.PlayMaker.Actions;
using SFCore.Utils;
using UnityEngine;
using Vasi;

namespace FiveKnights.Zemer
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
        private readonly float GroundY = (OWArenaFinder.IsInOverWorld) ? 108.3f : 
            (CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim) ? 9f : 28.8f;
        private readonly float LeftX = (OWArenaFinder.IsInOverWorld) ? 240.1f : 
            (CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim) ? 61.0f : 11.2f;
        private readonly float RightX = (OWArenaFinder.IsInOverWorld) ? 273.9f : 
            (CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim) ? 91.0f : 45.7f;
        private readonly float SlamY = (OWArenaFinder.IsInOverWorld) ? 105f : 
            (CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim) ? 6.5f : 25.9f;
        private const int Phase2HP = 200;
        private const int MaxHPV2 = 500 + Phase2HP;
        private const int MaxHPV1 = 1200;
        private const int DoSpinSlashPhase = 900;
        private bool doingIntro;
        private PlayMakerFSM _pvFsm;
        private GameObject[] traitorSlam;
        private int traitorSlamIndex;
        private Coroutine _counterRoutine;
        private bool _blockedHit;
        private const float DashDelay = 0.18f;
        private const float TurnDelay = 0.05f;
        private float Att1BaseDelay = 0.4f;
        private const float SheoAttDelay = 0.2f;
        private const float WalkSpeed = 10f;
        private const float AerialDelay = 0.25f;
        private const float TwoFancyDelay = 0.25f;
        private bool _countering;
        private MusicPlayer _ap;
        public static bool WaitForTChild = false;
        private const int MaxDreamAmount = 3;

        private void Awake()
        {
            OnDestroy();
            On.HealthManager.TakeDamage += HealthManager_TakeDamage;
            On.EnemyDreamnailReaction.RecieveDreamImpact += OnReceiveDreamImpact;
            On.SpellFluke.DoDamage += SpellFlukeOnDoDamage;
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
            Mirror.SetField(_dnailReac, "convoAmount", MaxDreamAmount);

            _rand = new System.Random();
            _hitEffects = gameObject.AddComponent<EnemyHitEffectsUninfected>();
            _hitEffects.enabled = true;
            gameObject.AddComponent<Flash>();
            _pvFsm = FiveKnights.preloadedGO["PV"].LocateMyFSM("Control");
            traitorSlam = new GameObject[2];
            traitorSlamIndex = 0;
            string partic = (CustomWP.boss == CustomWP.Boss.All) ? "ZemParticDung" : "ZemParticPetal";
            FiveKnights.preloadedGO["TraitorSlam"].transform.Find("Grass").GetComponent<ParticleSystemRenderer>()
                .material.mainTexture = ArenaFinder.Sprites[partic].texture;
            for (int i = 0; i < traitorSlam.Length; i++)
            {
                traitorSlam[i] = Instantiate(FiveKnights.preloadedGO["TraitorSlam"]);
                Destroy(traitorSlam[i].GetComponent<AutoRecycleSelf>());
                traitorSlam[i].transform.Find("slash_core").Find("hurtbox").GetComponent<DamageHero>().damageDealt = 1;
                traitorSlam[i].SetActive(false);
            }
        }

        private void DoTitle()
        {
            GameObject area = null;
            foreach (GameObject i in FindObjectsOfType<GameObject>().Where(x => x.name.Contains("Area Title Holder")))
            {
                area = i.transform.Find("Area Title").gameObject;
            }

            string title = CustomWP.boss == CustomWP.Boss.Ze ? "Mysterious" : "Mystic";
            area = Instantiate(area);
            area.SetActive(true);
            AreaTitleCtrl.ShowBossTitle(
                this, area, 2f, 
                "","","",
                "Ze'mer",title);
        }

        private IEnumerator Start()
        {
            _hm.hp = CustomWP.boss == CustomWP.Boss.Ze ? MaxHPV1 : MaxHPV2;
            _bc.enabled = doingIntro = false;
            gameObject.transform.localScale *= 0.8f;
            gameObject.layer = 11;
            yield return new WaitWhile(() => !(_target = HeroController.instance.gameObject));
            Destroy(GameObject.Find("Bounds Cage"));
            Destroy(GameObject.Find("World Edge v2"));
			if(!GGBossManager.alone && !OWArenaFinder.IsInOverWorld) StartCoroutine(SilLeave());
			else yield return new WaitForSeconds(1.7f);
			//StartCoroutine(MusicControl());
			gameObject.SetActive(true);
            gameObject.transform.position = OWArenaFinder.IsInOverWorld ? 
                    new Vector2(254f, GroundY + 0.5f) : 
                    new Vector2(RightX - 10f, GroundY + 0.5f);
            
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
            gameObject.transform.position = new Vector2(OWArenaFinder.IsInOverWorld ? 254 : RightX - 10f, GroundY);
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
            _anim.enabled = false;
            yield return new WaitForSeconds(0.3f);
            
            yield return new WaitWhile(() => WaitForTChild);
            StartCoroutine(MusicControl());
            DoTitle();
            doingIntro = true;

            _anim.enabled = true;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 10);
            PlayAudioClip("ZAudBow");
            doingIntro = _bc.enabled = true;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 12);
            _anim.enabled = false;
            yield return new WaitForSeconds(0.4f);
            _anim.enabled = true;
            yield return new WaitWhile(() => _anim.IsPlaying());
            _anim.Play("ZWalkRight");
            _rb.velocity = new Vector2(WalkSpeed, 0f);
            yield return new WaitWhile(() => transform.GetPositionX() < RightX - 15f);
            _rb.velocity = Vector2.zero;
            doingIntro = false;
            _anim.Play("ZIdle");
            StartCoroutine(Attacks());
            Log("Done Intro");
        }

        private IEnumerator MusicControl()
        {
            if (OWArenaFinder.IsInOverWorld)
            {
                OWBossManager.PlayMusic(null);
                OWBossManager.PlayMusic(FiveKnights.Clips["ZP1Intro"]);
                yield return new WaitForSecondsRealtime(7.04f);
                OWBossManager.PlayMusic(null);
                OWBossManager.PlayMusic(FiveKnights.Clips["ZP1Loop"]);
            }
            else
            {
                GGBossManager.Instance.PlayMusic(null);
                GGBossManager.Instance.PlayMusic(FiveKnights.Clips["ZP1Intro"]);
                yield return new WaitForSecondsRealtime(7.04f);
                GGBossManager.Instance.PlayMusic(null);
                GGBossManager.Instance.PlayMusic(FiveKnights.Clips["ZP1Loop"]);
            }
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
            int counterCount = 0;
            Dictionary<Func<IEnumerator>, int> rep = new Dictionary<Func<IEnumerator>, int>
            {
                [Dash] = 0,
                [Attack1Base] = 0,
                [AerialAttack] = 0,
                [Attack1Complete] = 0,
                [ZemerSlam] = 0
            };
            if (_countering) yield return (Countered());
            
            while (true)
            {
                Log("[Waiting to start calculation]");
                float xDisp = (transform.position.x < RightX - 22f) ? 8f : -8f;
                yield return Walk(xDisp);
                Log("[Setting Attacks]");
                Vector2 posZem = transform.position;
                Vector2 posH = _target.transform.position;

                //If the player is close
                if (posH.y > GroundY + 3f && (posH.x <= LeftX + 1f || posH.x >= RightX - 1))
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
                    else if (r < 3)
                    {
                        counterCount = 0;
                        yield return Dodge();
                        var a = _rand.Next(0, 3);
                        Log($"Calc rand att {a}");
                        yield return a > 0 ? Dash() : FancyAttack();
                    }
                    else
                    {
                        counterCount = 0;
                    }
                }

                yield return null;
                
                List<Func<IEnumerator>> attLst = new List<Func<IEnumerator>>
                {
                    Dash, Attack1Base, Attack1Base, AerialAttack, ZemerSlam
                };
                Func<IEnumerator> currAtt = attLst[_rand.Next(0, attLst.Count)];
                while (rep[currAtt] > 2)
                {
                    attLst.Remove(currAtt);
                    rep[currAtt] = 0;
                    currAtt = attLst[_rand.Next(0, attLst.Count)];
                }

                Log("Doing " + currAtt.Method.Name);
                rep[currAtt]++;
                yield return currAtt();
                Log("Done " + currAtt.Method.Name);

                yield return null;
                
                if (currAtt == Attack1Base)
                {
                    Func<IEnumerator>[] lst2 = { FancyAttack, Attack1Complete, null };
                    if (_hm.hp < DoSpinSlashPhase) lst2 = new Func<IEnumerator>[] { FancyAttack, FancyAttack, Attack1Complete };
                    if (FastApproximately(transform.position.x, _target.transform.position.x, 7f))
                    {
                        lst2 = (_hm.hp < DoSpinSlashPhase) ? new Func<IEnumerator>[] {Attack1Complete} : new Func<IEnumerator>[] {Attack1Complete, null};
                    }

                    currAtt = lst2[_rand.Next(0, lst2.Length)];
                    if (currAtt != null)
                    { 
                        Log("Doing " + currAtt.Method.Name);
                        yield return currAtt();
                        Log("Done " + currAtt.Method.Name);
                    }

                    if (currAtt == FancyAttack)
                    {
                        int rand = _rand.Next(0, 3);
                        if (rand == 0)
                        {
                            Log("Doing Special Fancy Attack");
                            yield return Dodge();
                            yield return new WaitForSeconds(TwoFancyDelay);
                            yield return FancyAttack();
                            Log("Done Special Fancy Attack");
                        }
                        else if (rand == 1)
                        {
                            yield return ZemerSlam();
                        }
                        else
                        {
                            yield return Dash();
                        }
                    }
                }
                
                yield return null;
                
                Log("[Done Doing Attacks]");

                _anim.Play("ZIdle");
                Log("[Restarting Calculations]");
                yield return new WaitForEndOfFrame();
            }
        }

        private IEnumerator AerialAttack()
        {
            IEnumerator Attack()
            {
                if (!IsFacingPlayer())
                {
                    yield return Turn();
                }

                float xVel = FaceHero() * -1f;
                transform.Find("BladeAerialShadow").gameObject.SetActive(false);
                _anim.Play("ZAerial2");
                PlayAudioClip("ZAudAtt" + _rand.Next(1,7));
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
                _anim.enabled = false;
                yield return new WaitForSeconds(AerialDelay);
                _anim.enabled = true;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 8);
                _rb.velocity = new Vector2(xVel * 38f, 15f);
                _rb.gravityScale = 1.3f;
                _rb.isKinematic = false;
                yield return new WaitForSeconds(0.1f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 10);
                PlayAudioClip("AudBigSlash2",0.85f,1.15f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 13);
                PlayAudioClip("AudBigSlash2",0.85f,1.15f);
                yield return new WaitWhile(() => transform.position.y > GroundY);
                _rb.velocity = Vector2.zero;
                _rb.gravityScale = 0f;
                _rb.isKinematic = true;
                transform.position = new Vector2(transform.position.x, GroundY);
                yield return new WaitWhile(() => _anim.IsPlaying());
            }

            yield return Attack();
        }

        private IEnumerator Walk(float displacement)
        {
            IEnumerator Walk()
            {
                float xPos = transform.position.x;
                float hPosX = _target.transform.position.x;
                int signX = (int) Mathf.Sign(displacement);
                int diffSig = (int) Mathf.Sign(hPosX - xPos);
                string animName = (diffSig != signX) ? "ZWalkRight" : "ZWalkLeft";
                Vector3 pScale = gameObject.transform.localScale;
                float xScl = Mathf.Abs(pScale.x) * signX;
                xScl = (animName == "ZWalkRight") ? xScl : -xScl;
                gameObject.transform.localScale = new Vector3(xScl, pScale.y, 1f);
                _anim.speed = 1.38f;
                _anim.Play(animName);
                yield return null;
                _rb.velocity = new Vector2(signX * WalkSpeed, 0f);
                yield return new WaitWhile
                (
                    () => 
                        !_rb.velocity.x.Within(0f,0.05f) && 
                        !(xPos+displacement).Within(transform.GetPositionX(),0.15f)
                );
                _anim.speed = 1f;
                _rb.velocity = Vector2.zero;
                _anim.Play("ZIdle");
            }

            yield return Walk();
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
            
            yield return (Slam());
        }

        private void SpawnShockwaves(float vertScale, float speed, int damage, Vector2 pos)
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

        private IEnumerator Turn()
        {
            _anim.Play("ZTurn");
            yield return new WaitForSeconds(TurnDelay);
        }

        // Stolen from Jngo
        private void ZemerCounter()
        {
            float dir = 0f;

            IEnumerator CounterAntic()
            {
                if (!IsFacingPlayer())
                {
                    yield return Turn();
                }

                dir = FaceHero() * -1f;
                _anim.Play("ZCInit");
                PlayAudioClip("ZAudCounter");
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
                _countering = false;
            }

            _counterRoutine = StartCoroutine(CounterAntic());
        }

        private IEnumerator FancyAttack()
        {
            IEnumerator Attack()
            {
                if (!IsFacingPlayer())
                {
                    yield return Turn();
                }

                float dir = FaceHero();

                _anim.Play("ZAtt2");
                PlayAudioClip("ZAudAtt" + _rand.Next(2,5));
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

            yield return Attack();

        }

        // Put these IEnumerators outside so that they can be started in OnBlockedHit
        private IEnumerator Countered()
        {
            _hm.IsInvincible = false;
            On.HealthManager.Hit -= OnBlockedHit;
            yield return _anim.PlayToEndWithActions("ZCAtt",
                (3, () => PlayAudioClip("Slash", 0.85f, 1.15f))
            );
            _anim.Play("ZIdle");
            yield return new WaitForSeconds(0.25f);
            _countering = false;
        }

        private IEnumerator Attack1Base()
        {
            IEnumerator Attack1Reg()
            {
                if (!IsFacingPlayer())
                {
                    yield return Turn();
                }

                float xVel = FaceHero() * -1f;
                _anim.Play("ZAtt1Base");
                PlayAudioClip("ZAudAtt" + _rand.Next(1,7));
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                _anim.enabled = false;
                yield return new WaitForSeconds(Att1BaseDelay);
                _anim.enabled = true;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                PlayAudioClip("Slash", 0.85f, 1.15f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 7);
                PlayAudioClip("Slash", 0.85f, 1.15f);
                _rb.velocity = new Vector2(23f * xVel, 0f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 11);
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(() => _anim.IsPlaying());
            }
            
            IEnumerator Attack1Full()
            {
                if (!IsFacingPlayer())
                {
                    yield return Turn();
                }

                float xVel = FaceHero() * -1f;
                PlayAudioClip("ZAudAtt" + _rand.Next(1,7));
                yield return _anim.PlayToFrame("ZAtt1Intro", 1);
                
                _anim.enabled = false;

                yield return new WaitForSeconds(Att1BaseDelay);

                _anim.enabled = true;

                yield return _anim.WaitToFrame(2);

                PlayAudioClip("Slash", 0.85f, 1.15f);

                yield return _anim.WaitToFrame(6);

                yield return _anim.PlayToEnd();
                
                _rb.velocity = new Vector2(23f * xVel, 0f);

                _anim.speed = 1.5f;
                
                while ((xVel > 0 && transform.position.x < RightX - 10f) ||
                       (xVel < 0 && transform.position.x > LeftX + 10f))
                {
                    yield return _anim.PlayToEndWithActions("ZAtt1Loop",
                        (0, ()=> PlayAudioClip("Slash", 0.85f, 1.15f))
                    );
                }

                _anim.speed = 1f;

                _anim.Play("ZAtt1End");
                _rb.velocity = Vector2.zero;
                
                yield return _anim.PlayToEnd();
            }

            yield return _hm.hp < DoSpinSlashPhase ? Attack1Full() : Attack1Reg();
        }

        private IEnumerator Dash()
        {
            IEnumerator Dash()
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
                if (FastApproximately(_target.transform.position.x, transform.position.x, 5f))
                {
                    yield return StrikeAlternate();
                    transform.position = new Vector3(transform.position.x, GroundY);
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
            }

            IEnumerator StrikeAlternate()
            {
                if (!IsFacingPlayer())
                {
                    yield return Turn();
                }

                FaceHero();
                _anim.Play("DashCounter");
                PlayAudioClip("Slash", 0.85f, 1.15f);
                yield return null;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("ZIdle");
            }

            yield return Dash();
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
                //PlayAudioClip("ZAudAtt" + _rand.Next(1,7));
                _rb.velocity = new Vector2(-xVel * 40f, 0f);
                yield return null;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _rb.velocity = new Vector2(0f, 0f);
                _anim.Play("ZIdle");
            }

            yield return Dodge();
        }

        private IEnumerator Attack1Complete()
        {
            IEnumerator Attack1Complete()
            {
                _anim.Play("ZAtt1");
                float xVel = FaceHero() * -1;

                yield return null;
                
                _anim.enabled = false;
                
                yield return new WaitForSeconds(SheoAttDelay);

                _anim.enabled = true;

                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);

                PlayAudioClip("AudBigSlash",0.85f, 1.15f);

                _rb.velocity = new Vector2(40f * xVel, 0f);

                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);

                _rb.velocity = Vector2.zero;

                yield return new WaitWhile(() => _anim.IsPlaying());

                _anim.Play("ZIdle");
            }
            
            yield return (Attack1Complete());
        }

        // This isn't actually a spin attack, it's the anti-cheese attack
        private IEnumerator SpinAttack()
        {
            IEnumerator SpinAttack()
            {
                if (!IsFacingPlayer())
                {
                    yield return Turn();
                }

                float xVel = FaceHero() * -1f;
                float diffX = Mathf.Abs(_target.transform.GetPositionX() - transform.GetPositionX());
                float diffY = Mathf.Abs(_target.transform.GetPositionY() - transform.GetPositionY());
                float rot = Mathf.Atan(diffY / diffX);
                rot = (xVel < 0) ? Mathf.PI - rot : rot;
                _anim.Play("ZSpin");
                PlayAudioClip("ZAudAtt" + _rand.Next(1,7));
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

            yield return SpinAttack();
        }

        private void OnBlockedHit(On.HealthManager.orig_Hit orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.name.Contains("Zemer"))
            {
                // Prevent code block from running every frame
                if (!_blockedHit)
                {
                    GameManager.instance.StartCoroutine(GameManager.instance.FreezeMoment(0.01f, 0.35f, 0.1f, 0.0f));
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
                _dnailReac.SetConvoTitle("ZEM_DREAM");
            }

            orig(self);
        }

        private void HealthManager_TakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            DoTakeDamage(self.gameObject, hitInstance.Direction);
            orig(self, hitInstance);
        }
        
        private void SpellFlukeOnDoDamage(On.SpellFluke.orig_DoDamage orig, SpellFluke self, GameObject tar, 
            int upwardrecursionamount, bool burst)
        {
            DoTakeDamage(tar, 0);
            orig(self, tar, upwardrecursionamount, burst);
        }

        private bool _hasDied;
        
        private void DoTakeDamage(GameObject tar, float dir)
        {
            if (tar.name.Contains("Zemer"))
            {
                _hitEffects.RecieveHitEffect(dir);

                if (doingIntro)
                {
                    StopCoroutine(nameof(Start));
                    _rb.velocity = new Vector2(0f, 0f);
                    doingIntro = false;
                    _bc.enabled = true;
                    _anim.enabled = true;
                    _anim.speed = 1f;
                    _countering = true;
                    StartCoroutine(Attacks());
                }

                if (_hm.hp <= Phase2HP && !_hasDied)
                {
                    Log("Going to phase 2");
                    _hasDied = true;
                    _bc.enabled = false;
                    
                    if (OWArenaFinder.IsInOverWorld ) OWBossManager.PlayMusic(null);
                    else GGBossManager.Instance.PlayMusic(null, 1f);
                    
                    GameObject extraNail = GameObject.Find("ZNailB");
                    if (extraNail != null && extraNail.transform.parent == null)
                    {
                        Destroy(extraNail);
                    }
                    StopCoroutine(nameof(Attacks));
                    OnDestroy();
                    gameObject.AddComponent<ZemerControllerP2>().DoPhase =
                        CustomWP.boss == CustomWP.Boss.Ze;
                    Destroy(this);
                }
            }
        }

        private IEnumerator SilLeave()
        {
            SpriteRenderer sil = GameObject.Find("Silhouette Zemer").GetComponent<SpriteRenderer>();
            sil.transform.localScale *= 1.2f;
            sil.sprite = ArenaFinder.Sprites["Zem_Sil_1"];
            yield return new WaitForSeconds(0.05f);
            sil.sprite = ArenaFinder.Sprites["Zem_Sil_2"];
            yield return new WaitForSeconds(0.05f);
            sil.sprite = ArenaFinder.Sprites["Zem_Sil_3"];
            yield return new WaitForSeconds(0.05f);
            Destroy(sil.gameObject);
        }

        private float FaceHero(bool shouldRev = false)
        {
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
                Spawn = gameObject
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
                        return FiveKnights.Clips["TraitorSlam"];
                    default:
                        return FiveKnights.Clips[clipName];
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