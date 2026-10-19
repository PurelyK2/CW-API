using CWAPI.Items;
using CWAPI.Utilities;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using Color = UnityEngine.Color;

namespace CWAPI.Tiles;

public abstract class ModdedTileType
{
    public abstract string TileName { get; }
    public TileType tileType = 0;
    public abstract Color tileColor { get; }
    public abstract Mesh tileMesh { get; }
    public virtual string tileSelectionClip => "Tiles/Select Colour Tile Red";
    public virtual bool canRandomSpread => false;
    public virtual bool isXenocolor => false;

    public virtual GameObject tileGameObject { get; private set; } = null;
    public GameObject GetTileObject()
    {
        if(tileGameObject == null)
        {
            GameObject tileParent = new GameObject("TileNormalParent(Clone)", new System.Type[] { typeof(Transform) });
            GameObject tileChild = new GameObject("TileNormalB", components: new System.Type[] { typeof(Transform), typeof(MeshRenderer), typeof(OutlineFx.OutlineFx) });
            MeshFilter filter = tileChild.AddComponent<MeshFilter>();
            filter.mesh = tileMesh;

            tileChild.transform.SetParent(tileParent.transform);

            tileGameObject = tileParent;
        }
        return tileGameObject;
    }

    public virtual void ApplyTileBonus(ScoreCalcVizInfo step, int index, List<Tile> tiles, List<TileSelection> selections, List<HistoricWord> previousWords, GridData gridData){}
    public virtual void ApplyWordBonus(ScoreCalcVizInfo step, int gridNumber, List<Tile> tiles, List<string> words, List<TileSelection> selections, List<HistoricWord> previousWords, GridData gridData){}

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
        public static void Postfix(ref GameObject[] ____tileMeshGOs, ref Renderer ____tileRenderer, ref Renderer[] ____tileRenderers, Tile tile)
        {
            if (tile == null) return;

            tile.MyTileType = cachedTileType;

            //if (tile.MyTileType <= TileType.Glitch) return;

            tile.MyTileType = (TileType)12; //TESTING

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

                Log.LogInfo(string.Join("\n", ____tileMeshGOs.ToArray<object>()));

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
                GameObject moddedTileColorGO = GameObject.Instantiate(____tileColorGOs[10], ____tileColorGOs[10].transform.parent, false);

                moddedTileColorGO.name = "ModdedTile";

                if (moddedTileColorGO == null) Log.LogError("Error: ModdedTileColorGameObject Failed To Set");

                List<GameObject> tileColorGOList = ____tileColorGOs.ToList();
                tileColorGOList.Add(moddedTileColorGO);
                ____tileColorGOs = new GameObject[tileColorGOList.Count];
                ____tileColorGOs = tileColorGOList.ToArray();
            }

            __result = ____tileColorGOs.FirstOrDefault(GO => GO.name == "ModdedTile");

            return false;
        }
    }
}
