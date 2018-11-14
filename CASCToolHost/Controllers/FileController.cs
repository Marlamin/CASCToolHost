using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;

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
            if(string.IsNullOrEmpty(buildConfig) || string.IsNullOrEmpty(cdnConfig) || string.IsNullOrEmpty(contenthash) || string.IsNullOrEmpty(filename))
            {
                throw new NullReferenceException("Invalid arguments!");
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
            if (string.IsNullOrEmpty(buildConfig) || string.IsNullOrEmpty(cdnConfig) || filedataid == 0 || string.IsNullOrEmpty(filename))
            {
                throw new NullReferenceException("Invalid arguments!");
            }

            Logger.WriteLine("Serving file \"" + filename + "\" (fdid " + filedataid + ") for build " + buildConfig + " and cdn " + cdnConfig);

            try
            {
                return new FileContentResult(CASC.GetFile(buildConfig, cdnConfig, filedataid), "application/octet-stream")
                {
                    FileDownloadName = filename
                };
            }
            catch (FileNotFoundException e)
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
            if (string.IsNullOrEmpty(buildConfig) || string.IsNullOrEmpty(cdnConfig) || string.IsNullOrEmpty(filename))
            {
                throw new NullReferenceException("Invalid arguments!");
            }

            Logger.WriteLine("Serving file \"" + filename + "\" for build " + buildConfig + " and cdn " + cdnConfig);

            try
            {
                return new FileContentResult(CASC.GetFileByFilename(buildConfig, cdnConfig, filename), "application/octet-stream")
                {
                    FileDownloadName = filename
                };
            }
            catch (FileNotFoundException e)
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
