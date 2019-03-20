using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CASCToolHost.Controllers
{
    [Route("casc/reloadsettings")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        [HttpGet]
        public string Get()
        {
            Console.WriteLine("[" + DateTime.Now + "] Reloading settings!");
            SettingsManager.LoadSettings();
            return "Reloaded settings!";
        }
    }
}