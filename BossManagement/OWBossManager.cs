using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FiveKnights.BossManagement;
using FiveKnights.Dryya;
using FiveKnights.Hegemol;
using FiveKnights.Isma;
using FiveKnights.Zemer;
using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
//using SFCore.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using Vasi;

namespace FiveKnights
{
    public class OWBossManager : MonoBehaviour
    {
        private HealthManager _hm;
        private PlayMakerFSM _fsm;
        private GameObject _dd;
        private tk2dSpriteAnimator _tk;
        public MusicPlayer _ap;
        public MusicPlayer _ap2;
        public static OWBossManager Instance;
        
        private IEnumerator Start()
        {
            Instance = this;
            var oldDung = GameObject.Find("White Defender");
            if(oldDung != null)
            {
                Destroy(oldDung);
            }

            _dd = Instantiate(FiveKnights.preloadedGO["WhiteDef"]);
            FiveKnights.preloadedGO["WD"] = _dd;
            _dd.SetActive(false);
            _hm = _dd.GetComponent<HealthManager>();
            _fsm = _dd.LocateMyFSM("Dung Defender");
            _tk = _dd.GetComponent<tk2dSpriteAnimator>();
            OnDestroy();
            Log("Curr Boss " + CustomWP.boss);
            
            if (CustomWP.boss == CustomWP.Boss.Isma)
            {
                GameCameras.instance.cameraShakeFSM.FsmVariables.FindFsmBool("RumblingMed").Value = false;
                GameObject ogrim = GameObject.Find("Ogrim");
                yield return new WaitWhile(() => HeroController.instance == null);
                yield return new WaitWhile(()=> HeroController.instance.transform.position.x < 110.5f);
                IsmaController ic = BossLoader.CreateIsma(true);
                ogrim.AddComponent<OgrimBG>().target = ic.transform;
                ic.onlyIsma = true;
                ic.gameObject.SetActive(true);
                yield return new WaitWhile(() => ic != null);
                PlayMusic(null);

                yield return new WaitForSeconds(1.0f);
                WinRoutine(OWArenaFinder.PrevIsmScene, 3);
                
                Log("Done with Isma boss");
                Destroy(this);
            }
            else if (CustomWP.boss == CustomWP.Boss.Dryya)
            {
                yield return new WaitWhile(() => HeroController.instance == null);
                DryyaSetup dc = BossLoader.CreateDryya();
                dc.gameObject.SetActive(false);
                PlayMusic(FiveKnights.Clips["DryyaAreaMusic"]);
                HeroController hc = HeroController.instance;
                yield return new WaitUntil(() => hc.transform.position.x > 427.5f && hc.transform.position.y < 120f);
                PlayMusic(null);
                dc.gameObject.SetActive(true);
                yield return new WaitWhile(() => dc != null);
                PlayMusic(null);
                
                yield return new WaitForSeconds(1.0f);
                WinRoutine(OWArenaFinder.PrevDryScene, 0);
                Log("Done with Dryya boss");
                Destroy(this);
            }
            else if (CustomWP.boss == CustomWP.Boss.Hegemol)
            {
                var water = GameObject.Find("waterfall");
                foreach (Transform f in water.transform)
                {
                    f.GetComponent<SpriteRenderer>().material = new Material(Shader.Find("UI/BlendModes/LinearDodge"));
                }

                AddTramAndNPCs();
                
                HegemolController hegemolCtrl = BossLoader.CreateHegemol();
                GameCameras.instance.cameraShakeFSM.FsmVariables.FindFsmBool("RumblingMed").Value = false;

                yield return new WaitWhile(() => HeroController.instance == null);
                
                PlayMusic(FiveKnights.Clips["HegAreaMusicIntro"]);
                PlayHegemolBGSound(hegemolCtrl);
                yield return new WaitForSeconds(FiveKnights.Clips["HegAreaMusicIntro"].length);
                PlayMusic(FiveKnights.Clips["HegAreaMusic"]);
                yield return new WaitWhile(()=> HeroController.instance.transform.position.x < 427f);
                PlayMusic(FiveKnights.Clips["HegemolMusic"]);
                hegemolCtrl.gameObject.SetActive(true);

                yield return new WaitWhile(() => hegemolCtrl != null);
                yield return new WaitForSeconds(1.0f);

                foreach(Tram tram in UnityEngine.Object.FindObjectsOfType<Tram>())
				{
                    tram.FadeAudio();
				}

                WinRoutine(OWArenaFinder.PrevHegScene, 2);
                Log("Done with Heg, transitioning out");
                Destroy(this);
            }
            else if (CustomWP.boss == CustomWP.Boss.Ze)
            {
                ZemerController.WaitForTChild = true;
                ZemerController zc = BossLoader.CreateZemer();
                PlayMusic(FiveKnights.Clips["Zem_Area"]);
                GameObject zem = zc.gameObject;
                zem.SetActive(true);
                zem.GetComponent<HealthManager>().IsInvincible = true;
                GameObject child = Instantiate(FiveKnights.preloadedGO["TChild"]);
                var tChild = child.AddComponent<TChildCtrl>();
                child.SetActive(true);
                tChild.zemer = zem;

                yield return null;
                
                yield return new WaitWhile(() => !tChild.helpZemer);
                
                ZemerController.WaitForTChild = false;
                zem.GetComponent<HealthManager>().IsInvincible = false;
                yield return new WaitWhile(() => zc != null);
                if (zem == null)
                {
                    Log("Zem did not exist so destroying");
                    Destroy(this);
                    yield break;
                }
                ZemerControllerP2 zc2 = zem.GetComponent<ZemerControllerP2>();
                yield return new WaitWhile(() => zc2 != null);

                yield return new WaitForSeconds(1f);
                WinRoutine(OWArenaFinder.PrevZemScene, 1);
                Destroy(this);
            }
        }

