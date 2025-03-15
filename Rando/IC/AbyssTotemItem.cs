using ItemChanger;
using ItemChanger.Items;
using ItemChanger.UIDefs;

namespace FiveKnights.Rando.IC;

public class AbyssTotemItem : SoulTotemItem
{
    public AbyssTotemItem()
    {
        name = "Abyss_Totem";
        soul = 200;
        soulTotemSubtype = SoulTotemSubtype.B;
        hitCount = -1;
        UIDef = new MsgUIDef()
        {
            name = new BoxedString("Soul Refill"),
            shopDesc = new BoxedString("It comes from the depths of the abyss."),
            sprite = new ItemChangerSprite("ShopIcons.Soul"),
        };
    }
    public override string GetPreferredContainer() => AbyssTotemContainer.ClassName;
}

