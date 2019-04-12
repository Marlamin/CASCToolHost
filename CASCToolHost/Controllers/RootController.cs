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
            var lookups = Database.GetKnownLookups();

            var rootFrom = NGDP.GetRoot(Path.Combine(CDN.cacheDir, "tpr", "wow"), from, true);
            var rootTo = NGDP.GetRoot(Path.Combine(CDN.cacheDir, "tpr", "wow"), to, true);

            var fromEntries = rootFrom.entries.Keys.ToHashSet();
            var toEntries = rootTo.entries.Keys.ToHashSet();

            var commonEntries = fromEntries.Intersect(toEntries);
            var removedEntries = fromEntries.Except(commonEntries);
            var addedEntries = toEntries.Except(commonEntries);

            Action<RootEntry, string> print = delegate (RootEntry entry, string action)
            {
                var lookup = entry.lookup.ToString("x").PadLeft(16, '0');
                var md5 = entry.md5.ToHexString().ToLower();
                var fileName = lookups.ContainsKey(entry.lookup) ? lookups[entry.lookup] : "Unknown File: " + entry.lookup.ToString("x").PadLeft(16, '0');

                result.Add(string.Format("[{0}] <b>{1}</b> (lookup: {2}, content md5: {3}, FileData ID: {4})", action, fileName, lookup, md5, entry.fileDataID));
            };

            foreach (var id in addedEntries)
            {
                var entry = rootTo.entries[id].First();
                print(entry, "ADDED");
            }

            foreach (var id in removedEntries)
            {
                var entry = rootFrom.entries[id].First();
                print(entry, "REMOVED");
            }

            foreach (var id in commonEntries)
            {
                var originalFile = rootFrom.entries[id].First();
                var patchedFile = rootTo.entries[id].First();

                if (originalFile.md5.ToHexString() == patchedFile.md5.ToHexString())
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