using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using InControl;
using Modding;
using SFCore.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vasi;
using Random = UnityEngine.Random;

namespace FiveKnights
{
    public class AutoSwing : MonoBehaviour
    {
        private bool first = false;
        private bool autoSwing = false;
        private AttackDirection attackDir = AttackDirection.normal;
        private HeroController _hc = HeroController.instance;

        private void OnEnable()
        {
            On.HeroController.CanNailCharge += CancelNailArts;
        }

        private bool CancelNailArts(On.HeroController.orig_CanNailCharge orig, HeroController self)
        {
            if (PlayerData.instance.equippedCharm_32 && _hc.ATTACK_COOLDOWN_TIME_CH <= .17f || !PlayerData.instance.equippedCharm_32 && _hc.ATTACK_COOLDOWN_TIME <= .21f)
            {
                return false;
            }
            else
            {
                return orig(self);
            }
        }

        private void OnDisable()
        {
            On.HeroController.CanNailCharge -= CancelNailArts;
        }
        private void Update()
        {
            if (HeroController.instance.vertical_input > Mathf.Epsilon) { attackDir = AttackDirection.upward; }
            else if (HeroController.instance.vertical_input < 0f - Mathf.Epsilon)
            {
                if (HeroController.instance.hero_state != ActorStates.idle && HeroController.instance.hero_state != ActorStates.running) { attackDir = AttackDirection.downward; }
                else { attackDir = AttackDirection.normal; }
            }
            else { attackDir = AttackDirection.normal; }

            if (InputHandler.Instance.inputActions.attack.IsPressed)
            {
                if (PlayerData.instance.equippedCharm_32 && _hc.ATTACK_COOLDOWN_TIME_CH <= .13f ||
                    !PlayerData.instance.equippedCharm_32 && _hc.ATTACK_COOLDOWN_TIME <= .17f)
                {
                    if (ReflectionHelper.CallMethod<HeroController, bool>(HeroController.instance, "CanAttack"))
                    {
                        HeroController.instance.Attack(attackDir);
                    }
                }
            }

        }
        
        

    }
}
