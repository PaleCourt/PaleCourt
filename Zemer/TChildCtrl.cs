using System.Collections;
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
        private const float StopAtX = 256f;
        
        public bool helpZemer;

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
            yield return LookAndRun();
        }

        private void Update()
        {
            if (_run || _isAtt) return;
            if (HeroController.instance.transform.position.x < transform.position.x) return;
            _run = true;
            StartCoroutine(TPAway());
        }

        private IEnumerator TPAway()
        {
            yield return Leave(false);
            yield return Arrive(new Vector3(StopAtX, GroundY));
            StartCoroutine(LeaveAndReturn());
        }

        private IEnumerator LookAndRun()
        {
            // Turn to player and run
            yield return _anim.PlayBlocking("Turn");
            yield return new WaitForSeconds(0.25f);
            yield return _anim.PlayToFrame("RunStart",2);
            _rb.velocity = new Vector2(17.5f, 0f);
            yield return _anim.PlayToEnd();
            Run();
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
            // Waiting for player routine
            StartCoroutine(LeaveAndReturn());
        }

        private IEnumerator LeaveAndReturn()
        {
            _hm.IsInvincible = true;
            yield return new WaitWhile(() => HeroController.instance.transform.position.x < 243f);
            
            OWBossManager.PlayMusic(null);
            HeroController.instance.GetComponent<tk2dSpriteAnimator>().Play("Roar Lock");
            HeroController.instance.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            //HeroController.instance.GetComponent<tk2dSpriteAnimator>().Stop();
            HeroController.instance.RelinquishControl();
            HeroController.instance.StopAnimationControl();
            HeroController.instance.GetComponent<Rigidbody2D>().Sleep();
            
            yield return new WaitForSeconds(1.5f);
            yield return Leave(true);
            
            yield return new WaitForSeconds(1f);
            yield return Arrive(new Vector3(259.1f, 108.4f, 7f));
            
            Destroy(this);
        }

        private IEnumerator Leave(bool unfreezeH)
        {
            _anim.speed = 1f;

            yield return _anim.PlayToFrame("Leave", 2);

            if (unfreezeH)
            {
                helpZemer = true;
                HeroController.instance.GetComponent<Rigidbody2D>().WakeUp();
                HeroController.instance.RegainControl();
                HeroController.instance.StartAnimationControl();
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
        
        private void Run(float xSpd = 15f)
        {
            _anim.speed *= 1.4f;
            _rb.velocity = new Vector2(xSpd, 0f);
            _anim.Play("Run");
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