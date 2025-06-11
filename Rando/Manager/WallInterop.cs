using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BreakableWallRandomizer.IC;
using FiveKnights.Rando;
using ItemChanger;
using Newtonsoft.Json;
using RandomizerCore.Logic;
using RandomizerCore.StringItems;
using RandomizerMod.RC;
using RandomizerMod.Settings;

internal static class WallInterop
{
    internal static void Hook()
    {
        DefineObjects();
        RCData.RuntimeLogicOverride.Subscribe(3f, AddLogic);
        RequestBuilder.OnUpdate.Subscribe(5f, AddObjects);
    }

    private static void DefineObjects()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        JsonSerializer jsonSerializer = new() { TypeNameHandling = TypeNameHandling.Auto };
        using Stream stream = assembly.GetManifestResourceStream("FiveKnights.Rando.Resources.Data.WallObjects.json");
        StreamReader reader = new(stream);
        List<WallObject> wallList = jsonSerializer.Deserialize<List<WallObject>>(new JsonTextReader(reader));

        foreach (WallObject wall in wallList)
        {
            BreakableWallItem wallItem = new(wall.name, wall.sceneName, wall.gameObject, wall.fsmType, wall.persistentBool, wall.sprite, wall.extra, wall.groupWalls);
            BreakableWallLocation wallLocation = new(wall.name, wall.sceneName, wall.gameObject, wall.fsmType, wall.alsoDestroy, wall.x, wall.y, wall.groupWalls, wall.pinType);
            Finder.DefineCustomItem(wallItem);
            Finder.DefineCustomLocation(wallLocation);
        }
    }

    private static void AddLogic(GenerationSettings gs, LogicManagerBuilder lmb)
    {
        if (!RandoManager.Settings.Enabled)
            return;

        if (!lmb.Terms.TermLookup.ContainsKey("Broken_Walls"))
            return;
        
        Assembly assembly = Assembly.GetExecutingAssembly();
        JsonSerializer jsonSerializer = new() { TypeNameHandling = TypeNameHandling.Auto };
        using Stream stream = assembly.GetManifestResourceStream("FiveKnights.Rando.Resources.Data.WallObjects.json");
        StreamReader reader = new(stream);
        List<WallObject> wallList = jsonSerializer.Deserialize<List<WallObject>>(new JsonTextReader(reader));
        
        // Iterate twice - once to define all items, next to add their logic defs.
        foreach (WallObject wall in wallList)
        {
            lmb.GetOrAddTerm(wall.name);
            lmb.AddItem(new StringItemTemplate(wall.name, $"Broken_{wall.name.Split('-')[0]}s++ >> {wall.name}++"));
        }

        foreach (WallObject wall in wallList)
        {
            lmb.AddLogicDef(new(wall.name, wall.logic));

            foreach(var logicOverride in wall.logicOverrides)
            {
                bool exists = lmb.LogicLookup.TryGetValue(logicOverride.Key, out _);
                if (exists)
                    lmb.DoLogicEdit(new(logicOverride.Key, logicOverride.Value));
            }

            foreach (var substitutionDef in wall.logicSubstitutions)
            { 
                foreach (var substitution in substitutionDef.Value)
                {
                    bool exists = lmb.LogicLookup.TryGetValue(substitutionDef.Key, out _);
                    if (exists)
                        lmb.DoSubst(new(substitutionDef.Key, substitution.Key, substitution.Value));  
                }
            }
        }
    }

    private static void AddObjects(RequestBuilder rb)
    {
        if (!RandoManager.Settings.Enabled)
            return;

        if (!rb.ctx.LM.Terms.TermLookup.ContainsKey("Broken_Walls"))
            return;

        Assembly assembly = Assembly.GetExecutingAssembly();
        JsonSerializer jsonSerializer = new() { TypeNameHandling = TypeNameHandling.Auto };
        using Stream stream = assembly.GetManifestResourceStream("FiveKnights.Rando.Resources.Data.WallObjects.json");
        StreamReader reader = new(stream);
        List<WallObject> wallList = jsonSerializer.Deserialize<List<WallObject>>(new JsonTextReader(reader));

        foreach (WallObject wall in wallList)
        {
            bool rockWalls = rb.GetItemGroupFor("Wall-Crossroads_Grub").Items.GetCount("Wall-Crossroads_Grub") > 0;
            bool collapsers = rb.GetItemGroupFor("Collapser-Glowing_Womb_Tunnel").Items.GetCount("Collapser-Glowing_Womb_Tunnel") > 0;
            FiveKnights.FiveKnights.Instance.Log(rockWalls);
            FiveKnights.FiveKnights.Instance.Log(collapsers);
            bool include = wall.name.StartsWith("Wall") && rockWalls;
            include |= wall.name.StartsWith("Collapser") && collapsers;
            FiveKnights.FiveKnights.Instance.Log(include);
            if (include)
            {
                rb.AddItemByName(wall.name);
                rb.EditItemRequest(wall.name, info =>
                {
                    info.getItemDef = () => new()
                    {
                        Name = wall.name,
                        Pool = $"{wall.name.Split('-')[0]}s",
                        MajorItem = false,
                        PriceCap = 500
                    };
                });
                rb.AddLocationByName(wall.name);
                rb.EditLocationRequest(wall.name, info =>
                {
                    info.getLocationDef = () => new()
                    {
                        Name = wall.name,
                        SceneName = wall.sceneName,
                        FlexibleCount = false,
                        AdditionalProgressionPenalty = false
                    };
                });
            }
            else
            {
                rb.AddToVanilla(new(wall.name, wall.name));
            }
        }
    }
}