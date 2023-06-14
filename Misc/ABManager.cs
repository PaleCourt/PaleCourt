using System.Collections;
using System.Collections.Generic;
using System.IO;
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
            GArenaDep, GArenaHub, GArenaHub2, GArenaIsma, GArenaH, GArenaD, GArenaZ, GArenaI, GReward,
            OWArenaD, OWArenaZ, OWArenaH, OWArenaI,
            OWArenaDep, WSArenaDep, WSArena,
            Credits, Misc, Charms, CharmUnlock,
            Artist,
            TisoBund
        }

        private static string BundleToString(Bundle bd)
        {
            return bd switch
            {
                Bundle.Sound => "soundbund",
                Bundle.TitleScreen => "titlescreen",
                Bundle.GIsma => "isma" + FiveKnights.OS,
                Bundle.GDryya => "dryya" + FiveKnights.OS,
                Bundle.GHegemol => "hegemol" + FiveKnights.OS,
                Bundle.GZemer => "zemer" + FiveKnights.OS,
                Bundle.GArenaIsma => "ismabg",
                Bundle.GArenaDep => "ggarenadep", 
                Bundle.GArenaHub => "ggarenahub",
                Bundle.GArenaHub2 => "hubasset1",
                Bundle.GArenaH => "ggarenahegemol",
                Bundle.GArenaD => "ggarenadryya",
                Bundle.GArenaI => "ggarenaisma", 
                Bundle.GArenaZ => "ggarenazemer",
                Bundle.GReward => "rewardroom", 
                Bundle.OWArenaD => "owarenadryya",
                Bundle.OWArenaH => "owarenahegemol",
                Bundle.OWArenaZ => "owarenazemer",
                Bundle.OWArenaI => "owarenaisma",
                Bundle.OWArenaDep => "owarenadep",
                Bundle.WSArenaDep => "workshopdep", 
                Bundle.WSArena => "workshopentrance",
                Bundle.Credits => "creditsbundle",
                Bundle.Misc => "miscbund",
                Bundle.Charms => "pureamulets",
                Bundle.CharmUnlock => "charmunlock",
                Bundle.Artist => "artistsbund",
                Bundle.TisoBund => "tisobundle",
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