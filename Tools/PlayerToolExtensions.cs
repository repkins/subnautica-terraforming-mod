using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Terraforming.Tools
{
    static class PlayerToolExtensions
    {
        private static readonly FieldInfo usingPlayerField = typeof(PlayerTool).GetField("usingPlayer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo energyMixinField = typeof(PlayerTool).GetField("energyMixin", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        public static Player GetUsingPlayer(this PlayerTool playerTool)
        {
            return usingPlayerField.GetValue(playerTool) as Player;
        }

        public static EnergyMixin GetEnergyMixin(this PlayerTool playerTool)
        {
            return energyMixinField.GetValue(playerTool) as EnergyMixin;
        }
    }
}
