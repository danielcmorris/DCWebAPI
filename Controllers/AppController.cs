using DCElectricWebAPI.Models;
using DCElectricWebAPI.Modules;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DCElectricWebAPI.Controllers
{
   
    [ApiController]
    public class AppController : ControllerBase
    {
        IOptions<QuickBaseSettings> _settings;
        [Route("api/app")]
        [HttpGet]
        public async Task<IActionResult> Get(string id)
        {             
            var obj = new QuickBaseConnector(_settings);
            var retval = await obj.GetApp(id);            
            return Ok(retval);
        }
    }
   
}
