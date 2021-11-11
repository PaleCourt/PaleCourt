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
            Sound, TitleScreen, 
            GIsma, GDryya, GHegemol, GZemer, 
            GArenaDep, GArenaHub, GArenaHub2, GArenaIsma, GArenaH, GArenaD, GArenaZ, GArenaI,
            OWArenaD, OWArenaZ, OWArenaH, OWArenaI,
            OWArenaDep, WSArenaDep, WSArena,
            Misc, Charms
        }

        private static string BundleToString(Bundle bd)
        {
            return bd switch
            {
                Bundle.Sound => "soundbund",
                Bundle.TitleScreen => "titleScreen",
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
                Bundle.OWArenaD => "owarenadryya",
                Bundle.OWArenaH => "owarenahegemol",
                Bundle.OWArenaZ => "owarenazemer",
                Bundle.OWArenaI => "owarenaisma",
                Bundle.OWArenaDep => "owarenadep",
                Bundle.WSArenaDep => "workShopEntranceDep",
                Bundle.WSArena => "workShopEntrance",
                Bundle.Misc => "miscbund",
                Bundle.Charms => "pureamulets",
                _ => ""
            };
        }

        public static AssetBundle Load(Bundle bd)
        {
            if (AssetBundles.ContainsKey(bd) && AssetBundles[bd] != null) return AssetBundles[bd];
            using Stream s = _asm.GetManifestResourceStream($"FiveKnights.StreamingAssets.{BundleToString(bd)}");
            var ab = AssetBundle.LoadFromStream(s);
            AssetBundles[bd] = ab;
            s?.Dispose();
            return ab;
        }
        public static IEnumerator LoadAsync(Bundle bd)
        {
            using Stream s = _asm.GetManifestResourceStream($"FiveKnights.StreamingAssets.{BundleToString(bd)}");
            var request = AssetBundle.LoadFromStreamAsync(s);
            yield return request;
            AssetBundles[bd] = request.assetBundle;
            s?.Dispose();
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
            foreach (var k in AssetBundles.Keys)
            {
                var ab = AssetBundles[k];
                if (ab != null && k != Bundle.TitleScreen)
                {
                    Log($"Unloaded assetbundle {ab.name}");
                    ab.Unload(true);
                }
            }
        }

        private static void Log(object o)
        {
            Logger.Log("[BundleManager] " + o);
        }
    }
}