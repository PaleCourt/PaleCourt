using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using FiveKnights.BossManagement;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.Tags;
using Newtonsoft.Json;
using RandomizerCore.Logic;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;
using RandomizerMod.Settings;

namespace FiveKnights.Rando;
internal static class ItemHandler
{
    internal static void Hook()
    {
        DefineObjects();
        RequestBuilder.OnUpdate.Subscribe(0f, AddObjects);
        RequestBuilder.OnUpdate.Subscribe(1f, IncreaseNotchCost);
        RequestBuilder.OnUpdate.Subscribe(2f, RandomizeNotchCosts);
        RequestBuilder.OnUpdate.Subscribe(50f, RemoveLore);
        RequestBuilder.OnUpdate.Subscribe(-180f, DefineTransitions);
        ProgressionInitializer.OnCreateProgressionInitializer += SetOgrimCall;
    }

    private static void SetOgrimCall(LogicManager lm, GenerationSettings gs, ProgressionInitializer pi)
    {
        if (!RandoManager.Settings.Enabled)
            return;

        if (RandoManager.Settings.WhiteDefenderRequirement == WhiteDefenderRequirement.NotRequired)
        {
            pi.Setters.Add(new(lm.GetTermStrict("Ogrim's_Call"), 1));
        }
    }

    private static void DefineObjects()
    {
        // Define charms
        Finder.DefineCustomItem(new PC_CharmItem("Mark_of_Purity", 0));
        Finder.DefineCustomItem(new PC_CharmItem("Vessels_Lament", 1));
        Finder.DefineCustomItem(new PC_CharmItem("Boon_of_Hallownest", 2));
        Finder.DefineCustomItem(new PC_CharmItem("Abyssal_Bloom", 3));
        Finder.DefineCustomItem(new KingsHonourItem());
        Finder.DefineCustomLocation(new BossCharmLocation("Boon_of_Hallownest", OWArenaFinder.PrevHegScene, "hegemol"));
        Finder.DefineCustomLocation(new BossCharmLocation("Kings_Honour", OWArenaFinder.PrevIsmScene, "isma", 0.7f, -0.5f));
        Finder.DefineCustomLocation(new BossCharmLocation("Mark_of_Purity", OWArenaFinder.PrevDryScene, "dryya", 0.1f, 0.7f));
        Finder.DefineCustomLocation(new BossCharmLocation("Vessels_Lament", OWArenaFinder.PrevZemScene, "zemer"));
        Finder.DefineCustomLocation(new AbyssalBloomLocation());
        Finder.DefineCustomLocation(new ChampionsCallLocation(false));
        Finder.DefineCustomLocation(new ChampionsCallLocation(true));

        // Define Abyss totems
        Container.DefineContainer<AbyssTotemContainer>();
        Finder.DefineCustomItem(new AbyssTotemItem());
        Vector2[] worldPos = [new(106.6582f, 27.4f), new(76.4f, 86.6f), new(198.4582f, 114.6f), new(227.6582f, 107.6f)];
        foreach (var pos in worldPos)
        {
            Finder.DefineCustomLocation(new AbyssTotemLocation(worldPos.IndexOf(pos) + 1, pos.X, pos.Y, -1.0f + 0.4f * worldPos.IndexOf(pos), 1.0f));
        }

        // Define Ogrim's Call (used to unlock bosses)
        Finder.DefineCustomItem(new OgrimCallItem());
    }

    private static void RandomizeNotchCosts(RequestBuilder rb)
    {
        if (!RandoManager.Settings.Enabled || !rb.gs.MiscSettings.RandomizeNotchCosts)
            return;
        
        // The idea is that the notch costs for PC charms should be somewhere close to standard charms when rando'd.
        // High or low mostly depends on defined settings.
        FiveKnights.Instance.SaveSettings.notchCosts.Clear();
        int total = rb.ctx.notchCosts.Sum();
        int minTotal = rb.gs.MiscSettings.MinRandomNotchTotal;
        int maxTotal = rb.gs.MiscSettings.MaxRandomNotchTotal;
        int variance = maxTotal - minTotal;
        FiveKnights.Instance.SaveSettings.notchCosts.AddRange(RandomizeCharmCost(rb.rng, total, variance));
    }

