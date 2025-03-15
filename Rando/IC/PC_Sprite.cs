using ItemChanger;
using ItemChanger.Internal;
using Newtonsoft.Json;
using System;
using UnityEngine;

namespace FiveKnights;
[Serializable]
public class PC_Sprite : ISprite
{
    private static SpriteManager EmbeddedSpriteManager = new(typeof(FiveKnights).Assembly, "FiveKnights.assets.");
    public string Key { get; set; }
    public PC_Sprite(string key)
    {
        if (!string.IsNullOrEmpty(key))
            Key = key;
    }
    [JsonIgnore]
    public Sprite Value => EmbeddedSpriteManager.GetSprite(Key);
    public ISprite Clone() => (ISprite)MemberwiseClone();
}