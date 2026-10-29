using CWAPI.Items;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CWAPI.Utilities;

// Make Modded Save Data

public static class ItemUtils
{
    public static List<Item> FullItemsList = new List<Item>();
    public static List<ModdedItem> ModdedItems = new List<ModdedItem>();

    public static void PopulateModdedItems()
    {

        ModdedItems = new List<ModdedItem>();

        foreach (Assembly pluginAssembly in Plugin.loadedPlugins.Values.Select(info => info.Instance.GetType().Assembly))
        {
            List<System.Type> itemTypes = pluginAssembly.GetTypes().Where(t => t.IsSubclassOf(typeof(ModdedItem))).ToList();

            foreach (System.Type itemType in itemTypes)
            {
                if (!ModdedItems.Exists(item => item.GetType() == itemType))
                    ModdedItems.Add((ModdedItem)System.Activator.CreateInstance(itemType));
            }
        }

        if (ModdedItems.Count > 0)
            Log.LogInfo("Modded Items Found: " + string.Join("\n", ModdedItems.Select(item => item.Name + item)));
        else
            Log.LogWarning("No Modded Items Found");

        FullItemsList = new List<Item>(ModdedItems);
        FullItemsList.AddRange(Assembly.GetAssembly(typeof(Item)).GetTypes().Where(t => t.IsClass && t.IsSubclassOf(typeof(Item))).Select(item => (Item)System.Activator.CreateInstance(item)));
    }

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

    [HarmonyPatch(typeof(CollectionCanvasController), "OnDropdownValueChangedCallback")]
    public static class OnlyShowModdedItems
    {
        public static void Postfix(ref CollectionCanvasController __instance, ref List<Item> ___AlphabeticalItems, TMP_Dropdown change, ref bool ___OnlyShowNewItems, ref bool ___OnlyShowUnlockedItems, ref bool ___OnlyShowUnlockableItems, ref int ____pageNumber, ref List<Item> ____itemsToDisplay, ref int ____totalPages)
        {
            try
            {
                if (change.value == 4)
                {
                    Log.LogInfo(change.value);

                    ___OnlyShowNewItems = false;
                    ___OnlyShowUnlockableItems = false;
                    ___OnlyShowUnlockedItems = false;

                    ____pageNumber = 1;
                    GameStatics.LastCollectionPageViewed = 1;
                    ____itemsToDisplay = ___AlphabeticalItems.Where(item => item is ModdedItem).ToList();
                    ____totalPages = (int)Mathf.Ceil((float)____itemsToDisplay.Count / 8f);

                    __instance.Populate();
                }
            }
            catch (System.Exception e)
            {
                Log.LogFatal(e);
            }
        }
    }
    [HarmonyPatch(typeof(SaveManager), "IsItemUnlocked", [typeof(Type)])]
    public static class ProperlyLockItemViz
    {
        public static void Postfix(Type itemType, ref bool __result)
        {
            if(itemType.IsSubclassOf(typeof(ModdedItem)))
            {
                ModdedItem item = ItemUtils.ModdedItems.FirstOrDefault(i => i.GetType() == itemType);

                if (item == null) return;

                __result = item.IsUnlocked;
            }
        }
    }

    /// Adding an item automatically adds it to the pools
    [HarmonyPatch(typeof(ItemPools), "AddItemsToPools", new System.Type[] { typeof(List<System.Type>) })]
    public static class AddItemsToPools_Patch
    {
        public static void Prefix(ref List<System.Type> itemTypes, ref List<System.Type> ____unlockedItemsTypes, ref List<System.Type> ____lockedItems)
        {
            foreach (ModdedItem item in ModdedItems)
            {
                try
                {
                    Sprite sprite = item.GetCurrentSprite();
                }
                catch (System.Exception e)
                {
                    Log.LogError(item.Name + ":");
                    Log.LogError(e);
                }

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
                    if (ModdedItems.Any(item => item.Name.Replace(" ", "-") == argument))
                    {
                        type = ModdedItems.FirstOrDefault(item => item.Name.Replace(" ", "-") == argument).GetType();

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
                }
            }
        }
    }

    //Sprites
}