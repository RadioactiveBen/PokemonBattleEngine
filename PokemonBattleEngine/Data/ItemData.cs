﻿using System.Collections.Generic;

namespace Kermalis.PokemonBattleEngine.Data
{
    public sealed class PItemData
    {
        public int FlingPower;

        public static Dictionary<PItem, PItemData> Data = new Dictionary<PItem, PItemData>()
        {
            { PItem.BrightPowder, new PItemData { FlingPower = 10 } },
            { PItem.ChoiceBand, new PItemData { FlingPower = 10 } },
            { PItem.DampRock, new PItemData { FlingPower = 60 } },
            { PItem.DeepSeaScale, new PItemData { FlingPower = 30 } },
            { PItem.DeepSeaTooth, new PItemData { FlingPower = 90 } },
            { PItem.Leftovers, new PItemData { FlingPower = 10 } },
            { PItem.LightBall, new PItemData { FlingPower = 30 } },
            { PItem.LightClay, new PItemData { FlingPower = 30 } },
            { PItem.MetalPowder, new PItemData { FlingPower = 10 } },
            { PItem.PowerHerb, new PItemData { FlingPower = 10 } },
            { PItem.SoulDew, new PItemData { FlingPower = 30 } },
            { PItem.ThickClub, new PItemData { FlingPower = 90 } },
            { PItem.WideLens, new PItemData { FlingPower = 10 } },
        };
    }
}
