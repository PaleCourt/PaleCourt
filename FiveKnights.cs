using System;
using System.Reflection;
using Modding;
using JetBrains.Annotations;
using UnityEngine;
using UObject = UnityEngine.Object;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FiveKnights.BossManagement;
using FiveKnights.Isma;
using FiveKnights.Tiso;
using HutongGames.PlayMaker.Actions;
using SFCore.Utils;
using SFCore;
using UnityEngine.Audio;
using Random = UnityEngine.Random;
using UnityEngine.UI;
using FrogCore;
using SFCore.Generics;
using TMPro;
using Vasi;
using GetLanguageString = On.HutongGames.PlayMaker.Actions.GetLanguageString;
using System.Collections;

namespace FiveKnights
{

    [UsedImplicitly]
    public class FiveKnights : SaveSettingsMod<SaveModSettings>
    {
        private int paleCourtLogoId = -1;
        public static bool isDebug = true;
        public static Dictionary<string, AudioClip> Clips { get; } = new ();
        public static Dictionary<string, AnimationClip> AnimClips { get; } = new ();
        public static Dictionary<string, Material> Materials { get; } = new ();
        private LanguageCtrl langStrings { get; set; }
        public static Dictionary<string, GameObject> preloadedGO = new ();
        public static readonly Dictionary<string, Sprite> SPRITES = new ();
        public static FiveKnights Instance;
        public List<int> charmIDs;
        public static Dictionary<string, JournalHelper> journalEntries = new ();
        public static readonly string[] CharmKeys = { "PURITY", "LAMENT", "BOON", "BLOOM", "HONOUR" };
        public static string OS
        {
            get
            {
                return SystemInfo.operatingSystemFamily switch
                {
                    OperatingSystemFamily.Windows => "win",
                    //OperatingSystemFamily.Linux => "lin",
                    //OperatingSystemFamily.MacOSX => "mc",
                    //_ => null
                    _ => "win"
                };
            }
        }
        
        public FiveKnights() : base("Pale Court")
        {
            //On.GameCameras.Awake += (orig, self) => self.gameObject.AddComponent<PostProcessing>();
            
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
                        "LogoBlack",
                        "journal_dryya",
                        "journal_icon_dryya",
                        "journal_hegemol",
                        "journal_icon_hegemol",
                        "journal_isma",
                        "journal_icon_isma",
                        "journal_zemer",
                        "journal_icon_zemer",
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
            On.UIManager.Awake += ChangeDlcListSprite;
            On.SetVersionNumber.Start += ChangeVersionNumber;
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

            SFCore.AchievementHelper.AddAchievement("PALE_COURT_ISMA_ACH", SPRITES["ach_isma"], 
                "ISMA_ACH_TITLE", "ISMA_ACH_DESC", false);
            
            SFCore.AchievementHelper.AddAchievement("PALE_COURT_DRYYA_ACH", SPRITES["ach_dryya"], 
                "DRYYA_ACH_TITLE", "DRYYA_ACH_DESC", false);
            
            SFCore.AchievementHelper.AddAchievement("PALE_COURT_HEG_ACH", SPRITES["ach_heg"], 
                "HEG_ACH_TITLE", "HEG_ACH_DESC", false);

            SFCore.AchievementHelper.AddAchievement("PALE_COURT_ZEM_ACH", SPRITES["ach_zem"], 
                "ZEM_ACH_TITLE", "ZEM_ACH_DESC", false);
            
            SFCore.AchievementHelper.AddAchievement("PALE_COURT_PANTH_ACH", SPRITES["ach_panth"], 
                "PANTH_ACH_TITLE", "PANTH_ACH_DESC", true);

            #endregion

            #region Language & Hooks

            langStrings = new LanguageCtrl();

            ModHooks.BeforeSavegameSaveHook += SaveEntries;
            ModHooks.SetPlayerVariableHook += SetVariableHook;
            ModHooks.GetPlayerVariableHook += GetVariableHook;
            ModHooks.AfterSavegameLoadHook += (SaveGameData s) => StartGame();
            On.GameManager.StartNewGame += GameManager_StartNewGame;
            ModHooks.GetPlayerBoolHook += ModHooks_GetPlayerBool;
            ModHooks.SetPlayerBoolHook += ModHooks_SetPlayerBool;
            ModHooks.GetPlayerIntHook += ModHooks_GetPlayerInt;
            On.Language.Language.DoSwitch += SwitchLanguage;
            ModHooks.LanguageGetHook += LangGet;
            On.AudioManager.ApplyMusicCue += LoadPaleCourtMenuMusic;
            RewardRoom.Hook();
            Credits.Hook();

            #endregion

            #region Load Assetbundles
            LoadDep();
            LoadBossBundles();
            LoadCharms();

            #endregion
        }

