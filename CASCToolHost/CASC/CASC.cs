using CASCToolHost.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CASCToolHost
{
    public static class CASC
    {
        public static Dictionary<MD5Hash, IndexEntry> indexDictionary = new Dictionary<MD5Hash, IndexEntry>(new MD5HashComparer());
        public static List<MD5Hash> indexNames = new List<MD5Hash>();

        public struct Build
        {
            public BuildConfigFile buildConfig;
            public CDNConfigFile cdnConfig;
            public RootFile root;
            public MD5Hash root_cdn;
            public DateTime loadedAt;
        }

        public static async Task<Build> LoadBuild(string program, string buildConfigHash)
        {
            var cdnConfig = await Database.GetCDNConfigByBuildConfig(buildConfigHash);
            if (string.IsNullOrEmpty(cdnConfig))
            {
                throw new Exception("Unable to locate CDNconfig for buildconfig " + buildConfigHash);
            }

            return await LoadBuild(program, buildConfigHash, cdnConfig);
        }

        public static async Task<Build> LoadBuild(string program, string buildConfigHash, string cdnConfigHash)
        {
            Logger.WriteLine("Loading build " + buildConfigHash + "..");

            var build = new Build();

            build.buildConfig = Config.GetBuildConfig(Path.Combine(CDN.cacheDir, "tpr/wow"), buildConfigHash);
            build.cdnConfig = Config.GetCDNConfig(Path.Combine(CDN.cacheDir, "tpr/wow"), cdnConfigHash);

            Logger.WriteLine("Loading encoding..");
            if (build.buildConfig.encodingSize == null || build.buildConfig.encodingSize.Count() < 2)
            {
                await NGDP.GetEncoding(Path.Combine(CDN.cacheDir, "tpr/wow"), build.buildConfig.encoding[1].ToHexString(), 0);
            }
            else
            {
                await NGDP.GetEncoding(Path.Combine(CDN.cacheDir, "tpr/wow"), build.buildConfig.encoding[1].ToHexString(), int.Parse(build.buildConfig.encodingSize[1]));
            }

            Logger.WriteLine("Loading root..");

            if (NGDP.encodingDictionary.TryGetValue(build.buildConfig.root, out var rootEntry))
            {
                build.root_cdn = rootEntry;
            }
            else
            {
                throw new KeyNotFoundException("Root encoding key not found!");
            }

            build.root = await NGDP.GetRoot(Path.Combine(CDN.cacheDir, "tpr/wow"), build.root_cdn.ToHexString().ToLower(), true);

            build.loadedAt = DateTime.Now;

            Logger.WriteLine("Loading indexes..");
            NGDP.GetIndexes(Path.Combine(CDN.cacheDir, "tpr/wow"), build.cdnConfig.archives);

            Logger.WriteLine("Done loading build " + buildConfigHash);
            return build;
        }

        public static async Task<bool> FileExists(string buildConfig, uint filedataid)
        {
            var build = await BuildCache.GetOrCreate(buildConfig);

            if (build.root.entriesFDID.ContainsKey(filedataid))
            {
                return true;
            }

            return false;
        }

        public static async Task<bool> FileExists(string buildConfig, string filename)
        {
            var build = await BuildCache.GetOrCreate(buildConfig);

            using (var hasher = new Jenkins96())
            {
                var lookup = hasher.ComputeHash(filename, true);

                if (build.root.entriesLookup.ContainsKey(lookup))
                {
                    return true;
                }
            }

            return false;
        }

        public async static Task<byte[]> GetFile(string buildConfig, string cdnConfig, uint filedataid)
        {
            var build = await BuildCache.GetOrCreate(buildConfig, cdnConfig);

            var target = "";

            if (build.root.entriesFDID.TryGetValue(filedataid, out var entry))
            {
                var prioritizedEntry = entry.FirstOrDefault(subentry =>
                        subentry.contentFlags.HasFlag(ContentFlags.LowViolence) == false && (subentry.localeFlags.HasFlag(LocaleFlags.All_WoW) || subentry.localeFlags.HasFlag(LocaleFlags.enUS))
                    );

                var selectedEntry = (prioritizedEntry.fileDataID != 0) ? prioritizedEntry : entry.First();
                target = selectedEntry.md5.ToHexString().ToLower();
            }

            if (string.IsNullOrEmpty(target))
            {
                throw new FileNotFoundException("No file found in root for FileDataID " + filedataid);
            }

            return await GetFile(buildConfig, cdnConfig, target);
        }

        public async static Task<byte[]> GetFile(string buildConfig, string cdnConfig, string contenthash)
        {
            var foundTarget = false;
            var contenthashMD5 = contenthash.ToByteArray().ToMD5();

            if (!NGDP.encodingDictionary.TryGetValue(contenthashMD5, out MD5Hash target))
            {
                Logger.WriteLine("Contenthash " + contenthash + " not found in current encoding, loading build " + buildConfig + "..");
                
                await BuildCache.GetOrCreate(buildConfig, cdnConfig);
                if (NGDP.encodingDictionary.TryGetValue(contenthashMD5, out target))
                {
                    foundTarget = true;
                }

                // Remove build from cache, all encoding entries will be in encodingDictionary now for future reference
                BuildCache.Remove(buildConfig);
            }
            else
            {
                foundTarget = true;
            }

            if (!foundTarget)
            {
                throw new FileNotFoundException("Unable to find contenthash " + contenthash + " in encoding!");
            }

            return await RetrieveFileBytes(target);
        }

        public async static Task<byte[]> RetrieveFileBytes(MD5Hash target)
        {
            var targetString = target.ToHexString().ToLower();
            var unarchivedName = Path.Combine(CDN.cacheDir, "tpr/wow", "data", targetString[0] + "" + targetString[1], targetString[2] + "" + targetString[3], targetString);

            if (File.Exists(unarchivedName))
            {
                return BLTE.Parse(await File.ReadAllBytesAsync(unarchivedName));
            }

            if (!indexDictionary.TryGetValue(target, out IndexEntry entry))
            {
                throw new Exception("Unable to find file in archives. File is not available!?");
            }

            var index = indexNames[(int)entry.indexID].ToHexString().ToLower();

            var archiveName = Path.Combine(CDN.cacheDir, "tpr/wow", "data", index[0] + "" + index[1], index[2] + "" + index[3], index);
            if (!File.Exists(archiveName))
            {
                throw new FileNotFoundException("Unable to find archive " + index + " on disk!");
            }

            using (var stream = new FileStream(archiveName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                stream.Seek(entry.offset, SeekOrigin.Begin);

                try
                {
                    if (entry.offset > stream.Length || entry.offset + entry.size > stream.Length)
                    {
                        throw new Exception("File is beyond archive length, incomplete archive!");
                    }

                    var archiveBytes = new byte[entry.size];
                    await stream.ReadAsync(archiveBytes, 0, (int)entry.size);
                    return BLTE.Parse(archiveBytes);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            return new byte[0];
        }

        public async static Task<byte[]> GetFileByFilename(string buildConfig, string cdnConfig, string filename, LocaleFlags locale = LocaleFlags.All_WoW)
        {
            var build = await BuildCache.GetOrCreate(buildConfig, cdnConfig);

            using var hasher = new Jenkins96();
            var lookup = hasher.ComputeHash(filename, true);
            var target = "";

            if (build.root.entriesLookup.TryGetValue(lookup, out var entry))
            {
                RootEntry prioritizedEntry;

                if (locale == LocaleFlags.All_WoW)
                {
                    prioritizedEntry = entry.FirstOrDefault(subentry =>
                        subentry.contentFlags.HasFlag(ContentFlags.LowViolence) == false && (subentry.localeFlags.HasFlag(LocaleFlags.All_WoW) || subentry.localeFlags.HasFlag(LocaleFlags.enUS))
                    );
                }
                else
                {
                    prioritizedEntry = entry.FirstOrDefault(subentry =>
                        subentry.contentFlags.HasFlag(ContentFlags.LowViolence) == false && subentry.localeFlags.HasFlag(locale)
                    );
                }

                var selectedEntry = (prioritizedEntry.fileDataID != 0) ? prioritizedEntry : entry.First();
                target = selectedEntry.md5.ToHexString().ToLower();
            }

            if (string.IsNullOrEmpty(target))
            {
                var filedataid = await Database.GetFileDataIDByFilename(filename);
                if (filedataid != 0)
                {
                    if (build.root.entriesFDID.TryGetValue(filedataid, out var fdidentry))
                    {
                        var prioritizedEntry = fdidentry.FirstOrDefault(subentry =>
                            subentry.contentFlags.HasFlag(ContentFlags.LowViolence) == false && (subentry.localeFlags.HasFlag(LocaleFlags.All_WoW) || subentry.localeFlags.HasFlag(LocaleFlags.enUS))
                        );

                        var selectedEntry = (prioritizedEntry.fileDataID != 0) ? prioritizedEntry : fdidentry.First();
                        target = selectedEntry.md5.ToHexString().ToLower();
                    }
                }
            }

            if (string.IsNullOrEmpty(target))
            {
                throw new FileNotFoundException("No file found in root for filename " + filename);
            }

            return await GetFile(buildConfig, cdnConfig, target);
        }

        public static async Task<uint> GetFileDataIDByFilename(string buildConfig, string cdnConfig, string filename)
        {
            var build = await BuildCache.GetOrCreate(buildConfig, cdnConfig);

            using var hasher = new Jenkins96();
            var lookup = hasher.ComputeHash(filename, true);

            if (build.root.entriesLookup.TryGetValue(lookup, out var entry))
            {
                return entry[0].fileDataID;
            }

            return 0;
        }

        public static async Task<uint[]> GetFileDataIDsInBuild(string buildConfig, string cdnConfig)
        {
            var rootcdn = await Database.GetRootCDNByBuildConfig(buildConfig);

            RootFile root;

            if (!string.IsNullOrEmpty(rootcdn))
            {
                root = await NGDP.GetRoot(Path.Combine(SettingsManager.cacheDir, "tpr", "wow"), rootcdn, true);
            }
            else
            {
                if (string.IsNullOrEmpty(cdnConfig))
                {
                    cdnConfig = await Database.GetCDNConfigByBuildConfig(buildConfig);
                }

                var build = await BuildCache.GetOrCreate(buildConfig, cdnConfig);
                root = build.root;
            }

            return root.entriesFDID.Keys.ToArray();
        }
    }
}
