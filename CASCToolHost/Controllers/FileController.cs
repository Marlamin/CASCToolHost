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
        // GET casc/file
        [HttpGet]
        public FileContentResult Get(string buildConfig, string cdnConfig, string contenthash, string filename)
        {
            if(string.IsNullOrEmpty(buildConfig) || string.IsNullOrEmpty(cdnConfig) || string.IsNullOrEmpty(contenthash) || string.IsNullOrEmpty(filename))
            {
                throw new NullReferenceException("Invalid arguments!");
            }

            return new FileContentResult(CASC.GetFile(buildConfig, cdnConfig, contenthash), "application/octet-stream")
            {
                FileDownloadName = filename
            };
        }
    }
}
