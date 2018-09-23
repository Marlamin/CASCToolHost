using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
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
        public FileContentResult GetByFileDataID(string buildConfig, string cdnConfig, int filedataid, string filename)
        {
            if (string.IsNullOrEmpty(buildConfig) || string.IsNullOrEmpty(cdnConfig) || filedataid == 0 || string.IsNullOrEmpty(filename))
            {
                throw new NullReferenceException("Invalid arguments!");
            }

            Logger.WriteLine("Serving file \"" + filename + "\" (fdid " + filedataid + ") for build " + buildConfig + " and cdn " + cdnConfig);

            return new FileContentResult(CASC.GetFile(buildConfig, cdnConfig, filedataid), "application/octet-stream")
            {
                FileDownloadName = filename
            };
        }
    }
}
