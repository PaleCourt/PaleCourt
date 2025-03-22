using System;
using GodhomeRandomizer.Modules;
using ItemChanger;
using ItemChanger.UIDefs;
using Newtonsoft.Json;

namespace FiveKnights.Rando;

public class GodhomeItem : AbstractItem
{
    [JsonProperty]
    string suffix { get; set; }
    public GodhomeItem(string boss, bool rematch)
    {
        name = rematch ? $"Statue_Mark-{boss}_Rematch" : $"Statue_Mark-{boss}";
        suffix = rematch ? "2" : "";
        UIDef = new MsgUIDef()
        {
            name = new BoxedString(name.Replace("-", " - ").Replace('_', ' ')),
            shopDesc = new BoxedString("Godhome yippee"),
            sprite = new PC_Sprite(boss)
        };
    }

    public override void GiveImmediate(GiveInfo info)
    {
        string boss = name.Split('-')[1].Replace("_Rematch", "");
        StatueModule module = ItemChangerMod.Modules.Get<StatueModule>();
        BossStatue.Completion statue = FiveKnights.Instance.SaveSettings.GetVariable<BossStatue.Completion>($"Completion{boss}{suffix}");
        if (module.Settings.RandomizeStatueAccess == GodhomeRandomizer.Settings.AccessMode.Randomized && !statue.isUnlocked)
        {
            module.UnlockedScenes.Add(name);
            statue.isUnlocked = true;
        }
        else if (module.Settings.RandomizeTiers > GodhomeRandomizer.Settings.TierLimitMode.Vanilla && !statue.completedTier1)
        {
            module.AttunedStatues.Add(name);
            statue.completedTier1 = true;
        }
        else if (module.Settings.RandomizeTiers > GodhomeRandomizer.Settings.TierLimitMode.ExcludeAscended && !statue.completedTier2)
        {
            module.AttunedStatues.Add(name);
            statue.completedTier2 = true;
        }
        else if (module.Settings.RandomizeTiers > GodhomeRandomizer.Settings.TierLimitMode.ExcludeRadiant && !statue.completedTier3)
        {
            module.AttunedStatues.Add(name);
            statue.completedTier3 = true;
        }
        else
        {
            throw new ArgumentException("This item should not be present in the pool or is having unexpected effects.");
        }
        FiveKnights.Instance.SaveSettings.SetVariable($"Completion{boss}{suffix}", statue);
        int current = module.CurrentMarks(name);
        int total = module.TotalMarks;
        if (UIDef is MsgUIDef ui && total > 1)
            ui.name = new BoxedString($"{ui.name.Value} ({current} / {total})");
    }

    public override bool Redundant() => StatueModule.Instance.CurrentMarks(name.Split('-')[1]) >= StatueModule.Instance.TotalMarks;
}