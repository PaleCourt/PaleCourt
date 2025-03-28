using FiveKnights.BossManagement;
using GodhomeRandomizer.Modules;
using ItemChanger;
using ItemChanger.Locations;
using ItemChanger.Tags;
using ItemChanger.Util;

namespace FiveKnights.Rando;

public class GodhomeLocation : AutoLocation
{
    public enum Tier
        {
            Unlock = -1,
            Attuned = 0,
            Ascended = 1,
            Radiant = 2
        }
    public string bossName { get; set; }
    public Tier statueTier { get; set; }

    public GodhomeLocation(string _name, string _bossName, int tier)
    {
        name = _name;
        sceneName = "GG_Workshop";
        statueTier = (Tier)tier;
        bossName = _bossName;
        tags = [LocationTag()];
    }
    private InteropTag LocationTag()
    {
        InteropTag tag = new();
        tag.Properties["ModSource"] = FiveKnights.Instance.GetName();
        tag.Properties["PoolGroup"] = "Statue Marks";
        tag.Properties["VanillaItem"] = name;
        tag.Message = "RandoSupplementalMetadata";
        return tag;
    }

    protected override void OnLoad()
    {
        OWBossManager.UnlockGodhome += GrantUnlock;
        GGBossManager.UnlockGodhome += GrantUnlock;
        GGBossManager.RewardGodhome += GrantFight;
        GGBossManager.IsTierRando += SetAsTrue;
    }

    protected override void OnUnload()
    {
        OWBossManager.UnlockGodhome -= GrantUnlock;
        GGBossManager.UnlockGodhome -= GrantUnlock;
        GGBossManager.RewardGodhome -= GrantFight;
        GGBossManager.IsTierRando -= SetAsTrue;
    }

    private bool GrantUnlock(string boss)
    {
        bool result = bossName == boss && statueTier == Tier.Unlock;
        StatueModule module = ItemChangerMod.Modules.Get<StatueModule>();
        if (result)
        {
            {
                ItemUtility.GiveSequentially(Placement.Items, Placement, new GiveInfo()
                {
                    FlingType = FlingType.Everywhere,
                    MessageType = MessageType.Corner,
                });

                Placement.AddVisitFlag(VisitState.Opened);
            }
        }
        return module.Settings.RandomizeStatueAccess == GodhomeRandomizer.Settings.AccessMode.Randomized;
    }

    private void GrantFight(string boss, int level)
    {
        if (bossName == boss && level >= (int)statueTier && statueTier > Tier.Unlock)
        {
            ItemUtility.GiveSequentially(Placement.Items, Placement, new GiveInfo()
            {
                FlingType = FlingType.Everywhere,
                MessageType = MessageType.Any,
            });

            Placement.AddVisitFlag(VisitState.Opened);
        }
    }

    
    private bool SetAsTrue(int level)
    {
        return level <= (int)statueTier;
    }
}