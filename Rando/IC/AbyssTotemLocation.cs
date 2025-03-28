using System;
using System.Collections.Generic;
using ItemChanger;
using ItemChanger.Locations;
using ItemChanger.Tags;
using RandomizerMod.RandomizerData;

namespace FiveKnights.Rando;

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
        tags = [LocationTag(), RecentItemsTag()];
    }

    private InteropTag RecentItemsTag()
    {
        InteropTag tag = new();
        tag.Properties["DisplaySource"] = "The Abyss";
        tag.Message = "RecentItems";
        return tag;
    }

    private InteropTag LocationTag()
    {
        InteropTag tag = new();
        tag.Properties["ModSource"] = FiveKnights.Instance.GetName();
        tag.Properties["PoolGroup"] = PoolNames.Soul;
        tag.Properties["VanillaItem"] = "Abyss_Totem";
        tag.Properties["MapLocations"] = new (string, float, float)[] {(SceneNames.Abyss_10, pinX, pinY)};
        tag.Properties["PinSpriteKey"] = "Soul Totems";
        tag.Message = "RandoSupplementalMetadata";
        return tag;
    }
    protected override void OnLoad()
    {
        base.OnLoad();
        AbyssalTemple.AreTotemsRando += SetAsTrue;
    }

    protected override void OnUnload()
    {
        base.OnUnload();
        AbyssalTemple.AreTotemsRando -= SetAsTrue;
    }

    private bool SetAsTrue()
    {
        return true;
    }
}