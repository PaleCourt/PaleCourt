using System;
using System.Collections.Generic;
using Modding;
using FrogCore;

namespace FiveKnights
{
    [Serializable]
    public class SaveModSettings : ModSettings
    {
        public BossStatue.Completion CompletionIsma = new BossStatue.Completion
        {
            isUnlocked = true,
            hasBeenSeen = true
        };
        public BossStatue.Completion CompletionIsma2 = new BossStatue.Completion()
        {
            isUnlocked = true,
            hasBeenSeen = true
        };
        public BossStatue.Completion CompletionZemer = new BossStatue.Completion
        {
            isUnlocked = true,
            hasBeenSeen = true
        };
        public BossStatue.Completion CompletionZemer2 = new BossStatue.Completion
        {
            isUnlocked = true,
            hasBeenSeen = true
        };
        public BossStatue.Completion CompletionDryya = new BossStatue.Completion
        {
            isUnlocked = true,
            hasBeenSeen = true
        };
        public BossStatue.Completion CompletionHegemol = new BossStatue.Completion
        {
            isUnlocked = true,
            hasBeenSeen = true
        };

        public JournalHelper.JournalPlayerData IsmaEntryData = new JournalHelper.JournalPlayerData
        {
            haskilled = false,
            Hidden = true,
            killsremaining = 2,
            newentry = true
        };
        public JournalHelper.JournalPlayerData ZemerEntryData = new JournalHelper.JournalPlayerData
        {
            haskilled = false,
            Hidden = true,
            killsremaining = 2,
            newentry = true
        };
        public JournalHelper.JournalPlayerData DryyaEntryData = new JournalHelper.JournalPlayerData
        {
            haskilled = false,
            Hidden = true,
            killsremaining = 2,
            newentry = true
        };
        public JournalHelper.JournalPlayerData HegemolEntryData = new JournalHelper.JournalPlayerData
        {
            haskilled = false,
            Hidden = true,
            killsremaining = 2,
            newentry = true
        };

        public bool AltStatueIsma;

        public bool AltStatueZemer;


        //public bool newCharm_41 = true;
        //public bool newCharm_42 = true;
        //public bool newCharm_43 = true;
        //public bool newCharm_44 = true;

        //public bool gotCharm_41 = false;
        //public bool gotCharm_42 = false;
        //public bool gotCharm_43 = false;
        //public bool gotCharm_44 = false;

        //public bool equippedCharm_41 = false;
        //public bool equippedCharm_42 = false;
        //public bool equippedCharm_43 = false;
        //public bool equippedCharm_44 = false;

        public List<bool> newCharms = new List<bool>() { true, true, true, true };

        public List<bool> gotCharms = new List<bool>() { true, true, true, true };

        public List<bool> equippedCharms = new List<bool>() { false, false, false, false };

        public bool upgradedCharm_10 = true;

        public List<int> charmCosts = new List<int>()
        {
            Charms.charmCost_41,
            Charms.charmCost_42,
            Charms.charmCost_43,
            Charms.charmCost_44,
        };
    }
}