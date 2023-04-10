using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SFCore.Utils;
using UnityEngine;
using Vasi;
using Random = System.Random;

namespace FiveKnights.Tiso
{

    public class TisoController : MonoBehaviour
    {

        private HealthManager _hm;
        private BoxCollider2D _bc;
        private SpriteRenderer _sr;
        private EnemyDreamnailReaction _dnailReac;
        private GameObject _dd;
        private GameObject _dnailEff;
        private Animator _anim;
        private Rigidbody2D _rb;
        private bool _hasDied;
        private EnemyHitEffectsUninfected _hitEffects;
        private GameObject _target;
        public static readonly float GroundY = 3.5f;
        public static readonly float LeftX = 51.2f;
        public static readonly float RightX = 71.7f;
        public static readonly float MiddleX = 61f;
        private const int MaxHP = 1000;
        private const int MaxDreamAmount = 3;
        private Random _rand;
        private TisoAttacks _attacks;

        private Dictionary<Func<IEnumerator>, int> _rep;

        private Dictionary<Func<IEnumerator>, int> _max;

        private void Awake()
        {
            On.HealthManager.TakeDamage += HealthManager_TakeDamage;
            On.EnemyDreamnailReaction.RecieveDreamImpact += OnReceiveDreamImpact;
            On.SpellFluke.DoDamage += SpellFlukeOnDoDamage;
            _hm = gameObject.AddComponent<HealthManager>();
            _anim = gameObject.GetComponent<Animator>();
            _bc = gameObject.AddComponent<BoxCollider2D>();
            _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.isKinematic = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _sr = GetComponent<SpriteRenderer>();
            _dnailReac = gameObject.AddComponent<EnemyDreamnailReaction>();
            gameObject.AddComponent<DamageHero>().damageDealt = 1;
            _dd = Instantiate(FiveKnights.preloadedGO["WhiteDef"]);
            _dd.SetActive(false);
            _dnailEff = _dd.GetComponent<EnemyDreamnailReaction>()
                .GetAttr<EnemyDreamnailReaction, GameObject>("dreamImpactPrefab");
            _rand = new Random();
            _dnailReac.enabled = true;
            Mirror.SetField(_dnailReac, "convoAmount", MaxDreamAmount);

            _hitEffects = gameObject.AddComponent<EnemyHitEffectsUninfected>();
            _hitEffects.enabled = true;
            gameObject.AddComponent<Flash>();

            _target = HeroController.instance.gameObject;

            _attacks = new TisoAttacks(transform, _rb, _bc, _anim);

            _rep = new Dictionary<Func<IEnumerator>, int>
            {
                [_attacks.Shoot] = 0,
                [_attacks.ThrowShield] = 0,
            };

            _max = new Dictionary<Func<IEnumerator>, int>
            {
                [_attacks.Shoot] = 1,
                [_attacks.ThrowShield] = 1,
            };

            AssignFields();

            _hm.hp = MaxHP;
            gameObject.layer = 11;
        }

        private IEnumerator Start()
        {
            yield return DoIntro();
            StartCoroutine(Attacks());
        }

        private void Update()
        {
            Vector3 pos = transform.position;

            if (pos.x > RightX)
            {
                _rb.velocity = Vector2.zero;
                transform.position = new Vector3(RightX, pos.y);
            }
            if (pos.x < LeftX)
            {
                _rb.velocity = Vector2.zero;
                transform.position = new Vector3(LeftX, pos.y);
            }
        }

        private IEnumerator DoIntro()
        {
            _sr.enabled = false;
            yield return new WaitForSeconds(3f);
            // Spawn him in top right of arena so he jumps down
            _sr.enabled = true;
            transform.position = new Vector3(MiddleX + 5f, GroundY + 14f);
            _bc.enabled = false;
            _rb.gravityScale = 1.5f;
            _rb.isKinematic = false;
            Log("In here baby");
            _anim.Play("TisoSpin");
            // Wait till he hits the ground
            yield return new WaitWhile(() => transform.position.y > GroundY);
            Log("In here baby2");
            _bc.enabled = true;
            _rb.gravityScale = 0f;
            _rb.isKinematic = true;
            _rb.velocity = Vector2.zero;
            transform.position = new Vector3(transform.position.x, GroundY);
            // Play intro and wait a bit in the part where he shows off his shield
            yield return _anim.PlayToEnd("TisoLand");
            _anim.Play("TisoRoar");
            yield return new WaitForSeconds(0.75f);
            _anim.Play("TisoIntro");
            yield return new WaitForSeconds(0.75f);
        }

        private IEnumerator Attacks()
        {
            while (true)
            {
                Log("[Setting Attacks]");

                Vector2 hPos = _target.transform.position;
                Vector2 tPos = transform.position;

                if (Mathf.Abs(hPos.x - tPos.x) > 4f && _rand.Next(3) == 0)
                {
                    Log("Doing Walk");
                    float dir = Mathf.Sign(hPos.x - tPos.x);
                    yield return _attacks.Walk(dir > 0 ? tPos.x + 3f : tPos.x - 3f);
                    Log("Done Walk");
                }
                else if (hPos.x.Within(tPos.x, 1f))
                {
                    Log("Doing Dodge");
                    yield return _attacks.Dodge();
                    Log("Done Dodge");
                }

                List<Func<IEnumerator>> attLst = new List<Func<IEnumerator>>
                {
                    _attacks.Shoot, _attacks.ThrowShield
                };

                Func<IEnumerator> currAtt = ChooseAttack(attLst);

                Log("Doing " + currAtt.Method.Name);
                yield return currAtt();
                Log("Done " + currAtt.Method.Name);

                Log("[Restarting Calculations]");
                yield return new WaitForEndOfFrame();
            }
        }

        private void HealthManager_TakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self,
            HitInstance hitInstance)
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

        private void DoTakeDamage(GameObject tar, float dir)
        {
            if (tar.name.Contains("Zemer"))
            {
                _hitEffects.RecieveHitEffect(dir);

                if (_hm.hp <= 50)
                {
                    _hasDied = true;
                    _bc.enabled = false;
                    // Die method here
                }
            }
        }

        private void OnReceiveDreamImpact(On.EnemyDreamnailReaction.orig_RecieveDreamImpact orig,
            EnemyDreamnailReaction self)
        {
            if (self.name.Contains("Zemer"))
            {
                StartCoroutine(FlashWhite());
                Instantiate(_dnailEff, transform.position, Quaternion.identity);
                _dnailReac.SetConvoTitle("ZEM_DREAM");
            }

            orig(self);
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
        }

        Func<IEnumerator> ChooseAttack(List<Func<IEnumerator>> attLst)
        {
            List<Func<IEnumerator>> cpyList = new List<Func<IEnumerator>>(attLst);
            Func<IEnumerator> currAtt = cpyList[_rand.Next(0, cpyList.Count)];

            while (currAtt != null && cpyList.Count > 0 && _rep[currAtt] >= _max[currAtt])
            {
                currAtt = cpyList[_rand.Next(0, cpyList.Count)];
                cpyList.Remove(currAtt);
            }

            if (cpyList.Count == 0)
            {
                foreach (var att in attLst.Where(x => x != null))
                {
                    _rep[att] = 0;
                }

                currAtt = attLst[_rand.Next(0, attLst.Count)];
            }

            if (currAtt != null) _rep[currAtt]++;

            return currAtt;
        }

        private void Log(object o)
        {
            Modding.Logger.Log("[Tiso] " + o);
        }
    }
}