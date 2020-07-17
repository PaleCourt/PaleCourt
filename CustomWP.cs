using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Modding;
using HutongGames.PlayMaker.Actions;
using System.Collections;
using ModCommon;
using UnityEngine.UI;

namespace FiveKnights
{
    public class  CustomWP : MonoBehaviour
    {
        private bool correctedTP;
        public static bool isFromGodhome;
        public static Boss boss;
        public static CustomWP Instance;
        public bool wonLastFight;
        public int lev;
        public enum Boss { Ogrim, Dryya, Isma, Hegemol, All, None, Mystic, Zemer };

        private void Start()
        {
            Instance = this;
            On.GameManager.EnterHero += GameManager_EnterHero;
            On.GameManager.BeginSceneTransition += GameManager_BeginSceneTransition;
            On.BossChallengeUI.LoadBoss_int_bool += BossChallengeUI_LoadBoss_int_bool;
            ModHooks.Instance.TakeHealthHook += Instance_TakeHealthHook;
            boss = Boss.None;
        }

        private int Instance_TakeHealthHook(int damage)
        {
            return damage;
        }

        private void GameManager_BeginSceneTransition(On.GameManager.orig_BeginSceneTransition orig, GameManager self, GameManager.SceneLoadInfo info)
        {
            if (info.SceneName == "White_Palace_09")
            {
                if (boss == Boss.Isma || boss == Boss.Ogrim)
                {
                    info.EntryGateName = "door_dreamReturnGGstatueStateIsma_GG_Statue_ElderHu(Clone)(Clone)";
                }
                else if (boss == Boss.Zemer || boss == Boss.Mystic)
                {
                    info.EntryGateName = "door_dreamReturnGGstatueStateZemer_GG_Statue_TraitorLord(Clone)(Clone)";
                }
                else
                {
                    info.EntryGateName = "door_dreamReturnGGstatueState" + boss + "_GG_Statue_TraitorLord(Clone)(Clone)";
                }
            }

            //isFromGodhome = (self.sceneName == "GG_Workshop");

            if (info.SceneName != "Dream_04_White_Defender" || correctedTP)
            {
                correctedTP = false;
                orig(self, info);
                return;
            }
            correctedTP = true; 
            ArenaFinder.defeats = PlayerData.instance.whiteDefenderDefeats;
            PlayerData.instance.whiteDefenderDefeats = 0;
            GameManager.instance.BeginSceneTransition(new GameManager.SceneLoadInfo
            {
                SceneName = "Dream_04_White_Defender",
                EntryGateName = "door1",
                EntryDelay = 0,
                Visualization = GameManager.SceneLoadVisualizations.Default,
                WaitForSceneTransitionCameraFade = false,
                PreventCameraFadeOut = true
            });
        }

        private void GameManager_EnterHero(On.GameManager.orig_EnterHero orig, GameManager self, bool additiveGateSearch)
        {
            if (self.sceneName == "White_Palace_09")
            {
                CreateStatues();
                HubRemove();
                AddLift();
                CreateGateway("left test2", new Vector2(14f, 94.4f), Vector2.zero, 
                              null, null, false, true, true, 
                              GameManager.SceneLoadVisualizations.Default);
                orig(self, false);
                SetupHub();
                Log("MADE CUSTOM WP");
                return;
            }
            orig(self, false);
        }

        private void SetupHub()
        {
            IEnumerator HubSet()
            {
                GameObject go = Instantiate(FiveKnights.preloadedGO["Warp"]);
                GameObject go2 = Instantiate(FiveKnights.preloadedGO["WarpBase"]);
                GameObject go3 = Instantiate(FiveKnights.preloadedGO["WarpAnim"]);
                go.SetActive(true);
                go2.SetActive(true);
                go3.SetActive(true);
                go.transform.position = new Vector3(24.5f,94.4f) - new Vector3(0f, 0.7f, 0f);
                go3.transform.position = go2.transform.position = new Vector3(go.transform.position.x + 0.1f, go.transform.position.y - 0.4f, -0.5f);
                var fsm = go.LocateMyFSM("Door Control");
                fsm.GetAction<BeginSceneTransition>("Change Scene", 3).sceneName = "GG_Workshop";
                fsm.GetAction<BeginSceneTransition>("Change Scene", 3).entryGateName = "door_dreamReturnGG_GG_Statue_Defender";
                yield return new WaitWhile(() => !HeroController.instance);
                Log("Checking if from godhome ");
                Log(isFromGodhome);
                if (isFromGodhome)
                {
                    HeroController.instance.transform.position = new Vector2(12f,94.4f);
                    //tk2dSpriteAnimator anim = HeroController.instance.gameObject.GetComponent<tk2dSpriteAnimator>();
                    //HeroController.instance.StopAnimationControl();
                    //yield return new WaitForSeconds(1.5f);
                    //HeroController.instance.StartAnimationControl();
                    //anim.Play("Exit Door To Idle");
                    //yield return null;
                    //yield return new WaitWhile(() => anim.IsPlaying("Exit Door To Idle"));
                    //HeroController.instance.RegainControl();
                }
            }

            StartCoroutine(HubSet());
        }

