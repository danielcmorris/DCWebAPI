using Dapper;
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

        private readonly DataLayerBase _db;
        public AppController(DataLayerBase db)
        {
            _db = db;
        }
        [Route("api/app/test")]
        [HttpGet]
        public IActionResult Test()
        {
            var connStr = _db._connectionString;
            var dict = connStr.Split(';')
            .Select(part => part.Split('=', 2))
            .Where(split => !split[0].Trim().Equals("password", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(split => split[0].Trim(), split => split[1].Trim());
            try
            {
                var sql = "SELECT 1";
                var result = _db.Connection.ExecuteScalar<int>(sql);
                dict["DatabaseConnection"] = "Success";

            }catch(Exception ex)
            {
                dict["DatabaseConnection"] = "Failed: " + ex.Message  ;
            }

            return Ok(dict);
        }



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
