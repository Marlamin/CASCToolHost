using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace CASCToolHost.Controllers
{
    [Route("casc/preview")]
    [ApiController]
    public class PreviewController : ControllerBase
    {
        [HttpGet]
        public FileContentResult Get(string buildConfig, string cdnConfig, string contenthash, string filename)
        {
            Console.WriteLine("[" + DateTime.Now + "] Serving preview of \"" + filename + "\" (" + contenthash + ") for build " + buildConfig + " and cdn " + cdnConfig);

            System.Net.Mime.ContentDisposition cd = new System.Net.Mime.ContentDisposition
            {
                FileName = "preview",
                Inline = true
            };

            Response.Headers[HeaderNames.ContentDisposition] = cd.ToString();

            var fileBytes = CASC.GetFile(buildConfig, cdnConfig, contenthash);
            var ext = Path.GetExtension(filename);

            var mime = GetMimeTypeByExt(ext);

            if(ext == ".blp")
            {
                using (var stream = new MemoryStream(fileBytes))
                using (var outStream = new MemoryStream())
                {
                    var blpReader = new SereniaBLPLib.BlpFile(stream);
                    var blp = blpReader.GetBitmap(0);
                    blp.Save(outStream, ImageFormat.Png);
                    fileBytes = outStream.ToArray();
                }

                mime = "image/png";
            }

            return new FileContentResult(fileBytes, mime);
        }

        private string GetMimeTypeByExt(string ext)
        {
            switch (ext)
            {
                case ".mp3":
                    return "audio/mpeg";
                case ".xml":
                    return "text/xml";
                case ".ogg":
                    return "audio/ogg";
                default:
                    Console.WriteLine("Not familiar with extension " + ext + ", returning default mime type..");
                    return "application/octet-stream";
            }
        }
    }
}