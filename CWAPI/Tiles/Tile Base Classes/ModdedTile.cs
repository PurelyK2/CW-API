using CWAPI.Items;
using CWAPI.Utilities;
using FMOD.Studio;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using OutlineFx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using Color = UnityEngine.Color;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace CWAPI.Tiles;

// Let Items Spawn Tiles
// Get Meshes To Swap Out Right

public abstract class ModdedTileType
{
    private GameObject privateTileMesh;
    public virtual GameObject tileMesh { get => privateTileMesh; set => privateTileMesh = value; }
    public abstract string TileName { get; }
    public TileType tileType = 0;
    public virtual string tileSelectionClip => "Tiles/Select Colour Tile Shiny";
    public virtual bool canRandomSpread => false;
    public virtual bool isXenocolor => false;
    public virtual bool pulses => true;

    public virtual (Color tile, Color word) textColor => (Color.green, Color.green);

    //Need To happen after boss effects, glitch tiles, currency tiles, and poker hands, and before items
    public virtual ScorePacket ModifyTileValue(ScorePacket tileValue) { return tileValue; }
    public virtual void ApplyTileBonus(ScoreCalcVizInfo step, int index, List<Tile> tiles, List<TileSelection> selections, List<HistoricWord> previousWords, GridData gridData) { }
    public virtual void ApplyWordBonus(ScoreCalcVizInfo step, int gridNumber, List<Tile> tiles, List<string> words, List<TileSelection> selections, List<HistoricWord> previousWords, GridData gridData) { }
    public virtual void ApplyTileSpawnEffect(Tile tile, GridData gridData, int gridNumber, int numberOfGrids, List<HistoricWord> previousWords, List<BoardGenVizInfo> vizSteps, bool isReroll) { }

