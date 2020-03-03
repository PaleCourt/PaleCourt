using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;

namespace FiveKnights
{
    public class CustomWP : MonoBehaviour
    {
        private bool correctedTP;

        private void Start()
        {
            On.GameManager.EnterHero += GameManager_EnterHero;
            On.GameManager.BeginSceneTransition += GameManager_BeginSceneTransition;
        }

        private void GameManager_BeginSceneTransition(On.GameManager.orig_BeginSceneTransition orig, GameManager self, GameManager.SceneLoadInfo info)
        {
            orig(self, info);
            if (info.SceneName != "Dream_04_White_Defender" || correctedTP)
            {
                correctedTP = false;
                orig(self, info);
                return;
            }
            correctedTP = true; 
            ArenaFinder.defeats = PlayerData.instance.whiteDefenderDefeats;
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

        private void GameManager_EnterHero(On.GameManager.orig_EnterHero orig, GameManager self, bool additiveGateSearch)
        {
            if (self.sceneName == "White_Palace_09")
            {
                CreateGateway("left test2", new Vector2(14f, 94.4f), new Vector2(1f, 4f),
                              "GG_Workshop", "left test", false, true, true, GameManager.SceneLoadVisualizations.Default);
                GameObject black = new GameObject("black_mask");
                SpriteRenderer sr = black.AddComponent<SpriteRenderer>();
                sr.sprite = FiveKnights.SPRITES[1];
                sr.material = new Material(Shader.Find("Sprites/Diffuse"));
                sr.material.renderQueue = 4000;
                black.transform.position = new Vector3(18.7f, 94.4f, -1000f);
                black.transform.localScale *= 100f;
                orig(self, false);
                StartCoroutine(HubSet(black));
                return;
            }
            orig(self, false);
        }

        IEnumerator HubSet(GameObject black)
        {
            SpriteRenderer sr = black.GetComponent<SpriteRenderer>();
            yield return new WaitWhile(() => !HeroController.instance);
            HeroController.instance.transform.position = new Vector2(12.5f, 94.5f);
            foreach (var i in FindObjectsOfType<SpriteRenderer>().Where(x => x != null && x.name.Contains("SceneBorder"))) Destroy(i);
            foreach (var i in FindObjectsOfType<SpriteRenderer>().Where(x => x.enabled && !x.name.Contains("white") && !x.name.Contains("wp") && !x.name.Contains("black")))
            {
                bool skip = false;
                foreach (SpriteRenderer j in FiveKnights.preloadedGO["isma_stat"].GetComponentsInChildren<SpriteRenderer>())
                {
                    if (j.name == i.name)
                    {
                        skip = true;
                        break;
                    }
                }
                if (skip || FiveKnights.preloadedGO["isma_stat"].name == i.name) continue;
                i.enabled = false;
            }
            foreach (var i in FindObjectsOfType<GameObject>().Where(x => x.name.Contains("abyss") || x.name.Contains("Abyss"))) Destroy(i);
            GameObject lift = Instantiate(FiveKnights.preloadedGO["lift"]);
            lift.transform.position = new Vector2(14f, 91.8f);
            lift.SetActive(true);
            lift.transform.localScale *= 1.15f;
            Vector2 sc = lift.transform.localScale;
            lift.transform.localScale = new Vector2(sc.x * 1.15f, sc.y);
            lift.LocateMyFSM("Control").enabled = false;
            yield return new WaitForSeconds(1.9f);
            for (float i = 1f; i >= 0f; i -= 0.1f)
            {
                Color col = sr.color;
                sr.color = new Color(col.r, col.g, col.b, i);
                yield return new WaitForSeconds(0.05f);
            }
            Destroy(black);
            GameObject go = Instantiate(FiveKnights.preloadedGO["throne"]);
            go.SetActive(true);
            go.transform.position = new Vector3(60.5f, 97.7f, 0.2f);
            PlayMakerFSM fsm = go.LocateMyFSM("Sit");
            FiveKnights.preloadedGO["isma_stat"].transform.position = new Vector3(48.2f, 98.4f, HeroController.instance.transform.position.z);
            for (int i = 0; i < 3; i++)
            {
                GameObject s = Instantiate(FiveKnights.preloadedGO["isma_stat"]);
                float y = s.transform.position.y;
                s.transform.position = new Vector3(50.2f + i * 5f, y, HeroController.instance.transform.GetPositionZ());
            }
            yield return new WaitWhile(() => fsm.ActiveStateName != "Resting");
            fsm.enabled = false;
            PlayDialogue(fsm);
        }

        private void PlayDialogue(PlayMakerFSM fsm)
        {
            IEnumerator LookForDialogClosed()
            {
                PlayMakerFSM textYN = GameObject.Find("Text YN").LocateMyFSM("Dialogue Page Control");
                while (textYN.ActiveStateName != "Ready for Input") yield return new WaitForEndOfFrame();
                while (textYN.ActiveStateName == "Ready for Input") yield return new WaitForEndOfFrame();
                GameObject.Find("DialogueManager").LocateMyFSM("Box Open YN").SendEvent("BOX DOWN YN");
                fsm.enabled = true;
                yield return new WaitForSeconds(0.5f);
                PlayMakerFSM pm = GameCameras.instance.tk2dCam.gameObject.LocateMyFSM("CameraFade");
                pm.SendEvent("FADE OUT");
                yield return new WaitForSeconds(0.5f);
                GameManager.instance.BeginSceneTransition(new GameManager.SceneLoadInfo
                {
                    SceneName = "Dream_04_White_Defender",
                    EntryGateName = "door1",
                    Visualization = GameManager.SceneLoadVisualizations.Dream,
                    WaitForSceneTransitionCameraFade = false,

                });
            }

            GameObject.Find("DialogueManager").LocateMyFSM("Box Open YN").SendEvent("BOX UP YN");
            GameObject.Find("Text YN").GetComponent<DialogueBox>().StartConversation("YN_THRONE", "YN_THRONE");
            GameObject.Find("Text YN").GetComponent<MonoBehaviour>().StartCoroutine(LookForDialogClosed());
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

        private void OnDestroy()
        {
            On.GameManager.EnterHero -= GameManager_EnterHero;
            On.GameManager.BeginSceneTransition -= GameManager_BeginSceneTransition;
        }
    }
}
