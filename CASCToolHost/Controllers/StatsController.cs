using CASCToolHost.Utils;
using Microsoft.AspNetCore.Mvc;
using System;

namespace CASCToolHost.Controllers
{
    [Route("casc/stats")]
    [ApiController]
    public class StatsController : ControllerBase
    {
        [HttpGet]
        public string Get()
        {
            Console.WriteLine("[" + DateTime.Now + "] Serving stats!");
            return "Builds loaded: " + RootCache.Count() + "\nIndexes loaded: " + CASC.indexNames.Count + "\nFiles indexed: " + CASC.indexDictionary.Count + "\nEncoding mappings: " + NGDP.encodingDictionary.Count;
        }
    }
}