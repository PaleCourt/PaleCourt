using Newtonsoft.Json;
using RandomizerMod.Logging;
using RandomizerMod.RC;

namespace FiveKnights;

internal static class RandoManager
{
    public static RandoSettings Settings => FiveKnights.GlobalSettings.RandoSettings;
    public static void Hook()
    {
        LogicHandler.Hook();
        ItemHandler.Hook();
        SettingsLog.AfterLogSettings += AddFileSettings;
        RandoController.OnExportCompleted += StoreSave;
    }

    private static void StoreSave(RandoController controller)
    {
        FiveKnights.Instance.SaveSettings.RandoSave = RandomizerMod.RandomizerMod.IsRandoSave;
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