using FiveKnights.Dryya;
using FiveKnights.Hegemol;
using FiveKnights.Isma;
using FiveKnights.Tiso;
using FiveKnights.Zemer;
using ItemChanger;
using ItemChanger.Locations;
using ItemChanger.Tags;
using ItemChanger.Util;
using RandomizerMod.RandomizerData;
using TheRealJournalRando.IC;

namespace FiveKnights.Rando;

public class JournalLocation : AutoLocation
{
    public string bossName { get; set; }
    public EnemyJournalLocationType type;
    public JournalLocation(string _name, string _bossName, EnemyJournalLocationType _type)
    {
        name = _name;
        type = _type;
        sceneName = "GG_Workshop";
        bossName = _bossName;
        tags = [LocationTag()];
    }
    private InteropTag LocationTag()
    {
        InteropTag tag = new();
        tag.Properties["ModSource"] = FiveKnights.Instance.GetName();
        tag.Properties["PoolGroup"] = PoolNames.Journal;
        tag.Properties["VanillaItem"] = name;
        tag.Message = "RandoSupplementalMetadata";
        return tag;
    }

    protected override void OnLoad()
    {
        DryyaSetup.GrantReward += GrantFight;
        DryyaSetup.IsJournalRando += SetAsTrue;
        HegemolController.GrantReward += GrantFight;
        HegemolController.IsJournalRando += SetAsTrue;
        IsmaController.GrantReward += GrantFight;
        IsmaController.IsJournalRando += SetAsTrue;
        TisoAttacks.GrantReward += GrantFight;
        TisoAttacks.IsJournalRando += SetAsTrue;
        ZemerControllerP2.GrantReward += GrantFight;
        ZemerControllerP2.IsJournalRando += SetAsTrue;
    }

    protected override void OnUnload()
    {
        DryyaSetup.GrantReward -= GrantFight;
        DryyaSetup.IsJournalRando -= SetAsTrue;
        HegemolController.GrantReward -= GrantFight;
        HegemolController.IsJournalRando -= SetAsTrue;
        IsmaController.GrantReward -= GrantFight;
        IsmaController.IsJournalRando -= SetAsTrue;
        TisoAttacks.GrantReward -= GrantFight;
        TisoAttacks.IsJournalRando -= SetAsTrue;
        ZemerControllerP2.GrantReward -= GrantFight;
        ZemerControllerP2.IsJournalRando -= SetAsTrue;
    }

    private void GrantFight(string boss)
    {
        if (bossName == boss)
        {
            ItemUtility.GiveSequentially(Placement.Items, Placement, new GiveInfo()
            {
                FlingType = FlingType.Everywhere,
                MessageType = MessageType.Any,
            });

            Placement.AddVisitFlag(VisitState.Opened);
        }
    }

    private bool SetAsTrue()
    {
        return true;
    }
}