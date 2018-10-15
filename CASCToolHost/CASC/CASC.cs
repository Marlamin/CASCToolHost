using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;

namespace CASCToolHost
{
    public static class CASC
    {
        public static Dictionary<string, Build> buildDictionary = new Dictionary<string, Build>();
        public static Dictionary<MD5Hash, IndexEntry> indexDictionary = new Dictionary<MD5Hash, IndexEntry>(new MD5HashComparer());
        public static List<MD5Hash> indexNames = new List<MD5Hash>();

        private static Jenkins96 hasher = new Jenkins96();

        public struct Build
        {
            public BuildConfigFile buildConfig;
            public CDNConfigFile cdnConfig;
            public EncodingFile encoding;
            public RootFile root;
            public DateTime loadedAt;
        }

        public static void LoadBuild(string program, string buildConfigHash, string cdnConfigHash)
        {
            Logger.WriteLine("Loading build " + buildConfigHash + "..");

            var unloadList = new List<string>();

            foreach(var loadedBuild in buildDictionary)
            {
                if (loadedBuild.Value.loadedAt < DateTime.Now.AddHours(-4))
                {
                    Logger.WriteLine("Unloading build " + loadedBuild.Key + " as it its been loaded over 4 hours ago.");
                    unloadList.Add(loadedBuild.Key);
                }
            }

            foreach(var unloadBuild in unloadList)
            {
                buildDictionary.Remove(unloadBuild);
            }

            var build = new Build();

            var cdnsFile = NGDP.GetCDNs(program);
            build.buildConfig = Config.GetBuildConfig(Path.Combine(CDN.cacheDir, cdnsFile.entries[0].path), buildConfigHash);
            build.cdnConfig = Config.GetCDNConfig(Path.Combine(CDN.cacheDir, cdnsFile.entries[0].path), cdnConfigHash);

            Logger.WriteLine("Loading encoding..");
            if (build.buildConfig.encodingSize == null || build.buildConfig.encodingSize.Count() < 2)
            {
                build.encoding = NGDP.GetEncoding("http://" + cdnsFile.entries[0].hosts[0] + "/" + cdnsFile.entries[0].path + "/", build.buildConfig.encoding[1].ToHexString(), 0);
            }
            else
            {
                build.encoding = NGDP.GetEncoding("http://" + cdnsFile.entries[0].hosts[0] + "/" + cdnsFile.entries[0].path + "/", build.buildConfig.encoding[1].ToHexString(), int.Parse(build.buildConfig.encodingSize[1]));
            }

            Logger.WriteLine("Loading root..");
            var rootHash = "";

            if(build.encoding.aEntries.TryGetValue(build.buildConfig.root, out var rootEntry))
            {
                rootHash = rootEntry.eKey.ToHexString().ToLower();
            }
            else
            {
                throw new KeyNotFoundException("Root encoding key not found!");
            }

            build.root = NGDP.GetRoot("http://" + cdnsFile.entries[0].hosts[0] + "/" + cdnsFile.entries[0].path + "/", rootHash, true);

            build.loadedAt = DateTime.Now;

            Logger.WriteLine("Loading indexes..");
            NGDP.GetIndexes(Path.Combine(CDN.cacheDir, cdnsFile.entries[0].path), build.cdnConfig.archives);

            if (buildDictionary.ContainsKey(buildConfigHash))
            {
                Logger.WriteLine("Build was already loaded while this iteration was loading, not adding to cache!");
            }
            else
            {
                buildDictionary.Add(buildConfigHash, build);
                Logger.WriteLine("Loaded build " + build.buildConfig.buildName + "!");
            }

        }

