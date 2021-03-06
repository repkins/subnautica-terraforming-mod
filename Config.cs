﻿using Oculus.Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Terraforming
{
    class Config
    {
        public bool rebuildMessages = true;
        public bool habitantModulesPartialBurying = true;
        public bool terrainImpactWithPropulsionCannon = true;
        public float spaceBetweenTerrainHabitantModule = 1.0f;
        public bool destroyLargerObstaclesOnConstruction = false;

        private static string assemblyName = Assembly.GetCallingAssembly().GetName().Name;
        private static string configPath = Environment.CurrentDirectory + @"\QMods\" + assemblyName + @"\config.json";

        public static Config Instance { get; private set; } = new Config();

        private Config()
        {
        }

        public static void Load()
        {
            if (!File.Exists(configPath))
            {
                File.WriteAllText(configPath, JsonConvert.SerializeObject(Instance, Formatting.Indented));
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
    }
}
