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
            LoadHubBundles();
        }

        private void GameManager_BeginSceneTransition(On.GameManager.orig_BeginSceneTransition orig, GameManager self, GameManager.SceneLoadInfo info)
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
        }

        //Code from SFGrenade
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
        private void BossStatueLever_OnTriggerEnter2D2(On.BossStatueLever.orig_OnTriggerEnter2D orig, BossStatueLever self, Collider2D collision)
        {
            //52.6 62.2
            Vector2 pos = HeroController.instance.transform.position;
            if (pos.x > 49f && pos.x < 62.2f && collision.tag == "Nail Attack")
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
            self.cameraYMax = 13.6f;
            self.cameraYMin = 13.6f;
        }                                                                                                            
        
        private void SceneChanged(Scene arg0, Scene arg1)
        {
            CustomWP.isFromGodhome = arg0.name == "GG_Workshop";
            
            if (arg1.name == "GG_Workshop")
            {
                StartCoroutine(CameraFixer()); //97.5 19.3
                StartCoroutine(Arena());
            }

            if (arg0.name == "Waterways_13" && arg1.name == "GG_White_Defender")
            {
                CustomWP.boss = CustomWP.Boss.Isma;
                StartCoroutine(AddComponent());
            }
            
            if (arg0.name == "White_Palace_09" && arg1.name == "GG_White_Defender") //DO arg0.name == "White_Palace_09"
            {
                On.CameraLockArea.OnTriggerEnter2D += CameraLockAreaOnOnTriggerEnter2D;
                StartCoroutine(AddComponent());
            }

            if (arg1.name == "White_Palace_09" && arg0.name == "GG_White_Defender") //REMOVE THIS ONCE DONE
            {
                GameCameras.instance.cameraFadeFSM.Fsm.SetState("FadeIn");
                GameCameras.instance.tk2dCam.ZoomFactor = 1f;
                PlayerData.instance.isInvincible = false;
            }

            if ((arg0.name == "White_Palace_09" && arg1.name == "Dream_04_White_Defender") ||
                (arg0.name == "Dream_04_White_Defender" && arg1.name == "Dream_04_White_Defender" && CustomWP.boss == CustomWP.Boss.All))
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

            if (fightCtrl != null && arg1.name != "Dream_04_White_Defender" && arg1.name != "GG_White_Defender")
            {
                Log("Destroying fightctrl");
                On.CameraLockArea.OnTriggerEnter2D -= CameraLockAreaOnOnTriggerEnter2D;
                Destroy(fightCtrl);
            }
        }

        private IEnumerator CameraFixer()
        {
            yield return new WaitWhile(() => GameManager.instance.gameState != GlobalEnums.GameState.PLAYING);
            yield return new WaitForSeconds(1f);
            while(GameCameras.instance.cameraFadeFSM.ActiveStateName != "Normal")
            {
                GameCameras.instance.cameraFadeFSM.SetState("FadeIn");
                yield return new WaitForSeconds(1f);
            }
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
                Object[] misc = null;
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
                foreach (Object asset in misc)
                {
                    Log("Loading " + asset.name);
                    if (asset.name == "Shockwave") FiveKnights.preloadedGO["WaveShad"] = asset as GameObject;
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
            }

            StartCoroutine(LoadMiscBund());
            Log("Finished hub bundle");
            // StartCoroutine(test());
        }

        private IEnumerator test()
        {
            yield return new WaitWhile(() => !Input.GetKey(KeyCode.R));
    
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
            
            Assembly asm = Assembly.GetExecutingAssembly();
            using Stream s = asm.GetManifestResourceStream("FiveKnights.StreamingAssets.dBund");
            AssetBundle ab = AssetBundle.LoadFromStream(s);
            str = ab.GetAllScenePaths()[0];
            Log("str " + str);
            yield return new WaitWhile(() => !Input.GetKey(KeyCode.R));
            
            //On.SceneManager.Start += SceneManagerOnStart;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Zemer godhome arena");
            /*GameManager.instance.BeginSceneTransition(new GameManager.SceneLoadInfo
            {
                SceneName = str,
                EntryGateName = "door_test1",
                Visualization = GameManager.SceneLoadVisualizations.Dream,
                WaitForSceneTransitionCameraFade = false,

            });*/
        }
        
        private void SceneManagerOnStart(On.SceneManager.orig_Start orig, SceneManager self)
        {
            Log("Log " + self.mapZone);
            orig(self);
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
        }

        private void Log(object o)
        {
            Logger.Log("[Scene] " + o);
        }
    }
}