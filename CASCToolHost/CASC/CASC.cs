using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Net.Http;

namespace CASCToolHost
{
    public static class CASC
    {
        public static Dictionary<string, Build> buildDictionary = new Dictionary<string, Build>();
        public static Dictionary<string, IndexEntry> indexDictionary = new Dictionary<string, IndexEntry>();

        static CASC()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                CDN.cacheDir = "H:/";
            }
            else
            {
                CDN.cacheDir = "/var/www/bnet.marlam.in/";
            }

            CDN.client = new HttpClient();
        }

        public struct Build
        {
            public BuildConfigFile buildConfig;
            public CDNConfigFile cdnConfig;
            public EncodingFile encoding;
            public Dictionary<string, IndexEntry> indexDictionary;
        }

        public static void LoadBuild(string program, string buildConfigHash, string cdnConfigHash)
        {
            var build = new Build();

            var cdnsFile = NGDP.GetCDNs(program);
            build.buildConfig = Config.GetBuildConfig(Path.Combine(CDN.cacheDir, cdnsFile.entries[0].path), buildConfigHash);
            build.cdnConfig = Config.GetCDNConfig(Path.Combine(CDN.cacheDir, cdnsFile.entries[0].path), cdnConfigHash);

            if (build.buildConfig.encodingSize == null || build.buildConfig.encodingSize.Count() < 2)
            {
                build.encoding = NGDP.GetEncoding("http://" + cdnsFile.entries[0].hosts[0] + "/" + cdnsFile.entries[0].path + "/", build.buildConfig.encoding[1], 0);
            }
            else
            {
                build.encoding = NGDP.GetEncoding("http://" + cdnsFile.entries[0].hosts[0] + "/" + cdnsFile.entries[0].path + "/", build.buildConfig.encoding[1], int.Parse(build.buildConfig.encodingSize[1]));
            }

            NGDP.GetIndexes(Path.Combine(CDN.cacheDir, cdnsFile.entries[0].path), build.cdnConfig.archives, indexDictionary);

            buildDictionary.Add(buildConfigHash, build);
        }

        public static byte[] GetFile(string buildConfig, string cdnConfig, string contenthash)
        {
            if (!buildDictionary.ContainsKey(buildConfig))
            {
                LoadBuild("wowt", buildConfig, cdnConfig);
            }

            var build = buildDictionary[buildConfig];

            string target = "";

            foreach (var entry in build.encoding.aEntries)
            {
                if (entry.hash.ToLower() == contenthash)
                {
                    target = entry.key.ToLower();
                    break;
                }
            }

            if (string.IsNullOrEmpty(target))
            {
                throw new FileNotFoundException("Unable to find file in encoding!");
            }

            return RetrieveFileBytes(buildConfig, target);
        }

        public static byte[] RetrieveFileBytes(string buildConfig, string target, bool raw = false, string cdndir = "tpr/wow")
        {
            var unarchivedName = Path.Combine(CDN.cacheDir, cdndir, "data", target[0] + "" + target[1], target[2] + "" + target[3], target);
            var build = buildDictionary[buildConfig];

            if (File.Exists(unarchivedName))
            {
                if (!raw)
                {
                    return BLTE.Parse(File.ReadAllBytes(unarchivedName));
                }
                else
                {
                    return File.ReadAllBytes(unarchivedName);
                }
            }

            if (!build.indexDictionary.TryGetValue(target.ToUpper(), out IndexEntry entry))
            {
                throw new Exception("Unable to find file in archives. File is not available!?");
            }
            else
            {
                var index = build.cdnConfig.archives[entry.index];

                var archiveName = Path.Combine(CDN.cacheDir, cdndir, "data", index[0] + "" + index[1], index[2] + "" + index[3], index);
                if (!File.Exists(archiveName))
                {
                    throw new FileNotFoundException("Unable to find archive " + index + " on disk!");
                }

                using (BinaryReader bin = new BinaryReader(File.Open(archiveName, FileMode.Open, FileAccess.Read)))
                {
                    bin.BaseStream.Position = entry.offset;
                    try
                    {
                        if (!raw)
                        {
                            return BLTE.Parse(bin.ReadBytes((int)entry.size));
                        }
                        else
                        {
                            return bin.ReadBytes((int)entry.size);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }

            return new byte[0];
        }

    }
}
