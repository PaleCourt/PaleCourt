using UnityEngine;
using Modding;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HutongGames.PlayMaker.Actions;
using InControl;
using ModCommon;
using ModCommon.Util;
using ReflectionHelper = Modding.ReflectionHelper;
using UObject = UnityEngine.Object;

namespace FiveKnights
{
    public class WDController : MonoBehaviour
    {
        private HealthManager _hm;
        private PlayMakerFSM _fsm;
        public GameObject dd; 
        private tk2dSpriteAnimator _tk;
        private List<AssetBundle> _assetBundles;
        public MusicPlayer _ap;
        public MusicPlayer _ap2;
        public static bool alone;
        private bool HIT_FLAG;
        public static WDController Instance;

        private IEnumerator Start()
        {
            Instance = this;
            _hm = dd.GetComponent<HealthManager>();
            _fsm = dd.LocateMyFSM("Dung Defender");
            _tk = dd.GetComponent<tk2dSpriteAnimator>();
            FiveKnights.preloadedGO["WD"] = dd;
            alone = true;
            _assetBundles= new List<AssetBundle>();
            OnDestroy();
            On.HealthManager.TakeDamage += HealthManager_TakeDamage;
            ModHooks.Instance.BeforePlayerDeadHook += BeforePlayerDied;
            On.MusicCue.GetChannelInfo += MusicCue_GetChannelInfo;
            string dret = PlayerData.instance.dreamReturnScene;
            PlayerData.instance.dreamReturnScene = (dret == "Waterways_13") ? dret : "White_Palace_09";
            dret = PlayerData.instance.dreamReturnScene;
            PlayerData.instance.dreamReturnScene = (CustomWP.boss == CustomWP.Boss.All) ? "Dream_04_White_Defender" : dret;
            Log("Curr Boss " + CustomWP.boss);
            //Be sure to do CustomWP.Instance.wonLastFight = true; on win
            if (CustomWP.boss == CustomWP.Boss.Isma)
            {
                yield return LoadIsmaBundle();
                dd.SetActive(false);
                FightController.Instance.CreateIsma();
                IsmaController ic = FiveKnights.preloadedGO["Isma2"].GetComponent<IsmaController>();
                ic.onlyIsma = true;
                yield return new WaitWhile(() => ic != null);
                if (CustomWP.Instance.wonLastFight)
                {
                    int lev = CustomWP.Instance.lev + 1;
                    var box = (object) FiveKnights.Instance.Settings.CompletionIsma;
                    var fi = ReflectionHelper.GetField(typeof(BossStatue.Completion), $"completedTier{lev}");
                    fi.SetValue(box, true);
                    FiveKnights.Instance.Settings.CompletionIsma = (BossStatue.Completion) box;
                }
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
                yield return LoadIsmaBundle();
                alone = false;
                _hm.hp = 950;
                _fsm.GetAction<Wait>("Rage Roar", 9).time = 1.5f;
                _fsm.FsmVariables.FindFsmBool("Raged").Value = true;
                yield return new WaitForSeconds(1f);
                GameCameras.instance.cameraFadeFSM.Fsm.SetState("FadeIn");
                yield return new WaitWhile(() => _hm.hp > 600);
                _fsm.ChangeTransition("Rage Roar", "FINISHED", "Music");
                _fsm.ChangeTransition("Music", "FINISHED", "Set Rage");
                var ac1 = _fsm.GetAction<TransitionToAudioSnapshot>("Music", 1).snapshot;
                var ac2 = _fsm.GetAction<ApplyMusicCue>("Music", 2).musicCue;
                _fsm.AddAction("Rage Roar", new TransitionToAudioSnapshot()
                {
                    snapshot = ac1,
                    transitionTime = 0
                });
                _fsm.AddAction("Rage Roar", new ApplyMusicCue()
                {
                    musicCue = ac2,
                    transitionTime = 0,
                    delayTime = 0
                });
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
                _ap.Volume = 1f;
                _ap.UpdateMusic();
                _fsm.enabled = true;
                _fsm.SetState("Stun Recover");
                yield return null;
                yield return new WaitWhile(() => _fsm.ActiveStateName == "Stun Recover");
                _fsm.SetState("Rage Roar");
                PlayerData.instance.isInvincible = false;
                GameManager.instance.playerData.disablePause = false;
                HeroController.instance.RegainControl();
                PlayMakerFSM burrow = GameObject.Find("Burrow Effect").LocateMyFSM("Burrow Effect");
                yield return new WaitWhile(() => burrow.ActiveStateName != "Burrowing");
                burrow.SendEvent("BURROW END");
                yield return new WaitWhile(() => ic != null);
                _ap.StopMusic();
                _ap2.StopMusic();
                if (CustomWP.Instance.wonLastFight)
                {
                    int lev = CustomWP.Instance.lev + 1;
                    var box = (object) FiveKnights.Instance.Settings.CompletionIsma2;
                    var fi = ReflectionHelper.GetField(typeof(BossStatue.Completion), $"completedTier{lev}");
                    fi.SetValue(box, true);
                    FiveKnights.Instance.Settings.CompletionIsma2 = (BossStatue.Completion) box;
                }
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
                yield return LoadDryyaAssets();
                
                dd.SetActive(false);
                DryyaSetup dc = FightController.Instance.CreateDryya();
                yield return new WaitWhile(() => dc != null);
                if (CustomWP.Instance.wonLastFight)
                {
                    int lev = CustomWP.Instance.lev + 1;
                    var box = (object) FiveKnights.Instance.Settings.CompletionDryya;
                    var fi = ReflectionHelper.GetField(typeof(BossStatue.Completion), $"completedTier{lev}");
                    fi.SetValue(box, true);
                    FiveKnights.Instance.Settings.CompletionDryya = (BossStatue.Completion) box;
                }
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
                yield return LoadHegemolBundle();
                
                dd.SetActive(false);
                HegemolController hegemolCtrl = FightController.Instance.CreateHegemol();
                GameObject.Find("Burrow Effect").SetActive(false);
                GameCameras.instance.cameraShakeFSM.FsmVariables.FindFsmBool("RumblingMed").Value = false;
                yield return new WaitWhile(() => hegemolCtrl != null);
                if (CustomWP.Instance.wonLastFight)
                {
                    int lev = CustomWP.Instance.lev + 1;
                    var box = (object) FiveKnights.Instance.Settings.CompletionHegemol;
                    var fi = ReflectionHelper.GetField(typeof(BossStatue.Completion), $"completedTier{lev}");
                    fi.SetValue(box, true);
                    FiveKnights.Instance.Settings.CompletionHegemol = (BossStatue.Completion) box;
                }
                var bossSceneController = GameObject.Find("Boss Scene Controller");
                var bsc = bossSceneController.GetComponent<BossSceneController>();
                GameObject transition = Instantiate(bsc.transitionPrefab);
                PlayMakerFSM transitionsFSM = transition.LocateMyFSM("Transitions");
                transitionsFSM.SetState("Out Statue");
                yield return new WaitForSeconds(1.0f);
                bsc.DoDreamReturn();
                Destroy(this);
            }
            else if (CustomWP.boss == CustomWP.Boss.Ze || CustomWP.boss == CustomWP.Boss.Mystic)
            {
                yield return LoadZemerBundle();
                dd.SetActive(false);
                GameObject.Find("Burrow Effect").SetActive(false);
                GameCameras.instance.cameraShakeFSM.FsmVariables.FindFsmBool("RumblingMed").Value = false;
                yield return null;
                ZemerController zc = FightController.Instance.CreateZemer();
                GameObject zem = zc.gameObject;
                yield return new WaitWhile(() => zc != null);
                ZemerControllerP2 zc2 = zem.GetComponent<ZemerControllerP2>();
                yield return new WaitWhile(() => zc2 != null);
                if (CustomWP.Instance.wonLastFight)
                {
                    int lev = CustomWP.Instance.lev + 1;
                    if (CustomWP.boss == CustomWP.Boss.Ze)
                    {
                        var box = (object) FiveKnights.Instance.Settings.CompletionZemer;
                        var fi = ReflectionHelper.GetField(typeof(BossStatue.Completion), $"completedTier{lev}");
                        fi.SetValue(box, true);
                        FiveKnights.Instance.Settings.CompletionZemer = (BossStatue.Completion) box;
                    }
                    else
                    {
                        var box = (object) FiveKnights.Instance.Settings.CompletionZemer2;
                        var fi = ReflectionHelper.GetField(typeof(BossStatue.Completion), $"completedTier{lev}");
                        fi.SetValue(box, true);
                        FiveKnights.Instance.Settings.CompletionZemer2 = (BossStatue.Completion) box;
                    }
                }
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
                yield return null;
                bool flag = false;
                StartCoroutine(Wow());
                var isma = StartCoroutine(LoadIsmaBundle());
                var dryya = StartCoroutine(LoadDryyaAssets());
                var hegem = StartCoroutine(LoadHegemolBundle());
                var zemer = StartCoroutine(LoadZemerBundle());
                yield return isma;
                yield return dryya;
                yield return hegem;
                yield return zemer;
                flag = true;
                HeroController.instance.RegainControl();
                HeroController.instance.AcceptInput();
                
                IEnumerator Wow()
                {
                    while (!flag)
                    {
                        HeroController.instance.RelinquishControl();
                        HeroController.instance.IgnoreInput();
                        HeroController.instance.IgnoreInputWithoutReset();
                        yield return null;
                    }
                }

                alone = false;
                _hm.hp = 950;
                _fsm.GetAction<Wait>("Rage Roar", 9).time = 1.5f;
                _fsm.FsmVariables.FindFsmBool("Raged").Value = true;
                yield return new WaitForSeconds(1f);
                GameCameras.instance.cameraFadeFSM.Fsm.SetState("FadeIn");
                yield return new WaitWhile(() => _hm.hp > 600);
                _fsm.ChangeTransition("Rage Roar", "FINISHED", "Music");
                _fsm.ChangeTransition("Music", "FINISHED", "Set Rage");
                var ac1 = _fsm.GetAction<TransitionToAudioSnapshot>("Music", 1).snapshot;
                var ac2 = _fsm.GetAction<ApplyMusicCue>("Music", 2).musicCue;
                _fsm.AddAction("Rage Roar", new TransitionToAudioSnapshot()
                {
                    snapshot = ac1,
                    transitionTime = 0
                });
                _fsm.AddAction("Rage Roar", new ApplyMusicCue()
                {
                    musicCue = ac2,
                    transitionTime = 0,
                    delayTime = 0
                });
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
                _ap.Volume = 1f;
                _ap.UpdateMusic();
                _fsm.enabled = true;
                _fsm.SetState("Stun Recover");
                yield return null;
                yield return new WaitWhile(() => _fsm.ActiveStateName == "Stun Recover");
                _fsm.SetState("Rage Roar");
                PlayerData.instance.isInvincible = false;
                GameManager.instance.playerData.disablePause = false;
                HeroController.instance.RegainControl();
                PlayMakerFSM burrow = GameObject.Find("Burrow Effect").LocateMyFSM("Burrow Effect");
                yield return new WaitWhile(() => burrow.ActiveStateName != "Burrowing");
                burrow.SendEvent("BURROW END");
                yield return new WaitWhile(() => ic != null);
                PlayMusic(null, 1f);
                dd.SetActive(false);
                _ap.StopMusic();
                _ap2.StopMusic();
                
                
                GameObject dryyaSilhouette = GameObject.Find("Silhouette Dryya");
                SpriteRenderer sr = dryyaSilhouette.GetComponent<SpriteRenderer>();
                dryyaSilhouette.transform.localScale *= 1.2f;
                sr.sprite = ArenaFinder.Sprites["Dryya_Silhouette_1"];
                yield return new WaitForSeconds(0.1f);
                sr.sprite = ArenaFinder.Sprites["Dryya_Silhouette_2"];
                yield return new WaitForSeconds(0.1f);
                sr.sprite = ArenaFinder.Sprites["Dryya_Silhouette_3"];
                yield return new WaitForSeconds(0.1f);
                Destroy(dryyaSilhouette);
                yield return new WaitForSeconds(0.5f);
                DryyaSetup dc = FightController.Instance.CreateDryya();
                yield return new WaitWhile(() => dc != null);
                
                GameObject hegSil = GameObject.Find("Silhouette Hegemol");
                SpriteRenderer sr2 = hegSil.GetComponent<SpriteRenderer>();
                hegSil.transform.localScale *= 1.2f;
                for (int i = 0; i <= 5; i++)
                {
                    sr2.sprite = ArenaFinder.Sprites["hegemol_silhouette_"+i];
                    yield return new WaitForSeconds(0.1f);
                }
                sr2.sprite = ArenaFinder.Sprites["hegemol_silhouette_6"];
                hegSil.AddComponent<Rigidbody2D>().gravityScale = 0;
                hegSil.GetComponent<Rigidbody2D>().velocity = new Vector2(0f,50f);
                yield return new WaitForSeconds(0.1f);
                sr2.sprite = ArenaFinder.Sprites["hegemol_silhouette_7"];
                yield return new WaitForSeconds(0.5f);
                Destroy(hegSil);
                HegemolController hegemolCtrl = FightController.Instance.CreateHegemol();
                yield return new WaitWhile(() => hegemolCtrl != null);
                
                yield return new WaitForSeconds(0.5f);
                ZemerController zc = FightController.Instance.CreateZemer();
                GameObject zem = zc.gameObject;
                yield return new WaitWhile(() => zc != null);
                ZemerControllerP2 zc2 = zem.GetComponent<ZemerControllerP2>();
                yield return new WaitWhile(() => zc2 != null);
            }
        }

