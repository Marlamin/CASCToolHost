using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
            return "Builds loaded: " + CASC.buildDictionary.Count + "\nFiles indexed: " + CASC.indexDictionary.Count;
        }
    }
}