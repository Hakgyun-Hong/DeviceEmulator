using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DeviceEmulator.Models;

namespace DeviceEmulator.Services
{
    public static class MacroTemplateService
    {
        private static string ConfigPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "macro_templates.json");

        public static void Save(IEnumerable<MacroTemplate> templates)
        {
            try
            {
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true
                };
                var json = JsonSerializer.Serialize(templates, options);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MacroTemplateService] Failed to save: {ex.Message}");
            }
        }

        public static List<MacroTemplate> Load()
        {
            if (!File.Exists(ConfigPath))
            {
                var defaults = GetDefaultTemplates();
                Save(defaults);
                return defaults;
            }

            try
            {
                var json = File.ReadAllText(ConfigPath);
                var templates = JsonSerializer.Deserialize<List<MacroTemplate>>(json);
                return templates ?? GetDefaultTemplates();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MacroTemplateService] Failed to load: {ex.Message}");
                return GetDefaultTemplates();
            }
        }

        private static List<MacroTemplate> GetDefaultTemplates()
        {
            return new List<MacroTemplate>
            {
                new MacroTemplate
                {
                    Name = "Sleep (Delay)",
                    Description = "Pauses execution for a specified number of milliseconds.",
                    RequiredArguments = { "DurationMs" },
                    ScriptTemplate = "await System.Threading.Tasks.Task.Delay({{DurationMs}});"
                },
                new MacroTemplate
                {
                    Name = "Print Log",
                    Description = "Returns a text log to output.",
                    RequiredArguments = { "Message" },
                    ScriptTemplate = "return \"{{Message}}\";"
                }
            };
        }
    }
}
