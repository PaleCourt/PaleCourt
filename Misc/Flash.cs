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
        private static readonly int FlashColor = Shader.PropertyToID("_FlashColor");

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
			On.ExtraDamageable.RecieveExtraDamage += ExtraDamageableRecieveExtraDamage;
        }

		private void ResetValues(Color color, float amount, float timeUp, float stayTime, float timeDown)
        {
            _sr.material.SetColor(FlashColor, color);
            this.amount = amount;
            this.timeUp = timeUp;
            this.stayTime = stayTime;
            this.timeDown = timeDown;
            _sr.material.SetFloat(FlashAmount, 0f);
            flashingState = FlashState.Increase;
            flashTimer = 0f;
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

        public void FlashFocusHeal()
		{
            ResetValues(Color.white, 0.85f, 0.01f, 0.01f, 0.35f);
		}

        public void FlashDungQuick()
		{
            ResetValues(new Color(0.45f, 0.27f, 0f), 0.75f, 0.001f, 0.05f, 0.1f);
		}

        public void FlashSporeQuick()
		{
            ResetValues(new Color(0.95f, 0.9f, 0.15f), 0.75f, 0.001f, 0.05f, 0.1f);
		}

        private void HealthManager_TakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if(self.gameObject == gameObject)
            {
                FlashFocusHeal();
            }
            orig(self, hitInstance);
        }
        
        private void SpellFlukeOnDoDamage(On.SpellFluke.orig_DoDamage orig, SpellFluke self, GameObject tar, 
            int upwardrecursionamount, bool burst)
        {
            if(tar == gameObject)
            {
                FlashFocusHeal();
            }
            orig(self, tar, upwardrecursionamount, burst);
        }

        private void ExtraDamageableRecieveExtraDamage(On.ExtraDamageable.orig_RecieveExtraDamage orig, ExtraDamageable self, ExtraDamageTypes extraDamageType)
        {
            if(self != null && self.gameObject == gameObject)
            {
                if(extraDamageType == ExtraDamageTypes.Spore) FlashSporeQuick();
                else FlashDungQuick();
            }
            orig(self, extraDamageType);
        }

        private void OnDestroy()
        {
            On.HealthManager.TakeDamage -= HealthManager_TakeDamage;
            On.SpellFluke.DoDamage -= SpellFlukeOnDoDamage;
        }

        private void Log(object o)
		{
            Modding.Logger.Log("[Flash] " + o);
		}
    }
}
