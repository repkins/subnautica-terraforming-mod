using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Terraforming.Tools
{
    static class PlayerToolExtensions
    {
        public static Player GetUsingPlayer(this PlayerTool playerTool)
        {
            return playerTool.usingPlayer;
        }

        public static EnergyMixin GetEnergyMixin(this PlayerTool playerTool)
        {
            return playerTool.energyMixin;
        }
    }
}
