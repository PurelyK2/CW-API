using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace CWAPI.Tiles;

public class CrackedTile : ModdedTileType
{
    public override string TileName => "Cracked";

    public override ScorePacket ModifyTileValue(ScorePacket tileValue)
    {
        return new ScorePacket(10000000);
    }
    public override void ApplyTileBonus(ScoreCalcVizInfo step, int index, List<Tile> tiles, List<TileSelection> selections, List<HistoricWord> previousWords, GridData gridData)
    {
        List<ScorePacket> tileScores = step.TileScores;
        tileScores[index] *= 6;
        step.TileScoreMultipliers[index] = new int?(6);
    }
    public override void ApplyTileSpawnEffect(Tile tile, GridData gridData, int gridNumber, int numberOfGrids, List<HistoricWord> previousWords, List<BoardGenVizInfo> vizSteps, bool isReroll)
    {
    }
}
