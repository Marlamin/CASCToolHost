using CASCToolHost.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CASCToolHost.Controllers
{
    [Route("casc/root")]
    [ApiController]
    public class RootController : Controller
    {
        [Route("getfdid")]
        public uint GetFileDataIDByFilename(string buildConfig, string cdnConfig, string filename)
        {
            Logger.WriteLine("Serving filedataid for \"" + filename + "\" for build " + buildConfig + " and cdn " + cdnConfig);
            return CASC.GetFileDataIDByFilename(buildConfig, cdnConfig, filename);
        }

        [Route("exists/{filedataid}")]
        public bool Get(string buildConfig, string cdnConfig, uint filedataid)
        {
            Logger.WriteLine("Serving existence check of fdid " + filedataid + " for build " + buildConfig);
            return CASC.FileExists(buildConfig, filedataid);
        }

        [Route("exists")]
        public bool Get(string buildConfig, string cdnConfig, string filename)
        {
            Logger.WriteLine("Serving existence check of \"" + filename + "\" for build " + buildConfig);
            return CASC.FileExists(buildConfig, filename);
        }

        [Route("fdids")]
        public uint[] Get(string buildConfig, string cdnConfig)
        {
            Logger.WriteLine("Serving filedataid list for build " + buildConfig + " and cdn " + cdnConfig);
            return CASC.GetFileDataIDsInBuild(buildConfig, cdnConfig);
        }

        [Route("fdidcount")]
        public int Get(string rootcdn)
        {
            Logger.WriteLine("Serving filedataid count for root_cdn " + rootcdn);
            return NGDP.GetRoot(Path.Combine(SettingsManager.cacheDir, "tpr", "wow"), rootcdn, true).entriesFDID.Count;
        }

        [Route("diff_api_invalidate")]
        public ActionResult DiffApiInvalidateCache()
        {
            BuildDiffCache.Invalidate();

            return Ok();
        }

        [Route("diff_api")]
        public ActionResult DiffApi(string from, string to, int start = 0)
        {
            Logger.WriteLine("Serving root diff for root " + from + " => " + to);

            if (BuildDiffCache.Get(from, to, out ApiDiff diff))
            {
                Logger.WriteLine("Serving cached diff for root " + from + " => " + to);

                return Json(new
                {
                    added = diff.added.Count(),
                    modified = diff.modified.Count(),
                    removed = diff.removed.Count(),
                    data = diff.all.ToArray()
                });
            }

            var filedataids = Database.GetAllFiles();

            var rootFrom = NGDP.GetRoot(Path.Combine(CDN.cacheDir, "tpr", "wow"), from, true);
            var rootTo = NGDP.GetRoot(Path.Combine(CDN.cacheDir, "tpr", "wow"), to, true);

            var rootFromEntries = rootFrom.entriesFDID;
            var rootToEntries = rootTo.entriesFDID;

            var fromEntries = rootFromEntries.Keys.ToHashSet();
            var toEntries = rootToEntries.Keys.ToHashSet();

            var commonEntries = fromEntries.Intersect(toEntries);
            var removedEntries = fromEntries.Except(commonEntries);
            var addedEntries = toEntries.Except(commonEntries);

            static RootEntry prioritize(List<RootEntry> entries)
            {
                var prioritized = entries.FirstOrDefault(subentry =>
                       subentry.contentFlags.HasFlag(ContentFlags.LowViolence) == false && (subentry.localeFlags.HasFlag(LocaleFlags.All_WoW) || subentry.localeFlags.HasFlag(LocaleFlags.enUS))
                );

                if (prioritized.fileDataID != 0)
                {
                    return prioritized;
                }
                else
                {
                    return entries.First();
                }
            }

            Func<RootEntry, DiffEntry> toDiffEntry(string action)
            {
                return delegate (RootEntry entry)
                {
                    var file = filedataids.ContainsKey(entry.fileDataID) ? filedataids[entry.fileDataID] : new CASCFile { filename = "", id = entry.fileDataID, type = "unk" };

                    return new DiffEntry
                    {
                        action = action,
                        filename = file.filename,
                        id = file.id.ToString(),
                        type = file.type,
                    };
                };
            }

            var addedFiles = addedEntries.Select(entry => rootToEntries[entry]).Select(prioritize);
            var removedFiles = removedEntries.Select(entry => rootFromEntries[entry]).Select(prioritize);

            // Modified files are a little bit more tricky, so we can't just throw a LINQ expression at it
            var modifiedFiles = new List<RootEntry>();

            foreach (var entry in commonEntries)
            {
                var originalFile = prioritize(rootFromEntries[entry]);
                var patchedFile = prioritize(rootToEntries[entry]);

                if (originalFile.md5.Equals(patchedFile.md5))
                {
                    continue;
                }

                modifiedFiles.Add(patchedFile);
            }

            var toAddedDiffEntryDelegate = toDiffEntry("added");
            var toRemovedDiffEntryDelegate = toDiffEntry("removed");
            var toModifiedDiffEntryDelegate = toDiffEntry("modified");

            diff = new ApiDiff
            {
                added = addedFiles.Select(toAddedDiffEntryDelegate),
                removed = removedFiles.Select(toRemovedDiffEntryDelegate),
                modified = modifiedFiles.Select(toModifiedDiffEntryDelegate)
            };

            Logger.WriteLine($"Added: {diff.added.Count()}, removed: {diff.removed.Count()}, modified: {diff.modified.Count()}, common: {commonEntries.Count()}");

            BuildDiffCache.Add(from, to, diff);

            return Json(new
            {
                added = diff.added.Count(),
                modified = diff.modified.Count(),
                removed = diff.removed.Count(),
                data = diff.all.ToArray()
            });
        }

        [Route("diff")]
        public string Diff(string from, string to)
        {
            Logger.WriteLine("Serving root diff for root " + from + " => " + to);
            var result = new List<string>();
            var csv = false;

            if (Request.Query.ContainsKey("csv"))
            {
                csv = true;
                result.Add("Action;Name;FileDataID");
            }

            var filedataids = Database.GetKnownFiles(true);

            var rootFrom = NGDP.GetRoot(Path.Combine(CDN.cacheDir, "tpr", "wow"), from, true);
            var rootTo = NGDP.GetRoot(Path.Combine(CDN.cacheDir, "tpr", "wow"), to, true);

            var rootFromEntries = rootFrom.entriesFDID;
            var rootToEntries = rootTo.entriesFDID;

            var fromEntries = rootFromEntries.Keys.ToHashSet();
            var toEntries = rootToEntries.Keys.ToHashSet();

            var commonEntries = fromEntries.Intersect(toEntries);
            var removedEntries = fromEntries.Except(commonEntries);
            var addedEntries = toEntries.Except(commonEntries);

            void print(RootEntry entry, string action)
            {
                var md5 = entry.md5.ToHexString().ToLower();

                var file = filedataids.ContainsKey(entry.fileDataID) ? filedataids[entry.fileDataID] : new CASCFile { filename = "Unknown File: " + entry.fileDataID, id = entry.fileDataID, type = "unk" };

                if (csv)
                {
                    result.Add(string.Format("{0};{1};{2}", action, file.filename, entry.fileDataID));
                }
                else
                {
                    if (entry.lookup == 0)
                    {
                        result.Add(string.Format("[{0}] <b>{1}</b> (content md5: {2}, FileData ID: {3})", action, file.filename, md5, entry.fileDataID));
                    }
                    else
                    {
                        var lookup = entry.lookup.ToString("x").PadLeft(16, '0');
                        result.Add(string.Format("[{0}] <b>{1}</b> (lookup: {2}, content md5: {3}, FileData ID: {4})", action, file.filename, lookup, md5, entry.fileDataID));
                    }
                }
            }

            foreach (var id in addedEntries)
            {
                var toEntry = rootToEntries[id];
                RootEntry? toPrio = toEntry.FirstOrDefault(subentry =>
                        subentry.contentFlags.HasFlag(ContentFlags.LowViolence) == false && (subentry.localeFlags.HasFlag(LocaleFlags.All_WoW) || subentry.localeFlags.HasFlag(LocaleFlags.enUS))
                    );

                var addedEntry = (toPrio.Value.fileDataID != 0) ? toPrio.Value : toEntry.First();
                print(addedEntry, "ADDED");
            }

            foreach (var id in removedEntries)
            {
                var fromEntry = rootFromEntries[id];
                RootEntry? fromPrio = fromEntry.FirstOrDefault(subentry =>
                        subentry.contentFlags.HasFlag(ContentFlags.LowViolence) == false && (subentry.localeFlags.HasFlag(LocaleFlags.All_WoW) || subentry.localeFlags.HasFlag(LocaleFlags.enUS))
                    );
                var removedEntry = (fromPrio.Value.fileDataID != 0) ? fromPrio.Value : fromEntry.First();

                print(removedEntry, "REMOVED");
            }

            foreach (var id in commonEntries)
            {
                var fromEntry = rootFromEntries[id];
                var toEntry = rootToEntries[id];

                RootEntry? fromPrio = fromEntry.FirstOrDefault(subentry =>
                        subentry.contentFlags.HasFlag(ContentFlags.LowViolence) == false && (subentry.localeFlags.HasFlag(LocaleFlags.All_WoW) || subentry.localeFlags.HasFlag(LocaleFlags.enUS))
                    );

                var originalFile = (fromPrio.Value.fileDataID != 0) ? fromPrio.Value : fromEntry.First();

                RootEntry? toPrio = toEntry.FirstOrDefault(subentry =>
                        subentry.contentFlags.HasFlag(ContentFlags.LowViolence) == false && (subentry.localeFlags.HasFlag(LocaleFlags.All_WoW) || subentry.localeFlags.HasFlag(LocaleFlags.enUS))
                    );

                var patchedFile = (toPrio.Value.fileDataID != 0) ? toPrio.Value : toEntry.First();


                if (originalFile.md5.Equals(patchedFile.md5))
                {
                    continue;
                }

                print(patchedFile, "MODIFIED");
            }

            result.Sort();
            return string.Join('\n', result.ToArray());
        }
    }
}