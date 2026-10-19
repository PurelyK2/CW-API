global using static CWAPI.Plugin;

using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using CWAPI.Utilities;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace CWAPI;

// Allow For Custom Tile Types (Classes?)
// Allow For Custom Glyphs
// Custom Characters
// Custom Quests
// Settings Menu Adjustments (For Modded Settings)


[BepInPlugin("Purely_K2.CursedAPI", "Cursed Words API", "0.0.1")]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance;
    public static Dictionary<string, PluginInfo> loadedPlugins = new Dictionary<string, PluginInfo>();
    public static ManualLogSource Log => Instance.Logger;

    void Awake()
    {
        Instance = this;

        Log.LogInfo("Starting API");

        var harmony = new Harmony("Purely_K2.CursedAPI");

        try
        {
            harmony.PatchAll();

            //Assign List Of Plugins
            loadedPlugins = Chainloader.PluginInfos;

            Log.LogWarning($"Currently Loading {loadedPlugins.Count} Mod{(loadedPlugins.Count == 1 ? "" : "s")}:");
            foreach (PluginInfo value in loadedPlugins.Values)
            {
                Log.LogWarning(value.Metadata.Name);
            }
        }
        catch (System.Exception e)
        {
            Log.LogError(e);
        }

        Console.onOrOffMethod = typeof(ConsoleController).GetMethod(
            "OnOrOff",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        );

        Console.tryExecuteCommandMethod = typeof(ConsoleController).GetMethod(
            "TryExecuteCommand",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        Console.inputFieldField = typeof(ConsoleController).GetField(
            "_inputField",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        Console.Instance = new Console();
    }
    void Start()
    {
        ItemUtils.PopulateModdedItems();
        TileUtils.PopulateModdedTileTypes();
    }
    void Update()
    {
        Console.Instance.UpdateConsole();
    }
}