        public static bool FileExists(string buildConfig, string cdnConfig, uint filedataid)
        {
            if (!buildDictionary.ContainsKey(buildConfig))
            {
                LoadBuild("wowt", buildConfig, cdnConfig);
            }

            var build = buildDictionary[buildConfig];

            foreach (var entry in build.root.entries)
            {
                if (entry.Value[0].fileDataID == filedataid)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool FileExists(string buildConfig, string cdnConfig, string filename)
        {
            if (!buildDictionary.ContainsKey(buildConfig))
            {
                LoadBuild("wowt", buildConfig, cdnConfig);
            }

            var build = buildDictionary[buildConfig];

            var hasher = new Jenkins96();
            var lookup = hasher.ComputeHash(filename, true);

            foreach (var entry in build.root.entries)
            {
                if (entry.Value[0].lookup == lookup)
                {
                    return true;
                }
            }

            return false;
        }

        public static byte[] GetFile(string buildConfig, string cdnConfig, uint filedataid)
        {
            if (!buildDictionary.ContainsKey(buildConfig))
            {
                LoadBuild("wowt", buildConfig, cdnConfig);
            }

            var build = buildDictionary[buildConfig];

            var target = "";

            foreach (var entry in build.root.entries)
            {
                if (entry.Value[0].fileDataID == filedataid)
                {
                    RootEntry? prioritizedEntry = entry.Value.First(subentry =>
                        subentry.contentFlags.HasFlag(ContentFlags.LowViolence) == false && (subentry.localeFlags.HasFlag(LocaleFlags.All_WoW) || subentry.localeFlags.HasFlag(LocaleFlags.enUS))
                    );

                    var selectedEntry = (prioritizedEntry != null) ? prioritizedEntry.Value : entry.Value.First();
                    target = selectedEntry.md5.ToHexString().ToLower();
                }
            }

            if (string.IsNullOrEmpty(target))
            {
                throw new FileNotFoundException("No file found in root for FileDataID " + filedataid);
            }

            return GetFile(buildConfig, cdnConfig, target);
        }

        public static byte[] GetFile(string buildConfig, string cdnConfig, string contenthash)
        {
            if (!buildDictionary.ContainsKey(buildConfig))
            {
                LoadBuild("wowt", buildConfig, cdnConfig);
            }

            var build = buildDictionary[buildConfig];

            string target = "";

            if (build.encoding.aEntries.TryGetValue(contenthash.ToByteArray().ToMD5(), out var entry))
            {
                target = entry.eKey.ToHexString().ToLower();
            }
            else
            {
                throw new KeyNotFoundException("Key not found in encoding!");
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

            if (!buildDictionary.ContainsKey(buildConfig))
            {
                throw new Exception("Build is not loaded!");
            }

            var build = buildDictionary[buildConfig];

            IndexEntry entry = new IndexEntry();

            indexDictionary.TryGetValue(target.ToByteArray().ToMD5(), out entry);

            if (entry.size == 0)
            {
                throw new Exception("Unable to find file in archives. File is not available!?");
            }

            var index = indexNames[(int)entry.indexID].ToHexString().ToLower();

            var archiveName = Path.Combine(CDN.cacheDir, cdndir, "data", index[0] + "" + index[1], index[2] + "" + index[3], index);
            if (!File.Exists(archiveName))
            {
                throw new FileNotFoundException("Unable to find archive " + index + " on disk!");
            }

            using (var stream = new FileStream(archiveName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var bin = new BinaryReader(stream))
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

            return new byte[0];
        }

        public static byte[] GetFileByFilename(string buildConfig, string cdnConfig, string filename)
        {
            if (!buildDictionary.ContainsKey(buildConfig))
            {
                LoadBuild("wowt", buildConfig, cdnConfig);
            }

            var build = buildDictionary[buildConfig];

            var lookup = hasher.ComputeHash(filename, true);
            var target = "";

            if (build.root.entries.TryGetValue(lookup, out var entry))
            {
                RootEntry? prioritizedEntry = entry.FirstOrDefault(subentry =>
                        subentry.contentFlags.HasFlag(ContentFlags.LowViolence) == false && (subentry.localeFlags.HasFlag(LocaleFlags.All_WoW) || subentry.localeFlags.HasFlag(LocaleFlags.enUS))
                    );

                var selectedEntry = (prioritizedEntry != null) ? prioritizedEntry.Value : entry.First();
                target = selectedEntry.md5.ToHexString().ToLower();
            }

            if (string.IsNullOrEmpty(target))
            {
                throw new FileNotFoundException("No file found in root for filename " + filename);
            }

            return GetFile(buildConfig, cdnConfig, target);
        }

        public static uint GetFileDataIDByFilename(string buildConfig, string cdnConfig, string filename)
        {
            if (!buildDictionary.ContainsKey(buildConfig))
            {
                LoadBuild("wowt", buildConfig, cdnConfig);
            }

            var build = buildDictionary[buildConfig];

            var lookup = hasher.ComputeHash(filename, true);

            if (build.root.entries.TryGetValue(lookup, out var entry))
            {
                return entry[0].fileDataID;
            }

            return 0;
        }
    }
}
