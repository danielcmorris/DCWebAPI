using DCElectricWebAPI.Models;
using DCElectricWebAPI.Moduiles;
using DCElectricWebAPI.Modules;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace DCElectricWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] LoginCredentials credentials)
        {


            var sec = new SecurityModule();
            Auth0AccessToken retval =sec.Auth0(credentials);


            return Ok(retval);


        }
    }
}
