using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using HutongGames.PlayMaker.Actions;
using ModCommon.Util;
using ModCommon;
using System.Linq;
using System.Reflection;
using GlobalEnums;
using HutongGames.PlayMaker;
using Vasi;
using Logger = Modding.Logger;
using UObject = UnityEngine.Object;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace FiveKnights
{
    internal class ArenaFinder : MonoBehaviour
    {
        public static Dictionary<string, tk2dSpriteAnimation> spriteAnimations;

        public static Dictionary<string, tk2dSpriteCollection> spriteCollections;

        public static Dictionary<string, tk2dSpriteCollectionData> collectionData;
        public static Dictionary<string, Material> Materials { get; private set; }
        public static Dictionary<string, Sprite> Sprites { get; private set; }

        public static int defeats;

        private FightController fightCtrl;

        private static bool hasSummonElevator;

        private bool hasKingFrag;

        private string prevScene;
        
        private string currScene;

        public const string DryyaScene = "gg dryya";
        
        public const string ZemerScene = "gg zemer";
        
        public const string HegemolScene = "gg hegemol";

        public const string IsmaScene = "gg isma";

        private void Start()
        {
            USceneManager.activeSceneChanged += SceneChanged;
            On.BossStatueLever.OnTriggerEnter2D += BossStatueLever_OnTriggerEnter2D2;
            On.GameManager.BeginSceneTransition += GameManager_BeginSceneTransition;
            spriteAnimations = new Dictionary<string, tk2dSpriteAnimation>();
            spriteCollections = new Dictionary<string, tk2dSpriteCollection>();
            collectionData = new Dictionary<string, tk2dSpriteCollectionData>();
            Materials = new Dictionary<string, Material>();
            Sprites = new Dictionary<string, Sprite>();
            hasKingFrag = PlayerData.instance.gotKingFragment;
            PlayerData.instance.gotKingFragment = true;
            LoadHubBundles();
        }

        // x is 11.2 to 45.7
        // y is 27.4 to 39.2
        
        private void GameManager_BeginSceneTransition(On.GameManager.orig_BeginSceneTransition orig, GameManager self, GameManager.SceneLoadInfo info)
        {
            Log($"Before: Going to {info.SceneName} from {prevScene} using gate {info.EntryGateName}");
            if (info.SceneName == "White_Palace_09")
            {
                if (CustomWP.boss == CustomWP.Boss.Isma || CustomWP.boss == CustomWP.Boss.Ogrim)
                {
                    info.EntryGateName = "door_dreamReturnGGstatueStateIsma_GG_Statue_ElderHu(Clone)(Clone)";
                }
                else if (CustomWP.boss == CustomWP.Boss.Ze || CustomWP.boss == CustomWP.Boss.Mystic)
                {
                    info.EntryGateName = "door_dreamReturnGGstatueStateZemer_GG_Statue_TraitorLord(Clone)(Clone)";
                }
                else if (CustomWP.boss == CustomWP.Boss.All)
                {
                    info.EntryGateName = "door_dreamReturnGGTestingIt";
                } 
                else if (CustomWP.boss == CustomWP.Boss.Dryya)
                {
                    info.EntryGateName = "door_dreamReturnGGstatueState" + CustomWP.boss +
                                         "_GG_Statue_TraitorLord(Clone)(Clone)";
                } 
                else if (CustomWP.boss == CustomWP.Boss.Hegemol || CustomWP.boss == CustomWP.Boss.Dryya)
                {
                    info.EntryGateName = "door_dreamReturnGGstatueState" + CustomWP.boss +
                                         "_GG_Statue_TraitorLord(Clone)(Clone)";
                }
            }
            /*else if (prevScene == "White_Palace_09" && info.SceneName == "Zemer godhome arena")
            {
                //info.EntryGateName = "door_dreamEnter";
                Log("Sending them forward");
                prevScene = "Zemer godhome arena";
                GameManager.instance.BeginSceneTransition(new GameManager.SceneLoadInfo
                {
                    SceneName = "Zemer godhome arena",
                    EntryGateName = "door_dreamEnter",
                    Visualization = GameManager.SceneLoadVisualizations.Dream,
                    WaitForSceneTransitionCameraFade = true,
                    AlwaysUnloadUnusedAssets = false

                });
                return;
            }*/
            else if (prevScene == "Dream_04_White_Defender" && info.SceneName == prevScene)
            {
                Log("in here boi");
                info.SceneName = "White_Palace_09";
                info.EntryGateName = "door_dreamReturnGGTestingIt";
            }
            Log($"After: Going to {info.SceneName} from {prevScene} using gate {info.EntryGateName}");
            prevScene = info.SceneName;
            currScene = info.SceneName;
            orig(self, info);
        }

        private void BossStatueLever_OnTriggerEnter2D2(On.BossStatueLever.orig_OnTriggerEnter2D orig, BossStatueLever self, Collider2D collision)
        {
            Vector2 pos = HeroController.instance.transform.position;
            if (pos.x > 49f && pos.x < 62.2f && pos.y > 35f && collision.tag == "Nail Attack")
            {
                self.switchSound.SpawnAndPlayOneShot(self.audioPlayerPrefab, transform.position);
                GameManager.instance.FreezeMoment(1);
                GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
                if (self.strikeNailPrefab && self.hitOrigin)
                {
                    self.strikeNailPrefab.Spawn(self.hitOrigin.transform.position);
                }
                if (self.leverAnimator)
                {
                    self.leverAnimator.Play("Hit",-1,0f);
                }
                hasSummonElevator = true;
                return;
            }
            orig(self, collision);
        }

        private void CreateLever()
        {
            GameObject altLever = Instantiate(FiveKnights.preloadedGO["StatueMed"].FindGameObjectInChildren("alt_lever"));
            Vector3 alt = altLever.transform.localScale;
            altLever.transform.localScale = new Vector3(alt.x*-1f, alt.y, alt.z);
            altLever.SetActive(true);
            GameObject switchBracket = altLever.FindGameObjectInChildren("GG_statue_switch_bracket");
            switchBracket.SetActive(true);

            GameObject switchLever = altLever.FindGameObjectInChildren("GG_statue_switch_lever");
            switchLever.SetActive(true);
            BossStatueLever toggle = switchLever.GetComponent<BossStatueLever>();
            toggle.SetState(true);
            altLever.transform.position = new Vector2(57.4f,37.5f);
            
        }
        
        private void CameraLockAreaOnOnTriggerEnter2D(On.CameraLockArea.orig_OnTriggerEnter2D orig, CameraLockArea self, Collider2D othercollider)
        {
            switch (currScene)
            {
                case DryyaScene:
                    
                    break;
                case IsmaScene:
                    
                    break;
                case ZemerScene:
                    self.cameraXMin = 22.3f;
                    self.cameraYMin = self.cameraYMax = 31.6f;
                    break;
                case HegemolScene:
                    self.cameraXMin = 22.3f;
                    self.cameraYMin = self.cameraYMax = 31.6f;
                    break;
                default:
                    self.cameraXMin = self.cameraXMin;
                    self.cameraYMin = self.cameraYMax = 13.6f;
                    break;
            }
        }                                                                                                            
        
        private void SceneChanged(Scene arg0, Scene arg1)
        {
            CustomWP.isFromGodhome = arg0.name == "GG_Workshop";

            if (arg1.name == DryyaScene || arg1.name == IsmaScene || arg1.name == HegemolScene || arg1.name == ZemerScene)
            {
                // Done using SFGrenade code
                StartCoroutine(wow());
                IEnumerator wow()
                { 
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

                    foreach (var i in FindObjectsOfType<GameObject>()
                        .Where(x => x.name == "BlurPlane"))
                    {
                        i.GetComponent<MeshRenderer>().materials = blurPlaneMaterials;
                    }
                    
                    foreach (var i in FindObjectsOfType<GrassCut>())
                    {
                        Destroy(i);
                    }
                    
                    Destroy(GameObject.Find("Boss Scene Controller"));
                    FiveKnights.preloadedGO["BSCW"].SetActive(false);
                    var bsc = Instantiate(FiveKnights.preloadedGO["BSCW"]);
                    bsc.SetActive(false);
                    BossSceneController.Instance.transitionInHoldTime = 0;
                    var dreamEntryControlFsm = bsc.FindGameObjectInChildren("Dream Entry").LocateMyFSM("Control");
                    dreamEntryControlFsm.RemoveAction("Pause", 0);
                    dreamEntryControlFsm.AddAction("Pause", new NextFrameEvent() { sendEvent = FsmEvent.Finished });
                    bsc.SetActive(true);
                    yield return null;
                    EventRegister.SendEvent("GG TRANSITION IN");
                    BossSceneController.Instance.GetType().GetProperty("HasTransitionedIn").SetValue(BossSceneController.Instance, true, null);
                    if (arg1.name == ZemerScene || arg1.name == HegemolScene)
                    {
                        bsc.GetComponent<BossSceneController>().heroSpawn.position = new Vector3(25.0f, 27.4f);
                        HeroController.instance.transform.position = new Vector3(25.0f, 27.4f);
                        Log("Changed the hero's pos");
                    }
                    Log("Done trans in dream thing");
                    while (GameObject.Find("Godseeker Crowd") == null) yield return null;
                    Destroy(GameObject.Find("Godseeker Crowd"));
                }
                Log("Done dream entry");
            }

            if (arg0.name == "White_Palace_09")
            {
                Destroy(CustomWP.Instance);
                CustomWP.Instance = null;
            }
            
            if (arg1.name == "GG_Workshop")
            {
                StartCoroutine(CameraFixer());
                StartCoroutine(Arena());
            }

            if (arg0.name == "Waterways_13" && arg1.name == "GG_White_Defender")
            {
                CustomWP.boss = CustomWP.Boss.Isma;
                StartCoroutine(AddComponent());
            }
            
            if (arg0.name == "White_Palace_09" && arg1.name == "GG_White_Defender")
            {
                On.CameraLockArea.OnTriggerEnter2D += CameraLockAreaOnOnTriggerEnter2D;
                StartCoroutine(AddComponent());
            }
            
            if (arg0.name == "White_Palace_09" && 
                (arg1.name == DryyaScene || arg1.name == IsmaScene || arg1.name == ZemerScene || arg1.name == HegemolScene))
            {
                On.CameraLockArea.OnTriggerEnter2D += CameraLockAreaOnOnTriggerEnter2D;
                StartCoroutine(AddComponent());
            }

            if (arg1.name == "White_Palace_09" && 
                (arg0.name == IsmaScene || arg0.name == DryyaScene || arg0.name == ZemerScene || arg0.name == HegemolScene || 
                 arg0.name == "GG_White_Defender" || arg0.name == "Dream_04_White_Defender"))
            {
                GameCameras.instance.cameraFadeFSM.Fsm.SetState("FadeIn");
                GameCameras.instance.tk2dCam.ZoomFactor = 1f;
                PlayerData.instance.isInvincible = false;
                
            }

            if ((arg0.name == "White_Palace_09" && arg1.name == "Dream_04_White_Defender") ||
                (arg0.name == "Dream_04_White_Defender" && arg1.name == "Dream_04_White_Defender" 
                                                        && CustomWP.boss == CustomWP.Boss.All))
            {
                On.CameraLockArea.OnTriggerEnter2D += CameraLockAreaOnOnTriggerEnter2D;
                StartCoroutine(AddComponent());
            }

            if (arg1.name == "White_Palace_09" && arg0.name == "Dream_04_White_Defender") //DO arg1.name == "White_Palace_09" EVENTUALLY
            {
                Log("DEATH");
                GameCameras.instance.cameraFadeFSM.Fsm.SetState("FadeIn");
                PlayerData.instance.whiteDefenderDefeats = defeats;
                PlayerData.instance.isInvincible = false;
            }
            
            if (arg1.name == "White_Palace_09" && arg0.name != "White_Palace_13")
            {
                if (CustomWP.Instance == null)
                {
                    GameManager.instance.gameObject.AddComponent<CustomWP>();
                }
                StartCoroutine(CameraFixer());
                MakeBench(arg1.name, "WhiteBenchNew2", new Vector3(110.6f, 94.1f, 1));
            }

            if (fightCtrl != null && arg1.name != "Dream_04_White_Defender" 
                                  && arg1.name != "GG_White_Defender" && arg1.name != DryyaScene
                                  && arg1.name != IsmaScene && arg1.name != HegemolScene
                                  && arg1.name != ZemerScene)
            {
                Log("Destroying fightctrl");
                On.CameraLockArea.OnTriggerEnter2D -= CameraLockAreaOnOnTriggerEnter2D;
                if (fightCtrl != null)
                {
                    Log("Killed fightCtrl");
                    Destroy(fightCtrl);
                    Log("Killed fightCtrl2");
                }
            }
        }

        private IEnumerator CameraFixer()
        {
            yield return new WaitWhile(() => GameManager.instance.gameState != GlobalEnums.GameState.PLAYING);
            yield return new WaitForSeconds(1f);
            do
            {
                GameCameras.instance.cameraFadeFSM.SetState("FadeIn");
                yield return new WaitForSeconds(1f);
            } 
            while (GameCameras.instance.cameraFadeFSM.ActiveStateName != "Normal");
        }

        private void MakeBench(string scene, string name, Vector3 pos)
        {
            GameObject go = Instantiate(FiveKnights.preloadedGO["Bench"]);
            go.transform.position = pos;
            go.SetActive(true);
            var fsm = go.LocateMyFSM("Bench Control");
            fsm.FsmVariables.FindFsmString("Scene Name").Value = scene;
            fsm.FsmVariables.FindFsmString("Spawn Name").Value = name;
            fsm.FsmVariables.FindFsmVector3("Sit Vector").Value = new Vector3(0f,0.5f,0f);
            PlayerData.instance.respawnScene = "White_Palace_09";
            PlayerData.instance.respawnMarkerName = go.name;
        }

        private IEnumerator AddComponent()
        {
            yield return null;
            fightCtrl = GameManager.instance.gameObject.AddComponent<FightController>();
        }

        private void LoadHubBundles()
        {
            Log("Loading hub bundle");
            Assembly asm = Assembly.GetExecutingAssembly();
            using (Stream s = asm.GetManifestResourceStream("FiveKnights.StreamingAssets.hubasset1"))
            {
                AssetBundle ab = AssetBundle.LoadFromStream(s);
                ab.LoadAllAssets();
                FiveKnights.preloadedGO["hubfloor"] = ab.LoadAsset<GameObject>("white_palace_floor_set_02 (16)");
            }

            IEnumerator LoadMiscBund()
            {
                UObject[] misc = null;
                using (Stream s = asm.GetManifestResourceStream("FiveKnights.StreamingAssets.miscbund"))
                {
                    var req = AssetBundle.LoadFromStreamAsync(s);
                    yield return req;
                    var req2 = req.assetBundle.LoadAllAssetsAsync();
                    yield return req2;
                    misc = req2.allAssets;
                }

                if (misc == null)
                {
                    Log("Failed to load misc bundle");
                    yield break;
                }

                Texture tex = null;
                foreach (UObject asset in misc)
                {
                    if (asset.name == "GG_Statue_Isma") FiveKnights.preloadedGO["GG_Statue_Isma"] = asset as GameObject;
                    else if (asset.name == "IsmaOgrimStatue") FiveKnights.preloadedGO["IsmaOgrimStatue"] = asset as GameObject;
                    else if (asset.name == "Shockwave") FiveKnights.preloadedGO["WaveShad"] = asset as GameObject;
                    else if (asset.name == "WaveEffectMaterial") ArenaFinder.Materials["WaveEffectMaterial"] = asset as Material;
                    else if (asset.name == "UnlitFlashMat") ArenaFinder.Materials["flash"] = asset as Material;
                    else if (asset.name == "sonar") tex = asset as Texture;
                    else if (asset.name == "petal-test") ArenaFinder.Sprites["ZemParticPetal"] = asset as Sprite;
                    else if (asset.name == "dung-test") ArenaFinder.Sprites["ZemParticDung"] = asset as Sprite;
                    else if (asset.name.Contains("Zem_Sil_")) ArenaFinder.Sprites[asset.name] = asset as Sprite;
                    else if (asset.name.Contains("Sil_Isma_")) ArenaFinder.Sprites[asset.name] = asset as Sprite;
                    else if (asset.name.Contains("Dryya_Silhouette_")) ArenaFinder.Sprites[asset.name] = asset as Sprite;
                    else if (asset.name.Contains("hegemol_silhouette_")) ArenaFinder.Sprites[asset.name] = asset as Sprite;
                    if (asset is GameObject i)
                    {
                        if (i.GetComponent<SpriteRenderer>() == null)
                        {
                            foreach (SpriteRenderer sr in i.GetComponentsInChildren<SpriteRenderer>(true))
                            {
                                sr.material = new Material(Shader.Find("Sprites/Default"));
                            }
                        }
                        else i.GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));
                    }
                }
                
                ArenaFinder.Materials["TestDist"] = new Material(ArenaFinder.Materials["WaveEffectMaterial"]);
                ArenaFinder.Materials["TestDist"].SetTexture("_NoiseTex", tex);
                ArenaFinder.Materials["TestDist"].SetFloat("_Intensity", 0.2f);
                FiveKnights.preloadedGO["WaveShad"].GetComponent<SpriteRenderer>().material = ArenaFinder.Materials["TestDist"];
                Log("Done loading misc bund");
                StartCoroutine(LoadZemerArena());
            }

            IEnumerator LoadZemerArena()
            {
                Log("Loading Zemer Arena");
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

                string str = "";
                //zemer godhome arena
                Assembly assembly = Assembly.GetExecutingAssembly();
                using Stream s = assembly.GetManifestResourceStream("FiveKnights.StreamingAssets.ggArenaDryya");
                yield return null;
                var req = AssetBundle.LoadFromStreamAsync(s);
                yield return req;
                yield return null;
                str = req.assetBundle.GetAllScenePaths()[0];
                Log($"Done loading {str} for Dryya");
                yield return null;
                
                using Stream s2 = assembly.GetManifestResourceStream("FiveKnights.StreamingAssets.ggArenaIsma");
                yield return null;
                var req2 = AssetBundle.LoadFromStreamAsync(s2);
                yield return req2;
                yield return null;
                str = req2.assetBundle.GetAllScenePaths()[0];
                Log($"Done loading {str} for Isma");
                yield return null;
                
                using Stream s3 = assembly.GetManifestResourceStream("FiveKnights.StreamingAssets.ggArenaZemer");
                yield return null;
                var req3 = AssetBundle.LoadFromStreamAsync(s3);
                yield return req3;
                yield return null;
                str = req3.assetBundle.GetAllScenePaths()[0];
                Log($"Done loading {str} for Zemer");
                yield return null;
                
                using Stream s4 = assembly.GetManifestResourceStream("FiveKnights.StreamingAssets.ggArenaHegemol");
                yield return null;
                var req4 = AssetBundle.LoadFromStreamAsync(s4);
                yield return req4;
                yield return null;
                str = req4.assetBundle.GetAllScenePaths()[0];
                Log($"Done loading {str} for Hegemol");
                yield return null;
            }

            StartCoroutine(LoadMiscBund());
        }

        IEnumerator Arena()
        {
            yield return new WaitWhile(() => !HeroController.instance);
            yield return new WaitWhile(() =>HeroController.instance.transform.GetPositionX() < 35f);
            CreateLever();
            while (true)
            {
                yield return new WaitWhile(() => HeroController.instance.transform.GetPositionX() < 53f);
                GameObject go = Instantiate(FiveKnights.preloadedGO["lift"]);
                Destroy(go.transform.Find("Rise Beam").gameObject);
                Destroy(go.transform.Find("Pt").gameObject);
                go.transform.position = new Vector2(53.2f, 20f);
                go.SetActive(true);
                PlayMakerFSM fsm = go.LocateMyFSM("Control");
                fsm.RemoveAction("Rise Antic", 2);
                fsm.enabled = true;
                fsm.FsmVariables.FindFsmFloat("Top Y").Value = 36.4f;
                fsm.SetState("Init");
                yield return null;

                yield return new WaitWhile(() => !hasSummonElevator);
                //yield return new WaitWhile(() => !Input.GetKeyUp(KeyCode.R));

                fsm.SetState("Antic Pause");
                yield return null;
                yield return new WaitWhile(() => fsm.ActiveStateName != "Hit Top");
                fsm.FsmVariables.FindFsmFloat("Top Y").Value = 91f;
                fsm.SetState("Bot");
                bool trig = false;
                while (go.transform.GetPositionY() < 65f)
                {
                    if (!trig && 
                        HeroController.instance.transform.GetPositionY() > 47f &&
                        go.transform.GetPositionY() > 45f)
                    {
                        trig = true;
                        HeroController.instance.RelinquishControl();
                    }

                    yield return null;
                }
                if (!trig)
                {
                    yield return null;
                    hasSummonElevator = false;
                    continue;
                }
                HeroController.instance.StopAnimationControl();
                GameManager.instance.playerData.disablePause = true;
                PlayMakerFSM pm = GameCameras.instance.tk2dCam.gameObject.LocateMyFSM("CameraFade");
                pm.SendEvent("FADE OUT");
                Destroy(go);
                yield return new WaitForSeconds(0.5f);
                GameManager.instance.BeginSceneTransition(new GameManager.SceneLoadInfo
                {
                    SceneName = "White_Palace_09",
                    EntryGateName = "left test2",
                    Visualization = GameManager.SceneLoadVisualizations.Default,
                    WaitForSceneTransitionCameraFade = false,
                });
                yield break;
            }
        }

        private void OnDestroy()
        {
            USceneManager.activeSceneChanged -= SceneChanged;
            On.BossStatueLever.OnTriggerEnter2D -= BossStatueLever_OnTriggerEnter2D2;
            PlayerData.instance.gotKingFragment = hasKingFrag;
        }

        private void Log(object o)
        {
            Logger.Log("[Scene] " + o);
        }
        
        // ------------------UNUSED------------------
        
        //Code from SFGrenade
        
        /*private void GameManager_BeginSceneTransition(On.GameManager.orig_BeginSceneTransition orig, GameManager self, GameManager.SceneLoadInfo info)
        {
            if (info.SceneName == "Waterways_13")
            {
                GameObject dn = GameObject.Find("Dream Dialogue");
                if (dn != null) Destroy(dn);
                CreateDreamGateway("Dream Enter", "door_dreamEnter", new Vector2(93.5f, 19.3f), 
                    new Vector2(5.25f, 5.25f),
                    "GG_White_Defender", "Waterways_13");
                Log(info.EntryGateName);
            }
            orig(self, info);
        }*/
        private void CreateDreamGateway(string gateName, string toGate, Vector2 pos, Vector2 size, string toScene, string returnScene)
        {
            Log("Creating Dream Gateway");
            
            GameObject dreamEnter = GameObject.Instantiate(FiveKnights.preloadedGO["DPortal"]);
            dreamEnter.name = gateName;
            dreamEnter.SetActive(true);
            dreamEnter.transform.position = pos;
            dreamEnter.transform.localScale = Vector3.one;
            dreamEnter.transform.eulerAngles = Vector3.zero;

            dreamEnter.GetComponent<BoxCollider2D>().size = size;
            dreamEnter.GetComponent<BoxCollider2D>().offset = Vector2.zero;

            foreach (var pfsm in dreamEnter.GetComponents<PlayMakerFSM>())
            {
                if (pfsm.FsmName == "Control")
                {
                    pfsm.FsmVariables.GetFsmString("Return Scene").Value = returnScene;
                    pfsm.FsmVariables.GetFsmString("To Scene").Value = toScene;
                    pfsm.GetAction<BeginSceneTransition>("Change Scene", 4).entryGateName = toGate;
                }
            }

            GameObject dreamPT = GameObject.Instantiate(FiveKnights.preloadedGO["DPortal2"]);
            dreamPT.SetActive(true);
            dreamPT.transform.position = new Vector3(pos.x, pos.y, -0.002f);
            dreamPT.transform.localScale = Vector3.one;
            dreamPT.transform.eulerAngles = Vector3.zero;

            var shape = dreamPT.GetComponent<ParticleSystem>().shape;
            shape.scale = new Vector3(size.x, size.y, 0.001f);

            Log("Done Creating Dream Gateway");
        }
    }
}