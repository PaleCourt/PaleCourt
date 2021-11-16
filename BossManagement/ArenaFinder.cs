using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using HutongGames.PlayMaker.Actions;
using System.Linq;
using GlobalEnums;
using HutongGames.PlayMaker;
using SFCore.Utils;
using Logger = Modding.Logger;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace FiveKnights
{
    internal class ArenaFinder : MonoBehaviour
    {
        public static Dictionary<string, tk2dSpriteAnimation> spriteAnimations;

        public static Dictionary<string, tk2dSpriteCollection> spriteCollections;

        public static Dictionary<string, tk2dSpriteCollectionData> collectionData;
        public static Dictionary<string, Sprite> Sprites { get; private set;} = new Dictionary<string, Sprite>();

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
            On.SceneManager.Start += SceneManagerOnStart;
            On.BossStatueLever.OnTriggerEnter2D += BossStatueLever_OnTriggerEnter2D2;
            On.GameManager.BeginSceneTransition += GameManager_BeginSceneTransition;
            spriteAnimations = new Dictionary<string, tk2dSpriteAnimation>();
            spriteCollections = new Dictionary<string, tk2dSpriteCollection>();
            collectionData = new Dictionary<string, tk2dSpriteCollectionData>();
            hasKingFrag = PlayerData.instance.gotKingFragment;
            PlayerData.instance.gotKingFragment = true;
        }

        private void SceneManagerOnStart(On.SceneManager.orig_Start orig, SceneManager self)
        {
            Log("Changing SceneManager settings");
            if (currScene == ZemerScene)
            {
                self.environmentType = 7;
            }
            else if (currScene == "White_Palace_09")
            {
                Log("Changed SceneManager settings for WP_09");
                //self.noLantern = true;
                //self.darknessLevel = 1;
            }
            orig(self);
        }

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
            Log("Are in locked cam");
            switch (currScene)
            {
                /*case DryyaScene:
                    
                    break;
                case IsmaScene:
                    
                    break;*/
                case ZemerScene:
                    self.cameraXMin = 22.3f;
                    self.cameraYMin = self.cameraYMax = 31.6f;
                    break;
                case HegemolScene:
                    self.cameraXMin = 22.3f;
                    self.cameraYMin = self.cameraYMax = 31.6f;
                    break;
                /*default:
                    self.cameraXMin = self.cameraXMin;
                    self.cameraYMin = self.cameraYMax = 13.6f;
                    break;*/
            }
        }                                                                                                            
        
        private void SceneChanged(Scene arg0, Scene arg1)
        {
            CustomWP.isFromGodhome = arg0.name == "GG_Workshop";
            if (arg0.name == "White_Palace_13" && arg1.name == "White_Palace_09") return;
            
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
                        String grassType = "Grass0";
                        
                        if (i.gameObject.name.Contains("green_grass_1"))
                        {
                            grassType = "Grass1";
                        }
                        else if (i.gameObject.name.Contains("green_grass_2"))
                        {
                            grassType = "Grass2";
                        }
                        else if (i.gameObject.name.Contains("green_grass_3"))
                        {
                            grassType = "Grass3";
                        }
                        
                        GameObject newGrass = Instantiate(FiveKnights.preloadedGO[grassType]);
                        newGrass.SetActive(true);
                        newGrass.transform.position = i.transform.position;
                        Destroy(i.gameObject);
                    }
                    
                    var breakable = FindObjectsOfType<Breakable>()
                        .Where(x => x.name == "cliffs_pole (2)").ToArray();
                    if (breakable.Length > 0 && breakable[0] != null)
                    {
                        Destroy(breakable[0]);
                    }

                    foreach (var i in FindObjectsOfType<GameObject>()
                        .Where(x => x.name.Contains("ruin_water_bounce")))
                    {
                        if (i.name.Contains("21") || i.name.Contains("22") || i.name.Contains("23") ||
                            i.name.Contains("24"))
                        {
                            Destroy(i);
                        }
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
                    GameObject oldGS = GameObject.Find("Godseeker Crowd");
                    GameObject newGS = Instantiate(FiveKnights.preloadedGO["Godseeker"]);
                    newGS.SetActive(true);
                    newGS.transform.position = oldGS.transform.position;
                    Destroy(oldGS);
                }
                Log("Done dream entry");
            }

            if (arg0.name == "White_Palace_09")
            {
                Destroy(CustomWP.Instance);
                CustomWP.Instance = null;
            }
            
            if (arg1.name == "GG_Workshop" && FiveKnights.Instance.SaveSettings.UnlockedGodhome())
            {
                StartCoroutine(CameraFixer());
                Arena();
            }

            if (arg0.name == "White_Palace_09" && 
                (arg1.name == DryyaScene || arg1.name == IsmaScene || 
                 arg1.name == ZemerScene || arg1.name == HegemolScene ||
                 arg1.name == "GG_White_Defender"))
            {
                On.CameraLockArea.OnTriggerEnter2D += CameraLockAreaOnOnTriggerEnter2D;
                StartCoroutine(AddComponent());
            }

            if (arg1.name == "White_Palace_09" && 
                (arg0.name == IsmaScene || arg0.name == DryyaScene || arg0.name == ZemerScene || arg0.name == HegemolScene || 
                 arg0.name == "GG_White_Defender" || arg0.name == "Dream_04_White_Defender"))
            {
                //GameCameras.instance.cameraFadeFSM.Fsm.SetState("FadeIn");
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
                PlayerData.instance.whiteDefenderDefeats = defeats;
            }
            
            if (arg1.name == "White_Palace_09")
            {
                ResetBossBundle();
                LoadHubBundles();
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
                    Destroy(fightCtrl);
                    Log("Killed fightCtrl2");
                }
            }
        }

        private void ResetBossBundle()
        {
            Log($"Destroying {CustomWP.boss}");
            if (CustomWP.boss == CustomWP.Boss.Dryya)
            {
                ABManager.ResetBundle(ABManager.Bundle.GDryya);
                ABManager.ResetBundle(ABManager.Bundle.GArenaD);
            }
            else if (CustomWP.boss == CustomWP.Boss.Hegemol)
            {
                ABManager.ResetBundle(ABManager.Bundle.GHegemol);
                ABManager.ResetBundle(ABManager.Bundle.GArenaH);
            }
            else if (CustomWP.boss == CustomWP.Boss.Isma || CustomWP.boss == CustomWP.Boss.Ogrim)
            {
                ABManager.ResetBundle(ABManager.Bundle.GIsma);
                ABManager.ResetBundle(ABManager.Bundle.GArenaI);
                ABManager.ResetBundle(ABManager.Bundle.GArenaIsma);
            }
            else if (CustomWP.boss == CustomWP.Boss.Ze || CustomWP.boss == CustomWP.Boss.Mystic)
            {
                ABManager.ResetBundle(ABManager.Bundle.GArenaZ);
                ABManager.ResetBundle(ABManager.Bundle.GZemer);
            }

            if (CustomWP.boss != CustomWP.Boss.None)
            {
                ABManager.ResetBundle(ABManager.Bundle.Sound);
            }

            Log("Destroying2");
        }

        private IEnumerator CameraFixer()
        {
            yield return new WaitWhile(() => GameManager.instance.gameState != GameState.PLAYING);
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
            FiveKnights.preloadedGO["hubfloor"] = ABManager.AssetBundles[ABManager.Bundle.GArenaHub2].LoadAsset<GameObject>("white_palace_floor_set_02 (16)");
            AssetBundle misc = ABManager.AssetBundles[ABManager.Bundle.Misc];
            FiveKnights.Materials["WaveEffectMaterial"] = misc.LoadAsset<Material>("WaveEffectMaterial");
            FiveKnights.Materials["flash"] = misc.LoadAsset<Material>("UnlitFlashMat");
            
            foreach (GameObject i in misc.LoadAllAssets<GameObject>())
            {
                if (i.name == "GG_Statue_Isma") FiveKnights.preloadedGO["GG_Statue_Isma"] = i;
                else if (i.name == "IsmaOgrimStatue") FiveKnights.preloadedGO["IsmaOgrimStatue"] = i;
                else if (i.name == "Shockwave") FiveKnights.preloadedGO["WaveShad"] = i;
                
                if (i.GetComponent<SpriteRenderer>() == null)
                {
                    foreach (SpriteRenderer sr in i.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        sr.material = new Material(Shader.Find("Sprites/Default"));
                    }
                }
                else i.GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));
            }

            Texture tex = misc.LoadAsset<Texture>("sonar");
            FiveKnights.Materials["TestDist"] = new Material(FiveKnights.Materials["WaveEffectMaterial"]);
            FiveKnights.Materials["TestDist"].SetTexture("_NoiseTex", tex);
            FiveKnights.Materials["TestDist"].SetFloat("_Intensity", 0.2f);
            FiveKnights.preloadedGO["WaveShad"].GetComponent<SpriteRenderer>().material = FiveKnights.Materials["TestDist"];
            Log("Loading hub bundle");
        }

        void Arena()
        {
            CustomWP.boss = CustomWP.Boss.None;

            FiveKnights.preloadedGO["Entrance"] = ABManager.AssetBundles[ABManager.Bundle.WSArena].LoadAsset<GameObject>("gg_workshop_pale_court_entrance");
            
            GameObject go = Instantiate(FiveKnights.preloadedGO["Entrance"]);
            go.SetActive(true);
            go.transform.position -= new Vector3(23.6f, 0f);
            foreach (SpriteRenderer i in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                i.material = new Material(Shader.Find("Sprites/Default"));
            }
            
            GameObject crack = Instantiate(FiveKnights.preloadedGO["StartDoor"]);
            crack.SetActive(true);
            crack.transform.position = new Vector3(28.07f, 37.68f, 4.21f);
            crack.transform.localScale = new Vector3(1.33f, 1.02f, 0.87f);
            GameObject secret = crack.transform.Find("GG_secret_door").gameObject;
            secret.GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));
            TransitionPoint tp = secret.transform.Find("door_Land_of_Storms").GetComponent<TransitionPoint>();
            tp.targetScene = "White_Palace_09";
            tp.entryPoint = "door_Land_of_Storms_return";
            secret.transform.Find("door_Land_of_Storms").gameObject.LocateMyFSM("Door Control")
                .FsmVariables.FindFsmString("New Scene").Value = "White_Palace_09";
            secret.transform.Find("door_Land_of_Storms").gameObject.LocateMyFSM("Door Control")
                .FsmVariables.FindFsmString("Entry Gate").Value = "door_Land_of_Storms_return";
            secret.LocateMyFSM("Deactivate").enabled = false;
            secret.SetActive(true);
            Log("Finished with crack setting");
        }

        private void OnDestroy()
        {
            USceneManager.activeSceneChanged -= SceneChanged;
            On.SceneManager.Start -= SceneManagerOnStart;
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
        }
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
        }*/
    }
}
