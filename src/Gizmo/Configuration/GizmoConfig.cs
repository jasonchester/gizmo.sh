using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Gizmo.Configuration
{
    public class GizmoConfig
    {
        public Dictionary<string, CosmosDbConnection> CosmosDbConnections { get; set; } = new Dictionary<string, CosmosDbConnection>();
        
        public static string ProfileConfigPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gizmo", "config.json");
        public static string LocalConfigPath => Path.Combine(Directory.GetCurrentDirectory(), ".gizmo", "config.json");

        public static string GetConfigPath(bool global = false)
        {
            return global ? ProfileConfigPath : LocalConfigPath;
        }
        public static async Task<GizmoConfig> LoadConfig(string configPath)
        {
            GizmoConfig settings;
            if(File.Exists(configPath))
            {
                var configString = await File.ReadAllTextAsync(configPath);
                settings = JsonConvert.DeserializeObject<GizmoConfig>(configString);
            }
            else
            {
                settings = new GizmoConfig();
            }

            return settings;
        }

        public static async Task SaveConfig(string configPath, GizmoConfig settings)
        {
            var configString = JsonConvert.SerializeObject(settings, Formatting.Indented);
            await File.WriteAllTextAsync(configPath, configString);
        }
    }
}