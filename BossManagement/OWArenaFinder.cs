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
            On.GameManager.GetCurrentMapZone += GameManagerOnGetCurrentMapZone;
        }

        private string GameManagerOnGetCurrentMapZone(On.GameManager.orig_GetCurrentMapZone orig, GameManager self)
        {
            return _currScene is ZemerScene or DryyaScene or IsmaScene or HegemolScene ? MapZone.DREAM_WORLD.ToString() : orig(self);
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
                        PlayerData.instance.nailsmithConvoArt && FiveKnights.Instance.SaveSettings.UnlockedChampionsCall)
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
                    if (self.sceneName == PrevDryScene && PlayerData.instance.GetBool(nameof(PlayerData.whiteDefenderDefeated)))
                    {
                        CreateGateway("door_dreamReturn", new Vector2(40.5f, 94.4f), Vector2.zero, // 39.2f, 94.4f
                            null, null, false, false, true, 
                            GameManager.SceneLoadVisualizations.Dream);
                    }
                    else if (self.sceneName == PrevIsmScene && PlayerData.instance.GetBool(nameof(PlayerData.whiteDefenderDefeated)))
                    {
                        CreateGateway("door_dreamReturn", new Vector2(95.7f, 18.4f), Vector2.zero, 
                            null, null, false, false, true, 
                            GameManager.SceneLoadVisualizations.Dream);
                    }
                    else if (self.sceneName == PrevZemScene && PlayerData.instance.GetBool(nameof(PlayerData.whiteDefenderDefeated)) &&
                            PlayerData.instance.GetBool(nameof(PlayerData.xunRewardGiven)))
                    {
                        CreateGateway("door_dreamReturn", new Vector2(22.1f, 6.4f), Vector2.zero, 
                            null, null, false, false, true, 
                            GameManager.SceneLoadVisualizations.Dream);
                    }
                    else if (self.sceneName == PrevHegScene && PlayerData.instance.GetBool(nameof(PlayerData.whiteDefenderDefeated)) &&
                            PlayerData.instance.GetBool(nameof(PlayerData.openedCityGate)))
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
            if (_currScene == PrevDryScene && PlayerData.instance.GetBool(nameof(PlayerData.whiteDefenderDefeated)))
            {
                if (_prevScene == DryyaScene)
                {
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
                BossLoader.LoadDryyaBundle();
                foreach (var i in FindObjectsOfType<GameObject>()
                    .Where(x => x.name == "Dream Dialogue"))
                {
                    Destroy(i);
                }
                CreateDreamGateway("Dream Enter", "door1", 
                    new Vector2(40.9f, 94.4f), new Vector2(3f, 3f), new Vector2(3f, 3f),
                    Vector2.zero, DryyaScene, PrevDryScene);
            }
            else if (_currScene == PrevZemScene && PlayerData.instance.GetBool(nameof(PlayerData.whiteDefenderDefeated)) && 
                PlayerData.instance.GetBool(nameof(PlayerData.xunRewardGiven)))
            {
                if (_prevScene == ZemerScene)
                {
                    if (OWBossManager.Instance != null)
                    {
                        Log("Destroying OWBossManager");
                        // Stop music first because the scene has no stop music trigger
                        OWBossManager.PlayMusic(null);
                        Destroy(OWBossManager.Instance);
                    }
                    var fsm = HeroController.instance.gameObject.LocateMyFSM("Dream Return");
                    fsm.FsmVariables.FindFsmBool("Dream Returning").Value = true;
                    HeroController.instance.RelinquishControl();
                }
                BossLoader.LoadZemerBundle();
                CreateDreamGateway("Dream Enter", "door1", 
                    new Vector2(25.1f, 6.4f), new Vector2(3f, 3f), new Vector2(3f, 3f),
                    Vector2.zero, ZemerScene, PrevZemScene);
            }
            else if (_currScene == PrevHegScene && PlayerData.instance.GetBool(nameof(PlayerData.whiteDefenderDefeated)) && 
                PlayerData.instance.GetBool(nameof(PlayerData.openedCityGate)))
            {
                if (_prevScene == HegemolScene)
                {
                    if (OWBossManager.Instance != null)
                    {
                        Log("Destroying OWBossManager");
                        Destroy(OWBossManager.Instance);
                    }
                    var fsm = HeroController.instance.gameObject.LocateMyFSM("Dream Return");
                    fsm.FsmVariables.FindFsmBool("Dream Returning").Value = true;
                    HeroController.instance.RelinquishControl();
                }
                BossLoader.LoadHegemolBundle();
                CreateDreamGateway("Dream Enter", "door1", 
                    new Vector2(118.1f, 13.5f), new Vector2(5f, 5f), new Vector2(3f, 3f),
                    Vector2.zero, HegemolScene, PrevHegScene);
                Log("Done with hegemol idiocy");
            }
            else if (_currScene == PrevIsmScene && PlayerData.instance.GetBool(nameof(PlayerData.whiteDefenderDefeated)))
            {
                if (_prevScene == IsmaScene)
                {
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
                BossLoader.LoadIsmaBundle();
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
                if(i.material.shader.name == badShader)
                {
                    if(i.material.name.Contains("Particle_Lift_Dust"))
                    {
                        i.material.shader = Shader.Find("Sprites/Lit");
                    }
                    else if(!ParticleMatToShader.ContainsKey(matName))
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
                    if(ParticleMatToShader.ContainsKey(i.material.name) &&
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
                    PlayerData.instance.dreamReturnScene = PrevDryScene;
                    FixBlur();
                    AddBattleGate(427.5f,new Vector3(421.925f, 99.5f));
                    DreamEntry();
                    AddSuperDashCancel();
                    FixPitDeath();
                    FixDryyaSpikes();
                    AddCreditsTablets();
                    GameManager.instance.gameObject.AddComponent<OWBossManager>();
                    break;
                case IsmaScene:
                    Log("Trying to enter fight isma");
                    CustomWP.boss = CustomWP.Boss.Isma;
                    PlayerData.instance.dreamReturnScene = PrevIsmScene;
                    FixBlur();
                    //FixCameraIsma();
                    AddBattleGate(110f,new Vector3(104.5f, 8.5f));
                    DreamEntry();
                    FixIsmaSprites();
                    AddSuperDashCancel();
                    // Falling off pit doesn't send you back anymore so this is here to patch that
                    FixPitDeath();
                    GameManager.instance.gameObject.AddComponent<OWBossManager>();
                    break;
                case ZemerScene:
                    Log("Trying to enter fight zemer");
                    CustomWP.boss = CustomWP.Boss.Ze;
                    PlayerData.instance.dreamReturnScene = PrevZemScene;
                    FixBlur();
                    AddBattleGate(243.9f, new Vector2(238.4f, 107f));
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
                    AddBattleGate(426.5f, new Vector2(420.925f, 156.8f));
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
			rm.transform.parent = gate.transform;
            rm.tag = "RespawnPoint";
            rm.transform.SetPosition2D(pos);
            tp.respawnMarker = rm.AddComponent<HazardRespawnMarker>();
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

        private void FixDryyaSpikes()
		{
            GameObject[] spikes = new GameObject[]
            {
                GameObject.Find("ruind_bridge_roof_02spikes"),
                GameObject.Find("ruind_bridge_roof_01").Find("ruind_bridge_roof_spike")
            };
            foreach(GameObject spike in spikes)
			{
                spike.layer = (int)PhysLayers.INTERACTIVE_OBJECT;
                spike.AddComponent<Pogoable>();
                spike.AddComponent<Tink>();

                DamageHero dh = spike.AddComponent<DamageHero>();
                dh.damageDealt = 1;
                dh.shadowDashHazard = true;
                dh.hazardType = 2;
            }
		}

        private void AddCreditsTablets()
		{
            GameObject parent = GameObject.Find("Credits Tablets");
            GameObject[] tablets = new GameObject[4]
            {
                parent.Find("Coding Tablet"),
                parent.Find("Art Tablet"),
                parent.Find("Sound Tablet"),
                parent.Find("Playtesting Tablet")
            };
            foreach(GameObject tablet in tablets)
			{
                GameObject shrine = Instantiate(FiveKnights.preloadedGO["Backer Shrine"], tablet.transform.position, Quaternion.identity);
                Destroy(shrine.GetComponent<SpriteRenderer>());
                Destroy(shrine.GetComponent<Breakable>());
                Destroy(shrine.GetComponent<PersistentBoolItem>());
                Destroy(shrine.GetComponent<BoxCollider2D>());
                Destroy(shrine.Find("Particle_rocks_small (3)"));
                Destroy(shrine.Find("Fungus Base Horned"));

                GameObject glowObject = shrine.Find("Active").Find("Glow Response Object");
                Destroy(glowObject.Find("Fade Sprite").GetComponent<SpriteRenderer>());
                GlowResponse glow = glowObject.GetComponent<GlowResponse>();
                glow.FadeSprites = new List<SpriteRenderer>() { tablet.Find("Overlay").GetComponent<SpriteRenderer>() };

                GameObject inspectObject = shrine.Find("Active").Find("Inspect Region");
                PlayMakerFSM inspectFSM = inspectObject.LocateMyFSM("inspect_region");
                inspectFSM.GetFsmStringVariable("Game Text Convo").Value = tablet.name.Split(new char[] { ' ' })[0].ToUpper() + "_CONVO";
                inspectFSM.GetFsmStringVariable("Game Text Sheet").Value = "Pale Court Credits";

                shrine.SetActive(true);
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
            cla.maxPriority = true;
            parentlock.SetActive(true);
            lockCol.enabled = cla.enabled = true;
        }

        private void FixIsmaSprites()
        {
			foreach(var i in FindObjectsOfType<SpriteRenderer>().Where(x=> x.name.Contains("_white")))
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
                new Vector2(18.9981f, 27.22f), new Vector2(-7.528451f, 2.554f), 
                new Vector2(263, 160f), new Vector2(323f, 160f), true);
            
            CreateCameraLock("CLA1B", new Vector2(325f, 156.1f),new Vector2(5f, 1.5f),
                new Vector2(16.48628f, 27.22f), new Vector2(10.19836f, 2.554f), 
                new Vector2(345f, 160f), new Vector2(402f, 160f), true);

            CreateCameraLock("CLA2", new Vector2(437.5f, 174f),new Vector2(5f, 1f),
                new Vector2(10f, 45f), new Vector2(1f,1.4f), 
                new Vector2(434.7f, 160f), new Vector2(442.7f, 160f), true);
            Log("Fixed floor");

            foreach(Renderer renderer in FindObjectsOfType<Renderer>())
			{
                if(renderer.gameObject.name.Contains("dream particles") ||
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
                audGate.outputAudioMixerGroup = HeroController.instance.GetComponent<AudioSource>().outputAudioMixerGroup;
                bcGate.enabled = false;
                
                yield return new WaitWhile(() => HeroController.instance.transform.position.x < x);
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
            Log("Destroyed OWArenaFinder");
            USceneManager.activeSceneChanged -= USceneManagerOnactiveSceneChanged;
            On.GameManager.EnterHero -= GameManagerOnEnterHero;
            On.GameManager.RefreshTilemapInfo -= GameManagerOnRefreshTilemapInfo;
            On.GameManager.GetCurrentMapZone -= GameManagerOnGetCurrentMapZone;
        }

        private static void Log(object o)
        {
            Logger.Log($"[OverWorldArena] {o}");
        }
    }
}