using Dapper;
using DCElectricWebAPI.Models;
using DCElectricWebAPI.Modules;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlTypes;
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

            string sql = $"select * from dbo.[fnSecurity_UserBySessionId](@SessionID)";
           
                var parameters = new DynamicParameters();
                parameters.Add("@SessionID", sid, DbType.String);

                var x = dl.Query<User>(sql, parameters);
                return Ok(x);
          
        }

        [HttpPost]
        public async Task<IActionResult> Post(Credentials creds)
        {
            
          
            var sql = "uspLogin";

            //sqlcmd - S 127.0.0.1,1433 - U apiserver - P "Educated5-Wham-Dentist-Cover" - d DCE

            var parameters = new DynamicParameters();
            parameters.Add("@LoginName", creds.UserName, DbType.String);
            parameters.Add("@Password", creds.Password, DbType.String);
            parameters.Add("@responseMessage", dbType: DbType.String, size: 250, direction: ParameterDirection.Output);
            try
            {
             
                dl.Connection.ExecuteScalar(sql, parameters, commandType: CommandType.StoredProcedure);
            }catch(Exception ex)
            {
                dynamic retval2 = new ExpandoObject();
                retval2.response = ex.Message;
                retval2.status = "error";
                return Ok(retval2);
                return StatusCode(500, retval2 + dl._connectionString.Replace("Educated5-Wham-Dentist-Cover", "--PASSWORD--"));
            }


            // Get the value of the output parameter
            var ResponseMessage = parameters.Get<string>("@responseMessage");

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