using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FiveKnights.Misc;
using FrogCore;
using Modding;
using SFCore.Utils;
using UnityEngine;
using UnityEngine.Audio;
using GlobalEnums;
using HutongGames.PlayMaker.Actions;
using Logger = Modding.Logger;
using FrogCore.Fsm;
using UnityEngine.SceneManagement;


namespace FiveKnights
{
    public static class BloomParkour
    {
        private static List<string> spikygates = new List<string>()
        {
           "spiky_gate_1",
           "spiky_gate_2",
           "spiky_gate_3",
           "spiky_gate_4",
           "spiky_gate_5"
        };

        public static void Hook()
        {
            //On.GameManager.GetCurrentMapZone += GameManagerGetCurrentMapZone;
            On.GameManager.EnterHero += GameManagerEnterHero;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += ActiveSceneChanged;
        }
        public static void Unhook()
        {
            //On.GameManager.GetCurrentMapZone -= GameManagerGetCurrentMapZone;
            On.GameManager.EnterHero -= GameManagerEnterHero;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= ActiveSceneChanged;
        }

        private static void ActiveSceneChanged(Scene From, Scene To)
        {
            if (To.name == "Abyss_09")
            {
                SetupEntranceTerrain(To);
                CreateGateway("palecourt_top1", new Vector2(209.6533f, 60), new Vector2(4, 1), "Parkour", "entry_bot1", true, false, false, GameManager.SceneLoadVisualizations.Default);
            }
            if (To.name == "Abyss_10")
            {
                SetupExitTerrain(To);
                CreateGateway("palecourt_top2", new Vector2(102.28f, 31.4f), new Vector2(4, 1), "Parkour", "entry_bot2", true, true, false, GameManager.SceneLoadVisualizations.Default);
            }
            if (To.name == "Parkour")
            {
                SetSceneSettings();
                SetupShadegates(To);
                SetupBreakables(To);
                FixHazardSpikes(To);
            }
        }
        
