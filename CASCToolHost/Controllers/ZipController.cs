using CASCToolHost.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace CASCToolHost.Controllers
{
    [Route("casc/[controller]")]
    [ApiController]
    public class ZipController : ControllerBase
    {
        [Route("fdids")]
        [HttpGet]
        public async Task<ActionResult> GetByFileDataID(string buildConfig, string cdnConfig, string ids, string filename)
        {
            var filedataidlist = new List<uint>();
            foreach (var fdid in ids.Split(','))
            {
                filedataidlist.Add(uint.Parse(fdid));
            }

            var filedataids = filedataidlist.ToArray();

            if (string.IsNullOrEmpty(buildConfig) || string.IsNullOrEmpty(cdnConfig) || filedataids.Length == 0 || string.IsNullOrEmpty(filename))
            {
                throw new NullReferenceException("Invalid arguments!");
            }

            Logger.WriteLine("Serving zip file \"" + filename + "\" (" + filedataids.Length + " fdids starting with " + filedataids[0].ToString() + ") for build " + buildConfig + " and cdn " + cdnConfig);

            var errors = new List<string>();

            using (var zip = new MemoryStream())
            {
                using (var archive = new ZipArchive(zip, ZipArchiveMode.Create))
                {
                    foreach (var filedataid in filedataids)
                    {
                        if (zip.Length > 100000000)
                        {
                            errors.Add("Max of 100MB per archive reached, didn't include file " + filedataid);
                            Logger.WriteLine("Max of 100MB per archive reached!");
                            continue;
                        }

                        try
                        {
                            using (var cascStream = new MemoryStream(await CASC.GetFile(buildConfig, cdnConfig, filedataid)))
                            {
                                var entryname = Path.GetFileName(await Database.GetFilenameByFileDataID(filedataid));
                                if (entryname == "")
                                {
                                    entryname = filedataid.ToString() + ".unk";
                                }

                                var entry = archive.CreateEntry(entryname);
                                using (var entryStream = entry.Open())
                                {
                                    cascStream.CopyTo(entryStream);
                                }
                            }
                        }
                        catch (FileNotFoundException)
                        {
                            errors.Add("File " + filedataid + " not found in root of buildconfig " + buildConfig + " cdnconfig " + cdnConfig);
                            Logger.WriteLine("File " + filedataid + " not found in root of buildconfig " + buildConfig + " cdnconfig " + cdnConfig);
                        }
                        catch (Exception e)
                        {
                            errors.Add("Error " + e.Message + " occured when getting file " + filedataid);
                            Logger.WriteLine("Error " + e.Message + " occured when getting file " + filedataid + " of buildconfig " + buildConfig + " cdnconfig " + cdnConfig);
                        }
                    }

                    if (errors.Count > 0)
                    {
                        using (var errorStream = new MemoryStream())
                        {
                            var entry = archive.CreateEntry("errors.txt");
                            using (var entryStream = entry.Open())
                            {
                                foreach (var error in errors)
                                {
                                    entryStream.Write(Encoding.UTF8.GetBytes(error + "\n"));
                                }
                            }
                        }
                    }
                }

                return new FileContentResult(zip.ToArray(), "application/octet-stream")
                {
                    FileDownloadName = filename
                };
            }
        }
    }
}
