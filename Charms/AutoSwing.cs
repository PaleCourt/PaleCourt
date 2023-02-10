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

        private void Update()
        {
            if (InputHandler.Instance.inputActions.attack.WasReleased)
            {
                if (!autoSwing)
                {
                    autoSwing = true;
                }
                else autoSwing = false;
            }

            if (HeroController.instance.vertical_input > Mathf.Epsilon) { attackDir = AttackDirection.upward; }
            else if (HeroController.instance.vertical_input < 0f - Mathf.Epsilon)
            {
                if (HeroController.instance.hero_state != ActorStates.idle && HeroController.instance.hero_state != ActorStates.running) { attackDir = AttackDirection.downward; }
                else { attackDir = AttackDirection.normal; }
            }
            else { attackDir = AttackDirection.normal; }


            if (autoSwing)
            {
                if (ReflectionHelper.CallMethod<HeroController, bool>(HeroController.instance, "CanAttack"))
                {
                    HeroController.instance.Attack(attackDir);
                }
            }
        }
        
        

    }
}
