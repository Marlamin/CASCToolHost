using CASCToolHost.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CASCToolHost.Controllers
{
    [Route("casc/install")]
    [ApiController]
    public class InstallController : Controller
    {
        [Route("dumpbybuild")]
        public async Task<ActionResult> DumpByBuild(string buildConfigHash)
        {
            string installHash;

            // Force load build
            await BuildCache.GetOrCreate(buildConfigHash);

            var buildConfig = await Config.GetBuildConfig(buildConfigHash);

            if (buildConfig.install.Length == 2)
            {
                installHash = buildConfig.install[1].ToHexString().ToLower();
            }
            else
            {
                if (NGDP.encodingDictionary.TryGetValue(buildConfig.install[0], out var installEntry))
                {
                    installHash = installEntry[0].ToHexString().ToLower();
                }
                else
                {
                    throw new KeyNotFoundException("Install encoding key not found!");
                }
            }

            return await DumpByHash(installHash);
        }

        [Route("dump")]
        public async Task<ActionResult> DumpByHash(string hash)
        {
            var install = await NGDP.GetInstall(hash, true);
            return Json(install.entries);
        }

        [Route("diff")]
        public async Task<ActionResult> Diff(string from, string to)
        {
            var installFrom = await NGDP.GetInstall(from, true);
            var installTo = await NGDP.GetInstall(to, true);

            var installFromDict = new Dictionary<string, InstallFileEntry>();
            foreach (var entry in installFrom.entries)
            {
                installFromDict.Add(entry.name, entry);
            }

            var installToDict = new Dictionary<string, InstallFileEntry>();
            foreach (var entry in installTo.entries)
            {
                installToDict.Add(entry.name, entry);
            }

            var fromEntries = installFromDict.Keys.ToHashSet();
            var toEntries = installToDict.Keys.ToHashSet();

            var commonEntries = fromEntries.Intersect(toEntries);
            var removedEntries = fromEntries.Except(commonEntries);
            var addedEntries = toEntries.Except(commonEntries);

            var modifiedFiles = new List<InstallFileEntry>();
            foreach (var entry in commonEntries)
            {
                var originalFile = installFromDict[entry];
                var patchedFile = installToDict[entry];

                if (originalFile.contentHash.Equals(patchedFile.contentHash))
                {
                    continue;
                }

                modifiedFiles.Add(patchedFile);
            }

            return Json(new
            {
                added = addedEntries,
                modified = modifiedFiles,
                removed = removedEntries
            });
        }
    }
}