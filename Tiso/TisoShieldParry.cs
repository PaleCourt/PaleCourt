using System;
using System.Collections;
using GlobalEnums;
using UnityEngine;

namespace FiveKnights.Tiso;

public class TisoShieldParry : MonoBehaviour
{
    private BoxCollider2D _bc;
    public Action DoWhenHit = () => { };
    public Func<bool> Predicate;
    private HealthManager _hm;
    public bool hitFlag;
    private void Awake()
    {
        _bc = GetComponent<BoxCollider2D>();
        _hm = transform.parent.parent.GetComponent<HealthManager>();
        On.HealthManager.Hit += OnBlockedHit;
    }

    private void Update()
    {
        if (!_bc.enabled || _hm.IsInvincible || !Predicate()) return;
        _hm.InvincibleFromDirection = 0;
        _hm.IsInvincible = true;
        StartCoroutine(StopInvinc());
    }

    IEnumerator StopInvinc()
    {
        yield return new WaitForSeconds(0.1f);
        _hm.IsInvincible = false;
    }

    private void OnBlockedHit(On.HealthManager.orig_Hit orig, HealthManager self, HitInstance hitInstance)
    {
        if (self.name.Contains("Tiso") && _hm.IsInvincible)
        {
            hitFlag = true;
        }

        orig(self, hitInstance);
    }
    private void OnDestroy() => On.HealthManager.Hit -= OnBlockedHit;
}