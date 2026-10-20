using CWAPI.Items;
using CWAPI.Utilities;
using FMOD.Studio;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using Color = UnityEngine.Color;

namespace CWAPI.Tiles;

public abstract class ModdedTileType
{
    public abstract string TileName { get; }
    public TileType tileType = 0;
    public abstract Mesh tileMesh { get; }
    public virtual string tileSelectionClip => "Tiles/Select Colour Tile Shiny";
    public virtual bool canRandomSpread => false;
    public virtual bool isXenocolor => false;
    public virtual bool pulses => true;

    public virtual (Color tile, Color word) textColor => (Color.green, Color.green);
    public virtual GameObject tileGameObject => null;

    //Need To happen after boss effects, glitch tiles, currency tiles, and poker hands, and before items
    public virtual ScorePacket ModifyTileValue(ScorePacket tileValue) { return tileValue; }
    public virtual void ApplyTileBonus(ScoreCalcVizInfo step, int index, List<Tile> tiles, List<TileSelection> selections, List<HistoricWord> previousWords, GridData gridData) { }
    public virtual void ApplyWordBonus(ScoreCalcVizInfo step, int gridNumber, List<Tile> tiles, List<string> words, List<TileSelection> selections, List<HistoricWord> previousWords, GridData gridData) { }
    // Below Unimplemented
    public virtual GridData ApplyStartOfGridEffect(GridData gridData, int gridNumber, int numberOfGrids, List<HistoricWord> previousWords, List<BoardGenVizInfo> vizSteps, bool isReroll) { return gridData; }

    // Patch For Base Value
    [HarmonyPatch(typeof(Tile), "GetValue")]
    public static class ModifyBaseValuePatch
    {
        public static void Postfix(ref Tile __instance, ref ScorePacket __result)
        {
            if (__instance.MyTileType <= TileType.Glitch) return;

            TileType tileType = __instance.MyTileType;
            ModdedTileType moddedTile = TileUtils.ModdedTileTypes.FirstOrDefault(t => t.tileType == tileType);

            if (moddedTile == null) return;

            __result = moddedTile.ModifyTileValue(__result);
        }
    }

    // Patch Sound Of Custom Tiles
    [HarmonyPatch(typeof(SoundController), "PlayTileTypeSound", new System.Type[] { typeof(Tile), typeof(float) })]
    public static class PlayModdedTileSound
    {
        public static void Prefix(Tile tile, float pitchDifference, ref Dictionary<TileType, string> ____tileSelectionClips)
        {
            if (____tileSelectionClips.Keys.Count() > 10) return;

            foreach(ModdedTileType tileType in TileUtils.ModdedTileTypes)
            {
                ____tileSelectionClips.Add(tileType.tileType, tileType.tileSelectionClip);
            }
        }
    }

    // Add Tile Type Game Object To List
    [HarmonyPatch(typeof(TileObject), "Populate", new Type[] { typeof(Tile), typeof(bool) })]
    public static class CorrectlyPopulateModdedTile
    {
        static TileType cachedTileType = TileType.Normal;
        public static bool Prefix(ref TileObject __instance, ref Tile tile)
        {
            if (tile == null && __instance != null && __instance.MyTile != null && __instance.MyTile.MyTileType > TileType.Glitch) return false;
            if (tile == null) return true;

            cachedTileType = tile.MyTileType;

            if (cachedTileType > TileType.Glitch)
                tile.MyTileType = TileType.Normal;
            return true;
        }
        [HarmonyPriority(Priority.First)]
        public static void Postfix(ref TileObject __instance, ref GameObject[] ____tileMeshGOs, ref TextMeshPro ____letterTMP, ref TextMeshPro ____scoreTMP, ref Color ____textColor, ref Color ____scoreColor, Tile tile)
        {
            if (tile == null) return;

            tile.MyTileType = cachedTileType;

            tile.MyTileType = (TileType)12; //TESTING!!!

            if (tile.MyTileType <= TileType.Glitch) return;

            Log.LogInfo("Getting Modded Tile Type" + (int)tile.GetTileType());
            GameObject thisTile = null;
            if (TileUtils.ModdedTileTypes.Any(t => t.tileType == tile.GetTileType()))
            {
                if(____tileMeshGOs.Any(GO => GO.name == "ModdedTile"))
                {
                    thisTile = ____tileMeshGOs.FirstOrDefault(GO => GO.name == "ModdedTile");
                    Log.LogInfo("ModdedTile Already Exists Here");
                    goto TILECONT;
                }

                Log.LogInfo("Found Proper Tile Type");
                ModdedTileType thisTileType = TileUtils.ModdedTileTypes.FirstOrDefault(t => t.tileType == tile.MyTileType);

                if ((thisTile = thisTileType.tileGameObject) == null && (thisTile = GameObject.Instantiate(____tileMeshGOs[11], ____tileMeshGOs[11].transform.parent, false)) == null)
                {
                    Log.LogError("Failed To Get Tile Object");
                    return;
                }

                thisTile.name = "ModdedTile";

                try
                {
                    if (thisTileType != null && thisTileType.tileMesh != null)
                        thisTile.GetComponent<MeshFilter>().mesh = thisTileType.tileMesh;
                }
                catch (Exception e)
                {
                    Log.LogFatal(e);
                }
                List<GameObject> tileMeshGOList = ____tileMeshGOs.ToList();
                tileMeshGOList.Add(thisTile);
                ____tileMeshGOs = new GameObject[tileMeshGOList.Count];
                ____tileMeshGOs = tileMeshGOList.ToArray();
            }
            TILECONT:

            if (thisTile == null) return;

            ModdedTileType tileType = TileUtils.ModdedTileTypes.FirstOrDefault(t => t.tileType == tile.MyTileType);

            if(tileType != null)
            {
                ____letterTMP.color = tileType.textColor.tile;
                ____scoreTMP.color = tileType.textColor.tile;
                ____scoreTMP.SetText(__instance.MyTile.GetValueForDisplay());

                ____textColor = tileType.textColor.tile;
                ____scoreColor = tileType.textColor.tile;
            }

            foreach(GameObject tileMeshGO in ____tileMeshGOs)
            {
                tileMeshGO.SetActive(false);
            }

            if(____tileMeshGOs.Any(GO => GO.name == "ModdedTile"))
            {
                ____tileMeshGOs.FirstOrDefault(GO => GO.name == "ModdedTile").SetActive(true);
            }
        }
    }