		private void SwitchLanguage(On.Language.Language.orig_DoSwitch orig, Language.LanguageCode newLang)
        {
            orig(newLang);
            foreach (KeyValuePair<string, JournalHelper> keyValuePair in journalEntries)
            {
                string name = keyValuePair.Key;
                string prefix = "ENTRY_" + (name.Length == 4 ? "ISMA" : name.Substring(0, 3).ToUpper());
                JournalHelper journalHelper = keyValuePair.Value;
                if (journalHelper.playerData.killsremaining != 0)
                    journalHelper.playerData.killsremaining = 1;
                journalHelper.playerData.Hidden = true;
                if (langStrings.ContainsKey(prefix + "_LONGNAME", "Journal"))
                    journalHelper.nameStrings.name = langStrings.Get(prefix + "_LONGNAME", "Journal");
                if (langStrings.ContainsKey(prefix + "_DESC", "Journal"))
                    journalHelper.nameStrings.desc = langStrings.Get(prefix + "_DESC", "Journal");
                if (langStrings.ContainsKey(prefix + "_NOTE", "Journal"))
                    journalHelper.nameStrings.note = langStrings.Get(prefix + "_NOTE", "Journal");
                if (langStrings.ContainsKey(prefix + "_NAME", "Journal"))
                    journalHelper.nameStrings.shortname = langStrings.Get(prefix + "_NAME", "Journal");
            }
        }

        public override string GetVersion() => "6.16.2023-4";

