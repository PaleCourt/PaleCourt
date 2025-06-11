using ItemChanger;
using Modding;
using Newtonsoft.Json;
using RandomizerMod.Logging;
using RandomizerMod.RC;

namespace FiveKnights.Rando;

internal static class RandoManager
{
    public static RandoSettings Settings => FiveKnights.GlobalSettings.RandoSettings;
    public static RandoSettings SaveSettings => FiveKnights.Instance.SaveSettings.RandoSaveSettings;
    public static void Hook()
    {
        Events.AfterStartNewGame += StartHook;
        ConnectionMenu.Hook();
        LogicHandler.Hook();
        ItemHandler.Hook();
        if (ModHooks.GetMod("BreakableWallRandomizer") is Mod)
        {
            WallInterop.Hook();
        }
        if (ModHooks.GetMod("GodhomeRandomizer") is Mod)
        {
            GodhomeInterop.Hook();
        }
        if (ModHooks.GetMod("TheRealJournalRando") is Mod)
        {
            JournalInterop.Hook();
        }
        SettingsLog.AfterLogSettings += AddFileSettings;
        RandoController.OnExportCompleted += StoreSave;
    }

    private static void StartHook()
    {
        // If ItemChanger is on, the standard "StartGame" function doesn't run properly, so we hook it into IC's event.
        FiveKnights.Instance.StartGame();
    }

    private static void StoreSave(RandoController controller)
    {
        FiveKnights.Instance.SaveSettings.RandoSave = RandomizerMod.RandomizerMod.IsRandoSave;
        FiveKnights.Instance.SaveSettings.RandoSaveSettings = Settings.Clone();

        if (RandomizerMod.RandomizerMod.IsRandoSave && Settings.WhiteDefenderRequirement == WhiteDefenderRequirement.NotRequired)
        {
            FiveKnights.Instance.SaveSettings.UnlockedBosses = true;
        }
    }

    private static void AddFileSettings(LogArguments args, System.IO.TextWriter tw)
    {
        if (!Settings.Enabled)
            return;

        // Log settings into the settings file
        tw.WriteLine("Pale Court Settings:");
        using JsonTextWriter jtw = new(tw) { CloseOutput = false };
        RandomizerMod.RandomizerData.JsonUtil._js.Serialize(jtw, Settings);
        tw.WriteLine();            
    }
}