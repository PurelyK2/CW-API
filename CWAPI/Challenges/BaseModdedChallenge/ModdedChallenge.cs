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
}
