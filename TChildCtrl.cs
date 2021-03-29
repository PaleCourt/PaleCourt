using System;
using UnityEngine;
using System.Collections;
using System.Linq;
using System.Reflection;
using GlobalEnums;
using HutongGames.PlayMaker.Actions;
using Vasi;
namespace FiveKnights
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

        private void HealthManagerOnTakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitinstance)
        {
            if (!_isAtt && self.name.Contains("TChild"))
            {
                _hitEffects.RecieveHitEffect(hitinstance.Direction);
                _isAtt = true;
                StartCoroutine(DoAttack());
            }
            orig(self, hitinstance);
        }

        private IEnumerator Start()
        {
            gameObject.layer = 11;
            _hitEffects.enabled = true;
            _hm.enabled = false;
            _bc.enabled = false;
            transform.position = new Vector3(171.9f, 107.32f, 0f);
            transform.localScale *= 1.2f;
            yield return new WaitForSeconds(2.5f);
            _anim.Play("Idle");
            yield return null;
            yield return LookAndRun();
        }

        // TODO No check for if player gets ahead

        private IEnumerator LookAndRun()
        {
            _anim.Play("Turn");
            yield return _anim.PlayToEnd();
            yield return new WaitForSeconds(0.75f);

            yield return _anim.PlayToFrame("RunStart",2);
            _rb.velocity = new Vector2(15f, 0f);
            yield return _anim.PlayToEnd();

            Run();

            yield return new WaitWhile(() => transform.position.x < 250f);
            _anim.speed = 1f;
            if (helpZemer) yield break;
            _bc.enabled = _hm.enabled = true;
            _rb.velocity = Vector2.zero;
            _anim.Play("Turn");
            yield return _anim.PlayToEnd();
            yield return new WaitWhile(()=>HeroController.instance.transform.position.x < 243f);
            yield return new WaitForSeconds(3f);
            if (_isAtt) yield break;
            _bc.enabled = false;
            StartCoroutine(LeaveAndReturn());
        }

        private void Run(float xSpd = 15f)
        {
            _anim.speed *= 1.4f;
            _rb.velocity = new Vector2(xSpd, 0f);
            _anim.Play("Run");
        }

        private IEnumerator LeaveAndReturn()
        {
            Run(10);
            _bc.enabled = false;
            _hm.enabled = false;
            yield return new WaitForSeconds(0.5f);
            _rb.velocity = Vector2.zero;
            _anim.speed = 1f;
            helpZemer = true;
            _anim.Play("Leave");
            yield return _anim.PlayToEnd();
            _bc.enabled = false;
            _sr.enabled = false;
            yield return new WaitForSeconds(1f);
            _anim.Play("Back");
            _sr.enabled = true;
            transform.position = new Vector3(259.1f, 108.4f, 7f);
            yield return _anim.PlayToEnd();
            Modding.Logger.Log("DEVA");
            _bc.enabled = false;
            _hm.enabled = false;
            Destroy(this);
        }

        private IEnumerator DoAttack()
        {
            _anim.speed = 1f;
            _rb.velocity = Vector2.zero;
            _anim.Play("StartParry");
            yield return _anim.PlayToEnd();
            _anim.Play("Parry");
            yield return _anim.PlayToEnd();
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
    }
}