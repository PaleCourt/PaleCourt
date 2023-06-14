using System.Collections;
using System.Linq;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using SFCore.Utils;

namespace FiveKnights
{
    public class RoyalAura : MonoBehaviour
    {
        private PlayMakerFSM _dungControl;
        private GameObject _dungTrail;
        private PlayMakerFSM _dungTrailControl;
        private ParticleSystem _dungPt;

        private float _frequency = 0.5f;
        private int _dungDamage = 2;
        private float _timer;

        private void OnEnable()
        {
            // Getting references
            GameObject dung = HeroController.instance.transform.Find("Charm Effects").Find("Dung").gameObject;
            _dungControl = dung.LocateMyFSM("Control");

            foreach(var pool in ObjectPool.instance.startupPools)
            {
                if(pool.prefab.name == "Knight Dung Trail")
                {
                    _dungTrail = Instantiate(pool.prefab);
                    _dungTrail.SetActive(false);
                    DontDestroyOnLoad(_dungTrail);
                    break;
                }
            }

            // Change color of effect
            if(dung.Find("Particle 1").GetComponent<ModifyAuraColor>() == null) dung.Find("Particle 1").AddComponent<ModifyAuraColor>();

            _dungTrailControl = _dungTrail.LocateMyFSM("Control");
            _dungPt = _dungTrailControl.Fsm.GetFsmGameObject("Pt Normal").Value.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule main = _dungPt.main;
            main.startColor = new Color(0.65f, 0.65f, 0.65f, 0.75f);

            // Remove original spawn method
            _dungControl.GetAction<Wait>("Emit Pause", 2).time.Value = 0.1f;
            _dungControl.GetAction<SpawnObjectFromGlobalPoolOverTime>("Equipped", 0).Enabled = false;

			On.ExtraDamageable.RecieveExtraDamage += ExtraDamageableRecieveExtraDamage;
			On.ExtraDamageable.GetDamageOfType += ExtraDamageableGetDamageOfType;
			On.SpriteFlash.flashDungQuick += SpriteFlashFlashDungQuick;
		}

		private void ExtraDamageableRecieveExtraDamage(On.ExtraDamageable.orig_RecieveExtraDamage orig, ExtraDamageable self, ExtraDamageTypes extraDamageType)
		{
			if(extraDamageType == ExtraDamageTypes.Dung || extraDamageType == ExtraDamageTypes.Dung2)
			{
                if(!self.gameObject.GetComponent<RoyalAuraSpread>()) self.gameObject.AddComponent<RoyalAuraSpread>();
			}
            orig(self, extraDamageType);
		}

        private int ExtraDamageableGetDamageOfType(On.ExtraDamageable.orig_GetDamageOfType orig, ExtraDamageTypes extraDamageTypes)
        {
            if(extraDamageTypes == ExtraDamageTypes.Dung || extraDamageTypes == ExtraDamageTypes.Dung2)
            {
                return _dungDamage;
            }
            return orig(extraDamageTypes);
        }

        private void SpriteFlashFlashDungQuick(On.SpriteFlash.orig_flashDungQuick orig, SpriteFlash self)
        {
            self.flashArmoured();
        }

        private void Update()
		{
            _timer += Time.deltaTime;
            if(_timer > _frequency)
			{
                _timer = 0f;
                GameObject dungTrail = Instantiate(_dungTrail, HeroController.instance.transform.position, Quaternion.identity);
                dungTrail.transform.localScale *= 2f;
                dungTrail.transform.SetPositionZ(0.01f);
                dungTrail.SetActive(true);
			}
		}

        private void OnDisable()
		{
            Destroy(_dungTrail);

            _dungControl.GetAction<Wait>("Emit Pause", 2).time.Value = 0.5f;
			_dungControl.GetAction<SpawnObjectFromGlobalPoolOverTime>("Equipped", 0).Enabled = true;

            On.ExtraDamageable.RecieveExtraDamage -= ExtraDamageableRecieveExtraDamage;
            On.ExtraDamageable.GetDamageOfType -= ExtraDamageableGetDamageOfType;
            On.SpriteFlash.flashDungQuick -= SpriteFlashFlashDungQuick;
        }

        private void Log(object message) => Modding.Logger.Log("[FiveKnights][Royal Aura] " + message);
    }
}
