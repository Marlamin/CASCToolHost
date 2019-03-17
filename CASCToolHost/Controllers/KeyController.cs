using System;
using Microsoft.AspNetCore.Mvc;

namespace CASCToolHost.Controllers
{
    [Route("casc/reloadkeys")]
    [ApiController]
    public class KeyController : ControllerBase
    {
        [HttpGet]
        public string Get()
        {
            Console.WriteLine("[" + DateTime.Now + "] Reloading keys!");
            KeyService.LoadKeys();
            return "Reloaded keys!";
        }
    }
}