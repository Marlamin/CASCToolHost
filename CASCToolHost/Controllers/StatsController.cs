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
            return "Builds loaded: " + CASC.buildDictionary.Count + "\nIndexes loaded: " + CASC.indexNames.Count + "\nFiles indexed: " + CASC.indexDictionary.Count;
        }
    }
}