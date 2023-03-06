using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrogCore;
using Modding;
using UnityEngine;

namespace FiveKnights
{
    public static class RewardRoom
    {
        private static LanguageCtrl langCtrl;

        public static void Hook()
        {
            langCtrl = new LanguageCtrl();

            ModHooks.LanguageGetHook += LangGet;
			On.GameManager.EnterHero += GameManagerEnterHero;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += ActiveSceneChanged;
        }

		public static void UnHook()
        {
            ModHooks.LanguageGetHook -= LangGet;
            On.GameManager.EnterHero -= GameManagerEnterHero;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= ActiveSceneChanged;
        }

        private static string LangGet(string key, string sheet, string orig)
        {
            if(key.StartsWith("TITLE_") || key.Contains("_RR"))
            {
                sheet = "Reward Room";
            }
            return langCtrl.ContainsKey(key, sheet) ? langCtrl.Get(key, sheet) : orig;
        }

        private static void GameManagerEnterHero(On.GameManager.orig_EnterHero orig, GameManager self, bool additiveGateSearch)
        {
            if(self.sceneName == "hidden_reward_room")
			{
                CreateGateway("door_reward_room", new Vector2(266f, 129.38f), Vector2.zero,
                    null, null, false, true, true,
                    GameManager.SceneLoadVisualizations.Dream);
                GameObject.Destroy(GameObject.Find("CameraLockArea"));
                CreateCameraLock("CameraLock", new Vector2(464f, 105f), new Vector2(1f, 1f),
                    new Vector2(107f, 20f), new Vector2(-162f, 29f),
                    new Vector2(248.5f, 124f), new Vector2(355.5f, 144f), true);
            }
            orig(self, false);
        }

        private static void ActiveSceneChanged(UnityEngine.SceneManagement.Scene arg0, UnityEngine.SceneManagement.Scene arg1)
        {
            if (arg1.name == "White_Palace_09")
            {
                DialogueNPC entrance = DialogueNPC.CreateInstance();
                entrance.transform.position = new Vector3(65f, 98.4f, 0f);
                entrance.DialogueSelector = EntranceDialogue;
                entrance.SetTitle("TITLE_ENTER_RR");
                entrance.SetDreamKey("TITLE_ENTER_RR_SUB");
                entrance.SetUp();
            }
            if (arg1.name == "hidden_reward_room")
            {
                DialogueNPC dryya = DialogueNPC.CreateInstance();
                dryya.transform.position = new Vector3(298.74f, 129.67f, 0f);
                dryya.DialogueSelector = DryyaDialogue;
                dryya.SetTitle("TITLE_RR_DRYYA");
                dryya.SetDreamKey("TITLE_RR_DRYYA_SUB");
                dryya.SetUp();

                DialogueNPC isma = DialogueNPC.CreateInstance();
                isma.transform.position = new Vector3(306.73f, 129.0865f, 0f);
                isma.DialogueSelector = IsmaDialogue;
                isma.SetTitle("TITLE_RR_ISMA");
                isma.SetDreamKey("TITLE_RR_ISMA_SUB");
                isma.SetUp();

                DialogueNPC ogrim = DialogueNPC.CreateInstance();
                ogrim.transform.position = new Vector3(302.69f, 129.0865f, 0f);
                ogrim.DialogueSelector = OgrimDialogue;
                ogrim.SetTitle("TITLE_RR_OGRIM");
                ogrim.SetDreamKey("TITLE_RR_OGRIM_SUB");
                ogrim.SetUp();

                DialogueNPC hegemol = DialogueNPC.CreateInstance();
                hegemol.transform.position = new Vector3(293.92f, 129.38f, 0f);
                hegemol.DialogueSelector = HegemolDialogue;
                hegemol.SetTitle("TITLE_RR_HEGEMOL");
                hegemol.SetDreamKey("TITLE_RR_HEGEMOL_SUB");
                hegemol.SetUp();

                DialogueNPC zemer = DialogueNPC.CreateInstance();
                zemer.transform.position = new Vector3(310.33f, 129.0576f, 0f);
                zemer.DialogueSelector = ZemerDialogue;
                zemer.SetTitle("TITLE_RR_ZEMER");
                zemer.SetDreamKey("TITLE_RR_ZEMER_SUB");
                zemer.SetUp();
            }
        }

        private static void CreateGateway(string gateName, Vector2 pos, Vector2 size, string toScene, string entryGate,
                                  bool right, bool left, bool onlyOut, GameManager.SceneLoadVisualizations vis)
        {
            GameObject gate = new GameObject(gateName);
            gate.transform.SetPosition2D(pos);
            var tp = gate.AddComponent<TransitionPoint>();
            if(!onlyOut)
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

        private static void CreateCameraLock(string n, Vector2 pos, Vector2 scl, Vector2 cSize, Vector2 cOff,
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
            parentlock.SetActive(true);
            lockCol.enabled = cla.enabled = true;
        }

        private static IEnumerator DebugLoadRR()
        {
            yield return new WaitForSeconds(1f);
            GameManager.instance.BeginSceneTransition(new GameManager.SceneLoadInfo
            {
                SceneName = "hidden_reward_room",
                EntryGateName = "door_reward_room",
                Visualization = GameManager.SceneLoadVisualizations.Default,
                WaitForSceneTransitionCameraFade = false,
                PreventCameraFadeOut = true,
                EntryDelay = 0,
                HeroLeaveDirection = GlobalEnums.GatePosition.door
            });
        }

        private static DialogueOptions EntranceDialogue(DialogueCallbackOptions prev)
        {
            if (prev.Continue == false)
                return new() { Key = "ENTER_RR", Sheet = "Reward Room", Cost = 0, Type = DialogueType.YesNo, Continue = true };
            else
            {
                if (prev.Response == DialogueResponse.Yes)
                    GameManager.instance.StartCoroutine(DebugLoadRR());
                return new() { Continue = false };
            }
        }

        private static DialogueOptions DryyaDialogue(DialogueCallbackOptions prev)
        {
            if (prev.Continue == false)
                return new() { Key = "RR_DRYYA_MEET", Sheet = "Reward Room", Type = DialogueType.Normal, Continue = true };
            return new() { Continue = false };
        }

        private static DialogueOptions IsmaDialogue(DialogueCallbackOptions prev)
        {
            if (prev.Continue == false)
                return new() { Key = "RR_ISMA_MEET", Sheet = "Reward Room", Type = DialogueType.Normal, Continue = true };
            return new() { Continue = false };
        }

        private static DialogueOptions OgrimDialogue(DialogueCallbackOptions prev)
        {
            if(prev.Continue == false)
                return new() { Key = "RR_OGRIM_MEET", Sheet = "Reward Room", Type = DialogueType.Normal, Continue = true };
            return new() { Continue = false };
        }

        private static DialogueOptions HegemolDialogue(DialogueCallbackOptions prev)
        {
            if (prev.Continue == false)
                return new() { Key = "RR_HEGEMOL_MEET", Sheet = "Reward Room", Type = DialogueType.Normal, Continue = true };
            return new() { Continue = false };
        }

        private static DialogueOptions ZemerDialogue(DialogueCallbackOptions prev)
        {
            if (prev.Continue == false)
                return new() { Key = "RR_ZEMER_MEET", Sheet = "Reward Room", Type = DialogueType.Normal, Continue = true };
            return new() { Continue = false };
        }
    }
}
