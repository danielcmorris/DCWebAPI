using DCElectricWebAPI.Modules;
using Intuit.QuickBase.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DCElectricWebAPI.Modules;
using DCElectricWebAPI.Models;
using Microsoft.Extensions.Options;

namespace DCElectricWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FixtureInvoiceController : ControllerBase
    {

        IOptions<QuickBaseSettings> _settings;


        [HttpGet]
        public async Task<IActionResult> Get(int id = 0, string template = "", string referenceCode = "", string sid = "")
        {

            var app = new QuickBaseConnector(_settings);
            


            return Ok();


        }
    }
}
