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
using Object = UnityEngine.Object;


namespace FiveKnights
{
    public static class AbyssalTemple
    {
        private static string _currScene;
        
        public static void Hook()
        {
            //On.GameManager.GetCurrentMapZone += GameManagerGetCurrentMapZone;
            On.GameManager.EnterHero += GameManagerEnterHero;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += ActiveSceneChanged;
            ModHooks.DrawBlackBordersHook += ModHooksOnDrawBlackBordersHook;
            On.TinkEffect.Start += StopScreenshake;
        }
        public static void Unhook()
        {
            //On.GameManager.GetCurrentMapZone -= GameManagerGetCurrentMapZone;
            On.GameManager.EnterHero -= GameManagerEnterHero;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= ActiveSceneChanged;
            ModHooks.DrawBlackBordersHook -= ModHooksOnDrawBlackBordersHook;
        }

        private static void ActiveSceneChanged(Scene From, Scene To)
        {
            _currScene = To.name;

            if (To.name == "Abyss_09")
            {
                SetupEntranceTerrain(To);
                CreateGateway("palecourt_top1", new Vector2(209.6533f, 60), new Vector2(4, 1), "Abyssal_Temple", "entry_bot1", true, false, false, GameManager.SceneLoadVisualizations.Default);
            }
            if (To.name == "Abyss_10")
            {
                SetupExitTerrain(To);
                CreateGateway("palecourt_top2", new Vector2(102.28f, 31.4f), new Vector2(4, 1), "Abyssal_Temple", "entry_bot2", true, true, false, GameManager.SceneLoadVisualizations.Default);
            }
            if (To.name == "Abyssal_Temple")
            {
                SetSceneSettings();
                SetupShadegates(To);
                SetupBreakables(To);
                FixHazardSpikes(To);
                SetWater();
                DreamgateLock();
                ShadeSpawn();
                SetupSoulTotems();
            }
        }

        private static void SetupSoulTotems()
        {
            Vector3[] worldPos =
            {
                new(76.1418f, 86.4f, .1f), new(198, 114.4f, .1f), new(227.2f, 107.4f, .1f)
            };
            foreach (Vector3 pos in worldPos)
            {
                var totem = GameObject.Instantiate(FiveKnights.preloadedGO["SoulTotem"]);
                var totembase = GameObject.Instantiate(FiveKnights.preloadedGO["TotemBase"]);
                totem.transform.position = pos;
                totembase.transform.position = pos + new Vector3(0.6f, -2, -.01f);
                var totemfsm = totem.LocateMyFSM("soul_totem");
                totemfsm.SetAttr("fsmTemplate", (FsmTemplate)null);
                totemfsm.GetAction<SetIntValue>("Reset").intValue = int.MaxValue;
                totemfsm.GetAction<SetIntValue>("Reset?").intValue = int.MaxValue;

                totem.SetActive(true);
                totembase.SetActive(true);
            }

        }

        private static void ShadeSpawn()
        {
            var shademarker = GameObject.Instantiate(FiveKnights.preloadedGO["ShadeSpawn"]);
            shademarker.transform.position = new Vector2(70.3f, 35.7f);
            shademarker.SetActive(true);
        }

