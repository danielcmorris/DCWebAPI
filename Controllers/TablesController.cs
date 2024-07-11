using DCElectricWebAPI.Models;
using DCElectricWebAPI.Modules;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Threading.Tasks.Dataflow;

namespace DCElectricWebAPI.Controllers
{ 
    [ApiController]
    public class TablesController : ControllerBase
    {
        IOptions<QuickBaseSettings> _settings;
        [Route("api/tables/{appId}")]
        [HttpGet]
        public async Task<IActionResult> Get(string appId)
        {
            var obj = new QuickBaseConnector(_settings);
            var retval = await obj.GetTables(appId);
            return Ok(retval);
        }

        [Route("api/app/{appId}/table/{tableId}")]
        [HttpGet]
        public async Task<IActionResult> Get(string appId,string tableId)
        {
            var obj = new QuickBaseConnector(_settings);
            var retval = await obj.GetTables(appId);
            var customers = retval.Find(obj=>obj.id == tableId);

            return Ok(customers);
        }
        //[HttpGet]
        //[Route("api/tables/{appId}")]
        //public async Task<IActionResult> Get(string appId, string tableId, string record)
        //{
        //    var obj = new QuickBaseConnector(_settings);
        //    var retval = await obj.GetApp(appId);
        //    return Ok(retval);
        //}
    }
}