        private void CreateGateway(string gateName, Vector2 pos, Vector2 size, string toScene, string entryGate,
                                  bool right, bool left, bool onlyOut, GameManager.SceneLoadVisualizations vis)
        {
            GameObject gate = new GameObject(gateName);
            gate.transform.SetPosition2D(pos);
            var tp = gate.AddComponent<TransitionPoint>();
            if (!onlyOut)
            {
                var bc = gate.AddComponent<BoxCollider2D>();
                bc.size = size;
                bc.isTrigger = true;
                tp.targetScene = toScene;
                tp.entryPoint = entryGate;
            }
            tp.alwaysEnterLeft = left;
            tp.alwaysEnterRight = right;
            GameObject rm = new GameObject("Hazard Respawn Marker");
            rm.transform.parent = tp.transform;
            rm.transform.position = new Vector2(rm.transform.position.x - 3f, rm.transform.position.y);
            var tmp = rm.AddComponent<HazardRespawnMarker>();
            tp.respawnMarker = rm.GetComponent<HazardRespawnMarker>();
            tp.sceneLoadVisualization = vis;
        }

        private void CreateStatues()
        {
            //48f 98.75f Hegemol Top Left
            On.BossStatueLever.OnTriggerEnter2D -= BossStatueLever_OnTriggerEnter2D;
            On.BossStatueLever.OnTriggerEnter2D += BossStatueLever_OnTriggerEnter2D;
            GameObject stat = SetStatue(new Vector2(81.75f, 94.75f), new Vector2(-0.1f, 0.1f), new Vector2(0f,-0.5f), FiveKnights.preloadedGO["Statue"],
                                        "GG_White_Defender", FiveKnights.SPRITES[2], "ISMA_NAME", "ISMA_DESC", "statueStateIsma");
            GameObject stat2 = SetStatue(new Vector2(39.4f, 94.75f), new Vector2(-0.25f, -0.75f), new Vector2(-0f, -1f), FiveKnights.preloadedGO["StatueMed"],
                                        "GG_White_Defender", FiveKnights.SPRITES[3], "DRY_NAME", "DRY_DESC", "statueStateDryya");
            GameObject stat3 = SetStatue(new Vector2(73.3f, 98.75f), new Vector2(-0.13f, 2.03f), new Vector2(-0.3f, -0.8f), FiveKnights.preloadedGO["StatueMed"],
                                        "GG_White_Defender", FiveKnights.SPRITES[4], "ZEM_NAME", "ZEM_DESC", "statueStateZemer");
            GameObject stat4 = SetStatue(new Vector2(48f, 98.75f), new Vector2(-2f, 0.5f), new Vector2(-0.3f, -0.8f), FiveKnights.preloadedGO["StatueMed"],
                                        "GG_White_Defender", FiveKnights.SPRITES[5], "HEG_NAME", "HEG_DESC", "statueStateHegemol");
        }
        
        private Dictionary<string, StatueControl> StatueControls = new Dictionary<string, StatueControl>();
        private void BossStatueLever_OnTriggerEnter2D(On.BossStatueLever.orig_OnTriggerEnter2D orig, BossStatueLever self, Collider2D collision)
        {
            if (collision.tag != "Nail Attack") return;
            string namePD = self.gameObject.transform.parent.parent.GetComponent<BossStatue>().statueStatePD;
            string statName = namePD.Contains("Isma") ? "Isma" : "";
            statName = namePD.Contains("Zemer") ? "Zemer" : statName;
            if (statName == "")
            {
                orig(self, collision);
                return;
            }
            StatueControls[statName].StartLever(self);
        }

