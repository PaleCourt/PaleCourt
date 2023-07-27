using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using HutongGames.PlayMaker.Actions;
using System.Linq;
using FiveKnights.BossManagement;
using GlobalEnums;
using HutongGames.PlayMaker;
using SFCore.Utils;
using Vasi;
using Modding;
using Logger = Modding.Logger;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace FiveKnights
{
    internal class ArenaFinder : MonoBehaviour
    {
        public static Dictionary<string, tk2dSpriteAnimation> spriteAnimations;

        public static Dictionary<string, tk2dSpriteCollection> spriteCollections;

        public static Dictionary<string, tk2dSpriteCollectionData> collectionData;
        public static Dictionary<string, Sprite> Sprites { get; private set; } = new Dictionary<string, Sprite>();

        public static int defeats;

        private GGBossManager _ggBossManager;

        private static bool hasSummonElevator;

        private string prevScene;

        private string currScene;

        public const string Isma2Scene = "GG_White_Defender";

        public const string GauntletArena = "Dream_04_White_Defender";

        public const string DryyaScene = "gg dryya";

        public const string ZemerScene = "gg zemer";

        public const string HegemolScene = "gg hegemol";

        public const string IsmaScene = "gg isma";

        private const string PrevFightScene = "White_Palace_09";

        private int lastBossLevel;

        private BossStatue lastBossStatue;

        private bool _allowInteract;

        private void Start()
        {
            USceneManager.activeSceneChanged += SceneChanged;
            //On.SceneManager.Start += SceneManagerOnStart;
            On.BossStatueLever.OnTriggerEnter2D += BossStatueLever_OnTriggerEnter2D2;
            On.GameManager.BeginSceneTransition += GameManager_BeginSceneTransition;
            On.BossChallengeUI.LoadBoss_int_bool += BossChallengeUI_LoadBoss_int_bool;
            On.BossSceneController.Awake += BossSceneController_Awake;
            On.GameManager.GetCurrentMapZone += GameManagerOnGetCurrentMapZone;
            On.HeroController.CanInteract += HeroCanInteract;
            spriteAnimations = new Dictionary<string, tk2dSpriteAnimation>();
            spriteCollections = new Dictionary<string, tk2dSpriteCollection>();
            collectionData = new Dictionary<string, tk2dSpriteCollectionData>();
            _allowInteract = true;
        }

        private bool HeroCanInteract(On.HeroController.orig_CanInteract orig, HeroController self)
        {
            Log("HeroCanInteract");
            return _allowInteract && orig(self);
        }

        // Put this back in because we need it apparently??
        private string GameManagerOnGetCurrentMapZone(On.GameManager.orig_GetCurrentMapZone orig, GameManager self)
        {
            return currScene is ZemerScene or DryyaScene or IsmaScene or HegemolScene ? MapZone.GODS_GLORY.ToString() : orig(self);
        }
        
        private void BossChallengeUI_LoadBoss_int_bool(On.BossChallengeUI.orig_LoadBoss_int_bool orig, BossChallengeUI self, int level, bool doHideAnim)
        {
            lastBossLevel = level;
            lastBossStatue = Mirror.GetField<BossChallengeUI, BossStatue>(self, "bossStatue");
            orig(self, level, doHideAnim);
        }

        private void BossSceneController_Awake(On.BossSceneController.orig_Awake orig, BossSceneController self)
        {
            if(BossSceneController.SetupEvent == null)
            {
                Log("BSC SetupEvent is null");
                BossSceneController.SetupEvent = delegate (BossSceneController self)
                {
                    self.BossLevel = lastBossLevel;
                    self.DreamReturnEvent = "DREAM RETURN";
                    self.OnBossesDead += delegate ()
                    {
                        string fieldName = lastBossStatue.UsingDreamVersion ? lastBossStatue.dreamStatueStatePD : lastBossStatue.statueStatePD;
                        BossStatue.Completion playerDataVariable = GameManager.instance.GetPlayerDataVariable<BossStatue.Completion>(fieldName);
                        switch(lastBossLevel)
                        {
                            case 0:
                                playerDataVariable.completedTier1 = true;
                                break;
                            case 1:
                                playerDataVariable.completedTier2 = true;
                                break;
                            case 2:
                                playerDataVariable.completedTier3 = true;
                                break;
                        }
                        GameManager.instance.SetPlayerDataVariable<BossStatue.Completion>(fieldName, playerDataVariable);
                        GameManager.instance.playerData.SetString("currentBossStatueCompletionKey",
                            lastBossStatue.UsingDreamVersion ? lastBossStatue.dreamStatueStatePD : lastBossStatue.statueStatePD);
                        GameManager.instance.playerData.SetInt("bossStatueTargetLevel", lastBossLevel);
                    };
                    self.OnBossSceneComplete += delegate ()
                    {
                        self.DoDreamReturn();
                    };
                };
            }
            orig(self);
        }

        private void GameManager_BeginSceneTransition(On.GameManager.orig_BeginSceneTransition orig, GameManager self, GameManager.SceneLoadInfo info)
        {
            Log($"Before: Going to {info.SceneName} from {prevScene} using gate {info.EntryGateName}");
            if (info.SceneName == PrevFightScene)
            {
                if (CustomWP.boss == CustomWP.Boss.Isma || CustomWP.boss == CustomWP.Boss.Ogrim)
                {
                    info.EntryGateName = "door_dreamReturnGGstatueStateIsma_GG_Statue_Isma";
                }
                else if (CustomWP.boss == CustomWP.Boss.Ze || CustomWP.boss == CustomWP.Boss.Mystic)
                {
                    info.EntryGateName = "door_dreamReturnGGstatueStateZemer_GG_Statue_Zemer";
                }
                else if (CustomWP.boss == CustomWP.Boss.All)
                {
                    info.EntryGateName = "door_dreamReturnGGTestingIt";
                } 
                else if (CustomWP.boss == CustomWP.Boss.Dryya)
                {
                    info.EntryGateName = "door_dreamReturnGGstatueStateDryya_GG_Statue_Dryya";
                } 
                else if (CustomWP.boss == CustomWP.Boss.Hegemol)
                {
                    info.EntryGateName = "door_dreamReturnGGstatueStateHegemol_GG_Statue_Hegemol";
                }
            }
            else if (prevScene == "Dream_04_White_Defender" && info.SceneName == prevScene)
            {
                Log("in here boi");
                info.SceneName = PrevFightScene;
                info.EntryGateName = "door_dreamReturnGGTestingIt";
            }
            ModHooks.GetPlayerBoolHook -= GetPlayerBoolHook;
            if(info.SceneName == "White_Palace_09" && prevScene != "White_Palace_13") ModHooks.GetPlayerBoolHook += GetPlayerBoolHook;
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

        // This makes it so player cant immediately reinteract with doorway when teleporting between workshop and
        // pale court workshop
        private IEnumerator DelayInspection()
        {
            _allowInteract = false;
            
            bool change = false;
            tk2dSpriteAnimator tkAnim = HeroController.instance.GetComponent<tk2dSpriteAnimator>();
            string animName = "Exit Door To Idle";
            
            while (true)
            {
                if (change && tkAnim.CurrentClip.name == "Idle") break;
                if (!change && tkAnim.CurrentClip.name == animName) change = true;
                yield return null;
            }
            
            yield return new WaitForSeconds(1f);
            
            _allowInteract = true;
        }

        private void SceneChanged(Scene from, Scene to)
        {
            if(!(from.name is DryyaScene or HegemolScene or IsmaScene or ZemerScene or Isma2Scene or "hidden_reward_room" or 
                "Dream_04_White_Defender" or "GG_Workshop" or "Menu_Title") && to.name == "White_Palace_09")
            {
                CustomWP.isInGodhome = false;
                return;
            }

            if ((from.name == "GG_Workshop" && to.name == PrevFightScene) ||
                (from.name == PrevFightScene && to.name == "GG_Workshop"))
            {
                StartCoroutine(DelayInspection());
            }

            if (to.name is DryyaScene or IsmaScene or HegemolScene or ZemerScene)
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
                        Log("Found blur!");
                        i.SetActive(false);
                        i.GetComponent<MeshRenderer>().materials = blurPlaneMaterials;
                        i.SetActive(true);
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
                    //BossSceneController.Instance = bsc.GetComponent<BossSceneController>();
                    BossSceneController.Instance.transitionInHoldTime = 0;
                    var dreamEntryControlFsm = bsc.FindGameObjectInChildren("Dream Entry").LocateMyFSM("Control");
                    dreamEntryControlFsm.RemoveAction("Pause", 0);
                    dreamEntryControlFsm.AddAction("Pause", new NextFrameEvent() { sendEvent = FsmEvent.Finished });
                    bsc.SetActive(true); 
                    yield return null;
                    EventRegister.SendEvent("GG TRANSITION IN");
                    BossSceneController.Instance.GetType().GetProperty("HasTransitionedIn").SetValue(BossSceneController.Instance, true, null);
                    if (to.name is ZemerScene or HegemolScene)
                    {
                        bsc.GetComponent<BossSceneController>().heroSpawn.position = new Vector3(25.0f, 27.4f);
                        HeroController.instance.transform.position = new Vector3(25.0f, 27.4f); 
                        Log("Changed the hero's pos");
                    }
                    Log("Done trans in dream thing");
                    while(GameObject.Find("Godseeker Crowd") == null) yield return null;
                    GameObject oldGS = GameObject.Find("Godseeker Crowd");
                    GameObject newGS = Instantiate(FiveKnights.preloadedGO["Godseeker"]);
                    newGS.SetActive(true);
                    newGS.transform.position = oldGS.transform.position;
                    Destroy(oldGS);
                    GameObject.Find("GG_Arena_Prefab").GetComponent<AudioSource>().outputAudioMixerGroup = 
                        HeroController.instance.GetComponent<AudioSource>().outputAudioMixerGroup;
                }
                Log("Done dream entry");
            }

            if (from.name == "White_Palace_09")
            {
                Destroy(CustomWP.Instance);
                CustomWP.Instance = null;
            }
            
            if (to.name == "GG_Workshop")
            {
                StartCoroutine(CameraFixer());
                Arena();
            }

            if(CustomWP.boss != CustomWP.Boss.None && to.name is DryyaScene or IsmaScene or ZemerScene or HegemolScene or "GG_White_Defender")
            {
                SetSceneSettings(to);
                StartCoroutine(AddComponent());
            }

            if (to.name == "White_Palace_09" && 
                from.name is IsmaScene or DryyaScene or ZemerScene or HegemolScene or 
                "GG_White_Defender" or "Dream_04_White_Defender" or "hidden_reward_room")
            {
                PlayerData.instance.isInvincible = false;
                HeroController.instance.MaxHealth();
                PlayerData.instance.UpdateBlueHealth();
                HeroController.instance.ClearMPSendEvents();
            }

            if ((from.name == "White_Palace_09" && to.name == "Dream_04_White_Defender") ||
                (from.name == "Dream_04_White_Defender" && to.name == "Dream_04_White_Defender" 
                                                        && CustomWP.boss == CustomWP.Boss.All))
            {
                StartCoroutine(AddComponent());
                StartCoroutine(CameraFixer());
                HeroController.instance.MaxHealth();
                PlayerData.instance.UpdateBlueHealth();
                HeroController.instance.ClearMPSendEvents();
                HeroController.instance.EnterWithoutInput(true);
            }

            if (to.name == "White_Palace_09" && from.name == "Dream_04_White_Defender")
            {
                PlayerData.instance.whiteDefenderDefeats = defeats;
            }
            
            if (to.name == "White_Palace_09")
            {
                CustomWP.isInGodhome = true;
                LoadHubBundles();
                if (CustomWP.Instance == null)
                {
                    GameManager.instance.gameObject.AddComponent<CustomWP>();
                }
                StartCoroutine(CameraFixer());
                MakeBench(to.name, "WhiteBenchNew2", new Vector3(110.6f, 94.1f, 1));
            }

            if (_ggBossManager != null && to.name != "Dream_04_White_Defender" 
                                  && to.name != "GG_White_Defender" && to.name != DryyaScene
                                  && to.name != IsmaScene && to.name != HegemolScene
                                  && to.name != ZemerScene)
            {
                Log("Destroying GGBossManager");
                if (_ggBossManager != null)
                {
                    Destroy(_ggBossManager);
                    Log("Killed GGBossManager");
                }
            }
        }

        private bool GetPlayerBoolHook(string name, bool orig)
        {
            if(name == nameof(PlayerData.gotKingFragment))
            {
                return true;
            }
            return orig;
        }

        private IEnumerator CameraFixer()
        {
            yield return new WaitWhile(() => GameManager.instance.gameState != GameState.PLAYING);
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
            PlayerData.instance.respawnScene = PrevFightScene;
            PlayerData.instance.respawnMarkerName = go.name;
        }

        private IEnumerator AddComponent()
        {
            yield return null;
            _ggBossManager = GameManager.instance.gameObject.AddComponent<GGBossManager>();
        }

        private void LoadHubBundles()
        {
            FiveKnights.preloadedGO["hubfloor"] = ABManager.AssetBundles[ABManager.Bundle.GArenaHub2].LoadAsset<GameObject>("white_palace_floor_set_02 (16)");
            AssetBundle misc = ABManager.AssetBundles[ABManager.Bundle.Misc];
            FiveKnights.Materials["WaveEffectMaterial"] = misc.LoadAsset<Material>("WaveEffectMaterial");
            
            foreach (GameObject i in misc.LoadAllAssets<GameObject>())
            {
                if (i.name == "GG_Statue_Isma") FiveKnights.preloadedGO["GG_Statue_Isma"] = i;
                else if (i.name == "IsmaOgrimStatue") FiveKnights.preloadedGO["IsmaOgrimStatue"] = i;
                else if (i.name == "IsmaOgrimStatue_Silver") FiveKnights.preloadedGO["IsmaOgrimStatue_Silver"] = i;
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

        private void Arena() 
        {
            CustomWP.boss = CustomWP.Boss.None;

            var ddstat = GameObject.Find("GG_Statue_Defender");
            ddstat.transform.position = new Vector3(57.19f, 36.34f, 0.2f);

            ddstat.Find("Spotlight").Find("Glow Response statue_beam").GetComponent<BoxCollider2D>().size = new Vector2(11f, 100f);
            
            CreateCameraLock("CameraLockStat", new Vector2(57.6f, 49.7f), new Vector2(1f, 1f),
                new Vector2(7.5f, 16f), new Vector2(0f, 0f), 
                new Vector2(-1f, 43f), new Vector2(-1f, 43f));

            FiveKnights.preloadedGO["Entrance"] = ABManager.AssetBundles[ABManager.Bundle.WSArena]
                .LoadAsset<GameObject>("gg_workshop_pale_court_entrance");
            
            GameObject entrace = Instantiate(FiveKnights.preloadedGO["Entrance"]);
            entrace.SetActive(true);
            entrace.transform.position = new Vector3(56.46f, 20.3f,2.6f);
            entrace.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            entrace.transform.Find("StartTrigger").gameObject.AddComponent<StatueRaiseTrigger>().DDStatue = ddstat;
            Destroy(GameObject.Find("GG_pillar_bg (5)"));
            Destroy(GameObject.Find("GG_statues_0003_2 (5)"));
        }
        
        void SetSceneSettings(Scene scene)
        {
            GameObject pref = null;
            foreach (var i in FindObjectsOfType<SceneManager>())
            {
                var j = i.borderPrefab;
                pref = j;
                Destroy(i.gameObject);
            }
            GameObject o = Instantiate(FiveKnights.preloadedGO["SMTest"]);
            SceneManager sm = o.GetComponent<SceneManager>();
            if (pref != null) sm.borderPrefab = pref;

            // Does not work, affects all other scenes. Leaving in case we find a way to make this work in the future
            //GameCameras.instance.mainCamera.backgroundColor = new Color(0.65f, 0.65f, 0.65f);
            //bool lightBlurEnabled = GameCameras.instance.GetComponent<LightBlurredBackground>().enabled;
            //GameCameras.instance.GetComponent<LightBlurredBackground>().enabled = false;
            //GameCameras.instance.GetComponent<LightBlurredBackground>().enabled = lightBlurEnabled;

            sm.noLantern = true;
            sm.darknessLevel = -1;
            sm.sceneType = SceneType.GAMEPLAY;
            sm.saturation = 0.78f;
            sm.defaultIntensity = 0.968f;
            sm.defaultColor = new Color(0.934f, 0.961f, 0.961f, 1f);
            sm.mapZone = MapZone.GODS_GLORY;
            sm.noParticles = true;
            switch (scene.name)
            {
                case ZemerScene:
                    sm.environmentType = 7;
                    break;
                case DryyaScene:
                case IsmaScene:
                    sm.environmentType = 1;
                    break;
                case HegemolScene:
                    sm.environmentType = 0;
                    break;
            }
            o.SetActive(true);
        }

        private void CreateCameraLock(string n, Vector2 pos, Vector2 scl, Vector2 cSize, Vector2 cOff,
            Vector2 min, Vector2 max, bool preventLookDown = false)
        {
            GameObject parentlock = new GameObject(n);
            BoxCollider2D lockCol = parentlock.AddComponent<BoxCollider2D>();
            CameraLockArea cla = parentlock.AddComponent<CameraLockArea>();
            parentlock.transform.position = pos;
            parentlock.transform.localScale = scl;
            lockCol.isTrigger = true;
            lockCol.size = cSize;
            lockCol.offset = cOff;
            cla.cameraXMin = min.x;
            cla.cameraXMax = max.x;
            cla.cameraYMin = cla.cameraYMax = min.y;
            cla.preventLookDown = preventLookDown;
            cla.maxPriority = true;
            parentlock.SetActive(true);
            lockCol.enabled = cla.enabled = true;
        }

        private void OnDestroy()
        {
            Log("Destroyed ArenaFinder");
            USceneManager.activeSceneChanged -= SceneChanged;
            //On.SceneManager.Start -= SceneManagerOnStart;
            ModHooks.GetPlayerBoolHook -= GetPlayerBoolHook;
            On.BossStatueLever.OnTriggerEnter2D -= BossStatueLever_OnTriggerEnter2D2;
            On.GameManager.BeginSceneTransition -= GameManager_BeginSceneTransition;
            On.BossChallengeUI.LoadBoss_int_bool -= BossChallengeUI_LoadBoss_int_bool;
            On.BossSceneController.Awake -= BossSceneController_Awake;
            On.GameManager.GetCurrentMapZone -= GameManagerOnGetCurrentMapZone;
            On.HeroController.CanInteract -= HeroCanInteract;
        }

        private void Log(object o)
        {
            Logger.Log("[Scene] " + o);
        }

        class StatueRaiseTrigger : MonoBehaviour
        {
            public GameObject DDStatue;

            private void Start()
            {
                if(!PlayerData.instance.statueStateWhiteDefender.hasBeenSeen) return;
                if(!FiveKnights.Instance.SaveSettings.HasSeenWorkshopRaised) return;
                
                GameObject entrance = gameObject.transform.parent.gameObject;

                entrance.transform.position = new Vector3(56.46f, 28.11f, 2.1054f);
                DDStatue.transform.position = new Vector3(57.19f, 42.88f, 0.2f);

                SpawnPlatAndCrack();
            }

            private void OnTriggerStay2D(Collider2D col)
            {
                if(!PlayerData.instance.statueStateWhiteDefender.hasBeenSeen) return;
                if(FiveKnights.Instance.SaveSettings.HasSeenWorkshopRaised) return;
                FiveKnights.Instance.SaveSettings.HasSeenWorkshopRaised = true;
                StartCoroutine(RaisePlatform());
            }

            IEnumerator RaisePlatform()
            {
                PlayMakerFSM roarFSM = HeroController.instance.gameObject.LocateMyFSM("Roar Lock");
                roarFSM.GetFsmGameObjectVariable("Roar Object").Value = gameObject;
                roarFSM.SendEvent("ROAR ENTER");

                GameObject entrance = gameObject.transform.parent.gameObject;
                Transform parent = FiveKnights.preloadedGO["ObjRaise"].transform;
                
                GameCameras.instance.cameraShakeFSM.FsmVariables.FindFsmBool("RumblingMed").Value = true;
                GameObject raiseAud = Instantiate(parent.Find("Rise Audio").gameObject);
                raiseAud.SetActive(true);
                Destroy(raiseAud.GetComponent<PlayAudioAndRecycle>());
                HeroController.instance.PlayAudio(raiseAud.GetComponent<AudioSource>().clip);
                yield return new WaitForSeconds(0.5f);

                GameObject rumble = Instantiate(parent.Find("Rumble Dust").gameObject);
                rumble.SetActive(true);
                rumble.transform.position = new Vector3(56.1f, 35.1f);
                var rumblePartic = rumble.GetComponent<ParticleSystem>();
                rumblePartic.Play();

                GameObject rumble2 = Instantiate(parent.Find("Rumble Dust").gameObject);
                rumble2.SetActive(true);
                rumble2.transform.position = new Vector3(58.1f, 35.1f);
                var rumblePartic2 = rumble2.GetComponent<ParticleSystem>();
                rumblePartic2.Play();

                var ddstatEnd = new Vector3(57.19f, 42.88f, 0.2f);
                var pillarEnd = new Vector3(56.46f, 28.11f, 2.1054f);
                var ddstatStart = DDStatue.transform.position;
                var pillarStart = entrance.transform.position;
                float duration = 2.5f;
                float elapsedTime = 0f;
                while (elapsedTime < duration)
                {
                    //float normalized = elapsedTime / duration;
                    //float easeOut = normalized * (1f - 0.5f * normalized);
                    entrance.transform.position = Vector3.Lerp(pillarStart, pillarEnd, elapsedTime / duration);
                    DDStatue.transform.position = Vector3.Lerp(ddstatStart, ddstatEnd, elapsedTime / duration);
                    elapsedTime += Time.deltaTime;
                    yield return new WaitForEndOfFrame();
                }
            
                entrance.transform.position = pillarEnd;
                DDStatue.transform.position = ddstatEnd;
                GameCameras.instance.cameraShakeFSM.FsmVariables.FindFsmBool("RumblingMed").Value = false;
                
                rumblePartic.Stop();
                rumblePartic2.Stop();

                GameObject dust = Instantiate(parent.Find("Rise Dust").gameObject);
                dust.SetActive(true);
                dust.transform.position = new Vector3(56.1f, 35.1f);
                dust.GetComponent<ParticleSystem>().Play();
                GameObject dust2 = Instantiate(parent.Find("Rise Dust").gameObject);
                dust2.SetActive(true);
                dust2.transform.position = new Vector3(59.3f, 35.1f);
                dust2.GetComponent<ParticleSystem>().Play();
                
                GameObject impactAud = Instantiate(parent.Find("Impact Audio").gameObject);
                impactAud.SetActive(true);
                Destroy(impactAud.GetComponent<PlayAudioAndRecycle>());
                HeroController.instance.PlayAudio(impactAud.GetComponent<AudioSource>().clip);
                
                SpawnPlatAndCrack();
                yield return new WaitForSeconds(0.3f);

                roarFSM.SendEvent("ROAR EXIT");
                dust.GetComponent<ParticleSystem>().Stop();
                dust2.GetComponent<ParticleSystem>().Stop();

                yield return new WaitForSeconds(2f);
                Destroy(rumble);
                Destroy(rumble2);
                Destroy(impactAud);
                Destroy(raiseAud);
                Destroy(dust);
                Destroy(dust2);
            }

            private void SpawnPlatAndCrack()
            {
                GameObject entrance = gameObject.transform.parent.gameObject;

                foreach (Transform platPos in entrance.transform.Find("Platforms"))
                {
                    GameObject plat = Instantiate(FiveKnights.preloadedGO["RadPlat"]);
                    plat.transform.position = platPos.position;
                    plat.SetActive(true);
                    PlayMakerFSM fsm = plat.LocateMyFSM("radiant_plat"); 
                    fsm.SetState("Init");
                    fsm.SendEvent("APPEAR");
                }
                
                entrance.transform.Find("EntirePillar").Find("main pillar").Find("GG_pillar_top")
                    .GetComponent<BoxCollider2D>().enabled = true;
                
                GameObject crack = Instantiate(FiveKnights.preloadedGO["StartDoor"]);
                Destroy(crack.transform.Find("GG_secret_door").GetComponent<AudioSource>());
                crack.SetActive(true);
                crack.transform.position = new Vector3(57.45f, 38f, 4.21f);
                crack.transform.localScale = new Vector3(1.33f, 1.02f, 0.87f);
                GameObject secret = crack.transform.Find("GG_secret_door").gameObject;
                secret.GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));
                TransitionPoint tp = secret.transform.Find("door_Land_of_Storms").GetComponent<TransitionPoint>();
                tp.targetScene = PrevFightScene;
                tp.entryPoint = "door_Land_of_Storms_return";
                secret.transform.Find("door_Land_of_Storms").gameObject.LocateMyFSM("Door Control")
                    .FsmVariables.FindFsmString("New Scene").Value = PrevFightScene;
                secret.transform.Find("door_Land_of_Storms").gameObject.LocateMyFSM("Door Control")
                    .FsmVariables.FindFsmString("Entry Gate").Value = "door_Land_of_Storms_return";
                secret.LocateMyFSM("Deactivate").enabled = false;
                secret.SetActive(true);
            }
        }
        
    }
}
