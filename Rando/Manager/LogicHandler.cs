using RandomizerCore.Json;
using RandomizerCore.Logic;
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
        lmb.DeserializeFile(LogicFileType.Macros, fmt, assembly.GetManifestResourceStream($"FiveKnights.Rando.Resources.Logic.Macros.json"));
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
}