        public void BeforePlayerDied()
        {
            Log("RAN");
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

        public void PlayMusic(AudioClip clip, float vol = 0f)
        {
            MusicCue musicCue = ScriptableObject.CreateInstance<MusicCue>();
            List<MusicCue.MusicChannelInfo> channelInfos = new List<MusicCue.MusicChannelInfo>();
            MusicCue.MusicChannelInfo channelInfo = new MusicCue.MusicChannelInfo();
            channelInfo.SetAttr("clip", clip);
            channelInfos.Add(channelInfo);
            musicCue.SetAttr("channelInfos", channelInfos.ToArray());
            GameManager.instance.AudioManager.ApplyMusicCue(musicCue, 0, 0, false);
        }
        
        /*public void PlayMusic(string clip, float vol = 0f)
        {
            GameObject actor = GameObject.Find("Audio Player Actor");
            AudioClip ac = ArenaFinder.Clips[clip];
            CustomAudioPlayer = new MusicPlayer
            {
                Volume = vol,
                Clip = ac,
                Player = actor,
                MaxPitch = 1f,
                MinPitch = 1f,
                Loop = true,
                Spawn = HeroController.instance.gameObject
            };
            CustomAudioPlayer.DoPlayRandomClip();
        }*/

        private bool startedMusic;
        
        private MusicCue.MusicChannelInfo MusicCue_GetChannelInfo(On.MusicCue.orig_GetChannelInfo orig, MusicCue self, MusicChannels channel)
        {
            if (!startedMusic && self.name.Contains("Defender") && (CustomWP.boss == CustomWP.Boss.Ogrim || CustomWP.boss == CustomWP.Boss.All))
            {
                Log("PLayed Isma song too " + self.name);
                startedMusic = true;
                PlayMakerFSM spellControl = HeroController.instance.gameObject.LocateMyFSM("Spell Control");
                GameObject fireballParent = spellControl.GetAction<SpawnObjectFromGlobalPool>("Fireball 2", 3).gameObject.Value;
                PlayMakerFSM fireballCast = fireballParent.LocateMyFSM("Fireball Cast");
                GameObject actor = fireballCast.GetAction<AudioPlayerOneShotSingle>("Cast Right", 3).audioPlayer.Value;
                _ap = new MusicPlayer
                {
                    Volume = 0f,
                    Player = actor,
                    MaxPitch = 1f,
                    MinPitch = 1f,
                    Loop = true,
                    Clip = FiveKnights.Clips["IsmaMusic"],
                    Spawn = HeroController.instance.gameObject
                };
                _ap2 = new MusicPlayer
                {
                    Volume = 1f,
                    Player = actor,
                    MaxPitch = 1f,
                    MinPitch = 1f,
                    Loop = true,
                    Clip = FiveKnights.Clips["OgrimMusic"],
                    Spawn = HeroController.instance.gameObject
                };

                _ap.DoPlayRandomClip();
                _ap2.DoPlayRandomClip();
                
                return null;
            }

            if (startedMusic && self.name.Contains("Defender") &&
                (CustomWP.boss == CustomWP.Boss.Ogrim || CustomWP.boss == CustomWP.Boss.All))
            {
                return null;
            }
            return orig(self, channel);
        }

        private void OnDestroy()
        {
            foreach (var i in _assetBundles)
            {
                if (i == null) continue;
                Log("Removing Bundle " + i.name);
                i.Unload(true);
            }

            
            ModHooks.Instance.BeforePlayerDeadHook -= BeforePlayerDied;
            On.HealthManager.TakeDamage -= HealthManager_TakeDamage;
            On.MusicCue.GetChannelInfo -= MusicCue_GetChannelInfo;
        }

        private void Log(object o)
        {
            Modding.Logger.Log("[White Defender] " + o);
        }
        
        private IEnumerator LoadIsmaBundle()
        {
            Log("Loading Isma Bundle");
            if (FiveKnights.preloadedGO.TryGetValue("Isma", out var go) && go != null)
            {
                Log("broke Isma");
                yield break;
            }
            
            Assembly asm = Assembly.GetExecutingAssembly();
            using (Stream s = asm.GetManifestResourceStream("FiveKnights.StreamingAssets.isma"+FiveKnights.OS))
            {
                AssetBundle ab = null;
                if (CustomWP.boss == CustomWP.Boss.All)
                {
                    var req = AssetBundle.LoadFromStreamAsync(s);
                    yield return req;
                    ab = req.assetBundle;
                }
                else
                {
                    ab = AssetBundle.LoadFromStream(s);
                }

                yield return null;
                foreach (GameObject i in ab.LoadAllAssets<GameObject>())
                {
                    if (i.name == "Isma") FiveKnights.preloadedGO["Isma"] = i;
                    else if (i.name == "Gulka") FiveKnights.preloadedGO["Gulka"] = i;
                    else if (i.name == "Plant") FiveKnights.preloadedGO["Plant"] = i;
                    else if (i.name == "Fool") FiveKnights.preloadedGO["Fool"] = i;
                    else if (i.name == "Wall") FiveKnights.preloadedGO["Wall"] = i;
                    yield return null;
                    if (i.GetComponent<SpriteRenderer>() == null)
                    {
                        foreach (SpriteRenderer sr in i.GetComponentsInChildren<SpriteRenderer>(true))
                        {
                            sr.material = new Material(Shader.Find("Sprites/Default"));
                        }
                    }
                    else i.GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));
                }
                
                _assetBundles.Add(ab);
                yield return null; // Have to wait a frame before unloading /shrug
                ab.Unload(false);
            }

