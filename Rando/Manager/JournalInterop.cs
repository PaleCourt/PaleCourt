using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FiveKnights.Rando;
using ItemChanger;
using ItemChanger.UIDefs;
using Newtonsoft.Json;
using RandomizerCore.Logic;
using RandomizerCore.StringItems;
using RandomizerMod.RC;
using RandomizerMod.Settings;
using TheRealJournalRando;
using TheRealJournalRando.Data;
using TheRealJournalRando.IC;

namespace FiveKnights.Rando;

internal static class JournalInterop
{
    internal static void Hook()
    {
        DefineObjects();
        RCData.RuntimeLogicOverride.Subscribe(5f, AddLogic);
        RequestBuilder.OnUpdate.Subscribe(11f, AddObjects);
    }

    private static void DefineObjects()
    {
        // Step 1: Read journal data
        Assembly assembly = Assembly.GetExecutingAssembly();
        JsonSerializer jsonSerializer = new() {TypeNameHandling = TypeNameHandling.Auto};
        using Stream stream = assembly.GetManifestResourceStream("FiveKnights.Rando.Resources.Data.EnemyDefs.json");
        StreamReader reader = new(stream);
        List<EnemyDef> data = jsonSerializer.Deserialize<List<EnemyDef>>(new JsonTextReader(reader)) ?? throw new IOException("Failed to load enemy definitions");
        FiveKnights.Instance.Log(data.Count());
        // Step 2: Define items and locations
        foreach (EnemyDef enemy in data)
        {
            FiveKnights.Instance.Log(enemy.pdName);
            Finder.DefineCustomItem(new EnemyJournalEntryOnlyItem(enemy.pdName)
            {
                name = enemy.icName.AsEntryName(),
                UIDef = new MsgUIDef
                {
                    name = new FormatString(new LanguageString("Fmt", "ENTRY_ITEM_NAME"), $"JOURNAL_{enemy.convoName}".Clone()),
                    shopDesc = new FormatString(new LanguageString("Fmt", "ENTRY_ITEM_DESC"), $"JOURNAL_{enemy.convoName}".Clone()),
                    sprite = new PC_Sprite(enemy.icName)
                },
                tags = [InteropTagFactory.CmiSharedTag(poolGroup: "Journal Entries")]
            });
            Finder.DefineCustomItem(new EnemyJournalNotesOnlyItem(enemy.pdName)
            {
                name = enemy.icName.AsNotesName(),
                UIDef = new MsgUIDef
                {
                    name = new FormatString(new LanguageString("Fmt", "NOTES_ITEM_NAME"), $"JOURNAL_{enemy.convoName}".Clone()),
                    shopDesc = new FormatString(new LanguageString("Fmt", "NOTES_ITEM_DESC"), $"JOURNAL_{enemy.convoName}".Clone()),
                    sprite = new PC_Sprite(enemy.icName)
                },
                tags = [InteropTagFactory.CmiSharedTag(poolGroup: "Journal Entries")]
            });
            Finder.DefineCustomLocation(new EnemyJournalLocation(enemy.pdName, EnemyJournalLocationType.Entry)
            {
                name = enemy.icName.AsEntryName(),
                sceneName = enemy.singleSceneName,
                flingType = FlingType.Everywhere,
                tags =
                [
                    InteropTagFactory.CmiLocationTag(
                        poolGroup: "Journal Entries",
                        pinSprite: new PC_Sprite(enemy.icName),
                        sceneNames: enemy.allScenes,
                        titledAreas: enemy.allTitledAreas,
                        mapAreas: enemy.allMapAreas,
                        highlightScenes: enemy.allScenes?.ToArray(),
                        pinSort: enemy.index * 2,
                        mapLocations: default
                    ),
                    InteropTagFactory.RecentItemsLocationTag(sourceOverride: "the Hunter")
                ]
            });
            Finder.DefineCustomLocation(new EnemyJournalLocation(enemy.pdName, EnemyJournalLocationType.Notes)
            {
                name = enemy.icName.AsNotesName(),
                sceneName = enemy.singleSceneName,
                flingType = FlingType.Everywhere,
                tags =
                [
                    InteropTagFactory.CmiLocationTag(
                        poolGroup: "Journal Entries",
                        pinSprite: new PC_Sprite(enemy.icName),
                        sceneNames: enemy.allScenes,
                        titledAreas: enemy.allTitledAreas,
                        mapAreas: enemy.allMapAreas,
                        highlightScenes: enemy.allScenes?.ToArray(),
                        pinSort: enemy.index * 2,
                        mapLocations: default
                    ),
                    InteropTagFactory.RecentItemsLocationTag(sourceOverride: "the Hunter")
                ]
            });
        }
    }

    private static void AddLogic(GenerationSettings gs, LogicManagerBuilder lmb)
    {
        if (!RandoManager.Settings.Enabled)
            return;
        
        lmb.AddItem(new StringItemTemplate("Journal_Entry-Isma", "_"));
        lmb.AddItem(new StringItemTemplate("Hunter's_Notes-Isma", "_"));
        lmb.AddLogicDef(new ("Journal_Entry-Isma", "ANY"));
        lmb.AddLogicDef(new ("Hunter's_Notes-Isma", "ANY"));
    }

    private static void AddObjects(RequestBuilder rb)
    {
        if (!RandoManager.Settings.Enabled)
            return;

        rb.AddItemByName("Journal_Entry-Isma");
        rb.AddItemByName("Hunter's_Notes-Isma");
        rb.AddLocationByName("Journal_Entry-Isma");
        rb.AddLocationByName("Hunter's_Notes-Isma");
    }
}