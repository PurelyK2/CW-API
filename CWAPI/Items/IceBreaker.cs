using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CWAPI.Items;

public class IceBreaker : ModdedItem
{
    public IceBreaker()
    {
        Name = "Ice Breaker";
        Cost = 10;
        UpgradeableComponents = new List<UpgradeableComponent>()
        {
            new UpgradeableComponent(1, 1, 1)
        };
    }

    public override string SpritePath => "CWAPI.Resources.IceBreaker.png";

    public override GridData ApplyStartOfGridEffect(GridData gridData, int gridNumber, int numberOfGrids, List<HistoricWord> previousWords, List<BoardGenVizInfo> vizSteps, bool isReroll)
    {
        Log.LogInfo("Doing Start Of Grid Item Stuff");

        List<Tile> list = new List<Tile>();
        List<Suit> list2 = new List<Suit>();
        list2.Add(Suit.Clubs);
        list2.Add(Suit.Diamonds);
        list2.Add(Suit.Hearts);
        list2.Add(Suit.Spades);
        List<TileType> list3 = new List<TileType>
        {
            (TileType)12
        };
        Suit randomCardSuit = PlayingCardUtility.GetRandomCardSuit();
        for (int i = 0; i < 4; i++)
        {
            Tile tileForItemScatter = GridUtility.Singleton.GetTileForItemScatter(gridData, list3[0], GlyphType.None, null, true);
            if (tileForItemScatter != null)
            {
                tileForItemScatter.SetSuit(randomCardSuit);
                tileForItemScatter.SetTileType((TileType)12);
                list.Add(tileForItemScatter);

                Log.LogInfo("Scattering Tile Type: " + tileForItemScatter.MyTileType);
            }
        }
        if (list.Count > 0)
        {
            vizSteps.Add(new BoardGenVizInfo(gridData, this, list, false, null, false, false, false, vizSteps[vizSteps.Count - 1].PlayerConsumableTiles));
        }
        return gridData;
    }
}
