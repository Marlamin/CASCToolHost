using CASCToolHost.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
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

        [Route("download/csv")]
        public ActionResult DownloadCSV()
        {
            Logger.WriteLine("Serving CSV listfile");
            var knownFiles = Database.GetKnownFiles();
            var nameList = new List<string>();
            foreach(var entry in knownFiles)
            {
                nameList.Add(entry.Key + ";" + entry.Value);
            }

            return new FileContentResult(Encoding.ASCII.GetBytes(string.Join('\n', nameList.ToArray())), "text/plain")
            {
                FileDownloadName = "listfile.csv"
            };
        }

        [Route("download/csv/unverified")]
        public ActionResult DownloadCSVUnverified()
        {
            Logger.WriteLine("Serving unverified CSV listfile");
            var knownFiles = Database.GetKnownFiles(true);
            var nameList = new List<string>();
            foreach (var entry in knownFiles)
            {
                nameList.Add(entry.Key + ";" + entry.Value);
            }

            return new FileContentResult(Encoding.ASCII.GetBytes(string.Join('\n', nameList.ToArray())), "text/plain")
            {
                FileDownloadName = "listfile.csv"
            };
        }
    }
}