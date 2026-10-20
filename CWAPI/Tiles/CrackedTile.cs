using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CWAPI.Tiles;

public class CrackedTile : ModdedTileType
{
    public override string TileName => "Cracked";
    public override Mesh tileMesh => null;

    public override ScorePacket ModifyTileValue(ScorePacket tileValue)
    {
        return new ScorePacket(-100);
    }
    public override void ApplyTileBonus(ScoreCalcVizInfo step, int index, List<Tile> tiles, List<TileSelection> selections, List<HistoricWord> previousWords, GridData gridData)
    {
        Log.LogInfo("Tile Done: " + index);

        List<ScorePacket> tileScores = step.TileScores;
        tileScores[index] *= 6;
        step.TileScoreMultipliers[index] = new int?(6);
    }
}
