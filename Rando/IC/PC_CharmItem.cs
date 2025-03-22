using ItemChanger;
using ItemChanger.UIDefs;

namespace FiveKnights.Rando;

public class PC_CharmItem : AbstractItem
{
    public int charmIndex;
    public override void GiveImmediate(GiveInfo info)
    {
        FiveKnights.Instance.SaveSettings.gotCharms[charmIndex] = true;
    }
    protected override void OnLoad()
    {
        Events.OnStringGet += AddNotchCostToCharmName;
    }

    protected override void OnUnload()
    {
        Events.OnStringGet -= AddNotchCostToCharmName;
    }
    public PC_CharmItem(string charmName, int charmID)
    {
        name = charmName;
        charmIndex = charmID;
        UIDef = new MsgUIDef()
        {
            name = new LanguageString("Prompts", $"CHARM_NAME_{FiveKnights.CharmKeys[charmIndex]}"),
            shopDesc = new LanguageString("Prompts", $"CHARM_DESC_{FiveKnights.CharmKeys[charmIndex]}"),
            sprite = new PC_Sprite(name)
        };
    }

    private void AddNotchCostToCharmName(StringGetArgs args)
    {
        if (args.Source is LanguageString ls && ls.key.StartsWith("CHARM_NAME_") && ls.key.EndsWith(FiveKnights.CharmKeys[charmIndex]))
        {
            args.Current.Replace("-9999", $"{FiveKnights.Instance.SaveSettings.notchCosts[charmIndex]}");
        }
    }

    public override bool Redundant() => FiveKnights.Instance.SaveSettings.gotCharms[charmIndex];
}

