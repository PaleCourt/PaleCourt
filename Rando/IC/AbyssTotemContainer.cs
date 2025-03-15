using ItemChanger;
using ItemChanger.Containers;
using UnityEngine;
using SFCore.Utils;

namespace FiveKnights;
public class AbyssTotemContainer : SoulTotemContainer
{
    public const string ClassName = "AbyssTotem";
    public override string Name => ClassName;

    public override GameObject GetNewContainer(ContainerInfo info)
    {
        var totem = base.GetNewContainer(info);
        totem.GetComponent<SpriteRenderer>().sprite = ABManager.AssetBundles[ABManager.Bundle.Misc].LoadAsset<Sprite>("inf_totem_a");
        totem.Find("Dimmer").GetComponent<SpriteRenderer>().sprite = ABManager.AssetBundles[ABManager.Bundle.Misc].LoadAsset<Sprite>("inf_totem_b");
        totem.Find("Glower").GetComponent<SpriteRenderer>().sprite = ABManager.AssetBundles[ABManager.Bundle.Misc].LoadAsset<Sprite>("inf_totem_c");
        return totem;
    }
}