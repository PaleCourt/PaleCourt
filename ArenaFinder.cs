using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ModCommon.Util;
using ModCommon;
using System.Linq;
using Logger = Modding.Logger;
using UObject = UnityEngine.Object;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using On;
using System;

namespace FiveKnights
{
    internal class ArenaFinder : MonoBehaviour
    {
        public static Dictionary<string, AudioClip> audioClips;
        public static Dictionary<string, Material> materials;
        public static Dictionary<string, Shader> shaders;
        public static Dictionary<string, Sprite> sprites;
        public static Dictionary<string, Texture> textures;
        public static Dictionary<string, AudioClip> clips;
        public static Dictionary<string, tk2dSpriteAnimation> spriteAnimations;
        public static Dictionary<string, tk2dSpriteCollection> spriteCollections;
        public static Dictionary<string, tk2dSpriteCollectionData> collectionData;
        public static int defeats;
        public static bool returnToWP;
        private FightController fightCtrl;
        private static bool hasSummonElevator;

        private void Start()
        {
            USceneManager.activeSceneChanged += SceneChanged;
            On.BossStatueLever.OnTriggerEnter2D += BossStatueLever_OnTriggerEnter2D2;
            audioClips = new Dictionary<string, AudioClip>();
            materials = new Dictionary<string, Material>();
            shaders = new Dictionary<string, Shader>();
            sprites = new Dictionary<string, Sprite>();
            clips = new Dictionary<string, AudioClip>();
            spriteAnimations = new Dictionary<string, tk2dSpriteAnimation>();
            spriteCollections = new Dictionary<string, tk2dSpriteCollection>();
            collectionData = new Dictionary<string, tk2dSpriteCollectionData>();
            LoadIsmaBundle();
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

        private void SceneChanged(Scene arg0, Scene arg1)
        {
            if (arg1.name == "GG_Workshop")
            {
                StartCoroutine(CameraFixer());
                StartCoroutine(Arena());
            }

            if (arg0.name == "White_Palace_09" && arg1.name == "GG_White_Defender") //DO arg0.name == "White_Palace_09"
            {
                StartCoroutine(AddComponent());
            }

            if (arg1.name == "White_Palace_09" && arg0.name == "GG_White_Defender") //REMOVE THIS ONCE DONE
            {
                GameCameras.instance.cameraFadeFSM.Fsm.SetState("FadeIn");
                Destroy(fightCtrl);
                PlayerData.instance.isInvincible = false;
            }

            if (arg0.name == "White_Palace_09" && arg1.name == "Dream_04_White_Defender")
            {
                StartCoroutine(AddComponent());
            }

            if (arg1.name == "White_Palace_09" && arg0.name == "Dream_04_White_Defender") //DO arg1.name == "White_Palace_09" EVENTUALLY
            {
                GameCameras.instance.cameraFadeFSM.Fsm.SetState("FadeIn");
                PlayerData.instance.whiteDefenderDefeats = defeats;
                Destroy(fightCtrl);
                PlayerData.instance.isInvincible = false;
            }
            
            if (arg1.name == "White_Palace_09")
            {
                if (CustomWP.Instance == null)
                {
                    GameManager.instance.gameObject.AddComponent<CustomWP>();
                }
                StartCoroutine(CameraFixer());
                MakeBench(arg1.name, "WhiteBenchNew2", new Vector3(110.6f, 94.1f, 1));
            }
        }

        private IEnumerator CameraFixer()
        {
            yield return new WaitWhile(() => GameManager.instance.gameState != GlobalEnums.GameState.PLAYING);
            yield return new WaitForSeconds(1f);
            while(GameCameras.instance.cameraFadeFSM.ActiveStateName != "Normal")
            {
                GameCameras.instance.cameraFadeFSM.SetState("FadeIn");
                yield return new WaitForSeconds(0.5f);
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

        private void LoadIsmaBundle()
        {
            Log("Loading Isma Bundle");
            string path = "";
            switch (SystemInfo.operatingSystemFamily)
            {
                case OperatingSystemFamily.Windows:
                    path = "ismawin";
                    break;
                case OperatingSystemFamily.Linux:
                    path = "ismalin";
                    break;
                case OperatingSystemFamily.MacOSX:
                    path = "ismamc";
                    break;
                default:
                    Log("ERROR UNSUPPORTED SYSTEM: " + SystemInfo.operatingSystemFamily);
                    return;
            }
            AssetBundle ab = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, path));
            AssetBundle ab2 = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "ismabg"));
            UObject[] assets = ab.LoadAllAssets();
            FiveKnights.preloadedGO["Isma"] = ab.LoadAsset<GameObject>("Isma");
            FiveKnights.preloadedGO["Plant"] = ab.LoadAsset<GameObject>("Plant");
            FiveKnights.preloadedGO["Gulka"] = ab.LoadAsset<GameObject>("Gulka");
            FiveKnights.preloadedGO["Fool"] = ab.LoadAsset<GameObject>("Fool");
            FiveKnights.preloadedGO["Wall"] = ab.LoadAsset<GameObject>("Wall");
            FiveKnights.preloadedGO["ismaBG"] = ab2.LoadAsset<GameObject>("gg_dung_set (1)");
            clips["IsmaMusic"] = ab.LoadAsset<AudioClip>("Aud_Isma");
            foreach (AudioClip i in ab.LoadAllAssets<AudioClip>().Where(x=>x.name.Contains("IsmaAud")))
            {
                audioClips[i.name] = i;
            }
            foreach (GameObject i in ab.LoadAllAssets<GameObject>())
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
            foreach (Sprite spr in ab.LoadAllAssets<Sprite>())
            {
                sprites[spr.name] = spr;
            }
            materials["flash"] = ab.LoadAsset<Material>("UnlitFlashMat");
            Log("Finished Loading Isma Bundle");
            LoadDryyaAssets();
        }
        
