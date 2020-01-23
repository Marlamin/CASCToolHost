using CASCToolHost.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

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
        public async Task<ActionResult> Download()
        {
            Logger.WriteLine("Serving listfile");
            return new FileContentResult(Encoding.ASCII.GetBytes(string.Join('\n', await Database.GetFiles())), "text/plain")
            {
                FileDownloadName = "listfile.txt"
            };
        }

        [Route("download/build/{buildConfig}")]
        public async Task<ActionResult> DownloadByBuild(string buildConfig)
        {
            Logger.WriteLine("Serving listfile for build " + buildConfig);
            var filesPerBuild = await Database.GetFilesByBuild(buildConfig);
            return new FileContentResult(Encoding.ASCII.GetBytes(string.Join('\n', filesPerBuild.Values)), "text/plain")
            {
                FileDownloadName = "listfile.txt"
            };
        }

        [Route("download/csv")]
        public async Task<ActionResult> DownloadCSV()
        {
            Logger.WriteLine("Serving CSV listfile");
            var knownFiles = await Database.GetKnownFiles();
            var nameList = new List<string>();
            foreach (var entry in knownFiles)
            {
                nameList.Add(entry.Key + ";" + entry.Value.filename);
            }

            return new FileContentResult(Encoding.ASCII.GetBytes(string.Join('\n', nameList.ToArray())), "text/plain")
            {
                FileDownloadName = "listfile.csv"
            };
        }

        [Route("download/csv/unverified")]
        public async Task<ActionResult> DownloadCSVUnverified()
        {
            Logger.WriteLine("Serving unverified CSV listfile");
            var knownFiles = await Database.GetKnownFiles(true);
            var nameList = new List<string>();
            foreach (var entry in knownFiles)
            {
                nameList.Add(entry.Key + ";" + entry.Value.filename);
            }

            return new FileContentResult(Encoding.ASCII.GetBytes(string.Join('\n', nameList.ToArray())), "text/plain")
            {
                FileDownloadName = "listfile.csv"
            };
        }

        [Route("download/csv/build")]
        public async Task<ActionResult> DownloadCSVByBuild(string buildConfig)
        {
            Logger.WriteLine("Serving CSV listfile for build " + buildConfig);

            var knownFiles = await Database.GetFilesByBuild(buildConfig);

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

        [Route("download/csv/unknown")]
        public async Task<ActionResult> DownloadUnknownCSV()
        {
            Logger.WriteLine("Serving unknown listfile");

            var unkFiles = await Database.GetUnknownFiles();

            return new FileContentResult(Encoding.ASCII.GetBytes(string.Join('\n', unkFiles)), "text/plain")
            {
                FileDownloadName = "unknown.csv"
            };
        }
    }
}