using Microsoft.AspNetCore.Mvc;

namespace CASCToolHost.Controllers
{
    [Route("casc/install")]
    [ApiController]
    public class InstallController : Controller
    {
        [Route("dump")]
        public ActionResult DumpByHash(string hash)
        {
            var install = NGDP.GetInstall("http://cdn.blizzard.com/tpr/wow/", hash, true);
            return Json(install.entries);
        }
    }
}