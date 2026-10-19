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
    public override Color tileColor => Color.black;
    public override Mesh tileMesh => null;
}
