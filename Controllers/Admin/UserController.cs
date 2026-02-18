using Dapper;
using DCElectricWebAPI.Models;
using DCElectricWebAPI.Modules;
using Microsoft.AspNetCore.Mvc;

namespace DCElectricWebAPI.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {


        DataLayerBase dl;
        public UserController(DataLayerBase _dl)
        {
            dl = _dl;
        }
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            string sql = "SELECT userid, firstname, lastname, email, phone, userlevel, permissions, status FROM users WHERE status <> 'Deleted'";
           
            var x = dl.Query<User>(sql);

            return Ok(x);

        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            string sql = $"SELECT userid, firstname, lastname, email, phone, userlevel, permissions, status FROM users WHERE status <> 'Deleted' AND userid = {id}";
          
                var x = dl.Query<User>(sql);
                return Ok(x);
           
           

            

        }

        [HttpPost]
        public async Task<IActionResult> AddUser(User userParams, [FromQuery] string sid)
        {
 
            var ResponseMessage = dl.Connection.ExecuteScalar<string>(
                "SELECT usp_add_user(@Login, @Password, @FirstName, @LastName, @UserLevel, @Permissions, @SessionID)",
                new
                {
                    Login = userParams.Email,
                    Password = userParams.Password,
                    FirstName = userParams.FirstName,
                    LastName = userParams.LastName,
                    UserLevel = userParams.UserLevel,
                    Permissions = userParams.Permissions,
                    SessionID = sid
                });


            return Ok(ResponseMessage);

        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser([FromBody] User userParams, int id, [FromQuery] string sid)
        {


          
                var user = await dl.Connection.QuerySingleAsync<User>(
                    "SELECT * FROM usp_update_user(@UserID, @FirstName, @LastName, @Password, @Email, @Phone, @UserLevel, @Permissions, @Status, @SessionID)",
                    new
                    {
                        UserID = id,
                        FirstName = userParams.FirstName,
                        LastName = userParams.LastName,
                        Password = userParams.Password,
                        Email = userParams.Email,
                        Phone = userParams.Phone,
                        UserLevel = userParams.UserLevel,
                        Permissions = userParams.Permissions,
                        Status = userParams.Status,
                        SessionID = sid
                    });


                return Ok(user);
             
        }

    }
}