            if (CustomWP.boss != CustomWP.Boss.All)
            {
                using (Stream stream2 = asm.GetManifestResourceStream("FiveKnights.StreamingAssets.ismabg"))
                {
                    AssetBundle ab2 = AssetBundle.LoadFromStream(stream2);
                    FiveKnights.preloadedGO["ismaBG"] = ab2.LoadAsset<GameObject>("gg_dung_set (1)");
                    _assetBundles.Add(ab2);

                    yield return null; // Have to wait a frame before unloading /shrug

                    ab2.Unload(false);
                }
            }

            Log("Finished Loading Isma Bundle");
        }
        
        private IEnumerator LoadDryyaAssets()
        {
            Log("Loading Dryya Bundle");
            if (FiveKnights.preloadedGO.TryGetValue("Dryya", out var go) && go != null)
            {
                Log("broke Dryya");
                yield break;
            }
            
            Assembly asm = Assembly.GetExecutingAssembly();
            
            using (Stream s = asm.GetManifestResourceStream("FiveKnights.StreamingAssets.dryya" + FiveKnights.OS))
            {
                AssetBundle dryyaAssetBundle = null;
                if (CustomWP.boss == CustomWP.Boss.All)
                {
                    var req = AssetBundle.LoadFromStreamAsync(s);
                    yield return req;
                    dryyaAssetBundle = req.assetBundle;
                }
                else
                {
                    dryyaAssetBundle = AssetBundle.LoadFromStream(s);
                }

                FiveKnights.preloadedGO["Dryya"] = dryyaAssetBundle.LoadAsset<GameObject>("Dryya");
                FiveKnights.preloadedGO["Stab Effect"] = dryyaAssetBundle.LoadAsset<GameObject>("Stab Effect");
                FiveKnights.preloadedGO["Dive Effect"] = dryyaAssetBundle.LoadAsset<GameObject>("Dive Effect");
                FiveKnights.preloadedGO["Elegy Beam"] = dryyaAssetBundle.LoadAsset<GameObject>("Elegy Beam");
                yield return null;
                
                _assetBundles.Add(dryyaAssetBundle);
                dryyaAssetBundle.Unload(false);
            }
            
            Log("Finished Loading Dryya Bundle");
        }

