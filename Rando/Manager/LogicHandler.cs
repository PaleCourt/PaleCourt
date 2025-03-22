using Modding;
using RandomizerCore.Json;
using RandomizerCore.Logic;
using RandomizerCore.StringItems;
using RandomizerMod.RC;
using RandomizerMod.Settings;
using System.Reflection;

namespace FiveKnights.Rando;

internal static class LogicHandler
{
    internal static void Hook()
    {
        RCData.RuntimeLogicOverride.Subscribe(0f, ApplyLogic);
    }

    private static void ApplyLogic(GenerationSettings gs, LogicManagerBuilder lmb)
    {
        if (!RandoManager.Settings.Enabled)
            return;
        
        Assembly assembly = Assembly.GetExecutingAssembly();
        JsonLogicFormat fmt = new();
        lmb.DeserializeFile(LogicFileType.Terms, fmt, assembly.GetManifestResourceStream($"FiveKnights.Rando.Resources.Logic.Terms.json"));
        lmb.DeserializeFile(LogicFileType.ItemStrings, fmt, assembly.GetManifestResourceStream($"FiveKnights.Rando.Resources.Logic.Items.json"));
        lmb.DeserializeFile(LogicFileType.Locations, fmt, assembly.GetManifestResourceStream($"FiveKnights.Rando.Resources.Logic.Locations.json"));
        lmb.DeserializeFile(LogicFileType.Transitions, fmt, assembly.GetManifestResourceStream($"FiveKnights.Rando.Resources.Logic.Transitions.json"));
        lmb.DeserializeFile(LogicFileType.Waypoints, fmt, assembly.GetManifestResourceStream($"FiveKnights.Rando.Resources.Logic.Waypoints.json"));

        string[] bossLocations = ["Boon_of_Hallownest", "Kings_Honour", "Mark_of_Purity", "Vessels_Lament"];
        if (RandoManager.Settings.WhiteDefenderRequirement > WhiteDefenderRequirement.Vanilla)
        {
            foreach (string location in bossLocations)
            {
                lmb.DoSubst(new (location, "Defeated_White_Defender", "Ogrim's_Call"));
            }
        }        
    }

    private static void AddGodhomeLogic(GenerationSettings gs, LogicManagerBuilder lmb)
    {
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
}