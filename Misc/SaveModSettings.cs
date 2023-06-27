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
        public BossStatue.Completion CompletionMawlek2 = BossStatue.Completion.None;

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

        public int IsmaOWWinCount = 0;
        public int DryyaOWWinCount = 0;
        public int HegOWWinCount = 0;
        public int ZemerOWWinCount = 0;

        public bool AltStatueIsma;

        public bool AltStatueZemer;

        public bool AltStatueMawlek;

        public bool[] newCharms = new bool[] { true, true, true, true };

        public bool[] gotCharms = new bool[] { false, false, false, false };

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

        public bool DryyaFirstConvo1 = false;
        public bool DryyaFirstConvo2 = false;
        public bool DryyaSecondConvo1 = false;
        public bool DryyaSecondConvo2 = false;
        public bool DryyaThirdConvo1 = false;
        public bool DryyaCharmConvo = false;
        public bool DryyaOldNailConvo = false;

        public bool IsmaFirstConvo1 = false;
        public bool IsmaFirstConvo2 = false;
        public bool IsmaSecondConvo1 = false;
        public bool IsmaSecondConvo2 = false;
        public bool IsmaThirdConvo1 = false;
        public bool IsmaCharmConvo = false;
        public bool IsmaOldNailConvo = false;

        public bool OgrimFirstConvo1 = false;
        public bool OgrimFirstConvo2 = false;
        public bool OgrimSecondConvo1 = false;
        public bool OgrimSecondConvo2 = false;
        public bool OgrimThirdConvo1 = false;
        public bool OgrimCharmConvo = false;
        public bool OgrimOldNailConvo = false;

        public bool HegemolFirstConvo1 = false;
        public bool HegemolFirstConvo2 = false;
        public bool HegemolSecondConvo1 = false;
        public bool HegemolSecondConvo2 = false;
        public bool HegemolThirdConvo1 = false;
        public bool HegemolCharmConvo = false;
        public bool HegemolOldNailConvo = false;

        public bool ZemerFirstConvo1 = false;
        public bool ZemerFirstConvo2 = false;
        public bool ZemerSecondConvo1 = false;
        public bool ZemerSecondConvo2 = false;
        public bool ZemerThirdConvo1 = false;
        public bool ZemerCharmConvo = false;
        public bool ZemerOldNailConvo = false;

        public bool UnlockedChampionsCall => CompletionIsma.isUnlocked && CompletionDryya.isUnlocked &&
                                             CompletionHegemol.isUnlocked && CompletionZemer.isUnlocked;
        public bool HasSeenWorkshopRaised = false;
        public bool SeenChampionsCall = false;
        public int ChampionsCallClears = 0;
        public bool HasSeenCredits = false;
        public bool IndicatorActivated = false;
        public UnityEngine.Vector3 IndicatorPosition1;
        public UnityEngine.Vector3 IndicatorPosition2;
    }
}
