using Dapper;
using DCElectricWebAPI.Models;
using DCElectricWebAPI.Modules;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Dynamic;

namespace DCElectricWebAPI.Controllers
{



    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {

        DataLayerBase dl;

        public LoginController(DataLayerBase _dl)
        {
            dl = _dl;
        }


        [HttpGet("{sid}")]
        public async Task<IActionResult> GetBySession(string sid)
        {

            string sql = "SELECT * FROM fn_security_user_by_session_id(@p_session_id)";

                var parameters = new DynamicParameters();
                parameters.Add("@p_session_id", sid, DbType.String);

                var x = dl.Query<User>(sql, parameters);
                return Ok(x);
          
        }

        [HttpPost]
        public async Task<IActionResult> Post(Credentials creds)
        {
            
          
            string ResponseMessage;
            try
            {
                ResponseMessage = dl.Connection.ExecuteScalar<string>(
                    "SELECT usp_login(@p_login_name, @p_password)",
                    new { p_login_name = creds.UserName, p_password = creds.Password });
            }
            catch (Exception ex)
            {
                dynamic retval2 = new ExpandoObject();
                retval2.response = ex.Message;
                retval2.status = "error";
                return Ok(retval2);
            }

            dynamic retval = new ExpandoObject();

            if (ResponseMessage == "Invalid login")
            {
                retval.response = ResponseMessage;
                retval.status = "fail";
                return Unauthorized(retval);
            }
            if (ResponseMessage == "Incorrect password")
            {
                retval.response = ResponseMessage;
                retval.status = "fail";
                return Unauthorized(retval);
            }

            retval.sessionID = ResponseMessage;
            retval.status = "success";
            return Ok(retval);
        }
    }
}