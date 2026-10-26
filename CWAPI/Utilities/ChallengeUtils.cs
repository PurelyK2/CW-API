using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using CWAPI.Challenges;

namespace CWAPI.Utilities;

public static class ChallengeUtils
{
    public static List<ModdedChallenge> ModdedChallenges = new List<ModdedChallenge>();

    public static void PopulateModdedChallenges()
    {
        ModdedChallenges = new List<ModdedChallenge>();

        foreach (Assembly pluginAssembly in Plugin.loadedPlugins.Values.Select(info => info.Instance.GetType().Assembly))
        {
            List<System.Type> itemTypes = pluginAssembly.GetTypes().Where(t => t.IsSubclassOf(typeof(ModdedChallenge))).ToList();

            foreach (System.Type itemType in itemTypes)
            {
                if (!ModdedChallenges.Exists(item => item.GetType() == itemType))
                    ModdedChallenges.Add((ModdedChallenge)System.Activator.CreateInstance(itemType));
            }
        }

        if (ModdedChallenges.Count > 0)
            Log.LogInfo("Modded Challenges Found: " + string.Join("\n", ModdedChallenges.Select(challenge => challenge.ChallengeName + challenge)));
        else
            Log.LogWarning("No Modded Challenges Found");
    }
}