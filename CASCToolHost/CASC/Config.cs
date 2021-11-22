using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CASCToolHost
{
    public class Config
    {
        public static async Task<CDNConfigFile> GetCDNConfig(string hash)
        {
            var cdnConfig = new CDNConfigFile();

            string content = System.Text.Encoding.UTF8.GetString(await CDNCache.Get("config", hash));

            var cdnConfigLines = content.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < cdnConfigLines.Length; i++)
            {
                if (cdnConfigLines[i].StartsWith("#") || cdnConfigLines[i].Length == 0) { continue; }
                var cols = cdnConfigLines[i].Split(new string[] { " = " }, StringSplitOptions.RemoveEmptyEntries);
                switch (cols[0])
                {
                    case "archives":
                        var archives = cols[1].Split(' ');
                        cdnConfig.archives = new MD5Hash[archives.Length];
                        for (var j = 0; j < archives.Length; j++)
                        {
                            cdnConfig.archives[j] = archives[j].ToByteArray().ToMD5();
                        }
                        break;
                    default:
                        break;
                }
            }

            return cdnConfig;
        }
        public static async Task<BuildConfigFile> GetBuildConfig(string hash)
        {
            var buildConfig = new BuildConfigFile();

            string content = System.Text.Encoding.UTF8.GetString(await CDNCache.Get("config", hash));

            if (string.IsNullOrEmpty(content) || !content.StartsWith("# Build"))
            {
                Console.WriteLine("Error reading build config!");
                return buildConfig;
            }

            var lines = content.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("#") || lines[i].Length == 0) { continue; }
                var cols = lines[i].Split(new string[] { " = " }, StringSplitOptions.RemoveEmptyEntries);
                switch (cols[0])
                {
                    case "root":
                        buildConfig.root = cols[1].ToByteArray().ToMD5();
                        break;
                    case "install":
                        var installSplit = cols[1].Split(' ');

                        buildConfig.install = new MD5Hash[installSplit.Length];
                        buildConfig.install[0] = installSplit[0].ToByteArray().ToMD5();

                        if (installSplit.Length > 1)
                        {
                            buildConfig.install[1] = installSplit[1].ToByteArray().ToMD5();
                        }
                        break;
                    case "encoding":
                        var encodingSplit = cols[1].Split(' ');

                        buildConfig.encoding = new MD5Hash[encodingSplit.Length];
                        buildConfig.encoding[0] = encodingSplit[0].ToByteArray().ToMD5();

                        if (encodingSplit.Length > 1)
                        {
                            buildConfig.encoding[1] = encodingSplit[1].ToByteArray().ToMD5();
                        }
                        break;
                    case "encoding-size":
                        var encodingSize = cols[1].Split(' ');
                        buildConfig.encodingSize = encodingSize;
                        break;
                    default:
                        break;
                }
            }

            return buildConfig;
        }
    }
}