        private static void SetWater()
        {
            var surface = FiveKnights.preloadedGO["AbyssWater1"];
            var wBox = FiveKnights.preloadedGO["AbyssWater2"];

            Vector3[] surfacePoses =
            {
                new (248.1f, 31.2f, -0.196f), new (229f, 31.2f, 0.004f), new (232f, 31.2f, 0.004f),
                new (234.5f, 31.2f, -0.396f), new (251f, 31.2f, 0.004f), new (251.9309f, 31.2f, 0.004f),
            };

            Vector3[] wBoxPoses =
            {
                new (252f, 30.5f, 0.004f), new (231.6483f, 30.5f, 0.004f)
            };

            foreach (var pos in surfacePoses)
            {
                var surf = Object.Instantiate(surface);
                surf.transform.position = pos;
                surf.SetActive(true);
            }
            
            foreach (var pos in wBoxPoses)
            {
                var w = Object.Instantiate(wBox);
                w.transform.position = pos;
                w.SetActive(true);
            }
        }
        private static void DreamgateLock()
        {
            var dreamlockright = GameObject.Instantiate(FiveKnights.preloadedGO["DreamgateLock"]);
            dreamlockright.transform.localScale = new Vector3(5.9911809816f, 17, 0);
            dreamlockright.transform.position = new Vector3(177.77f, 177.77f, 0);
            dreamlockright.SetActive(true);
            var dreamlockleft = GameObject.Instantiate(FiveKnights.preloadedGO["DreamgateLock"]);
            dreamlockleft.transform.localScale = new Vector3(1.5f, 7, 0);
            dreamlockleft.transform.position = new Vector3(54, 125.77f, 0);
            dreamlockleft.SetActive(true);
            

        }
        private static void SetSceneSettings()
        {
            if (GameObject.Find("_Effects").transform.Find("Darkness Region") != null)
            {
                var oldD = GameObject.Find("_Effects").transform.Find("Darkness Region");
                var newD = Object.Instantiate(FiveKnights.preloadedGO["DReg"]);
                
                var oldBC = oldD.GetComponent<BoxCollider2D>();
                var newBC = newD.GetComponent<BoxCollider2D>();

                newBC.size = oldBC.size;
                newBC.offset = oldBC.offset;

                newD.transform.position = oldD.transform.position;
                newD.transform.localScale = oldD.transform.localScale;
                
                newD.SetActive(true);

                Object.Destroy(oldD);
            }

            foreach (var i in Object.FindObjectsOfType<SceneManager>())
            {
                Object.Destroy(i.gameObject);
            }
            GameObject o = Object.Instantiate(FiveKnights.preloadedGO["AbyssSM"]);
            SceneManager sm = o.GetComponent<SceneManager>();
            
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
        private static void ModHooksOnDrawBlackBordersHook(List<GameObject> obj)
        {
            // Remove black border
            if (_currScene == "Abyssal_Temple")
            {
                foreach (var border in obj) border.SetActive(false);
            }
        }
        private static void SetupShadegates(Scene scene)
        {
            var shadegate = GameObject.Instantiate(FiveKnights.preloadedGO["ShadeGate"]);
            var spikygates = scene.Find("SpikyGates");
            foreach (Transform t in spikygates.transform)
            {
                var spikygate = t.gameObject;
                var z = spikygate.transform.rotation.eulerAngles.z;

                spikygate.GetComponent<AudioSource>().outputAudioMixerGroup = shadegate.GetComponent<AudioSource>().outputAudioMixerGroup;


                var slasheffect = GameObject.Instantiate(shadegate.Find("Slash Effect"));
                slasheffect.name = "Slash Effect";
                slasheffect.transform.parent = spikygate.transform;
                slasheffect.transform.position = z == 0 ? spikygate.transform.position + new Vector3(3.2f, 0, 0) : spikygate.Find("shade_gate_0001_shadegate_front").transform.position;
                slasheffect.transform.SetScaleY(spikygate.Find("solid").transform.GetScaleX() * .8f);
                var slashfsm = slasheffect.LocateMyFSM("Control");
                if (z == 0)
                {
                    slasheffect.transform.rotation = Quaternion.Euler(0, 0, 90);
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
                    effectS.RemoveAction<SendEventByName>();                   
                    slashfsm.GetState("L").GetAction<SetPosition>(2).y = slashfsm.FindFloatVariable("Self X");
                    slashfsm.GetState("L").GetAction<SetPosition>(2).x = slashfsm.FindFloatVariable("Hero Y");
                    slashfsm.GetState("R").GetAction<SetPosition>(2).y = slashfsm.FindFloatVariable("Self X");
                    slashfsm.GetState("R").GetAction<SetPosition>(2).x = slashfsm.FindFloatVariable("Hero Y");
                    slashfsm.GetState("L").RemoveFsmAction(3);
                    slashfsm.GetState("R").GetAction<SendMessage>(3).functionCall.FunctionName = nameof(HeroController.instance.RecoilDown);
                    slashfsm.GetState("Pause").GetAction<SetPosition>(3).y = slashfsm.FindFloatVariable("Self X");
                    slashfsm.GetState("Pause").GetAction<SetPosition>(3).x = slashfsm.FindFloatVariable("Hero Y");
                }
                if (spikygate.name == "spiky_gate_12_L")
                {
                    slashfsm.GetState("R").RemoveAction<SendMessage>();
                }
                if (spikygate.name == "spiky_gate_12_R")
                {
                    slashfsm.GetState("L").RemoveAction<SendMessage>();
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
                if (z != 0)
                { particlesystem.transform.SetScaleY(spikygate.Find("solid").transform.GetScaleX() * .8f); }
                else
                { particlesystem.transform.SetScaleX(spikygate.Find("solid").transform.GetScaleX() *.8f); }
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
                t.gameObject.AddComponent<NoShakeTink>();
            }
        }
        private static void StopScreenshake(On.TinkEffect.orig_Start orig, TinkEffect self)
        {
            if (GameManager.instance.GetSceneNameString() != "Abyssal_Temple")
            {
                orig(self);
            }
        }
        internal class NoShakeTink : MonoBehaviour
        {
            internal static AudioClip TinkClip = ABManager.AssetBundles[ABManager.Bundle.Sound].LoadAsset<AudioClip>("sword_hit_reject");

            private void OnTriggerEnter2D(Collider2D other)
            {
                if (other.gameObject.name != "Clash Tink" && !other.gameObject.CompareTag("Nail Attack")) return;
                this.PlayAudio(TinkClip, 1f, 0.15f);
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
            if (self.sceneName == "Abyssal_Temple") 
            {
                self.sceneWidth = self.tilemap.width = 1000;
                self.sceneHeight = self.tilemap.height = 1000;
            }
            orig(self, false);
        }

        private static void Log(object o) => Modding.Logger.Log("[AbyssalTemple] " + o);
    }

}
