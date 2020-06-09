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
using Object = UnityEngine.Object;

namespace FiveKnights
{
    public class ZemerController : MonoBehaviour
    {
        private HealthManager _hm;
        private BoxCollider2D _bc;
        private SpriteRenderer _sr;
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
        private const float LeftX = 62.5f;
        private const float RightX = 90.6f;
        private const int MaxHP = 1000;
        private const int Phase2HP = 100;
        private PlayMakerFSM _pvFsm;
        private Coroutine _counterRoutine;
        private bool _blockedHit;
        private bool atPhase2;
        private bool _attacking;
        private List<Action> _moves;
        private Dictionary<Action, int> _maxRepeats;
        private Dictionary<Action, int> _repeats;

        private readonly string[] _dnailDial =
        {
            "ISMA_DREAM_1",
            "ISMA_DREAM_2",
            "ISMA_DREAM_3"
        };
        
        private void EndPhase1()
        {
            float dir;

            IEnumerator Wait()
            {
                yield return new WaitWhile(() => _attacking);
                _attacking = true;
                StartCoroutine(KnockedOut());
            }

            IEnumerator KnockedOut()
            {
                dir = -FaceHero();
                PlayDeathFor(gameObject);
                _bc.enabled = false;
                _anim.Play("ZKnocked");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);
                _anim.enabled = false;
                _rb.velocity = new Vector2(dir * -15f, 30f);
                _rb.gravityScale = 1.5f;
                yield return new WaitForSeconds(0.5f);
                yield return new WaitWhile(() => transform.GetPositionY() > GroundY-1.8f);
                _rb.velocity = Vector2.zero;
                _rb.gravityScale = 0;
                _anim.enabled = true;
                yield return new WaitWhile(() => _anim.IsPlaying());
                yield return new WaitForSeconds(1.75f);

                //TEMP
                Destroy(this);


                StartCoroutine(Recover());
            }

            IEnumerator Recover()
            {
                _anim.Play("ZRecover");
                yield return null;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("ZZip");
                yield return null;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _rb.velocity = new Vector2(dir * 15f, 30f);
                yield return new WaitForSeconds(0.25f);
                _bc.enabled = true;
                _rb.velocity = Vector2.zero;
                _sr.enabled = false;
            }

            StartCoroutine(Wait());
        }

        private void Awake()
        {
            _hm = gameObject.AddComponent<HealthManager>();
            _anim = gameObject.GetComponent<Animator>();
            _bc = gameObject.GetComponent<BoxCollider2D>();
            _rb = gameObject.GetComponent<Rigidbody2D>();
            _rb.isKinematic = true;
            _sr = GetComponent<SpriteRenderer>();
            _dnailReac = gameObject.AddComponent<EnemyDreamnailReaction>();
            _aud = gameObject.AddComponent<AudioSource>();
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
        }

