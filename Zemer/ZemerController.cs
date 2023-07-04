using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FiveKnights.BossManagement;
using FiveKnights.Misc;
using HutongGames.PlayMaker.Actions;
using SFCore.Utils;
using UnityEngine;
using Vasi;
using Random = UnityEngine.Random;

namespace FiveKnights.Zemer
{
    public class ZemerController : MonoBehaviour
    {
        private HealthManager _hm;
        private BoxCollider2D _bc;
        private SpriteRenderer _sr;
        private EnemyDreamnailReaction _dreamNailReaction;
        private GameObject _dd;
        private GameObject _dnailEff;
        private Animator _anim;
        private Rigidbody2D _rb;
        private System.Random _rand;
        private ExtraDamageable _extraDamageable;
        private EnemyHitEffectsUninfected _hitEffects;
        private GameObject _target;
        private readonly float GroundY = (OWArenaFinder.IsInOverWorld) ? 108.3f : 
            (CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim) ? 9f : 28.8f;
        private readonly float LeftX = (OWArenaFinder.IsInOverWorld) ? 240.1f : 
            (CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim) ? 61.0f : 11.2f;
        private readonly float RightX = (OWArenaFinder.IsInOverWorld) ? 273.9f : 
            (CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim) ? 91.0f : 45.7f;
        private readonly float SlamY = (OWArenaFinder.IsInOverWorld) ? 105f : 
            (CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim) ? 6f : 25.9f;
        // HP if the fight is mystic zemer version
        private readonly int MaxHPV2 = 400;
        private readonly int MaxHPV1 = CustomWP.lev > 0 ? 1400 : 1200;
        private readonly int DoSpinSlashPhase = CustomWP.lev > 0 ? 800 : 700;
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
        public static bool WaitForTChild = false;
        private const float ThrowDelay = 0.2f;
        private const float NailSize = 1.15f;
        private const float NailSpeed = 80f;
        private const float DashSpeed = 60f;
        private readonly Vector3 LeaveOffset = new Vector3(1.5f, 1.5f);
        private readonly int DreamConvoAmount = 3;
        private readonly string DreamConvoKey = OWArenaFinder.IsInOverWorld ? "ZEM_DREAM" :
            ((CustomWP.boss is CustomWP.Boss.Ze or CustomWP.Boss.Mystic) ? "ZEM_GG_DREAM" : "ZEM_CC_DREAM");

        private void Awake()
        {
            OnDestroy();
            On.HealthManager.TakeDamage += HealthManager_TakeDamage;
            On.HealthManager.Die += HealthManagerOnDie;
            On.EnemyDreamnailReaction.RecieveDreamImpact += OnReceiveDreamImpact;
            On.SpellFluke.DoDamage += SpellFlukeOnDoDamage;
            _hm = gameObject.AddComponent<HealthManager>();
            _anim = gameObject.GetComponent<Animator>();
            _bc = gameObject.GetComponent<BoxCollider2D>();
            _rb = gameObject.GetComponent<Rigidbody2D>();
            _rb.isKinematic = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _sr = GetComponent<SpriteRenderer>();
            _dreamNailReaction = gameObject.AddComponent<EnemyDreamnailReaction>();
            gameObject.AddComponent<AudioSource>();
            gameObject.AddComponent<DamageHero>().damageDealt = 1;
            _dd = FiveKnights.preloadedGO["WD"];
            _dnailEff = _dd.GetComponent<EnemyDreamnailReaction>().GetAttr<EnemyDreamnailReaction, GameObject>("dreamImpactPrefab");
            
            // So she gets hit by dcrest I think
            _extraDamageable = gameObject.AddComponent<ExtraDamageable>();

            _dreamNailReaction.enabled = true;
            Mirror.SetField(_dreamNailReaction, "convoAmount", DreamConvoAmount);
            _dreamNailReaction.SetConvoTitle(DreamConvoKey);

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
            AssignFields();

            _hm.hp = CustomWP.boss == CustomWP.Boss.Ze ? MaxHPV1 : MaxHPV2;
			EnemyHPBarImport.RefreshHPBar(gameObject);
             doingIntro = false;
             // For some reason setting the _bc to false here in the OW arena results in Zemer's hitbox never activating
             // after so I've had to do this ugly thing
             if (!OWArenaFinder.IsInOverWorld)
             {
                 _stopForcingHB = false;
                 _bc.enabled = false;
                 StartCoroutine(ForceDisableHB());
             }
            gameObject.transform.localScale *= 0.8f;
            gameObject.layer = 11;
        }