        private static void SetSceneSettings()
        {
            GameObject pref = null;
            foreach (var i in UnityEngine.Object.FindObjectsOfType<SceneManager>())
            {
                var j = i.borderPrefab;
                pref = j;
                UnityEngine.Object.Destroy(i.gameObject);
            }
            GameObject o = UnityEngine.Object.Instantiate(FiveKnights.preloadedGO["AbyssSM"]);
            SceneManager sm = o.GetComponent<SceneManager>(); 
            if (pref != null) sm.borderPrefab = pref; 

            sm.noLantern = false;
            sm.darknessLevel = 1;
            sm.isWindy = false;
            sm.sceneType = SceneType.GAMEPLAY;
            sm.saturation = 0.5f;
            sm.defaultIntensity = 0.6f;
            sm.defaultColor = new Color(1f, 0.9117647f, 0.7794118f, 1f);
            sm.heroLightColor = new Color(1f, 0.9472616f, 0.8529412f, 0.3843137f);
            sm.mapZone = MapZone.ABYSS;
            sm.noParticles = false;
            sm.overrideParticlesWith = MapZone.NONE;
            sm.environmentType = 0;
            sm.ignorePlatformSaturationModifiers = false;
            o.SetActive(true);
        }
        private static void SetupShadegates(Scene scene)
        {
            var shadegate = GameObject.Instantiate(FiveKnights.preloadedGO["ShadeGate"]);
            foreach (string name in spikygates)
            {
                var spikygate = scene.Find(name);
                var z = spikygate.transform.rotation.eulerAngles.z;

                spikygate.GetComponent<AudioSource>().outputAudioMixerGroup = shadegate.GetComponent<AudioSource>().outputAudioMixerGroup;

                var slasheffect = GameObject.Instantiate(shadegate.Find("Slash Effect"));
                slasheffect.name = "Slash Effect";
                slasheffect.transform.parent = spikygate.transform;
                slasheffect.transform.position = z == 0 ? spikygate.transform.position + new Vector3(3.2f, 0, 0) : spikygate.Find("shade_gate_0001_shadegate_front").transform.position;
                slasheffect.transform.SetScaleY(spikygate.Find("solid").transform.GetScaleX() * .8f); 
                if (z == 0)
                {
                    slasheffect.transform.rotation = Quaternion.Euler(0, 0, 90);
                    var slashfsm = slasheffect.LocateMyFSM("Control");
                    var effectS = slashfsm.GetState("Effect");
                    slashfsm.GetState("Init").GetAction<GetPosition>(2).y = slashfsm.FindFloatVariable("Self X");
                    effectS.GetAction<CheckTargetDirection>(7).aboveEvent = slashfsm.FsmEvents[2];
                    effectS.GetAction<CheckTargetDirection>(7).belowEvent = slashfsm.FsmEvents[3];
                    effectS.GetAction<CheckTargetDirection>(7).aboveEvent = null;
                    effectS.GetAction<CheckTargetDirection>(7).aboveEvent = null;
                    effectS.GetAction<GetPosition>(2).x = slashfsm.FindFloatVariable("Hero Y");
                    effectS.GetAction<GetPosition>(2).y = 0;
                    effectS.GetAction<SetPosition>(5).x = slashfsm.FindFloatVariable("Hero Y");
                    effectS.GetAction<SetPosition>(5).y = 0;
                    effectS.GetAction<SetPosition>(6).x = slashfsm.FindFloatVariable("Hero Y");
                    effectS.GetAction<SetPosition>(6).y = 0;
                    slashfsm.GetState("L").GetAction<SetPosition>(2).y = slashfsm.FindFloatVariable("Self X");
                    slashfsm.GetState("L").GetAction<SetPosition>(2).x = slashfsm.FindFloatVariable("Hero Y");
                    slashfsm.GetState("R").GetAction<SetPosition>(2).y = slashfsm.FindFloatVariable("Self X");
                    slashfsm.GetState("R").GetAction<SetPosition>(2).x = slashfsm.FindFloatVariable("Hero Y");
                    slashfsm.GetState("L").RemoveFsmAction(3);
                    slashfsm.GetState("R").GetAction<SendMessage>(3).functionCall.FunctionName = nameof(HeroController.instance.RecoilDown);
                    slashfsm.GetState("Pause").GetAction<SetPosition>(3).y = slashfsm.FindFloatVariable("Self X");
                    slashfsm.GetState("Pause").GetAction<SetPosition>(3).x = slashfsm.FindFloatVariable("Hero Y");
                }
                    slasheffect.SetActive(true);

                var dasheffect = GameObject.Instantiate(shadegate.Find("Dash Effect"));
                dasheffect.name = "Dash Effect";
                dasheffect.transform.parent = spikygate.transform;
                dasheffect.transform.position = z == 0 ? spikygate.transform.position + new Vector3(3.2f, 0, 0) : spikygate.Find("shade_gate_0001_shadegate_front").transform.position;
                dasheffect.transform.SetScaleY(spikygate.Find("solid").transform.GetScaleX() * .8f); 

                if (z == 0)
                {
                    dasheffect.transform.rotation = Quaternion.Euler(0, 0, 90);

                    var dashfsm = dasheffect.LocateMyFSM("Control");
                    var effectD = dashfsm.GetState("Effect");
                    effectD.GetAction<GetPosition>(3).x = dashfsm.FindFloatVariable("Hero Y");
                    effectD.GetAction<GetPosition>(3).y = 0;
                    effectD.GetAction<SetPosition>(7).x = dashfsm.FindFloatVariable("Hero Y");
                    effectD.GetAction<SetPosition>(7).y = 0;
                    effectD.GetAction<SetPosition>(8).x = dashfsm.FindFloatVariable("Hero Y");
                    effectD.GetAction<SetPosition>(8).y = 0;
                    effectD.GetAction<SetPosition>(9).x = dashfsm.FindFloatVariable("Hero Y");
                    effectD.GetAction<SetPosition>(9).y = 0;
                    effectD.RemoveFsmAction(4);
                }
                dasheffect.SetActive(true);

                var particlesystem = GameObject.Instantiate(shadegate.Find("Particle System"));
                particlesystem.name = "Particle System";
                particlesystem.transform.parent = spikygate.transform;
                particlesystem.transform.position = z == 0 ? spikygate.transform.position + new Vector3(3.2f, 0, 0) : spikygate.Find("shade_gate_0001_shadegate_front").transform.position;
                particlesystem.transform.SetScaleY(spikygate.Find("solid").transform.GetScaleX() * .8f); 
                if (spikygate.transform.rotation.eulerAngles.z != 0)
                { particlesystem.transform.rotation = Quaternion.Euler(0, 0, 270); }
                else { particlesystem.transform.rotation = Quaternion.Euler(0, 0, 0); }
                
                particlesystem.SetActive(true);
            }
        }
        private static void SetupBreakables(Scene scene)
        {
            /* var breakables = scene.GetObjectsOfType<BreakableObject>();
             foreach (var item in breakables)
             {
                 Log("Found: " + item.name);
                 var go = item.gameObject;
                 var name = item.name.Split('_');
                 if (name[0] == "ruin")
                 {
                     var newobj = GameObject.Instantiate(FiveKnights.preloadedGO["BreakableFossil"]);
                     newobj.transform.position = go.transform.position;
                     newobj.transform.localScale = go.transform.localScale;
                     newobj.transform.rotation = go.transform.rotation;                    
                     newobj.SetActive(true);
                     GameObject.Destroy(go);
                 }
                 if (name[0] == "tutorial")
                 {
                     var newobj = GameObject.Instantiate(FiveKnights.preloadedGO["BreakableClawPole"]);
                     newobj.transform.position = go.transform.position;
                     newobj.transform.localScale = go.transform.localScale;
                     newobj.transform.rotation = go.transform.rotation;
                     newobj.SetActive(true);
                     GameObject.Destroy(go);
                 }
             }
             var vines = scene.GetObjectsOfType<GrassBehaviour>();
             foreach (var item in vines)
             {
                 Log("Found: " + item.name);
                 var go = item.gameObject;
                 var newobj = GameObject.Instantiate(FiveKnights.preloadedGO["BreakableVines"]);
                 newobj.transform.position = go.transform.position;
                 newobj.transform.localScale = go.transform.localScale;
                 newobj.transform.rotation = go.transform.rotation;
                 newobj.SetActive(true);
                 GameObject.Destroy(go);
             }*/

            var breakables = scene.Find("Breakable");
            foreach (Transform t in breakables.transform)
            {
                if (t.GetComponent<BreakableObject>() != null)
                {
                    Log("Found: " + t.name);
                    var go = t.gameObject;
                    var name = t.name.Split('_');
                    if (name[0] == "ruin")
                    {
                        var newobj = GameObject.Instantiate(FiveKnights.preloadedGO["BreakableFossil"]);
                        newobj.transform.position = go.transform.position;
                        newobj.transform.localScale = go.transform.localScale;
                        newobj.transform.rotation = go.transform.rotation;
                        newobj.SetActive(true);
                        GameObject.Destroy(go);
                    }
                    if (name[0] == "tutorial")
                    {
                        var newobj = GameObject.Instantiate(FiveKnights.preloadedGO["BreakableClawPole"]);
                        newobj.transform.position = go.transform.position;
                        newobj.transform.localScale = go.transform.localScale;
                        newobj.transform.rotation = go.transform.rotation;
                        newobj.SetActive(true);
                        GameObject.Destroy(go);
                    }
                }
                if (t.GetComponent<GrassBehaviour>() != null)
                {
                    Log("Found: " + t.name);
                    var go = t.gameObject;
                    var newobj = GameObject.Instantiate(FiveKnights.preloadedGO["BreakableVines"]);
                    newobj.transform.position = go.transform.position;
                    newobj.transform.localScale = go.transform.localScale;
                    newobj.transform.rotation = go.transform.rotation;
                    newobj.SetActive(true);
                    GameObject.Destroy(go);
                }
            }
        }
        public static IEnumerable<TComponent> GetObjectsOfType<TComponent>(this Scene scene) where TComponent : Component
        {
            return scene.GetRootGameObjects()
                .Where(x => x.GetComponent<TComponent>() != null)
                .Select(x => x.GetComponent<TComponent>());
        }
        private static void FixHazardSpikes(Scene scene)
        {
            var spikes = scene.Find("Spikes");
            foreach (Transform t in spikes.transform)
            {
                t.gameObject.layer = (int)PhysLayers.HERO_ATTACK;
                t.gameObject.AddComponent<DamageHero>().damageDealt = 1;
                t.gameObject.GetComponent<DamageHero>().hazardType = 2;
                t.gameObject.AddComponent<TinkEffect>().blockEffect = ABManager.AssetBundles[ABManager.Bundle.OWArenaDep].LoadAsset<GameObject>("Block Hit V2");
                t.gameObject.AddComponent<Tink>();
            }
        }
        private static void SetupEntranceTerrain(Scene scene)
        {
            //Setup terrain for the transition 
            var roofcollider = scene.Find("Roof Collider top right").GetComponent<PolygonCollider2D>();
            Vector2[] pointarray = new Vector2[50];
            pointarray[21] = new Vector2(35.2028f, -18.6f);
            pointarray[22] = new Vector2(39.363f, -18.6f);
            int index = -1;
            foreach (Vector2 point in roofcollider.points)
            {
                if (index == 20)
                { index = 23; }
                else { index++; }
                if (index == 23) { pointarray[index] = new Vector2(39.363f, -21.2158f); }
                else { pointarray[index] = point; }
            }
            roofcollider.CreatePrimitive(50);
            roofcollider.points = pointarray;
            roofcollider.SetPath(0, pointarray);

            var ceiling = scene.Find("TileMap Render Data").Find("Scenemap").Find("Chunk 1 6");
            var ceilingcolliders = ceiling.GetComponents<EdgeCollider2D>();
            GameObject.Destroy(ceilingcolliders[0]);

            //Add breakable ceiling
            var breakable = GameObject.Instantiate(FiveKnights.preloadedGO["BreakableCeiling"]);
            breakable.transform.position = new Vector3(209.5f, 59, 0);
            breakable.transform.localScale = new Vector3(1, 1, 1);
            // REMOVE BEFORE RELEASE
            breakable.GetComponent<PersistentBoolItem>().semiPersistent = true;
            // REMOVE BEFORE RELEASE
            GameObject.Destroy(breakable.Find("Particle System"));
            breakable.SetActive(true); 

            // Add indicator particles
            scene.Find("frame collider (1)").layer = (int)PhysLayers.TERRAIN;
            var iholder = GameObject.Instantiate(ABManager.AssetBundles[ABManager.Bundle.Misc].LoadAsset<GameObject>("iholder"));
            var iherodetector = iholder.Find("iherodetector");
            var particlerocks = GameObject.Instantiate(breakable.Find("Particle_rocks_small"), iholder.transform);
            var irock1 = iholder.Find("irock");
            var irock2 = iholder.Find("irock (1)");
            var idust = GameObject.Instantiate(breakable.LocateMyFSM("breakable_wall_v2").GetAction<SpawnObjectFromGlobalPool>("Hit Up").gameObject.Value, iholder.transform);
            iholder.SetActive(false);
            iholder.transform.position = new Vector2(209.5f, 58.5f);
            particlerocks.transform.position = iholder.transform.position;
            iherodetector.transform.position = new Vector2(212, 53);
            idust.transform.position = iholder.transform.position;
            idust.transform.rotation = Quaternion.Euler(90, 180, 0);
            idust.GetComponent<ParticleSystem>().gravityModifier = 10;
            idust.SetActive(false);
            iherodetector.AddComponent<IndicatorDetectorControl>().particlerocks = particlerocks;
            iherodetector.GetComponent<IndicatorDetectorControl>().dust = idust;
            irock1.AddComponent<RockControl>();
            irock2.AddComponent<RockControl>();
            particlerocks.SetActive(false);
            iholder.SetActive(true);
            if (FiveKnights.Instance.SaveSettings.IndicatorActivated)
            {
                iherodetector.SetActive(false);
                irock1.transform.localPosition = FiveKnights.Instance.SaveSettings.IndicatorPosition1;
                irock2.transform.localPosition = FiveKnights.Instance.SaveSettings.IndicatorPosition2;
                irock1.SetActive(true);
                irock2.SetActive(true);
            }


        }
        private class IndicatorDetectorControl : MonoBehaviour
        {
            public GameObject particlerocks;
            public GameObject dust;
            void OnTriggerEnter2D(Collider2D collision)
            {
                if (collision.gameObject.layer == (int)PhysLayers.PLAYER)
                {
                    Log("HERO ENTERED");
                    gameObject.transform.parent.Find("irock").gameObject.SetActive(true);
                    gameObject.transform.parent.Find("irock (1)").gameObject.SetActive(true);
                    dust.SetActive(true);
                    particlerocks.SetActive(true);
                    particlerocks.GetComponent<ParticleSystem>().Emit(15);

                    FiveKnights.Instance.SaveSettings.IndicatorActivated = true;
                    gameObject.SetActive(false);
                }
            }
        }
        private class RockControl : MonoBehaviour
        {
            void OnCollisionEnter2D(Collision2D collision)
            {
                if (collision.gameObject.layer == (int)PhysLayers.TERRAIN)
                {
                    Log("Collided with " + collision.gameObject.name);
                    if (gameObject.name == "irock (1)")
                    { FiveKnights.Instance.SaveSettings.IndicatorPosition2 = gameObject.transform.localPosition; Log("Rock 2 position = " + FiveKnights.Instance.SaveSettings.IndicatorPosition2); }
                    else
                    { FiveKnights.Instance.SaveSettings.IndicatorPosition1 = gameObject.transform.localPosition; Log("Rock 1 position = " + FiveKnights.Instance.SaveSettings.IndicatorPosition1); }
                }
            }
        }