        private IEnumerator Start()
        {
            Log("Start");
            GameObject.Find("Burrow Effect").SetActive(false);
            GameCameras.instance.cameraShakeFSM.FsmVariables.FindFsmBool("RumblingMed").Value = false;
            _hm.hp = MaxHP;
            _bc.enabled = false;
            gameObject.transform.localScale *= 0.9f;
            gameObject.layer = 11;
            yield return new WaitWhile(()=> !(_target = HeroController.instance.gameObject));
            if (!WDController.alone) StartCoroutine(SilLeave());
            else yield return new WaitForSeconds(1.7f);
            gameObject.SetActive(true);
            float x = _target.transform.GetPositionX() + 5f;
            gameObject.transform.position = new Vector2(x,GroundY + 10f);
            FaceHero();
            AssignFields();
            On.HealthManager.TakeDamage += HealthManager_TakeDamage;
            On.EnemyDreamnailReaction.RecieveDreamImpact += OnReceiveDreamImpact;
            _anim.Play("ZIntro");
            _rb.velocity = new Vector2(0f,-40f);
            yield return null;
            yield return new WaitWhile(() => transform.GetPositionY() > GroundY && _anim.GetCurrentFrame() < 2);
            _anim.enabled = false;
            yield return new WaitWhile(()=> transform.GetPositionY() > GroundY);
            _rb.velocity = Vector2.zero;
            GameCameras.instance.cameraShakeFSM.SendEvent("SmallShake");
            transform.position = new Vector2(x, GroundY);
            yield return new WaitForSeconds(0.2f);
            GameObject area = null;
            foreach (GameObject i in FindObjectsOfType<GameObject>().Where(x => x.name.Contains("Area Title Holder")))
            {
                area = i.transform.Find("Area Title").gameObject;
            }
            StartCoroutine(ChangeIntroText(Instantiate(area), "Zemer", "", "Mysterious", false));
            yield return new WaitForSeconds(0.8f);
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
            _anim.Play("ZIdle");
            _bc.enabled = true;
            _moves = new List<Action>
            {
                ZemerCounter,
                MultiAttack,
                Dodge,
                SpinAttack,
                Turn
            };

            _repeats = new Dictionary<Action, int>
            {
                [ZemerCounter] = 0,
                [MultiAttack] = 0,
                [Dodge] = 0,
                [SpinAttack] = 0,
                [Turn] = 0,
            };

            _maxRepeats = new Dictionary<Action, int>
            {
                [ZemerCounter] = 1,
                [MultiAttack] = 2,
                [Dodge] = 1,
                [SpinAttack] = 2,
                [Turn] = 1
            };
            StartCoroutine(Attacks());
            Log("END");
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
        }

        private IEnumerator Attacks()
        {
            while (true)
            {
                yield return new WaitWhile(()=>_attacking);
                yield return new WaitForSeconds(0.1f);
                yield return new WaitWhile(() => _attacking);
                _attacking = true;

                Vector2 pos = transform.position;
                float dir = -Mathf.Sign(transform.localScale.x);
                float diff = Mathf.Sign(pos.x - _target.transform.GetPositionX());
                int ind = _rand.Next(0, _moves.Count);
                Action curr = _moves[ind];
                
                while (_repeats[curr] >= _maxRepeats[curr] || curr == Turn)
                {
                    ind = _rand.Next(0, _moves.Count);
                    curr = _moves[ind];
                }

                if (diff * dir > 0 && _repeats[Turn] < _maxRepeats[Turn])
                {
                    curr = Turn;
                }

                foreach (Action act in _moves)
                {
                    if (act == curr) _repeats[curr]++;
                    else _repeats[act] = 0;
                }
                curr.Invoke();
                yield return new WaitForEndOfFrame();
            }
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

        //Stolen from Jngo
        private void ZemerCounter()
        {
            float dir = FaceHero() * -1f;
            IEnumerator CounterAntic()
            {
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
                yield return new WaitWhile(()=>_anim.IsPlaying());
                yield return new WaitForSeconds(0.75f);
                _attacking = false;
            }

            _counterRoutine = StartCoroutine(CounterAntic());
        }
        
        // Put these IEnumerators outside so that they can be started in OnBlockedHit
        private IEnumerator Countered()
        {
            _anim.Play("ZCAtt");
            On.HealthManager.Hit -= OnBlockedHit;
            yield return new WaitWhile(() => _anim.GetCurrentFrame()<15);
            PlayAudioClip("Slash", 0.85f, 1.15f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            
            _anim.Play("ZIdle");
            yield return new WaitForSeconds(0.3f);
            _attacking = false;
        }
        
        private void MultiAttack()
        {
            IEnumerator MultiAttack()
            {
                float xVel = FaceHero() * -1f;
                yield return new WaitForSeconds(0.03f);
                _anim.Play("ZAtt1");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                PlayAudioClip("Slash", 0.85f, 1.15f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 7);
                PlayAudioClip("Slash", 0.85f, 1.15f);
                _rb.velocity = new Vector2(30f * xVel, 0f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 11);
                _rb.velocity = new Vector2(0f, 0f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 14);
                xVel = FaceHero() * -1;
                _anim.enabled = false;
                yield return new WaitForSeconds(0.1f);
                _anim.enabled = true;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 17);
                PlayAudioClip("Slash", 0.85f, 1.15f);
                _rb.velocity = new Vector2(40f * xVel, 0f);
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 18);
                _rb.velocity = Vector2.zero;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("ZIdle");
                yield return new WaitForSeconds(0.3f);
                _attacking = false;
            }

            StartCoroutine(MultiAttack());
        }