        private void WinRoutine(string area, int index)
        {
            if(GameManager.instance.GetComponent<AwardCharms>()) GameManager.instance.GetComponent<AwardCharms>().bossWin[index] = true;
            string msgKey = "placeholder key aaaaaaaa";
            int wins;
			switch(index)
			{
                case 0:
                    FiveKnights.Instance.SaveSettings.CompletionDryya.isUnlocked = true;
                    FiveKnights.Instance.SaveSettings.DryyaOWWinCount++;
                    wins = FiveKnights.Instance.SaveSettings.DryyaOWWinCount;
                    if(wins < 5) msgKey = "DRYYA_OUTRO_" + wins;
                    else msgKey = "DRYYA_OUTRO_5";
                    break;
                case 1:
                    FiveKnights.Instance.SaveSettings.CompletionZemer.isUnlocked = true;
                    FiveKnights.Instance.SaveSettings.ZemerOWWinCount++;
                    wins = FiveKnights.Instance.SaveSettings.ZemerOWWinCount;
                    if(wins < 5) msgKey = "ZEM_OUTRO_" + wins;
                    else msgKey = "ZEM_OUTRO_5";
                    break;
                case 2:
                    FiveKnights.Instance.SaveSettings.CompletionHegemol.isUnlocked = true;
                    FiveKnights.Instance.SaveSettings.HegOWWinCount++;
                    wins = FiveKnights.Instance.SaveSettings.HegOWWinCount;
                    if(wins < 5) msgKey = "HEG_OUTRO_" + wins;
                    else msgKey = "HEG_OUTRO_5";
                    break;
                case 3:
                    FiveKnights.Instance.SaveSettings.CompletionIsma.isUnlocked = true;
                    FiveKnights.Instance.SaveSettings.IsmaOWWinCount++;
                    wins = FiveKnights.Instance.SaveSettings.IsmaOWWinCount;
                    if(wins < 5) msgKey = "ISMA_OUTRO_" + wins;
                    else msgKey = "ISMA_OUTRO_5";
                    break;
            }
            foreach(PlayMakerFSM hcFSM in HeroController.instance.gameObject.GetComponentsInChildren<PlayMakerFSM>())
			{
                hcFSM.SendEvent("FSM CANCEL");
			}
            HeroController.instance.AffectedByGravity(true);
            HeroController.instance.StartAnimationControl();
            HeroController.instance.RelinquishControl();
            PlayerData.instance.disablePause = true;
            GameObject dreambye = GameObject.Find("Dream Exit Particle Field");
            if (dreambye != null)
            {
                dreambye.GetComponent<ParticleSystem>().Play();
            }
            var deathcomp = (EnemyDeathEffects) _dd.GetComponent<EnemyDeathEffectsUninfected>();
            var corpsePrefab = Mirror.GetField<EnemyDeathEffects, GameObject>(deathcomp, "corpsePrefab");
            GameObject transDevice = Instantiate(corpsePrefab);
            transDevice.SetActive(true);
            var fsm = transDevice.LocateMyFSM("Control");
            GameObject text = fsm.GetAction<SetTextMeshProAlignment>("New Scene", 1).gameObject.GameObject.Value;
            TextMeshPro tmp = text.GetComponent<TextMeshPro>();
            fsm.GetAction<Wait>("Fade Out", 4).time.Value += 2f;
            PlayMakerFSM fsm2 = GameObject.Find("Blanker White").LocateMyFSM("Blanker Control");
            fsm2.FsmVariables.FindFsmFloat("Fade Time").Value = 0;
            fsm.GetState("Fade Out").RemoveAction(0);
            fsm.ChangeTransition("Take Control", "FINISHED", "Outro Msg 1a");
            fsm.ChangeTransition("Outro Msg 1a", "CONVO_FINISH", "New Scene");
            tmp.color = Color.black;
            tmp.alignment = TextAlignmentOptions.Center;

            fsm.GetAction<CallMethodProper>("Outro Msg 1a", 0).parameters[0].stringValue = msgKey;
            fsm.GetAction<CallMethodProper>("Outro Msg 1a", 0).parameters[1].stringValue = "Speech";

            fsm.GetAction<BeginSceneTransition>("New Scene", 6).preventCameraFadeOut = true;
            fsm.GetAction<BeginSceneTransition>("New Scene", 6).sceneName = area;
            fsm.GetAction<BeginSceneTransition>("New Scene", 6).entryGateName = "door_dreamReturn";
            fsm.GetAction<BeginSceneTransition>("New Scene", 6).visualization.Value = GameManager.SceneLoadVisualizations.Default;
            fsm.GetAction<BeginSceneTransition>("New Scene", 6).entryDelay = 0;
            HeroController.instance.EnterWithoutInput(true);
            HeroController.instance.MaxHealth();
            fsm.SetState("Fade Out");
        }

