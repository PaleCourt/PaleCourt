using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FiveKnights.Rando;
using ItemChanger;
using Newtonsoft.Json;
using RandomizerCore.Logic;
using RandomizerCore.StringItems;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;
using RandomizerMod.Settings;

internal static class GodhomeInterop
{
    internal static void Hook()
    {
        DefineObjects();
        RCData.RuntimeLogicOverride.Subscribe(5f, AddLogic);
        RequestBuilder.OnUpdate.Subscribe(11f, AddObjects);
        //RequestBuilder.OnUpdate.Subscribe(-180f, DefineTransitions);
    }

    private static void DefineObjects()
    {
        Finder.DefineCustomItem(new GodhomeItem("Isma", false));
        Finder.DefineCustomItem(new GodhomeItem("Isma", true));
        Finder.DefineCustomItem(new GodhomeItem("Zemer", false));
        Finder.DefineCustomItem(new GodhomeItem("Zemer", true));
        Finder.DefineCustomItem(new GodhomeItem("Dryya", false));
        Finder.DefineCustomItem(new GodhomeItem("Hegemol", false));

        string[] bosses = ["Isma", "Isma2", "Dryya", "Hegemol", "Zemer", "Zemer2"];
        foreach (string boss in bosses)
        {
            Finder.DefineCustomLocation(new GodhomeLocation($"Empty_Mark-{boss.Replace("2", "_Rematch")}", boss, -1));
            Finder.DefineCustomLocation(new GodhomeLocation($"Bronze_Mark-{boss.Replace("2", "_Rematch")}", boss, 0));
            Finder.DefineCustomLocation(new GodhomeLocation($"Silver_Mark-{boss.Replace("2", "_Rematch")}", boss, 1));
            Finder.DefineCustomLocation(new GodhomeLocation($"Gold_Mark-{boss.Replace("2", "_Rematch")}", boss, 2));
        }
    }

    private static void AddLogic(GenerationSettings gs, LogicManagerBuilder lmb)
    {
        if (!RandoManager.Settings.Enabled)
            return;

        try
        {
            lmb.GetTerm("GG_Dung_Defender");
        }
        catch (KeyNotFoundException)
        {
            return;
        }

        string[] bosses = ["Isma", "Isma2", "Dryya", "Hegemol", "Zemer", "Zemer2"];
        foreach (string boss in bosses)
        {
            lmb.GetOrAddTerm($"GG_{boss}");
            lmb.AddItem(new StringItemTemplate($"Statue_Mark-{boss.Replace("2", "_Rematch")}", $"GG_{boss}++"));
            if (boss.Contains("2"))
            {
                lmb.AddLogicDef(new RawLogicDef($"Empty_Mark-{boss.Replace("2", "_Rematch")}", $"White_Palace_09[door_Land_of_Storms_return] + GG_{boss.Replace("2", "")}>0 + GG_{boss}>0"));
                lmb.AddLogicDef(new RawLogicDef($"Bronze_Mark-{boss.Replace("2", "_Rematch")}", $"White_Palace_09[door_Land_of_Storms_return] + GG_{boss.Replace("2", "")}>0 + GG_{boss}>0 + COMBAT[{boss}]"));
                lmb.AddLogicDef(new RawLogicDef($"Silver_Mark-{boss.Replace("2", "_Rematch")}", $"White_Palace_09[door_Land_of_Storms_return] + GG_{boss.Replace("2", "")}>0 + GG_{boss}>0 + COMBAT[{boss}]"));
                lmb.AddLogicDef(new RawLogicDef($"Gold_Mark-{boss.Replace("2", "_Rematch")}", $"White_Palace_09[door_Land_of_Storms_return] + GG_{boss.Replace("2", "")}>0 + GG_{boss}>2 + COMBAT[{boss}]"));
            }
            else
            {
                lmb.AddLogicDef(new RawLogicDef($"Empty_Mark-{boss.Replace("2", "_Rematch")}", $"White_Palace_09[door_Land_of_Storms_return] + GG_{boss}>0"));
                lmb.AddLogicDef(new RawLogicDef($"Bronze_Mark-{boss.Replace("2", "_Rematch")}", $"White_Palace_09[door_Land_of_Storms_return] + GG_{boss}>0 + COMBAT[{boss}]"));
                lmb.AddLogicDef(new RawLogicDef($"Silver_Mark-{boss.Replace("2", "_Rematch")}", $"White_Palace_09[door_Land_of_Storms_return] + GG_{boss}>0 + COMBAT[{boss}]"));
                lmb.AddLogicDef(new RawLogicDef($"Gold_Mark-{boss.Replace("2", "_Rematch")}", $"White_Palace_09[door_Land_of_Storms_return] + GG_{boss}>2 + COMBAT[{boss}]"));
            }
        }
    }

