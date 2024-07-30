using DCElectricWebAPI.Models;
using DCElectricWebAPI.Modules;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using static DCElectricWebAPI.Models.QuickBaseLibrary;

namespace DCElectricWebAPI.Controllers
{
    [ApiController]
    public class QueryController : ControllerBase
    {
        IOptions<QuickBaseSettings> _settings;
        [Route("api/tables/query")]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] QBQuery q)
        {
            var obj = new QuickBaseConnector(_settings);
            var retval = await obj.Query(q);

            var fields = retval.fields;
            var results = new List<Customer>();
            foreach (var dr in retval.data)
            {
                var jo = new Customer();

                foreach (var field in fields)
                {
                    var name = field.label.Replace(" ", "").Replace("#", "").ToLower();
                    var value = dr[field.id.ToString()]["value"].ToString();

                    if (name == "customername") jo.CustomerName = value;

                }

                results.Add(jo);
            }

            //var x = Newtonsoft.Json.JsonConvert.SerializeObject(results);
            //var customers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Customer>>(x);

            return Ok(results);
        }
    }


    //public class Customer
    //{
    //    public string customerName { get; set; }
    //}

}
