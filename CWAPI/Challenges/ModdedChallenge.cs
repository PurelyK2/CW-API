using CWAPI.Items;
using CWAPI.Saves;
using CWAPI.Utilities;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CWAPI.Challenges;

public abstract class ModdedChallenge : ChallengeRun
{
    public abstract Type ChallengeUnlock { get; }
    public bool IsCompleted = false;

    [HarmonyPatch(typeof(ChallengesCanvasController), "Start")]
    public static class PopulateModdedChallenges
    {
        public static void Prefix(ref RectTransform ____contentRT)
        {
            foreach(ModdedChallenge challenge in ChallengeUtils.ModdedChallenges)
            {
                if (ChallengeRuns.ItemUnlocks.ContainsKey(challenge.GetType())) continue;

                ChallengeRuns.ItemUnlocks.Add(challenge.GetType(), challenge.ChallengeUnlock);
            }

            int rowCount = (int)Mathf.Ceil((float)ChallengeRuns.ItemUnlocks.Keys.Count / 7f);

            if (rowCount < 6) return;

            GridLayoutGroup gridLayout = ____contentRT.GetComponent<GridLayoutGroup>();
            gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;
            gridLayout.cellSize *= 5.5f / (float)rowCount;
        }
    }

    [HarmonyPatch(typeof(SaveManager), "SetChallengeComplete", [typeof(Type)])]
    public static class UnlockChallengeItem
    {
        public static bool Prefix(Type challengeType)
        {
            if(challengeType.IsSubclassOf(typeof(ModdedChallenge)))
            {
                ModdedChallenge challenge = ChallengeUtils.ModdedChallenges.FirstOrDefault(chal => chal.GetType() == challengeType);

                if (challenge == null)
                {
                    Log.LogError("Couldn't Find Challenge You Just Completed!");
                    return false;
                }

                challenge.UnlockItem();
                ModdedSaveManager.SaveToFile(ModdedSaveManager.ModSaveFile, GameStatics.SaveSlot, true);
                return false;
            }
            return true;
        }
    }

    public void UnlockItem()
    {
        ModdedItem item = ItemUtils.ModdedItems.FirstOrDefault(i => i.GetType() == ChallengeUnlock);

        if(item == null)
        {
            Log.LogError("Error: Couldn't Find Challenge Item: " + ChallengeUnlock);
        }

        item.isUnlocked = true;
    }

    // Completed Challenge Checks
    [HarmonyPatch(typeof(SaveManager), "IsChallengeComplete", [typeof(Type)])]
    public static class CheckChallengeTypeComplete
    {
        public static void Postfix(Type challengeType, ref bool __result)
        {
            if (!challengeType.IsSubclassOf(typeof(ModdedChallenge))) return;

            ModdedChallenge challenge = ChallengeUtils.ModdedChallenges.FirstOrDefault(chal => chal.GetType() == challengeType);

            if (challenge != null)
            {
                __result = challenge.IsCompleted;
            }
        }
    }
    [HarmonyPatch(typeof(SaveManager), "IsChallengeComplete", [typeof(ChallengeRun)])]
    public static class CheckChallengeRunComplete
    {
        public static void Postfix(ChallengeRun challenge, ref bool __result)
        {
            if (challenge is not ModdedChallenge) return;

            ModdedChallenge moddedChallenge = ChallengeUtils.ModdedChallenges.FirstOrDefault(chal => chal.GetType() == challenge.GetType());

            if (moddedChallenge != null)
            {
                __result = moddedChallenge.IsCompleted;
            }
        }
    }
}
