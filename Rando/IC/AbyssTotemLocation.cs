using ItemChanger;
using ItemChanger.Locations;
using ItemChanger.Tags;
using RandomizerMod.RandomizerData;

namespace FiveKnights;

public class AbyssTotemLocation : CoordinateLocation
{
    public float pinX;
    public float pinY;
    public AbyssTotemLocation(int index, float _x, float _y, float _pinX, float _pinY)
    {
        name = $"Soul_Totem-Abyssal_Temple_{index}";
        sceneName = "Abyssal_Temple";
        x = _x;
        y = _y;
        pinX = _pinX;
        pinY = _pinY;
        tags = [LocationTag()];
    }

    private InteropTag LocationTag()
    {
        InteropTag tag = new();
        tag.Properties["ModSource"] = FiveKnights.Instance.GetName();
        tag.Properties["PoolGroup"] = PoolNames.Soul;
        tag.Properties["VanillaItem"] = "Abyss_Totem";
        tag.Properties["MapLocation"] = new (string, float, float)[] {(SceneNames.Abyss_09, pinX, pinY)};
        tag.Message = "RandoSupplementalMetadata";
        return tag;
    }
}