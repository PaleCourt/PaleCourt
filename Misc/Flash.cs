using System;
using UnityEngine;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace FiveKnights
{
    public class Flash : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private float amount;
        private float t;
        private float timeUp;
        private float stayTime;
        private float timeDown;
        private float flashTimer;
        private bool flashing;
        private FlashState flashingState;
        private float amountCurrent;
        private static readonly int FlashAmount = Shader.PropertyToID("_FlashAmount");

        private enum FlashState
        {
            Increase,
            Stay,
            Decrease,
            Stop
        };

        private void Start()
        {
            _sr = gameObject.GetComponent<SpriteRenderer>();
            _sr.material = FiveKnights.Materials["flash"];
            On.HealthManager.TakeDamage += HealthManager_TakeDamage;
            On.SpellFluke.DoDamage += SpellFlukeOnDoDamage;
        }

        private void ResetValues()
        {
            amount = 0.85f;
            timeUp = 0.01f;
            stayTime = 0.01f;
            timeDown = 0.35f;
            _sr.material.SetFloat(FlashAmount, 0f);
            flashingState = FlashState.Increase;
            flashTimer = 0.0f;
        }

        private void Update()
        {
            if (flashingState == FlashState.Increase)
            {
                if (flashTimer < timeUp)
                {
                    flashTimer += Time.deltaTime;
                    t = flashTimer / timeUp;
                    amountCurrent = Mathf.Lerp(0.0f, amount, t);
                    _sr.material.SetFloat(FlashAmount, amountCurrent);
                }
                else
                {
                    _sr.material.SetFloat(FlashAmount, amount);
                    flashTimer = 0.0f;
                    flashing = false;
                    flashingState = FlashState.Stay;
                }
            }
            if (flashingState == FlashState.Stay)
            {
                if (flashTimer < stayTime)
                {
                    flashTimer += Time.deltaTime;
                }
                else
                {
                    flashTimer = 0.0f;
                    flashingState = FlashState.Decrease;
                }
            }
            if (flashingState == FlashState.Decrease)
            {
                if (flashTimer < timeDown)
                {
                    flashTimer += Time.deltaTime;
                    t = flashTimer / timeDown;
                    amountCurrent = Mathf.Lerp(amount, 0.0f, t);
                    _sr.material.SetFloat(FlashAmount, amountCurrent);
                }
                else
                {
                    _sr.material.SetFloat(FlashAmount, 0f);
                    flashTimer = 0.0f;
                    flashingState = FlashState.Stop;
                }
            }
        }

        private void HealthManager_TakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.gameObject == gameObject)
            {
                ResetValues();
            }
            orig(self, hitInstance);
        }
        
        private void SpellFlukeOnDoDamage(On.SpellFluke.orig_DoDamage orig, SpellFluke self, GameObject tar, 
            int upwardrecursionamount, bool burst)
        {
            if (tar == gameObject)
            {
                ResetValues();
            }
            orig(self, tar, upwardrecursionamount, burst);
        }

        private void OnDestroy()
        {
            On.HealthManager.TakeDamage -= HealthManager_TakeDamage;
            On.SpellFluke.DoDamage -= SpellFlukeOnDoDamage;
        }
    }
}
