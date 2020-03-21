using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Audio;
using Modding;
using System.Collections;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using HutongGames.Utility;
using ModCommon.Util;
using ModCommon;
using Object = UnityEngine.Object;

namespace FiveKnights
{
    public class WDController : MonoBehaviour
    {
        private HealthManager _hm;
        private PlayMakerFSM _fsm;
        public static GameObject _mus;
        private tk2dSpriteAnimator _tk;
        public static bool alone;
        private bool HIT_FLAG;

        private void Awake()
        {
            _hm = gameObject.GetComponent<HealthManager>();
            _fsm = gameObject.LocateMyFSM("Dung Defender");
            _tk = gameObject.GetComponent<tk2dSpriteAnimator>();
            _mus = new GameObject("IsmaMusHolder");
            FiveKnights.preloadedGO["WD"] = gameObject;
            alone = true;
        }

        private IEnumerator Start()
        {
            On.HealthManager.TakeDamage += HealthManager_TakeDamage;
            On.MusicCue.GetChannelInfo += MusicCue_GetChannelInfo;
            PlayerData.instance.dreamReturnScene = "White_Palace_09";
            Log("COUNT " + CustomWP.boss);
            if (CustomWP.boss == CustomWP.Boss.ISMA)
            {
                yield return null;
                gameObject.SetActive(false);
                PlayerData.instance.isInvincible = true;
                HeroController.instance.RelinquishControl();
                GameManager.instance.playerData.disablePause = true;
                FightController.Instance.CreateIsma();
                IsmaController ic = FiveKnights.preloadedGO["Isma2"].GetComponent<IsmaController>();
                ic.onlyIsma = true;
                yield return new WaitWhile(() => !ic.introDone);
                PlayerData.instance.isInvincible = false;
                GameManager.instance.playerData.disablePause = false;
                yield return new WaitWhile(() => ic != null);
                Destroy(this);
            }
            else if (CustomWP.boss == CustomWP.Boss.DRYYA)
            {
                yield return null;
                gameObject.SetActive(false);
                FightController.Instance.CreateDryya();
            }
            else if (CustomWP.boss == CustomWP.Boss.ZEMER)
            {
                yield return null;
                gameObject.SetActive(false);
                FightController.Instance.CreateZemer();
                ZemerController ic = FiveKnights.preloadedGO["Zemer"].GetComponent<ZemerController>();
            }
            else if (CustomWP.boss == CustomWP.Boss.ALL)
            {
                alone = false;
                _hm.hp = 950;
                _fsm.GetAction<Wait>("Rage Roar", 9).time = 1.5f;
                _fsm.FsmVariables.FindFsmBool("Raged").Value = true;
                yield return new WaitForSeconds(1f);
                GameCameras.instance.cameraFadeFSM.Fsm.SetState("FadeIn");
                yield return new WaitWhile(() => _hm.hp > 600);
                HIT_FLAG = false;
                yield return new WaitWhile(() => !HIT_FLAG);
                PlayerData.instance.isInvincible = true;
                HeroController.instance.RelinquishControl();
                GameManager.instance.playerData.disablePause = true;
                _fsm.SetState("Stun Set");
                yield return new WaitWhile(() => _fsm.ActiveStateName != "Stun Land");
                _fsm.enabled = false;
                FightController.Instance.CreateIsma();
                IsmaController ic = FiveKnights.preloadedGO["Isma2"].GetComponent<IsmaController>();
                yield return new WaitWhile(() => !ic.introDone);
                _fsm.enabled = true;
                _fsm.SetState("Stun Recover");
                yield return null;
                yield return new WaitWhile(() => _fsm.ActiveStateName == "Stun Recover");
                _mus.SetActive(true);
                _mus.GetComponent<AudioSource>().volume = 1f;
                _fsm.SetState("Rage Roar");
                PlayerData.instance.isInvincible = false;
                GameManager.instance.playerData.disablePause = false;
                yield return new WaitWhile(() => ic != null);
                Destroy(this);
            }
            /*
             GameObject dryyaSilhouette = GameObject.Find("Silhouette Dryya");
                dryyaSilhouette.GetComponent<SpriteRenderer>().sprite = ArenaFinder.sprites["Dryya_Silhouette_1"];
                yield return new WaitForSeconds(0.125f);
                dryyaSilhouette.GetComponent<SpriteRenderer>().sprite = ArenaFinder.sprites["Dryya_Silhouette_2"];
                yield return new WaitForSeconds(0.125f);
                Destroy(dryyaSilhouette);
                yield return new WaitForSeconds(0.5f);
                FightController.Instance.CreateDryya();
                FightController.Instance.CreateIsma();
                */
        }

        private void HealthManager_TakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.name.Contains("White Defender"))
            {
                HIT_FLAG = true;
            }
            orig(self, hitInstance);
        }

        private void OnCollisionEnter2D(Collision2D c)
        {
            if (!_tk.IsPlaying("Roll")) return;
            if (c.gameObject.layer == 8 && c.gameObject.name.Contains("Front"))
            {
                Log("HIT WALL");
                _fsm.SetState("RJ Wall");
            }
        }

        private MusicCue.MusicChannelInfo MusicCue_GetChannelInfo(On.MusicCue.orig_GetChannelInfo orig, MusicCue self, MusicChannels channel)
        {
            if (_mus.GetComponent<AudioController>() == null && self.name.Contains("Defender"))
            {
                _mus.SetActive(true);
                _mus.transform.position = new Vector2(75f, 15f);
                AudioSource mus = _mus.AddComponent<AudioSource>();
                mus.clip = ArenaFinder.clips["IsmaMusic"];
                mus.loop = true;
                mus.bypassReverbZones = mus.bypassEffects = true;
                mus.volume = 0f;
                mus.Play();
                _mus.AddComponent<AudioController>();
            }
            return orig(self, channel);
        }

        private void OnDestroy()
        {
            On.HealthManager.TakeDamage -= HealthManager_TakeDamage;
            On.MusicCue.GetChannelInfo -= MusicCue_GetChannelInfo;
        }

        private void Log(object o)
        {
            Modding.Logger.Log("[White Defender] " + o);
        }
    }
}