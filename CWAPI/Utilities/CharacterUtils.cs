using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using CWAPI.Characters;

namespace CWAPI.Utilities;

public static class CharacterUtils
{
    public static List<Character> FullCharactersList = new List<Character>();
    public static List<ModdedCharacter> ModdedCharacters = new List<ModdedCharacter>();

    public static void PopulateModdedCharacters()
    {

        ModdedCharacters = new List<ModdedCharacter>();

        foreach (Assembly pluginAssembly in Plugin.loadedPlugins.Values.Select(info => info.Instance.GetType().Assembly))
        {
            List<System.Type> itemTypes = pluginAssembly.GetTypes().Where(t => t.IsSubclassOf(typeof(ModdedCharacter))).ToList();

            foreach (System.Type itemType in itemTypes)
            {
                if (!ModdedCharacters.Exists(item => item.GetType() == itemType))
                    ModdedCharacters.Add((ModdedCharacter)System.Activator.CreateInstance(itemType));
            }
        }

        if (ModdedCharacters.Count > 0)
            Log.LogInfo("Modded Characters Found: " + string.Join("\n", ModdedCharacters.Select(character => character.GetName() + character)));
        else
            Log.LogWarning("No Modded Characters Found");

        FullCharactersList = new List<Character>(ModdedCharacters);
        FullCharactersList.AddRange(Character.GetAllCharacters().Select(character => (Character)System.Activator.CreateInstance(character)));
    }
}