        private IEnumerator LoadHegemolBundle()
        {
            Log("Loading Hegemol Bundle");
            if (FiveKnights.preloadedGO.TryGetValue("Hegemol Collection Prefab", out var go) && go != null)
            {
                Log("broke Hegemol Collection Prefab");
                yield break;
            }
            
            Assembly asm = Assembly.GetExecutingAssembly();

            using (Stream s = asm.GetManifestResourceStream("FiveKnights.StreamingAssets.hegemol" + FiveKnights.OS))
            {
                AssetBundle hegemolBundle = null;
                if (CustomWP.boss == CustomWP.Boss.All)
                {
                    var req = AssetBundle.LoadFromStreamAsync(s);
                    yield return req;
                    hegemolBundle = req.assetBundle;
                }
                else
                {
                    hegemolBundle = AssetBundle.LoadFromStream(s);
                }

                FiveKnights.preloadedGO["Hegemol Collection Prefab"] = hegemolBundle.LoadAsset<GameObject>("HegemolSpriteCollection");
                FiveKnights.preloadedGO["Hegemol Animation Prefab"] = hegemolBundle.LoadAsset<GameObject>("HegemolSpriteAnimation");
                FiveKnights.preloadedGO["Mace"] = hegemolBundle.LoadAsset<GameObject>("Mace");
                
                yield return null;
                
                hegemolBundle.Unload(false);
            }

            Log("Finished Loading Hegemol Bundle");
        }
        
