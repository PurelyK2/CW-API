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
        Name = "An Ice Breaker";
        Cost = 10;
        UpgradeableComponents = new List<UpgradeableComponent>()
        {
            new UpgradeableComponent(1, 1, 1)
        };
    }

    public override string SpritePath => "CWAPI.Resources.IceBreaker.png";
}
