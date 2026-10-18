using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;

namespace CWAPI.Utilities;

public static class ItemUtils
{
    public static List<Item> ModdedItems = new List<Item>();

    /// Adding an item automatically adds it to the collection
    [HarmonyPatch(typeof(CollectionCanvasController), "Start")]
    public static class AddItemsToColleciton_Patch
    {
        public static void Postfix(ref CollectionCanvasController __instance, ref List<Item> ___AlphabeticalItems)
        {
            TMP_Dropdown dropdown = __instance.gameObject.GetComponentInChildren<TMP_Dropdown>();
            dropdown.AddOptions(new List<string> { "Modded" });

            ___AlphabeticalItems.AddRange(ModdedItems);
            ___AlphabeticalItems = ___AlphabeticalItems.OrderBy(item => item.Name).ToList();
        }
    }

    /// Adding an item automatically adds it to the pools
    [HarmonyPatch(typeof(ItemPools), "AddItemsToPools", new System.Type[] { typeof(List<System.Type>) })]
    public static class AddItemsToPools_Patch
    {
        public static void Prefix(ref List<System.Type> itemTypes, ref List<System.Type> ____unlockedItemsTypes, ref List<System.Type> ____lockedItems)
        {
            PopulateModdedItems();

            foreach (Item item in ModdedItems)
            {
                if (GameStatics.IsUnlockRequired(item.GetType()))
                {
                    ____lockedItems.Add(item.GetType());
                }
                else
                {
                    ____unlockedItemsTypes.Add(item.GetType());
                }
            }

            itemTypes.AddRange(ModdedItems.Select(item => item.GetType()));
        }

        public static void PopulateModdedItems()
        {

            ModdedItems = new List<Item>();

            foreach (Assembly pluginAssembly in Plugin.loadedPlugins.Values.Select(info => info.Instance.GetType().Assembly))
            {
                List<System.Type> itemTypes = pluginAssembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Item))).ToList();
                if (itemTypes.Count == 0)
                    Log.LogWarning("No Items Found In Assembly: " + Plugin.loadedPlugins.Values.FirstOrDefault(info => info.Instance.GetType().Assembly == pluginAssembly)?.Metadata.Name);

                foreach (System.Type itemType in itemTypes)
                {
                    if (!ModdedItems.Exists(item => item.GetType() == itemType))
                        ModdedItems.Add((Item)System.Activator.CreateInstance(itemType));
                }
            }
        }
    }

    /// Adding an item automatically allows it to be gotten by the console "gi" command
    [HarmonyPatch(typeof(ConsoleController), "GiveItem", new System.Type[] { typeof(string[]) })]
    public static class AddItemsToConsolePool_Patch
    {
        public static void Prefix(ref string[] args)
        {
            if (args.Length != 1)
            {
                System.Type type = System.Type.GetType(args[1]);
                if (type == null)
                {
                    Log.LogDebug("Console Error: Type Is Null. Testing With Extension:");

                    string argument = args[1];
                    if (ModdedItems.Exists(item => item.GetType().ToString().Substring(item.GetType().ToString().ToList().IndexOf('.') + 1) == argument))
                    {
                        type = ModdedItems.FirstOrDefault(item => item.GetType().ToString().Substring(item.GetType().ToString().ToList().IndexOf('.') + 1) == argument).GetType();

                        if (type != null && type.IsSubclassOf(typeof(Item)))
                        {
                            Item item2 = System.Activator.CreateInstance(type) as Item;
                            if (args.Length >= 3 && (args[2] == "-f" || args[2] == "-foil"))
                            {
                                item2.IsFoil = true;
                            }
                            GameStatics.GetPlayer().AddItemToInventory(item2);
                            CharacterInfoPanel.SingletonInventoryVisualController.PopulateAll();
                            Log.LogDebug("Gave Item: " + item2.Name);
                        }
                    }
                    else
                        Log.LogError("Item Not Found Even In Modded Items");

                    return;
                }
            }
        }
    }
}
