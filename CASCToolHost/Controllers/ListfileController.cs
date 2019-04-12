using CASCToolHost.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Text;

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
            return new FileContentResult(Encoding.ASCII.GetBytes(string.Join('\n', Database.GetFiles())), "text/plain")
            {
                FileDownloadName = "listfile.txt"
            };
        }

        [Route("download/build/{buildConfig}")]
        public ActionResult DownloadByBuild(string buildConfig)
        {
            Logger.WriteLine("Serving listfile for build " + buildConfig);
            return new FileContentResult(Encoding.ASCII.GetBytes(string.Join('\n', Database.GetFilesByBuild(buildConfig))), "text/plain")
            {
                FileDownloadName = "listfile.txt"
            };
        }
    }
}