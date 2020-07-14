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
using GlobalEnums;
using InControl;

namespace FiveKnights
{
    internal class ArenaFinder : MonoBehaviour
    {
        public static Dictionary<string, AudioClip> ismaAudioClips;
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
            On.GameManager.BeginSceneTransition += GameManager_BeginSceneTransition;
            ismaAudioClips = new Dictionary<string, AudioClip>();
            materials = new Dictionary<string, Material>();
            shaders = new Dictionary<string, Shader>();
            sprites = new Dictionary<string, Sprite>();
            clips = new Dictionary<string, AudioClip>();
            spriteAnimations = new Dictionary<string, tk2dSpriteAnimation>();
            spriteCollections = new Dictionary<string, tk2dSpriteCollection>();
            collectionData = new Dictionary<string, tk2dSpriteCollectionData>();
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
                Log("YAYAYA");
                CustomWP.boss = CustomWP.Boss.Isma;
                StartCoroutine(AddComponent());
            }
            
            if (arg0.name == "White_Palace_09" && arg1.name == "GG_White_Defender") //DO arg0.name == "White_Palace_09"
            {
                StartCoroutine(AddComponent());
            }

            if (arg1.name == "White_Palace_09" && arg0.name == "GG_White_Defender") //REMOVE THIS ONCE DONE
            {
                GameCameras.instance.cameraFadeFSM.Fsm.SetState("FadeIn");
                GameCameras.instance.tk2dCam.ZoomFactor = 1f;
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
            
            if (arg1.name == "White_Palace_09" && arg0.name != "White_Palace_13")
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
            AssetBundle ab3 = FiveKnights.assetbundles["hubasset1"];
            ab3.LoadAllAssets();
            FiveKnights.preloadedGO["hubfloor"] = ab3.LoadAsset<GameObject>("white_palace_floor_set_02 (16)");
            Log("Finished hub bundle");
            LoadIsmaBundle();
        }

        private void LoadIsmaBundle()
        {
            Log("Loading Isma Bundle");
            AssetBundle ab = null;
            AssetBundle ab2 = FiveKnights.assetbundles["ismabg"];
            foreach (var i in
                FiveKnights.assetbundles.Keys.
                    Where(x => x.Contains("isma") && x != "ismabg")) ab = FiveKnights.assetbundles[i];
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
                ismaAudioClips[i.name] = i;
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
            string path = "";
            foreach (var i in FiveKnights.assetbundles.Keys.
                    Where(x => x.Contains("dryya"))) path = i;
            AssetBundle dryyaAssetBundle = FiveKnights.assetbundles[path]; //AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, dryyaAssetsPath));
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
            Log("Getting Hegemol Bundle");
            string path = "";
            foreach (var i in FiveKnights.assetbundles.Keys.
                Where(x => x.Contains("hegemol"))) path = i;
            AssetBundle hegemolBundle = FiveKnights.assetbundles[path]; //AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, hegemolBundlePath));

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
            foreach (var i in FiveKnights.assetbundles.Keys.
                Where(x => x.Contains("zemer"))) path = i;
            PlayMakerFSM fsm = FiveKnights.preloadedGO["Traitor"].LocateMyFSM("Mantis");
            FiveKnights.preloadedGO["TraitorSlam"] = fsm.GetAction<SpawnObjectFromGlobalPool>("Waves", 0).gameObject.Value;
            clips["TraitorSlam"] = fsm.GetAction<AudioPlayerOneShotSingle>("Waves", 4).audioClip.Value as AudioClip;
            AssetBundle ab = FiveKnights.assetbundles[path]; //AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, path));
            UObject[] assets = ab.LoadAllAssets();
            FiveKnights.preloadedGO["Zemer"] = ab.LoadAsset<GameObject>("Zemer");
            FiveKnights.preloadedGO["SlashBeam"] = ab.LoadAsset<GameObject>("NewSlash");
            FiveKnights.preloadedGO["SlashBeam2"] = ab.LoadAsset<GameObject>("NewSlash2");
            FiveKnights.preloadedGO["SlashBeam3"] = ab.LoadAsset<GameObject>("NewSlash3");
            
            FiveKnights.preloadedGO["WaveShad"] = ab.LoadAsset<GameObject>("Shockwave");
            Shader shader = ab.LoadAsset<Shader>("WaveEffectShader");
            Texture tex = ab.LoadAsset<Texture>("sonar");
            Log("Tex " + (tex == null));
            Log("SHAD " + (shader == null));
            materials["TestDist"] = new Material(shader);
            Log("SHAD2 " + materials["TestDist"].shader.name);
            materials["TestDist"].SetTexture("_NoiseTex", tex);
            Log("DIOSP " + materials["TestDist"].GetFloat("_Intensity"));
            materials["TestDist"].SetFloat("_Intensity", 0.2f);
            Log("DIOSP " + materials["TestDist"].GetFloat("_Intensity"));
            Log("MAPG");
            FiveKnights.preloadedGO["WaveShad"].GetComponent<SpriteRenderer>().material = materials["TestDist"];


            foreach (AudioClip aud in ab.LoadAllAssets<AudioClip>())
            {
                clips[aud.name] = aud;
            }
            
            FiveKnights.preloadedGO["SlashBeam"].GetComponent<SpriteRenderer>().material =  new Material(Shader.Find("Sprites/Default"));   
            
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