        public override List<(string, string)> GetPreloadNames()
        {
            return new List<(string, string)>
            {
                ("GG_Hive_Knight", "Battle Scene/Hive Knight/Slash 1"),
                ("GG_Hollow_Knight", "Battle Scene/HK Prime"),
                ("GG_Hollow_Knight", "Battle Scene/HK Prime/Counter Flash"),
                ("GG_Hollow_Knight", "Battle Scene/Focus Blasts/HK Prime Blast/Blast"),
                ("Abyss_05", "Dusk Knight/Dream Enter 2"),
                ("Abyss_05","Dusk Knight/Idle Pt"),
                ("GG_Failed_Champion","False Knight Dream"),
                // The dust for when Zemer slams into walls
                ("GG_Failed_Champion","Ceiling Dust"),
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
                ("Dream_04_White_Defender", "Dream Fall Catcher"),
                ("Dream_Final_Boss", "Boss Control/Radiance/Death/Knight Split/Knight Ball"),
                ("Dream_Final_Boss", "Boss Control/Radiance"),
                ("GG_Nosk", "Mimic Spider"),
                // For the needle sphere snd Hornet makes (we can remove by updating soundbund to have the snd)
                ("GG_Hornet_1", "Boss Holder/Hornet Boss 1"),
                // For Isma's thorn walls
                ("Fungus3_13", "Thorn Collider"),
                // For Isma's gulka shield effect
                ("Abyss_05", "Dusk Knight/Shield"),
                // For charm collect/upgrade cutscene
                ("Room_Queen", "UI Msg Get WhiteCharm"),
                ("Room_Queen", "Queen Item"),
                // The next three are for the dream exit field in Reward Room
                ("White_Palace_03_hub", "dream_nail_base"),
                ("White_Palace_03_hub", "dream_beam_animation"),
                ("White_Palace_03_hub", "doorWarp"),
                // For the credits tablets in Dryya's arena
                ("Dream_Room_Believer_Shrine", "Plaque_statue_01 (1)"),

                ("Room_Mansion","Heart Piece Folder/Heart Piece"),
                ("Room_Mansion","Xun NPC/White Flash"),
                ("GG_Radiance", "Boss Control/Plat Sets/Hazard Plat/Radiant Plat Small (1)"),
                ("GG_Atrium", "gg_roof_door_pieces"),

                // For Lament VFX
                ("Tutorial_01", "_Props/Tut_tablet_top/Glows"),
                ("Ruins1_23", "Mage"),

                // For tiso explosions
                ("Fungus2_03", "Mushroom Turret (2)"),
                
                // Tram
                ("Crossroads_46", "Tram Main")

               
            };
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            #region Preloads
            Log("Storing GOs");
            preloadedGO["Tram"] = preloadedObjects["Crossroads_46"]["Tram Main"];
            
            preloadedGO["Heart"] = preloadedObjects["Room_Mansion"]["Heart Piece Folder/Heart Piece"];
            preloadedGO["WhiteFlashZem"] = preloadedObjects["Room_Mansion"]["Xun NPC/White Flash"];
            preloadedGO["RadPlat"] = preloadedObjects["GG_Radiance"]["Boss Control/Plat Sets/Hazard Plat/Radiant Plat Small (1)"];
            preloadedGO["ObjRaise"] = preloadedObjects["GG_Atrium"]["gg_roof_door_pieces"];
            preloadedGO["BombTurret"] = preloadedObjects["Fungus2_03"]["Mushroom Turret (2)"];
            
            preloadedGO["HornetSphere"] = preloadedObjects["GG_Hornet_1"]["Boss Holder/Hornet Boss 1"];
            preloadedGO["Nosk"] = preloadedObjects["GG_Nosk"]["Mimic Spider"];
            preloadedGO["Thorn Collider"] = preloadedObjects["Fungus3_13"]["Thorn Collider"];
            preloadedGO["Shield"] = preloadedObjects["Abyss_05"]["Dusk Knight/Shield"];
            preloadedGO["Statue"] = preloadedObjects["GG_Workshop"]["GG_Statue_ElderHu"];
            preloadedGO["DPortal"] = preloadedObjects["Abyss_05"]["Dusk Knight/Dream Enter 2"];
            preloadedGO["DPortal2"] = preloadedObjects["Abyss_05"]["Dusk Knight/Idle Pt"];
            preloadedGO["StatueMed"] = preloadedObjects["GG_Workshop"]["GG_Statue_TraitorLord"];
            preloadedGO["Bench"] = preloadedObjects["White_Palace_01"]["WhiteBench"];

            preloadedGO["TinkEff"] = preloadedObjects["White_Palace_01"]["White_ Spikes"];

            preloadedGO["Slash"] = preloadedObjects["GG_Hive_Knight"]["Battle Scene/Hive Knight/Slash 1"];
            preloadedGO["PV"] = preloadedObjects["GG_Hollow_Knight"]["Battle Scene/HK Prime"];
            preloadedGO["CounterFX"] = preloadedObjects["GG_Hollow_Knight"]["Battle Scene/HK Prime/Counter Flash"];
            preloadedGO["Blast"] = preloadedObjects["GG_Hollow_Knight"]["Battle Scene/Focus Blasts/HK Prime Blast/Blast"];
            preloadedGO["Kin"] = preloadedObjects["GG_Lost_Kin"]["Lost Kin"];
            preloadedGO["Mage"] = preloadedObjects["GG_Soul_Tyrant"]["Dream Mage Lord"];
            preloadedGO["fk"] = preloadedObjects["GG_Failed_Champion"]["False Knight Dream"];

            preloadedGO["Ceiling Dust"] = preloadedObjects["GG_Failed_Champion"]["Ceiling Dust"];
            
            preloadedGO["throne"] = preloadedObjects["White_Palace_09"]["White King Corpse/Throne Sit"];
            
            preloadedGO["PTurret"] = preloadedObjects["Fungus1_12"]["Plant Turret"];
            preloadedGO["Grass0"] = preloadedObjects["Fungus1_12"]["simple_grass"];
            preloadedGO["Grass2"] = preloadedObjects["Fungus1_12"]["green_grass_2"];
            preloadedGO["Grass3"] = preloadedObjects["Fungus1_12"]["green_grass_3"];
            preloadedGO["Grass1"] = preloadedObjects["Fungus1_12"]["green_grass_1 (1)"];
            preloadedGO["PTrap"] = preloadedObjects["Fungus1_19"]["Plant Trap"];

            preloadedGO["Dream Base"] = preloadedObjects["White_Palace_03_hub"]["dream_nail_base"];
            preloadedGO["Dream Beam"] = preloadedObjects["White_Palace_03_hub"]["dream_beam_animation"];
            preloadedGO["Dream Door Warp"] = preloadedObjects["White_Palace_03_hub"]["doorWarp"];

            preloadedGO["Backer Shrine"] = preloadedObjects["Dream_Room_Believer_Shrine"]["Plaque_statue_01 (1)"];

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

            preloadedGO["Knight Ball"] = preloadedObjects["Dream_Final_Boss"]["Boss Control/Radiance/Death/Knight Split/Knight Ball"];
            preloadedGO["Radiance"] = preloadedObjects["Dream_Final_Boss"]["Boss Control/Radiance"];

            preloadedGO["isma_stat"] = null;

            preloadedGO["CharmGet"] = preloadedObjects["Room_Queen"]["UI Msg Get WhiteCharm"];
            preloadedGO["Shiny"] = preloadedObjects["Room_Queen"]["Queen Item"];

            preloadedGO["SoulTwister"] = preloadedObjects["Ruins1_23"]["Mage"];
            preloadedGO["SoulEffect"] = preloadedObjects["Tutorial_01"]["_Props/Tut_tablet_top/Glows"];
			#endregion

			#region Journal Entries
			journalEntries.Add("Isma", new JournalHelper(SPRITES["journal_icon_isma"], SPRITES["journal_isma"], SaveSettings.IsmaEntryData, new JournalHelper.JournalNameStrings
            {
                name = langStrings.Get("ENTRY_ISMA_LONGNAME", "Journal"),
                desc = langStrings.Get("ENTRY_ISMA_DESC", "Journal"),
                note = langStrings.Get("ENTRY_ISMA_NOTE", "Journal"),
                shortname = langStrings.Get("ENTRY_ISMA_NAME", "Journal")
            }, "WhiteDefender", JournalHelper.EntryType.Dream, null, true, true));
            journalEntries.Add("Dryya", new JournalHelper(SPRITES["journal_icon_dryya"], SPRITES["journal_dryya"], SaveSettings.DryyaEntryData, new JournalHelper.JournalNameStrings
            {
                name = langStrings.Get("ENTRY_DRYYA_LONGNAME", "Journal"),
                desc = langStrings.Get("ENTRY_DRYYA_DESC", "Journal"),
                note = langStrings.Get("ENTRY_DRYYA_NOTE", "Journal"),
                shortname = langStrings.Get("ENTRY_DRYYA_NAME", "Journal")
            }, "WhiteDefender", JournalHelper.EntryType.Dream, null, true, true));
            journalEntries.Add("Hegemol", new JournalHelper(SPRITES["journal_icon_hegemol"], SPRITES["journal_hegemol"], SaveSettings.HegemolEntryData, new JournalHelper.JournalNameStrings
            {
                name = langStrings.Get("ENTRY_HEG_LONGNAME", "Journal"),
                desc = langStrings.Get("ENTRY_HEG_DESC", "Journal"),
                note = langStrings.Get("ENTRY_HEG_NOTE", "Journal"),
                shortname = langStrings.Get("ENTRY_HEG_NAME", "Journal")
            }, "WhiteDefender", JournalHelper.EntryType.Dream, null, true, true));
            journalEntries.Add("Zemer", new JournalHelper(SPRITES["journal_icon_zemer"], SPRITES["journal_zemer"], SaveSettings.ZemerEntryData, new JournalHelper.JournalNameStrings
            {
                name = langStrings.Get("ENTRY_ZEM_LONGNAME", "Journal"),
                desc = langStrings.Get("ENTRY_ZEM_DESC", "Journal"),
                note = langStrings.Get("ENTRY_ZEM_NOTE", "Journal"),
                shortname = langStrings.Get("ENTRY_ZEM_NAME", "Journal")
            }, "WhiteDefender", JournalHelper.EntryType.Dream, null, true, true));
            #endregion

            #region Charms
            charmIDs = CharmHelper.AddSprites(SPRITES["Mark_of_Purity"], SPRITES["Vessels_Lament"], SPRITES["Boon_of_Hallownest"], SPRITES["Abyssal_Bloom"]);

            //preloadedGO["Royal Aura"] = ABManager.AssetBundles[ABManager.Bundle.Charms].LoadAsset<GameObject>("Royal Aura");
            preloadedGO["Crest Anim Prefab"] = ABManager.AssetBundles[ABManager.Bundle.Charms].LoadAsset<GameObject>("CrestAnim");
            preloadedGO["Bloom Anim Prefab"] = ABManager.AssetBundles[ABManager.Bundle.Charms].LoadAsset<GameObject>("BloomAnim");
            preloadedGO["Bloom Sprite Prefab"] = ABManager.AssetBundles[ABManager.Bundle.Charms].LoadAsset<GameObject>("AbyssalBloom");
			#endregion

			Instance = this;
            UObject.Destroy(preloadedGO["DPortal"].LocateMyFSM("Check if midwarp or completed"));
            UObject.Destroy(preloadedGO["DPortal"].LocateMyFSM("FSM"));
            GameManager.instance.StartCoroutine(WaitForTitle());
            PlantChanger();

			GSPImport.AddFastDashPredicate((prev, next) => next.name == "White_Palace_09" && prev.name != "White_Palace_13");
			// GSPImport.AddInfiniteChallengeReturnScenePredicate((info) => info.SceneName is "White_Palace_09");

            Log("Initializing");
        }

