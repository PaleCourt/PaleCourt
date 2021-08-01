using System;
using System.Collections;
using System.Reflection;
using Modding;
using JetBrains.Annotations;
using UnityEngine;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using UObject = UnityEngine.Object;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FiveKnights.BossManagement;
using FiveKnights.Misc;
using HutongGames.PlayMaker.Actions;
using SFCore.Utils;
using UnityEngine.Audio;
using Random = UnityEngine.Random;
using UnityEngine.UI;

namespace FiveKnights
{

    [UsedImplicitly]
    public class FiveKnights : Mod, ILocalSettings<SaveModSettings>
    {
        private int paleCourtLogoId = -1;
        public static bool isDebug = true;
        public static Dictionary<string, AudioClip> Clips { get; } = new Dictionary<string, AudioClip>();
        public static Dictionary<string, AudioClip> IsmaClips { get; } = new Dictionary<string, AudioClip>();
        public static Dictionary<string, Material> Materials { get; } = new Dictionary<string, Material>();
        private LanguageCtrl langStrings { get; set; }
        public static Dictionary<string, GameObject> preloadedGO = new Dictionary<string, GameObject>();
        public static readonly Dictionary<string, Sprite> SPRITES = new Dictionary<string, Sprite>();
        public static FiveKnights Instance;
        //public SaveModSettings Settings = new SaveModSettings();
        public static string OS
        {
            get
            {
                return SystemInfo.operatingSystemFamily switch
                {
                    OperatingSystemFamily.Windows => "win",
                    OperatingSystemFamily.Linux => "lin",
                    OperatingSystemFamily.MacOSX => "mc",
                    _ => null
                };
            }
        }
        
        public FiveKnights() : base("Pale Court")
        {
            #region Load Embedded Images

            int ind = 0;
            Assembly asm = Assembly.GetExecutingAssembly();
            foreach (string res in asm.GetManifestResourceNames())
            {
                Log("looking at file " + res);
                using Stream s = asm.GetManifestResourceStream(res);
                if (s == null) continue;
                if (res.EndsWith(".png"))
                {
                    byte[] buffer = new byte[s.Length];
                    s.Read(buffer, 0, buffer.Length);
                    s.Dispose();
                    // Create texture from bytes
                    var tex = new Texture2D(1, 1);
                    tex.LoadImage(buffer, true);
                    // Name
                    string resName = res.Split('.')[2];
                    // Difference in sizes, make some sprites 1.5x the size, because that's apparently what the size in game is for all sprites
                    float ppu = 100;
                    List<string> biggerList = new List<string>()
                    {
                        "DlcList",
                        "LogoBlack"
                    };
                    if (biggerList.Contains(resName)) ppu = 200f / 3f;
                    // Create sprite from texture
                    SPRITES.Add(resName, Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu));
                    Log("Created sprite from embedded image: " + resName + " at ind " + ++ind);
                }
            }

            #endregion
            #region Menu Customization

            LoadTitleScreen();
            On.UIManager.Awake += OnUIManagerAwake;
            On.SetVersionNumber.Start += OnSetVersionNumberStart;
            //SFCore.MenuStyleHelper.Initialize();
            SFCore.MenuStyleHelper.AddMenuStyleHook += AddPCMenuStyle;
            //SFCore.TitleLogoHelper.Initialize();
            paleCourtLogoId = SFCore.TitleLogoHelper.AddLogo(SPRITES["LogoBlack"]);

            #endregion
            #region Enviroment Effects

            SFCore.EnviromentParticleHelper.AddCustomDashEffectsHook += AddCustomDashEffectsHook;
            SFCore.EnviromentParticleHelper.AddCustomHardLandEffectsHook += AddCustomHardLandEffectsHook;
            SFCore.EnviromentParticleHelper.AddCustomJumpEffectsHook += AddCustomJumpEffectsHook;
            SFCore.EnviromentParticleHelper.AddCustomSoftLandEffectsHook += AddCustomSoftLandEffectsHook;
            SFCore.EnviromentParticleHelper.AddCustomRunEffectsHook += AddCustomRunEffectsHook;

            #endregion
            #region Achievements

