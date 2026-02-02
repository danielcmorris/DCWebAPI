using DCElectricWebAPI.Models;
using DCElectricWebAPI.Modules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DCElectricWebAPI.Controllers
{
    /// <summary>
    /// DEPRECATED: This controller used the legacy Intuit QuickBase SDK.
    /// Use StreetlightsFixtureController instead which uses the REST API.
    /// </summary>
    [ApiController]
    [Obsolete("This controller used the deprecated Intuit QuickBase SDK. Use StreetlightsFixtureController instead.")]
    public class FixtureController : ControllerBase
    {
        IOptions<QuickBaseSettings> _settings;

        public FixtureController(IOptions<QuickBaseSettings> options)
        {
            _settings = options;
        }

        [HttpGet]
        [Route("api/fixture")]
        public IActionResult Get(string customer, string pricelevel)
        {
            return BadRequest("This endpoint is deprecated. Use /api/StreetlightsFixture/fixtures instead, which uses the REST API.");
        }
    }
}