        private void LoadDryyaAssets()
        {
            string dryyaAssetsPath;
            switch (SystemInfo.operatingSystemFamily)
            {
                case OperatingSystemFamily.Windows:
                    dryyaAssetsPath = "dryyawin";
                    break;
                case OperatingSystemFamily.Linux:
                    dryyaAssetsPath = "dryyalin";
                    break;
                case OperatingSystemFamily.MacOSX:
                    dryyaAssetsPath = "dryyamc";
                    break;
                default:
                    Log("ERROR UNSUPPORTED SYSTEM: " + SystemInfo.operatingSystemFamily);
                    return;
            }

            AssetBundle dryyaAssetBundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, dryyaAssetsPath));
            FiveKnights.preloadedGO["Dryya"] = dryyaAssetBundle.LoadAsset<GameObject>("Dryya");
            FiveKnights.preloadedGO["Stab Effect"] = dryyaAssetBundle.LoadAsset<GameObject>("Stab Effect");
            FiveKnights.preloadedGO["Dive Effect"] = dryyaAssetBundle.LoadAsset<GameObject>("Dive Effect");
            FiveKnights.preloadedGO["Elegy Beam"] = dryyaAssetBundle.LoadAsset<GameObject>("Elegy Beam");
            Log("Getting Sprites");
            foreach (Sprite sprite in dryyaAssetBundle.LoadAssetWithSubAssets<Sprite>("Dryya_Silhouette"))
            {
                Log("Sprite Name: " + sprite.name);
                sprites[sprite.name] = sprite;
            }
            
            Log("Finished Loading Dryya Bundle");
            LoadHegemolBundle();
        }

        private void LoadHegemolBundle()
        {
            string hegemolBundlePath;
            switch (SystemInfo.operatingSystemFamily)
            {
                case OperatingSystemFamily.Windows:
                    hegemolBundlePath = "hegemolwin";
                    break;
                case OperatingSystemFamily.Linux:
                    hegemolBundlePath = "hegemollin";
                    break;
                case OperatingSystemFamily.MacOSX:
                    hegemolBundlePath = "hegemolmc";
                    break;
                default:
                    Log("ERROR UNSUPPORTED SYSTEM: " + SystemInfo.operatingSystemFamily);
                    return;
            }

            Log("Getting Hegemol Bundle");
            AssetBundle hegemolBundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, hegemolBundlePath));

            UnityEngine.Object[] objects = hegemolBundle.LoadAllAssets();
            foreach (UnityEngine.Object obj in objects)
            {
                Log("Object Name: " + obj.name);
            }
            
            Log("Getting SpriteCollections");
            
            FiveKnights.preloadedGO["Hegemol Collection Prefab"] = hegemolBundle.LoadAsset<GameObject>("HegemolSpriteCollection");
            FiveKnights.preloadedGO["Hegemol Animation"] = hegemolBundle.LoadAsset<GameObject>("HegemolSpriteAnimation");

            Log("Finished Loading Hegemol Bundle");
            LoadZemerBundle();
        }
        
        private void LoadZemerBundle()
        {
            Log("Loading Zemer Bundle");
            string path = "";
            switch (SystemInfo.operatingSystemFamily)
            {
                case OperatingSystemFamily.Windows:
                    path = "zemerwin";
                    break;
                case OperatingSystemFamily.Linux:
                    path = "zemerlin";
                    break;
                case OperatingSystemFamily.MacOSX:
                    path = "zemermc";
                    break;
                default:
                    Log("ERROR UNSUPPORTED SYSTEM: " + SystemInfo.operatingSystemFamily);
                    return;
            }
            AssetBundle ab = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, path));
            UObject[] assets = ab.LoadAllAssets();
            FiveKnights.preloadedGO["Zemer"] = ab.LoadAsset<GameObject>("Zemer");
            foreach (GameObject i in ab.LoadAllAssets<GameObject>())
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
            foreach (Sprite spr in ab.LoadAllAssets<Sprite>())
            {
                sprites[spr.name] = spr;
            }
            Log("Finished Loading Zemer Bundle");
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

        IEnumerator Arena()
        {
            yield return new WaitWhile(() => !HeroController.instance);
            yield return new WaitWhile(() =>HeroController.instance.transform.GetPositionX() < 35f);
            CreateLever();
            while (true)
            {
                yield return new WaitWhile(() => HeroController.instance.transform.GetPositionX() < 53f);
                GameObject go = Instantiate(FiveKnights.preloadedGO["lift"]);
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
                yield return new WaitWhile(() => go.transform.GetPositionY() < 90f);
                if (HeroController.instance.transform.GetPositionY() < 85f)
                {
                    yield return null;
                    hasSummonElevator = false;
                    continue;
                }
                HeroController.instance.RelinquishControl();
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

        public static void Log(object o)
        {
            Logger.Log("[Lost Arena] " + o);
        }
    }
}