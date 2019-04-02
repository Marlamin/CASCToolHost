using Microsoft.AspNetCore.Mvc;
using System;

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