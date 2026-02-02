using DCElectricWebAPI.Models;
using DCElectricWebAPI.Modules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DCElectricWebAPI.Controllers
{
    /// <summary>
    /// DEPRECATED: This controller used the legacy Intuit QuickBase SDK.
    /// Use the REST API via QuickBaseConnector.Query() instead.
    /// TODO: Refactor to use REST API if this functionality is still needed.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Obsolete("This controller used the deprecated Intuit QuickBase SDK. Needs refactoring to use REST API.")]
    public class StartupController : ControllerBase
    {
        IOptions<QuickBaseSettings> _settings;

        public StartupController(IOptions<QuickBaseSettings> options)
        {
            _settings = options;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return BadRequest("This endpoint is deprecated. The legacy QuickBase SDK has been removed. Please use the REST API endpoints instead.");
        }
    }
}
