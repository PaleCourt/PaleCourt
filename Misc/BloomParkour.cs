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

namespace FiveKnights
{
    public static class BloomParkour
    {
        private static List<string> spikygates = new List<string>()
        {
           "spiky_gate",
           "spiky_gate_1"
        };
        public static void Hook()
        {
            On.GameManager.GetCurrentMapZone += GameManagerGetCurrentMapZone;
            On.GameManager.EnterHero += GameManagerEnterHero;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += ActiveSceneChanged;
        }
        public static void Unhook()
        {
            On.GameManager.GetCurrentMapZone -= GameManagerGetCurrentMapZone;
            On.GameManager.EnterHero -= GameManagerEnterHero;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= ActiveSceneChanged;
        }

        private static void ActiveSceneChanged(UnityEngine.SceneManagement.Scene From, UnityEngine.SceneManagement.Scene To)
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
                SetupShadegates(To);
            }
        }


        private static void SetupShadegates(UnityEngine.SceneManagement.Scene scene)
        {
            var shadegate = GameObject.Instantiate(FiveKnights.preloadedGO["ShadeGate"]);
            foreach (string name in spikygates)
            {
                var spikygate = scene.Find(name);
                spikygate.GetComponent<AudioSource>().outputAudioMixerGroup = HeroController.instance.GetComponent<AudioSource>().outputAudioMixerGroup;

                var slasheffect = GameObject.Instantiate(shadegate.Find("Slash Effect"));
                slasheffect.name = "Slash Effect";
                slasheffect.transform.parent = spikygate.transform;
                slasheffect.transform.position = spikygate.transform.position + new Vector3(4,0,0);
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
                Log("Just checking");
                slashfsm.GetState("R").GetAction<SendMessage>(3).functionCall.FunctionName = "RecoilDown";
                Log("Has it been this the whole time somehow?");
                slashfsm.GetState("Pause").GetAction<SetPosition>(3).y = slashfsm.FindFloatVariable("Self X");
                slashfsm.GetState("Pause").GetAction<SetPosition>(3).x = slashfsm.FindFloatVariable("Hero Y");
                slasheffect.SetActive(true);

                var dasheffect = GameObject.Instantiate(shadegate.Find("Dash Effect"));
                dasheffect.name = "Dash Effect";
                dasheffect.transform.parent = spikygate.transform;
                dasheffect.transform.position = spikygate.transform.position + new Vector3(4, 0, 0);
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

                dasheffect.SetActive(true);

                var particlesystem = GameObject.Instantiate(shadegate.Find("Particle System"));
                particlesystem.name = "Particle System";
                particlesystem.transform.parent = spikygate.transform;
                particlesystem.transform.position = spikygate.transform.position;
                particlesystem.transform.rotation = Quaternion.Euler(0, 0, 0);
                particlesystem.SetActive(true);
            }
        }
        private static void SetupEntranceTerrain(UnityEngine.SceneManagement.Scene scene)
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


        }
        private static void SetupExitTerrain(UnityEngine.SceneManagement.Scene scene)
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

        private static string GameManagerGetCurrentMapZone(On.GameManager.orig_GetCurrentMapZone orig, GameManager self)
        {
            if (self.sceneName == "Parkour") return MapZone.ABYSS_DEEP.ToString();
            return orig(self);
        }

        private static void Log(object o) => Modding.Logger.Log("[AutoSwing] " + o);
    }

}
