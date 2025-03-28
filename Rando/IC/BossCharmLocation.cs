using ItemChanger;
using ItemChanger.Locations;
using ItemChanger.Tags;
using ItemChanger.Util;
using RandomizerMod.RandomizerData;
using TMPro;

namespace FiveKnights.Rando;

public class BossCharmLocation : AutoLocation
{
    string bossName { get; set; }
    float x { get; set; }
    float y { get; set; }
    public BossCharmLocation(string _name, string _sceneName, string _bossName, float _x = 0.0f, float _y = 0.0f)
    {
        name = _name;
        sceneName = _sceneName;
        bossName = _bossName;
        x = _x;
        y = _y;
        tags = [LocationTag()];
    }

    private InteropTag LocationTag()
    {
        InteropTag tag = new();
        tag.Properties["ModSource"] = FiveKnights.Instance.GetName();
        tag.Properties["PoolGroup"] = PoolNames.Charm;
        tag.Properties["VanillaItem"] = name;
        tag.Properties["MapLocations"] = new (string, float, float)[] {(sceneName, x, y)};
        tag.Message = "RandoSupplementalMetadata";
        return tag;
    }

    protected override void OnLoad()
    {
        AwardCharms.AreCharmsRando += SetAsTrue;
        AwardCharms.OnCharmReward += GrantPlacement;
    }

    protected override void OnUnload()
    {
        AwardCharms.AreCharmsRando -= SetAsTrue;
        AwardCharms.OnCharmReward -= GrantPlacement;
    }

    private void GrantPlacement(string boss)
    {
        if (bossName == boss)
        {
            ItemUtility.GiveSequentially(Placement.Items, Placement, new GiveInfo()
            {
                FlingType = FlingType.Everywhere,
                MessageType = MessageType.Any,
            });

            Placement.AddVisitFlag(VisitState.Opened);
        }
    }

    private bool SetAsTrue()
    {
        return true;
    }
}