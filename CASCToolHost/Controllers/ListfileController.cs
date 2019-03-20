using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using CASCToolHost.Utils;

namespace CASCToolHost.Controllers
{
    [Route("casc/listfile")]
    [ApiController]
    public class ListfileController : ControllerBase
    {
        [Route("check")]
        public string Check()
        {
            // Check filenames and add to DB, show result
            return "todo";
        }

        [Route("download")]
        public ActionResult Download()
        {
            Logger.WriteLine("Serving listfile");
            return new FileContentResult(Encoding.ASCII.GetBytes(string.Join('\n', Listfile.GetFiles())), "text/plain")
            {
                FileDownloadName = "listfile.txt"
            };
        }

        [Route("download/build/{buildConfig}")]
        public ActionResult DownloadByBuild(string buildConfig)
        {
            Logger.WriteLine("Serving listfile for build " + buildConfig);
            return new FileContentResult(Encoding.ASCII.GetBytes(string.Join('\n', Listfile.GetFilesByBuild(buildConfig))), "text/plain")
            {
                FileDownloadName = "listfile.txt"
            };
        }
    }
}