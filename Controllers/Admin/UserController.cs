using Dapper;
using DCElectricWebAPI.Models;
using DCElectricWebAPI.Modules;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Reflection.PortableExecutable;

namespace DCElectricWebAPI.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            string sql = "Select UserID, FirstName, LastName, Email, Phone, UserLevel,Permissions,Status from [User] Where Status<>'Deleted'";
            var dl = new DataLayerBase();
            var x = dl.Query<User>(sql);

            return Ok(x);

        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            string sql = $"Select UserID, FirstName, LastName, Email, Phone, UserLevel, Permissions,Status from [User] Where Status<>'Deleted' and UserID={id}";
            using (var dl = new DataLayerBase())
            {
                var x = dl.Query<User>(sql);
                return Ok(x);
            } ;
           

            

        }

        [HttpPost]
        public async Task<IActionResult> AddUser(User userParams, [FromQuery] string sid)
        {

            var dl = new DataLayerBase();
            var sql = "uspAddUser";

            var parameters = new DynamicParameters();
            parameters.Add("@Login", userParams.Email, DbType.String);
            parameters.Add("@Password", userParams.Password, DbType.String);
            parameters.Add("@FirstName", userParams.FirstName, DbType.String);
            parameters.Add("@LastName", userParams.LastName, DbType.String);
            parameters.Add("@UserLevel", userParams.UserLevel, DbType.String);
            parameters.Add("@Permissions", userParams.Permissions, DbType.String);
            parameters.Add("@SessionID", sid, DbType.String);
            parameters.Add("@responseMessage", dbType: DbType.String, size: 250, direction: ParameterDirection.Output);

            dl.Connection.Execute("dbo.uspAddUser", parameters, commandType: CommandType.StoredProcedure);

            // Get the value of the output parameter
            var ResponseMessage = parameters.Get<string>("@responseMessage");


            return Ok(ResponseMessage);

        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser([FromBody] User userParams, int id, [FromQuery] string sid)
        {


            using (var dl = new DataLayerBase())
            {
                var sql = "uspUpdateUser";

                var parameters = new DynamicParameters();
                parameters.Add("@UserID", id, DbType.Int64);
                parameters.Add("@Email", userParams.Email, DbType.String);
                parameters.Add("@FirstName", userParams.FirstName, DbType.String);
                parameters.Add("@LastName", userParams.LastName, DbType.String);
                parameters.Add("@UserLevel", userParams.UserLevel, DbType.String);
                parameters.Add("@Phone", userParams.Phone, DbType.String);
                parameters.Add("@Permissions", userParams.Permissions, DbType.String);
                parameters.Add("@Status", userParams.Status, DbType.String);
                parameters.Add("@SessionID", sid, DbType.String);

                var user = await dl.Connection.QuerySingleAsync<User>(sql, parameters, commandType: CommandType.StoredProcedure);


                return Ok(user);
            }
        }

    }
}
