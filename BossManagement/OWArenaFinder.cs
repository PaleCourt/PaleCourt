using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using HutongGames.PlayMaker.Actions;
using ModCommon.Util;
using ModCommon;
using System.Linq;
using Logger = Modding.Logger;
using UObject = UnityEngine.Object;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace FiveKnights
{
    public class OWArenaFinder : MonoBehaviour
    {
        private static OWArenaFinder Instance;
        
        private const string DryyaScene = "dryya overworld";

        private const string ZemerScene = "zemer overworld arena";
        
        private const string HegemolScene = "hegemol overworld arena";
        
        private const string IsmaScene = "Dream_04_White_Defender";
        
        public static readonly string PrevDryScene = "Fungus3_48";

        public static readonly string PrevZemScene = "Fungus3_49";
        
        public static readonly string PrevHegScene = "Fungus2_21";

        public static readonly string PrevIsmScene = "Waterways_13";
        
        public static bool IsInOverWorld
        {
            get => Instance != null && (Instance._currScene == DryyaScene ||
                                        Instance._currScene == IsmaScene  ||
                                        Instance._currScene == ZemerScene ||
                                        Instance._currScene == HegemolScene );
        }
        
        private string _currScene;
        private string _prevScene;
        
        private IEnumerator Start()
        {
            Instance = this;
            USceneManager.activeSceneChanged += USceneManagerOnactiveSceneChanged;
            On.GameManager.EnterHero += GameManagerOnEnterHero;
            On.GameManager.RefreshTilemapInfo += GameManagerOnRefreshTilemapInfo;
            On.CameraLockArea.OnTriggerEnter2D += CameraLockAreaOnOnTriggerEnter2D;
            On.GameManager.GetCurrentMapZone += GameManagerOnGetCurrentMapZone;
            
            yield return new WaitWhile(()=>!Input.GetKey(KeyCode.R));
            
            Platform.Current.EncryptedSharedData.SetBool("IsmaAchiev2", false);
            yield return null;
            GameManager.instance.AwardAchievement("IsmaAchiev2");

            /*GameManager.instance.BeginSceneTransition(new GameManager.SceneLoadInfo()
            {
                EntryGateName = "right1",
                SceneName = PrevHegScene,
                Visualization = GameManager.SceneLoadVisualizations.Default,
                WaitForSceneTransitionCameraFade = false,
            });*/
        }

        private string GameManagerOnGetCurrentMapZone(On.GameManager.orig_GetCurrentMapZone orig, GameManager self)
        {
            if (_currScene == ZemerScene || _currScene == DryyaScene || 
                _currScene == IsmaScene || _currScene == HegemolScene)
            {
                return "DREAM_WORLD";
            }
            return orig(self);
        }

        private void CameraLockAreaOnOnTriggerEnter2D(On.CameraLockArea.orig_OnTriggerEnter2D orig, CameraLockArea self, Collider2D othercollider)
        {
            if (_currScene == DryyaScene)
            {
                self.cameraYMin = 103f;
                self.cameraYMax = 103f;
                if (self.gameObject.name.Contains("(1)(Clone)"))
                {
                    self.cameraXMin = 390f;
                    self.cameraXMax = 418f;
                }
                else
                {
                    // Right side
                    self.cameraXMin = 435.5f;
                    self.cameraXMax = 443f;
                }
                
                return;
            }
            Log($"minX: {self.cameraXMin} maxX {self.cameraXMax}, minY: {self.cameraYMin}, maxY {self.cameraYMax}");
            orig(self, othercollider);
        }

        private void GameManagerOnRefreshTilemapInfo(On.GameManager.orig_RefreshTilemapInfo orig, GameManager self, string targetscene)
        {
            orig(self, targetscene);
            if (targetscene == DryyaScene || targetscene == ZemerScene || 
                targetscene == HegemolScene || targetscene == IsmaScene)
            {
                self.sceneWidth = 500;
                self.sceneHeight = 500;
                self.tilemap.width = 500;
                self.tilemap.height = 500;
                FindObjectOfType<GameMap>().SetManualTilemap(0, 0, 500, 500);
            }
        }

        private void GameManagerOnEnterHero(On.GameManager.orig_EnterHero orig, GameManager self, bool additivegatesearch)
        {
            if (self.sceneName == DryyaScene)
            {
                CreateGateway("door1", new Vector2(385.36f, 98.4f), Vector2.zero, 
                    null, null, true, false, true, 
                    GameManager.SceneLoadVisualizations.Dream);
            }
            else if (self.sceneName == ZemerScene)
            {
                if (GameObject.Find("door1") != null)
                {
                    Destroy(GameObject.Find("door1"));
                }
                CreateGateway("door1", new Vector2(165.86f, 105.92f), Vector2.zero, 
                    null, null, true, false, true, 
                    GameManager.SceneLoadVisualizations.Dream);
            }
            else if (self.sceneName == HegemolScene)
            {
                if (GameObject.Find("door1") != null)
                {
                    Destroy(GameObject.Find("door1"));
                }
                CreateGateway("door1", new Vector2(165.86f, 105.92f), Vector2.zero, 
                    null, null, true, false, true, 
                    GameManager.SceneLoadVisualizations.Dream);
            }
            else if (self.sceneName == PrevDryScene)
            {
                CreateGateway("door_dreamReturn", new Vector2(39.2f, 94.4f), Vector2.zero, 
                    null, null, false, false, true, 
                    GameManager.SceneLoadVisualizations.Dream);
            }
            else if (self.sceneName == PrevIsmScene)
            {
                CreateGateway("door_dreamReturn", new Vector2(95.7f, 18.4f), Vector2.zero, 
                    null, null, false, false, true, 
                    GameManager.SceneLoadVisualizations.Dream);
            }
            else if (self.sceneName == PrevZemScene)
            {
                CreateGateway("door_dreamReturn", new Vector2(22f, 6.4f), Vector2.zero, 
                    null, null, false, false, true, 
                    GameManager.SceneLoadVisualizations.Dream);
            }
            else if (self.sceneName == PrevHegScene)
            {
                CreateGateway("door_dreamReturn", new Vector2(22f, 6.4f), Vector2.zero, 
                    null, null, false, false, true, 
                    GameManager.SceneLoadVisualizations.Dream);
            }

            orig(self, additivegatesearch);
        }

        private void ArenaBundleManage()
        {
            if (_currScene == PrevDryScene)
            {
                if (_prevScene == DryyaScene)
                {
                    Log("Redoing dryya content");
                    ABManager.ResetBundle(ABManager.Bundle.GDryya);
                    ABManager.ResetBundle(ABManager.Bundle.OWArenaD);
                    ABManager.ResetBundle(ABManager.Bundle.Sound);
                    if (OWBossManager.Instance != null)
                    {
                        Log("Destroying OWBossManager");
                        Destroy(OWBossManager.Instance);
                    }
                    var fsm = HeroController.instance.gameObject.LocateMyFSM("Dream Return");
                    fsm.FsmVariables.FindFsmBool("Dream Returning").Value = true;
                    HeroController.instance.RelinquishControl();
                    PlayerData.instance.disablePause = true;
                }
                
                StartCoroutine(LoadDryyaBundle());
                foreach (var i in FindObjectsOfType<GameObject>()
                    .Where(x => x.name == "Dream Dialogue"))
                {
                    Destroy(i);
                }
                CreateDreamGateway("Dream Enter", "door1", 
                    new Vector2(40.9f, 94.4f), new Vector2(3f, 3f), new Vector2(3f, 3f),
                    Vector2.zero, DryyaScene, PrevDryScene);
            }
            else if (_currScene == PrevZemScene)
            {
                if (_prevScene == ZemerScene)
                {
                    Log("Redoing Zemer content");
                    ABManager.ResetBundle(ABManager.Bundle.GZemer);
                    ABManager.ResetBundle(ABManager.Bundle.OWArenaZ);
                    ABManager.ResetBundle(ABManager.Bundle.Sound);
                    if (OWBossManager.Instance != null)
                    {
                        Log("Destroying OWBossManager");
                        Destroy(OWBossManager.Instance);
                    }
                    var fsm = HeroController.instance.gameObject.LocateMyFSM("Dream Return");
                    fsm.FsmVariables.FindFsmBool("Dream Returning").Value = true;
                    HeroController.instance.RelinquishControl();
                }
                
                StartCoroutine(LoadZemerBundle());
                foreach (var i in FindObjectsOfType<GameObject>()
                    .Where(x => x.name == "Dream Dialogue"))
                {
                    Destroy(i);
                }
                CreateDreamGateway("Dream Enter", "door1", 
                    new Vector2(22, 6.4f), new Vector2(3f, 3f), new Vector2(3f, 3f),
                    Vector2.zero, ZemerScene, PrevZemScene);
                Log("Done with zemer idiocy");
            }
            else if (_currScene == PrevHegScene)
            {
                if (_prevScene == HegemolScene)
                {
                    Log("Redoing Zemer content");
                    ABManager.ResetBundle(ABManager.Bundle.GHegemol);
                    ABManager.ResetBundle(ABManager.Bundle.OWArenaH);
                    ABManager.ResetBundle(ABManager.Bundle.Sound);
                    if (OWBossManager.Instance != null)
                    {
                        Log("Destroying OWBossManager");
                        Destroy(OWBossManager.Instance);
                    }
                    var fsm = HeroController.instance.gameObject.LocateMyFSM("Dream Return");
                    fsm.FsmVariables.FindFsmBool("Dream Returning").Value = true;
                    HeroController.instance.RelinquishControl();
                }

                StartCoroutine(LoadHegemolBundle());
                CreateDreamGateway("Dream Enter", "door1", 
                    new Vector2(118.1f, 13.5f), new Vector2(5f, 5f), new Vector2(3f, 3f),
                    Vector2.zero, HegemolScene, PrevHegScene);
                Log("Done with hegemol idiocy");
            }
            else if (_currScene == PrevIsmScene)
            {
                if (_prevScene == IsmaScene)
                {
                    Log("Redoing Isma content");
                    ABManager.ResetBundle(ABManager.Bundle.GIsma);
                    ABManager.ResetBundle(ABManager.Bundle.OWArenaI);
                    ABManager.ResetBundle(ABManager.Bundle.Sound);
                    if (OWBossManager.Instance != null)
                    {
                        Log("Destroying OWBossManager");
                        Destroy(OWBossManager.Instance);
                    }
                    var fsm = HeroController.instance.gameObject.LocateMyFSM("Dream Return");
                    fsm.FsmVariables.FindFsmBool("Dream Returning").Value = true;
                    HeroController.instance.RelinquishControl();
                    PlayerData.instance.disablePause = true;
                }
                
                StartCoroutine(LoadIsmaBundle());
                foreach (var i in FindObjectsOfType<GameObject>()
                    .Where(x => x.name == "Dream Dialogue"))
                {
                    Destroy(i);
                }
                CreateDreamGateway("Dream Enter", "door1", 
                    new Vector2(97.6f, 19.2f), new Vector2(5f, 5f), new Vector2(3f, 3f), 
                    new Vector2(0f, 4f), IsmaScene, PrevIsmScene);
            }
            else
            {
                Log("Unloading unused bosses");
                OnDestroy();
            }
        }

        private void USceneManagerOnactiveSceneChanged(Scene arg0, Scene arg1)
        {
            _currScene = arg1.name;
            _prevScene = arg0.name;
            
            if (_currScene == DryyaScene)
            {
                Log("Trying to enter fight dryya");
                CustomWP.boss = CustomWP.Boss.Dryya;
                PlayerData.instance.dreamReturnScene = arg0.name;
                FixBlur();
                FixCameraDryya();
                AddBattleGate(422.5f,new Vector3(421.91f, 99.5f));
                DreamEntry();
                GameManager.instance.gameObject.AddComponent<OWBossManager>();
            }
            else if (_currScene == IsmaScene)
            {
                Log("Trying to enter fight isma");
                CustomWP.boss = CustomWP.Boss.Isma;
                PlayerData.instance.dreamReturnScene = arg0.name;
                GameManager.instance.gameObject.AddComponent<OWBossManager>();
            }
            else if (_currScene == ZemerScene)
            {
                Log("Trying to enter fight zemer");
                CustomWP.boss = CustomWP.Boss.Ze;
                PlayerData.instance.dreamReturnScene = PrevZemScene;
                FixBlur();
                FixZemerArena();
                AddBattleGate(243f, new Vector2(238.4f, 107f));
                DreamEntry();
                GameManager.instance.gameObject.AddComponent<OWBossManager>();
            }
            else if (_currScene == HegemolScene)
            {
                Log("Trying to enter fight hegemol");
                CustomWP.boss = CustomWP.Boss.Hegemol;
                PlayerData.instance.dreamReturnScene = PrevHegScene;
                FixBlur();
                FixHegemolArena();
                AddBattleGate(432f, new Vector2(419.48f, 156.8f));
                DreamEntry();
                GameManager.instance.gameObject.AddComponent<OWBossManager>();
            }
            else
            {
                ArenaBundleManage();
            }
        }

        private void DreamEntry()
        {
            foreach (var i in FindObjectsOfType<GameObject>()
                .Where(x => x.name == "Dream Entry"))
            {
                HeroController.instance.isHeroInPosition = true;
                GameObject de = Instantiate(FiveKnights.preloadedGO["DreamEntry"]);
                de.transform.position = i.transform.position;
                Destroy(i);
                de.SetActive(true);
                HeroController.instance.FaceRight();
            }
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
        
        private void FixBlur()
        {
            GameObject o = Instantiate(FiveKnights.preloadedGO["SMTest"]);
            o.SetActive(true);
                
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
                .Where(x => x.name.Contains("BlurPlane")))
            {
                i.GetComponent<MeshRenderer>().materials = blurPlaneMaterials;
            }
        }
        private void FixCameraDryya()
        {
            GameObject parentlock = GameObject.Find("Battle Scene").transform.GetChild(0).gameObject;
            if (parentlock != null)
            {
                parentlock.SetActive(true);
                Transform camlock1 = parentlock.transform.Find("CameraLockArea (1)");
                camlock1.transform.localPosition = new Vector3(18.14f, 1.87f);
                GameObject camlock2 = Instantiate(camlock1.gameObject, parentlock.transform);
                camlock2.transform.localPosition = new Vector3(-15f, 1.87f);
                camlock2.transform.localScale = new Vector3(3f, 1f, 1f);
                BoxCollider2D bc = camlock2.GetComponent<BoxCollider2D>();
                bc.size = new Vector2(11.15505f, bc.size.y);
                bc.offset = new Vector2(-8.354845f, bc.offset.y);
                BoxCollider2D bc2 = camlock2.GetComponent<BoxCollider2D>();
                bc2.size = new Vector2(14.90794f, bc2.size.y);
                bc2.offset = new Vector2(-10.39885f, bc2.offset.y);
                camlock1.gameObject.SetActive(true);
                camlock2.SetActive(true);
                Log("Done setting locks up");
            }
        }

        private void CreateCameraLock(string n, Vector2 pos, Vector2 scl, Vector2 cSize, Vector2 cOff,
                                      Vector2 min, Vector2 max)
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
            parentlock.SetActive(true);
            lockCol.enabled = cla.enabled = true;
        }
        
        private void FixZemerArena()
        {
            foreach (var i in FindObjectsOfType<CameraLockArea>())
            {
                Destroy(i);
            }
            CreateCameraLock("CLA1", new Vector2(197.4f, 113.8f),new Vector2(3.41f, 1f),
                new Vector2(27.399f, 22.142f), new Vector2(-1.71f, 1.216f), 
                new Vector2(163f, 110f), new Vector2(225f, 110f));

            CreateCameraLock("CLA2", new Vector2(256.2f, 113.8f),new Vector2(3.41f, 1f),
                new Vector2(11.331f, 22.14f), new Vector2(0.389f, 1.216f), 
                new Vector2(253f, 110f), new Vector2(262f, 110f));

            GameObject floor = GameObject.Find("plattaform");
            floor.layer = (int) GlobalEnums.PhysLayers.TERRAIN;
            Log("Fixed floor");
        }
        
        private void FixHegemolArena()
        {
            foreach (var i in FindObjectsOfType<CameraLockArea>())
            {
                Destroy(i);
            }
            CreateCameraLock("CLA1", new Vector2(325f, 156.1f),new Vector2(5f, 1.5f),
                new Vector2(35.469f, 27.22f), new Vector2(0.707f, 2.554f), 
                new Vector2(263, 160f), new Vector2(402f, 160f));

            CreateCameraLock("CLA2", new Vector2(437.5f, 174f),new Vector2(5f, 1f),
                new Vector2(10f, 45f), new Vector2(1f,1.4f), 
                new Vector2(434.7f, 160f), new Vector2(442.7f, 160f));
            
            Log("Fixed floor");
        }

        private void AddBattleGate(float x, Vector2 pos)
        {
            IEnumerator WorkBattleGate()
            {
                GameObject oldBG = GameObject.Find("Battle Gate (1)");
                if (oldBG != null)
                {
                    Log("Found old battle gate");
                    Destroy(oldBG);
                }
            
                GameObject battleGate = Instantiate(FiveKnights.preloadedGO["BattleGate"]);
                battleGate.name = "opa";
                battleGate.SetActive(true);
                battleGate.transform.position = pos;
                battleGate.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                var fsm = battleGate.LocateMyFSM("BG Control");
                AudioClip close = fsm.GetAction<AudioPlayerOneShotSingle>("Close 1", 0).audioClip.Value as AudioClip;
                fsm.enabled = false;
                var animGate = battleGate.GetComponent<tk2dSpriteAnimator>();
                var bcGate = battleGate.GetComponent<BoxCollider2D>();
                var audGate = battleGate.GetComponent<AudioSource>();
                audGate.pitch = UnityEngine.Random.Range(0.9f, 1.2f);
                bcGate.enabled = false;
                
                yield return new WaitWhile(()=>HeroController.instance.transform.position.x < x);
                audGate.PlayOneShot(close);
                animGate.Play("BG Close 1");
                bcGate.enabled = true;
                yield return null;
                yield return new WaitWhile(() => animGate.IsPlaying("BG Close 1"));
                animGate.Play("BG Close 2");
                GameCameras.instance.cameraShakeFSM.SetState("EnemyKillShake");
                battleGate.transform.Find("Dust").GetComponent<ParticleSystem>().Play();
                battleGate.transform.Find("Close Effect").GetComponent<MeshRenderer>().enabled = true;
                battleGate.transform.Find("Close Effect").GetComponent<tk2dSpriteAnimator>().PlayFromFrame(0);
            }

            StartCoroutine(WorkBattleGate());
        }
        
        private IEnumerator LoadDryyaBundle()
        {
            Log("Loading Dryya Bundle");
            if (FiveKnights.preloadedGO.TryGetValue("Dryya", out var go) && go != null)
            {
                Log("Already have Dryya");
                yield break;
            }
            
            yield return null;
            yield return null;
            
            AssetBundle dryyaAssetBundle = ABManager.AssetBundles[ABManager.Bundle.GDryya];
            FiveKnights.preloadedGO["Dryya"] = dryyaAssetBundle.LoadAsset<GameObject>("Dryya");
            FiveKnights.preloadedGO["Stab Effect"] = dryyaAssetBundle.LoadAsset<GameObject>("Stab Effect");
            FiveKnights.preloadedGO["Dive Effect"] = dryyaAssetBundle.LoadAsset<GameObject>("Dive Effect");
            FiveKnights.preloadedGO["Elegy Beam"] = dryyaAssetBundle.LoadAsset<GameObject>("Elegy Beam");

            Log("Finished Loading Dryya Bundle");
        }
        
        private IEnumerator LoadIsmaBundle()
        {
            Log("Loading Isma Bundle");
            if (FiveKnights.preloadedGO.TryGetValue("Isma", out var go) && go != null)
            {
                Log("Already Loaded Isma");
                yield break;
            }

            yield return null;
            yield return null;
            
            AssetBundle ab = ABManager.AssetBundles[ABManager.Bundle.GIsma];
            AssetBundle ab2 = ABManager.AssetBundles[ABManager.Bundle.OWArenaI];
            FiveKnights.preloadedGO["IsmaArena"] = ab2.LoadAsset<GameObject>("new stuff isma 1");
            foreach (GameObject i in ab.LoadAllAssets<GameObject>())
            {
                if (i.name == "Isma") FiveKnights.preloadedGO["Isma"] = i;
                else if (i.name == "Gulka") FiveKnights.preloadedGO["Gulka"] = i;
                else if (i.name == "Plant") FiveKnights.preloadedGO["Plant"] = i;
                else if (i.name == "Fool") FiveKnights.preloadedGO["Fool"] = i;
                else if (i.name == "Wall") FiveKnights.preloadedGO["Wall"] = i;
                yield return null;
                if (i.GetComponent<SpriteRenderer>() == null)
                {
                    foreach (SpriteRenderer sr in i.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        sr.material = new Material(Shader.Find("Sprites/Default"));
                    }
                }
                else i.GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));
            }
            foreach (SpriteRenderer spr in FiveKnights.preloadedGO["IsmaArena"].GetComponentsInChildren<SpriteRenderer>(true))
            {
                spr.material = new Material(Shader.Find("Sprites/Default"));
            }

            Log("Finished Loading Isma Bundle");
        }

        private IEnumerator LoadHegemolBundle()
        {
            Log("Loading Hegemol Bundle");
            if (FiveKnights.preloadedGO.TryGetValue("Hegemol Collection Prefab", out var go) && go != null)
            {
                Log("Already Loaded Hegemol");
                yield break;
            }
            
            yield return null;
            yield return null;
            
            AssetBundle hegemolBundle = ABManager.AssetBundles[ABManager.Bundle.GHegemol];

            FiveKnights.preloadedGO["Hegemol Collection Prefab"] = hegemolBundle.LoadAsset<GameObject>("HegemolSpriteCollection");
            FiveKnights.preloadedGO["Hegemol Animation Prefab"] = hegemolBundle.LoadAsset<GameObject>("HegemolSpriteAnimation");
            //FiveKnights.preloadedGO["Mace"] = hegemolBundle.LoadAsset<GameObject>("Mace");

            Log("Finished Loading Hegemol Bundle");
        }
        
        private IEnumerator LoadZemerBundle()
        {
            Log("Loading Zemer Bundle");
            
            if (FiveKnights.preloadedGO.TryGetValue("Zemer", out var go) && go != null)
            {
                Log("Already Loaded Zemer");
                yield break;
            }
            
            PlayMakerFSM fsm = FiveKnights.preloadedGO["Traitor"].LocateMyFSM("Mantis");
            FiveKnights.preloadedGO["TraitorSlam"] =
                fsm.GetAction<SpawnObjectFromGlobalPool>("Waves", 0).gameObject.Value;
            FiveKnights.Clips["TraitorSlam"] = fsm.GetAction<AudioPlayerOneShotSingle>("Waves", 4).audioClip.Value as AudioClip;
            
            yield return null;
            yield return null;
            
            AssetBundle ab = ABManager.AssetBundles[ABManager.Bundle.GZemer];
            foreach (GameObject i in ab.LoadAllAssets<GameObject>())
            {
                if (i.name == "Zemer") FiveKnights.preloadedGO["Zemer"] = i;
                if (i.name == "TChild") FiveKnights.preloadedGO["TChild"] = i;
                else if (i.name == "NewSlash") FiveKnights.preloadedGO["SlashBeam"] = i;
                else if (i.name == "NewSlash2") FiveKnights.preloadedGO["SlashBeam2"] = i;
                yield return null;
                if (i.GetComponent<SpriteRenderer>() == null)
                {
                    foreach (SpriteRenderer sr in i.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        sr.material = new Material(Shader.Find("Sprites/Default"));
                    }
                }
                else i.GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));
            }
            FiveKnights.preloadedGO["SlashBeam"].GetComponent<SpriteRenderer>().material =
                new Material(Shader.Find("Sprites/Default"));

            Log("Finished Loading Zemer Bundle");
        }
        
        private void CreateDreamGateway(string gateName, string toGate, Vector2 pos, Vector2 hitSize,
                                        Vector2 particSize, Vector2 particOff, string toScene, string returnScene)
        {
            Log("Creating Dream Gateway");
            GameObject dreamEnter = GameObject.Instantiate(FiveKnights.preloadedGO["DPortal"]);
            dreamEnter.name = gateName;
            dreamEnter.SetActive(true);
            dreamEnter.transform.position = pos;
            dreamEnter.transform.localScale = Vector3.one;
            dreamEnter.transform.eulerAngles = Vector3.zero;

            dreamEnter.GetComponent<BoxCollider2D>().size = hitSize;
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

            var pt = dreamEnter.transform.Find("Attack Pt");
            pt.position += new Vector3(particOff.x, particOff.y);

            GameObject dreamPT = GameObject.Instantiate(FiveKnights.preloadedGO["DPortal2"]);
            dreamPT.SetActive(true);
            dreamPT.transform.position = new Vector3(pos.x + particOff.x, pos.y + particOff.y, -0.002f);
            dreamPT.transform.localScale = Vector3.one;
            dreamPT.transform.eulerAngles = Vector3.zero;

            var shape = dreamPT.GetComponent<ParticleSystem>().shape;
            shape.scale = new Vector3(particSize.x, particSize.y, 0.001f);

            Log("Done Creating Dream Gateway");
        }

        private void OnDestroy()
        {
            Log("Ending");
        }

        private void Log(object o)
        {
            Logger.Log($"[OverWorldArena] {o}");
        }
    }
}