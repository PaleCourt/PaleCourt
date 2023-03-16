using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FiveKnights.Misc;
using FrogCore;
using GlobalEnums;
using HutongGames.PlayMaker.Actions;
using SFCore.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = Modding.Logger;
using Random = UnityEngine.Random;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace FiveKnights.BossManagement
{
    public class OWArenaFinder : MonoBehaviour
    {
        private static OWArenaFinder Instance;
        
        private const string DryyaScene = "dryya overworld";

        private const string ZemerScene = "zemer overworld arena";
        
        private const string HegemolScene = "hegemol overworld arena";
        
        private const string IsmaScene = "isma overworld";
        
        public static readonly string PrevDryScene = "Fungus3_48";

        public static readonly string PrevZemScene = "Room_Mansion";
        
        public static readonly string PrevHegScene = "Fungus2_21";

        public static readonly string PrevIsmScene = "Waterways_13";
        
        private const string SheoScene = "Room_nailmaster_02";

        private static Dictionary<string, Shader> ParticleMatToShader = new();
        
        public Dictionary<string, AnimationClip> clips;
        
        public static bool IsInOverWorld =>
            Instance != null && (Instance._currScene is DryyaScene or IsmaScene or ZemerScene or HegemolScene );

        private string _currScene;
        private string _prevScene;

        private void Start()
        {
            Instance = this;
            USceneManager.activeSceneChanged += USceneManagerOnactiveSceneChanged;
            On.GameManager.EnterHero += GameManagerOnEnterHero;
            On.GameManager.RefreshTilemapInfo += GameManagerOnRefreshTilemapInfo;
            On.CameraLockArea.OnTriggerEnter2D += CameraLockAreaOnOnTriggerEnter2D;
            On.GameManager.GetCurrentMapZone += GameManagerOnGetCurrentMapZone;
        }

        private string GameManagerOnGetCurrentMapZone(On.GameManager.orig_GetCurrentMapZone orig, GameManager self)
        {
            return _currScene is ZemerScene or DryyaScene or IsmaScene or HegemolScene ? MapZone.DREAM_WORLD.ToString() : orig(self);
        }

        private void CameraLockAreaOnOnTriggerEnter2D(On.CameraLockArea.orig_OnTriggerEnter2D orig, CameraLockArea self, Collider2D othercollider)
        {
            /*if (_currScene == ZemerScene && self.name == "CLA2")
            {
                HeroController.instance.superDash.SendEvent("SLOPE CANCEL");
            }*/

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
            orig(self, othercollider);
        }

        private void GameManagerOnRefreshTilemapInfo(On.GameManager.orig_RefreshTilemapInfo orig, GameManager self, string targetscene)
        {
            orig(self, targetscene);
            if (targetscene != DryyaScene && targetscene != ZemerScene && targetscene != HegemolScene &&
                targetscene != IsmaScene) return;
            self.sceneWidth = 500;
            self.sceneHeight = 500;
            self.tilemap.width = 500;
            self.tilemap.height = 500;
            FindObjectOfType<GameMap>().SetManualTilemap(0, 0, 500, 500);
        }

        private void GameManagerOnEnterHero(On.GameManager.orig_EnterHero orig, GameManager self, bool additivegatesearch)
        {
            switch (self.sceneName)
            {
                case SheoScene:
                {
                    Log("Sheo scene");
                    if (PlayerData.instance.nailsmithSheo && PlayerData.instance.sheoConvoNailsmith &&
                        PlayerData.instance.nailsmithConvoArt)
                    {
                        GameManager.instance.gameObject.AddComponent<Artists>();
                    }
                    break;
                }
                case DryyaScene:
                {
                    CreateGateway("door1", new Vector2(385.36f, 98.4f), Vector2.zero, 
                        null, null, true, false, true, 
                        GameManager.SceneLoadVisualizations.Dream);
                    break;
                }
                case IsmaScene:
                {
                    if (GameObject.Find("door1") != null)
                    {
                        Destroy(GameObject.Find("door1"));
                    }
                    CreateGateway("door1", new Vector2(385.36f, 98.4f), Vector2.zero, 
                        null, null, true, false, true, 
                        GameManager.SceneLoadVisualizations.Dream);
                    break;
                }
                case ZemerScene:
                {
                    if (GameObject.Find("door1") != null)
                    {
                        Destroy(GameObject.Find("door1"));
                    }
                    CreateGateway("door1", new Vector2(165.86f, 105.92f), Vector2.zero, 
                        null, null, true, false, true, 
                        GameManager.SceneLoadVisualizations.Dream);
                    break;
                }
                case HegemolScene:
                {
                    if (GameObject.Find("door1") != null)
                    {
                        Destroy(GameObject.Find("door1"));
                    }
                    CreateGateway("door1", new Vector2(165.86f, 105.92f), Vector2.zero, 
                        null, null, true, false, true, 
                        GameManager.SceneLoadVisualizations.Dream);
                    break;
                }
                default:
                {
                    if (self.sceneName == PrevDryScene)
                    {
                        CreateGateway("door_dreamReturn", new Vector2(40.5f, 94.4f), Vector2.zero, // 39.2f, 94.4f
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
                        CreateGateway("door_dreamReturn", new Vector2(22.1f, 6.4f), Vector2.zero, 
                            null, null, false, false, true, 
                            GameManager.SceneLoadVisualizations.Dream);
                    }
                    else if (self.sceneName == PrevHegScene)
                    {
                        CreateGateway("door_dreamReturn", new Vector2(114.1f, 12.4f), Vector2.zero, 
                            null, null, false, false, true, 
                            GameManager.SceneLoadVisualizations.Dream);
                    }

                    break;
                }
            }

            orig(self, additivegatesearch);
        }

        private void ArenaBundleManage()
        {
            Log("Arena bund");
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
                /*foreach (var i in FindObjectsOfType<GameObject>()
                    .Where(x => x.name == "Dream Dialogue"))
                {
                    Destroy(i);
                }*/
                CreateDreamGateway("Dream Enter", "door1", 
                    new Vector2(25.1f, 6.4f), new Vector2(3f, 3f), new Vector2(3f, 3f),
                    Vector2.zero, ZemerScene, PrevZemScene);
            }
            else if (_currScene == PrevHegScene)
            {
                if (_prevScene == HegemolScene)
                {
                    Log("Redoing Hegemol content");
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
                Log("load isma bund");
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
        }
        
        IEnumerator ShaderFixer()
        {
            yield return new WaitForSeconds(0.5f);
            foreach (var i in FindObjectsOfType<ParticleSystemRenderer>(true))
            {
                string matName = i.material.name;
                string badShader = "Hidden/InternalErrorShader";
                if (i.material.shader.name == badShader)
                {
                    if (i.material.name.Contains("Particle_Lift_Dust"))
                    {
                        i.material.shader = Shader.Find("Sprites/Lit");
                    }
                    else if (!ParticleMatToShader.ContainsKey(matName))
                    {
                        Log($"Did not have shader of mat {matName}");
                    }
                    else
                    {
                        //Log($"Changing material {matName} to have shader {ParticleMatToShader[matName]}");
                        i.material.shader = ParticleMatToShader[matName];
                    }
                }
                else
                {
                    if (ParticleMatToShader.ContainsKey(i.material.name) && 
                        ParticleMatToShader[i.material.name].name != badShader) continue;
                    ParticleMatToShader.Add(i.material.name, i.material.shader);   
                }
            }
        }

        private void USceneManagerOnactiveSceneChanged(Scene arg0, Scene arg1)
        {
            _currScene = arg1.name;
            _prevScene = arg0.name;

            StartCoroutine(ShaderFixer());
            
            switch (_currScene)
            {
                case DryyaScene:
                    Log("Trying to enter fight dryya");
                    CustomWP.boss = CustomWP.Boss.Dryya;
                    PlayerData.instance.dreamReturnScene = arg0.name;
                    FixBlur();
                    FixCameraDryya();
                    AddBattleGate(422.5f,new Vector3(421.925f, 99.5f));
                    DreamEntry();
                    AddSuperDashCancel();
                    FixPitDeath();
                    GameManager.instance.gameObject.AddComponent<OWBossManager>();
                    break;
                case IsmaScene:
                    Log("Trying to enter fight isma");

                    CustomWP.boss = CustomWP.Boss.Isma;
                    PlayerData.instance.dreamReturnScene = arg0.name;
                    FixBlur();
                    //FixCameraIsma();
                    AddBattleGate(110f,new Vector3(104.5f, 8.5f));
                    DreamEntry();
                    FixIsmaSprites();
                    // Falling off pit doesn't send you back anymore so this is here to patch that
                    AddSuperDashCancel();
                    FixPitDeath();
                    GameManager.instance.gameObject.AddComponent<OWBossManager>();
                    break;
                case ZemerScene:
                    Log("Trying to enter fight zemer");
                    CustomWP.boss = CustomWP.Boss.Ze;
                    PlayerData.instance.dreamReturnScene = PrevZemScene;
                    FixBlur();
                    AddBattleGate(243f, new Vector2(238.4f, 107f));
                    AddSuperDashCancel();
                    FixPitDeath();
                    DreamEntry();
                    GameManager.instance.gameObject.AddComponent<OWBossManager>();
                    break;
                case HegemolScene:
                    Log("Trying to enter fight hegemol");
                    CustomWP.boss = CustomWP.Boss.Hegemol;
                    PlayerData.instance.dreamReturnScene = PrevHegScene;
                    FixBlur();
                    FixHegemolArena();
                    AddSuperDashCancel();
                    FixPitDeath();
                    AddBattleGate(432f, new Vector2(420.925f, 156.8f));
                    DreamEntry();
                    GameManager.instance.gameObject.AddComponent<OWBossManager>();
                    break;
                default:
                    ArenaBundleManage();
                    break;
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
                de.name = "Dream Entry";
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
            GameObject pref = null;
            foreach (var i in FindObjectsOfType<SceneManager>())
            {
                var j = i.borderPrefab;
                pref = j;
                Destroy(i.gameObject);
            }
            GameObject o = Instantiate(FiveKnights.preloadedGO["SMTest"]);
            if (pref != null)
            {
                o.GetComponent<SceneManager>().borderPrefab = pref;
            }
            o.GetComponent<SceneManager>().noLantern = true;
            o.GetComponent<SceneManager>().darknessLevel = -1;
            o.SetActive(true);
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
                                      Vector2 min, Vector2 max, bool preventLookDown=false)
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
            parentlock.SetActive(true);
            lockCol.enabled = cla.enabled = true;
        }

        private void FixCameraIsma()
        {
            foreach (var i in FindObjectsOfType<CameraLockArea>())
            {
                Destroy(i);
            }
            CreateCameraLock("CLA1", new Vector2(50.24f,9.5f),new Vector2(108.83f, 25f),
                new Vector2(1f, 1f), new Vector2(0f, 0f), 
                new Vector2(0f, 12f), new Vector2(88.8f, 12f), true);

            CreateCameraLock("CLA2", new Vector2(122.3f, 9.5f),new Vector2(35.6f, 25f),
                new Vector2(1f, 1f), new Vector2(0f, 0f), 
                new Vector2(119f, 12f), new Vector2(125f, 12f), true);
                
            
            Log("Fixed floor");
        }

        private void FixIsmaSprites()
        {
            foreach (var i in FindObjectsOfType<MeshRenderer>()
                .Where(x=>x.gameObject.name.Contains("Chunk")))
            {
                i.material.shader = Shader.Find("Sprites/Default");
            }

            foreach (var i in FindObjectsOfType<ParticleSystemRenderer>())
            {
                string partic = i.name == "Fungus_Steam" ? "Sprites/Default" : "Particles/Additive (Soft)";
                i.material.shader = Shader.Find(partic);
            }

            foreach (Transform i in GameObject.Find("wp_clouds").transform)
            {
                i.GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));
            }

            foreach (var i in FindObjectsOfType<SpriteRenderer>()
                .Where(x=> x.name.Contains("_white") || 
                                      x.name.Contains("water_fog") || 
                                      x.name.Contains("wp_rib")))
            {
                i.material.shader = Shader.Find("Sprites/Default");
            }
        }

        private void FixPitDeath()
        {
            Log("Checking for Bottom");

            foreach (GameObject i in FindObjectsOfType<GameObject>()
                .Where(x => x.name == "Dream Fall Catcher"))
            {
                GameObject newDeath = Instantiate(FiveKnights.preloadedGO["DreamFall"]);
                BoxCollider2D newBott = newDeath.GetComponentInChildren<BoxCollider2D>();
                BoxCollider2D oldBott = i.GetComponentInChildren<BoxCollider2D>();
                newDeath.transform.position = i.transform.position;
                newBott.size = oldBott.size;
                newBott.offset = oldBott.offset;
                newBott.transform.position = oldBott.transform.position;
                newDeath.SetActive(true);
                newBott.gameObject.SetActive(true);
                var fsm = newDeath.LocateMyFSM("Control");
                fsm.GetAction<FloatCompare>("Detect", 1).float2 = 
                    newDeath.transform.GetPositionY() + newBott.size.y;
                Destroy(i);

            }

        }

        private void AddSuperDashCancel()
        {
            foreach (GameObject i in FindObjectsOfType<GameObject>()
                .Where(x => x.name.Contains("Superdash Cancel")))
            {
                i.AddComponent<SuperDashCancel>();
            }
        }
        
        private void FixHegemolArena()
        {
            foreach(var i in FindObjectsOfType<CameraLockArea>())
            {
                Destroy(i);
            }
            CreateCameraLock("CLA1", new Vector2(325f, 156.1f),new Vector2(5f, 1.5f),
                new Vector2(35.469f, 27.22f), new Vector2(0.707f, 2.554f), 
                new Vector2(263, 160f), new Vector2(402f, 160f), true);

            CreateCameraLock("CLA2", new Vector2(437.5f, 174f),new Vector2(5f, 1f),
                new Vector2(10f, 45f), new Vector2(1f,1.4f), 
                new Vector2(434.7f, 160f), new Vector2(442.7f, 160f), true);
            Log("Fixed floor");

            foreach(Renderer renderer in FindObjectsOfType<Renderer>())
			{
                if(renderer.gameObject.name.Contains("Arena Bottom Border") || renderer.gameObject.name.Contains("dream particles") ||
                    renderer.gameObject.name.Contains("Dream Exit Particle Field"))
				{
                    renderer.sortingOrder = 1;
				}
			}
            Log("Fixed renderer sorting orders");
        }

        private void AddBattleGate(float x, Vector2 pos)
        {
            IEnumerator WorkBattleGate()
            {
                foreach (GameObject i in FindObjectsOfType<GameObject>()
                    .Where(x => x.name.Contains("Battle Gate")))
                {
                    Destroy(i);
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
                audGate.pitch = Random.Range(0.9f, 1.2f);
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
            if (FiveKnights.preloadedGO.TryGetValue("Dryya2", out var go) && go != null)
            {
                Log("Already have Dryya");
                yield break;
            }
            
            yield return null;
            yield return null;
            
            AssetBundle dryyaAssetBundle = ABManager.AssetBundles[ABManager.Bundle.GDryya];
            foreach (var c in dryyaAssetBundle.LoadAllAssets<AnimationClip>())
            {
                Log($"Name of anim adding is {c.name}");
                FiveKnights.AnimClips[c.name] = c;
            }
            FiveKnights.preloadedGO["Dryya2"] = dryyaAssetBundle.LoadAsset<GameObject>("Dryya2");
            FiveKnights.preloadedGO["Stab Effect"] = dryyaAssetBundle.LoadAsset<GameObject>("Stab Effect");
            FiveKnights.preloadedGO["Dive Effect"] = dryyaAssetBundle.LoadAsset<GameObject>("Dive Effect");
            FiveKnights.preloadedGO["Beams"] = dryyaAssetBundle.LoadAsset<GameObject>("Beams");
            FiveKnights.preloadedGO["Beams"].GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));
            FiveKnights.preloadedGO["Dagger"] = dryyaAssetBundle.LoadAsset<GameObject>("Dagger");
			FiveKnights.preloadedGO["Dagger"].GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));

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
            foreach (var c in ab.LoadAllAssets<AnimationClip>())
            {
                Log($"Name of anim adding is {c.name}");
                FiveKnights.AnimClips[c.name] = c;
            }
            //AssetBundle ab2 = ABManager.AssetBundles[ABManager.Bundle.OWArenaI];
            //FiveKnights.preloadedGO["IsmaArena"] = ab2.LoadAsset<GameObject>("new stuff isma 1");
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

            Log("Finished Loading Isma Bundle");
        }

        private IEnumerator LoadHegemolBundle()
        {
            Log("Loading Hegemol Bundle");
            if(FiveKnights.preloadedGO.TryGetValue("Hegemol", out var go) && go != null)
            {
                Log("Already Loaded Hegemol");
                yield break;
            }

            yield return null;

            PlayMakerFSM fsm = FiveKnights.preloadedGO["Traitor"].LocateMyFSM("Mantis");
            FiveKnights.preloadedGO["TraitorSlam"] =
                fsm.GetAction<SpawnObjectFromGlobalPool>("Waves", 0).gameObject.Value;
            FiveKnights.Clips["TraitorSlam"] = fsm.GetAction<AudioPlayerOneShotSingle>("Waves", 4).audioClip.Value as AudioClip;

            AssetBundle hegemolBundle = ABManager.AssetBundles[ABManager.Bundle.GHegemol];

            FiveKnights.preloadedGO["Hegemol"] = hegemolBundle.LoadAsset<GameObject>("Hegemol");
            FiveKnights.preloadedGO["Mace"] = hegemolBundle.LoadAsset<GameObject>("Mace");
            FiveKnights.preloadedGO["Debris"] = hegemolBundle.LoadAsset<GameObject>("Debris");
            FiveKnights.preloadedGO["Mace"].GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));

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
            foreach (var c in ab.LoadAllAssets<AnimationClip>())
            {
                Log($"Name of anim adding is {c.name}");
                FiveKnights.AnimClips[c.name] = c;
            }
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

            clips = new Dictionary<string, AnimationClip>();
            foreach (var c in ab.LoadAllAssets<AnimationClip>())
            {
                clips[c.name] = c;
            }

            Log("Finished Loading Zemer Bundle");
        }
        
        private void CreateDreamGateway(string gateName, string toGate, Vector2 pos, Vector2 hitSize,
                                        Vector2 particSize, Vector2 particOff, string toScene, string returnScene)
        {
            Log("Creating Dream Gateway");
            GameObject dreamEnter = Instantiate(FiveKnights.preloadedGO["DPortal"]);
            dreamEnter.name = gateName;
            dreamEnter.SetActive(true);
            dreamEnter.transform.position = pos;
            dreamEnter.transform.localScale = Vector3.one;
            dreamEnter.transform.eulerAngles = Vector3.zero;
                
            var bc = dreamEnter.GetComponent<BoxCollider2D>();
            bc.size = hitSize;
            bc.offset = Vector2.zero;
            foreach (var pfsm in dreamEnter.GetComponents<PlayMakerFSM>())
            {
                if (pfsm.FsmName != "Control") continue;
                pfsm.FsmVariables.GetFsmString("Return Scene").Value = returnScene;
                pfsm.FsmVariables.GetFsmString("To Scene").Value = toScene;
                pfsm.GetAction<BeginSceneTransition>("Change Scene", 4).entryGateName = toGate;
            }

            var pt = dreamEnter.transform.Find("Attack Pt");
            pt.position += new Vector3(particOff.x, particOff.y);

            GameObject dreamPt = Instantiate(FiveKnights.preloadedGO["DPortal2"]);
            dreamPt.SetActive(true);
            dreamPt.transform.position = new Vector3(pos.x + particOff.x, pos.y + particOff.y, -0.002f);
            dreamPt.transform.localScale = Vector3.one;
            dreamPt.transform.eulerAngles = Vector3.zero;

            var shape = dreamPt.GetComponent<ParticleSystem>().shape;
            shape.scale = new Vector3(particSize.x, particSize.y, 0.001f);

            Log("Done Creating Dream Gateway");
        }

        private void OnDestroy()
        {
            USceneManager.activeSceneChanged -= USceneManagerOnactiveSceneChanged;
            On.GameManager.EnterHero -= GameManagerOnEnterHero;
            On.GameManager.RefreshTilemapInfo -= GameManagerOnRefreshTilemapInfo;
            On.CameraLockArea.OnTriggerEnter2D -= CameraLockAreaOnOnTriggerEnter2D;
            On.GameManager.GetCurrentMapZone -= GameManagerOnGetCurrentMapZone;
        }

        private static void Log(object o)
        {
            Logger.Log($"[OverWorldArena] {o}");
        }
    }
}