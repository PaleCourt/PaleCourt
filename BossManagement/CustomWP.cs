using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Modding;
using SFCore.Utils;
using System.Collections;
using UnityEngine.UI;
using Vasi;
using HutongGames.PlayMaker.Actions;
using FiveKnights.Misc;
using Random = UnityEngine.Random;

namespace FiveKnights
{
    public class CustomWP : MonoBehaviour
    {
        public static bool isInGodhome;
        public static Boss boss;
        public static Boss prevBoss;
        public static CustomWP Instance;
        public static bool wonLastFight;
        public static int lev;
        public enum Boss { None, Ogrim, Dryya, Isma, Hegemol, All, Mystic, Ze };

        private void Start()
        {
            if(!isInGodhome) return;

            Instance = this;
            On.GameManager.EnterHero += GameManager_EnterHero;
            On.BossChallengeUI.LoadBoss_int_bool += BossChallengeUI_LoadBoss_int_bool;
            prevBoss = boss;
            boss = Boss.None;

            FiveKnights.preloadedGO["HubRoot"] = ABManager.AssetBundles[ABManager.Bundle.GArenaHub].LoadAsset<GameObject>("pale court gg throne aditions");
            GameObject root = Instantiate(FiveKnights.preloadedGO["HubRoot"]);
            FiveKnights.preloadedGO["ThroneCovered"] = root.transform.Find("throne_covered").gameObject;
            FiveKnights.preloadedGO["ThroneCovered"].SetActive(!FiveKnights.Instance.SaveSettings.UnlockedChampionsCall || !FiveKnights.Instance.SaveSettings.SeenChampionsCall);

            root.SetActive(true);
            foreach (var i in root.transform.GetComponentsInChildren<SpriteRenderer>(true))
            {
                i.material = new Material(Shader.Find("Sprites/Default"));
            }

            foreach (var go in FindObjectsOfType<GameObject>().Where(x => x.name.Contains("wp_rib_back")))
            {
                if (go.name != "wp_rib_back(3)" && go.name != "wp_rib_back(4)")
                {
                    Destroy(go);
                }
            }

            var del = GameObject.Find("core_extras_0024_wp(14)");
            if (del != null)
            {
                Log("Found del, deleting");
                Destroy(del);
            }
            
			// Move curtain to behind a little bit to reveal Isma statue lever
			foreach (GameObject i in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
			{
				switch (i.name)
				{
					case "core_extras_0025_wp (6)":
						i.transform.SetPositionZ(2.2f);
						break;
					case "core_extras_0025_wp":
						i.transform.SetPositionZ(3.34f);
						break;
					default:
						break;
				}
			}

            foreach (var i in FindObjectsOfType<GameObject>()
                .Where(x => x.name.Contains("new_cloud") 
                            && x.transform.position.x <= 25f))
            {
                Destroy(i);
            }

			Material[] blurPlaneMaterials = new Material[1];
			blurPlaneMaterials[0] = new Material(Shader.Find("UI/Blur/UIBlur"));
			blurPlaneMaterials[0].SetColor(Shader.PropertyToID("_TintColor"), new Color(1.0f, 1.0f, 1.0f, 0.0f));
			blurPlaneMaterials[0].SetFloat(Shader.PropertyToID("_Size"), 53.7f);
			blurPlaneMaterials[0].SetFloat(Shader.PropertyToID("_Vibrancy"), 0.2f);
			blurPlaneMaterials[0].SetInt(Shader.PropertyToID("_StencilComp"), 8);
			blurPlaneMaterials[0].SetInt(Shader.PropertyToID("_Stencil"), 0);
			blurPlaneMaterials[0].SetInt(Shader.PropertyToID("_StencilOp"), 0);
			blurPlaneMaterials[0].SetInt(Shader.PropertyToID("_StencilWriteMask"), 255);
			blurPlaneMaterials[0].SetInt(Shader.PropertyToID("_StencilReadMask"), 255);
			Log("Look for blur!");
            foreach(var i in FindObjectsOfType<GameObject>()
                .Where(x => x.name == "BlurPlane"))
            {
                Log("Found blur!");
                i.SetActive(false);
				i.GetComponent<MeshRenderer>().materials = blurPlaneMaterials;
				i.SetActive(true);
			}

            GameObject cameraLock = GameObject.Find("CameraLockArea (2)");
            if(cameraLock != null)
            {
                BoxCollider2D bc = cameraLock.GetComponent<BoxCollider2D>();
                bc.size = new Vector2(50f, bc.size.y);
                bc.offset = new Vector2(-10, bc.offset.y);
                Log("Fixed WP_09 camera at edges");
            }

            // This disables looking up and down, currently not sure of how else to accomplish it
            On.CameraController.UpdateTargetDestinationDelta += CameraControllerUpdateTarget;

            /*StartCoroutine(DebugMyThing());*/
        }

		private void CameraControllerUpdateTarget(On.CameraController.orig_UpdateTargetDestinationDelta orig, CameraController self)
		{
            self.lookOffset = 0f;
            orig(self);
		}

		/*private IEnumerator DebugMyThing()
        {
            GameObject heartOld = FiveKnights.preloadedGO["Heart"];
            GameObject startCircle = heartOld.transform.Find("Appear Trail").gameObject;
            GameObject whiteflashOld = FiveKnights.preloadedGO["WhiteFlashZem"];
            GameObject glowOld = heartOld.transform.Find("Get Anim").Find("Get Glow").gameObject;
            

            while (true)
            {

                Log("Waiting for R bitch");
                yield return new WaitWhile(() => !Input.GetKey(KeyCode.R));
                Log("Done waiting for R");

                GameObject startCircleNew = Instantiate(startCircle);
                startCircleNew.SetActive(true);
                startCircleNew.transform.position = HeroController.instance.transform.position;
                startCircleNew.GetComponent<ParticleSystem>().Play();

                yield return new WaitForSeconds(0.2f);
                
                Destroy(startCircleNew);
                
                GameObject whiteFlash = Instantiate(whiteflashOld);
                whiteFlash.SetActive(true);
                whiteFlash.transform.position = HeroController.instance.transform.position;
                Log("Created glow can you see it tho?");

                for (int i = 0; i < 5; i++)
                {
                    GameObject glow = Instantiate(glowOld);
                    glow.SetActive(true);
                    glow.transform.position = HeroController.instance.transform.position;
                    glow.transform.SetRotation2D(i * 90 + Random.Range(20,70));
                }
                
                yield return new WaitForSeconds(0.5f);
            }
        }*/

		private void GameManager_EnterHero(On.GameManager.orig_EnterHero orig, GameManager self, bool additiveGateSearch)
        {
            if (self.sceneName == "White_Palace_09")
            {
                foreach (var i in FindObjectsOfType<GameObject>().Where(x => x.name == "GG_extra_walls_0000_2_2_crack"))
                {
                    Destroy(i);
                }
                
                // Create extra floor
                GameObject go = Instantiate(FiveKnights.preloadedGO["hubfloor"]);
                GameObject go2 = GameObject.Find("Chunk 2 0");
                go.transform.Find("Chunk 2 0").GetComponent<MeshRenderer>().material =
                    go2.GetComponent<MeshRenderer>().material;
                
                GameObject crack = Instantiate(FiveKnights.preloadedGO["StartDoor"]);
                crack.SetActive(true);
                crack.transform.position = new Vector3(13.8f, 95.93f, 4.21f);
                crack.transform.localScale = new Vector3(1.33f, 1.02f, 0.87f);
                Destroy(crack.transform.Find("GG_secret_door").GetComponent<AudioSource>());
                GameObject secret = crack.transform.Find("GG_secret_door").gameObject;
                TransitionPoint tp = secret.transform.Find("door_Land_of_Storms").GetComponent<TransitionPoint>();
                tp.targetScene = "GG_Workshop";
                tp.entryPoint = "door_Land_of_Storms_return";
                crack.transform.Find("door_Land_of_Storms_return").gameObject.SetActive(true);
                secret.transform.Find("door_Land_of_Storms").gameObject.LocateMyFSM("Door Control")
                    .FsmVariables.FindFsmString("New Scene").Value = "GG_Workshop";
                secret.transform.Find("door_Land_of_Storms").gameObject.LocateMyFSM("Door Control")
                    .FsmVariables.FindFsmString("Entry Gate").Value = "door_Land_of_Storms_return";
                secret.LocateMyFSM("Deactivate").enabled = false;
                secret.SetActive(true);

                CreateStatues();
                HubRemove();
                AddLift();
                CreateGateway("door_dreamReturnGGTestingIt", new Vector2(60.5f, 98.4f), Vector2.zero, 
                    null, null, false, true, true, 
                    GameManager.SceneLoadVisualizations.Default);
                orig(self, false);
                //SetupHub();
                SetupThrone();
                Log("MADE CUSTOM WP");
                return;
            }
            orig(self, false);
        }

        private void SetupThrone()
        {
            GameObject throne = Instantiate(FiveKnights.preloadedGO["throne"]);
            throne.transform.position = new Vector3(60.5f, 97.7f, 0.2f);
            PlayMakerFSM fsm = throne.LocateMyFSM("Sit");

            GameObject effectParent = new GameObject("Throne Flash Effect");
            effectParent.transform.position = new Vector3(60.5f, 102.62f, -2f);
            effectParent.layer = (int)GlobalEnums.PhysLayers.HERO_DETECTOR;

            GameObject effect = Instantiate(FiveKnights.preloadedGO["Statue"].Find("Base").Find("GG_Statue_First_Appear"), 
                effectParent.transform);
            effect.name = "Throne First Appear";
            effect.transform.position = new Vector3(60.5f, 102.62f, -2f);

            Destroy(effect.Find("GG_statues_0027_26"));
            Destroy(effect.GetComponent<BossStatueFlashEffect>());
            effectParent.AddComponent<ThroneFlash>().throne = throne;

            effectParent.SetActive(true);

            IEnumerator Throne()
            {
                while (gameObject)
                {
                    yield return new WaitWhile(() => fsm.ActiveStateName != "Resting");
                    fsm.enabled = false;
                    PlayerData.instance.disablePause = true;
                    GameObject.Find("DialogueManager").LocateMyFSM("Box Open YN").SendEvent("BOX UP YN");
                    GameObject.Find("DialogueManager").SetActive(true);
                    GameObject.Find("Text YN").SetActive(true);
                    GameObject.Find("Text YN").GetComponent<DialogueBox>().StartConversation("YN_THRONE", "Speech");
                    PlayMakerFSM textYN = GameObject.Find("Text YN").LocateMyFSM("Dialogue Page Control");
                    textYN.FsmVariables.FindFsmInt("Toll Cost").Value = 0;
                    textYN.InsertCoroutine("Yes", 1, SaidYes);
                    textYN.InsertCoroutine("No", 1, SaidNo);
                    textYN.enabled = true;
                    while (textYN.ActiveStateName != "Ready for Input") yield return new WaitForEndOfFrame();
                    while (textYN.ActiveStateName == "Ready for Input") yield return new WaitForEndOfFrame();
                    yield return new WaitForSeconds(0.1f);
                }
            }

            IEnumerator SaidNo()
            {
                yield return null;
                PlayerData.instance.disablePause = false;
                PlayMakerFSM textYN = GameObject.Find("Text YN").LocateMyFSM("Dialogue Page Control");
                GameObject.Find("DialogueManager").LocateMyFSM("Box Open YN").SendEvent("BOX DOWN YN"); 
                fsm.enabled = true;
                textYN.enabled = true;
                fsm.SetState("Get Up");
                textYN.RemoveAction("No", 1);
                textYN.RemoveAction("Yes", 1);
            }
            
            IEnumerator SaidYes()
            {
                PlayerData.instance.disablePause = false;
                PlayMakerFSM textYN = GameObject.Find("Text YN").LocateMyFSM("Dialogue Page Control");
                GameObject.Find("DialogueManager").LocateMyFSM("Box Open YN").SendEvent("BOX DOWN YN");
				PlayMakerFSM pm = GameCameras.instance.tk2dCam.gameObject.LocateMyFSM("CameraFade");
				pm.SendEvent("FADE OUT");
				yield return new WaitForSeconds(0.5f);
                boss = Boss.All;
                ArenaFinder.defeats = PlayerData.instance.whiteDefenderDefeats;
                PlayerData.instance.whiteDefenderDefeats = 0;
                PlayerData.instance.respawnMarkerName = throne.name;
                GameManager.instance.BeginSceneTransition(new GameManager.SceneLoadInfo
                {
                    SceneName = "Dream_04_White_Defender",
                    EntryGateName = "door1",
                    Visualization = GameManager.SceneLoadVisualizations.Dream,
                    WaitForSceneTransitionCameraFade = false,
                    EntryDelay = 0f
                });

                textYN.RemoveAction("Yes", 1);
                textYN.RemoveAction("No", 1);
                textYN.enabled = true;
                fsm.enabled = true;
            }

            StartCoroutine(Throne());
            GameObject.Find("throne").transform.Find("core_extras_0029_wp").gameObject.SetActive(FiveKnights.Instance.SaveSettings.UnlockedChampionsCall && FiveKnights.Instance.SaveSettings.SeenChampionsCall);
            throne.SetActive(FiveKnights.Instance.SaveSettings.UnlockedChampionsCall && FiveKnights.Instance.SaveSettings.SeenChampionsCall);
            FiveKnights.preloadedGO["ThroneCovered"].SetActive(!FiveKnights.Instance.SaveSettings.UnlockedChampionsCall || !FiveKnights.Instance.SaveSettings.SeenChampionsCall);
        }

		private void HubRemove()
        {
            foreach(var i in FindObjectsOfType<SpriteRenderer>().Where(x => x != null && x.name.Contains("SceneBorder"))) Destroy(i);
            string[] arr = { "Breakable Wall Waterways", "black_fader","White_Palace_throne_room_top_0000_2", "White_Palace_throne_room_top_0001_1",
                             "Glow Response floor_ring large2 (1)", "core_extras_0006_wp", "msk_station",
                             "core_extras_0028_wp (12)", "wp_additions_01",
                             "Inspect Region (1)", "core_extras_0021_wp (4)", "core_extras_0021_wp (5)","core_extras_0021_wp (1)",
                             "core_extras_0021_wp (6)", "core_extras_0021_wp (7)","core_extras_0021_wp (2)", "Darkness Region"};
            foreach(var i in FindObjectsOfType<GameObject>().Where(x => x.activeSelf))
            {
                foreach(string j in arr)
                {
                    if(i.name.Contains(j))
                    {
                        Destroy(i);
                    }
                }
            }
            foreach(var i in FindObjectsOfType<GameObject>().Where(x => x.name.Contains("abyss") || x.name.Contains("Abyss"))) Destroy(i);
        }

        private void AddLift()
        {
            IEnumerator FixArena()
            {
                yield return null;
                string[] removes = {"white_palace_wall_set_01 (10)", "white_palace_wall_set_01 (18)",
                    "_0028_white (4)", "_0028_white (3)"};
                foreach(var i in FindObjectsOfType<GameObject>()
                    .Where(x => removes.Contains(x.name)))
                {
                    Destroy(i);
                }
                yield return null;
            }

            StartCoroutine(FixArena());
        }

        private void BossChallengeUI_LoadBoss_int_bool(On.BossChallengeUI.orig_LoadBoss_int_bool orig, BossChallengeUI self, int level, bool doHideAnim)
        {
            DontDestroyOnLoad(GameManager.instance);

            string title = self.transform.Find("Panel").Find("BossName_Text").GetComponent<Text>().text;
            foreach(Boss b in Enum.GetValues(typeof(Boss)))
            {
                if(title.Contains(b.ToString()))
                {
                    boss = b;
                    if(b != Boss.Isma) break;
                    if(b != Boss.Ze) break;
                }
            }
            lev = level;
            orig(self, level, doHideAnim);
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

        private Dictionary<string, StatueControl> StatueControls = new Dictionary<string, StatueControl>();
        private void CreateStatues()
        {
            // To manually change statues for Isma/Ze'mer
            On.BossStatueLever.OnTriggerEnter2D -= BossStatueLever_OnTriggerEnter2D;
            On.BossStatueLever.OnTriggerEnter2D += BossStatueLever_OnTriggerEnter2D;
            // To manually enable the statue sprite when unlocking
            On.BossStatueFlashEffect.FlashApex -= BossStatueFlashEffect_FlashApex;
            On.BossStatueFlashEffect.FlashApex += BossStatueFlashEffect_FlashApex;
            // To manually change the locked dialogue box
            On.HutongGames.PlayMaker.Actions.CallMethodProper.OnEnter -= CallMethodProperOnEnter;
            On.HutongGames.PlayMaker.Actions.CallMethodProper.OnEnter += CallMethodProperOnEnter;
            // Do win effect
            GameManager.instance.OnFinishedEnteringScene -= GMOnFinishedEnteringScene;
            if(wonLastFight) GameManager.instance.OnFinishedEnteringScene += GMOnFinishedEnteringScene;
            else lev = 0;
            wonLastFight = false; 

            SetStatue(new Vector2(81.75f, 94.15f), new Vector2(0.5f, 0.1f), new Vector2(0f,-0.5f), FiveKnights.preloadedGO["Statue"],
                                        ArenaFinder.IsmaScene, FiveKnights.SPRITES["Isma"], "ISMA_NAME", "ISMA_DESC", "statueStateIsma");
            SetStatue(new Vector2(39.4f, 94.15f), new Vector2(-0.25f, -0.75f), new Vector2(0f, -1f), FiveKnights.preloadedGO["StatueMed"],
                                        ArenaFinder.DryyaScene, FiveKnights.SPRITES["Dryya"], "DRYYA_NAME", "DRYYA_DESC", "statueStateDryya");
            SetStatue(new Vector2(73.3f, 98.25f), new Vector2(-0.13f, 1.3f), new Vector2(0f, -1.7f), FiveKnights.preloadedGO["StatueMed"],
                                        ArenaFinder.ZemerScene, FiveKnights.SPRITES["Zemer"], "ZEM_NAME", "ZEM_DESC", "statueStateZemer");
            SetStatue(new Vector2(48f, 98.25f), new Vector2(-0.2f, 0.1f), new Vector2(0f, -0.8f), FiveKnights.preloadedGO["StatueMed"],
                                        ArenaFinder.HegemolScene, FiveKnights.SPRITES["Hegemol"], "HEG_NAME", "HEG_DESC", "statueStateHegemol");
        }

		private void BossStatueLever_OnTriggerEnter2D(On.BossStatueLever.orig_OnTriggerEnter2D orig, BossStatueLever self, Collider2D collision)
        {
            if(collision.tag != "Nail Attack") return;
            string namePD = self.gameObject.transform.parent.parent.GetComponent<BossStatue>().statueStatePD;
            if(namePD.Contains("Isma"))
            {
                StatueControls["Isma"].StartLever(self);
            }
            else if(namePD.Contains("Zemer"))
            {
                StatueControls["Zemer"].StartLever(self);
            }
            else
            {
                orig(self, collision);
            }
        }

        private void BossStatueFlashEffect_FlashApex(On.BossStatueFlashEffect.orig_FlashApex orig, BossStatueFlashEffect self)
        {
            BossStatue bs = Mirror.GetField<BossStatueFlashEffect, BossStatue>(self, "parentStatue");
            switch(bs.bossScene.sceneName)
            {
                case ArenaFinder.IsmaScene:
                    bs.gameObject.Find("FakeStatIsma").SetActive(true);
                    break;
                case ArenaFinder.DryyaScene:
                    bs.gameObject.Find("FakeStatDryya").SetActive(true);
                    break;
                case ArenaFinder.ZemerScene:
                    bs.gameObject.Find("FakeStatZemer").SetActive(true);
                    break;
                case ArenaFinder.HegemolScene:
                    bs.gameObject.Find("FakeStatHegemol").SetActive(true);
                    break;
            }
            orig(self);
        }

        private void CallMethodProperOnEnter(On.HutongGames.PlayMaker.Actions.CallMethodProper.orig_OnEnter orig, CallMethodProper self)
		{
            if(self.Fsm.Name == "inspect_region" && self.Fsm.GameObject.name == "Inspect_Locked" && self.State.Name == "Read")
			{
                BossStatue bs = self.Fsm.GameObject.transform.parent.gameObject.GetComponent<BossStatue>();
                switch(bs.bossScene.sceneName)
                {
                    case ArenaFinder.IsmaScene:
                        self.Fsm.Variables.FindFsmString("Game Text Convo").Value = "ISMA_LOCKED_DESC";
                        break;
                    case ArenaFinder.DryyaScene:
                        self.Fsm.Variables.FindFsmString("Game Text Convo").Value = "DRYYA_LOCKED_DESC";
                        break;
                    case ArenaFinder.ZemerScene:
                        self.Fsm.Variables.FindFsmString("Game Text Convo").Value = "ZEM_LOCKED_DESC";
                        break;
                    case ArenaFinder.HegemolScene:
                        self.Fsm.Variables.FindFsmString("Game Text Convo").Value = "HEG_LOCKED_DESC";
                        break;
                }
            }
            orig(self);
		}

        private void GMOnFinishedEnteringScene() => DoWinEffect();

        private void DoWinEffect()
		{
            GameObject plaque = null;
            switch(prevBoss)
            {
                case Boss.Isma:
                    if(FiveKnights.Instance.SaveSettings.CompletionIsma2.isUnlocked)
					{
                        plaque = GameObject.Find("GG_Statue_Isma").Find("Base").Find("Plaque").Find("Plaque_Trophy_Left");
                    }
                    else plaque = GameObject.Find("GG_Statue_Isma").Find("Base").Find("Plaque").Find("Plaque_Trophy_Centre");
                    break;
                case Boss.Ogrim:
                    plaque = GameObject.Find("GG_Statue_Isma").Find("Base").Find("Plaque").Find("Plaque_Trophy_Right");
                    break;
                case Boss.Dryya:
                    plaque = GameObject.Find("GG_Statue_Dryya").Find("Base").Find("Plaque").Find("Plaque_Trophy_Centre");
                    break;
                case Boss.Ze:
                    if(FiveKnights.Instance.SaveSettings.CompletionZemer2.isUnlocked)
                    {
                        plaque = GameObject.Find("GG_Statue_Zemer").Find("Base").Find("Plaque").Find("Plaque_Trophy_Left");
                    }
                    else plaque = GameObject.Find("GG_Statue_Zemer").Find("Base").Find("Plaque").Find("Plaque_Trophy_Centre");
                    break;
                case Boss.Mystic:
                    plaque = GameObject.Find("GG_Statue_Zemer").Find("Base").Find("Plaque").Find("Plaque_Trophy_Right");
                    break;
                case Boss.Hegemol:
                    plaque = GameObject.Find("GG_Statue_Hegemol").Find("Base").Find("Plaque").Find("Plaque_Trophy_Centre");
                    break;
            }
            if(plaque != null)
            {
                plaque.GetComponent<BossStatueTrophyPlaque>().tierCompleteEffectDelay = 0.5f;
                plaque.GetComponent<BossStatueTrophyPlaque>().DoTierCompleteEffect((BossStatueTrophyPlaque.DisplayType)lev);
            }
            lev = 0;
        }

        // WARNING: THIS METHOD IS EXTREMELY JANK, PROCEED AT YOUR OWN RISK
        private GameObject SetStatue(Vector2 pos, Vector2 offset, Vector2 nameOffset,
                                    GameObject go, string sceneName, Sprite spr,
                                    string name, string desc, string state)
        {
            // Used 56's pale prince code here
            // Set statue info
            GameObject statue = Instantiate(go);
            statue.name = "GG_Statue_" + state.Substring(11);
            statue.transform.SetPosition3D(pos.x, pos.y, 0f);
            var scene = ScriptableObject.CreateInstance<BossScene>();
            scene.sceneName = sceneName;
            var bs = statue.GetComponent<BossStatue>();
            bs.bossScene = scene;
            bs.statueStatePD = null;

            switch (name)
            {
                case "ISMA_NAME":
                    if(FiveKnights.Instance.SaveSettings.CompletionIsma.isUnlocked)
                    {
                        bs.statueStatePD = state;
                        bs.StatueState = FiveKnights.Instance.SaveSettings.CompletionIsma;
                    }
                    if(FiveKnights.Instance.SaveSettings.CompletionIsma2.isUnlocked)
                    {
                        SetStatue2(statue, "GG_White_Defender", "statueStateIsma2", "DD_ISMA_NAME", "DD_ISMA_DESC");
                        bs.DreamStatueState = FiveKnights.Instance.SaveSettings.CompletionIsma2;
                        bs.SetDreamVersion(FiveKnights.Instance.SaveSettings.AltStatueIsma, false, false);
                    }
                    break;
                case "DRYYA_NAME":
                    if(FiveKnights.Instance.SaveSettings.CompletionDryya.isUnlocked)
                    {
                        bs.statueStatePD = state;
                        bs.StatueState = FiveKnights.Instance.SaveSettings.CompletionDryya;
                    }
                    break;
                case "ZEM_NAME":
                    if(FiveKnights.Instance.SaveSettings.CompletionZemer.isUnlocked)
					{
                        bs.statueStatePD = state;
                        bs.StatueState = FiveKnights.Instance.SaveSettings.CompletionZemer;
					}
                    if(FiveKnights.Instance.SaveSettings.CompletionZemer2.isUnlocked)
					{
                        SetStatue2(statue, sceneName, "statueStateZemer2", "ZEM2_NAME", "ZEM2_DESC");
                        bs.DreamStatueState = FiveKnights.Instance.SaveSettings.CompletionZemer2;
                        bs.SetDreamVersion(FiveKnights.Instance.SaveSettings.AltStatueZemer, false, false);
                    }
                    break;
                case "HEG_NAME":
                    if(FiveKnights.Instance.SaveSettings.CompletionHegemol.isUnlocked)
                    {
                        bs.statueStatePD = state;
                        bs.StatueState = FiveKnights.Instance.SaveSettings.CompletionHegemol;
                    }
                    break;
            }
            bs.SetPlaquesVisible(bs.StatueState.isUnlocked && bs.StatueState.hasBeenSeen);

            // Set statue UI details
            var details = new BossStatue.BossUIDetails();
            details.nameKey = name;
            details.nameSheet = "Speech";
            details.descriptionKey = desc;
            details.descriptionSheet = "Speech";
            bs.bossDetails = details;
            foreach (Transform i in statue.transform)
            {
                if (i.name.Contains("door"))
                {
                    i.name = "door_dreamReturnGG" + state;
                }
            }

            // Get info from old statues
            GameObject appearance = statue.transform.Find("Base").Find("Statue").gameObject;
            appearance.SetActive(true);
            SpriteRenderer sr = appearance.transform.Find("GG_statues_0006_5").GetComponent<SpriteRenderer>();
            sr.enabled = false;
            sr.sprite = spr;
            float scaler = state.Contains("Zemer") ? 1.2f : 1.4f;
            sr.transform.localScale *= scaler;
            sr.transform.SetPosition3D(sr.transform.GetPositionX() + offset.x, sr.transform.GetPositionY() + offset.y, 2f);

            // Create fake statues
            Sprite sprite = spr;
            GameObject fakeStat = new GameObject("FakeStat" + state.Substring(11));
            SpriteRenderer sr2 = fakeStat.AddComponent<SpriteRenderer>();
            sr2.sprite = sprite;
            fakeStat.transform.parent = statue.transform;
            fakeStat.transform.localScale = appearance.transform.Find("GG_statues_0006_5").localScale;
            fakeStat.transform.position = appearance.transform.Find("GG_statues_0006_5").position;
            fakeStat.SetActive(bs.StatueState.isUnlocked && bs.StatueState.hasBeenSeen);
            BossStatueFlashEffect flashFX = statue.Find("Base").Find("GG_Statue_First_Appear").GetComponent<BossStatueFlashEffect>();
            Mirror.SetField(flashFX, "statueSprites", new SpriteRenderer[] { sr2 });

            if (state.Contains("Isma") || state.Contains("Zemer"))
            {
                StatueControl sc = statue.transform.Find("Base").gameObject.AddComponent<StatueControl>();
                sc.StatueName = state;
                sc._bs = bs;
                sc._sr = sr2;
                if (state.Contains("Isma"))
                {
                    GameObject fake2 = Instantiate(FiveKnights.preloadedGO["IsmaOgrimStatue"], statue.transform);
                    fake2.transform.localScale = appearance.transform.Find("GG_statues_0006_5").localScale / 1.15f;
                    fake2.transform.position = appearance.transform.Find("GG_statues_0006_5").position;
                    sc._fakeStatAlt2 = fake2;
                }
                sc._fakeStatAlt = fakeStat;
                if (state.Contains("Isma")) StatueControls["Isma"] = sc;
                else StatueControls["Zemer"] = sc;
            }

            var tmp = statue.transform.Find("Inspect").Find("Prompt Marker").position;
            statue.transform.Find("Inspect").Find("Prompt Marker").position = new Vector3(tmp.x + nameOffset.x, tmp.y + nameOffset.y, tmp.z);
            bs.disableIfLocked = new GameObject[]
            {
                statue.transform.Find("Inspect").gameObject, statue.transform.Find("Spotlight").gameObject
            };
            bs.enableIfLocked = new GameObject[]
            {
                statue.transform.Find("Inspect_Locked").gameObject
            };
            statue.SetActive(true);
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
            details.nameKey = key;
            details.nameSheet = "Speech";
            details.descriptionKey = desc;
            details.descriptionSheet = "Speech";
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
            On.BossStatueFlashEffect.FlashApex -= BossStatueFlashEffect_FlashApex;
            On.HutongGames.PlayMaker.Actions.CallMethodProper.OnEnter -= CallMethodProperOnEnter;
            GameManager.instance.OnFinishedEnteringScene -= GMOnFinishedEnteringScene;
            On.CameraController.UpdateTargetDestinationDelta -= CameraControllerUpdateTarget;
            On.GameManager.EnterHero -= GameManager_EnterHero;
            On.BossChallengeUI.LoadBoss_int_bool -= BossChallengeUI_LoadBoss_int_bool;
        }
        
        private static void Log(object o)
        {
            Modding.Logger.Log("[WP] " + o);
        }
    }
}
