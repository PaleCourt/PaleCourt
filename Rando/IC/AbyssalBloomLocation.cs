using ItemChanger;
using ItemChanger.Locations;
using ItemChanger.Tags;
using RandomizerMod.RandomizerData;

namespace FiveKnights.Rando;

public class AbyssalBloomLocation : CoordinateLocation
{
    public AbyssalBloomLocation()
    {
        name = $"Abyssal_Bloom";
        sceneName = "Abyssal_Temple";
        x = 241.3f;
        y = 34.12f;
        tags = [LocationTag(), RecentItemsTag()];
    }

    private InteropTag LocationTag()
    {
        InteropTag tag = new();
        tag.Properties["ModSource"] = FiveKnights.Instance.GetName();
        tag.Properties["PoolGroup"] = PoolNames.Charm;
        tag.Properties["MapLocations"] = new (string, float, float)[] {(SceneNames.Abyss_10, 0.6f, 1.0f)};
        tag.Properties["VanillaItem"] = "Abyssal_Bloom";
        tag.Message = "RandoSupplementalMetadata";
        return tag;
    }

    private InteropTag RecentItemsTag()
    {
        InteropTag tag = new();
        tag.Properties["DisplaySource"] = "The Abyss";
        tag.Message = "RecentItems";
        return tag;
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        AwardCharms.AreCharmsRando += SetAsTrue;
    }

    protected override void OnUnload()
    {
        base.OnUnload();
        AwardCharms.AreCharmsRando -= SetAsTrue;
    }

    private bool SetAsTrue()
    {
        return true;
    }
}