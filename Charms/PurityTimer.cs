using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using Modding.Utils;
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
    public class PurityTimer : MonoBehaviour
    {
        private float timer = 0;
        private bool timerRunning = false;
        private bool audioMax = false;
        private float duration;
        private const float PURITY_DURATION_DEFAULT = 3.4f;
        private const float PURITY_DURATION_26 = 5.2f;
        //private const float PURITY_DURATION_18 = 1.9f;
        // private const float PURITY_DURATION_13 = 2.1f;
        // private const float PURITY_DURATION_18_13 = 2.25f;
        private const float ATTACK_COOLDOWN_DEFAULT = .41f;
        private const float ATTACK_COOLDOWN_DEFAULT_32 = .25f;
        private const float ATTACK_COOLDOWN_44 = .49f;
        private const float ATTACK_COOLDOWN_44_32 = .31f;
        private const float ATTACK_DURATION_DEFAULT = .35f;
        private const float ATTACK_DURATION_DEFAULT_32 = .28f;
        private const float ATTACK_DURATION_44 = .44f;
        private const float ATTACK_DURATION_44_32 = .25f;
        private const float COOLDOWN_CAP_44 = .17f;
        private const float COOLDOWN_CAP_44_32 = .13f;
        private HeroController _hc = HeroController.instance;
        private PlayerData _pd = PlayerData.instance;
        private List<NailSlash> nailSlashes;

        private void OnEnable()
        {
            nailSlashes = new List<NailSlash>()
            {
                HeroController.instance.normalSlash,
                HeroController.instance.alternateSlash,
                HeroController.instance.downSlash,
                HeroController.instance.upSlash,
                HeroController.instance.wallSlash,
            };     
            On.HealthManager.TakeDamage += IncrementSpeed;
            //ModHooks.CharmUpdateHook += SetDuration;
            ModHooks.CharmUpdateHook += Duration;
            //On.NailSlash.SetLongnail += CancelLongnail;
            //On.NailSlash.SetMantis += CancelMantis;
            On.HeroController.CanDoubleJump += FixDoubleJump;
            On.HeroController.CanCast += FixCast;
            ModHooks.AfterTakeDamageHook += ResetSpeed;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += Dummy;

            _hc.ATTACK_COOLDOWN_TIME = ATTACK_COOLDOWN_44;
            _hc.ATTACK_COOLDOWN_TIME_CH = ATTACK_COOLDOWN_44_32;
            _hc.ATTACK_DURATION = ATTACK_DURATION_44;
            _hc.ATTACK_DURATION_CH = ATTACK_DURATION_44_32;
        }



        private void OnDisable()
        {
            On.HealthManager.TakeDamage -= IncrementSpeed;
           // ModHooks.CharmUpdateHook -= SetDuration;
           // On.NailSlash.SetMantis -= CancelMantis;
           // On.NailSlash.SetLongnail -= CancelLongnail;
            On.HeroController.CanDoubleJump -= FixDoubleJump;
            On.HeroController.CanCast -= FixCast;
            ModHooks.AfterTakeDamageHook -= ResetSpeed;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= Dummy;

            _hc.ATTACK_COOLDOWN_TIME = ATTACK_COOLDOWN_DEFAULT;
            _hc.ATTACK_COOLDOWN_TIME_CH = ATTACK_COOLDOWN_DEFAULT_32;
            _hc.ATTACK_DURATION = ATTACK_DURATION_DEFAULT;
            _hc.ATTACK_DURATION_CH = ATTACK_DURATION_DEFAULT_32;
        }

        private void Update()
        {
            if (timerRunning)
            {
                timer += Time.deltaTime;
                if (timer >= duration)
                {
                    timerRunning = false;
                    if (!_pd.equippedCharm_32 && _hc.ATTACK_COOLDOWN_TIME <= .48f || _pd.equippedCharm_32 && _hc.ATTACK_COOLDOWN_TIME_CH <= .3f)
                    {
                        Log("Play Reset Audio");
                        this.PlayAudio(ABManager.AssetBundles[ABManager.Bundle.CharmUnlock].LoadAsset<AudioClip>("purity_reset"), 1f);
                    }
                    _hc.ATTACK_COOLDOWN_TIME = ATTACK_COOLDOWN_44;
                    _hc.ATTACK_COOLDOWN_TIME_CH = ATTACK_COOLDOWN_44_32;
                    _hc.ATTACK_DURATION = ATTACK_DURATION_44;
                    _hc.ATTACK_DURATION_CH = ATTACK_DURATION_44_32;              
                    foreach (NailSlash nailslash in nailSlashes)
                    {
                        if (nailslash.GetComponent<tk2dSprite>().color != Color.black)
                        {
                            nailslash.GetComponent<tk2dSprite>().color = Color.white;
                            nailslash.GetComponent<AudioSource>().pitch = 1;
                        }
                    }
                    audioMax = false;
                    timer = 0;
                }
            }
        }
        private int ResetSpeed(int hazardType, int damageAmount)
        {
            timerRunning = false;
            if (!_pd.equippedCharm_32 && _hc.ATTACK_COOLDOWN_TIME <= .48f || _pd.equippedCharm_32 && _hc.ATTACK_COOLDOWN_TIME_CH <= .3f)
            {
                Log("Play Reset Audio");
                this.PlayAudio(ABManager.AssetBundles[ABManager.Bundle.CharmUnlock].LoadAsset<AudioClip>("purity_reset"), 1f);
            }
            _hc.ATTACK_COOLDOWN_TIME = ATTACK_COOLDOWN_44;
            _hc.ATTACK_COOLDOWN_TIME_CH = ATTACK_COOLDOWN_44_32;
            _hc.ATTACK_DURATION = ATTACK_DURATION_44;
            _hc.ATTACK_DURATION_CH = ATTACK_DURATION_44_32;       
            foreach (NailSlash nailslash in nailSlashes)
            {
                if(nailslash.GetComponent<tk2dSprite>().color != Color.black) {
                    nailslash.GetComponent<tk2dSprite>().color = Color.white;
                    nailslash.GetComponent<AudioSource>().pitch = 1;
                }
                
            }
            audioMax = false;
            timer = 0;
            return damageAmount;
        }
        
        private void IncrementSpeed(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (hitInstance.AttackType == AttackTypes.Nail || hitInstance.AttackType == AttackTypes.NailBeam)
            {
                float damageDealt = hitInstance.DamageDealt;
                damageDealt *= .8f;
                hitInstance.DamageDealt = Mathf.RoundToInt(damageDealt);
            }
            orig(self, hitInstance);
            if (_hc.ATTACK_COOLDOWN_TIME_CH <= .17f)
            {
                ReflectionHelper.SetField<HealthManager, float>(self, "evasionByHitRemaining", 0.1f);
            }            
            if (hitInstance.AttackType == AttackTypes.Nail || hitInstance.AttackType == AttackTypes.NailBeam)
            {
               
                timer = 0;
                timerRunning = true;
                if (hitInstance.AttackType == AttackTypes.Nail)
                {

                    _hc.ATTACK_COOLDOWN_TIME -= (ATTACK_COOLDOWN_44 - COOLDOWN_CAP_44) / 9;
                    _hc.ATTACK_COOLDOWN_TIME_CH -= (ATTACK_COOLDOWN_44_32 - COOLDOWN_CAP_44_32) / 11;
                    _hc.ATTACK_DURATION -= (ATTACK_COOLDOWN_44 - COOLDOWN_CAP_44) / 9;
                    _hc.ATTACK_DURATION_CH -= (ATTACK_COOLDOWN_44_32 - COOLDOWN_CAP_44_32) / 11;

                }

                if (!_pd.equippedCharm_32 && _hc.ATTACK_COOLDOWN_TIME <= .18f && !audioMax || _pd.equippedCharm_32 && _hc.ATTACK_COOLDOWN_TIME_CH <= .14f && !audioMax)     
                {
                    audioMax = true;
                    Log("Play Audio");
                    this.PlayAudio(ABManager.AssetBundles[ABManager.Bundle.CharmUnlock].LoadAsset<AudioClip>("purity_max"), .5f);
                }

                if (_hc.ATTACK_COOLDOWN_TIME <= COOLDOWN_CAP_44) { _hc.ATTACK_COOLDOWN_TIME = COOLDOWN_CAP_44; }
                if (_hc.ATTACK_COOLDOWN_TIME_CH <= COOLDOWN_CAP_44_32) { _hc.ATTACK_COOLDOWN_TIME_CH = COOLDOWN_CAP_44_32; }
                if (_hc.ATTACK_DURATION <= COOLDOWN_CAP_44) { _hc.ATTACK_DURATION = COOLDOWN_CAP_44; }
                if (_hc.ATTACK_DURATION_CH <= COOLDOWN_CAP_44_32) { _hc.ATTACK_DURATION_CH = COOLDOWN_CAP_44_32; }

                if (!_pd.equippedCharm_32 && _hc.ATTACK_COOLDOWN_TIME <= COOLDOWN_CAP_44 || _pd.equippedCharm_32 && _hc.ATTACK_COOLDOWN_TIME_CH == COOLDOWN_CAP_44_32)
                {
                    if (_pd.equippedCharm_6 && _pd.health == 1) { }
                    else
                    {
                        foreach (NailSlash nailslash in nailSlashes)
                        {
                            if (nailslash.GetComponent<tk2dSprite>().color != Color.black) { nailslash.GetComponent<tk2dSprite>().color = new Color(.619f, .798f, .881f); }
                        }
                    }
                }

            }
        }
        private void Duration(PlayerData data, HeroController controller)
        {
            if (data.equippedCharm_26) { duration = PURITY_DURATION_26; }
            else { duration = PURITY_DURATION_DEFAULT; }
        }

        /*private void SetDuration(PlayerData data, HeroController controller)
        {
            bool ln = data.equippedCharm_18;
            bool mop = data.equippedCharm_13;

            if (mop && ln) duration = PURITY_DURATION_18_13;
            else if (mop && !ln) duration = PURITY_DURATION_13;
            else if (ln && !mop) duration = PURITY_DURATION_18;
            else if (!ln && !mop) duration = PURITY_DURATION_DEFAULT;


            foreach (NailSlash nailSlash in nailSlashes)
            {
                if (nailSlash == null) break;
                switch (nailSlash.name)
                {
                    case "Slash":
                        nailSlash.SetLongnail(false);
                        nailSlash.SetMantis(false);
                        nailSlash.scale = new Vector3(1.62f, 1.645244f, 1.277858f);
                        break;
                    case "AltSlash":
                        nailSlash.SetLongnail(false);
                        nailSlash.SetMantis(false);
                        nailSlash.scale = new Vector3(1.257f, 1.4224f, 1.12369f);
                        break;
                    case "DownSlash":
                        nailSlash.SetLongnail(false);
                        nailSlash.SetMantis(false);
                        nailSlash.scale = new Vector3(1.125f, 1.28f, 1f);
                        break;
                    case "UpSlash":
                        nailSlash.SetLongnail(false);
                        nailSlash.SetMantis(false);
                        nailSlash.scale = new Vector3(1.15f, 1.4f, 1.2455f);
                        break;
                        nailSlash.SetLongnail(false);
                        nailSlash.SetMantis(false);
                    case "WallSlash":
                        nailSlash.SetLongnail(false);
                        nailSlash.SetMantis(false);
                        nailSlash.scale = new Vector3(-1.62f, 1.6452f, 1f);
                        break;
                }

            }
            
        }*/
        private void Dummy(Scene From, Scene To)
        {
            if (To.name == "Deepnest_East_16")
            {
                var dummy = To.FindGameObject("Training Dummy");
                dummy.LocateMyFSM("Hit").GetState("Light Dir").InsertMethod(() => IncrementSpeedDummy(), 0);
            }
        }
        private void IncrementSpeedDummy()
        {
            timer = 0;
            timerRunning = true;

            _hc.ATTACK_COOLDOWN_TIME -= (ATTACK_COOLDOWN_44 - COOLDOWN_CAP_44) / 9;
            _hc.ATTACK_COOLDOWN_TIME_CH -= (ATTACK_COOLDOWN_44_32 - COOLDOWN_CAP_44_32) / 11;
            _hc.ATTACK_DURATION -= (ATTACK_COOLDOWN_44 - COOLDOWN_CAP_44) / 9;
            _hc.ATTACK_DURATION_CH -= (ATTACK_COOLDOWN_44_32 - COOLDOWN_CAP_44_32) / 11;
            //foreach(NailSlash nailslash in nailSlashes)
            //{
            //	nailslash.GetComponent<AudioSource>().pitch += _pd.equippedCharm_32 ? .08f : .04f;
            //	if(nailslash.GetComponent<AudioSource>().pitch >= 1.2f) nailslash.GetComponent<AudioSource>().pitch = 1.4f;
            //}

            if (!_pd.equippedCharm_32 && _hc.ATTACK_COOLDOWN_TIME <= .18f && !audioMax || _pd.equippedCharm_32 && _hc.ATTACK_COOLDOWN_TIME_CH <= .14f && !audioMax)
            {
                audioMax = true;
                Log("Play Audio");
                this.PlayAudio(ABManager.AssetBundles[ABManager.Bundle.CharmUnlock].LoadAsset<AudioClip>("purity_max"), .5f);
            }

            if (_hc.ATTACK_COOLDOWN_TIME <= COOLDOWN_CAP_44) { _hc.ATTACK_COOLDOWN_TIME = COOLDOWN_CAP_44; }
            if (_hc.ATTACK_COOLDOWN_TIME_CH <= COOLDOWN_CAP_44_32) { _hc.ATTACK_COOLDOWN_TIME_CH = COOLDOWN_CAP_44_32; }
            if (_hc.ATTACK_DURATION <= COOLDOWN_CAP_44) { _hc.ATTACK_DURATION = COOLDOWN_CAP_44; }
            if (_hc.ATTACK_DURATION_CH <= COOLDOWN_CAP_44_32) { _hc.ATTACK_DURATION_CH = COOLDOWN_CAP_44_32; }

            if (!_pd.equippedCharm_32 && _hc.ATTACK_COOLDOWN_TIME <= COOLDOWN_CAP_44 || _pd.equippedCharm_32 && _hc.ATTACK_COOLDOWN_TIME_CH == COOLDOWN_CAP_44_32)
            {
                if (_pd.equippedCharm_6 && _pd.health == 1) { }
                else
                {
                    foreach (NailSlash nailslash in nailSlashes)
                    {
                        if (nailslash.GetComponent<tk2dSprite>().color != Color.black) { nailslash.GetComponent<tk2dSprite>().color = new Color(.619f, .798f, .881f); }
                    }
                }
            }


        }
        private bool FixCast(On.HeroController.orig_CanCast orig, HeroController self)
        {
            return !GameManager.instance.isPaused && !self.cState.dashing && self.hero_state != ActorStates.no_input && !self.cState.backDashing &&
                /*(!self.cState.attacking || ReflectionHelper.GetField<HeroController, float>(self, "attack_time") >= self.ATTACK_RECOVERY_TIME) &&*/ !self.cState.recoiling &&
                !self.cState.recoilFrozen && !self.cState.transitioning && !self.cState.hazardDeath && !self.cState.hazardRespawning && self.CanInput() &&
                ReflectionHelper.GetField<HeroController, float>(self, "preventCastByDialogueEndTimer") <= 0f;
        }

        private bool FixDoubleJump(On.HeroController.orig_CanDoubleJump orig, HeroController self)
        {
            return self.playerData.GetBool("hasDoubleJump") && !self.controlReqlinquished && !ReflectionHelper.GetField<HeroController, bool>(self, "doubleJumped") && !self.inAcid && self.hero_state != ActorStates.no_input 
                && self.hero_state != ActorStates.hard_landing && self.hero_state != ActorStates.dash_landing && !self.cState.dashing && !self.cState.wallSliding &&
                !self.cState.backDashing && /*!self.cState.attacking &&*/ !self.cState.bouncing && !self.cState.shroomBouncing && !self.cState.onGround;
        }
        private void CancelMantis(On.NailSlash.orig_SetMantis orig, NailSlash self, bool set) { orig(self, false); }
        private void CancelLongnail(On.NailSlash.orig_SetLongnail orig, NailSlash self, bool set) { orig(self, false); }


        private void Log(object message) => Modding.Logger.Log("[FiveKnights][Purity Timer] " + message);
    }
    
}
