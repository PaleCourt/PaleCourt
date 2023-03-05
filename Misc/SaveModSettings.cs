using FrogCore;

namespace FiveKnights
{
    //[Serializable]
    public class SaveModSettings
    {
        public BossStatue.Completion CompletionIsma = BossStatue.Completion.None;
        public BossStatue.Completion CompletionIsma2 = BossStatue.Completion.None;
        public BossStatue.Completion CompletionZemer = BossStatue.Completion.None;
        public BossStatue.Completion CompletionZemer2 = BossStatue.Completion.None;
        public BossStatue.Completion CompletionDryya = BossStatue.Completion.None;
        public BossStatue.Completion CompletionHegemol = BossStatue.Completion.None;

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

        public bool[] newCharms = new bool[] { true, true, true, true };

        public bool[] gotCharms = new bool[] { false, false, false, true };

        public bool[] equippedCharms = new bool[] { false, false, false, false };

        public bool upgradedCharm_10 = false;

        public int[] charmCosts = new int[]
        {
            Charms.charmCost_41,
            Charms.charmCost_42,
            Charms.charmCost_43,
            Charms.charmCost_44,
        };
        
        public bool GreetedNailsmith = false;
        public bool GreetedSheo = false;

        public bool UnlockedChampionsCall => CompletionIsma.isUnlocked && CompletionDryya.isUnlocked &&
                                             CompletionHegemol.isUnlocked && CompletionZemer.isUnlocked;
        public bool SeenChampionsCall = false;
    }
}
