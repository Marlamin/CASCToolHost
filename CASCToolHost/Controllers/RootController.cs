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
    public class RootController : ControllerBase
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
            Logger.WriteLine("Serving existence check of fdid " + filedataid + " for build " + buildConfig + " and cdn " + cdnConfig);
            return CASC.FileExists(buildConfig, cdnConfig, filedataid);
        }

        [Route("exists")]
        public bool Get(string buildConfig, string cdnConfig, string filename)
        {
            Logger.WriteLine("Serving existence check of \"" + filename + "\" for build " + buildConfig + " and cdn " + cdnConfig);
            return CASC.FileExists(buildConfig, cdnConfig, filename);
        }

        [Route("fdids")]
        public uint[] Get(string buildConfig, string cdnConfig)
        {
            Logger.WriteLine("Serving filedataid list for build " + buildConfig + " and cdn " + cdnConfig);
            return CASC.GetFileDataIDsInBuild(buildConfig, cdnConfig);
        }

        [Route("diff")]
        public string Diff(string from, string to)
        {
            Logger.WriteLine("Serving root diff for root " + from + " => " + to);

            var result = new List<string>();
            var filedataids = Database.GetKnownFiles();

            // Note the conversion to FDID => Entry at the end.
            var rootFrom = NGDP.GetRoot(Path.Combine(CDN.cacheDir, "tpr", "wow"), from, true).entries.ToDictionary(x => x.Value.First().fileDataID, x => x.Value);
            var rootTo = NGDP.GetRoot(Path.Combine(CDN.cacheDir, "tpr", "wow"), to, true).entries.ToDictionary(x => x.Value.First().fileDataID, x => x.Value);

            var fromEntries = rootFrom.Keys.ToHashSet();
            var toEntries = rootTo.Keys.ToHashSet();

            var commonEntries = fromEntries.Intersect(toEntries);
            var removedEntries = fromEntries.Except(commonEntries);
            var addedEntries = toEntries.Except(commonEntries);

            Action<RootEntry, string> print = delegate (RootEntry entry, string action)
            {
                var lookup = entry.lookup.ToString("x").PadLeft(16, '0');
                var md5 = entry.md5.ToHexString().ToLower();
                var fileName = filedataids.ContainsKey(entry.fileDataID) ? filedataids[entry.fileDataID] : "Unknown File: " + entry.lookup.ToString("x").PadLeft(16, '0');

                result.Add(string.Format("[{0}] <b>{1}</b> (lookup: {2}, content md5: {3}, FileData ID: {4})", action, fileName, lookup, md5, entry.fileDataID));
            };

            foreach (var id in addedEntries)
            {
                var entry = rootTo[id].First();
                print(entry, "ADDED");
            }

            foreach (var id in removedEntries)
            {
                var entry = rootFrom[id].First();
                print(entry, "REMOVED");
            }

            foreach (var id in commonEntries)
            {
                var originalFile = rootFrom[id].First();
                var patchedFile = rootTo[id].First();

                if (originalFile.md5.ToHexString() == patchedFile.md5.ToHexString())
                {
                    continue;
                }

                print(patchedFile, "MODIFIED");
            }

            result.Sort();
            return string.Join('\n', result.ToArray());
        }

        public struct DataTablesResult
        {
            public int draw;
            public int recordsFiltered;
            public int recordsTotal;
            public List<List<string>> data;
        }

        [Route("files")]
        public DataTablesResult Get(string buildConfig, int draw, int start, int length)
        {
            var result = new DataTablesResult();
            result.draw = draw;

            Logger.WriteLine("Serving file table data for build " + buildConfig);

            var searching = false;

            if (string.IsNullOrWhiteSpace(Request.Query["search[value]"]))
            {
                Logger.WriteLine("Serving file table data " + start + "," + length + " for build " + buildConfig + " for draw " + draw);
            }
            else
            {
                searching = true;
                Logger.WriteLine("Serving file table data " + start + "," + length + " for build " + buildConfig + " for draw " + draw + " with filter " + Request.Query["search[value]"]);

            }
            if (!CASC.buildDictionary.ContainsKey(buildConfig))
            {
                CASC.LoadBuild("wowt", buildConfig);
            }

            var build = CASC.buildDictionary[buildConfig];

            result.recordsTotal = build.root.entries.Count;

            result.data = new List<List<string>>();

            var resultCount = 0;

            if (searching)
            {
                var filenameMap = Database.GetKnownLookups();

                foreach (var entry in build.root.entries)
                {
                    var matches = false;
                    var row = new List<string>();
                    row.Add(entry.Value[0].fileDataID.ToString());

                    if(entry.Value[0].fileDataID == Request.Query["search[value]"])
                    {
                        matches = true;
                    }

                    if (filenameMap.TryGetValue(entry.Key, out var filename))
                    {
                        row.Add(filename);
                        if (filename.Contains(Request.Query["search[value]"]))
                        {
                            matches = true;
                        }
                    }
                    else
                    {
                        row.Add("");
                    }

                    row.Add(entry.Key.ToString("x"));
                    row.Add("");
                    row.Add("unk");
                    row.Add(""); // soundkits
                    row.Add("");
                    if (matches)
                    {
                        result.data.Add(row);
                        resultCount++;
                    }
                }

                result.recordsFiltered = resultCount;

                var takeLength = length;
                if ((start + length) > resultCount)
                {
                    takeLength = resultCount - start;
                }

                result.data = result.data.GetRange(start, takeLength);
            }
            else
            {
                result.recordsFiltered = result.recordsTotal;

                var entries = build.root.entries.ToList();
                foreach (var entry in entries.GetRange(start, length))
                {
                    var row = new List<string>();
                    row.Add(entry.Value[0].fileDataID.ToString());

                    var filename = Database.GetFilenameByFileDataID(entry.Value[0].fileDataID);

                    if (!string.IsNullOrEmpty(filename))
                    {
                        row.Add(filename);
                    }
                    else
                    {
                        row.Add("");
                    }

                    row.Add(entry.Key.ToString("x"));
                    row.Add("");
                    row.Add("unk");
                    row.Add(""); // soundkits
                    row.Add("");
                    result.data.Add(row);
                }
            }

            return result;
        }
    }
}