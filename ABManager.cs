using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Logger = Modding.Logger;

namespace FiveKnights
{
    public static class ABManager
    {
        public static Dictionary<Bundle, AssetBundle> AssetBundles { get; } = new Dictionary<Bundle, AssetBundle>();
        private static readonly Assembly _asm = Assembly.GetExecutingAssembly();

        public enum Bundle
        {
            Sound, 
            GIsma, GDryya, GHegemol, GZemer, 
            GArenaDep, GArenaHub, GArenaHub2, GArenaIsma, GArenaH, GArenaD, GArenaZ, GArenaI,
            OWArenaD, OWArenaZ, OWArenaH,
            OWArenaDep, WSArenaDep, WSArena,
            Misc
        }

        private static string BundleToString(Bundle bd)
        {
            return bd switch
            {
                Bundle.Sound => "soundbund",
                Bundle.GIsma => "isma" + FiveKnights.OS,
                Bundle.GDryya => "dryya" + FiveKnights.OS,
                Bundle.GHegemol => "hegemol" + FiveKnights.OS,
                Bundle.GZemer => "zemer" + FiveKnights.OS,
                Bundle.GArenaIsma => "ismabg",
                Bundle.GArenaDep => "ggArenaDep",
                Bundle.GArenaHub => "ggArenaHub",
                Bundle.GArenaHub2 => "hubasset1",
                Bundle.GArenaH => "ggArenaHegemol",
                Bundle.GArenaD => "ggArenaDryya",
                Bundle.GArenaI => "ggArenaIsma",
                Bundle.GArenaZ => "ggArenaZemer",
                Bundle.OWArenaD => "owArenaDryya",
                Bundle.OWArenaH => "owArenaHegemol",
                Bundle.OWArenaZ => "owArenaZemer",
                Bundle.OWArenaDep => "owArenaDep",
                Bundle.WSArenaDep => "workShopEntranceDep",
                Bundle.WSArena => "workShopEntrance",
                Bundle.Misc => "miscbund",
                _ => ""
            };
        }

        public static AssetBundle Load(Bundle bd)
        {
            using Stream s = _asm.GetManifestResourceStream($"FiveKnights.StreamingAssets.{BundleToString(bd)}");
            var ab = AssetBundle.LoadFromStream(s);
            AssetBundles[bd] = ab;
            s?.Dispose();
            return ab;
        }

        public static void ResetBundle(Bundle bd)
        {
            Log($"Resetting bundle {bd}");
            
            if (!AssetBundles.TryGetValue(bd, out var bundle) || bundle == null)
            {
                Log($"Could not find AssetBundle {BundleToString(bd)}!");
                return;
            }
            
            bundle.Unload(true);
            GameManager.instance.StartCoroutine(DelayedReset());
            
            IEnumerator DelayedReset()
            {
                yield return null;
                AssetBundles[bd] = Load(bd);
                Log($"Reset bundle {bd}");
            }
        }

        public static void UnloadAll()
        {
            foreach (var i in AssetBundles.Values)
            {
                if (i != null)
                {
                    i.Unload(true);
                    Log($"Unloaded assetbundle {i.name}");
                }
            }
        }

        private static void Log(object o)
        {
            Logger.Log("[BundleManager] " + o);
        }
    }
}