using CWAPI.Items;
using CWAPI.Tiles;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CWAPI.Utilities;

public static class TileUtils
{
    public static List<ModdedTileType> ModdedTileTypes = new List<ModdedTileType>();
    public static GameObject DefaultTileObject;

    public static void PopulateModdedTileTypes()
    {

        ModdedTileTypes = new List<ModdedTileType>();

        int i = 12;
        foreach (Assembly pluginAssembly in Plugin.loadedPlugins.Values.Select(info => info.Instance.GetType().Assembly))
        {
            List<System.Type> tileTypes = pluginAssembly.GetTypes().Where(t => t.IsSubclassOf(typeof(ModdedTileType))).ToList();

            foreach (System.Type tileType in tileTypes)
            {
                if (!ModdedTileTypes.Exists(tile => tile.GetType() == tileType))
                {
                    ModdedTileType thisTile = (ModdedTileType)System.Activator.CreateInstance(tileType);
                    thisTile.tileType = (TileType)i++;

                    ModdedTileTypes.Add(thisTile);
                }
            }
        }

        if (ModdedTileTypes.Count > 0)
            Log.LogInfo("Modded Tiles Found: " + string.Join("\n", ModdedTileTypes.Select(tile => tile.TileName + tile.tileType)));
        else
            Log.LogWarning("No Modded Items Found");
    }
    
    public static TileType GetTileType<T>() where T : ModdedTileType
    {
        return ModdedTileTypes.FirstOrDefault(t => t.GetType() == typeof(T))?.tileType ?? TileType.Normal;
    }
}