            //SFCore.AchievementHelper.Initialize();
            
            SFCore.AchievementHelper.AddAchievement("IsmaAchiev2", SPRITES["ach_isma"], 
                "ISMA_ACH_TITLE", "ISMA_ACH_DESC", false);
            
            SFCore.AchievementHelper.AddAchievement("DryyaAchiev", SPRITES["ach_dryya"], 
                "DRYYA_ACH_TITLE", "DRYYA_ACH_DESC", false);
            
            SFCore.AchievementHelper.AddAchievement("HegAchiev", SPRITES["ach_heg"], 
                "HEG_ACH_TITLE", "HEG_ACH_DESC", false);

            SFCore.AchievementHelper.AddAchievement("ZemAchiev", SPRITES["ach_zem"], 
                "ZEM_ACH_TITLE", "ZEM_ACH_DESC", false);
            
            SFCore.AchievementHelper.AddAchievement("PanthAchiev", SPRITES["ach_panth"], 
                "PANTH_ACH_TITLE", "PANTH_ACH_DESC", false);

            #endregion

            #region Language & Hooks

            langStrings = new LanguageCtrl();

            ModHooks.SetPlayerVariableHook += SetVariableHook;
            ModHooks.GetPlayerVariableHook += GetVariableHook;
            ModHooks.AfterSavegameLoadHook += SaveGame;
            ModHooks.NewGameHook += AddComponent;

            ModHooks.LanguageGetHook += LangGet;

            On.AudioManager.ApplyMusicCue += OnAudioManagerApplyMusicCue;
            On.UIManager.Start += OnUIManagerStart;

            #endregion

            #region Load Assetbundles

            GameObject assetLoaderGo = new GameObject("Pale Court Asset Loader", typeof(NonBouncer));
            GameObject.DontDestroyOnLoad(assetLoaderGo);
            var nb = assetLoaderGo.GetComponent<NonBouncer>();
            nb.StartCoroutine(LoadDep());
            nb.StartCoroutine(LoadBossBundles());

            #endregion
        }

        public override string GetVersion() => "0.5.0.0";

