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
        public static void Hook()
        {
            ModHooks.LanguageGetHook += ModHooks_LanguageGetHook;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        }

        public static void UnHook()
        {
            ModHooks.LanguageGetHook -= ModHooks_LanguageGetHook;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;
        }

        private static string ModHooks_LanguageGetHook(string key, string sheetTitle, string orig)
        {
            switch (key)
            {
                case "ENTER_RR":
                    return "Enter Reward Room?";
                case "TITLE_ENTER_RR_SUPER":
                    return "The one and only";
                case "TITLE_ENTER_RR_MAIN":
                    return "REWARD ROOM NPC";
                case "TITLE_ENTER_RR_SUB":
                    return "placeholder";
                case "TITLE_RR_DRYYA_SUPER":
                case "TITLE_RR_DRYYA_MAIN":
                case "TITLE_RR_DRYYA_SUB":
                    return "dryya placeholder";
                case "TITLE_RR_ISMA_SUPER":
                case "TITLE_RR_ISMA_MAIN":
                case "TITLE_RR_ISMA_SUB":
                    return "isma placeholder";
                case "TITLE_RR_HEGEMOL_SUPER":
                case "TITLE_RR_HEGEMOL_MAIN":
                case "TITLE_RR_HEGEMOL_SUB":
                    return "hegemol placeholder";
                case "TITLE_RR_ZEMER_SUPER":
                case "TITLE_RR_ZEMER_MAIN":
                case "TITLE_RR_ZEMER_SUB":
                    return "zemer placeholder";
                case "RR_DRYYA_MEET":
                    return "dryya meet placeholder";
                case "RR_ISMA_MEET":
                    return "isma meet placeholder";
                case "RR_HEGEMOL_MEET":
                    return "hegemol meet placeholder";
                case "RR_ZEMER_MEET":
                    return "zemer meet placeholder";
            }
            return orig;
        }

        private static void SceneManager_activeSceneChanged(UnityEngine.SceneManagement.Scene arg0, UnityEngine.SceneManagement.Scene arg1)
        {
            if (arg1.name == "White_Palace_09")
            {
                DialogueNPC entrance = DialogueNPC.CreateInstance();
                entrance.transform.position = new Vector3(60.5f, 98.4f, 0f);
                entrance.DialogueSelector = EntranceDialogue;
                entrance.SetTitle("TITLE_ENTER_RR");
                entrance.SetDreamKey("TITLE_ENTER_RR_SUB");
                entrance.SetUp();
            }
            if (arg1.name == "hidden_reward_room")
            {
                DialogueNPC dryya = DialogueNPC.CreateInstance();
                dryya.transform.position = new Vector3(295.02f, 129.67f, 0f);
                dryya.DialogueSelector = DryyaDialogue;
                dryya.SetTitle("TITLE_RR_DRYYA");
                dryya.SetDreamKey("TITLE_RR_DRYYA_SUB");
                dryya.SetUp();

                DialogueNPC isma = DialogueNPC.CreateInstance();
                isma.transform.position = new Vector3(306.6366f, 129.0865f, 0f);
                isma.DialogueSelector = IsmaDialogue;
                isma.SetTitle("TITLE_RR_ISMA");
                isma.SetDreamKey("TITLE_RR_ISMA_SUB");
                isma.SetUp();

                DialogueNPC hegemol = DialogueNPC.CreateInstance();
                hegemol.transform.position = new Vector3(302.23f, 129.38f, 0f);
                hegemol.DialogueSelector = HegemolDialogue;
                hegemol.SetTitle("TITLE_RR_HEGEMOL");
                hegemol.SetDreamKey("TITLE_RR_HEGEMOL_SUB");
                hegemol.SetUp();

                DialogueNPC zemer = DialogueNPC.CreateInstance();
                zemer.transform.position = new Vector3(309.7428f, 129.0576f, 0f);
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
                return new() { Key = "ENTER_RR", Sheet = "", Cost = 0, Type = DialogueType.YesNo, Continue = true };
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
                return new() { Key = "RR_DRYYA_MEET", Sheet = "", Type = DialogueType.Normal, Continue = true };
            return new() { Continue = false };
        }

        private static DialogueOptions IsmaDialogue(DialogueCallbackOptions prev)
        {
            if (prev.Continue == false)
                return new() { Key = "RR_ISMA_MEET", Sheet = "", Type = DialogueType.Normal, Continue = true };
            return new() { Continue = false };
        }

        private static DialogueOptions HegemolDialogue(DialogueCallbackOptions prev)
        {
            if (prev.Continue == false)
                return new() { Key = "RR_HEGEMOL_MEET", Sheet = "", Type = DialogueType.Normal, Continue = true };
            return new() { Continue = false };
        }

        private static DialogueOptions ZemerDialogue(DialogueCallbackOptions prev)
        {
            if (prev.Continue == false)
                return new() { Key = "RR_ZEMER_MEET", Sheet = "", Type = DialogueType.Normal, Continue = true };
            return new() { Continue = false };
        }
    }
}
