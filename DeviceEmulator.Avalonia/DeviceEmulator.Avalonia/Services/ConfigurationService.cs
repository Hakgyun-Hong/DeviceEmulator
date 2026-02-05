using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeviceEmulator.Models;

namespace DeviceEmulator.Services
{
    public class SavedConfig
    {
        public List<DeviceConfig> Devices { get; set; } = new();
    }

    public static class ConfigurationService
    {
        private static string ConfigPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "device_emulator_config.json");

        public static void Save(IEnumerable<DeviceConfig> devices)
        {
            try
            {
                var config = new SavedConfig { Devices = new List<DeviceConfig>(devices) };
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true
                };
                var json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(ConfigPath, json);
                Console.WriteLine($"[Config] Saved to {ConfigPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Config] Failed to save: {ex.Message}");
            }
        }

        public static List<DeviceConfig> Load()
        {
            if (!File.Exists(ConfigPath)) return new List<DeviceConfig>();

            try
            {
                var json = File.ReadAllText(ConfigPath);
                var config = JsonSerializer.Deserialize<SavedConfig>(json);
                return config?.Devices ?? new List<DeviceConfig>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Config] Failed to load: {ex.Message}");
                return new List<DeviceConfig>();
            }
        }
    }
}
