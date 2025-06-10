using FiveKnights.BossManagement;
using ItemChanger;
using ItemChanger.Locations;
using ItemChanger.Tags;
using ItemChanger.Util;

namespace FiveKnights.Rando;

public class ChampionsCallLocation : AutoLocation
{
    bool hitless;
    public ChampionsCallLocation(bool _hitless)
    {
        name = _hitless ? "Radiant_Champion's_Call" : "Champion's_Call";
        hitless = _hitless;
        sceneName = "GG_Workshop";
        tags = [LocationTag()];
    }

    private InteropTag LocationTag()
    {
        InteropTag tag = new();
        tag.Properties["ModSource"] = FiveKnights.Instance.GetName();
        tag.Properties["PoolGroup"] = "Pantheon Marks";
        tag.Properties["MapLocation"] = new (string, float, float)[] { (SceneNames.GG_Waterways, 0.6f, 1.0f) };
        tag.Properties["VanillaItem"] = hitless ? "Radiant_Champion's_Call" : "Champion's_Call";
        tag.Message = "RandoSupplementalMetadata";
        return tag;
    }

    protected override void OnLoad()
    {
        GGBossManager.GrantReward += GrantReward;
    }

    protected override void OnUnload()
    {
        GGBossManager.GrantReward -= GrantReward;
    }
    
    private void GrantReward(bool radiant)
    {
        if (!hitless || radiant)
        {
            ItemUtility.GiveSequentially(Placement.Items, Placement, new GiveInfo()
            {
                FlingType = FlingType.Everywhere,
                MessageType = MessageType.Any,
            });

            Placement.AddVisitFlag(VisitState.Opened);
        }
    }
}