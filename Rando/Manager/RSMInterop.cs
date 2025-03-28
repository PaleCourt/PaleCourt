using RandoSettingsManager;
using RandoSettingsManager.SettingsManagement;
using RandoSettingsManager.SettingsManagement.Versioning;

namespace FiveKnights.Rando
{
    internal static class RSM_Interop
    {
        public static void Hook()
        {
            RandoSettingsManagerMod.Instance.RegisterConnection(new RandoSettingsProxy());
        }
    }

    internal class RandoSettingsProxy : RandoSettingsProxy<RandoSettings, string>
    {
        public override string ModKey => FiveKnights.Instance.GetName();

        public override VersioningPolicy<string> VersioningPolicy { get; }
            = new EqualityVersioningPolicy<string>(FiveKnights.Instance.GetVersion());

        public override void ReceiveSettings(RandoSettings settings)
        {
            if (settings != null)
            {
                ConnectionMenu.Instance!.Apply(settings);
            }
            else
            {
                ConnectionMenu.Instance!.Disable();
            }
        }

        public override bool TryProvideSettings(out RandoSettings settings)
        {
            settings = RandoManager.Settings;
            return settings.Enabled;
        }
    }
}