        public override List<(string, string)> GetPreloadNames()
        {
            return new List<(string, string)>
            {
                ("GG_Hive_Knight", "Battle Scene/Hive Knight/Slash 1"),
                ("GG_Hollow_Knight", "Battle Scene/HK Prime"),
                ("GG_Hollow_Knight", "Battle Scene/HK Prime/Counter Flash"),
                ("Abyss_05", "Dusk Knight/Dream Enter 2"),
                ("Abyss_05","Dusk Knight/Idle Pt"),
                ("GG_Failed_Champion","False Knight Dream"),
                ("White_Palace_09","White King Corpse/Throne Sit"),
                ("Fungus1_12","Plant Turret"),
                ("Fungus1_12","simple_grass"),
                ("Fungus1_12","green_grass_2"),
                ("Fungus1_12","green_grass_3"),
                ("Fungus1_12","green_grass_1 (1)"),
                ("Fungus1_19", "Plant Trap"),
                ("White_Palace_01","WhiteBench"),
                // We want the tink effect from the spike
                ("White_Palace_01","White_ Spikes"),
                ("GG_Workshop","GG_Statue_ElderHu"),
                ("GG_Lost_Kin", "Lost Kin"),
                ("GG_Soul_Tyrant", "Dream Mage Lord"),
                ("GG_Workshop","GG_Statue_TraitorLord"),
                ("Room_Mansion","Heart Piece Folder/Heart Piece/Plink"),
                ("Fungus3_23_boss","Battle Scene/Wave 3/Mantis Traitor Lord"),
                ("GG_White_Defender", "Boss Scene Controller"),
                ("GG_Atrium_Roof", "Land of Storms Doors"),
                ("GG_White_Defender", "GG_Arena_Prefab/Godseeker Crowd"),
                ("Dream_04_White_Defender","_SceneManager"),
                ("Dream_04_White_Defender", "Battle Gate (1)"),
                ("Dream_04_White_Defender", "Dream Entry"),
                ("Dream_04_White_Defender", "White Defender"),
                // Ensures falling into pits takes you out of dream
                ("Dream_04_White_Defender", "Dream Fall Catcher")
            };
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Storing GOs");
            preloadedGO["Statue"] = preloadedObjects["GG_Workshop"]["GG_Statue_ElderHu"];
            preloadedGO["DPortal"] = preloadedObjects["Abyss_05"]["Dusk Knight/Dream Enter 2"];
            preloadedGO["DPortal2"] = preloadedObjects["Abyss_05"]["Dusk Knight/Idle Pt"];
            preloadedGO["StatueMed"] = preloadedObjects["GG_Workshop"]["GG_Statue_TraitorLord"];
            preloadedGO["Bench"] = preloadedObjects["White_Palace_01"]["WhiteBench"];

            preloadedGO["TinkEff"] = preloadedObjects["White_Palace_01"]["White_ Spikes"];

            preloadedGO["Slash"] = preloadedObjects["GG_Hive_Knight"]["Battle Scene/Hive Knight/Slash 1"];
            preloadedGO["PV"] = preloadedObjects["GG_Hollow_Knight"]["Battle Scene/HK Prime"];
            preloadedGO["CounterFX"] = preloadedObjects["GG_Hollow_Knight"]["Battle Scene/HK Prime/Counter Flash"];
            preloadedGO["Kin"] = preloadedObjects["GG_Lost_Kin"]["Lost Kin"];
            preloadedGO["Mage"] = preloadedObjects["GG_Soul_Tyrant"]["Dream Mage Lord"];
            preloadedGO["fk"] = preloadedObjects["GG_Failed_Champion"]["False Knight Dream"];
            preloadedGO["throne"] = preloadedObjects["White_Palace_09"]["White King Corpse/Throne Sit"];
            
            preloadedGO["PTurret"] = preloadedObjects["Fungus1_12"]["Plant Turret"];
            preloadedGO["Grass0"] = preloadedObjects["Fungus1_12"]["simple_grass"];
            preloadedGO["Grass2"] = preloadedObjects["Fungus1_12"]["green_grass_2"];
            preloadedGO["Grass3"] = preloadedObjects["Fungus1_12"]["green_grass_3"];
            preloadedGO["Grass1"] = preloadedObjects["Fungus1_12"]["green_grass_1 (1)"];
            preloadedGO["PTrap"] = preloadedObjects["Fungus1_19"]["Plant Trap"];

            preloadedGO["VapeIn2"] = preloadedObjects["Room_Mansion"]["Heart Piece Folder/Heart Piece/Plink"];
            preloadedGO["Traitor"] = preloadedObjects["Fungus3_23_boss"]["Battle Scene/Wave 3/Mantis Traitor Lord"];
            preloadedGO["BSCW"] = preloadedObjects["GG_White_Defender"]["Boss Scene Controller"];
            preloadedGO["StartDoor"] = preloadedObjects["GG_Atrium_Roof"]["Land of Storms Doors"];
            preloadedGO["Godseeker"] = preloadedObjects["GG_White_Defender"]["GG_Arena_Prefab/Godseeker Crowd"];
            preloadedGO["WhiteDef"] = preloadedObjects["Dream_04_White_Defender"]["White Defender"];
            preloadedGO["DreamEntry"] = preloadedObjects["Dream_04_White_Defender"]["Dream Entry"];
            preloadedGO["SMTest"] = preloadedObjects["Dream_04_White_Defender"]["_SceneManager"];
            preloadedGO["BattleGate"] = preloadedObjects["Dream_04_White_Defender"]["Battle Gate (1)"];
            preloadedGO["DreamFall"] = preloadedObjects["Dream_04_White_Defender"]["Dream Fall Catcher"];
            preloadedGO["isma_stat"] = null;
            
            Instance = this;
            UObject.Destroy(preloadedGO["DPortal"].LocateMyFSM("Check if midwarp or completed"));
            PlantChanger();
            Log("Initalizing.");
        }

        #region Make Text Readable

        private void OnUIManagerStart(On.UIManager.orig_Start orig, UIManager self)
        {
            foreach (var item in self.gameObject.GetComponentsInChildren<Text>(true))
            {
                var outline = item.gameObject.AddComponent<Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }
            orig(self);
        }

