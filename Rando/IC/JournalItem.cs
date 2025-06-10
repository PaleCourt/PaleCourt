using ItemChanger;
using ItemChanger.Tags;
using TheRealJournalRando.Data;
using TheRealJournalRando.IC;
using static FrogCore.JournalHelper;

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
        if (itemType == EnemyJournalLocationType.Entry)
            ItemChangerMod.Modules.GetOrAdd<JournalControlModule>().RegisterEnemyEntry(enemyName);
        if (itemType == EnemyJournalLocationType.Notes)
            ItemChangerMod.Modules.GetOrAdd<JournalControlModule>().RegisterEnemyNotes(enemyName);
    }

    private ItemChainTag ItemTag()
    {
        ItemChainTag tag = new()
        {
            predecessor = itemType == EnemyJournalLocationType.Notes ? $"Journal_Entry-{enemyName}" : null,
            successor = itemType == EnemyJournalLocationType.Notes ? null : $"Hunter's_Notes-{enemyName}"
        };
        return tag;
    }

    public override void GiveImmediate(GiveInfo info)
    {
        JournalPlayerData enemy = FiveKnights.Instance.SaveSettings.GetVariable<JournalPlayerData>($"{enemyName}EntryData");
        JournalControlModule module = ItemChangerMod.Modules.GetOrAdd<JournalControlModule>();
        if (itemType == EnemyJournalLocationType.Entry)
        {
            enemy.haskilled = true;
            if (module.EnemyEntryIsRegistered(enemyName))
                module.hasEntry[enemyName] = true;
            else
                module.hasEntry.Add(enemyName, true);
        }
        if (itemType == EnemyJournalLocationType.Notes)
        {
            enemy.killsremaining = 0;
            if (module.EnemyNotesIsRegistered(enemyName))
                module.hasNotes[enemyName] = true;
            else
                module.hasNotes.Add(enemyName, true);
        }
        FiveKnights.Instance.SaveSettings.SetVariable($"{enemyName}EntryData", enemy);        
    }
}