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
            try
            {
                // Update user with optional password change
                string sql;
                object parameters;

                if (!string.IsNullOrEmpty(userParams.Password))
                {
                    sql = @"UPDATE users SET
                        firstname = @FirstName,
                        lastname = @LastName,
                        password = @Password,
                        email = @Email,
                        phone = @Phone,
                        userlevel = @UserLevel,
                        permissions = @Permissions,
                        status = @Status
                        WHERE userid = @UserId";
                    parameters = new
                    {
                        UserId = id,
                        FirstName = userParams.FirstName,
                        LastName = userParams.LastName,
                        Password = userParams.Password,
                        Email = userParams.Email,
                        Phone = userParams.Phone,
                        UserLevel = userParams.UserLevel,
                        Permissions = userParams.Permissions,
                        Status = userParams.Status
                    };
                }
                else
                {
                    sql = @"UPDATE users SET
                        firstname = @FirstName,
                        lastname = @LastName,
                        email = @Email,
                        phone = @Phone,
                        userlevel = @UserLevel,
                        permissions = @Permissions,
                        status = @Status
                        WHERE userid = @UserId";
                    parameters = new
                    {
                        UserId = id,
                        FirstName = userParams.FirstName,
                        LastName = userParams.LastName,
                        Email = userParams.Email,
                        Phone = userParams.Phone,
                        UserLevel = userParams.UserLevel,
                        Permissions = userParams.Permissions,
                        Status = userParams.Status
                    };
                }

                var rowsAffected = await dl.ExecuteAsync(sql, parameters);

                if (rowsAffected > 0)
                    return Ok(new { success = true, message = "User updated" });
                else
                    return NotFound(new { success = false, message = "User not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, detail = ex.InnerException?.Message });
            }
        }

    }
}
