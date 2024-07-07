using Dapper;
using DCElectricWebAPI.Models;
using DCElectricWebAPI.Modules;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Dynamic;

namespace DCElectricWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {

        [HttpGet("{sid}")]       
        public async Task<IActionResult> GetBySession(string sid)
        {

           
            string sql = $"select * from dbo.[fnSecurity_UserBySessionId](@SessionID)";
            using (var dl = new DataLayerBase())
            {
                var parameters = new DynamicParameters();
                parameters.Add("@SessionID",sid, DbType.String);

                var x = dl.Query<User>(sql, parameters);
                return Ok(x);
            };
        }

        [HttpPost]
        public async Task<IActionResult> Post(Credentials creds)
        {

            var dl = new DataLayerBase();
            var sql = "uspLogin";

            var parameters = new DynamicParameters();
            parameters.Add("@LoginName", creds.UserName, DbType.String);
            parameters.Add("@Password", creds.Password, DbType.String);
            parameters.Add("@responseMessage", dbType: DbType.String, size: 250, direction: ParameterDirection.Output);

            dl.Connection.Execute(sql, parameters, commandType: CommandType.StoredProcedure);

            // Get the value of the output parameter
            var ResponseMessage = parameters.Get<string>("@responseMessage");
          
            dynamic retval = new ExpandoObject();

            if (ResponseMessage == "Invalid login")
            {
                retval.response = ResponseMessage;
                retval.status = "fail";
                return Ok(retval);
            }
            if (ResponseMessage == "Incorrect password")
            {
                retval.response = ResponseMessage;
                retval.status = "fail";
                return Ok(retval);
            }
        
                retval.sessionID = ResponseMessage;
                retval.status = "success";
                return Ok(retval);
           





        }
    }
}
