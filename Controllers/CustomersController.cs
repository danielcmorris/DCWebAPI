using DCElectricWebAPI.Models;
using DCElectricWebAPI.Modules;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Collections;


namespace DCElectricWebAPI.Controllers
{

    [ApiController]
    public class CustomersController : ControllerBase
    {
        IOptions<QuickBaseSettings> _settings;
        string strMaintPriceTableId = "bjrvqd35a";
        Hashtable htPriceLevel = new Hashtable();
        Hashtable htFixturePrice = new Hashtable();


        public CustomersController(IOptions<QuickBaseSettings> options)
        {

            _settings = options;

        }

        [Route("api/customers")]
        [HttpGet]
        public async Task<IActionResult> Get([FromHeader]string Authorization)
        {
             
            var um = new UserModule(Authorization);
            if (!um.Secured) return Unauthorized();

            var obj = new QuickBaseConnector(_settings);
            var q = new QuickBaseLibrary.QBQuery();

            //q.from = "bjrvqd33q"; //bhrnewemu 
            q.from = "bhrnewemu";
            q.select = new List<int>() { 3, 4, 5, 6, 7, 8, 9, 10 };


            var retval = await obj.Query(q);
            var customers = new List<Customer>();

            foreach (var item in retval.data)
            {
      

                JObject o = item;
                var c = new Customer();
                c.CustomerId = (int)o.GetValue("3")["value"];
                c.CustomerName = o.GetValue("6")["value"].ToString();
                c.Address = o.GetValue("7")["value"].ToString();
                c.City = o.GetValue("8")["value"].ToString();
                c.State = o.GetValue("9")["value"].ToString();
                c.ZipCode = o.GetValue("10")["value"].ToString();


                customers.Add(c);
            }


            return Ok(customers);
        }

    }
}
