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
        public static Dictionary<string, Sprite> sprites;
        public static Dictionary<string, AudioClip> clips;
        public static int defeats;
        public static bool returnToWP;
        private FightController fightCtrl;

        private void Start()
        {
            USceneManager.activeSceneChanged += SceneChanged;
            audioClips = new Dictionary<string, AudioClip>();
            materials = new Dictionary<string, Material>();
            sprites = new Dictionary<string, Sprite>();
            clips = new Dictionary<string, AudioClip>();
            LoadIsmaBundle();
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
                GameManager.instance.gameObject.AddComponent<CustomWP>(); //110.6,94.4
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
            AssetBundle ab2 = AssetBundle.LoadFromFile(System.IO.Path.Combine(Application.streamingAssetsPath, "ismabg"));
            UObject[] assets = ab.LoadAllAssets();
            FiveKnights.preloadedGO["Isma"] = ab.LoadAsset<GameObject>("Isma");
            FiveKnights.preloadedGO["Plant"] = ab.LoadAsset<GameObject>("Plant");
            FiveKnights.preloadedGO["Gulka"] = ab.LoadAsset<GameObject>("Gulka");
            FiveKnights.preloadedGO["Fool"] = ab.LoadAsset<GameObject>("Fool");
            FiveKnights.preloadedGO["Wall"] = ab.LoadAsset<GameObject>("Wall");
            FiveKnights.preloadedGO["ismaBG"] = ab2.LoadAsset<GameObject>("gg_dung_set (1)");
            clips["IsmaMusic"] = ab.LoadAsset<AudioClip>("Aud_Isma");
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

        public static Shader FlashShader;
        private void LoadDryyaAssets()
        {
            string dryyaAssetsPath;
            switch (SystemInfo.operatingSystemFamily)
            {
                case OperatingSystemFamily.Windows:
                    dryyaAssetsPath = "dryyaassets";
                    break;
                case OperatingSystemFamily.Linux:
                    dryyaAssetsPath = "dryyaassets";
                    break;
                case OperatingSystemFamily.MacOSX:
                    dryyaAssetsPath = "dryyaassets";
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

            FlashShader = dryyaAssetBundle.LoadAsset<Shader>("Flash Shader");
            Log("Finished Loading Dryya Bundle");
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
            yield return new WaitWhile(() => !Input.GetKeyUp(KeyCode.R));
            fsm.SetState("Antic Pause");
            yield return null;
            yield return new WaitWhile(() => fsm.ActiveStateName != "Hit Top");
            fsm.FsmVariables.FindFsmFloat("Top Y").Value = 91f;
            fsm.SetState("Bot");
            yield return new WaitWhile(() => go.transform.GetPositionY() < 90f);
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
        }

        private void OnDestroy()
        {
            USceneManager.activeSceneChanged -= SceneChanged;
        }

        public static void Log(object o)
        {
            Logger.Log("[Lost Arena] " + o);
        }
    }
}