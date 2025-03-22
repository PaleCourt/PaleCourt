using FiveKnights.Rando;
using RandomizerCore.Logic;
using RandomizerCore.StringItems;
using RandomizerMod.RC;
using RandomizerMod.Settings;

internal static class GodhomeInterop
{
    internal static void Hook()
    {
        RCData.RuntimeLogicOverride.Subscribe(5f, AddGodhomeLogic);
        RequestBuilder.OnUpdate.Subscribe(11f, AddStatueObjects);
    }

    private static void AddGodhomeLogic(GenerationSettings gs, LogicManagerBuilder lmb)
    {
        if (!RandoManager.Settings.Enabled)
            return;

        string[] bosses = ["Isma", "Isma2", "Dryya", "Hegemol", "Zemer", "Zemer2"];
        foreach (string boss in bosses)
        {
            lmb.GetOrAddTerm($"GG_{boss}");
            lmb.AddItem(new StringItemTemplate($"Statue_Mark-{boss.Replace("2", "_Rematch")}", $"GG_{boss}++"));
            lmb.AddLogicDef(new RawLogicDef($"Empty_Mark-{boss.Replace("2", "_Rematch")}", $"GG_Workshop + GG_Dung_Defender>0 + GG_{boss}>0"));
            lmb.AddLogicDef(new RawLogicDef($"Bronze_Mark-{boss.Replace("2", "_Rematch")}", $"GG_Workshop + GG_Dung_Defender>0 + GG_{boss}>0 + BOSS"));
            lmb.AddLogicDef(new RawLogicDef($"Silver_Mark-{boss.Replace("2", "_Rematch")}", $"GG_Workshop + GG_Dung_Defender>0 + GG_{boss}>0 + BOSS"));
            lmb.AddLogicDef(new RawLogicDef($"Gold_Mark-{boss.Replace("2", "_Rematch")}", $"GG_Workshop + GG_Dung_Defender>0 + GG_{boss}>2 + BOSS"));
        }
    }

    private static void AddStatueObjects(RequestBuilder rb)
    {
        if (!RandoManager.Settings.Enabled)
            return;
        
        int copies = rb.GetItemGroupFor("Statue_Mark-Gruz_Mother").Items.GetCount("Statue_Mark-Gruz_Mother");
        int locationCount = 0;
        bool empty = rb.TryGetLocationRequest("Empty_Mark-Gruz_Mother", out _);
        bool bronze = rb.TryGetLocationRequest("Bronze_Mark-Gruz_Mother", out _);
        bool silver = rb.TryGetLocationRequest("Silver_Mark-Gruz_Mother", out _);
        bool gold = rb.TryGetLocationRequest("Gold_Mark-Gruz_Mother", out _);

        string[] bosses = ["Isma", "Isma2", "Dryya", "Hegemol", "Zemer", "Zemer2"];
        foreach (string boss in bosses)
        {
            rb.AddItemByName($"Statue_Mark-{boss.Replace("2", "_Rematch")}", copies);
        }

        if (bronze)
        {
            locationCount++;
            foreach (string boss in bosses)
            {
                rb.AddLocationByName($"Bronze_Mark-{boss.Replace("2", "_Rematch")}");
            }
        }
        else
        {
            foreach (string boss in bosses)
            {
                rb.AddToVanilla(new ($"Statue_Mark-{boss.Replace("2", "_Rematch")}", $"Bronze_Mark-{boss.Replace("2", "_Rematch")}"));
            }
        }

        if (silver)
        {
            locationCount++;
            foreach (string boss in bosses)
            {
                rb.AddLocationByName($"Silver_Mark-{boss.Replace("2", "_Rematch")}");
            }
        }
        else
        {
            foreach (string boss in bosses)
            {
                rb.AddToVanilla(new ($"Statue_Mark-{boss.Replace("2", "_Rematch")}", $"Silver_Mark-{boss.Replace("2", "_Rematch")}"));
            }
        }

        if (gold)
        {
            locationCount++;
            foreach (string boss in bosses)
            {
                rb.AddLocationByName($"Gold_Mark-{boss.Replace("2", "_Rematch")}");
            }
        }
        else
        {
            foreach (string boss in bosses)
            {
                rb.AddToVanilla(new ($"Statue_Mark-{boss.Replace("2", "_Rematch")}", $"Gold_Mark-{boss.Replace("2", "_Rematch")}"));
            }
        }

        if (empty)
        {
            locationCount++;
            foreach (string boss in bosses)
            {
                rb.AddLocationByName($"Empty_Mark-{boss.Replace("2", "_Rematch")}");
                // If the location exists but the item is not present in the pool, we need to add a vanilla def for logic to work.
                if (copies < locationCount)
                {
                    rb.AddToVanilla(new ($"Statue_Mark-{boss.Replace("2", "_Rematch")}", $"Empty_Mark-{boss.Replace("2", "_Rematch")}"));
                }
            }
        }
        else
        {
            // We only want to create a vanilla def if both the item and the location are vanilla.
            if (copies == locationCount)
            {
                foreach (string boss in bosses)
                {
                    rb.AddToVanilla(new ($"Statue_Mark-{boss.Replace("2", "_Rematch")}", $"Empty_Mark-{boss.Replace("2", "_Rematch")}"));
                }
            }
        }
    }
}