    public static int[] RandomizeCharmCost(Random rng, int total, int variance)
    {
        int count = rng.Next(Math.Max(0, (total - variance) / 10), Math.Min(25, (total + variance) / 10));
        int[] costs = new int[4];

        for (int i = 0; i < count; i++)
        {
            int index;
            do
            {
                index = rng.Next(4);
            }
            while (costs[index] >= 6);

            costs[index]++;
        }

        return costs;
    }

    private static void IncreaseNotchCost(RequestBuilder rb)
    {
        if (!RandoManager.Settings.Enabled)
            return;

        // Vanilla total notch cost is 90. The four new charms add a total of 14 to the total vanilla cost.
        // The cost increase should be proportional to what's defined on the user's settings.
        // If the connection's disabled, PC charms will have vanilla costs.
        rb.gs.MiscSettings.MinRandomNotchTotal = (int)(rb.gs.MiscSettings.MinRandomNotchTotal * 104.0 / 90.0);
        rb.gs.MiscSettings.MaxRandomNotchTotal = (int)(rb.gs.MiscSettings.MaxRandomNotchTotal * 104.0 / 90.0);
    }

    private static void DefineTransitions(RequestBuilder rb)
    {
        if (!RandoManager.Settings.Enabled)
            return;

        Assembly assembly = Assembly.GetExecutingAssembly();
        JsonSerializer jsonSerializer = new() {TypeNameHandling = TypeNameHandling.Auto};
        using Stream stream = assembly.GetManifestResourceStream("FiveKnights.Rando.Resources.Data.Transitions.json");
        StreamReader reader = new(stream);
        List<TransitionDef> list = jsonSerializer.Deserialize<List<TransitionDef>>(new JsonTextReader(reader));

        int group = 1;
        foreach (TransitionDef def in list)
        {
            bool shouldBeIncluded = def.IsMapAreaTransition && (rb.gs.TransitionSettings.Mode >= TransitionSettings.TransitionMode.MapAreaRandomizer);
            shouldBeIncluded |= def.IsTitledAreaTransition && (rb.gs.TransitionSettings.Mode >= TransitionSettings.TransitionMode.FullAreaRandomizer);
            shouldBeIncluded |= rb.gs.TransitionSettings.Mode >= TransitionSettings.TransitionMode.RoomRandomizer;
            if (shouldBeIncluded)
            {
                rb.EditTransitionRequest($"{def.SceneName}[{def.DoorName}]", info => info.getTransitionDef = () => def);
                bool uncoupled = rb.gs.TransitionSettings.TransitionMatching == TransitionSettings.TransitionMatchingSetting.NonmatchingDirections;
                if (uncoupled)
                {
                    SelfDualTransitionGroupBuilder tgb = rb.EnumerateTransitionGroups().First(x => x.label == RBConsts.TwoWayGroup) as SelfDualTransitionGroupBuilder;
                    tgb.Transitions.Add($"{def.SceneName}[{def.DoorName}]");
                }
                else
                {
                    SymmetricTransitionGroupBuilder stgb = rb.EnumerateTransitionGroups().First(x => x.label == RBConsts.InTopOutBotGroup) as SymmetricTransitionGroupBuilder;
                    if (group == 1)
                        stgb.Group1.Add($"{def.SceneName}[{def.DoorName}]");
                    else
                        stgb.Group2.Add($"{def.SceneName}[{def.DoorName}]");
                }
                group = group == 1 ? 2 : 1;
            }
            else
            {
                rb.EditTransitionRequest($"{def.SceneName}[{def.DoorName}]", info => info.getTransitionDef = () => def);
                rb.EnsureVanillaSourceTransition($"{def.SceneName}[{def.DoorName}]");
            }
        }
    }

