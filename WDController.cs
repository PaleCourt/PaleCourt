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
        public GameObject dd;
        public static GameObject _mus;
        private tk2dSpriteAnimator _tk;
        public static bool alone;
        private bool HIT_FLAG;

        private IEnumerator Start()
        {
            _hm = dd.GetComponent<HealthManager>();
            _fsm = dd.LocateMyFSM("Dung Defender");
            _tk = dd.GetComponent<tk2dSpriteAnimator>();
            _mus = new GameObject("IsmaMusHolder");
            FiveKnights.preloadedGO["WD"] = dd;
            alone = true;


            On.HealthManager.TakeDamage += HealthManager_TakeDamage;
            On.MusicCue.GetChannelInfo += MusicCue_GetChannelInfo;
            PlayerData.instance.dreamReturnScene = "White_Palace_09";

            //Be sure to do CustomWP.Instance.wonLastFight = true; on win
            if (CustomWP.boss == CustomWP.Boss.Isma)
            {
                yield return null;
                dd.SetActive(false);
                FightController.Instance.CreateIsma();
                IsmaController ic = FiveKnights.preloadedGO["Isma2"].GetComponent<IsmaController>();
                ic.onlyIsma = true;
                yield return new WaitWhile(() => ic != null);
                //var endCtrl = GameObject.Find("Boss Scene Controller").LocateMyFSM("Dream Return");
                //endCtrl.SendEvent("DREAM RETURN");
                var bossSceneController = GameObject.Find("Boss Scene Controller");
                var bsc = bossSceneController.GetComponent<BossSceneController>();
                GameObject transition = Instantiate(bsc.transitionPrefab);
                PlayMakerFSM transitionsFSM = transition.LocateMyFSM("Transitions");
                transitionsFSM.SetState("Out Statue");
                yield return new WaitForSeconds(1.0f);
                bsc.DoDreamReturn();
                Destroy(this);
            }
            else if (CustomWP.boss == CustomWP.Boss.Ogrim)
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

                PlayMakerFSM pm = GameCameras.instance.tk2dCam.gameObject.LocateMyFSM("CameraFade");
                pm.SendEvent("FADE OUT INSTANT");
                PlayMakerFSM fsm2 = GameObject.Find("Blanker White").LocateMyFSM("Blanker Control");
                fsm2.FsmVariables.FindFsmFloat("Fade Time").Value = 0;
                fsm2.SendEvent("FADE IN");
                yield return null;
                HeroController.instance.MaxHealth();
                yield return null;
                GameCameras.instance.cameraFadeFSM.FsmVariables.FindFsmBool("No Fade").Value = true;
                yield return null;
                GameManager.instance.BeginSceneTransition(new GameManager.SceneLoadInfo
                {
                    SceneName = "White_Palace_09",
                    EntryGateName = "door_dreamReturnGGstatueStateIsma_GG_Statue_ElderHu(Clone)(Clone)",
                    Visualization = GameManager.SceneLoadVisualizations.GodsAndGlory,
                    WaitForSceneTransitionCameraFade = false,
                    PreventCameraFadeOut = true,
                    EntryDelay = 0

                });

                Destroy(this);
            }
            else if (CustomWP.boss == CustomWP.Boss.Dryya)
            {
                dd.SetActive(false);
                DryyaSetup dc = FightController.Instance.CreateDryya();

                yield return new WaitWhile(() => dc != null);

                yield return new WaitForSeconds(5.0f);
                var bossSceneController = GameObject.Find("Boss Scene Controller");
                var bsc = bossSceneController.GetComponent<BossSceneController>();
                GameObject transition = Instantiate(bsc.transitionPrefab);
                PlayMakerFSM transitionsFSM = transition.LocateMyFSM("Transitions");
                transitionsFSM.SetState("Out Statue");
                yield return new WaitForSeconds(1.0f);
                bsc.DoDreamReturn();
                Destroy(this);
            }
            else if (CustomWP.boss == CustomWP.Boss.Hegemol)
            {
                yield return null;
                dd.SetActive(false);
                HegemolController hegemolCtrl = FightController.Instance.CreateHegemol();
                GameObject.Find("Burrow Effect").SetActive(false);
                GameCameras.instance.cameraShakeFSM.FsmVariables.FindFsmBool("RumblingMed").Value = false;
                yield return new WaitWhile(() => hegemolCtrl != null);
                var bossSceneController = GameObject.Find("Boss Scene Controller");
                var bsc = bossSceneController.GetComponent<BossSceneController>();
                GameObject transition = Instantiate(bsc.transitionPrefab);
                PlayMakerFSM transitionsFSM = transition.LocateMyFSM("Transitions");
                transitionsFSM.SetState("Out Statue");
                yield return new WaitForSeconds(1.0f);
                bsc.DoDreamReturn();
                Destroy(this);
            }
            else if (CustomWP.boss == CustomWP.Boss.Zemer)
            {
                yield return null;
                dd.SetActive(false);
                ZemerController zc = FightController.Instance.CreateZemer();
                yield return new WaitWhile(() => zc != null);
                var bossSceneController = GameObject.Find("Boss Scene Controller");
                var bsc = bossSceneController.GetComponent<BossSceneController>();
                GameObject transition = Instantiate(bsc.transitionPrefab);
                PlayMakerFSM transitionsFSM = transition.LocateMyFSM("Transitions");
                transitionsFSM.SetState("Out Statue");
                yield return new WaitForSeconds(1.0f);
                bsc.DoDreamReturn();

                Destroy(this);
            }
            else if (CustomWP.boss == CustomWP.Boss.All)
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