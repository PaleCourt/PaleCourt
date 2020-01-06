using System;
using System.Diagnostics;
using System.Reflection;
using Modding;
using JetBrains.Annotations;
using ModCommon;
using MonoMod.RuntimeDetour;
using UnityEngine.SceneManagement;
using UnityEngine;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using UObject = UnityEngine.Object;
using System.Collections.Generic;
using System.IO;

namespace FiveKnights
{
    [UsedImplicitly]
    public class FiveKnights : Mod, ITogglableMod
    {
        public static Dictionary<string, GameObject> preloadedGO = new Dictionary<string, GameObject>();
        public static readonly List<Sprite> SPRITES = new List<Sprite>();
        public static FiveKnights Instance;

        public override string GetVersion()
        {
            return "0.0.0.0";
        }

        public override List<(string, string)> GetPreloadNames()
        {
            return new List<(string, string)>
            {
                ("GG_Failed_Champion","False Knight Dream"),
                ("GG_Hive_Knight", "Battle Scene/Hive Knight/Slash 1"),
                ("GG_Hollow_Knight", "Battle Scene/HK Prime"),
                ("GG_Hollow_Knight", "Battle Scene/HK Prime/Counter Flash"),
                ("GG_Lost_Kin", "Lost Kin"),
                ("GG_Soul_Tyrant", "Dream Mage Lord"),
            };
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Storing GOs");
            preloadedGO["Slash"] = preloadedObjects["GG_Hive_Knight"]["Battle Scene/Hive Knight/Slash 1"];
            preloadedGO["PV"] = preloadedObjects["GG_Hollow_Knight"]["Battle Scene/HK Prime"];
            preloadedGO["CounterFX"] = preloadedObjects["GG_Hollow_Knight"]["Battle Scene/HK Prime/Counter Flash"];
            preloadedGO["Kin"] = preloadedObjects["GG_Lost_Kin"]["Lost Kin"];
            preloadedGO["Mage"] = preloadedObjects["GG_Soul_Tyrant"]["Dream Mage Lord"];
            preloadedGO["fk"] = preloadedObjects["GG_Failed_Champion"]["False Knight Dream"];

            Instance = this;
            Log("Initalizing.");

            Unload();
            ModHooks.Instance.AfterSavegameLoadHook += AfterSaveGameLoad;
            ModHooks.Instance.NewGameHook += AddComponent;
            ModHooks.Instance.LanguageGetHook += LangGet;

            int ind = 0;
            Assembly asm = Assembly.GetExecutingAssembly();
            foreach (string res in asm.GetManifestResourceNames())
            {
                if (!res.EndsWith(".png"))
                {
                    continue;
                }

                using (Stream s = asm.GetManifestResourceStream(res))
                {
                    if (s == null) continue;

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

        private string LangGet(string key, string sheettitle)
        {
            switch (key)
            {
                case "FK_NAME": return "The Great Five Knights";
                case "FK_DESC": return "Forgotten champion gods of a dead kingdom.";
                case "DRYYA_DIALOG_1_1": return "Must defend the Queen...";
                case "DRYYA_DIALOG_2_1": return "Allow none to enter the glade...";
                case "DRYYA_DIALOG_3_1": return "Protect...";
                case "DRYYA_DIALOG_4_1": return "Kin...seeks to find her?";
                case "DRYYA_DIALOG_5_1": return "Dryya Dialogue 5";
                case "ISMA_DREAM_1_1": return "Something about Sacrifice";
                case "ISMA_DREAM_2_1": return "Something about Grove";
                case "ISMA_DREAM_3_1": return "Something about Ogrim";
                default: return Language.Language.GetInternal(key, sheettitle);
            }
        }

        private void AfterSaveGameLoad(SaveGameData data) => AddComponent();

        private void AddComponent()
        {
            GameManager.instance.gameObject.AddComponent<ArenaFinder>();
        }

        public void Unload()
        {
            AudioListener.volume = 1f;
            AudioListener.pause = false;
            ModHooks.Instance.AfterSavegameLoadHook -= AfterSaveGameLoad;
            ModHooks.Instance.NewGameHook -= AddComponent;
            ModHooks.Instance.LanguageGetHook -= LangGet;

            // ReSharper disable once Unity.NoNullPropogation
            var x = GameManager.instance?.gameObject.GetComponent<ArenaFinder>();
            if (x == null) return;
            UObject.Destroy(x);
        }
    }
}