        #endregion

        #region Menu Customization

        private void OnSetVersionNumberStart(On.SetVersionNumber.orig_Start orig, SetVersionNumber self)
        {
            orig(self);
            self.GetAttr<SetVersionNumber, UnityEngine.UI.Text>("textUi").text = "1.6.1.3";
        }
        private void OnUIManagerAwake(On.UIManager.orig_Awake orig, UIManager self)
        {
            orig(self);
            self.transform.GetChild(1).GetChild(2).GetChild(2).GetChild(0).gameObject.GetComponent<SpriteRenderer>().sprite = SPRITES["DlcList"];
        }

        private void OnAudioManagerApplyMusicCue(On.AudioManager.orig_ApplyMusicCue orig, AudioManager self, MusicCue musicCue, float delayTime, float transitionTime, bool applySnapshot)
        {
            // Insert Custom Audio into main MusicCue
            var infos = (MusicCue.MusicChannelInfo[]) musicCue.GetType().GetField("channelInfos", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(musicCue);

            var audioFieldInfo = typeof(MusicCue.MusicChannelInfo).GetField("clip", BindingFlags.NonPublic | BindingFlags.Instance);
            var origAudio = (AudioClip) audioFieldInfo.GetValue(infos[0]);
            if (origAudio != null && origAudio.name.Equals("Title"))
            {
                if (!ABManager.AssetBundles.ContainsKey(ABManager.Bundle.Sound))
                {
                    LoadMusic();
                }
                infos[(int) MusicChannels.Tension] = new MusicCue.MusicChannelInfo();
                audioFieldInfo.SetValue(infos[(int) MusicChannels.Tension], ABManager.AssetBundles[ABManager.Bundle.Sound].LoadAsset("MM_Aud"));
                
                var tmpStyle = MenuStyles.Instance.styles.First(x => x.styleObject.name.Contains("Pale_Court"));
                MenuStyles.Instance.SetStyle(MenuStyles.Instance.styles.ToList().IndexOf(tmpStyle), false);
            }
            musicCue.GetType().GetField("channelInfos", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(musicCue, infos);
            orig(self, musicCue, delayTime, transitionTime, applySnapshot);
        }

        private (string languageString, GameObject styleGo, int titleIndex, string unlockKey, string[] achievementKeys,
            MenuStyles.MenuStyle.CameraCurves cameraCurves, AudioMixerSnapshot musicSnapshot) AddPCMenuStyle(
                MenuStyles self)
        {
            Log("Start");

            #region Setting up materials

            var defaultSpriteMaterial = new Material(Shader.Find("Sprites/Default"));
            defaultSpriteMaterial.SetColor(Shader.PropertyToID("_Color"), new Color(1.0f, 1.0f, 1.0f, 1.0f));
            defaultSpriteMaterial.SetInt(Shader.PropertyToID("PixelSnap"), 0);
            defaultSpriteMaterial.SetFloat(Shader.PropertyToID("_EnableExternalAlpha"), 0.0f);
            defaultSpriteMaterial.SetInt(Shader.PropertyToID("_StencilComp"), 8);
            defaultSpriteMaterial.SetInt(Shader.PropertyToID("_Stencil"), 0);
            defaultSpriteMaterial.SetInt(Shader.PropertyToID("_StencilOp"), 0);
            defaultSpriteMaterial.SetInt(Shader.PropertyToID("_StencilWriteMask"), 255);
            defaultSpriteMaterial.SetInt(Shader.PropertyToID("_StencilReadMask"), 255);

            #endregion

            #region Loading assetbundle

            //ABManager.ResetBundle(ABManager.Bundle.TitleScreen);

            #endregion

            GameObject pcStyleGo = GameObject.Instantiate(ABManager.AssetBundles[ABManager.Bundle.TitleScreen].LoadAsset<GameObject>("Pale_Court_Style_1"));
            if (pcStyleGo == null)
            {
                pcStyleGo = new GameObject("Pale_Court");
            }

            foreach (var t in pcStyleGo.GetComponentsInChildren<Transform>())
            {
                t.gameObject.SetActive(true);
            }
            foreach (var t in pcStyleGo.GetComponentsInChildren<SpriteRenderer>())
            {
                t.materials = new Material[] { defaultSpriteMaterial };
            }

            pcStyleGo.transform.SetParent(self.gameObject.transform);
            pcStyleGo.transform.localPosition = new Vector3(0, -1.2f, 0);

            var cameraCurves = new MenuStyles.MenuStyle.CameraCurves();
            cameraCurves.saturation = 1f;
            cameraCurves.redChannel = new AnimationCurve();
            cameraCurves.redChannel.AddKey(new Keyframe(0f, 0f));
            cameraCurves.redChannel.AddKey(new Keyframe(1f, 1f));
            cameraCurves.greenChannel = new AnimationCurve();
            cameraCurves.greenChannel.AddKey(new Keyframe(0f, 0f));
            cameraCurves.greenChannel.AddKey(new Keyframe(1f, 1f));
            cameraCurves.blueChannel = new AnimationCurve();
            cameraCurves.blueChannel.AddKey(new Keyframe(0f, 0f));
            cameraCurves.blueChannel.AddKey(new Keyframe(1f, 1f));

            #region Fader

            // ToDo Make this part of the cursor

            //GameObject fader1 = new GameObject("Fader");
            //fader1.transform.SetParent(pcStyleGo.transform);
            //fader1.transform.localPosition = new Vector3(-6.125f, -1.75f, 1f);
            //fader1.transform.localScale = new Vector3(3, 5, 1);
            //var sr = fader1.AddComponent<SpriteRenderer>();
            //sr.sprite = SPRITES["Fader"];
            //sr.material = defaultSpriteMaterial;

            #endregion

            return ("UI_MENU_STYLE_PALE_COURT", pcStyleGo, paleCourtLogoId, "", null, cameraCurves, Resources.FindObjectsOfTypeAll<AudioMixer>().First(x => x.name == "Music").FindSnapshot("Tension Only"));
        }

        #endregion

        #region Enviroment Effects

        private GameObject ChangePsrTexture(GameObject o)
        {
            GameObject ret = GameObject.Instantiate(o, o.transform.parent);
            foreach (var psr in ret.GetComponentsInChildren<ParticleSystemRenderer>())
            {
                psr.material.mainTexture = SPRITES["Petal"].texture;
            }
            return ret;
        }
        private (int enviromentType, GameObject dashEffects) AddCustomDashEffectsHook(DashEffect self) => (7, ChangePsrTexture(self.dashGrass));
        private (int enviromentType, GameObject hardLandEffects) AddCustomHardLandEffectsHook(HardLandEffect self) => (7, ChangePsrTexture(self.grassObj));
        private (int enviromentType, GameObject jumpEffects) AddCustomJumpEffectsHook(JumpEffects self) => (7, ChangePsrTexture(self.grassEffects));
        private (int enviromentType, GameObject softLandEffects) AddCustomSoftLandEffectsHook(SoftLandEffect self) => (7, ChangePsrTexture(self.grassEffects));
        private (int enviromentType, GameObject runEffects) AddCustomRunEffectsHook(GameObject self) => (7, ChangePsrTexture(self.transform.GetChild(1).gameObject));

        #endregion

        private void LoadTitleScreen()
        {
            ABManager.Load(ABManager.Bundle.TitleScreen);
        }
        
        private void LoadMusic()
        {
            ABManager.Load(ABManager.Bundle.Sound);
        }

        private IEnumerator LoadBossBundles()
        {
            yield return ABManager.LoadAsync(ABManager.Bundle.GDryya);
            yield return ABManager.LoadAsync(ABManager.Bundle.GHegemol);
            yield return ABManager.LoadAsync(ABManager.Bundle.GIsma);
            yield return ABManager.LoadAsync(ABManager.Bundle.GZemer);
        }
        
        private IEnumerator LoadDep()
        {
            yield return ABManager.LoadAsync(ABManager.Bundle.GArenaDep);
            yield return ABManager.LoadAsync(ABManager.Bundle.OWArenaDep);
            yield return ABManager.LoadAsync(ABManager.Bundle.WSArenaDep);
            yield return ABManager.LoadAsync(ABManager.Bundle.WSArena);
            yield return ABManager.LoadAsync(ABManager.Bundle.GArenaHub);
            yield return ABManager.LoadAsync(ABManager.Bundle.GArenaHub2);
            yield return ABManager.LoadAsync(ABManager.Bundle.Misc);
            yield return ABManager.LoadAsync(ABManager.Bundle.GArenaH);
            yield return ABManager.LoadAsync(ABManager.Bundle.GArenaD);
            yield return ABManager.LoadAsync(ABManager.Bundle.GArenaZ);
            yield return ABManager.LoadAsync(ABManager.Bundle.GArenaI);
            yield return ABManager.LoadAsync(ABManager.Bundle.OWArenaD);
            yield return ABManager.LoadAsync(ABManager.Bundle.OWArenaZ);
            yield return ABManager.LoadAsync(ABManager.Bundle.OWArenaH);
            yield return ABManager.LoadAsync(ABManager.Bundle.OWArenaI);
            yield return ABManager.LoadAsync(ABManager.Bundle.GArenaIsma);

            Log("Finished bundling");
        }
        private object SetVariableHook(Type t, string key, object obj)
        {
            if (key == "statueStateIsma")
                LocalSaveData.CompletionIsma = (BossStatue.Completion)obj;
            else if (key == "statueStateDryya")
                LocalSaveData.CompletionDryya = (BossStatue.Completion)obj;
            else if (key == "statueStateZemer")
                LocalSaveData.CompletionZemer = (BossStatue.Completion)obj;
            else if (key == "statueStateZemer2")
                LocalSaveData.CompletionZemer2 = (BossStatue.Completion)obj;
            else if (key == "statueStateIsma2")
                LocalSaveData.CompletionIsma2 = (BossStatue.Completion)obj;
            else if (key == "statueStateHegemol")
                LocalSaveData.CompletionHegemol = (BossStatue.Completion)obj;
            return obj;
        }

        private object GetVariableHook(Type t, string key, object orig)
        {
            if (key == "statueStateIsma")
                return LocalSaveData.CompletionIsma;
            if (key == "statueStateDryya")
                return LocalSaveData.CompletionDryya;
            if (key == "statueStateZemer")
                return LocalSaveData.CompletionZemer;
            if (key == "statueStateZemer2")
                return LocalSaveData.CompletionZemer2;
            if (key == "statueStateIsma2")
                return LocalSaveData.CompletionIsma2;
            if (key == "statueStateHegemol")
                return LocalSaveData.CompletionHegemol;
            return orig;
        }

        private string LangGet(string key, string sheet, string orig)
        {
            if (langStrings.ContainsKey(key, sheet))
            {
                return langStrings.Get(key, sheet);
            }
            if (langStrings.ContainsKey(key, "Speech"))
            {
                return langStrings.Get(key, "Speech");
            }

            return orig;
            //return Language.Language.GetInternal(key, sheet);
        }

        private void SaveGame(SaveGameData data)
        {
            AddComponent();
        }

        private void AddComponent()
        {
            foreach (GameObject i in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (i.PrintSceneHierarchyPath() != "Hollow Shade\\Slash")
                    continue;
                
                preloadedGO["parryFX"] = i.LocateMyFSM("nail_clash_tink").GetAction<SpawnObjectFromGlobalPool>("No Box Down", 1).gameObject.Value;

                AudioClip aud = i
                    .LocateMyFSM("nail_clash_tink")
                    .GetAction<AudioPlayerOneShot>("Blocked Hit", 5)
                    .audioClips[0];

                var clashSndObj = new GameObject();
                var clashSnd = clashSndObj.AddComponent<AudioSource>();

                clashSnd.clip = aud;
                clashSnd.pitch = Random.Range(0.85f, 1.15f);

                Tink.TinkClip = aud;

                preloadedGO["ClashTink"] = clashSndObj;
                break;
            }

            //PlantChanger();
            //GameManager.instance.gameObject.AddComponent<ArenaFinder>();
            GameManager.instance.gameObject.AddComponent<OWArenaFinder>();
        }
        
        private void PlantChanger()
        {
            foreach (var trapType in new[] {"PTrap","PTurret"})
            {
                GameObject trap = preloadedGO[trapType];
                UObject.DestroyImmediate(trap.GetComponent<InfectedEnemyEffects>());
                var newDD = preloadedGO["WhiteDef"];
                var ddHit = newDD.GetComponent<EnemyHitEffectsUninfected>();
                var newHit = trap.AddComponent<EnemyHitEffectsUninfected>();
                foreach (FieldInfo fi in typeof(EnemyHitEffectsUninfected).GetFields(BindingFlags.Instance |
                    BindingFlags.NonPublic | BindingFlags.Public))
                {
                    if (fi.Name.Contains("Origin"))
                    {
                        newHit.effectOrigin = new Vector3(0f, 0.5f, 0f);
                        continue;
                    }

                    fi.SetValue(newHit, fi.GetValue(ddHit));
                }
                
                var newEff2 = trap.AddComponent<EnemyDeathEffectsUninfected>();
                var oldEff2 = newDD.GetComponent<EnemyDeathEffectsUninfected>();
                var oldEff3 = trap.GetComponent<EnemyDeathEffects>();
                
                foreach (FieldInfo fi in typeof(EnemyDeathEffects).GetFields(BindingFlags.Instance |
                                                                             BindingFlags.NonPublic |
                                                                             BindingFlags.Public | BindingFlags.Static))
                {
                    fi.SetValue(newEff2, fi.GetValue(oldEff3));
                }
                
                UObject.DestroyImmediate(trap.GetComponent<EnemyDeathEffects>());
                
                foreach (FieldInfo fi in typeof(EnemyDeathEffectsUninfected)
                    .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .Where(x => x.Name.IndexOf("corpse", StringComparison.OrdinalIgnoreCase) < 0))
                {
                    fi.SetValue(newEff2, fi.GetValue(oldEff2));
                }
                
                foreach (FieldInfo fi in typeof(EnemyDeathEffects).GetFields(BindingFlags.Instance |
                                                                             BindingFlags.NonPublic |
                                                                             BindingFlags.Public | BindingFlags.Static)
                    .Where(x => x.Name.IndexOf("corpse", StringComparison.OrdinalIgnoreCase) < 0))
                {
                    fi.SetValue((EnemyDeathEffects) newEff2, fi.GetValue((EnemyDeathEffects) oldEff2));
                }
                
                HealthManager hm = trap.GetComponent<HealthManager>();
                
                HealthManager hornHP = newDD.GetComponent<HealthManager>();
                foreach (FieldInfo fi in typeof(HealthManager).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                    .Where(x => x.Name.Contains("Prefab")))
                {
                    fi.SetValue(hm, fi.GetValue(hornHP));
                }
                foreach (PersistentBoolItem i in trap.GetComponentsInChildren<PersistentBoolItem>(true))
                {
                    UObject.Destroy(i);
                }
                GameObject hello = ((EnemyDeathEffects) newEff2).GetAttr<EnemyDeathEffects, GameObject>("corpsePrefab");
                if (trapType == "PTrap" && hello.transform.Find("Orange Puff") != null)
                {
                    UObject.Destroy(hello.transform.Find("Orange Puff").gameObject);
                }
                else
                {
                    foreach (var i in hello.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        var j = i.main;
                        j.startColor = new Color(0.16f, 0.5f, 0.003f);
                    }
                }
                newEff2.whiteWave = hello;
                GameObject fake = new GameObject();
                UObject.DontDestroyOnLoad(fake);
                newEff2.uninfectedDeathPt = fake;
                ((EnemyDeathEffects) newEff2).SetAttr("corpsePrefab", (GameObject) null);
                FiveKnights.preloadedGO[trapType] = trap;
            }
        }

        public SaveModSettings LocalSaveData { get; set; }
        
        public void OnLoadLocal(SaveModSettings s) => this.LocalSaveData = s;

        public SaveModSettings OnSaveLocal() => this.LocalSaveData;
    }
}