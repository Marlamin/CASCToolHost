using CASCToolHost.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using System.Net.Mime;

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
        public async Task<ActionResult> Download(string? typeFilter = null)
        {
            Logger.WriteLine("Serving listfile");

            var dataResponse = Encoding.ASCII.GetBytes(string.Join('\n', await Database.GetFiles(typeFilter)));

            var contentDispositionHeader = new ContentDispositionHeaderValue(DispositionTypeNames.Attachment)
            {
                FileName = "listfile.txt",
                Size = dataResponse.Length
            };

            Response.Headers[HeaderNames.ContentDisposition] = contentDispositionHeader.ToString();

            return new FileContentResult(dataResponse, "text/plain");
        }

        [Route("download/build/{buildConfig}")]
        public async Task<ActionResult> DownloadByBuild(string buildConfig, string? typeFilter = null)
        {
            Logger.WriteLine("Serving listfile for build " + buildConfig);
            var filesPerBuild = await Database.GetFilesByBuild(buildConfig, typeFilter);

            var dataResponse = Encoding.ASCII.GetBytes(string.Join('\n', filesPerBuild.Values));

            var contentDispositionHeader = new ContentDispositionHeaderValue(DispositionTypeNames.Attachment)
            {
                FileName = "listfile.txt",
                Size = dataResponse.Length
            };

            Response.Headers[HeaderNames.ContentDisposition] = contentDispositionHeader.ToString();

            return new FileContentResult(dataResponse, "text/plain");
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

            var dataResponse = Encoding.ASCII.GetBytes(string.Join('\n', nameList.ToArray()));

            var contentDispositionHeader = new ContentDispositionHeaderValue(DispositionTypeNames.Attachment)
            {
                FileName = "listfile.csv",
                Size = dataResponse.Length
            };

            Response.Headers[HeaderNames.ContentDisposition] = contentDispositionHeader.ToString();

            return new FileContentResult(dataResponse, "text/csv");
        }

        [Route("download/csv/unverified")]
        public async Task<ActionResult> DownloadCSVUnverified(string? typeFilter = null)
        {
            Logger.WriteLine("Serving unverified CSV listfile");
            var knownFiles = await Database.GetKnownFiles(true, typeFilter);
            var nameList = new List<string>();
            foreach (var entry in knownFiles)
            {
                nameList.Add(entry.Key + ";" + entry.Value.filename);
            }

            var dataResponse = Encoding.ASCII.GetBytes(string.Join('\n', nameList.ToArray()));

            var contentDispositionHeader = new ContentDispositionHeaderValue(DispositionTypeNames.Attachment)
            {
                FileName = "listfile.csv",
                Size = dataResponse.Length
            };

            Response.Headers[HeaderNames.ContentDisposition] = contentDispositionHeader.ToString();

            return new FileContentResult(dataResponse, "text/csv");
        }

        [Route("download/csv/build")]
        public async Task<ActionResult> DownloadCSVByBuild(string buildConfig, string? typeFilter = null)
        {
            Logger.WriteLine("Serving CSV listfile for build " + buildConfig);

            var knownFiles = await Database.GetFilesByBuild(buildConfig, typeFilter);

            var nameList = new List<string>();
            foreach (var entry in knownFiles)
            {
                nameList.Add(entry.Key + ";" + entry.Value);
            }

            var dataResponse = Encoding.ASCII.GetBytes(string.Join('\n', nameList.ToArray()));

            var contentDispositionHeader = new ContentDispositionHeaderValue(DispositionTypeNames.Attachment)
            {
                FileName = "listfile.csv",
                Size = dataResponse.Length
            };

            Response.Headers[HeaderNames.ContentDisposition] = contentDispositionHeader.ToString();

            return new FileContentResult(dataResponse, "text/csv");
        }

        [Route("download/csv/unknown")]
        public async Task<ActionResult> DownloadUnknownCSV()
        {
            Logger.WriteLine("Serving unknown listfile");

            var unkFiles = await Database.GetUnknownFiles();
            var dataResponse = Encoding.ASCII.GetBytes(string.Join('\n', unkFiles));

            var contentDispositionHeader = new ContentDispositionHeaderValue(DispositionTypeNames.Attachment)
            {
                FileName = "unknown.csv",
                Size = dataResponse.Length
            };

            Response.Headers[HeaderNames.ContentDisposition] = contentDispositionHeader.ToString();

            return new FileContentResult(dataResponse, "text/csv");
        }


        [Route("download/csv/unknownlookups")]
        public async Task<ActionResult> DownloadUnknownLookupCSV()
        {
            Logger.WriteLine("Serving unknown lookup listfile");

            var unkFiles = await Database.GetUnknownLookups();

            var dataResponse = Encoding.ASCII.GetBytes(string.Join('\n', unkFiles));

            var contentDispositionHeader = new ContentDispositionHeaderValue(DispositionTypeNames.Attachment)
            {
                FileName = "unknownlookups.csv",
                Size = dataResponse.Length
            };

            Response.Headers[HeaderNames.ContentDisposition] = contentDispositionHeader.ToString();

            return new FileContentResult(dataResponse, "text/csv");
        }
    }
}