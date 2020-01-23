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
            public List<List<string>> data;
        }

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

            result.data = new List<List<string>>();

            var resultCount = 0;

            if (searching)
            {
                //var filenameMap = Database.GetKnownLookups();

                foreach (var entry in build.root.entriesFDID)
                {
                    var matches = false;
                    var row = new List<string>
                    {
                        entry.Value[0].fileDataID.ToString()
                    };

                    if (entry.Value[0].fileDataID == Request.Query["search[value]"])
                    {
                        matches = true;
                    }

                    /*
                     * if (filenameMap.TryGetValue(entry.Key, out var filename))
                    {
                        row.Add(filename);
                        if (filename.Contains(Request.Query["search[value]"]))
                        {
                            matches = true;
                        }
                    }
                    else
                    {*/
                    row.Add("");
                    /*}*/

                    row.Add(entry.Key.ToString("x"));
                    row.Add("");
                    row.Add("unk");
                    row.Add(""); // soundkits
                    row.Add("");
                    if (matches)
                    {
                        result.data.Add(row);
                        resultCount++;
                    }
                }

                result.recordsFiltered = resultCount;

                var takeLength = length;
                if ((start + length) > resultCount)
                {
                    takeLength = resultCount - start;
                }

                result.data = result.data.GetRange(start, takeLength);
            }
            else
            {
                result.recordsFiltered = result.recordsTotal;

                var entries = build.root.entriesFDID.ToList();
                foreach (var entry in entries.GetRange(start, length))
                {
                    var row = new List<string>
                    {
                        entry.Value[0].fileDataID.ToString()
                    };

                    var filename = await Database.GetFilenameByFileDataID(entry.Value[0].fileDataID);

                    if (!string.IsNullOrEmpty(filename))
                    {
                        row.Add(filename);
                    }
                    else
                    {
                        row.Add("");
                    }

                    row.Add(entry.Key.ToString("x"));
                    row.Add("");
                    row.Add("unk");
                    row.Add(""); // soundkits
                    row.Add("");
                    result.data.Add(row);
                }
            }

            return result;
        }
    }
}