        private void HealthManagerOnDie(On.HealthManager.orig_Die orig, HealthManager self, float? attackDirection, AttackTypes attackType, bool ignoreEvasion)
        {
            if(self.gameObject.name.Contains("Zemer")) return;
            orig(self, attackDirection, attackType, ignoreEvasion);
        }

        // Sorry for this but Unity was being annoying :/
        IEnumerator ForceDisableHB()
        {
            while (!_stopForcingHB)
            {
                _bc.enabled = false;
                yield return new WaitForEndOfFrame();
                _bc.enabled = false;
            }
        }

        private bool _stopForcingHB;
        
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
            yield return new WaitWhile(() => !(_target = HeroController.instance.gameObject));
            Destroy(GameObject.Find("Bounds Cage"));
            Destroy(GameObject.Find("World Edge v2"));
			if(!GGBossManager.alone && !OWArenaFinder.IsInOverWorld) StartCoroutine(SilLeave());
			else yield return new WaitForSeconds(1.7f);

			gameObject.SetActive(true); 

            gameObject.transform.position = gameObject.transform.position = new Vector2(RightX - 10f, GroundY + 0.5f);
            
            FaceHero();
            _bc.enabled = _sr.enabled = false;

            yield return new WaitForSeconds(0.2f);

            _anim.Play("ZIntro");
            _sr.enabled = true;
            yield return null;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
            gameObject.transform.position = new Vector2(RightX - 10f, GroundY);
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
            _anim.enabled = false;
            yield return new WaitForSeconds(0.3f);
            
            yield return new WaitWhile(() => WaitForTChild);
            StartCoroutine(MusicControl());
            DoTitle();
            doingIntro = _stopForcingHB = true;
            _bc.enabled = true;
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
            _bc.enabled = true;
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
                [FancyAttack] = 0,
                [Dash] = 0,
                [Attack1Base] = 0,
                [AerialAttack] = 0,
                [Attack1Complete] = 0,
                [ZemerSlam] = 0,
                [NailLaunch] = 0,
            };
            
            Dictionary<Func<IEnumerator>, int> max = new Dictionary<Func<IEnumerator>, int>
            {
                [FancyAttack] = 2,
                [Dash] = 2,
                [Attack1Base] = 2,
                [AerialAttack] = 2,
                [Attack1Complete] = 2,
                [NailLaunch] = 2,
                [ZemerSlam] = 1,
            };

            if (_countering) yield return (Countered());
            
            while (true)
            {
                Log("[Waiting to start calculation]");
                float xDisp = (transform.position.x < RightX - 22f) ? 8f : -8f;
                yield return Walk(xDisp + Random.Range(-2,3));
                Log("[Setting Attacks]");
                Vector2 posZem = transform.position;
                Vector2 posH = _target.transform.position;
                bool isPhase2 = _hm.hp < DoSpinSlashPhase;

                //If the player is close
                if (posH.y > GroundY + 3f && (posH.x <= LeftX + 1f || posH.x >= RightX - 1))
                {
                    Log("Doing Spin Cheese Counter");
                    yield return SpinAttack();
                    Log("Done Spin Cheese Counter");
                }
                else if (FastApproximately(posZem.x, posH.x, 5f))
                {
                    int r = _rand.Next(0, 5); //0 1 2 3 4
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
                        counterCount = 0;
                        Log("Doing Special Dodge");
                        yield return Dodge();
                        Log("Done Special Dodge's Dodge");
                        var lst = new List<Func<IEnumerator>> { FancyAttack, null, null};
                        if (isPhase2) lst = new List<Func<IEnumerator>> { FancyAttack, NailLaunch };
                        Log("Choosing Attack");
                        var att = MiscMethods.ChooseAttack(lst, rep, max);
                        Log("Done Choosing Attack");
                        if (att != null)
                        {
                            Log("Doing " + att.Method.Name);
                            yield return att();
                            Log("Done " + att.Method.Name);
                        }
                        Log("Done Special Dodge");
                    }
                }
                
