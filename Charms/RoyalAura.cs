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

        private float timer;
        private float frequency = 0.5f;
        private int dungDamage = 2;

        private void OnEnable()
        {
            // Getting references
            GameObject something = HeroController.instance.transform.Find("Charm Effects").Find("Dung").gameObject;
            _dungControl = something.LocateMyFSM("Control");

            foreach(var pool in ObjectPool.instance.startupPools)
            {
                if(pool.prefab.name == "Knight Dung Trail")
                {
                    _dungTrail = pool.prefab;
                    break;
                }
            }
            _dungTrailControl = _dungTrail.LocateMyFSM("Control");
            _dungPt = _dungTrailControl.Fsm.GetFsmGameObject("Pt Normal").Value.GetComponent<ParticleSystem>();

            // Remove original spawn method
            _dungControl.GetAction<Wait>("Emit Pause", 2).time.Value = 0.1f;
            _dungControl.GetAction<SpawnObjectFromGlobalPoolOverTime>("Equipped", 0).Enabled = false;

			On.ExtraDamageable.RecieveExtraDamage += ExtraDamageableRecieveExtraDamage;
			On.ExtraDamageable.GetDamageOfType += ExtraDamageableGetDamageOfType;
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
                return dungDamage;
            }
            return orig(extraDamageTypes);
        }

        private void Update()
		{
            timer += Time.deltaTime;
            if(timer > frequency)
			{
                timer = 0f;
                GameObject dungTrail = Instantiate(_dungTrail, HeroController.instance.transform.position, Quaternion.identity);
                dungTrail.transform.localScale *= 2f;
                dungTrail.transform.SetPositionZ(0.01f);
                dungTrail.SetActive(true);
			}
		}

        private void OnDisable()
		{
            _dungControl.GetAction<Wait>("Emit Pause", 2).time.Value = 0.5f;
			_dungControl.GetAction<SpawnObjectFromGlobalPoolOverTime>("Equipped", 0).Enabled = true;

            On.ExtraDamageable.RecieveExtraDamage -= ExtraDamageableRecieveExtraDamage;
            On.ExtraDamageable.GetDamageOfType -= ExtraDamageableGetDamageOfType;
        }

        private void Log(object message) => Modding.Logger.Log("[FiveKnights][Royal Aura] " + message);
    }
}
