using ItemChanger;

namespace FiveKnights.Rando.IC;

public class ProgressiveDCrestItem : AbstractItem
{
    public override void GiveImmediate(GiveInfo info)
    {
        if (PlayerData.instance.gotCharm_10)
        {
            FiveKnights.Instance.SaveSettings.upgradedCharm_10 = true;
        }
        else
        {
            PlayerData.instance.gotCharm_10 = true;
        }
    }
    public override bool Redundant() => FiveKnights.Instance.SaveSettings.upgradedCharm_10;
}