        private void BossChallengeUI_LoadBoss_int_bool(On.BossChallengeUI.orig_LoadBoss_int_bool orig, BossChallengeUI self, int level, bool doHideAnim)
        {
            string name = self.transform.Find("Panel").Find("BossName_Text").GetComponent<Text>().text;
            foreach (Boss b in Enum.GetValues(typeof(Boss)))
            {
                if (name.Contains(b.ToString()))
                {
                    boss = b;
                    if (b != Boss.Isma) break;
                    if (b != Boss.Zemer) break;
                }
            }
            lev = level;
            orig(self, level, doHideAnim);
        }

        private void HubRemove()
        {
            foreach (var i in FindObjectsOfType<SpriteRenderer>().Where(x => x != null && x.name.Contains("SceneBorder"))) Destroy(i);
            string[] arr = { "Breakable Wall Waterways", "black_fader","White_Palace_throne_room_top_0000_2", "White_Palace_throne_room_top_0001_1",
                             "Glow Response floor_ring large2 (1)", "core_extras_0006_wp", "msk_station",
                             "core_extras_0028_wp (12)", "wp_additions_01", "BlurPlane",
                             "Inspect Region (1)", "core_extras_0021_wp (4)", "core_extras_0021_wp (5)","core_extras_0021_wp (1)", 
                             "core_extras_0021_wp (6)", "core_extras_0021_wp (7)","core_extras_0021_wp (2)"};
            foreach (var i in FindObjectsOfType<GameObject>().Where(x => x.activeSelf))
            {
                foreach (string j in arr)
                {
                    if (i.name.Contains(j))
                    {
                        Destroy(i);
                    }
                }
            }
            foreach (var i in FindObjectsOfType<GameObject>().Where(x => x.name.Contains("abyss") || x.name.Contains("Abyss"))) Destroy(i);
        }

        private void AddLift()
        {
            IEnumerator FixArena()
            {
                yield return null;
                string[] removes = {"white_palace_wall_set_01 (10)", "white_palace_wall_set_01 (18)",
                    "_0028_white (4)", "_0028_white (3)"};
                foreach (var i in FindObjectsOfType<GameObject>()
                    .Where(x=>removes.Contains(x.name)))
                {
                    Destroy(i);
                }
                yield return null;
                GameObject go = Instantiate(FiveKnights.preloadedGO["hubfloor"]);
                GameObject go2 = GameObject.Find("Chunk 2 0");
                go.transform.Find("Chunk 2 0").GetComponent<MeshRenderer>().material =
                    go2.GetComponent<MeshRenderer>().material;
            }
            
            StartCoroutine(FixArena());
        }

