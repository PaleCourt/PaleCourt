using System;
using System.Collections.Generic;
using UnityEngine;

namespace FiveKnights
{
    public class ModifyBloomProps : MonoBehaviour
    {
        private const float ATTACK_COOLDOWN_TIME = 0.41f;
        private const float ATTACK_COOLDOWN_TIME_CH = 0.25f;
        private const float ATTACK_DURATION = 0.36f;
        private const float ATTACK_DURATION_CH = 0.25f;
        private const float DASH_COOLDOWN = 0.6f;
        private const float DASH_COOLDOWN_CH = 0.4f;
        private const float DASH_SPEED = 20.0f;
        private const float DASH_SPEED_SHARP = 28.0f;
        private const float RUN_SPEED = 8.3f;
        private const float RUN_SPEED_CH = 10.0f;
        private const float RUN_SPEED_CH_COMBO = 11.4f;
        private const float SHADOW_DASH_COOLDOWN = 1.5f;

        private const float MULTIPLIER_L1 = 1.1f;
        private const float MULTIPLIER_L2 = 1.2f;

        private HeroController _hc => HeroController.instance;

        public void ModifyPropsL1()
        {
            //_hc.ATTACK_COOLDOWN_TIME = ATTACK_COOLDOWN_TIME / MULTIPLIER_L1;
            //_hc.ATTACK_COOLDOWN_TIME_CH = ATTACK_COOLDOWN_TIME_CH / MULTIPLIER_L1;
            //_hc.ATTACK_DURATION = ATTACK_DURATION / MULTIPLIER_L1;
            //_hc.ATTACK_DURATION_CH = ATTACK_DURATION_CH / MULTIPLIER_L1;
            _hc.DASH_COOLDOWN = DASH_COOLDOWN / MULTIPLIER_L1;
            _hc.DASH_COOLDOWN_CH = DASH_COOLDOWN_CH / MULTIPLIER_L1;
            //_hc.DASH_SPEED = DASH_SPEED * MULTIPLIER_L1;
            //_hc.DASH_SPEED_SHARP = DASH_SPEED_SHARP * MULTIPLIER_L1;
            _hc.RUN_SPEED = RUN_SPEED * MULTIPLIER_L1;
            _hc.RUN_SPEED_CH = RUN_SPEED_CH * MULTIPLIER_L1;
            _hc.RUN_SPEED_CH_COMBO = RUN_SPEED_CH_COMBO * MULTIPLIER_L1;
            _hc.SHADOW_DASH_COOLDOWN = SHADOW_DASH_COOLDOWN / MULTIPLIER_L1;
            _hc.shadowRechargePrefab.GetComponent<PlayMakerFSM>().FsmVariables.GetFsmFloat("Shadow Recharge Time").Value = SHADOW_DASH_COOLDOWN / MULTIPLIER_L1;
        }

        public void ModifyPropsL2()
        {
            //_hc.ATTACK_COOLDOWN_TIME = ATTACK_COOLDOWN_TIME / MULTIPLIER_L2;
            //_hc.ATTACK_COOLDOWN_TIME_CH = ATTACK_COOLDOWN_TIME_CH /MULTIPLIER_L2;
            //_hc.ATTACK_DURATION = ATTACK_DURATION / MULTIPLIER_L2;
            //_hc.ATTACK_DURATION_CH = ATTACK_DURATION_CH / MULTIPLIER_L2;
            _hc.DASH_COOLDOWN = DASH_COOLDOWN / MULTIPLIER_L2;
            _hc.DASH_COOLDOWN_CH = DASH_COOLDOWN_CH / MULTIPLIER_L2;
            //_hc.DASH_SPEED = DASH_SPEED * MULTIPLIER_L2;
            //_hc.DASH_SPEED_SHARP = DASH_SPEED_SHARP * MULTIPLIER_L2;
            _hc.RUN_SPEED = RUN_SPEED * MULTIPLIER_L2;
            _hc.RUN_SPEED_CH = RUN_SPEED_CH * MULTIPLIER_L2;
            _hc.RUN_SPEED_CH_COMBO = RUN_SPEED_CH_COMBO * MULTIPLIER_L2;
            _hc.SHADOW_DASH_COOLDOWN = SHADOW_DASH_COOLDOWN / MULTIPLIER_L2;
            _hc.shadowRechargePrefab.GetComponent<PlayMakerFSM>().FsmVariables.GetFsmFloat("Shadow Recharge Time").Value = SHADOW_DASH_COOLDOWN / MULTIPLIER_L2 ;
        }

        public void ResetProps()
        {
            //_hc.ATTACK_COOLDOWN_TIME = ATTACK_COOLDOWN_TIME;
            //_hc.ATTACK_COOLDOWN_TIME_CH = ATTACK_COOLDOWN_TIME_CH;
            //_hc.ATTACK_DURATION = ATTACK_DURATION;
            //_hc.ATTACK_DURATION_CH = ATTACK_DURATION_CH;
            _hc.DASH_COOLDOWN = DASH_COOLDOWN;
            _hc.DASH_COOLDOWN_CH = DASH_COOLDOWN_CH;
            //_hc.DASH_SPEED = DASH_SPEED;
            //_hc.DASH_SPEED_SHARP = DASH_SPEED_SHARP;
            _hc.RUN_SPEED = RUN_SPEED;
            _hc.RUN_SPEED_CH = RUN_SPEED_CH;
            _hc.RUN_SPEED_CH_COMBO = RUN_SPEED_CH_COMBO;
            _hc.SHADOW_DASH_COOLDOWN = SHADOW_DASH_COOLDOWN;
            _hc.shadowRechargePrefab.GetComponent<PlayMakerFSM>().FsmVariables.GetFsmFloat("Shadow Recharge Time").Value = SHADOW_DASH_COOLDOWN;
        }
    }
}
