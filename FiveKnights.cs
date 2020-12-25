using System;
using System.CodeDom;
using System.Collections;
using System.Reflection;
using Modding;
using JetBrains.Annotations;
using UnityEngine;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using UObject = UnityEngine.Object;
using System.Collections.Generic;
using System.IO;
using ModCommon;
using On.HutongGames.PlayMaker.Actions;

namespace FiveKnights
{

    [UsedImplicitly]
    public class FiveKnights : Mod, ITogglableMod
    {
        public FiveKnights() : base("Pale Court") { }
        public static Dictionary<string, AudioClip> Clips { get; private set; }
        public static Dictionary<string, AudioClip> IsmaClips { get; private set; }

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
        
        public static Dictionary<string, GameObject> preloadedGO = new Dictionary<string, GameObject>();
        public static readonly List<Sprite> SPRITES = new List<Sprite>();
        public static FiveKnights Instance;
        
        public SaveModSettings Settings = new SaveModSettings();
        public override ModSettings SaveSettings
        {
            get => Settings;
            set => Settings = (SaveModSettings) value;
        }

        public override string GetVersion() => "0.0.0.0";

        public override List<(string, string)> GetPreloadNames()
        {
            return new List<(string, string)>
            {
                ("GG_Hive_Knight", "Battle Scene/Hive Knight/Slash 1"),
                ("GG_Hollow_Knight", "Battle Scene/HK Prime"),
                ("GG_Hollow_Knight", "Battle Scene/HK Prime/Counter Flash"),
                ("GG_Failed_Champion","False Knight Dream"),
                ("White_Palace_09","White Palace Lift"),
                ("White_Palace_09","White King Corpse/Throne Sit"),
                ("Fungus1_12","Plant Turret"),
                ("Fungus1_19", "Plant Trap"),
                ("White_Palace_01","WhiteBench"),
                ("GG_Workshop","GG_Statue_ElderHu"),
                ("GG_Lost_Kin", "Lost Kin"),
                ("GG_Soul_Tyrant", "Dream Mage Lord"),
                ("GG_Workshop","GG_Statue_ElderHu"),
                ("GG_Workshop","GG_Statue_TraitorLord"),
                ("White_Palace_03_hub","doorWarp"),
                ("White_Palace_03_hub","dream_beam_animation"),
                ("White_Palace_03_hub","dream_nail_base"),
                ("Abyss_05", "Dusk Knight/Dream Enter 2"),
                ("Abyss_05","Dusk Knight/Idle Pt"),
                ("Room_Mansion","Heart Piece Folder/Heart Piece/Plink"),
                ("Fungus3_23_boss","Battle Scene/Wave 3/Mantis Traitor Lord"),
                ("Fungus3_13","BlurPlane"),
                ("Fungus3_34","_Scenery/fung_lamp2 (1)/Active/haze2 (1)"),
            };
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Storing GOs");
            preloadedGO["Warp"] = preloadedObjects["White_Palace_03_hub"]["doorWarp"];
            preloadedGO["WarpAnim"] = preloadedObjects["White_Palace_03_hub"]["dream_beam_animation"];
            preloadedGO["WarpBase"] = preloadedObjects["White_Palace_03_hub"]["dream_nail_base"];
            preloadedGO["Statue"] = preloadedObjects["GG_Workshop"]["GG_Statue_ElderHu"];
            preloadedGO["StatueMed"] = preloadedObjects["GG_Workshop"]["GG_Statue_TraitorLord"];
            preloadedGO["Bench"] = preloadedObjects["White_Palace_01"]["WhiteBench"];
            preloadedGO["Slash"] = preloadedObjects["GG_Hive_Knight"]["Battle Scene/Hive Knight/Slash 1"];
            preloadedGO["PV"] = preloadedObjects["GG_Hollow_Knight"]["Battle Scene/HK Prime"];
            preloadedGO["CounterFX"] = preloadedObjects["GG_Hollow_Knight"]["Battle Scene/HK Prime/Counter Flash"];
            preloadedGO["Kin"] = preloadedObjects["GG_Lost_Kin"]["Lost Kin"];
            preloadedGO["Mage"] = preloadedObjects["GG_Soul_Tyrant"]["Dream Mage Lord"];
            preloadedGO["fk"] = preloadedObjects["GG_Failed_Champion"]["False Knight Dream"];
            preloadedGO["lift"] = preloadedObjects["White_Palace_09"]["White Palace Lift"];
            preloadedGO["throne"] = preloadedObjects["White_Palace_09"]["White King Corpse/Throne Sit"];
            preloadedGO["PTurret"] = preloadedObjects["Fungus1_12"]["Plant Turret"];
            preloadedGO["PTrap"] = preloadedObjects["Fungus1_19"]["Plant Trap"];
            preloadedGO["DPortal"] = preloadedObjects["Abyss_05"]["Dusk Knight/Dream Enter 2"];
            preloadedGO["DPortal2"] = preloadedObjects["Abyss_05"]["Dusk Knight/Idle Pt"];
            preloadedGO["VapeIn2"] = preloadedObjects["Room_Mansion"]["Heart Piece Folder/Heart Piece/Plink"];
            preloadedGO["Traitor"] = preloadedObjects["Fungus3_23_boss"]["Battle Scene/Wave 3/Mantis Traitor Lord"];
            preloadedGO["isma_stat"] = null;
            
            Instance = this;
            Log("Initalizing.");

            Unload();
            GameManager.instance.StartCoroutine(LoadMusic());
            
            ModHooks.Instance.SetPlayerVariableHook += SetVariableHook;
            ModHooks.Instance.GetPlayerVariableHook += GetVariableHook;
            ModHooks.Instance.AfterSavegameLoadHook += SaveGame;
            ModHooks.Instance.NewGameHook += AddComponent;
            ModHooks.Instance.LanguageGetHook += LangGet;

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
                    SPRITES.Add(Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)));

