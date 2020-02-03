using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CASCToolHost.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CASCToolHost.Controllers
{
    [Route("casc/filetable")]
    [ApiController]
    public class FileTableController : ControllerBase
    {
        public struct DataTablesResult
        {
            public int draw;
            public int recordsFiltered;
            public int recordsTotal;
            public List<List<object>> data;
        }

        public struct DataTablesRow { 
            public uint ID { get; set; }
            public string Filename { get; set; }
            public string Lookup { get; set; }
            public List<FileVersion> Versions { get; set; }
            public string Type { get; set; }
        }

        public struct DBFile
        {
            public uint ID { get; set; }
            public string Lookup { get; set; }
            public string Filename { get; set; }
            public bool Verified { get; set; }
            public string Type { get; set; }
            public string FirstSeen { get; set; }
        }

        public struct FileVersion { }

        [Route("files")]
        public async Task<DataTablesResult> Get(string buildConfig, int draw, int start, int length)
        {
            if (string.IsNullOrEmpty(buildConfig))
            {
                throw new ArgumentException("Invalid arguments!");
            }

            var result = new DataTablesResult
            {
                draw = draw
            };

            Logger.WriteLine("Serving file table data for build " + buildConfig);

            var searching = false;

            if (string.IsNullOrWhiteSpace(Request.Query["search[value]"]))
            {
                Logger.WriteLine("Serving file table data " + start + "," + length + " for build " + buildConfig + " for draw " + draw);
            }
            else
            {
                searching = true;
                Logger.WriteLine("Serving file table data " + start + "," + length + " for build " + buildConfig + " for draw " + draw + " with filter " + Request.Query["search[value]"]);

            }

            var build = await BuildCache.GetOrCreate(buildConfig);
            result.recordsTotal = build.root.entriesFDID.Count;

            result.data = new List<List<object>>();

            result.recordsFiltered = result.recordsTotal;

            var entries = build.root.entriesFDID.ToList();

            if (start + length > entries.Count)
                length = entries.Count - start;

            foreach (var entry in entries.GetRange(start, length))
            {
                var row = new List<object>
                {
                    entry.Value[0].fileDataID
                };

                var dbFile = await Database.GetFileByFileDataID(entry.Value[0].fileDataID);
  
                if(dbFile.ID == 0)
                {
                    Logger.WriteLine("WARNING! File " + entry.Value[0].fileDataID + " is not known in database!");
                }

                // Filename
                if (!string.IsNullOrEmpty(dbFile.Filename))
                {
                    row.Add(dbFile.Filename);
                }
                else
                {
                    row.Add("");
                }

                // Lookup
                if (!string.IsNullOrEmpty(dbFile.Lookup))
                {
                    row.Add(dbFile.Lookup);
                }
                else if(entry.Value[0].lookup != 0)
                {
                    row.Add(entry.Value[0].lookup.ToString("X").ToLower().PadLeft(16, '0'));
                }
                else
                {
                    row.Add("000000000000000");
                }

                // Versions
                row.Add(new List<FileVersion>());

                // Type
                if (!string.IsNullOrEmpty(dbFile.Type))
                {
                    row.Add(dbFile.Type);
                }
                else
                {
                    row.Add("unk");
                }

                result.data.Add(row);
            }

            return result;
        }
    }
}