using DCElectricWebAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections;

namespace DCElectricWebAPI.Controllers
{
    [Route("api/[controller]")]
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



    }
}
