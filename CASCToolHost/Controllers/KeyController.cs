using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CASCToolHost.Controllers
{
    [Route("casc/reloadkeys")]
    [ApiController]
    public class KeyController : ControllerBase
    {
        [HttpGet]
        public async Task<string> Get()
        {
            Console.WriteLine("[" + DateTime.Now + "] Reloading keys!");
            await KeyService.LoadKeys();
            return "Reloaded keys!";
        }
    }
}