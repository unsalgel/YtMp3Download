using System;
using System.IO;
using Newtonsoft.Json;

namespace YoutubeMp3.Helpers
{
    public static class SettingsManager
    {
        private static readonly string SettingsPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YoutubeMp3", "settings.json");

        private sealed class Settings
        {
            public string? DownloadFolder { get; set; }
        }

        public static string GetDownloadFolder()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    var s = JsonConvert.DeserializeObject<Settings>(json);
                    if (!string.IsNullOrWhiteSpace(s?.DownloadFolder) &&
                        Directory.Exists(s.DownloadFolder))
                        return s.DownloadFolder!;
                }
            }
            catch { /* Hata durumunda varsayılan konuma düşer */ }

            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        public static void SaveDownloadFolder(string folder)
        {
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(SettingsPath)!);
                var json = JsonConvert.SerializeObject(
                    new Settings { DownloadFolder = folder },
                    Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch { /* Kaydetme hatası sessizce geçilir */ }
        }
    }
}
