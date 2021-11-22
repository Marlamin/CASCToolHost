using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace CASCToolHost.Controllers
{
    [Route("casc/preview")]
    [ApiController]
    public class PreviewController : ControllerBase
    {
        [Route("fdid")]
        [HttpGet]
        public async Task<FileContentResult> GetByFileDataID(string buildConfig, string cdnConfig, uint filedataid, string filename, byte mipmap = 0)
        {
            Logger.WriteLine("Serving preview of \"" + filename + "\" for build " + buildConfig + " and cdn " + cdnConfig);

            System.Net.Mime.ContentDisposition cd = new()
            {
                FileName = "preview",
                Inline = true
            };

            Response.Headers[HeaderNames.ContentDisposition] = cd.ToString();
            var fileBytes = await CASC.GetFile(buildConfig, cdnConfig, filedataid);
            var ext = Path.GetExtension(filename);

            var mime = GetMimeTypeByExt(ext);

            if (ext == ".blp")
            {
                using (var stream = new MemoryStream(fileBytes))
                using (var outStream = new MemoryStream())
                {
                    var blpReader = new SereniaBLPLib.BlpFile(stream);
                    var blp = blpReader.GetBitmap(mipmap);
                    blp.Save(outStream, ImageFormat.Png);
                    fileBytes = outStream.ToArray();
                    Response.Headers["X-WoWTools-Res-Width"] = blp.Width.ToString();
                    Response.Headers["X-WoWTools-Res-Height"] = blp.Height.ToString();
                    Response.Headers["X-WoWTools-AvailableMipMaps"] = blpReader.MipMapCount.ToString();
                }

                mime = "image/png";
            }

            return new FileContentResult(fileBytes, mime);
        }

        [Route("")]
        [Route("chash")]
        [HttpGet]
        public async Task<FileContentResult> GetByContentHash(string buildConfig, string cdnConfig, string contenthash, string filename, byte mipmap = 0)
        {
            Console.WriteLine("[" + DateTime.Now + "] Serving preview of \"" + filename + "\" (" + contenthash + ") for build " + buildConfig + " and cdn " + cdnConfig);

            System.Net.Mime.ContentDisposition cd = new()
            {
                FileName = Path.GetFileNameWithoutExtension(filename),
                Inline = true
            };

            Response.Headers[HeaderNames.ContentDisposition] = cd.ToString();

            var fileBytes = await CASC.GetFile(buildConfig, cdnConfig, contenthash);

            var ext = Path.GetExtension(filename);
            var mime = GetMimeTypeByExt(ext);

            if (ext == ".blp")
            {
                using (var stream = new MemoryStream(fileBytes))
                using (var outStream = new MemoryStream())
                {
                    var blpReader = new SereniaBLPLib.BlpFile(stream);
                    var blp = blpReader.GetBitmap(mipmap);
                    blp.Save(outStream, ImageFormat.Png);
                    fileBytes = outStream.ToArray();
                    Response.Headers["X-WoWTools-Res-Width"] = blp.Width.ToString();
                    Response.Headers["X-WoWTools-Res-Height"] = blp.Height.ToString();
                    Response.Headers["X-WoWTools-AvailableMipMaps"] = blpReader.MipMapCount.ToString();
                }

                mime = "image/png";
            }

            return new FileContentResult(fileBytes, mime);
        }

        private static string GetMimeTypeByExt(string ext)
        {
            switch (ext)
            {
                case ".mp3":
                    return "audio/mpeg";
                case ".xml":
                    return "text/xml";
                case ".ogg":
                    return "audio/ogg";
                case ".blp":
                    return "image/blp";
                default:
                    Console.WriteLine("Not familiar with extension " + ext + ", returning default mime type..");
                    return "application/octet-stream";
            }
        }
    }
}
