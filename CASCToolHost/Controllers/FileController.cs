using CASCToolHost.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CASCToolHost.Controllers
{
    [Route("casc/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        [Route("")]
        [Route("chash")]
        [HttpGet]
        public async Task<FileContentResult> GetByContentHash(string buildConfig, string cdnConfig, string contenthash, string filename)
        {
            if (NGDP.encodingDictionary.TryGetValue(contenthash.ToByteArray().ToMD5(), out var entry))
            {
                Logger.WriteLine("Serving cached file \"" + filename + "\" (" + contenthash + ")", ConsoleColor.Green);

                return new FileContentResult(await CASC.RetrieveFileBytes(entry), "application/octet-stream")
                {
                    FileDownloadName = filename
                };
            }
            else
            {
                Logger.WriteLine("Looking up and serving file \"" + filename + "\" (" + contenthash + ") for build " + buildConfig + " and cdn " + cdnConfig, ConsoleColor.Yellow);
                if (string.IsNullOrEmpty(buildConfig) || string.IsNullOrEmpty(cdnConfig) || string.IsNullOrEmpty(contenthash) || string.IsNullOrEmpty(filename))
                {
                    throw new ArgumentException("Invalid arguments!");
                }

                // Retrieve CDNConfig from DB if not set in request
                if (string.IsNullOrEmpty(cdnConfig) && !string.IsNullOrEmpty(buildConfig))
                {
                    cdnConfig = await Database.GetCDNConfigByBuildConfig(buildConfig);
                }

                return new FileContentResult(await CASC.GetFile(buildConfig, cdnConfig, contenthash), "application/octet-stream")
                {
                    FileDownloadName = filename
                };
            }
        }

        [Route("fdid")]
        [HttpGet]
        public async Task<ActionResult> GetByFileDataID(string buildConfig, string cdnConfig, uint filedataid, string filename)
        {
            // Retrieve CDNConfig from DB if not set in request
            if (string.IsNullOrEmpty(cdnConfig) && !string.IsNullOrEmpty(buildConfig))
            {
                cdnConfig = await Database.GetCDNConfigByBuildConfig(buildConfig);
            }

            // Retrieve filename from DB if not set in request
            if (string.IsNullOrEmpty(filename) && filedataid != 0)
            {
                filename = Path.GetFileName(await Database.GetFilenameByFileDataID(filedataid));
                if (string.IsNullOrEmpty(filename))
                {
                    filename = filedataid + ".unk";
                }
            }

            if (string.IsNullOrEmpty(buildConfig) || string.IsNullOrEmpty(cdnConfig) || filedataid == 0)
            {
                throw new ArgumentException("Invalid arguments!");
            }

            Logger.WriteLine("Serving file \"" + filename + "\" (fdid " + filedataid + ") for build " + buildConfig + " and cdn " + cdnConfig);

            var file = await CASC.GetFile(buildConfig, cdnConfig, filedataid);

            try
            {
                return new FileContentResult(file, "application/octet-stream")
                {
                    FileDownloadName = filename
                };
            }
            catch (FileNotFoundException)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Logger.WriteLine("File " + filedataid + " not found in root of buildconfig " + buildConfig + " cdnconfig " + cdnConfig);
                Console.ResetColor();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Logger.WriteLine("Error " + e.Message + " occured when getting file " + filedataid + " of buildconfig " + buildConfig + " cdnconfig " + cdnConfig);
                Console.ResetColor();
            }

            return NotFound();
        }

        [Route("fname")]
        [HttpGet]
        public async Task<ActionResult> GetByFileName(string buildConfig, string cdnConfig, string filename)
        {
            // Retrieve CDNConfig from DB if not set in request
            if (string.IsNullOrEmpty(cdnConfig) && !string.IsNullOrEmpty(buildConfig))
            {
                cdnConfig = await Database.GetCDNConfigByBuildConfig(buildConfig);
            }

            if (string.IsNullOrEmpty(buildConfig) || string.IsNullOrEmpty(cdnConfig) || string.IsNullOrEmpty(filename))
            {
                throw new ArgumentException("Invalid arguments!");
            }

            Logger.WriteLine("Serving file \"" + filename + "\" for build " + buildConfig + " and cdn " + cdnConfig);

            try
            {
                return new FileContentResult(await CASC.GetFileByFilename(buildConfig, cdnConfig, filename), "application/octet-stream")
                {
                    FileDownloadName = Path.GetFileName(filename)
                };
            }
            catch (FileNotFoundException)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Logger.WriteLine("File " + filename + " not found in root of buildconfig " + buildConfig + " cdnconfig " + cdnConfig);
                Console.ResetColor();
                return NotFound();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Logger.WriteLine("Error " + e.Message + " occured when getting file " + filename + " of buildconfig " + buildConfig + " cdnconfig " + cdnConfig);
                Console.ResetColor();
            }

            return NotFound();
        }

        [Route("gametable")]
        [HttpGet]
        public async Task<ActionResult> GetGameTableByGameTableName(string gameTableName, string fullBuild)
        {
            var buildConfig = await Database.GetBuildConfigByFullBuild(fullBuild);
            var cdnConfig = await Database.GetCDNConfigByBuildConfig(buildConfig);

            if (string.IsNullOrEmpty(buildConfig) || string.IsNullOrEmpty(cdnConfig) || string.IsNullOrEmpty(gameTableName))
            {
                throw new ArgumentException("Invalid arguments!");
            }

            Logger.WriteLine("Serving gametable \"" + gameTableName + "\" for build " + fullBuild);

            try
            {
                return new FileContentResult(await CASC.GetFileByFilename(buildConfig, cdnConfig, "gametables/" + gameTableName.ToLower() + ".txt"), "application/octet-stream")
                {
                    FileDownloadName = Path.GetFileName(gameTableName.ToLower() + ".txt")
                };
            }
            catch (FileNotFoundException)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Logger.WriteLine("GameTable " + gameTableName + " not found in root of buildconfig " + buildConfig + " cdnconfig " + cdnConfig);
                Console.ResetColor();
                return NotFound();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Logger.WriteLine("Error " + e.Message + " occured when getting file " + gameTableName + " of buildconfig " + buildConfig + " cdnconfig " + cdnConfig);
                Console.ResetColor();
            }

            return NotFound();
        }

        [Route("db2")]
        [HttpGet]
        public async Task<ActionResult> GetDB2ByTableName(string tableName, string fullBuild, LocaleFlags locale = LocaleFlags.All_WoW)
        {
            var buildConfig = await Database.GetBuildConfigByFullBuild(fullBuild);
            var cdnConfig = await Database.GetCDNConfigByBuildConfig(buildConfig);

            if (string.IsNullOrEmpty(buildConfig) || string.IsNullOrEmpty(cdnConfig) || string.IsNullOrEmpty(tableName))
            {
                throw new ArgumentException("Invalid arguments!");
            }

            Logger.WriteLine("Serving DB2 \"" + tableName + "\" for build " + fullBuild + " with locale " + locale);

            try
            {
                return new FileContentResult(await CASC.GetFileByFilename(buildConfig, cdnConfig, "dbfilesclient/" + tableName.ToLower() + ".db2", locale), "application/octet-stream")
                {
                    FileDownloadName = Path.GetFileName(tableName.ToLower() + ".db2")
                };
            }
            catch (FileNotFoundException)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Logger.WriteLine("Table " + tableName + " not found in root of buildconfig " + buildConfig + " cdnconfig " + cdnConfig);
                Console.ResetColor();
                return NotFound();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Logger.WriteLine("Error " + e.Message + " occured when getting file " + tableName + " of buildconfig " + buildConfig + " cdnconfig " + cdnConfig);
                Console.ResetColor();
            }

            return NotFound();
        }
    }
}