    // Patch For Start Of Grid Stuff
    static List<Tile> moddedTiles = new List<Tile>();
    [HarmonyPatch(typeof(GridUtilitySingleton), "GenerateGrid", new System.Type[] {
        typeof(int), // width
        typeof(int), // height
        typeof(int), // gridNumber
        typeof(int), // numberOfGrids
        typeof(List<HistoricWord>), // previousWords
        typeof(List<BossModifier>), // bossModifiers
        typeof(List<BoardGenVizInfo>), // out vizSteps (Note the MakeByRefType)
        typeof(bool), // isReroll
        typeof(List<Tile>) // staticTiles
    }, new ArgumentType[] {
            ArgumentType.Normal, // width
            ArgumentType.Normal, // height
            ArgumentType.Normal, // gridNumber
            ArgumentType.Normal, // numberOfGrids
            ArgumentType.Normal, // previousWords
            ArgumentType.Normal, // bossModifiers
            ArgumentType.Out,    // vizSteps (Tells Harmony this one is an 'out' param)
            ArgumentType.Normal, // isReroll
            ArgumentType.Normal  // staticTiles
    })]
    public static class OrganizeAndDoTileStuff
    {
        public static void Prefix()
        {
            moddedTiles = new List<Tile>();
        }
        public static void Postfix(ref GridData __result, int gridNumber, int numberOfGrids, List<HistoricWord> previousWords, List<BoardGenVizInfo> vizSteps, bool isReroll)
        {
            foreach(Tile tile in vizSteps[vizSteps.Count - 1].Grid.GridTiles)
            {
                if (tile.MyTileType <= TileType.Glitch) continue;

                ModdedTileType moddedTile = TileUtils.ModdedTileTypes.FirstOrDefault(t => t.tileType == tile.MyTileType);

                moddedTile.ApplyTileSpawnEffect(tile, vizSteps[vizSteps.Count - 1].Grid, gridNumber, numberOfGrids, previousWords, vizSteps, isReroll);
            }
        }
    }

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
        static Dictionary<TileObject, GameObject> normalGlitchMeshes = new Dictionary<TileObject, GameObject>();
        static TileType currentTileType;
        public static bool Prefix(Tile tile, ref TileObject __instance, ref GameObject[] ____textParentGOs, ref GameObject[] ____tileMeshGOs)
        {
            if (!normalGlitchMeshes.ContainsKey(__instance))
            {
                normalGlitchMeshes[__instance] = GameObject.Instantiate(____tileMeshGOs[11], ____tileMeshGOs[11].transform.parent, false);
                normalGlitchMeshes[__instance].SetActive(false);
            }

            ____tileMeshGOs[11].SetActive(false);
            ____tileMeshGOs[11] = GameObject.Instantiate(normalGlitchMeshes[__instance], normalGlitchMeshes[__instance].transform.parent, false);
            ____tileMeshGOs[11].SetActive(false);

            if (tile != null)
            {
                currentTileType = tile.MyTileType;
                if (tile.MyTileType > TileType.Glitch)
                    tile.MyTileType = TileType.Glitch;
            }
            else 
            {
                currentTileType = __instance.MyTile.MyTileType;
                if (__instance.MyTile?.MyTileType > TileType.Glitch)
                    __instance.MyTile.MyTileType = TileType.Glitch;
            }

            if (tile != null) __instance.MyTile = tile;

            if (__instance?.MyTile == null || __instance?.MyTile?.MyTileType <= TileType.Glitch || __instance.MyTile.HasBeenDestroyed || __instance.MyTile.IsInTheVoid) return true;

            return false;
        }
        public static void Postfix(ref TileObject __instance, ref Renderer ____tileRenderer, ref string ____displayedTileValue, ref Color ____textColor, ref Color ____scoreColor, ref GameObject[] ____tileMeshGOs, ref Renderer[] ____tileRenderers, ref OutlineFx.OutlineFx[] ____outlines, ref GameObject[] ____textParentGOs, ref GameObject ____crossOutGO, ref GameObject ____scatteredItemParentGO, ref ItemObject ____scatteredItemObject, ref TextMeshPro ____letterTMP, ref TextMeshPro[] ____letterTMPs, ref TextMeshPro[] ____scoreTMPs, ref TextMeshPro ____scoreTMP, ref CharacterWobble ____characterWobble, ref TextMeshPro ____suitTMP, ref int ____currentLightLayer, ref Color[] ____suitColors)
        {
            __instance?.MyTile?.MyTileType = currentTileType;

            ModdedTileType tileType = TileUtils.ModdedTileTypes.FirstOrDefault(t => t.tileType == currentTileType);
            ____scoreTMP.SetText(__instance.MyTile.GetValue().ToString());

            if (tileType == null) return;

            if (tileType.tileMesh == null)
            {
                tileType.tileMesh = GameObject.Instantiate(____tileMeshGOs[10], ____tileMeshGOs[11].transform.parent, false);
                tileType.tileMesh.SetActive(false);
            }

            if (____tileMeshGOs[11].name != "Modded Tile")
            {
                if (____tileMeshGOs[11] != null)
                {
                    GameObject.Destroy(____tileMeshGOs[11]);
                }

                ____tileMeshGOs[11] = GameObject.Instantiate(tileType.tileMesh, ____tileMeshGOs[11].transform.parent, false);
                ____tileMeshGOs[11].name = "Modded Tile";
            }
            ____tileMeshGOs[11].SetActive(true);

            ____tileRenderer = ____tileRenderers[11];

            ____scoreTMP.color = tileType.textColor.tile;

            ____letterTMP?.color = tileType.textColor.tile;
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

    [HarmonyPatch(typeof(FullSerializer.Internal.fsDictionaryConverter), "AddItemToDictionary")] //IDEK why this is needed! :sob:
    public static class FixDuplicateKeyPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(IDictionary dictionary, object key, object value)
        {
            if (key == null) return false;

            if (dictionary.Contains(key))
            {
                dictionary[key] = value;

                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(BoardGenVizInfo), MethodType.Constructor, new System.Type[] { typeof(GridData), typeof(Item), typeof(List<Tile>), typeof(bool), typeof(Type), typeof(bool), typeof(bool), typeof(bool), typeof(Tile[]) })]
    public static class UnserializeGridData
    {
        public static void Postfix(GridData grid, ref BoardGenVizInfo __instance)
        {
            // change to foreach for tile types instead of 
            if (grid != null && __instance?.Grid != null)
            {
                int i = 0;
                foreach (Tile tile in __instance.Grid.GridTiles)
                {
                    tile.MyTileType = grid.GridTiles[i].MyTileType;
                    i++;
                }
            }
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
