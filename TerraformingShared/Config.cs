﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Newtonsoft.Json;

namespace Terraforming
{
    class DefaultConfig
    {
        public static readonly bool rebuildMessages = true;
        public static readonly bool habitantModulesPartialBurying = true;
        public static readonly bool terrainImpactWithRepulsionCannon = true;
        public static readonly float spaceBetweenTerrainHabitantModule = 1.0f;
        public static readonly float destroyableObstacleTransparency = .1f;
        public static readonly bool destroyLargerObstaclesOnConstruction = false;
    }

    class Config
    {
        public bool rebuildMessages = DefaultConfig.rebuildMessages;
        public bool habitantModulesPartialBurying = DefaultConfig.habitantModulesPartialBurying;
        public bool terrainImpactWithRepulsionCannon = DefaultConfig.terrainImpactWithRepulsionCannon;
        public float spaceBetweenTerrainHabitantModule = DefaultConfig.spaceBetweenTerrainHabitantModule;
        public float destroyableObstacleTransparency = DefaultConfig.destroyableObstacleTransparency;
        public bool destroyLargerObstaclesOnConstruction = DefaultConfig.destroyLargerObstaclesOnConstruction;

        private static string configPath = Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location), "config.json");

        public static Config Instance { get; private set; } = new Config();

        private Config()
        {
        }

        public static void Load()
        {
            if (!File.Exists(configPath))
            {
                Logger.Info($"Creating config.");

                Save();
                return;
            }

            var json = File.ReadAllText(configPath);
            var userSettings = JsonConvert.DeserializeObject<Config>(json);

            var fields = typeof(Config).GetFields();

            foreach (var field in fields)
            {
                var userValue = field.GetValue(userSettings);
                field.SetValue(Instance, userValue);
            }
        }

        public static void Save()
        {
            Logger.Info($"Saving config.");
            File.WriteAllText(configPath, JsonConvert.SerializeObject(Instance, Formatting.Indented));
        }
    }
}
