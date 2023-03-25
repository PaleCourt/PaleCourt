using HutongGames.PlayMaker.Actions;
using UnityEngine;
using GlobalEnums;

namespace FiveKnights
{
    public class ShadeSlash : MonoBehaviour
    {
		public AttackDirection attackDirection;

		private HeroController _hc => HeroController.instance;

        private void OnTriggerEnter2D(Collider2D collider)
        {
			// Most of this is from NailSlash.OnTriggerEnter2D, purpose is to add recoil and pogoing
			if(collider != null)
			{
				switch(attackDirection)
				{
					case AttackDirection.normal:
						if(!_hc.cState.facingRight)
						{
							if(collider.gameObject.layer == 11 && (collider.gameObject.GetComponent<NonBouncer>() == null || 
								!collider.gameObject.GetComponent<NonBouncer>().active))
							{
								if(collider.gameObject.GetComponent<BounceShroom>() != null)
								{
									_hc.RecoilRightLong();
									Bounce(collider, false);
								}
								else
								{
									_hc.RecoilRight();
								}
							}
							if(collider.gameObject.layer == 19 && collider.gameObject.GetComponent<BounceShroom>() != null)
							{
								_hc.RecoilRightLong();
								Bounce(collider, false);
								return;
							}
						}
						else
						{
							if(collider.gameObject.layer == 11 && (collider.gameObject.GetComponent<NonBouncer>() == null ||
								!collider.gameObject.GetComponent<NonBouncer>().active))
							{
								if(collider.gameObject.GetComponent<BounceShroom>() != null)
								{
									_hc.RecoilLeftLong();
									Bounce(collider, false);
								}
								else
								{
									_hc.RecoilLeft();
								}
							}
							if(collider.gameObject.layer == 19 && collider.gameObject.GetComponent<BounceShroom>() != null)
							{
								_hc.RecoilLeftLong();
								Bounce(collider, false);
								return;
							}
						}
						break;
					case AttackDirection.upward:
						if(collider.gameObject.layer == 11 && (collider.gameObject.GetComponent<NonBouncer>() == null ||
							!collider.gameObject.GetComponent<NonBouncer>().active))
						{
							if(collider.gameObject.GetComponent<BounceShroom>() != null)
							{
								_hc.RecoilDown();
								Bounce(collider, false);
							}
							else
							{
								_hc.RecoilDown();
							}
						}
						if(collider.gameObject.layer == 19 && collider.gameObject.GetComponent<BounceShroom>() != null)
						{
							_hc.RecoilDown();
							Bounce(collider, false);
							return;
						}
						break;
					case AttackDirection.downward:
						PhysLayers layer = (PhysLayers)collider.gameObject.layer;
						if((layer == PhysLayers.ENEMIES || layer == PhysLayers.INTERACTIVE_OBJECT || layer == PhysLayers.HERO_ATTACK) &&
							(collider.gameObject.GetComponent<NonBouncer>() == null || !collider.gameObject.GetComponent<NonBouncer>().active))
						{
							if(collider.gameObject.GetComponent<BigBouncer>() != null)
							{
								_hc.BounceHigh();
								return;
							}
							if(collider.gameObject.GetComponent<BounceShroom>() != null)
							{
								_hc.ShroomBounce();
								Bounce(collider, true);
								return;
							}
							_hc.Bounce();
						}
						break;
				}
			}
		}

		private void Bounce(Collider2D otherCollider, bool useEffects)
		{
			PlayMakerFSM playMakerFSM = FSMUtility.LocateFSM(otherCollider.gameObject, "Bounce Shroom");
			if(playMakerFSM)
			{
				playMakerFSM.SendEvent("BOUNCE UPWARD");
				return;
			}
			BounceShroom component = otherCollider.GetComponent<BounceShroom>();
			if(component)
			{
				component.BounceLarge(useEffects);
			}
		}

		private void Log(object o) => Modding.Logger.Log("[Shade Slash] " + o);
	}
}
