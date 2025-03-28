using ItemChanger;
using ItemChanger.Tags;
using ItemChanger.UIDefs;
using TheRealJournalRando.IC;

namespace FiveKnights.Rando;

public class JournalItem : AbstractItem
{
    public string enemyName { get; set; }
    public EnemyJournalLocationType itemType { get; set; }
    public JournalItem(string enemy, EnemyJournalLocationType type)
    {
        name = type == EnemyJournalLocationType.Notes ? $"Hunter's_Notes-{enemy}" : $"Journal_Entry-{enemy}";
        itemType = type;
        enemyName = enemy;
        tags = [ItemTag()];
    }

    protected override void OnLoad()
    {
        Events.OnStringGet += AddNotchCostToCharmName;
    }

    protected override void OnUnload()
    {
        Events.OnStringGet -= AddNotchCostToCharmName;
    }

    private ItemChainTag ItemTag()
    {
        ItemChainTag tag = new ()
        {
            predecessor = itemType == EnemyJournalLocationType.Notes ? $"Journal_Entry-{enemyName}" : null,
            successor = itemType == EnemyJournalLocationType.Notes ? null : $"Hunter's_Notes-{enemyName}"
        };
        return tag;
    }

    public override void GiveImmediate(GiveInfo info)
    {
        FiveKnights.Instance.SaveSettings.upgradedCharm_10 = true;
    }

    private void AddNotchCostToCharmName(StringGetArgs args)
    {
        if (args.Source is LanguageString ls && ls.key == "CHARM_NAME_HONOUR")
        {
            args.Current = args.Current.Replace("-9999", $"{PlayerData.instance.charmCost_10}");
        }
    }

    public override bool Redundant() => FiveKnights.Instance.SaveSettings.IsmaEntryData.killsremaining == 0;
}