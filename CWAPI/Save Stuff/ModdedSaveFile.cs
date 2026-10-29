using BepInEx;
using BepInEx.Bootstrap;
using CWAPI.Challenges;
using CWAPI.Characters;
using CWAPI.Items;
using CWAPI.Utilities;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CWAPI.Saves;

public class ModdedSaveFile
{
    public bool canResumeGame = true;

    public static ModdedSaveFile GetSaveFromPath(string path)
    {
        ModdedSaveFile save = new ModdedSaveFile();

        List<(string, string)> pluginsInSave = new List<(string, string)>();

        foreach(string line in File.ReadLines(path))
        {
            string thisDataType = line.Substring(1, 4);
            string dataModifier = line.Substring(line.IndexOf('|') + 2, line.IndexOf(']') - (line.IndexOf('|') + 2));
            string dataName = line.Substring(line.IndexOf(']') + 2);
            Log.LogInfo(thisDataType + '|' + dataModifier + '|' + dataName);

            switch(thisDataType)
            {
                case "PLUG":
                    pluginsInSave.Add(new(dataName, dataModifier));
                    break;
                case "ITEM":
                    ModdedItem item = ItemUtils.ModdedItems.FirstOrDefault(i => i.Name == dataName);
                    bool shouldUnlockItem;

                    if (item != null && bool.TryParse(dataModifier, out shouldUnlockItem))
                    {
                        Log.LogInfo(item.Name + " isUnlocked? " + shouldUnlockItem);

                        if (save.ModdedItemsDictionary.ContainsKey(item.GetType()))
                            save.ModdedItemsDictionary[item.GetType()] = shouldUnlockItem;
                        else
                            save.ModdedItemsDictionary.Add(item.GetType(), shouldUnlockItem);
                    }
                    else if (item != null)
                    {
                        Log.LogError("Error: Item Formatted Impoperly - " + line);
                    }
                    break;
                case "CHAR":
                    ModdedCharacter character = CharacterUtils.ModdedCharacters.FirstOrDefault(chara => chara.GetName() == dataName);
                    bool shouldUnlockChar;

                    if (character != null && bool.TryParse(dataModifier, out shouldUnlockChar))
                    {
                        Log.LogInfo(character.GetName() + " isUnlocked? " + shouldUnlockChar);

                        if (save.ModdedCharactersDictionary.ContainsKey(character.GetType()))
                            save.ModdedCharactersDictionary[character.GetType()] = shouldUnlockChar;
                        else
                            save.ModdedCharactersDictionary.Add(character.GetType(), shouldUnlockChar);
                    }
                    else if (character != null)
                    {
                        Log.LogError("Error: Character Formatted Impoperly - " + line);
                    }
                    break;
                case "CHAL":
                    ModdedChallenge challenge = ChallengeUtils.ModdedChallenges.FirstOrDefault(chal => chal.ChallengeName == dataName);
                    bool shouldUnlockChal;

                    if (challenge != null && bool.TryParse(dataModifier, out shouldUnlockChal))
                    {
                        Log.LogInfo(challenge.ChallengeName + " isUnlocked? " + shouldUnlockChal);

                        if (save.ModdedChallengesDictionary.ContainsKey(challenge.GetType()))
                            save.ModdedChallengesDictionary[challenge.GetType()] = shouldUnlockChal;
                        else
                            save.ModdedCharactersDictionary.Add(challenge.GetType(), shouldUnlockChal);
                    }
                    else if (challenge != null)
                    {
                        Log.LogError("Error: Challenge Formatted Impoperly - " + line);
                    }
                    break;
                default:
                    HandleCustomData(thisDataType, dataModifier, dataName);
                    break;
            }
        }

        //determine if mods are the same
        if (pluginsInSave.Count != Plugin.loadedPlugins.Keys.Count)
        {
            save.canResumeGame = false;
            Log.LogError("Error: Mismatched Mods! Won't Be Able To Resume Run!");
        }
        else
        {
            foreach((string, string) plugin in pluginsInSave)
            {
                if (!Plugin.loadedPlugins.ContainsKey(plugin.Item1))
                {
                    save.canResumeGame = false;
                    Log.LogError("Error: Mismatched Mods! Won't Be Able To Resume Run!");
                    break;
                }
                else if (Plugin.loadedPlugins[plugin.Item1].Metadata.Version.ToString() != plugin.Item2)
                {
                    save.canResumeGame = false;
                    Log.LogError("Error: Mismatched Mods! Won't Be Able To Resume Run!");
                    break;
                }
            }
        }

        return save;
    }

    public static void HandleCustomData(string dataType, string dataModifier, string dataName)
    {
        // Override This With A HarmonyPatch Or Something To Use!
    }

    public ModdedSaveFile(int slotIndex = 0, bool update = true)
    {
        if(update)
            UpdateToCurrent();
    }

    public void UpdateToCurrent()
    {
        foreach (ModdedItem item in ItemUtils.ModdedItems)
        {
            ModdedItemsDictionary[item.GetType()] = item.IsUnlocked;
        }

        foreach (ModdedCharacter character in CharacterUtils.ModdedCharacters)
        {
            ModdedCharactersDictionary[character.GetType()] = character.IsUnlocked;
        }
    }

    public void UpdateCurrentToThis()
    {
        foreach (ModdedItem item in ItemUtils.ModdedItems)
        {
            item.isUnlocked = ModdedItemsDictionary[item.GetType()];
            Log.LogDebug(item.Name + " Is Unlocked: " + item.IsUnlocked);
        }

        foreach (ModdedCharacter character in CharacterUtils.ModdedCharacters)
        {
            character.isUnlocked = ModdedCharactersDictionary[character.GetType()];
            Log.LogDebug(character.GetName() + " Is Unlocked: " + character.IsUnlocked);
        }

        foreach (ModdedChallenge challenge in ChallengeUtils.ModdedChallenges)
        {
            challenge.IsCompleted = ModdedCharactersDictionary[challenge.GetType()];
            Log.LogDebug(challenge.ChallengeName + " Is Completed: " + challenge.IsCompleted);
        }
    }

    public bool UsingSameMods = true;
    public Dictionary<Type, bool> ModdedItemsDictionary = new Dictionary<Type, bool>();
    public Dictionary<Type, bool> ModdedCharactersDictionary = new Dictionary<Type, bool>();
    public Dictionary<Type, bool> ModdedChallengesDictionary = new Dictionary<Type, bool>();
}