        private static void SetupExitTerrain(Scene scene)
        {
            //Setup terrain for the transition
            var colliderR = scene.Find("Roof Collider (1)").GetComponent<PolygonCollider2D>();
            Vector2[] pointarrayR = new Vector2[37];
            int indexR = 0;

            foreach (var point in colliderR.points)
            {
                if (indexR == 21 || indexR == 22 || indexR == 23 || indexR == 24)
                {
                    if (indexR == 21) { pointarrayR[21] = new Vector2(36.3528f, -5.6227f); }
                    if (indexR == 22) { pointarrayR[22] = new Vector2(36.3528f, -.4f); }
                    if (indexR == 23) { pointarrayR[23] = new Vector2(40.3528f, -.4f); }
                    if (indexR == 24) { pointarrayR[24] = new Vector2(40.3528f, -5.2621f); }
                    indexR++;
                }
                else { pointarrayR[indexR] = point; indexR++; }         
            }
            colliderR.points = pointarrayR;
            colliderR.SetPath(0, pointarrayR);

            var ceiling = scene.Find("TileMap Render Data").Find("Scenemap").Find("Chunk 0 3");
            Vector2[] pointarrayC = new Vector2[26];
            int indexC = 0;
            foreach (var point in ceiling.GetComponent<EdgeCollider2D>().points)
            {
                if (indexC == 4 || indexC == 5)
                {
                    if (indexC == 4) { pointarrayC[4] = new Vector2(8.29f, 32); }
                    if (indexC == 5) { pointarrayC[5] = new Vector2(8.29f, 28); }
                    indexC++;
                }
                else {  pointarrayC[indexC] = point; indexC++; }
            }
            ceiling.GetComponent<EdgeCollider2D>().points = pointarrayC;

            // Add collapsing floor
            var collapser = GameObject.Instantiate(FiveKnights.preloadedGO["CollapseFloor"]);
            collapser.transform.position = new Vector3(99.58f, 28.5f);
            // REMOVE BEFORE RELEASE
            collapser.GetComponent<PersistentBoolItem>().semiPersistent = true;
            // REMOVE BEFORE RELEASE
            GameObject.Destroy(collapser.Find("floor1"));
            GameObject.Destroy(collapser.Find("floor2"));
            collapser.gameObject.SetActive(true);
        }

        private static void CreateGateway(string gateName, Vector2 pos, Vector2 size, string toScene, string entryGate,
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
        private static void GameManagerEnterHero(On.GameManager.orig_EnterHero orig, GameManager self, bool additiveGateSearch)
        {
            if (self.sceneName == "Parkour") 
            {
                self.tilemap.width = 500;
                self.tilemap.height = 200;

            }
            orig(self, false);
        }

        private static void Log(object o) => Modding.Logger.Log("[BloomParkour] " + o);
    }

}