        private IEnumerator LoadZemerBundle()
        {
            Log("Loading Zemer Bundle");
            
            if (FiveKnights.preloadedGO.TryGetValue("Zemer", out var go) && go != null)
            {
                Log("broke Zemer");
                yield break;
            }
            
            PlayMakerFSM fsm = FiveKnights.preloadedGO["Traitor"].LocateMyFSM("Mantis");
            FiveKnights.preloadedGO["TraitorSlam"] =
                fsm.GetAction<SpawnObjectFromGlobalPool>("Waves", 0).gameObject.Value;
            FiveKnights.Clips["TraitorSlam"] = fsm.GetAction<AudioPlayerOneShotSingle>("Waves", 4).audioClip.Value as AudioClip;
            
            Assembly asm = Assembly.GetExecutingAssembly();

            using (Stream s = asm.GetManifestResourceStream("FiveKnights.StreamingAssets.zemer" + FiveKnights.OS))
            {
                AssetBundle ab = null;
                if (CustomWP.boss == CustomWP.Boss.All)
                {
                    var req = AssetBundle.LoadFromStreamAsync(s);
                    yield return req;
                    ab = req.assetBundle;
                }
                else
                {
                    ab = AssetBundle.LoadFromStream(s);
                }

                yield return null;
                foreach (GameObject i in ab.LoadAllAssets<GameObject>())
                {
                    if (i.name == "Zemer") FiveKnights.preloadedGO["Zemer"] = i;
                    else if (i.name == "NewSlash") FiveKnights.preloadedGO["SlashBeam"] = i;
                    else if (i.name == "NewSlash2") FiveKnights.preloadedGO["SlashBeam2"] = i;
                    yield return null;
                    if (i.GetComponent<SpriteRenderer>() == null)
                    {
                        foreach (SpriteRenderer sr in i.GetComponentsInChildren<SpriteRenderer>(true))
                        {
                            sr.material = new Material(Shader.Find("Sprites/Default"));
                        }
                    }
                    else i.GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));
                }

                FiveKnights.preloadedGO["SlashBeam"].GetComponent<SpriteRenderer>().material =
                    new Material(Shader.Find("Sprites/Default"));
                
                _assetBundles.Add(ab);
                
                yield return null;
                
                ab.Unload(false);
            }

            Log("Finished Loading Zemer Bundle");
        }
    }
}