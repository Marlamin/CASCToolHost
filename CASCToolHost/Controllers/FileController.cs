using CASCToolHost.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;

namespace CASCToolHost.Controllers
{
    [Route("casc/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        [Route("")]
        [Route("chash")]
        [HttpGet]
        public FileContentResult GetByContentHash(string buildConfig, string cdnConfig, string contenthash, string filename)
        {
            if (string.IsNullOrEmpty(buildConfig) || string.IsNullOrEmpty(cdnConfig) || string.IsNullOrEmpty(contenthash) || string.IsNullOrEmpty(filename))
            {
                throw new ArgumentException("Invalid arguments!");
            }

            // Retrieve CDNConfig from DB if not set in request
            if (string.IsNullOrEmpty(cdnConfig) && !string.IsNullOrEmpty(buildConfig))
            {
                cdnConfig = Database.GetCDNConfigByBuildConfig(buildConfig);
            }

            Logger.WriteLine("Serving file \"" + filename + "\" (" + contenthash + ") for build " + buildConfig + " and cdn " + cdnConfig);

            return new FileContentResult(CASC.GetFile(buildConfig, cdnConfig, contenthash), "application/octet-stream")
            {
                FileDownloadName = filename
            };
        }

        [Route("fdid")]
        [HttpGet]
        public ActionResult GetByFileDataID(string buildConfig, string cdnConfig, uint filedataid, string filename)
        {
            // Retrieve CDNConfig from DB if not set in request
            if (string.IsNullOrEmpty(cdnConfig) && !string.IsNullOrEmpty(buildConfig))
            {
                cdnConfig = Database.GetCDNConfigByBuildConfig(buildConfig);
            }

            // Retrieve filename from DB if not set in request
            if (string.IsNullOrEmpty(filename) && filedataid != 0)
            {
                filename = Path.GetFileName(Database.GetFilenameByFileDataID(filedataid));
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

            try
            {
                return new FileContentResult(CASC.GetFile(buildConfig, cdnConfig, filedataid), "application/octet-stream")
                {
                    FileDownloadName = filename
                };
            }
            catch (FileNotFoundException)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Logger.WriteLine("File " + filedataid + " not found in root of buildconfig " + buildConfig + " cdnconfig " + cdnConfig);
                Console.ResetColor();
                return NotFound();
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
        public ActionResult GetByFileName(string buildConfig, string cdnConfig, string filename)
        {
            // Retrieve CDNConfig from DB if not set in request
            if (string.IsNullOrEmpty(cdnConfig) && !string.IsNullOrEmpty(buildConfig))
            {
                cdnConfig = Database.GetCDNConfigByBuildConfig(buildConfig);
            }

            if (string.IsNullOrEmpty(buildConfig) || string.IsNullOrEmpty(cdnConfig) || string.IsNullOrEmpty(filename))
            {
                throw new ArgumentException("Invalid arguments!");
            }

            Logger.WriteLine("Serving file \"" + filename + "\" for build " + buildConfig + " and cdn " + cdnConfig);

            try
            {
                return new FileContentResult(CASC.GetFileByFilename(buildConfig, cdnConfig, filename), "application/octet-stream")
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
    }
}
