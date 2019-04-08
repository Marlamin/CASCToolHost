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
            var listfile = new Database();
            return new FileContentResult(Encoding.ASCII.GetBytes(string.Join('\n', listfile.GetFiles())), "text/plain")
            {
                FileDownloadName = "listfile.txt"
            };
        }

        [Route("download/build/{buildConfig}")]
        public ActionResult DownloadByBuild(string buildConfig)
        {
            var listfile = new Database();
            Logger.WriteLine("Serving listfile for build " + buildConfig);
            return new FileContentResult(Encoding.ASCII.GetBytes(string.Join('\n', listfile.GetFilesByBuild(buildConfig))), "text/plain")
            {
                FileDownloadName = "listfile.txt"
            };
        }
    }
}