using BepInEx;
using CWAPI.Challenges;
using CWAPI.Characters;
using CWAPI.Items;
using CWAPI.Utilities;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CWAPI.Saves;

public class ModdedSaveManager
{
    public static void InitiateModdedSaveFiles()
    {
        for (int i = 1; i < 4; i++)
        {
            if (!File.Exists(GetSaveFilePath(i)))
                CreateSave(i);
        }
    }
    public static string GetSaveFilePath(int slotIndex)
    {
        string path = Path.Combine(UnityEngine.Application.persistentDataPath, "Saves");
        path = Path.Combine(path, "Local");

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        return Path.Combine(path, string.Format("slot_{1}_modded.txt", GameStatics.SaveDirectory, (slotIndex == 0) ? GameStatics.SaveSlot : slotIndex));
    }

    public static void CreateSave(int slotIndex = 0)
    {
        string path = GetSaveFilePath(slotIndex);

        File.Create(path);
    }

    public static void ResetSave(int slotIndex = 0)
    {
        File.WriteAllText(GetSaveFilePath(slotIndex), "");
    }

    public static void SaveToFile(ModdedSaveFile moddedSaveFile, int slotIndex = 0, bool UpdateToCurrent = true)
    {
        if (UpdateToCurrent)
            moddedSaveFile.UpdateToCurrent();

        string path = GetSaveFilePath(slotIndex);

        string modsList = "";
        string contents = "";

        // Plugins
        foreach (KeyValuePair<string, PluginInfo> kvp in Plugin.loadedPlugins)
        {
            contents += $"[PLUG | {kvp.Value.Metadata.Version}] {kvp.Key}\n";
        }

        // Items
        foreach (ModdedItem item in ItemUtils.ModdedItems)
        {

            contents += $"[ITEM | {moddedSaveFile.ModdedItemsDictionary.ContainsKey(item.GetType()) && moddedSaveFile.ModdedItemsDictionary[item.GetType()]}] {item.Name}\n";
        }

        // Characters
        foreach (ModdedCharacter character in CharacterUtils.ModdedCharacters)
        {
            contents += $"[CHAR | {character.IsUnlocked}] {character.GetName()}\n";
        }

        // Challenges
        foreach(ModdedChallenge challenge in ChallengeUtils.ModdedChallenges)
        {
            contents += $"[CHAL | {challenge.IsCompleted}] {challenge.ChallengeName}";
        }

        Log.LogInfo("File Contents:\n" + contents);

        File.WriteAllText(path, contents);
    }

    public static void LoadSaveFromFile(int slotIndex = 0, bool newFile = true)
    {
        if (File.ReadAllText(GetSaveFilePath(slotIndex)).Count() == 0)
        {
            ModSaveFile = new ModdedSaveFile();
            SaveToFile(ModSaveFile, slotIndex, true);
        }

        ModSaveFile = ModdedSaveFile.GetSaveFromPath(GetSaveFilePath(slotIndex));
        ModSaveFile.UpdateCurrentToThis();
    }

    public static ModdedSaveFile ModSaveFile;

    // Properly Load Save
    [HarmonyPatch(typeof(SaveManager), "LoadSaveFromFile")]
    public static class LoadFromFile
    {
        public static void Postfix(int slotIndex)
        {
            ModdedSaveManager.LoadSaveFromFile(slotIndex);
        }
    }

    // Properly Save Save
    [HarmonyPatch(typeof(SaveManager), "SaveFileToFile")]
    public static class SaveFileToFile
    {
        public static void Postfix(int slotIndex)
        {
            SaveToFile(ModSaveFile, slotIndex, true);
        }
    }

    // Properly Reset Save
    [HarmonyPatch(typeof(SaveManager), "ResetSave")]
    public static class ResetModdedSave
    {
        public static void Postfix(int slotIndex)
        {
            Log.LogInfo("Resetting Modded Save: " + slotIndex);
            ResetSave(slotIndex);
        }
    }

    //Don't allow resuming if mods aren't the same
    [HarmonyPatch(typeof(MainMenuOptionsController), "Start")]
    public static class DontResumeOnMismatchedMods
    {
        public static void Postfix(ref GameObject ____continueGO)
        {
            if (____continueGO.activeSelf && !ModSaveFile.canResumeGame)
            {
                Button continueButton = ____continueGO.GetComponentInChildren<Button>();

                continueButton.enabled = false;

                foreach(Image image in ____continueGO.GetComponentsInChildren<Image>())
                {
                    image.color = new Color(image.color.r * 0.5f, image.color.g * 0.5f, image.color.b * 0.5f, image.color.a);
                }    
            }
        }
    }
}

