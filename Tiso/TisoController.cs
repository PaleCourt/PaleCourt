using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SFCore.Utils;
using UnityEngine;
using Vasi;
using Random = System.Random;

namespace FiveKnights.Tiso;

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
    private static readonly float GroundY = 0f;
    private static readonly float LeftX = 0f;
    private static readonly float RightX = 0f;
    private static readonly float MiddleX = 0f;
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
        _rand = new Random();
        _dnailReac.enabled = true;
        Mirror.SetField(_dnailReac, "convoAmount", MaxDreamAmount);

        _hitEffects = gameObject.AddComponent<EnemyHitEffectsUninfected>();
        _hitEffects.enabled = true;
        gameObject.AddComponent<Flash>();

        _attacks = new TisoAttacks(transform, _rb, _bc, _anim);
        
        _rep = new Dictionary<Func<IEnumerator>, int>
        {
            [_attacks.Shoot] = 0,
            [_attacks.ThrowShield] = 0,
            [_attacks.Dodge] = 0,
        };
        
        _max = new Dictionary<Func<IEnumerator>, int>
        {
            [_attacks.Shoot] = 1,
            [_attacks.ThrowShield] = 1,
            [_attacks.Dodge] = 1,
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

    private IEnumerator DoIntro()
    {
        // Spawn him in top right of arena so he jumps down
        transform.position = new Vector3();
        _bc.enabled = false;
        _rb.gravityScale = 1f;
        _rb.isKinematic = true;
        _anim.Play("TisoSpin");
        // Wait till he hits the ground
        yield return new WaitWhile(() => transform.position.y > GroundY);
        _bc.enabled = true;
        _rb.gravityScale = 0f;
        _rb.isKinematic = false;
        _rb.velocity = Vector2.zero;
        transform.position = new Vector3(transform.position.x, GroundY);
        // Play intro and wait a bit in the part where he shows off his shield
        yield return _anim.PlayToEnd("TisoLand");
        yield return _anim.PlayToEnd("TisoRoar");
        yield return _anim.PlayToEnd("TisoIntro");
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator Attacks()
    {
        while (true)
        {
            Log("[Waiting to start calculation]");
            float xDisp = (transform.position.x < MiddleX) ? 8f : -8f;
            yield return _attacks.Walk(xDisp + _rand.Next(-2,3));
            Log("[Setting Attacks]");

            List<Func<IEnumerator>> attLst = new List<Func<IEnumerator>>
            {
                _attacks.Shoot, _attacks.ThrowShield, _attacks.Dodge
            };
            
            Func<IEnumerator> currAtt = ChooseAttack(attLst);
            
            Log("Doing " + currAtt.Method.Name);
            yield return currAtt();
            Log("Done " + currAtt.Method.Name);

            Log("[Restarting Calculations]");
            yield return new WaitForEndOfFrame();
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