        private IEnumerator ClearWhiteScreen()
		{
            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.J));
            Log("Clearing white screen");
            GameObject.Find("Blanker White").LocateMyFSM("Blanker Control").SendEvent("FADE OUT");
            HeroController.instance.EnableRenderer();
            HeroController.instance.AcceptInput();
            HeroController.instance.RegainControl();
        }

        public static void PlayMusic(AudioClip clip)
        {
            MusicCue musicCue = ScriptableObject.CreateInstance<MusicCue>();
            MusicCue.MusicChannelInfo channelInfo = new MusicCue.MusicChannelInfo();
            Mirror.SetField(channelInfo, "clip", clip);

            MusicCue.MusicChannelInfo[] channelInfos = new MusicCue.MusicChannelInfo[]
            {
                channelInfo, null, null, null, null, null
            };
            Mirror.SetField(musicCue, "channelInfos", channelInfos);
            var yoursnapshot = Resources.FindObjectsOfTypeAll<AudioMixer>().First(x => x.name == "Music").FindSnapshot("Main Only");
            yoursnapshot.TransitionTo(0);
            GameManager.instance.AudioManager.ApplyMusicCue(musicCue, 0, 0, false);
        }

        private void AddTramAndNPCs()
        {
            var mobs = GameObject.Find("BG_Mobs"); 
            foreach (Transform grp in mobs.transform)
            {
                foreach (Transform m in grp)
                {
                    GameObject mob = m.gameObject;
                    switch (grp.name)
                    {
                        case "Carriage":
                            mob.AddComponent<Carriage>();
                            break;
                        case "Husk":
                            mob.AddComponent<HuskCitizen>();
                            break;
                        case "HuskCart":
                            mob.AddComponent<HuskCart>();
                            break;
                        case "Maggot":
                            mob.AddComponent<Maggot>();
                            break;
                        case "MineCart":
                            mob.AddComponent<MineBugCart>(); 
                            break;
                    }
                }
            }
                
            var tram = Instantiate(FiveKnights.preloadedGO["Tram"]);
            tram.AddComponent<Tram>();
            GameObject riders = GameObject.Find("Riders");
            riders.transform.position = tram.transform.position;
            riders.transform.parent = tram.transform;
            riders.transform.localPosition = new Vector3(0f, -2.45f, 0f);
            tram.SetActive(true);
            
            var tram2 = Instantiate(FiveKnights.preloadedGO["Tram"]);
            tram2.AddComponent<TramSmall>();
            tram2.SetActive(true);
        }
        
        private static void PlayHegemolBGSound(HegemolController heg)
        {
            GameObject audioPlayer = new GameObject("Audio Player", typeof(AudioSource), typeof(AutoDestroy));
            audioPlayer.transform.position = new Vector3(437f, 171.1914f, 0f);

            AutoDestroy autoDestroy = audioPlayer.GetComponent<AutoDestroy>();
            autoDestroy.ShouldDestroy = () => heg.gameObject.activeSelf;

            AudioSource audioSource = audioPlayer.GetComponent<AudioSource>();
            audioSource.clip = FiveKnights.Clips["HegAreaMusicBG"];
            audioSource.volume = 1f;
            audioSource.pitch = 1f; 
            audioSource.loop = true;
            audioSource.maxDistance = 150;
            audioSource.outputAudioMixerGroup = HeroController.instance.GetComponent<AudioSource>().outputAudioMixerGroup;
            audioSource.Play();
        }

        private void OnDestroy()
        {
            _ap?.StopMusic();
            _ap2?.StopMusic();
            GameManager.instance.StartCoroutine(ClearWhiteScreen());
        }

        private void Log(object o)
        {
            if (!FiveKnights.isDebug) return;
            Modding.Logger.Log("[OWBossManager] " + o);
        }
    }
}
