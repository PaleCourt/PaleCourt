using System.Collections;
using System.Linq;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using SFCore.Utils;

namespace FiveKnights
{
    public class RoyalAura : MonoBehaviour
    {
        private GameObject _dungTrail;
        private GameObject _dungCloud;

        private PlayMakerFSM _dungControl;
        private PlayMakerFSM _dungTrailControl;
        private PlayMakerFSM _spellControl;

        private readonly Color PaleColor = new Color(0.65f, 0.65f, 0.65f, 0.75f);

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
                }
                if(pool.prefab.name == "Knight Dung Cloud")
				{
                    _dungCloud = Instantiate(pool.prefab);
                    _dungCloud.SetActive(false);
                    DontDestroyOnLoad(_dungCloud);
				}
            }
            _spellControl = HeroController.instance.spellControl;

            // Change color of effect
            if(dung.Find("Particle 1").GetComponent<ModifyAuraColor>() == null) dung.Find("Particle 1").AddComponent<ModifyAuraColor>();

            _dungTrailControl = _dungTrail.LocateMyFSM("Control");
            ParticleSystem trailPt = _dungTrailControl.Fsm.GetFsmGameObject("Pt Normal").Value.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule trailMain = trailPt.main;
            trailMain.startColor = PaleColor;

            foreach(ParticleSystem cloudPt in _dungCloud.GetComponentsInChildren<ParticleSystem>(true))
			{
                ParticleSystem.MainModule cloudMain = cloudPt.main;
                cloudMain.startColor = PaleColor;
            }
            foreach(tk2dSprite sprite in _dungCloud.GetComponentsInChildren<tk2dSprite>(true))
			{
                sprite.color = Color.white;
            }

            // Remove original spawn method
            _dungControl.GetAction<Wait>("Emit Pause", 2).time.Value = 0.1f;
            _dungControl.GetAction<SpawnObjectFromGlobalPoolOverTime>("Equipped", 0).Enabled = false;

            // Add custom dung cloud spawn
            foreach(string state in new []{ "Dung Cloud", "Dung Cloud 2" })
			{
                if(_spellControl == null) break;
                _spellControl.GetAction<SpawnObjectFromGlobalPool>(state, 0).Enabled = false;
                _spellControl.InsertMethod(state, () =>
                {
                    GameObject dungCloud = Instantiate(_dungCloud, transform.position - new Vector3(0f, 0f, -0.001f), Quaternion.identity);
                    dungCloud.SetActive(true);

                    // Ok this is really dumb but for some reason trying to just do it in OnEnable doesn't work and trying to use
                    // ModifyAuraColor on it doesn't work
                    GameObject recharge = HeroController.instance.transform.Find("Charm Effects").Find("Dung Recharge").gameObject;
                    ParticleSystem rechargePt = recharge.GetComponent<ParticleSystem>();
                    ParticleSystem.MainModule rechargeMain = rechargePt.main;
                    rechargeMain.startColor = Color.white;
                }, 1);
            }

			On.ExtraDamageable.RecieveExtraDamage += ExtraDamageableRecieveExtraDamage;
			On.ExtraDamageable.GetDamageOfType += ExtraDamageableGetDamageOfType;
			On.SpriteFlash.flashDungQuick += SpriteFlashFlashDungQuick;
			On.KnightHatchling.OnEnable += KnightHatchlingOnEnable;
			On.HutongGames.PlayMaker.Actions.ActivateAllChildren.OnEnter += ActivateAllChildrenOnEnter;
			On.HutongGames.PlayMaker.Actions.Tk2dPlayAnimation.OnEnter += Tk2dPlayAnimationOnEnter;
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

		private void KnightHatchlingOnEnable(On.KnightHatchling.orig_OnEnable orig, KnightHatchling self)
		{
            orig(self);
            Vasi.Mirror.SetField(self, "details", self.normalDetails with { damage = self.dungDetails.damage, dung = true });
            self.dungExplosionPrefab = _dungCloud;
            ParticleSystem.MainModule main = self.dungPt.main;
            main.startColor = PaleColor;
		}

        private void ActivateAllChildrenOnEnter(On.HutongGames.PlayMaker.Actions.ActivateAllChildren.orig_OnEnter orig, ActivateAllChildren self)
		{
            // Needed for the sound effect and impact lines
			if(self.Fsm.GameObjectName.Contains("Dung Explosion") && self.State.Name == "Explode")
			{
                GameObject dungTrail = Instantiate(_dungCloud, self.Fsm.GameObject.transform.position, Quaternion.identity);
                dungTrail.SetActive(true);
                foreach(Component comp in self.Fsm.GameObject.GetComponentsInChildren<Component>(true))
				{
                    if(!comp.gameObject.name.Contains("Impact"))
                    {
                        Destroy(comp.gameObject);
                    }
                    else
                    {
                        comp.gameObject.GetComponent<tk2dSprite>().color = Color.white;
                    }
				}
            }
            orig(self);
		}
		
        private void Tk2dPlayAnimationOnEnter(On.HutongGames.PlayMaker.Actions.Tk2dPlayAnimation.orig_OnEnter orig, Tk2dPlayAnimation self)
		{
            if(self.Fsm.GameObjectName.Contains("Spell Fluke Dung") && self.State.Name == "Init" && self.clipName.Value == "Dung Air")
            {
                PlayMakerFSM flukeFSM = self.Fsm.FsmComponent;
                GameObject fluke = flukeFSM.gameObject;
                fluke.GetComponent<tk2dSprite>().color = Color.white;

                GameObject flukePt = flukeFSM.GetFsmGameObjectVariable("Pt Antic").Value;
                ParticleSystem.MainModule main = flukePt.GetComponent<ParticleSystem>().main;
                main.startColor = PaleColor;

                GameObject flukeCloud = flukeFSM.GetFsmGameObjectVariable("Dung Cloud").Value;
                foreach(ParticleSystem pt in flukeCloud.GetComponentsInChildren<ParticleSystem>(true))
                {
                    ParticleSystem.MainModule cloudMain = pt.main;
                    cloudMain.startColor = PaleColor;
                }
                foreach(tk2dSprite sprite in flukeCloud.GetComponentsInChildren<tk2dSprite>(true))
                {
                    sprite.color = Color.white;
                }
            }
            orig(self);
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
            Destroy(_dungCloud);

            _dungControl.GetAction<Wait>("Emit Pause", 2).time.Value = 0.5f;
			_dungControl.GetAction<SpawnObjectFromGlobalPoolOverTime>("Equipped", 0).Enabled = true;

            foreach(string state in new[] { "Dung Cloud", "Dung Cloud 2" })
            {
                if(_spellControl == null) break;
                _spellControl.RemoveAction(state, 1);
                _spellControl.GetAction<SpawnObjectFromGlobalPool>(state, 0).Enabled = true;
            }

            On.ExtraDamageable.RecieveExtraDamage -= ExtraDamageableRecieveExtraDamage;
            On.ExtraDamageable.GetDamageOfType -= ExtraDamageableGetDamageOfType;
            On.SpriteFlash.flashDungQuick -= SpriteFlashFlashDungQuick;
            On.KnightHatchling.OnEnable -= KnightHatchlingOnEnable;
            On.HutongGames.PlayMaker.Actions.ActivateAllChildren.OnEnter -= ActivateAllChildrenOnEnter;
            On.HutongGames.PlayMaker.Actions.Tk2dPlayAnimation.OnEnter -= Tk2dPlayAnimationOnEnter;
        }

        private void Log(object message) => Modding.Logger.Log("[FiveKnights][Royal Aura] " + message);
    }
}
