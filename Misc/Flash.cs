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
            On.HealthManager.TakeDamage += HealthManagerTakeDamage;
            On.SpellFluke.DoDamage += SpellFlukeOnDoDamage;
            On.ExtraDamageable.RecieveExtraDamage += ExtraDamageableRecieveExtraDamage;
            On.EnemyDreamnailReaction.RecieveDreamImpact += EnemyDreamnailReactionRecieveDreamImpact;
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

        public void FlashArmoured()
        {
            ResetValues(Color.white, 0.85f, 0.01f, 0.01f, 0.25f);
        }

        public void FlashDungQuick()
        {
            ResetValues(new Color(0.45f, 0.27f, 0f), 0.75f, 0.001f, 0.05f, 0.1f);
        }

        public void FlashSporeQuick()
        {
            ResetValues(new Color(0.95f, 0.9f, 0.15f), 0.75f, 0.001f, 0.05f, 0.1f);
        }

        public void FlashDreamImpact()
        {
            ResetValues(Color.white, 0.9f, 0.01f, 0.25f, 0.75f);
        }

        private void HealthManagerTakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            orig(self, hitInstance);
            if(self.gameObject == gameObject)
            {
                FlashFocusHeal();
            }
        }
        
        private void SpellFlukeOnDoDamage(On.SpellFluke.orig_DoDamage orig, SpellFluke self, GameObject tar, 
            int upwardrecursionamount, bool burst)
        {
            orig(self, tar, upwardrecursionamount, burst);
            if(tar == gameObject)
            {
                FlashFocusHeal();
            }
        }

        private void ExtraDamageableRecieveExtraDamage(On.ExtraDamageable.orig_RecieveExtraDamage orig, ExtraDamageable self, ExtraDamageTypes extraDamageType)
        {
            orig(self, extraDamageType);
            if(self != null && self.gameObject == gameObject)
            {
                if(extraDamageType == ExtraDamageTypes.Spore) FlashSporeQuick();
                else if(FiveKnights.Instance.SaveSettings.upgradedCharm_10) FlashArmoured();
                else FlashDungQuick();
            }
        }

        private void EnemyDreamnailReactionRecieveDreamImpact(On.EnemyDreamnailReaction.orig_RecieveDreamImpact orig, EnemyDreamnailReaction self)
        {
            orig(self);
            if(self.gameObject == gameObject)
            {
                FlashDreamImpact();
            }
        }

        private void OnDestroy()
        {
            On.HealthManager.TakeDamage -= HealthManagerTakeDamage;
            On.SpellFluke.DoDamage -= SpellFlukeOnDoDamage;
            On.ExtraDamageable.RecieveExtraDamage -= ExtraDamageableRecieveExtraDamage;
            On.EnemyDreamnailReaction.RecieveDreamImpact -= EnemyDreamnailReactionRecieveDreamImpact;
        }

        private void Log(object o)
        {
            Modding.Logger.Log("[Flash] " + o);
        }
    }
}
