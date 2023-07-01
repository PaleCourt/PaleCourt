using System.Collections;
using FrogCore;
using HutongGames.PlayMaker.Actions;
using Modding;
using SFCore.Utils;
using TMPro;
using UnityEngine;
using Logger = UnityEngine.Logger;

namespace FiveKnights.Misc;

public static class WhiteLadyDialogue
{
    private static LanguageCtrl langCtrl;
    private const string QueenScene = "Room_Queen";
    private static PlayMakerFSM queenDialogue;
    private static DialogueBox dialogue;
    private const string Sheet = "Minor NPC";

    public static void Hook()
    {
        langCtrl = new LanguageCtrl();
        
        ModHooks.LanguageGetHook += LangGet;
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += ActiveSceneChanged;
    }
    
    public static void Unhook()
    {
        ModHooks.LanguageGetHook -= LangGet;
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= ActiveSceneChanged;
    }   
    
    private static string LangGet(string key, string sheet, string orig)
    {
        if(key.StartsWith("WL_"))
        {
            sheet = "Minor NPC";
        }
        return langCtrl.ContainsKey(key, sheet) ? langCtrl.Get(key, sheet) : orig;
    }

    private static void ActiveSceneChanged(UnityEngine.SceneManagement.Scene prev,
        UnityEngine.SceneManagement.Scene curr)
    {
        if (curr.name != QueenScene) return;

        var queen = GameObject.Find("Queen");
        queenDialogue = queen.LocateMyFSM("Conversation Control");

        var go = queenDialogue.GetAction<CallMethodProper>("Repeat", 1).gameObject.GameObject.Value;
        dialogue = go.GetComponent<DialogueBox>();


        string[] stateToChange = { "Repeat", "Kingsoul Repeat", "Shadecharm Repeat" };
        
        var newState = queenDialogue.CreateState("PCDialogue");
        newState.InsertMethod(CheckAndDoCharmDialogue, 0);
        queenDialogue.ChangeTransition("Convo Choice", "FINISHED", "PCDialogue");
        queenDialogue.ChangeTransition("Convo Choice", "KINGSOUL", "PCDialogue");
        
        queenDialogue.ChangeTransition("Convo Choice", "SHADECHARM", "PCDialogue");
        newState.AddTransition("CONVO_FINISH", "Flower?");
        
        foreach (var state in stateToChange)
        {
            newState.AddTransition("ELSE", state);
        }
    }

    private static void CheckAndDoCharmDialogue()
    {
        bool didDialogue = false;
        
        // Has king's honour
        if (FiveKnights.Instance.SaveSettings.upgradedCharm_10 && PlayerData.instance.equippedCharm_10)
        {
            if (!FiveKnights.Instance.SaveSettings.WLadyKHConvo1)
            {
                didDialogue = FiveKnights.Instance.SaveSettings.WLadyKHConvo1 = true;
                dialogue.StartConversation("WL_KH_1", Sheet);

            }
            else if (!FiveKnights.Instance.SaveSettings.WLadyKHConvo2)
            {
                didDialogue = FiveKnights.Instance.SaveSettings.WLadyKHConvo2 = true;
                dialogue.StartConversation("WL_KH_2", Sheet);
            }
        }
        // Has Mark of Purity
        else if (FiveKnights.Instance.SaveSettings.equippedCharms[0])
        {
            if (!FiveKnights.Instance.SaveSettings.WLadyMPConvo1)
            {
                didDialogue = FiveKnights.Instance.SaveSettings.WLadyMPConvo1 = true;
                dialogue.StartConversation("WL_MP_1", Sheet);
            }
            else if (!FiveKnights.Instance.SaveSettings.WLadyMPConvo2)
            {
                didDialogue = FiveKnights.Instance.SaveSettings.WLadyMPConvo2 = true;
                dialogue.StartConversation("WL_MP_2", Sheet);
            }
        }
        // Has Boon of Hallownest
        else if (FiveKnights.Instance.SaveSettings.equippedCharms[2])
        {
            if (!FiveKnights.Instance.SaveSettings.WLadyBHConvo1)
            {
                didDialogue = FiveKnights.Instance.SaveSettings.WLadyBHConvo1 = true;
                dialogue.StartConversation("WL_BH_1", Sheet);
            }
            else if (!FiveKnights.Instance.SaveSettings.WLadyBHConvo2)
            {
                didDialogue = FiveKnights.Instance.SaveSettings.WLadyBHConvo2 = true;
                dialogue.StartConversation("WL_BH_2", Sheet);
            }
        }
        // Has Vessel's Lament
        else if (FiveKnights.Instance.SaveSettings.equippedCharms[1])
        {
            if (!FiveKnights.Instance.SaveSettings.WLadyVLConvo1)
            {
                didDialogue = FiveKnights.Instance.SaveSettings.WLadyVLConvo1 = true;
                dialogue.StartConversation("WL_VL_1", Sheet);
            }
            else if (!FiveKnights.Instance.SaveSettings.WLadyVLConvo2)
            {
                didDialogue = FiveKnights.Instance.SaveSettings.WLadyVLConvo2 = true;
                dialogue.StartConversation("WL_VL_2", Sheet);
            }
        }
        // Has ALL
        else if (FiveKnights.Instance.SaveSettings.equippedCharms[0] &&
                 FiveKnights.Instance.SaveSettings.equippedCharms[1] &&
                 FiveKnights.Instance.SaveSettings.equippedCharms[2] &&
                 FiveKnights.Instance.SaveSettings.upgradedCharm_10 && PlayerData.instance.equippedCharm_10)
        {
            if (!FiveKnights.Instance.SaveSettings.WLadyAllConvo1)
            {
                didDialogue = FiveKnights.Instance.SaveSettings.WLadyAllConvo1 = true;
                dialogue.StartConversation("WL_ALL_1", Sheet);
            }
            else if (!FiveKnights.Instance.SaveSettings.WLadyAllConvo2)
            {
                didDialogue = FiveKnights.Instance.SaveSettings.WLadyAllConvo2 = true;
                dialogue.StartConversation("WL_ALL_2", Sheet);
            }
        }

        if (!didDialogue)
        {
            queenDialogue.SendEvent("ELSE");
        }
    }

    private static void Log(object o)
    {
        Modding.Logger.Log($"[White Lady]: {o}");
    }
}