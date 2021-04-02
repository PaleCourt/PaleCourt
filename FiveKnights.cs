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
using SFCore.Utils;
using UnityEngine.Audio;

namespace FiveKnights
{

    [UsedImplicitly]
    public class FiveKnights : Mod, ITogglableMod
    {
        private int paleCourtLogoId = -1;
        public static Dictionary<string, AudioClip> Clips { get; } = new Dictionary<string, AudioClip>();
        public static Dictionary<string, AudioClip> IsmaClips { get; } = new Dictionary<string, AudioClip>();
        public static Dictionary<string, Material> Materials { get; } = new Dictionary<string, Material>();
        private LanguageCtrl langStrings { get; set; }
        public static Dictionary<string, GameObject> preloadedGO = new Dictionary<string, GameObject>();
        public static readonly Dictionary<string, Sprite> SPRITES = new Dictionary<string, Sprite>();
        public static FiveKnights Instance;
        public SaveModSettings Settings = new SaveModSettings();
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
                    // Create sprite from texture
                    SPRITES.Add(res.Split('.')[2], Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)));
                    Log("Created sprite from embedded image: " + res + " at ind " + ++ind);
                }
            }

            #endregion
            #region Menu Customization
            
            On.UIManager.Awake += OnUIManagerAwake;
            On.SetVersionNumber.Start += OnSetVersionNumberStart;
            SFCore.MenuStyleHelper.Initialize();
            SFCore.MenuStyleHelper.AddMenuStyleHook += AddPCMenuStyle;
            SFCore.TitleLogoHelper.Initialize();
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

            SFCore.AchievementHelper.Initialize();
            
            SFCore.AchievementHelper.AddAchievement("IsmaAchiev", SPRITES["ach_isma"], 
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
        }

        public override string GetVersion() => "1.0.0.0";
        
        public override ModSettings SaveSettings
        {
            get => Settings;
            set => Settings = (SaveModSettings) value;
        }

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
                ("GG_Workshop","GG_Statue_ElderHu"),
                ("GG_Lost_Kin", "Lost Kin"),
                ("GG_Soul_Tyrant", "Dream Mage Lord"),
                ("GG_Workshop","GG_Statue_TraitorLord"),
                ("Room_Mansion","Heart Piece Folder/Heart Piece/Plink"),
                ("Fungus3_23_boss","Battle Scene/Wave 3/Mantis Traitor Lord"),
                ("GG_White_Defender", "Boss Scene Controller"),
                //("GG_White_Defender", "White Defender"),
                ("GG_Atrium_Roof", "Land of Storms Doors"),
                ("GG_White_Defender", "GG_Arena_Prefab/Godseeker Crowd"),
                ("Dream_04_White_Defender","_SceneManager"),
                ("Dream_04_White_Defender", "Battle Gate (1)"),
                ("Dream_04_White_Defender", "Dream Entry"),
                ("Dream_04_White_Defender", "White Defender"),
            };
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            MenuStyles.Instance.SetStyle(10, false);
            
            GameManager.instance.AwardAchievement("DryyaAchiev");
            GameManager.instance.AwardAchievement("HegAchiev");
            GameManager.instance.AwardAchievement("IsmaAchiev");
            GameManager.instance.AwardAchievement("ZemAchiev");
            GameManager.instance.AwardAchievement("PanthAchiev");

            Log("Storing GOs");
            preloadedGO["Statue"] = preloadedObjects["GG_Workshop"]["GG_Statue_ElderHu"];
            preloadedGO["DPortal"] = preloadedObjects["Abyss_05"]["Dusk Knight/Dream Enter 2"];
            preloadedGO["DPortal2"] = preloadedObjects["Abyss_05"]["Dusk Knight/Idle Pt"];
            preloadedGO["StatueMed"] = preloadedObjects["GG_Workshop"]["GG_Statue_TraitorLord"];
            preloadedGO["Bench"] = preloadedObjects["White_Palace_01"]["WhiteBench"];
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
            //preloadedGO["WhiteDef"] = preloadedObjects["GG_White_Defender"]["White Defender"];
            preloadedGO["StartDoor"] = preloadedObjects["GG_Atrium_Roof"]["Land of Storms Doors"];
            preloadedGO["Godseeker"] = preloadedObjects["GG_White_Defender"]["GG_Arena_Prefab/Godseeker Crowd"];
            
            preloadedGO["WhiteDef"] = preloadedObjects["Dream_04_White_Defender"]["White Defender"];
            preloadedGO["DreamEntry"] = preloadedObjects["Dream_04_White_Defender"]["Dream Entry"];
            preloadedGO["SMTest"] = preloadedObjects["Dream_04_White_Defender"]["_SceneManager"];
            preloadedGO["BattleGate"] = preloadedObjects["Dream_04_White_Defender"]["Battle Gate (1)"];
            preloadedGO["isma_stat"] = null;
            
            Instance = this;
            Log("Initalizing.");

            Unload();
            langStrings = new LanguageCtrl();
            GameManager.instance.StartCoroutine(LoadMusic());
            GameManager.instance.StartCoroutine(LoadDep());
            GameManager.instance.StartCoroutine(LoadBossBundles());
            
            ModHooks.Instance.SetPlayerVariableHook += SetVariableHook;
            ModHooks.Instance.GetPlayerVariableHook += GetVariableHook;
            ModHooks.Instance.AfterSavegameLoadHook += SaveGame;
            ModHooks.Instance.NewGameHook += AddComponent;
            
            ModHooks.Instance.LanguageGetHook += LangGet;
        }


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

            LoadTitleScreen();

            #endregion

            GameObject pcStyleGo = GameObject.Instantiate(ABManager.AssetBundles[ABManager.Bundle.TitleScreen].LoadAsset<GameObject>("Pale_Court_Style_1"));
            if (pcStyleGo == null)
            {
                pcStyleGo = new GameObject();
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
            UObject.DontDestroyOnLoad(pcStyleGo);

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

            return ("UI_MENU_STYLE_PALE_COURT", pcStyleGo, paleCourtLogoId, "", null, cameraCurves, null);
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
        
        private IEnumerator LoadMusic()
        {
            var ab = ABManager.Load(ABManager.Bundle.Sound);
            yield return null;
            AudioSource aud = GameObject.Find("Music").transform.Find("Main").GetComponent<AudioSource>();
            aud.clip = ab.LoadAsset<AudioClip>("MM_Aud");
            aud.Play();
            Log("Finished setting MM music");
        }

        private IEnumerator LoadBossBundles()
        {
            ABManager.Load(ABManager.Bundle.GDryya);
            yield return null;
            ABManager.Load(ABManager.Bundle.GHegemol);
            yield return null;
            ABManager.Load(ABManager.Bundle.GIsma);
            yield return null;
            ABManager.Load(ABManager.Bundle.GZemer);
        }
        
        private IEnumerator LoadDep()
        {

            ABManager.Load(ABManager.Bundle.GArenaDep);
            yield return null;
            ABManager.Load(ABManager.Bundle.OWArenaDep);
            yield return null;
            ABManager.Load(ABManager.Bundle.WSArenaDep);
            yield return null;
            ABManager.Load(ABManager.Bundle.WSArena);
            yield return null;
            ABManager.Load(ABManager.Bundle.GArenaHub);
            yield return null;
            ABManager.Load(ABManager.Bundle.GArenaHub2);
            yield return null;
            ABManager.Load(ABManager.Bundle.Misc);
            yield return null;
            ABManager.Load(ABManager.Bundle.GArenaH);
            yield return null;
            ABManager.Load(ABManager.Bundle.GArenaD);
            yield return null;
            ABManager.Load(ABManager.Bundle.GArenaZ);
            yield return null;
            ABManager.Load(ABManager.Bundle.GArenaI);
            yield return null;
            ABManager.Load(ABManager.Bundle.OWArenaD);
            yield return null;
            ABManager.Load(ABManager.Bundle.OWArenaZ);
            yield return null;
            ABManager.Load(ABManager.Bundle.OWArenaH);
            yield return null;
            ABManager.Load(ABManager.Bundle.OWArenaI);
            yield return null;
            ABManager.Load(ABManager.Bundle.GArenaIsma);

            Log("Finished bundling");
        }
        private object SetVariableHook(Type t, string key, object obj)
        {
            if (key == "statueStateIsma")
                Settings.CompletionIsma = (BossStatue.Completion)obj;
            else if (key == "statueStateDryya")
                Settings.CompletionDryya = (BossStatue.Completion)obj;
            else if (key == "statueStateZemer")
                Settings.CompletionZemer = (BossStatue.Completion)obj;
            else if (key == "statueStateZemer2")
                Settings.CompletionZemer2 = (BossStatue.Completion)obj;
            else if (key == "statueStateIsma2")
                Settings.CompletionIsma2 = (BossStatue.Completion)obj;
            else if (key == "statueStateHegemol")
                Settings.CompletionHegemol = (BossStatue.Completion)obj;
            return obj;
        }

        private object GetVariableHook(Type t, string key, object orig)
        {
            if (key == "statueStateIsma")
                return Settings.CompletionIsma;
            if (key == "statueStateDryya")
                return Settings.CompletionDryya;
            if (key == "statueStateZemer")
                return Settings.CompletionZemer;
            if (key == "statueStateZemer2")
                return Settings.CompletionZemer2;
            if (key == "statueStateIsma2")
                return Settings.CompletionIsma2;
            if (key == "statueStateHegemol")
                return Settings.CompletionHegemol;
            return orig;
        }

        private string LangGet(string key, string sheet)
        {
            if (langStrings.ContainsKey(key, sheet))
            {
                Log($"get thing {langStrings.Get(key, sheet)}");
                return langStrings.Get(key, sheet);
            }
            return Language.Language.GetInternal(key, sheet);
        }

        private void SaveGame(SaveGameData data)
        {
            AddComponent();
        }

        private void AddComponent()
        {
            //GameManager.instance.gameObject.AddComponent<ArenaFinder>();
            GameManager.instance.gameObject.AddComponent<OWArenaFinder>();
        }

        public void Unload()
        {
            ModHooks.Instance.AfterSavegameLoadHook -= SaveGame;
            ModHooks.Instance.NewGameHook -= AddComponent;
            ModHooks.Instance.LanguageGetHook -= LangGet;
            ModHooks.Instance.SetPlayerVariableHook -= SetVariableHook;
            ModHooks.Instance.GetPlayerVariableHook -= GetVariableHook;

            ABManager.UnloadAll();
            
            var x = GameManager.instance?.gameObject.GetComponent<ArenaFinder>();
            var y = GameManager.instance?.gameObject.GetComponent<OWArenaFinder>();
            if (x != null) UObject.Destroy(x);
            if (y != null) UObject.Destroy(y);
        }
    }
}