    private static void AddObjects(RequestBuilder rb)
    {
        if (!RandoManager.Settings.Enabled)
            return;

        int copies = rb.GetItemGroupFor("Statue_Mark-Dung_Defender").Items.GetCount("Statue_Mark-Dung_Defender");

        if (copies == 0)
            return;

        int locationCount = 0;
        bool empty = rb.TryGetLocationRequest("Empty_Mark-Dung_Defender", out _);
        bool bronze = rb.TryGetLocationRequest("Bronze_Mark-Dung_Defender", out _);
        bool silver = rb.TryGetLocationRequest("Silver_Mark-Dung_Defender", out _);
        bool gold = rb.TryGetLocationRequest("Gold_Mark-Dung_Defender", out _);

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
                rb.AddToVanilla(new($"Statue_Mark-{boss.Replace("2", "_Rematch")}", $"Bronze_Mark-{boss.Replace("2", "_Rematch")}"));
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
                rb.AddToVanilla(new($"Statue_Mark-{boss.Replace("2", "_Rematch")}", $"Silver_Mark-{boss.Replace("2", "_Rematch")}"));
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
                rb.AddToVanilla(new($"Statue_Mark-{boss.Replace("2", "_Rematch")}", $"Gold_Mark-{boss.Replace("2", "_Rematch")}"));
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
                    rb.AddToVanilla(new($"Statue_Mark-{boss.Replace("2", "_Rematch")}", $"Empty_Mark-{boss.Replace("2", "_Rematch")}"));
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
                    rb.AddToVanilla(new($"Statue_Mark-{boss.Replace("2", "_Rematch")}", $"Empty_Mark-{boss.Replace("2", "_Rematch")}"));
                }
            }
        }
    }
    
    private static void DefineTransitions(RequestBuilder rb)
    {
        if (!RandoManager.Settings.Enabled)
            return;

        Assembly assembly = Assembly.GetExecutingAssembly();
        JsonSerializer jsonSerializer = new() {TypeNameHandling = TypeNameHandling.Auto};
        using Stream stream = assembly.GetManifestResourceStream("FiveKnights.Rando.Resources.Data.GodhomeTransitions.json");
        StreamReader reader = new(stream);
        List<TransitionDef> list = jsonSerializer.Deserialize<List<TransitionDef>>(new JsonTextReader(reader));

        int group = 1;
        foreach (TransitionDef def in list)
        {
            bool shouldBeIncluded = def.IsMapAreaTransition && (rb.gs.TransitionSettings.Mode >= TransitionSettings.TransitionMode.MapAreaRandomizer);
            shouldBeIncluded |= def.IsTitledAreaTransition && (rb.gs.TransitionSettings.Mode >= TransitionSettings.TransitionMode.FullAreaRandomizer);
            shouldBeIncluded |= rb.gs.TransitionSettings.Mode >= TransitionSettings.TransitionMode.RoomRandomizer;
            if (shouldBeIncluded)
            {
                rb.EditTransitionRequest($"{def.SceneName}[{def.DoorName}]", info => info.getTransitionDef = () => def);
                bool uncoupled = rb.gs.TransitionSettings.TransitionMatching == TransitionSettings.TransitionMatchingSetting.NonmatchingDirections;
                if (uncoupled)
                {
                    SelfDualTransitionGroupBuilder tgb = rb.EnumerateTransitionGroups().First(x => x.label == RBConsts.TwoWayGroup) as SelfDualTransitionGroupBuilder;
                    tgb.Transitions.Add($"{def.SceneName}[{def.DoorName}]");
                }
                else
                {
                    SymmetricTransitionGroupBuilder stgb = rb.EnumerateTransitionGroups().First(x => x.label == RBConsts.TwoWayGroup) as SymmetricTransitionGroupBuilder;
                    if (group == 1)
                        stgb.Group1.Add($"{def.SceneName}[{def.DoorName}]");
                    else
                        stgb.Group2.Add($"{def.SceneName}[{def.DoorName}]");
                }
                group = group == 1 ? 2 : 1;
            }
            else
            {
                rb.EditTransitionRequest($"{def.SceneName}[{def.DoorName}]", info => info.getTransitionDef = () => def);
                rb.EnsureVanillaSourceTransition($"{def.SceneName}[{def.DoorName}]");
            }
        }
    }
}