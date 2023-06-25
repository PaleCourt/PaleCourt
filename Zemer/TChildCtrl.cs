using SFCore.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace FiveKnights.Zemer
{
    public class TChildCtrl : MonoBehaviour
    {
        private BoxCollider2D _bc;
        private HealthManager _hm;
        private EnemyHitEffectsUninfected _hitEffects;
        private SpriteRenderer _sr;
        private Rigidbody2D _rb;
        private Animator _anim;
        private bool _isAtt;
        private bool _run;
        private const float GroundY = 107.32f;
        private const float StopAtX = 265.9f;

        public bool helpZemer;
        public GameObject zemer;
        private bool leaveAndRet;

        private void Awake()
        {
            On.HealthManager.TakeDamage  += HealthManagerOnTakeDamage;
            _bc = GetComponent<BoxCollider2D>();
            _hm = gameObject.AddComponent<HealthManager>();
            _hitEffects = gameObject.AddComponent<EnemyHitEffectsUninfected>();
            _sr = GetComponent<SpriteRenderer>();
            _rb = GetComponent<Rigidbody2D>();
            _anim = GetComponent<Animator>();
            gameObject.AddComponent<DamageHero>().damageDealt = 1;
            gameObject.AddComponent<Flash>();
            AssignFields();
        }

        private IEnumerator Start()
        {
            gameObject.layer = 11;
            _hitEffects.enabled = true;
            leaveAndRet = false;
            _hm.enabled = false;
            _bc.enabled = false;
            transform.position = new Vector3(171.9f, GroundY, 0f);
            transform.localScale *= 1.2f;
            _anim.Play("Idle");
            yield return new WaitWhile(() => HeroController.instance == null);
            // Run away when the player's arrival animation begins playing
            var dEntry = GameObject.Find("Dream Entry");
            var fsm = dEntry.LocateMyFSM("Control");
            yield return new WaitWhile(() => !fsm.ActiveStateName.Contains("Anim"));
            StartCoroutine(LookAndRun());
            StartCoroutine(FreezePlayer());
        }

        private void Update()
        {
            if (_run || _isAtt) return;
            if (HeroController.instance.transform.position.x < transform.position.x) return;
            if (leaveAndRet) return;
            _run = true;
            StartCoroutine(TPAway());
        }

        private IEnumerator LookAndRun()
        {
            // Turn to player and run
            yield return _anim.PlayBlocking("Turn");
            yield return new WaitForSeconds(0.25f);
            yield return _anim.PlayToFrame("RunStart",2);
            _rb.velocity = new Vector2(17.5f, 0f);
            yield return _anim.PlayToEnd();

            // Start running
            _anim.speed *= 1.4f;
            _rb.velocity = new Vector2(15f, 0f);
            _anim.Play("Run");

            // Stops once gets behind Ze'mer or if hit do hit path
            yield return new WaitWhile(() => transform.position.x < StopAtX && !_isAtt && !_run);
            if (_isAtt)
            {
                StartCoroutine(DoAttack());
                yield break;
            }
            if (_run) yield break;
            _anim.speed = 1f;
            _rb.velocity = Vector2.zero;
            yield return _anim.PlayBlocking("Turn");
            _hm.IsInvincible = true;
            _bc.enabled = _hm.enabled = false;
            leaveAndRet = true;

            // Waiting for player routine
            StartCoroutine(LeaveAndReturn());
        }

        private IEnumerator FreezePlayer()
		{
            // Stops player until she leaves
            yield return new WaitWhile(() => HeroController.instance.transform.position.x < 256f);

            OWBossManager.PlayMusic(null);
            PlayMakerFSM roarFSM = HeroController.instance.gameObject.LocateMyFSM("Roar Lock");
            roarFSM.GetFsmGameObjectVariable("Roar Object").Value = gameObject;
            roarFSM.SendEvent("ROAR ENTER");
        }

        private IEnumerator TPAway()
        {
            yield return Leave(false);
            yield return Arrive(new Vector3(StopAtX, GroundY));
            StartCoroutine(LeaveAndReturn());
        }

        private IEnumerator LeaveAndReturn()
        {
            _hm.IsInvincible = true;
            yield return new WaitWhile(() => HeroController.instance.transform.position.x < 256f);
            
            yield return new WaitForSeconds(1.5f);
            yield return Leave(true);
            
            yield return new WaitForSeconds(1f);
            yield return Arrive(new Vector3(259.1f, 108.4f, 7f));

            StartCoroutine(HeadTurn());
        }

        IEnumerator HeadTurn()
        {
            Dictionary<int, (int, int)> ranges = new Dictionary<int, (int, int)>
            {
                [-2] = (0, 252),
                [-1] = (252, 258),
                [0] = (258, 262),
                [1] = (262, 500)
            };
            
            // What from to go to if going from left to right
            Dictionary<int, int> typeToFrameFromLeft = new Dictionary<int, int>
            {
                [-2] = 0,
                [-1] = 1,
                [0] = 3,
                [1] = 5
            };
            
            // What frame to go to if going from right to left
            Dictionary<int, int> typeToFrameFromRight = new Dictionary<int, int>
            {
                [-2] = 5,
                [-1] = 4,
                [0] = 2,
                [1] = 0
            };

            int oldType = 0;
            
            while (zemer != null)
            {
                float xPos = zemer.transform.position.x;

                // If we are at the correct range, then do nothing
                if (xPos > ranges[oldType].Item1 && xPos < ranges[oldType].Item2)
                {
                    yield return null;
                    continue;
                }

                int newType = FindCorrectRange(xPos);
                _anim.enabled = true;
                
                if (newType > oldType)
                {
                    yield return _anim.PlayToFrameAt("LookRight", typeToFrameFromLeft[oldType],
                        typeToFrameFromLeft[newType]);
                }
                else
                {
                    yield return _anim.PlayToFrameAt("LookLeft", typeToFrameFromRight[oldType],
                        typeToFrameFromRight[newType]);
                }

                _anim.enabled = false;
                oldType = newType;
            }

            int FindCorrectRange(float xPos)
            {
                return (from kp in ranges where xPos > kp.Value.Item1 && xPos < kp.Value.Item2 select kp.Key)
                    .FirstOrDefault();
            }
        }

        private IEnumerator Leave(bool unfreezeH)
        {
            _anim.speed = 1f;

            yield return _anim.PlayToFrame("Leave", 2);

            if(unfreezeH)
            {
                helpZemer = true;
                PlayMakerFSM roarFSM = HeroController.instance.gameObject.LocateMyFSM("Roar Lock");
                roarFSM.SendEvent("ROAR EXIT");
            }
            
            _rb.velocity = new Vector2(10f, 0f);
            yield return null;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 9);
            _rb.velocity = new Vector2(10f, 10f);
            yield return _anim.PlayToEnd();
            _rb.velocity = Vector2.zero;
            _sr.enabled = false;
            if (_bc != null) Destroy(_bc);
        }

        private IEnumerator Arrive(Vector3 pos)
        {
            _anim.Play("Back");
            _sr.enabled = true;
            transform.position = pos;
            yield return _anim.PlayToEnd();
            if (_bc != null) Destroy(_bc);
        }
        
        private void HealthManagerOnTakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitinstance)
        {
            if (!_run && !_isAtt && self.name.Contains("TChild"))
            {
                _hitEffects.RecieveHitEffect(hitinstance.Direction);
                _isAtt = true;
            }
            orig(self, hitinstance);
        }
        
        private IEnumerator DoAttack()
        {
            _anim.speed = 1f;
            _rb.velocity = Vector2.zero;
            _anim.Play("StartParry");
            yield return _anim.PlayToEnd();
            _anim.Play("Parry");
            yield return _anim.PlayToEnd();
            yield return Leave(false);
            yield return Arrive(new Vector3(StopAtX, GroundY));
            StartCoroutine(LeaveAndReturn());
        }
        
        private void AssignFields()
        {
            _hm.hp = 10000;
            GameObject _dd = FiveKnights.preloadedGO["WD"];
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
        }

        private void OnDestroy()
        {
            Modding.Logger.Log("Checking to make sure bruh");
            On.HealthManager.TakeDamage -= HealthManagerOnTakeDamage;
        }
    }
}