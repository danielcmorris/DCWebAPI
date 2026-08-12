using DCElectricWebAPI.Modules;
using Microsoft.AspNetCore.Mvc;

namespace DCElectricWebAPI.Controllers
{
    [ApiController]
    public class SupportController : ControllerBase
    {
        private readonly OpenProjectService _openProject;
        private readonly DataLayerBase _dl;

        public SupportController(OpenProjectService openProject, DataLayerBase dl)
        {
            _openProject = openProject;
            _dl = dl;
        }

        [Route("api/support/tickets/{status}")]
        [HttpGet]
        public async Task<IActionResult> GetTickets([FromHeader] string Authorization, string status)
        {
            var um = new UserModule(Authorization, _dl);
            if (!um.Secured) return Unauthorized();

            var statusOperator = status?.ToLower() switch
            {
                "open" => "o",
                "closed" => "c",
                _ => null
            };
            if (statusOperator == null) return BadRequest("status must be 'open' or 'closed'");

            var tickets = await _openProject.GetTickets(statusOperator);
            return Ok(tickets);
        }
    }
}