    // Allow Correctly Finding Tile Type Game Object
    [HarmonyPatch(typeof(WordLetterController), "GetGOFromTileType", new Type[] { typeof(TileType) })]
    public static class GetGOFromModdedTileType
    {
        public static bool Prefix(TileType tileType, ref GameObject __result, ref GameObject[] ____tileColorGOs)
        {
            if (tileType <= TileType.Glitch) return true;

            if (!____tileColorGOs.Any(GO => GO.name == "ModdedTile"))
            {
                GameObject moddedTileColorGO = GameObject.Instantiate(____tileColorGOs[2], ____tileColorGOs[0].transform.parent, false);

                moddedTileColorGO.name = "ModdedTile";

                if (moddedTileColorGO == null) Log.LogError("Error: ModdedTileColorGameObject Failed To Set");

                List<GameObject> tileColorGOList = ____tileColorGOs.ToList();
                tileColorGOList.Add(moddedTileColorGO);
                ____tileColorGOs = new GameObject[tileColorGOList.Count];
                ____tileColorGOs = tileColorGOList.ToArray();
            }

            __result = ____tileColorGOs.FirstOrDefault(GO => GO.name == "ModdedTile");

            ModdedTileType mTileType = TileUtils.ModdedTileTypes.FirstOrDefault(t => t.tileType == tileType);

            if (mTileType != null)
            {
                __result.GetComponentInChildren<TextMeshProUGUI>().color = mTileType.textColor.word;
            }

            return false;
        }
    }

    //===============================SCARY IL ZONE========================
    [HarmonyPatch(typeof(ScoreCalculation), "CalculateOverallScore", new Type[] {typeof(List<TileSelection>), typeof(List<string>), typeof(List<Item>), typeof(List<HistoricWord>), typeof(List<BossModifier>), typeof(GridData), typeof(int)})]
    public static class ApplyModdedTileBonuses
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            ConstructorInfo listConstructor = AccessTools.Constructor(typeof(List<Item>), new Type[] { typeof(List<Item>) });

            MethodInfo hookMethod = AccessTools.Method(typeof(ApplyModdedTileBonuses), nameof(ApplyModdedTileBonuses.PatchAllTiles));

            var matcher = new CodeMatcher(instructions);

            matcher.MatchStartForward(new CodeMatch(OpCodes.Newobj, listConstructor));

            if(matcher.IsValid)
            {
                matcher.Insert(
                    new CodeInstruction(OpCodes.Dup),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldarg_S, 3),
                    new CodeInstruction(OpCodes.Ldarg_S, 5),
                    new CodeInstruction(OpCodes.Ldarg_S, 6),
                    new CodeInstruction(OpCodes.Call, hookMethod)
                );
            }
            else
            {
                Log.LogInfo("Couldn't Find Patch Spot For Tile Apply Score Stuff");
            }

            return matcher.InstructionEnumeration();
        }

        static void PatchAllTiles(List<Item> newList, List<TileSelection> selectedTiles, List<string> words, List<ScoreCalcVizInfo> vizInfo, List<HistoricWord> historicWords, GridData gridData, int gridNumber)
        {
            Log.LogInfo("Patching In Tile Scoring");

            foreach(TileSelection selectedTile in selectedTiles.Where(selTile => selTile.SelectedTile.GetTileType() > TileType.Glitch))
            {
                ScoreCalcVizInfo nextStep = ScoreCalculation.GetNextStep(vizInfo);

                ModdedTileType moddedTile = TileUtils.ModdedTileTypes.FirstOrDefault(t => t.tileType == selectedTile.SelectedTile.MyTileType);
                if (moddedTile != null && moddedTile.pulses)
                    nextStep.LettersOnGridToPulse.Add(selectedTile.SelectedTile);

                moddedTile.ApplyTileBonus(nextStep, selectedTiles.IndexOf(selectedTile), selectedTiles.Select(selTile => selTile.SelectedTile).ToList(), selectedTiles, historicWords, gridData);
                moddedTile.ApplyWordBonus(nextStep, gridNumber, selectedTiles.Select(selTile => selTile.SelectedTile).ToList(), words, selectedTiles, historicWords, gridData);

                vizInfo.Add(nextStep);
            }
        }
    }
}
