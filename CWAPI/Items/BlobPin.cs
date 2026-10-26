using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace CWAPI.Items;

public class BlobPin : ModdedItem
{
    public BlobPin()
    {
        Name = "Blob Pin";
        Cost = 10;
        UpgradeableComponents = new List<UpgradeableComponent>()
        {
            new UpgradeableComponent(1, 1, 1),
            new UpgradeableComponent(1, 1, 1)
        };
    }

    public override string SpritePath => "";
    public override GridData ApplyStartOfGridEffect(GridData gridData, int gridNumber, int numberOfGrids, List<HistoricWord> previousWords, List<BoardGenVizInfo> vizSteps, bool isReroll)
    {
        Log.LogInfo("Doing Start Of Grid Item Stuff");

        List<Tile> list = new List<Tile>();
        Suit randomCardSuit = PlayingCardUtility.GetRandomCardSuit();
        for (int i = 0; i < 4; i++)
        {
            Tile tileForItemScatter = GridUtility.Singleton.GetTileForItemScatter(gridData, (TileType)12, GlyphType.None, null, true);
            if (tileForItemScatter != null)
            {
                tileForItemScatter.SetSuit(randomCardSuit);
                tileForItemScatter.MyTileType = i != 3 ? (TileType)12 : TileType.Glitch;
                list.Add(tileForItemScatter);
            }
        }
        if (list.Count > 0)
        {

            BoardGenVizInfo newInfo = new BoardGenVizInfo(gridData, this, list, false, null, false, false, false, vizSteps[vizSteps.Count - 1].PlayerConsumableTiles);

            vizSteps.Add(newInfo);
        }
        return gridData;
    }
}
