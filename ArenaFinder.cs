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

namespace FiveKnights
{
    internal class ArenaFinder : MonoBehaviour
    {
        public static Dictionary<string, AudioClip> audioClips;
        public static Dictionary<string, Material> materials;
        public static Dictionary<string, Sprite> sprites;
        private bool correctedTP;
        private int defeats;
        private FightController fightCtrl;

        private void Start()
        {
            Unload();
            On.GameManager.BeginSceneTransition += GameManager_BeginSceneTransition;
            USceneManager.activeSceneChanged += SceneChanged;
            audioClips = new Dictionary<string, AudioClip>();
            materials = new Dictionary<string, Material>();
            sprites = new Dictionary<string, Sprite>();
            LoadIsmaBundle();
        }

        private void LoadIsmaBundle()
        {
            string path = "";
            switch (SystemInfo.operatingSystemFamily)
            {
                case OperatingSystemFamily.Windows:
                    path = "ismaWin";
                    break;
                case OperatingSystemFamily.Linux:
                    path = "ismaWin";
                    break;
                case OperatingSystemFamily.MacOSX:
                    path = "ismaWin";
                    break;
                default:
                    Log("ERROR UNSUPPORTED SYSTEM: " + SystemInfo.operatingSystemFamily);
                    return;
            }
            AssetBundle ab = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, path));
            UObject[] assets = ab.LoadAllAssets();
            FiveKnights.preloadedGO["Isma"] = ab.LoadAsset<GameObject>("Isma");
            FiveKnights.preloadedGO["Plant"] = ab.LoadAsset<GameObject>("Plant");
            FiveKnights.preloadedGO["Zemer"] = ab.LoadAsset<GameObject>("Zemer");
            foreach (Sprite spr in ab.LoadAllAssets<Sprite>())
            {
                sprites[spr.name] = spr;
            }
            materials["flash"] = ab.LoadAsset<Material>("Material");
            
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
            FiveKnights.preloadedGO["Dryya Silhouette"] = dryyaAssetBundle.LoadAsset<GameObject>("Dryya Silhouette");
            Log("Getting Sprites");
            foreach (Sprite sprite in dryyaAssetBundle.LoadAssetWithSubAssets<Sprite>("Dryya_Silhouette"))
            {
                Log("Sprite Name: " + sprite.name);
                sprites[sprite.name] = sprite;
            }

            FlashShader = dryyaAssetBundle.LoadAsset<Shader>("Flash Shader");


        }
        
        private void SceneChanged(Scene arg0, Scene arg1)
        {
            if (arg0.name == "Dream_04_White_Defender")
            {
                GameCameras.instance.cameraFadeFSM.Fsm.SetState("FadeIn");
                PlayerData.instance.whiteDefenderDefeats = defeats;
                Destroy(fightCtrl);
                PlayerData.instance.isInvincible = false;
            }

            if (arg1.name == "GG_Workshop") SetStatue();

            if (arg1.name != "Dream_04_White_Defender") return;
            if (arg0.name != "GG_Workshop") return;
            StartCoroutine(AddComponent());
        }

        private void SetStatue()
        {
            //Used 56's pale prince code here
            GameObject statue = Instantiate(GameObject.Find("GG_Statue_ElderHu"));
            statue.transform.SetPosition2D(25.4f, statue.transform.GetPositionY());//6.5f); //248
            var scene = ScriptableObject.CreateInstance<BossScene>();
            scene.sceneName = "Dream_04_White_Defender";
            var bs = statue.GetComponent<BossStatue>();
            bs.bossScene = scene;
            bs.statueStatePD = "FennelArena";

            var gg = new BossStatue.Completion
            {
                completedTier1 = true,
                seenTier3Unlock = true,
                completedTier2 = true,
                completedTier3 = true,
                isUnlocked = true,
                hasBeenSeen = true,
                usingAltVersion = false,
            };
            bs.StatueState = gg;
            var details = new BossStatue.BossUIDetails();
            details.nameKey = details.nameSheet = "FK_NAME";
            details.descriptionKey = details.descriptionSheet = "FK_DESC";
            bs.bossDetails = details;
            foreach (var i in bs.statueDisplay.GetComponentsInChildren<SpriteRenderer>(true))
            {
                i.sprite = new Sprite();
            }
        }

        private IEnumerator AddComponent()
        {
            yield return null;
            fightCtrl = GameManager.instance.gameObject.AddComponent<FightController>();
        }

        private void GameManager_BeginSceneTransition(On.GameManager.orig_BeginSceneTransition orig, GameManager self, GameManager.SceneLoadInfo info)
        {
            if (info.SceneName != "Dream_04_White_Defender" || correctedTP)
            {
                correctedTP = false;
                orig(self, info);
                return;
            }
            correctedTP = true; 
            defeats = PlayerData.instance.whiteDefenderDefeats;
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

        private void OnDestroy()
        {
            USceneManager.activeSceneChanged -= SceneChanged;
        }

        private void Unload()
        {
            On.GameManager.BeginSceneTransition -= GameManager_BeginSceneTransition;
        }

        public static void Log(object o)
        {
            Logger.Log("[Lost Arena] " + o);
        }
    }
}