        private GameObject SetStatue(Vector2 pos, Vector2 offset, Vector2 nameOffset,
                                    GameObject go, string sceneName, Sprite spr, 
                                    string name, string desc, string state)
        {
            //Used 56's pale prince code here
            GameObject statue = Instantiate(go);
            statue.transform.SetPosition3D(pos.x, pos.y, 0f);
            var scene = ScriptableObject.CreateInstance<BossScene>();
            scene.sceneName = sceneName;
            var bs = statue.GetComponent<BossStatue>();
            switch (name)
            {
                case "ISMA_NAME":
                    bs.StatueState = FiveKnights.Instance.Settings.CompletionIsma;
                    SetStatue2(statue, "GG_White_Defender", "statueStateIsma2","DD_ISMA_NAME", "DD_ISMA_DESC");
                    bs.DreamStatueState = FiveKnights.Instance.Settings.CompletionIsma2;
                    bs.SetDreamVersion(FiveKnights.Instance.Settings.AltStatueIsma, false, false);
                    break;
                case "DRY_NAME":
                    bs.StatueState = FiveKnights.Instance.Settings.CompletionDryya;
                    break;
                case "ZEM_NAME":
                    bs.StatueState = FiveKnights.Instance.Settings.CompletionZemer;
                    SetStatue2(statue, "GG_White_Defender", "statueStateZemer2","ZEM2_NAME","ZEM2_DESC");
                    bs.DreamStatueState = FiveKnights.Instance.Settings.CompletionZemer2;
                    bs.SetDreamVersion(FiveKnights.Instance.Settings.AltStatueZemer, false, false);
                    break;
                case "HEG_NAME":
                    bs.StatueState = FiveKnights.Instance.Settings.CompletionHegemol;
                    break;
            }
            bs.bossScene = scene;
            bs.statueStatePD = state;
            bs.SetPlaquesVisible(bs.StatueState.isUnlocked && bs.StatueState.hasBeenSeen);
            var details = new BossStatue.BossUIDetails();
            details.nameKey = details.nameSheet = name;
            details.descriptionKey = details.descriptionSheet = desc;
            bs.bossDetails = details;
            foreach (Transform i in statue.transform)
            {
                if (i.name.Contains("door"))
                {
                    i.name = "door_dreamReturnGG" + state;
                }
            }
            GameObject appearance = statue.transform.Find("Base").Find("Statue").gameObject;
            appearance.SetActive(true);
            SpriteRenderer sr = appearance.transform.Find("GG_statues_0006_5").GetComponent<SpriteRenderer>();
            sr.enabled = true;
            sr.sprite = spr;
            var scaleX = sr.transform.GetScaleX();
            var scaleY = sr.transform.GetScaleY();
            float scaler = state.Contains("Hegemol") ? 2f : 1.4f;
            sr.transform.localScale *= scaler;
            sr.transform.SetPosition3D(sr.transform.GetPositionX() + offset.x, sr.transform.GetPositionY() + offset.y, 2f);
            if (bs.StatueState.isUnlocked && bs.StatueState.hasBeenSeen)
            {
                Sprite sprite = spr;
                GameObject fakeStat = new GameObject("FakeStat");
                SpriteRenderer sr2 = fakeStat.AddComponent<SpriteRenderer>();
                sr2.sprite = sprite;
                fakeStat.transform.localScale = appearance.transform.Find("GG_statues_0006_5").localScale;
                fakeStat.transform.position = appearance.transform.Find("GG_statues_0006_5").position;
                if (state.Contains("Isma") || state.Contains("Zemer"))
                {
                    StatueControl sc = statue.transform.Find("Base").gameObject.AddComponent<StatueControl>();
                    sc.StatueName = state;
                    sc._bs = bs;
                    sc._sr = sr2;
                    sc._fakeStat = fakeStat;
                    if (state.Contains("Isma")) StatueControls["Isma"] = sc;
                    else StatueControls["Zemer"] = sc;
                }
            }
            var tmp = statue.transform.Find("Inspect").Find("Prompt Marker").position;
            statue.transform.Find("Inspect").Find("Prompt Marker").position = new Vector3(tmp.x + nameOffset.x, tmp.y + nameOffset.y, tmp.z);
            statue.transform.Find("Inspect").gameObject.SetActive(true);
            statue.transform.Find("Spotlight").gameObject.SetActive(true);
            statue.SetActive(true);
            wonLastFight = false;
            return statue;
        }

        private void SetStatue2(GameObject statue, string sceneN, string stateN, string key, string desc)
        {
            BossScene scene = ScriptableObject.CreateInstance<BossScene>();
            scene.sceneName = sceneN;

            BossStatue bs = statue.GetComponent<BossStatue>();
            bs.dreamBossScene = scene;
            bs.dreamStatueStatePD = stateN;

            /* 56's code { */
            Destroy(statue.FindGameObjectInChildren("StatueAlt"));
            GameObject displayStatue = bs.statueDisplay;
            GameObject alt = Instantiate
            (
                displayStatue,
                displayStatue.transform.parent,
                true
            );
            alt.SetActive(bs.UsingDreamVersion);
            alt.GetComponentInChildren<SpriteRenderer>(true).flipX = true;
            alt.name = "StatueAlt";
            bs.statueDisplayAlt = alt;
            /* } 56's code */
            BossStatue.BossUIDetails details = new BossStatue.BossUIDetails();
            details.nameKey = details.nameSheet = key;
            details.descriptionKey = details.descriptionSheet = desc;
            bs.dreamBossDetails = details;

            GameObject altLever = statue.FindGameObjectInChildren("alt_lever");
            altLever.SetActive(true);
            GameObject switchBracket = altLever.FindGameObjectInChildren("GG_statue_switch_bracket");
            switchBracket.SetActive(true);

            GameObject switchLever = altLever.FindGameObjectInChildren("GG_statue_switch_lever");
            switchLever.SetActive(true);

            BossStatueLever toggle = statue.GetComponentInChildren<BossStatueLever>();
            toggle.SetOwner(bs);
            toggle.SetState(true);
        }

        private void OnDestroy()
        {
            On.BossStatueLever.OnTriggerEnter2D -= BossStatueLever_OnTriggerEnter2D;
            On.GameManager.EnterHero -= GameManager_EnterHero;
            On.GameManager.BeginSceneTransition -= GameManager_BeginSceneTransition;
            On.BossChallengeUI.LoadBoss_int_bool -= BossChallengeUI_LoadBoss_int_bool;
            ModHooks.Instance.TakeHealthHook -= Instance_TakeHealthHook;
        }
        
        private static void Log(object o)
        {
            Modding.Logger.Log("[WP] " + o);
        }
    }
}