    private static void AddObjects(RequestBuilder rb)
    {
        if (!RandoManager.Settings.Enabled)
            return;

        if (RandoManager.Settings.WhiteDefenderRequirement == WhiteDefenderRequirement.Randomized)
        {
            rb.AddItemByName("Ogrim's_Call");
        }
        if (RandoManager.Settings.BossRewards)
        {
            if (rb.gs.PoolSettings.Charms)
            {
                rb.AddItemByName("Mark_of_Purity");
                rb.EditItemRequest("Mark_of_Purity", info =>
                {
                    info.getItemDef = () => new()
                    {
                        Name = "Mark_of_Purity",
                        Pool = PoolNames.Charm,
                        MajorItem = false,
                        PriceCap = 2000
                    };
                });
                rb.AddItemByName("Vessels_Lament");
                rb.EditItemRequest("Vessels_Lament", info =>
                {
                    info.getItemDef = () => new()
                    {
                        Name = "Vessels_Lament",
                        Pool = PoolNames.Charm,
                        MajorItem = false,
                        PriceCap = 2000
                    };
                });
                rb.AddItemByName("Boon_of_Hallownest");
                rb.EditItemRequest("Boon_of_Hallownest", info =>
                {
                    info.getItemDef = () => new()
                    {
                        Name = "Boon_of_Hallownest",
                        Pool = PoolNames.Charm,
                        MajorItem = false,
                        PriceCap = 2000
                    };
                });
                rb.AddItemByName("Kings_Honour");
                rb.EditItemRequest("Kings_Honour", info =>
                {
                    info.getItemDef = () => new()
                    {
                        Name = "Kings_Honour",
                        Pool = PoolNames.Charm,
                        MajorItem = false,
                        PriceCap = 500
                    };
                });
                rb.AddLocationByName("Mark_of_Purity");
                rb.AddLocationByName("Vessels_Lament");
                rb.AddLocationByName("Boon_of_Hallownest");
                rb.AddLocationByName("Kings_Honour");

                // Add progression between Defender's Crest & King's Honour
                AbstractItem currentDef = Finder.GetItemInternal(ItemNames.Defenders_Crest);
                Finder.GetItemOverride += args =>
                {
                    if (args.ItemName == ItemNames.Defenders_Crest)
                    {
                        args.Current = currentDef;
                        ItemChainTag tag = new()
                        {
                            predecessor = null,
                            successor = "Kings_Honour"
                        };
                        if (args.Current.tags == null)
                        {
                            args.Current.tags = [tag];
                        }
                        else
                        {
                            args.Current.tags.Add(tag);
                        }
                    }
                };
            }
        }
        if (RandoManager.Settings.AbyssalTemple)
        {
            if (rb.gs.PoolSettings.Charms)
            {
                rb.AddItemByName("Abyssal_Bloom");
                rb.EditItemRequest("Abyssal_Bloom", info =>
                {
                    info.getItemDef = () => new()
                    {
                        Name = "Abyssal_Bloom",
                        Pool = PoolNames.Charm,
                        MajorItem = false,
                        PriceCap = 2000
                    };
                });
                rb.AddLocationByName("Abyssal_Bloom");
            }
            if (rb.gs.PoolSettings.SoulTotems)
            {
                rb.AddItemByName("Abyss_Totem", 4);
                rb.EditItemRequest("Abyss_Totem", info =>
                {
                    info.getItemDef = () => new()
                    {
                        Name = "Abyss_Totem",
                        Pool = PoolNames.Soul,
                        MajorItem = false,
                        PriceCap = 1
                    };
                });
                foreach (int x in new int[] { 1, 2, 3, 4 })
                {
                    rb.AddLocationByName($"Soul_Totem-Abyssal_Temple_{x}");
                    rb.EditLocationRequest($"Soul_Totem-Abyssal_Temple_{x}", info =>
                    info.getLocationDef = () => new()
                    {
                        Name = $"Soul_Totem-Abyssal_Temple_{x}",
                        SceneName = SceneNames.Abyss_10,
                        FlexibleCount = false,
                        AdditionalProgressionPenalty = false
                    });
                }
            }
        }
        if (RandoManager.Settings.ChampionCallCompletion >= ChampionsCall.Enabled)
        {
            rb.AddLocationByName("Champion's_Call");
        }
        if (RandoManager.Settings.ChampionCallCompletion == ChampionsCall.Radiant)
        {
            rb.AddLocationByName("Radiant_Champion's_Call");
        }
    }

    private static void RemoveLore(RequestBuilder rb)
    {
        // By default this won't remove those checks - but having LoreRandomizer on with vanilla Pale Court will likely
        // prevent a user from doing standard Pale Court content.
        if (!RandoManager.Settings.Enabled)
            return;

        rb.RemoveLocationByName("Isma_Dream");
        rb.RemoveLocationByName("Dryya_Dream");
    }
}