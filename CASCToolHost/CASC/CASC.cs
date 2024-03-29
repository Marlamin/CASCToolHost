﻿using CASCToolHost.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CASCToolHost
{
    public static class CASC
    {
        public static Dictionary<MD5Hash, IndexEntry> indexDictionary = new(new MD5HashComparer());
        public static List<MD5Hash> indexNames = new();

        public static async Task<RootFile> LoadRoot(string buildConfigHash, string cdnConfigHash)
        {
            Logger.WriteLine("Loading build " + buildConfigHash + "..");

            var buildConfig = await Config.GetBuildConfig(buildConfigHash);
            var cdnConfig = await Config.GetCDNConfig(cdnConfigHash);

            Logger.WriteLine("Loading encoding..");
            if (buildConfig.encodingSize == null || buildConfig.encodingSize.Length < 2)
            {
                await NGDP.GetEncoding(buildConfig.encoding[1].ToHexString(), 0);
            }
            else
            {
                await NGDP.GetEncoding(buildConfig.encoding[1].ToHexString(), int.Parse(buildConfig.encodingSize[1]));
            }

            Logger.WriteLine("Loading root..");
            if (NGDP.encodingDictionary.TryGetValue(buildConfig.root, out var rootEntry))
            {
                buildConfig.root_cdn = rootEntry[0];
            }
            else
            {
                throw new KeyNotFoundException("Root encoding key not found!");
            }

            var root = await NGDP.GetRoot(buildConfig.root_cdn.ToHexString().ToLower(), true);

            Logger.WriteLine("Loading indexes..");
            var loadedIndexes = await NGDP.GetIndexes(Path.Combine(CDNCache.cacheDir, "tpr/wow"), cdnConfig.archives);
            Logger.WriteLine("Loaded " + loadedIndexes + " indexes");

            Logger.WriteLine("Done loading build " + buildConfigHash);
            return root;
        }

        public static async Task<bool> FileExists(string buildConfig, uint filedataid)
        {
            var root = await RootCache.GetOrCreate(buildConfig);
            return root.entriesFDID.ContainsKey(filedataid);
        }

        public static async Task<bool> FileExists(string buildConfig, string filename)
        {
            var root = await RootCache.GetOrCreate(buildConfig);

            using (var hasher = new Jenkins96())
            {
                var lookup = hasher.ComputeHash(filename, true);

                if (root.entriesLookup.ContainsKey(lookup))
                {
                    return true;
                }
            }

            return false;
        }

        public async static Task<byte[]> GetFile(string buildConfig, string cdnConfig, uint filedataid)
        {
            var target = "";
            var root = await RootCache.GetOrCreate(buildConfig, cdnConfig);

            if (root.entriesFDID.TryGetValue(filedataid, out var entry))
            {
                var prioritizedEntry = entry.FirstOrDefault(subentry =>
                        subentry.contentFlags.HasFlag(ContentFlags.LowViolence) == false && (subentry.localeFlags.HasFlag(LocaleFlags.All_WoW) || subentry.localeFlags.HasFlag(LocaleFlags.enUS))
                    );

                var selectedEntry = (prioritizedEntry.fileDataID != 0) ? prioritizedEntry : entry.First();
                target = selectedEntry.md5.ToHexString().ToLower();
            }

            if (string.IsNullOrEmpty(target))
            {
                throw new FileNotFoundException("FileDataID " + filedataid + " not found in root for build " + buildConfig);
            }

            return await GetFile(buildConfig, cdnConfig, target);
        }

        public async static Task<byte[]> GetFile(string buildConfig, string cdnConfig, string contenthash)
        {
            var foundTarget = false;
            var contenthashMD5 = contenthash.ToByteArray().ToMD5();

            if (!NGDP.encodingDictionary.TryGetValue(contenthashMD5, out var targets))
            {
                Logger.WriteLine("Contenthash " + contenthash + " not found in encoding, loading build " + buildConfig + "..");

                await RootCache.GetOrCreate(buildConfig, cdnConfig);
                if (NGDP.encodingDictionary.TryGetValue(contenthashMD5, out targets))
                {
                    foundTarget = true;
                }
            }
            else
            {
                foundTarget = true;
            }

            if (!foundTarget)
            {
                throw new FileNotFoundException("Unable to find contenthash " + contenthash + " in encoding (bc " + buildConfig + ", cdnc " + cdnConfig + "!");
            }

            return await RetrieveFileBytes(targets);
        }

        public async static Task<byte[]> RetrieveFileBytes(List<MD5Hash> targets)
        {
            var targetEKey = targets[0];

            // TODO: ESpecString/key checking, this solution is slow and will not work well for all encryption scenarios
            if (targets.Count > 1)
            {
                for (var i = 0; i < targets.Count; i++)
                {
                    var targetBytes = await RetrieveFileBytes(new List<MD5Hash>() { targets[i] });

                    if (!Array.TrueForAll(targetBytes, x => x == 0))
                    {
                        targetEKey = targets[i];
                    }
                }
            }

            var targetString = targetEKey.ToHexString().ToLower();

            var unarchivedName = Path.Combine(CDNCache.cacheDir, "tpr/wow", "data", targetString[0] + "" + targetString[1], targetString[2] + "" + targetString[3], targetString);

            if (File.Exists(unarchivedName))
            {
                return BLTE.Parse(await File.ReadAllBytesAsync(unarchivedName));
            }

            if (!indexDictionary.TryGetValue(targetEKey, out IndexEntry entry))
            {
                throw new Exception("Unable to find file in archives. File is not available!?");
            }

            var index = indexNames[(int)entry.IndexID].ToHexString().ToLower();
            //Console.WriteLine("Retrieving file " + targetEKey.ToHexString().ToLower() + " archive from " + index);
            var archiveName = Path.Combine(CDNCache.cacheDir, "tpr/wow", "data", index[0] + "" + index[1], index[2] + "" + index[3], index);
            if (!File.Exists(archiveName))
            {
                Logger.WriteLine("Unable to find archive " + index + " on disk, attempting to stream from CDN instead");
                try
                {
                    return BLTE.Parse(await CDNCache.Get("data", index, true, false, entry.Size, entry.Offset));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            else
            {
                using (var stream = new FileStream(archiveName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    stream.Seek(entry.Offset, SeekOrigin.Begin);

                    try
                    {
                        if (entry.Offset > stream.Length || entry.Offset + entry.Size > stream.Length)
                        {
                            throw new Exception("File is beyond archive length, incomplete archive!");
                        }

                        var archiveBytes = new byte[entry.Size];
                        await stream.ReadAsync(archiveBytes.AsMemory(0, (int)entry.Size));
                        var content = BLTE.Parse(archiveBytes);

                        return content;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }

            return Array.Empty<byte>();
        }

        public async static Task<byte[]> GetFileByFilename(string buildConfig, string cdnConfig, string filename, LocaleFlags locale = LocaleFlags.All_WoW)
        {
            var root = await RootCache.GetOrCreate(buildConfig, cdnConfig);

            using var hasher = new Jenkins96();
            var lookup = hasher.ComputeHash(filename, true);
            var target = "";

            if (root.entriesLookup.TryGetValue(lookup, out var entry))
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
                    if (root.entriesFDID.TryGetValue(filedataid, out var fdidentry))
                    {
                        RootEntry prioritizedEntry;

                        if (locale == LocaleFlags.All_WoW)
                        {
                            prioritizedEntry = fdidentry.FirstOrDefault(subentry =>
                                subentry.contentFlags.HasFlag(ContentFlags.LowViolence) == false && (subentry.localeFlags.HasFlag(LocaleFlags.All_WoW) || subentry.localeFlags.HasFlag(LocaleFlags.enUS))
                            );
                        }
                        else
                        {
                            prioritizedEntry = fdidentry.FirstOrDefault(subentry =>
                                subentry.contentFlags.HasFlag(ContentFlags.LowViolence) == false && subentry.localeFlags.HasFlag(locale)
                            );
                        }

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
            var root = await RootCache.GetOrCreate(buildConfig, cdnConfig);

            using var hasher = new Jenkins96();
            var lookup = hasher.ComputeHash(filename, true);

            if (root.entriesLookup.TryGetValue(lookup, out var entry))
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
                root = await NGDP.GetRoot(rootcdn, true);
            }
            else
            {
                if (string.IsNullOrEmpty(cdnConfig))
                {
                    cdnConfig = await Database.GetCDNConfigByBuildConfig(buildConfig);
                }

                root = await RootCache.GetOrCreate(buildConfig, cdnConfig);
            }

            return root.entriesFDID.Keys.ToArray();
        }
    }
}
