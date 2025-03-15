using ItemChanger;
using ItemChanger.Internal;
using ItemChanger.UIDefs;

namespace FiveKnights.Rando.IC;

public class PC_CharmItem : AbstractItem
{
    public int charmIndex;
    public override void GiveImmediate(GiveInfo info)
    {
        FiveKnights.Instance.SaveSettings.gotCharms[charmIndex] = true;
    }
    public PC_CharmItem(string charmName, int charmID)
    {
        name = charmName;
        charmIndex = charmID;
        UIDef = new MsgUIDef()
        {
            name = new LanguageString("Prompts", $"CHARM_NAME_{FiveKnights.CharmKeys[charmIndex]}"),
            shopDesc = new BoxedString("New charm baby, hope it comes at a low cost."),
            sprite = new PC_Sprite(name)
        };
    }

    public override bool Redundant() => FiveKnights.Instance.SaveSettings.gotCharms[charmIndex];
}