                List<Func<IEnumerator>> attLst = new List<Func<IEnumerator>>
                {
                    Dash, Attack1Base, Attack1Base, AerialAttack, ZemerSlam
                };

                if (isPhase2)
                {
                    attLst.Add(NailLaunch);
                }
                
                Log("Choosing Attack");
                Func<IEnumerator> currAtt = MiscMethods.ChooseAttack(attLst, rep, max);
                Log("Done Choosing Attack");
                
                Log("Doing " + currAtt.Method.Name);
                yield return currAtt();
                Log("Done " + currAtt.Method.Name);

                if (isPhase2 && (currAtt == ZemerSlam || currAtt == Dash) && rep[NailLaunch] < max[NailLaunch] &&
                    Random.Range(0, 2) == 0)
                {
                    rep[NailLaunch]++;
                    Log("Doing NailLaunch");
                    yield return NailLaunch();
                    Log("Done NailLaunch");
                }
                else if (currAtt == Attack1Base)
                {
                    List<Func<IEnumerator>> lst2 = new List<Func<IEnumerator>>{ FancyAttack, Attack1Complete, null };
                    if (isPhase2) lst2 = new List<Func<IEnumerator>> { FancyAttack, FancyAttack, Attack1Complete };
                    if (FastApproximately(transform.position.x, _target.transform.position.x, 7f))
                    {
                        lst2 = (isPhase2) ? new List<Func<IEnumerator>> {Attack1Complete} : new List<Func<IEnumerator>> {Attack1Complete, null};
                    }
                    
                    Log("Choosing Attack");
                    currAtt = MiscMethods.ChooseAttack(lst2, rep, max);
                    Log("Done Choosing Attack");
                    
                    if (currAtt != null)
                    { 
                        Log("Doing " + currAtt.Method.Name);
                        yield return currAtt();
                        Log("Done " + currAtt.Method.Name);
                    }

                    if (currAtt == FancyAttack)
                    {
                        int rand = _rand.Next(0, isPhase2 ? 4 : 3);
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
                            Log("Doing Slam");
                            yield return ZemerSlam();
                            Log("Done Slam");
                        }
                        else if (isPhase2 && rand == 2)
                        {
                            Log("Doing NailLaunch");
                            yield return NailLaunch();
                            Log("Done NailLaunch");
                        }
                        else
                        {
                            Log("Doing Dash");
                            yield return Dash();
                            Log("Done Dash");
                        }
                    }
                }
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
                PlayAudioClip(ZemRandAudio.PickRandomZemAud(1, 8));
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
                PlayAudioClip("AudBigSlash2",0.15f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 13);
                PlayAudioClip("AudBigSlash2",0.15f);
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
            bool isEnd = false;
            
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
                _anim.Play(animName, -1, 0f);
                yield return null;
                _rb.velocity = new Vector2(signX * WalkSpeed, 0f);
                yield return new WaitWhile
                (
                    () =>
                    {
                        bool goingRight = signX > 0;
                        float newXPos = transform.GetPositionX();
                        bool hasNotReachedEnd = goingRight ? xPos + displacement > newXPos : xPos + displacement < newXPos;
                        bool isCloseToPlayer = _target.transform.position.x.Within(newXPos, 3f);
                        return !_rb.velocity.x.Within(0f, 0.05f) && hasNotReachedEnd && !isCloseToPlayer;
                    }
                );
                _anim.speed = 1f;
                _rb.velocity = Vector2.zero;
                isEnd = true;
                _anim.Play("ZIdle");
            }

            IEnumerator DodgeCheck(Coroutine walk)
            {
                int thresh = 25;
                int num = 0;
                while (!isEnd)
                {
                    var posZem = transform.position.x;
                    var posH = _target.transform.position;
                    if (posZem.Within(posH.x, 1f) && posH.y > GroundY + 3.5f)
                    {
                        if (num > thresh)
                        {
                            if (Random.Range(0, 3) == 0)
                            {
                                num = 0;
                                continue;
                            }
                            Log("Stopping walk to dodge.");
                            StopCoroutine(walk);
                            _anim.speed = 1f;
                            _rb.velocity = Vector2.zero;
                            yield return  (Random.Range(0,2) == 0 ? Dodge() : Upslash());
                            isEnd = true;
                            yield break;
                        }
                        num++;
                    }
                    yield return null;
                }
            }
            
            var routine = StartCoroutine(Walk());
            StartCoroutine(DodgeCheck(routine));
            yield return new WaitWhile(() => !isEnd);
        }

        private IEnumerator ZemerSlam()
        {
            IEnumerator Slam()
            {
                transform.position += new Vector3(0f, 1.32f);
                PlayAudioClip(ZemRandAudio.PickRandomZemAud(9, 13));
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
            _anim.Play("ZTurn", -1, 0f);
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
                Vector2 fxPos = transform.position + new Vector3(1.7f * dir, 0.8f, -0.1f);
                Quaternion fxRot = Quaternion.Euler(0, 0, dir * 80f);
                GameObject counterFx = Instantiate(FiveKnights.preloadedGO["CounterFX"], fxPos, fxRot);
                counterFx.SetActive(true);
                yield return new WaitForSeconds(0.42f);
                
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
                PlayAudioClip(ZemRandAudio.PickRandomZemAud(9, 13));
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
                (3, () => PlayAudioClip("Slash", 0.15f))
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
                PlayAudioClip(ZemRandAudio.PickRandomZemAud(2, 8));
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                _anim.enabled = false;
                yield return new WaitForSeconds(Att1BaseDelay);
                _anim.enabled = true;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                PlayAudioClip("Slash", 0.15f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 7);
                PlayAudioClip("Slash", 0.15f);
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
                PlayAudioClip(ZemRandAudio.PickRandomZemAud(2, 8));
                yield return _anim.PlayToFrame("ZAtt1Intro", 1);
                
                _anim.enabled = false;

                yield return new WaitForSeconds(Att1BaseDelay);

                _anim.enabled = true;

                yield return _anim.WaitToFrame(2);

                PlayAudioClip("Slash", 0.15f);

                yield return _anim.WaitToFrame(6);

                yield return _anim.PlayToEnd();
                
                _rb.velocity = new Vector2(23f * xVel, 0f);

                _anim.speed = 1.5f;
                
                while ((xVel > 0 && transform.position.x < RightX - 10f) ||
                       (xVel < 0 && transform.position.x > LeftX + 10f))
                {
                    yield return _anim.PlayToEndWithActions("ZAtt1Loop",
                        (0, ()=> PlayAudioClip("Slash", 0.15f))
                    );
                }

                _anim.speed = 1f;

                _anim.Play("ZAtt1End");
                _rb.velocity = Vector2.zero;
                
                yield return _anim.PlayToEnd();
            }

            yield return _hm.hp < DoSpinSlashPhase ? Attack1Full() : Attack1Reg();
        }

        private IEnumerator Upslash()
        {
            if (!IsFacingPlayer())
            {
                yield return Turn();
            }

            float dir = FaceHero();
            transform.Find("HyperCut").gameObject.SetActive(false);
            PlayAudioClip(ZemRandAudio.PickRandomZemAud(9, 13));
            _anim.Play("ZDash");
            transform.position = new Vector3(transform.position.x, GroundY - 0.3f, transform.position.z);

            yield return _anim.WaitToFrame(4);
            
            if (!IsFacingPlayer())
            {
                yield return Turn();
            }

            FaceHero();
            _anim.Play("DashCounter");
            PlayAudioClip("Slash", 0.15f);
            yield return null;
            yield return new WaitWhile(() => _anim.IsPlaying());
            _anim.Play("ZIdle");
                
            transform.position = new Vector3(transform.position.x, GroundY);
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
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 6);
                PlayAudioClip("AudDash");
                _anim.speed = 2f;
                _rb.velocity = new Vector2(-dir * DashSpeed, 0f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 7);
                _anim.enabled = false;
                _anim.speed = 1f;
                yield return new WaitForSeconds(0.1f);
                _anim.enabled = true;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 9);
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("ZIdle");
                transform.position = new Vector3(transform.position.x, GroundY, transform.position.z);
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

                _anim.Play("ZDodge", -1, 0f);
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

                PlayAudioClip("AudBigSlash",0.15f);

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
                PlayAudioClip(ZemRandAudio.PickRandomZemAud(2, 8));
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
        
        private IEnumerator NailLaunch()
        {
            float dir = 0f;
            float middle = (RightX + LeftX) / 2f;

            IEnumerator Throw()
            {
                Vector2 hero = _target.transform.position;
                
                // If player is too close dodge back or if too close to wall as well dash forward
                if (hero.x.Within(transform.position.x, 12f))
                {
                    // Too close to wall
                    if (transform.position.x < LeftX + 6f || transform.position.x > RightX - 6f)
                    {
                        yield return Dash();
                        if (_target.transform.position.x.Within(transform.position.x, 12f))
                        {
                            yield return Dodge();
                        }
                    }
                    else
                    {
                        yield return Dodge();
                    }
                }
                
                dir = FaceHero();
                float rot;
                _anim.Play("ZThrow1");
                PlayAudioClip(ZemRandAudio.PickRandomZemAud(2, 8));
                yield return _anim.WaitToFrame(2);
                _anim.enabled = false;
                yield return new WaitForSeconds(ThrowDelay / 2f);
                hero = _target.transform.position;
                dir = FaceHero();
                yield return FlashRepeat(hero, ThrowDelay / 2f);
                _anim.enabled = true;
                yield return _anim.WaitToFrame(3);

                Transform center = transform.Find("ZNailB").Find("Center");
                rot = GetAngleTo(center.position,  hero) * Mathf.Deg2Rad;
                // Predict where it will hit, if it is too high, lower the y until it's not
                var maskLayer = LayerMask.LayerToName(8);
                var rc = Physics2D.BoxCast(center.position, new Vector2(1f, 1f), 0f,
                    new Vector2(Mathf.Cos(rot), Mathf.Sin(rot)), Mathf.Infinity, LayerMask.GetMask(maskLayer));
                while (rc.point.y > GroundY + 3.2f)
                {
                    hero -= new Vector2(0f, 1f);
                    rot = GetAngleTo(center.position,  hero) * Mathf.Deg2Rad;
                    rc = Physics2D.BoxCast(center.position, new Vector2(1f, 1f), 0f,
                        new Vector2(Mathf.Cos(rot), Mathf.Sin(rot)), Mathf.Infinity, LayerMask.GetMask(maskLayer));
                }

                float nailRealPosOffset = -Mathf.Sign(Mathf.Cos(rot)) * 6;
                
                // Check if nail will land too close to Ze'mer
                // Note mystic is more complicated because she can throw in the air
                
                while (center.position.x.Within(rc.point.x + nailRealPosOffset, 10f))
                {
                    Log("Too close to zem");
                    hero += new Vector2(-dir, 0f);
                    Log(hero);
                    rot = GetAngleTo(center.position,  hero) * Mathf.Deg2Rad;
                    /*rc = Physics2D.Raycast(center.position,
                        new Vector2(Mathf.Cos(rot), Mathf.Sin(rot)), Mathf.Infinity,
                        LayerMask.GetMask(maskLayer));*/
                    rc = Physics2D.BoxCast(center.position, new Vector2(1f, 1f), 0f,
                        new Vector2(Mathf.Cos(rot), Mathf.Sin(rot)), Mathf.Infinity, LayerMask.GetMask(maskLayer));
                }
                Log($"Putting nail to {rc.point.x  + nailRealPosOffset}");

                float rotArm = rot + (dir > 0 ? Mathf.PI : 0f);
                
                GameObject arm = transform.Find("NailHand").gameObject;
                GameObject nailPar = Instantiate(transform.Find("ZNailB").gameObject);
                Rigidbody2D parRB = nailPar.GetComponent<Rigidbody2D>();

                if (CustomWP.boss == CustomWP.Boss.Mystic || CustomWP.boss == CustomWP.Boss.Ze)
                {
                    Log("double size for reg");
                    var a = nailPar.transform.Find("ZNailC").GetComponent<BoxCollider2D>();
                    a.size *= 2.5f;
                }
                else if (CustomWP.boss == CustomWP.Boss.All)
                {
                    Log("incr size for pantheon");
                    var a = nailPar.transform.Find("ZNailC").GetComponent<BoxCollider2D>();
                    a.size *= 1.5f;
                }

                nailPar.transform.position = transform.Find("ZNailB").position;
                nailPar.transform.localScale = new Vector3(Mathf.Sign(transform.localScale.x) * NailSize, NailSize, NailSize);
                arm.transform.SetRotation2D(rotArm * Mathf.Rad2Deg);
                nailPar.transform.SetRotation2D(rotArm * Mathf.Rad2Deg);

                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
                yield return new WaitForSeconds(0.07f);
                nailPar.SetActive(true);
                // TODO might want to readjust speed
                parRB.velocity = new Vector2(Mathf.Cos(rot) * NailSpeed, Mathf.Sin(rot) * NailSpeed);
                nailPar.AddComponent<ExtraNailBndCheck>();
                //yield return new WaitForSeconds(0.01f);

                var cc = nailPar.transform.Find("ZNailC").gameObject.AddComponent<CollisionCheck>();
                cc.Freeze = true;
                cc.OnCollide += () =>
                {
                    PlayAudioClip("AudLand");
                    GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
                    nailPar.GetComponent<SpriteRenderer>().enabled = false;
                    nailPar.transform.Find("ZNailN").GetComponent<SpriteRenderer>().enabled = true;
                    nailPar.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
                    Destroy(cc);
                };
                yield return new WaitWhile(() => _anim.IsPlaying());
                yield return LaunchSide(nailPar);
            }

            IEnumerator GndLeave(float dir)
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

            IEnumerator LaunchSide(GameObject nail)
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
                
                cc.Hit = rbNail.velocity == Vector2.zero;
                yield return GndLeave(dir);
                yield return new WaitWhile(() => !cc.Hit && rbNail.velocity != Vector2.zero);
                yield return new WaitForSeconds(0.75f);

                Vector2 zem = transform.position;
                Vector2 nl = nail.transform.Find("Point").position;
                Vector3 zemSc = transform.localScale;
                
                if (nl.x < middle)
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
                    Log($"Nail ended at {nail.transform.Find("Center").position}");
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
                    Log($"Nail ended at {nail.transform.Find("Center").position}");
                    Destroy(nail);
                }

                yield return _anim.PlayToEnd();
            }

            yield return Throw();
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
        
        private float GetAngleTo(Vector2 from, Vector2 to)
        {
            float num = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;
            while ((double) num < 0.0) num += 360f;
            return num;
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
        
        IEnumerator FlashRepeat(Vector3 targ, float timer = 0.2f)
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
            orig(self);
            if(self.name.Contains("Zemer"))
            {
                StartCoroutine(FlashWhite());
                Instantiate(_dnailEff, transform.position, Quaternion.identity);
            }
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
                    _stopForcingHB = true;
                    _bc.enabled = true;
                    _anim.enabled = true;
                    _anim.speed = 1f;
                    _countering = true;
                    FaceHero();
                    StartCoroutine(Attacks());
                }

                if (_hm.hp <= 0 && !_hasDied)
                {
                    Log("Going to phase 2");
                    _hasDied = true;
                    _bc.enabled = false;

                    if(OWArenaFinder.IsInOverWorld) GameManager.instance.AwardAchievement("PALE_COURT_ZEM_ACH");

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
            
            Mirror.SetField(_extraDamageable, "impactClipTable",
                Mirror.GetField<ExtraDamageable, RandomAudioClipTable>(_dd.GetComponent<ExtraDamageable>(), "impactClipTable"));
            Mirror.SetField(_extraDamageable, "audioPlayerPrefab",
                Mirror.GetField<ExtraDamageable, AudioSource>(_dd.GetComponent<ExtraDamageable>(), "audioPlayerPrefab"));
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
            return ((a - b) < 0 ? ((a - b) * -1) : (a - b)) <= threshold;
        }

        private void Log(object o)
        {
            Modding.Logger.Log("[Zemer] " + o);
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