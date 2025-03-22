using ItemChanger;
using ItemChanger.Tags;
using ItemChanger.UIDefs;

namespace FiveKnights.Rando;

public class KingsHonourItem : AbstractItem
{
    public KingsHonourItem()
    {
        name = "Kings_Honour";
        UIDef = new MsgUIDef()
        {
            name = new LanguageString("Prompts", "CHARM_NAME_HONOUR"),
            shopDesc = new LanguageString("Prompts", "CHARM_DESC_HONOUR"),
            sprite = new PC_Sprite(name)
        };
        tags = [ItemTag()];
    }

    protected override void OnLoad()
    {
        Events.OnStringGet += AddNotchCostToCharmName;
    }

    protected override void OnUnload()
    {
        Events.OnStringGet -= AddNotchCostToCharmName;
    }

    private ItemChainTag ItemTag()
    {
        ItemChainTag tag = new ()
        {
            predecessor = "Defender's_Crest",
            successor = null
        };
        return tag;
    }

    public override void GiveImmediate(GiveInfo info)
    {
        FiveKnights.Instance.SaveSettings.upgradedCharm_10 = true;
    }

    private void AddNotchCostToCharmName(StringGetArgs args)
    {
        if (args.Source is LanguageString ls && ls.key == "CHARM_NAME_HONOUR")
        {
            args.Current.Replace("-9999", $"{PlayerData.instance.charmCost_10}");
        }
    }

    public override bool Redundant() => FiveKnights.Instance.SaveSettings.upgradedCharm_10;
}