        private void Dodge()
        {
            IEnumerator Dodge()
            {
                float xVel = FaceHero() * -1f;

                _anim.Play("ZDodge");
                _rb.velocity = new Vector2(-xVel * 40f,0f);
                yield return null;
                yield return new WaitWhile(() => _anim.IsPlaying());
                _rb.velocity = new Vector2(0f, 0f);
                _attacking = false;
            }

            StartCoroutine(Dodge());
        }

        private void SpinAttack()
        {
            IEnumerator SpinAttack()
            {
                const float time = 0.385f;
                float xVel = FaceHero() * -1f;
                float diffX = _target.transform.GetPositionX() - transform.GetPositionX();
                if (diffX > 11f)
                {
                    _attacking = false;
                    yield break;
                }
                Vector2 vel = new Vector2(diffX / time, 31f);
                if (IsPlayAboveHead())
                {
                    vel = new Vector2(xVel * 15f, 35f);
                }
                yield return new WaitForSeconds(0.03f);
                _anim.Play("ZSpin");
                yield return null;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
                _rb.velocity = vel;
                _rb.gravityScale = 1.5f;
                _rb.isKinematic = false;
                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 12);
                yield return new WaitWhile(() => transform.position.y > GroundY);
                _rb.velocity = Vector2.zero;
                _rb.gravityScale = 0f;
                transform.position = new Vector3(transform.position.x, GroundY);
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("ZIdle");
                FaceHero();
                yield return new WaitForSeconds(0.3f);
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

        private IEnumerator SilLeave()
        {
            SpriteRenderer sil = GameObject.Find("Silhouette Zemer").GetComponent<SpriteRenderer>();
            sil.transform.localScale *= 1.15f;
            sil.gameObject.AddComponent<Rigidbody2D>().velocity = new Vector2(0f, 10f);
            sil.gameObject.GetComponent<Rigidbody2D>().gravityScale = 0f;
            sil.sprite = ArenaFinder.sprites["Zem_Sil_1"];
            yield return new WaitForSeconds(0.05f);
            sil.sprite = ArenaFinder.sprites["Zem_Sil_2"];
            yield return new WaitForSeconds(0.05f);
            sil.sprite = ArenaFinder.sprites["Zem_Sil_3"];
            yield return new WaitForSeconds(0.05f);
            sil.gameObject.SetActive(false);
        }

        private float FaceHero()
        {
            float currSign = Mathf.Sign(transform.GetScaleX());
            float heroSignX = Mathf.Sign(gameObject.transform.GetPositionX() - _target.transform.GetPositionX());
            Vector3 pScale = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector3(Mathf.Abs(pScale.x) * heroSignX, pScale.y, 1f);
            return heroSignX;
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
            if (self.name.Contains("Zemer"))
            {
                FlashWhite();
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
                if (!atPhase2 && _hm.hp < Phase2HP)
                {
                    atPhase2 = true;
                    EndPhase1();
                }
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

        private bool IsPlayAboveHead()
        {
            return FastApproximately(transform.position.x, _target.transform.GetPositionX(), 1.2f);
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
            foreach (FieldInfo fi in typeof(EnemyHitEffectsUninfected).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (fi.Name.Contains("Origin"))
                {
                    _hitEffects.effectOrigin = new Vector3(0f, 0.5f, 0f);
                    continue;
                }
                fi.SetValue(_hitEffects, fi.GetValue(ogrimHitEffects));
            }

            _deathEff = _dd.GetComponent<EnemyDeathEffectsUninfected>();
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
        
        public static bool FastApproximately(float a, float b, float threshold)
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

        private void Log(object o)
        {
            Modding.Logger.Log("[Zemer] " + o);
        }

        private void OnDestroy()
        {
            On.HealthManager.TakeDamage -= HealthManager_TakeDamage;
            On.EnemyDreamnailReaction.RecieveDreamImpact -= OnReceiveDreamImpact;
        }
    }
}