        #region Make Text Readable

        private IEnumerator WaitForTitle()
        {
            yield return new WaitUntil(() => GameObject.Find("LogoTitle") != null);
            UIManager.EditMenus += UIManagerEditMenus;
        }

        private void UIManagerEditMenus()
        {
            foreach(var item in UIManager.instance.gameObject.GetComponentsInChildren<Text>(true))
            {
                var outline = item.gameObject.AddComponent<Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }
        }

        #endregion

        #region Menu Customization

        private void ChangeVersionNumber(On.SetVersionNumber.orig_Start orig, SetVersionNumber self)
        {
            orig(self);
            self.GetAttr<SetVersionNumber, UnityEngine.UI.Text>("textUi").text = "1.6.1.3";
        }

        private void ChangeDlcListSprite(On.UIManager.orig_Awake orig, UIManager self)
        {
            orig(self);
            self.gameObject.Find("Hidden_Dreams_Logo").GetComponent<SpriteRenderer>().sprite = SPRITES["DlcList"];
        }

        private void LoadPaleCourtMenuMusic(On.AudioManager.orig_ApplyMusicCue orig, AudioManager self, MusicCue musicCue, float delayTime, float transitionTime, bool applySnapshot)
        {
            // Insert Custom Audio into main MusicCue
            var infos = (MusicCue.MusicChannelInfo[]) musicCue.GetType().GetField("channelInfos", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(musicCue);
            
            var origAudio = infos[0].GetAttr<MusicCue.MusicChannelInfo, AudioClip>("clip");
            if (origAudio != null && origAudio.name.Equals("Title"))
            {
                if (!ABManager.AssetBundles.ContainsKey(ABManager.Bundle.Sound))
                {
                    LoadMusic();
                }
                infos[(int) MusicChannels.Tension] = new MusicCue.MusicChannelInfo();
                infos[(int) MusicChannels.Tension].SetAttr("clip", ABManager.AssetBundles[ABManager.Bundle.Sound].LoadAsset("MM_Aud"));
                // Don't sync this audio with the not-as-long normal main menu theme
                infos[(int) MusicChannels.Tension].SetAttr("sync", MusicChannelSync.ExplicitOff);
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

            return ("UI_MENU_STYLE_PALE_COURT", pcStyleGo, paleCourtLogoId, "", null, cameraCurves, Resources.FindObjectsOfTypeAll<AudioMixer>().First(x => x.name == "Music").FindSnapshot("Tension Only"));
        }

        #endregion

        #region Enviroment Effects

        private GameObject ChangePsrTexture(GameObject o)
        {
            GameObject ret = GameObject.Instantiate(o, o.transform.parent);
            foreach (var psr in ret.GetComponentsInChildren<ParticleSystemRenderer>())
            {
                Material newMaterial = UObject.Instantiate(psr.material);
                newMaterial.mainTexture = SPRITES["Petal"].texture;
                psr.material = newMaterial;
            }
            return ret;
        }
        GameObject valueAddCustomDashEffectsHookOnce = null;
        private (int enviromentType, GameObject dashEffects) AddCustomDashEffectsHook(DashEffect self)
        {
            if (valueAddCustomDashEffectsHookOnce == null)
            {
                valueAddCustomDashEffectsHookOnce = ChangePsrTexture(self.dashGrass);
            }
            return (7, valueAddCustomDashEffectsHookOnce);
        }
        GameObject valueAddCustomHardLandEffectsHookOnce = null;
        private (int enviromentType, GameObject hardLandEffects) AddCustomHardLandEffectsHook(HardLandEffect self)
        {
            if (valueAddCustomHardLandEffectsHookOnce == null)
            {
                valueAddCustomHardLandEffectsHookOnce = ChangePsrTexture(self.grassObj);
            }
            return (7, valueAddCustomHardLandEffectsHookOnce);
        }
        GameObject valueAddCustomJumpEffectsHookOnce = null;
        private (int enviromentType, GameObject jumpEffects) AddCustomJumpEffectsHook(JumpEffects self)
        {
            if (valueAddCustomJumpEffectsHookOnce == null)
            {
                valueAddCustomJumpEffectsHookOnce = ChangePsrTexture(self.grassEffects);
            }
            return (7, valueAddCustomJumpEffectsHookOnce);
        }
        GameObject valueAddCustomSoftLandEffectsHookOnce = null;
        private (int enviromentType, GameObject softLandEffects) AddCustomSoftLandEffectsHook(SoftLandEffect self)
        {
            if (valueAddCustomSoftLandEffectsHookOnce == null)
            {
                valueAddCustomSoftLandEffectsHookOnce = ChangePsrTexture(self.grassEffects);
            }
            return (7, valueAddCustomSoftLandEffectsHookOnce);
        }
        GameObject valueAddCustomRunEffectsHookOnce = null;
        private (int enviromentType, GameObject runEffects) AddCustomRunEffectsHook(GameObject self)
        {
            if (valueAddCustomRunEffectsHookOnce == null)
            {
                valueAddCustomRunEffectsHookOnce = ChangePsrTexture(self.transform.GetChild(1).gameObject);
            }
            return (7, valueAddCustomRunEffectsHookOnce);
        }

        #endregion

        private void LoadTitleScreen()
        {
            ABManager.Load(ABManager.Bundle.TitleScreen);
        }
        
        private void LoadMusic()
        {
            ABManager.Load(ABManager.Bundle.Sound);
        }

        private void LoadCharms()
        {
            ABManager.Load(ABManager.Bundle.Charms);
            ABManager.Load(ABManager.Bundle.CharmUnlock);

        }

        private void LoadBossBundles()
        {
            ABManager.Load(ABManager.Bundle.GDryya);
            ABManager.Load(ABManager.Bundle.GHegemol);
            ABManager.Load(ABManager.Bundle.GIsma);
            ABManager.Load(ABManager.Bundle.GZemer);

            ABManager.Load(ABManager.Bundle.Artist);
            ABManager.Load(ABManager.Bundle.TisoBund);
        }
        
        private void LoadDep()
        {
            ABManager.Load(ABManager.Bundle.GArenaDep);
            ABManager.Load(ABManager.Bundle.OWArenaDep);
            ABManager.Load(ABManager.Bundle.WSArenaDep);
            ABManager.Load(ABManager.Bundle.WSArena);
            ABManager.Load(ABManager.Bundle.GArenaHub);
            ABManager.Load(ABManager.Bundle.GArenaHub2);
            ABManager.Load(ABManager.Bundle.Misc);
            ABManager.Load(ABManager.Bundle.GArenaH);
            ABManager.Load(ABManager.Bundle.GArenaD);
            ABManager.Load(ABManager.Bundle.GArenaZ);
            ABManager.Load(ABManager.Bundle.GArenaI);
            ABManager.Load(ABManager.Bundle.OWArenaD);
            ABManager.Load(ABManager.Bundle.OWArenaZ);
            ABManager.Load(ABManager.Bundle.OWArenaH);
            ABManager.Load(ABManager.Bundle.OWArenaI);
            ABManager.Load(ABManager.Bundle.GArenaIsma);
            ABManager.Load(ABManager.Bundle.GReward);
            ABManager.Load(ABManager.Bundle.Credits);

            Log("Finished bundling");
        }

        private void SaveEntries(SaveGameData data)
        {
			SaveSettings.IsmaEntryData = journalEntries["Isma"].playerData;
			SaveSettings.DryyaEntryData = journalEntries["Dryya"].playerData;
			SaveSettings.HegemolEntryData = journalEntries["Hegemol"].playerData;
			SaveSettings.ZemerEntryData = journalEntries["Zemer"].playerData;
		}

        private object SetVariableHook(Type t, string key, object obj)
        {
            if (key == "statueStateIsma")
                SaveSettings.CompletionIsma = (BossStatue.Completion)obj;
            else if (key == "statueStateDryya")
                SaveSettings.CompletionDryya = (BossStatue.Completion)obj;
            else if (key == "statueStateZemer")
                SaveSettings.CompletionZemer = (BossStatue.Completion)obj;
            else if (key == "statueStateZemer2")
                SaveSettings.CompletionZemer2 = (BossStatue.Completion)obj;
            else if (key == "statueStateIsma2")
                SaveSettings.CompletionIsma2 = (BossStatue.Completion)obj;
            else if (key == "statueStateHegemol")
                SaveSettings.CompletionHegemol = (BossStatue.Completion)obj;
            else if (key == "statueStateMawlek2")
                SaveSettings.CompletionMawlek2 = (BossStatue.Completion)obj;
            return obj;
        }

        private object GetVariableHook(Type t, string key, object orig)
        {
            if (key == "statueStateIsma")
                return SaveSettings.CompletionIsma;
            if (key == "statueStateDryya")
                return SaveSettings.CompletionDryya;
            if (key == "statueStateZemer")
                return SaveSettings.CompletionZemer;
            if (key == "statueStateZemer2")
                return SaveSettings.CompletionZemer2;
            if (key == "statueStateIsma2")
                return SaveSettings.CompletionIsma2;
            if (key == "statueStateHegemol")
                return SaveSettings.CompletionHegemol;
            if (key == "statueStateMawlek2")
                return SaveSettings.CompletionMawlek2;
            return orig;
        }

        private bool ModHooks_GetPlayerBool(string target, bool orig)
        {
            if (target.StartsWith("gotCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (charmIDs.Contains(charmNum))
                {
                    return SaveSettings.gotCharms[charmIDs.IndexOf(charmNum)];
                }
            }
            if (target.StartsWith("newCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (charmIDs.Contains(charmNum))
                {
                    return SaveSettings.newCharms[charmIDs.IndexOf(charmNum)];
                }
            }
            if (target.StartsWith("equippedCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if(charmIDs != null && charmIDs.Contains(charmNum))
                {
                    return SaveSettings.equippedCharms[charmIDs.IndexOf(charmNum)];
                }
            }
            return orig;
        }

        private bool ModHooks_SetPlayerBool(string target, bool orig)
        {
            if (target.StartsWith("gotCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (charmIDs.Contains(charmNum))
                {
                    SaveSettings.gotCharms[charmIDs.IndexOf(charmNum)] = orig;
                    return orig;
                }
            }
            if (target.StartsWith("newCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (charmIDs.Contains(charmNum))
                {
                    SaveSettings.newCharms[charmIDs.IndexOf(charmNum)] = orig;
                    return orig;
                }
            }
            if (target.StartsWith("equippedCharm_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (charmIDs.Contains(charmNum))
                {
                    SaveSettings.equippedCharms[charmIDs.IndexOf(charmNum)] = orig;
                    return orig;
                }
            }
            return orig;
        }

        private int ModHooks_GetPlayerInt(string target, int orig)
        {
            if (target.StartsWith("charmCost_"))
            {
                int charmNum = int.Parse(target.Split('_')[1]);
                if (charmIDs.Contains(charmNum))
                {
                    return SaveSettings.charmCosts[charmIDs.IndexOf(charmNum)];
                }
            }
            return orig;
        }

        private string LangGet(string key, string sheet, string orig)
        {
            if (key.StartsWith("CHARM_DESC_") || key.StartsWith("CHARM_NAME_"))
            {
                int charmNum = int.Parse(key.Split('_')[2]);
                if (charmIDs.Contains(charmNum))
                {
                    key = key.Substring(0, 11) + CharmKeys[charmIDs.IndexOf(charmNum)];
                }
                else if (charmNum == 10 && SaveSettings.upgradedCharm_10)
                    key = key.Substring(0, 11) + CharmKeys[4];
            }
            return langStrings.ContainsKey(key, sheet) ? langStrings.Get(key, sheet) : orig;
        }

        private void GameManager_StartNewGame(On.GameManager.orig_StartNewGame orig, GameManager self, bool permaDeath, bool bossRush)
        {
            orig(self, permaDeath, bossRush);
            if(bossRush)
			{
                SaveSettings.gotCharms = new bool[] { true, true, true, true };
                SaveSettings.upgradedCharm_10 = true;
                SaveSettings.HasSeenWorkshopRaised = true;
                SaveSettings.CompletionIsma.isUnlocked = true;
                SaveSettings.CompletionIsma.hasBeenSeen = true;
                SaveSettings.CompletionDryya.isUnlocked = true;
                SaveSettings.CompletionDryya.hasBeenSeen = true;
                SaveSettings.CompletionZemer.isUnlocked = true;
                SaveSettings.CompletionZemer.hasBeenSeen = true;
                SaveSettings.CompletionHegemol.isUnlocked = true;
                SaveSettings.CompletionHegemol.hasBeenSeen = true;
            }
            StartGame();
        }

        private void StartGame()
        {
            GameManager.instance.gameObject.AddComponent<ArenaFinder>();
            GameManager.instance.gameObject.AddComponent<TisoFinder>();
            GameManager.instance.gameObject.AddComponent<OWArenaFinder>();
            GameManager.instance.gameObject.AddComponent<Amulets>();
            GameManager.instance.gameObject.AddComponent<AwardCharms>();

            journalEntries["Isma"].playerData = SaveSettings.IsmaEntryData;
            journalEntries["Dryya"].playerData = SaveSettings.DryyaEntryData;
            journalEntries["Hegemol"].playerData = SaveSettings.HegemolEntryData;
            journalEntries["Zemer"].playerData = SaveSettings.ZemerEntryData;

            // Obtain additional preloads
            foreach (GameObject i in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (i.name == "Gas Explosion Recycle M")
                {
                    Log("Adding explosion");
                    preloadedGO["Explosion"] = i.gameObject;
                }

                if (!(i.name == "Slash" && i.transform.parent != null && i.transform.parent.gameObject.name == "Hollow Shade"))
                    continue;

                Log("Adding parry tink");
                preloadedGO["parryFX"] = i.LocateMyFSM("nail_clash_tink").GetAction<SpawnObjectFromGlobalPool>("No Box Down", 1).gameObject.Value;

                AudioClip aud = i
                    .LocateMyFSM("nail_clash_tink")
                    .GetAction<AudioPlayerOneShot>("Blocked Hit", 5)
                    .audioClips[0];

                var clashSndObj = new GameObject();
                var clashSnd = clashSndObj.AddComponent<AudioSource>();

                clashSnd.clip = aud;
                clashSnd.pitch = Random.Range(0.85f, 1.15f);

                ParryTink.TinkClip = aud;
                Tink.TinkClip = aud;

                preloadedGO["ClashTink"] = clashSndObj;
                break;
            }
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
                   hello.transform.Find("Orange Puff").gameObject.AddComponent<ManipOrangePuff>();
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
    }
}
