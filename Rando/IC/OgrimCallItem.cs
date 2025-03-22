using ItemChanger;
using ItemChanger.UIDefs;

namespace FiveKnights.Rando;

public class OgrimCallItem : AbstractItem
{
    public OgrimCallItem()
    {
        name = "Ogrim's_Call";
        UIDef = new MsgUIDef()
        {
            name = new BoxedString("Ogrim's Call"),
            shopDesc = new BoxedString("I call u."),
            sprite = new PC_Sprite("PaleCourtDlcIcon")
        };
    }

    public override void GiveImmediate(GiveInfo info)
    {
        FiveKnights.Instance.SaveSettings.UnlockedBosses = true;
    }

    public override bool Redundant() => FiveKnights.Instance.SaveSettings.UnlockedBosses;
}