using System;
using System.Collections.Generic;
using UnityEngine;
using Modding;
using GlobalEnums;

namespace FiveKnights
{
	public class CheckBloomStage : MonoBehaviour
	{
        private HeroController _hc => HeroController.instance;
        private AbyssalBloom _bloom;

		private void Awake()
		{
            _bloom = _hc.GetComponent<AbyssalBloom>();

			ModHooks.CharmUpdateHook += CharmUpdateHook;
			On.HeroController.TakeDamage += HeroControllerTakeDamage;
			On.HeroController.AddHealth += HeroControllerAddHealth;
			On.HeroController.MaxHealth += HeroControllerMaxHealth;
		}

        private void CharmUpdateHook(PlayerData data, HeroController hc) => CheckStage();

        private void HeroControllerTakeDamage(On.HeroController.orig_TakeDamage orig, HeroController self, GameObject go, CollisionSide damageSide, int damageAmount, int hazardType)
        {
            int oldHealth = PlayerData.instance.health;
            orig(self, go, damageSide, damageAmount, hazardType);
            if(PlayerData.instance.health == oldHealth) return;
            CheckStage();
        }

        private void HeroControllerAddHealth(On.HeroController.orig_AddHealth orig, HeroController self, int amount)
        {
            orig(self, amount);
            CheckStage();
        }

        private void HeroControllerMaxHealth(On.HeroController.orig_MaxHealth orig, HeroController self)
        {
            orig(self);
            CheckStage();
        }

        private void CheckStage()
		{
            if(!FiveKnights.Instance.SaveSettings.equippedCharms[3])
            {
                _bloom.SetLevel(0);
                return;
            }

            if(_hc.playerData.health > (_hc.playerData.maxHealth + 1) / 2)
            {
                _bloom.SetLevel(0);
            }
            else if(_hc.playerData.health > 1 && _hc.playerData.health <= (_hc.playerData.maxHealth + 1) / 2)
            {
                Log("On level 1");
                _bloom.SetLevel(1);
            }
            else if(_hc.playerData.health <= 1)
            {
                Log("On level 2");
                _bloom.SetLevel(2);
            }
        }

        private void OnDestroy()
		{
            ModHooks.CharmUpdateHook -= CharmUpdateHook;
            On.HeroController.TakeDamage -= HeroControllerTakeDamage;
            On.HeroController.AddHealth -= HeroControllerAddHealth;
            On.HeroController.MaxHealth -= HeroControllerMaxHealth;
        }

        private void Log(object o) => Modding.Logger.Log("[FiveKnights][CheckBloomStage] " + o);
	}
}