                    Log("Created sprite from embedded image: " + res + " at ind " + ++ind);
                }
            }
        }

        IEnumerator LoadMusic()
        {
            UObject[] clips = null;
            Assembly asm = Assembly.GetExecutingAssembly();
            Clips = new Dictionary<string, AudioClip>();
            IsmaClips = new Dictionary<string, AudioClip>();
            using (Stream s = asm.GetManifestResourceStream("FiveKnights.StreamingAssets.soundbund"))
            {
                var ab = AssetBundle.LoadFromStream(s);
                yield return null;
                clips = ab.LoadAllAssets();
            }

            if (clips == null)
            {
                Log("Failed to load clips");
                yield break;
            }

            foreach (var o in clips)
            {
                var clip = (AudioClip) o;
                if (clip.name.Contains("IsmaAud")) IsmaClips[clip.name] = clip;
                if (clip.name == "Aud_Isma") Clips["IsmaMusic"] = clip;
                else Clips[clip.name] = clip;
            }
            
            AudioSource aud = GameObject.Find("Music").transform.Find("Main").GetComponent<AudioSource>();
            aud.clip = Clips["MM_Aud"];
            aud.Play();
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

        private string LangGet(string key, string sheettitle)
        {
            switch (key)
            {
                case "ISMA_NAME": return "Kindly Isma";
                case "ISMA_DESC": return "Gentle god of moss and grove.";
                case "DD_ISMA_NAME": return "Loyal Ogrim & Kindly Isma";
                case "DD_ISMA_DESC": return "Loyal defender gods of land and beast.";
                case "HEG_NAME": return "Mighty Hegemol";
                case "HEG_DESC": return "Something something...";
                case "DRY_NAME": return "Fierce Dryya";
                case "DRY_DESC": return "Protective god of Root and King.";
                case "ZEM_NAME": return "Mysterious Ze'mer";
                case "ZEM2_NAME": return "Mystic Ze'mer";
                case "ZEM_DESC": return "Grieving god of lands beyond.";
                case "ZEM2_DESC": return "Strange god of a sacred land.";
                case "DRYYA_DIALOG_1_1": return "Must defend the Queen...";
                case "DRYYA_DIALOG_2_1": return "Allow none to enter the glade...";
                case "DRYYA_DIALOG_3_1": return "Protect...";
                case "DRYYA_DIALOG_4_1": return "Kin...seeks to find her?";
                case "DRYYA_DIALOG_5_1": return "Dryya Dialogue 5";
                case "ISMA_DREAM_1_1": return "Something about Sacrifice";
                case "ISMA_DREAM_2_1": return "Something about Grove";
                case "ISMA_DREAM_3_1": return "Something about Ogrim";
                case "FALSE_KNIGHT_D_1": return "Show me what you're made of!";
                case "FALSE_KNIGHT_D_2": return "Is that all you got?";
                case "FALSE_KNIGHT_D_3": return "Prove to me you're a champion!";
                case "ZEM_DREAM_1_1": return "Shoutout to 56 (can't remove this can you)";
                case "ZEM_DREAM_2_1": return "Shoutout to 56 (can't remove this can you)";
                case "ZEM_DREAM_3_1": return "Shoutout to 56 (can't remove this can you)";
                case "YN_THRONE": return "Answer the Champions' Call?";
                default: return Language.Language.GetInternal(key, sheettitle);
            }
        }

        private void SaveGame(SaveGameData data)
        {
            AddComponent();
        }

        private void AddComponent()
        {
            GameManager.instance.gameObject.AddComponent<ArenaFinder>();
        }

        public void Unload()
        {
            ModHooks.Instance.AfterSavegameLoadHook -= SaveGame;
            ModHooks.Instance.NewGameHook -= AddComponent;
            ModHooks.Instance.LanguageGetHook -= LangGet;
            ModHooks.Instance.SetPlayerVariableHook -= SetVariableHook;
            ModHooks.Instance.GetPlayerVariableHook -= GetVariableHook;

            var x = GameManager.instance?.gameObject.GetComponent<ArenaFinder>();
            if (x == null) return;
            UObject.Destroy(x);
        }
    }
}