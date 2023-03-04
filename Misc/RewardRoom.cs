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
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        }

		public static void UnHook()
        {
            ModHooks.LanguageGetHook -= LangGet;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;
        }

        private static string LangGet(string key, string sheet, string orig)
        {
            if(key.StartsWith("TITLE_") || key.Contains("_RR"))
            {
                sheet = "Reward Room";
            }
            return langCtrl.ContainsKey(key, sheet) ? langCtrl.Get(key, sheet) : orig;
        }

        private static void SceneManager_activeSceneChanged(UnityEngine.SceneManagement.Scene arg0, UnityEngine.SceneManagement.Scene arg1)
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

        private static IEnumerator tmpLoadRR()
        {
            yield return new WaitForSeconds(2f);
            UnityEngine.SceneManagement.SceneManager.LoadScene("hidden_reward_room");
            yield return null;
            yield return null;
            HeroController.instance.transform.position = new Vector3(302.23f, 129.38f, 0f);
        }

        private static DialogueOptions EntranceDialogue(DialogueCallbackOptions prev)
        {
            if (prev.Continue == false)
                return new() { Key = "ENTER_RR", Sheet = "Reward Room", Cost = 0, Type = DialogueType.YesNo, Continue = true };
            else
            {
                if (prev.Response == DialogueResponse.Yes)
                    GameManager.instance.StartCoroutine(tmpLoadRR());
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
