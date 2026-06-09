using System;
using System.IO;
using System.Text.Json;

namespace MaterialImportTool.Models
{
    public class AppSettings
    {
        private static readonly string SettingsPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public string DbPath { get; set; } = "";
        public string CodePrefix { get; set; } = "S";
        public int CodeLength { get; set; } = 4;
        public string OcrLanguage { get; set; } = "chi_sim";

        public static AppSettings Load()
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            return new AppSettings();
        }

        public void Save()
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }

        public void Reset()
        {
            if (File.Exists(SettingsPath))
            {
                File.Delete(SettingsPath);
            }
        }
    }
}
