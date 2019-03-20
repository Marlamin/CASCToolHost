using System.IO;
using Microsoft.Extensions.Configuration;

namespace CASCToolHost
{
    public static class SettingsManager
    {
        public static string cacheDir;
        public static string connectionString;

        static SettingsManager()
        {
            LoadSettings();
        }

        public static void LoadSettings()
        {
            var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("config.json", optional: false, reloadOnChange: false).Build();
            cacheDir = config.GetSection("config")["cacheDir"];
            connectionString = config.GetSection("config")["connectionString"];
        }
    }
}