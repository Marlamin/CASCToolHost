﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CASCToolHost.Controllers
{
    [Route("casc/encoding")]
    [ApiController]
    public class EncodingController : ControllerBase
    {
        [Route("dump")]
        public FileContentResult Dump()
        {
            var bytes = new List<byte>();

            foreach(var entry in CASC.buildDictionary)
            {
                foreach(var encodingEntry in entry.Value.encoding.aEntries)
                {
                    bytes.AddRange(Encoding.ASCII.GetBytes(encodingEntry.Value.eKey.ToHexString() + " " + encodingEntry.Key.ToHexString() + "\n"));
                }
            }
            return new FileContentResult(bytes.ToArray(), "application/octet-stream")
            {
                FileDownloadName = "dump.txt"
            };
        }
    }
}