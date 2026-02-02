using DCElectricWebAPI.Models;
using DCElectricWebAPI.Modules;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Threading.Tasks.Dataflow;

namespace DCElectricWebAPI.Controllers
{
    [ApiController]
    public class TablesController : ControllerBase
    {
        IOptions<QuickBaseSettings> _settings;

        public TablesController(IOptions<QuickBaseSettings> options)
        {
            _settings = options;
        }

        [Route("api/tables/{appId}")]
        [HttpGet]
        public async Task<IActionResult> Get(string appId)
        {
            var obj = new QuickBaseConnector(_settings);
            var retval = await obj.GetTables(appId);
            return Ok(retval);
        }

        [Route("api/app/{appId}/table/{tableId}")]
        [HttpGet]
        public async Task<IActionResult> Get(string appId,string tableId)
        {
            var obj = new QuickBaseConnector(_settings);
            var retval = await obj.GetTables(appId);
            var customers = retval.Find(obj=>obj.id == tableId);

            return Ok(customers);
        }

        /// <summary>
        /// Get all fields for a table
        /// </summary>
        [Route("api/fields/{tableId}")]
        [HttpGet]
        public async Task<IActionResult> GetFields(string tableId)
        {
            var obj = new QuickBaseConnector(_settings);
            var retval = await obj.GetFields(tableId);
            return Ok(retval);
        }

        /// <summary>
        /// Get fields formatted as markdown table for documentation
        /// </summary>
        [Route("api/fields/{tableId}/markdown")]
        [HttpGet]
        public async Task<IActionResult> GetFieldsMarkdown(string tableId)
        {
            var obj = new QuickBaseConnector(_settings);
            var fields = await obj.GetFields(tableId);

            if (fields == null || !fields.Any())
            {
                return NotFound($"No fields found for table {tableId}");
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"## Table: {tableId}");
            sb.AppendLine();
            sb.AppendLine("| Field ID | Label | Type | Mode | Default | Required |");
            sb.AppendLine("|----------|-------|------|------|---------|----------|");

            foreach (var field in fields.OrderBy(f => f.id))
            {
                var defaultVal = field.appearsByDefault ? "Yes" : "No";
                var required = field.required ? "Yes" : "No";
                var label = field.label?.Replace("|", "\\|") ?? "";
                sb.AppendLine($"| {field.id} | {label} | {field.fieldType} | {field.mode} | {defaultVal} | {required} |");
            }
            
            return Content(sb.ToString(), "text/markdown");
        }
        //[HttpGet]
        //[Route("api/tables/{appId}")]
        //public async Task<IActionResult> Get(string appId, string tableId, string record)
        //{
        //    var obj = new QuickBaseConnector(_settings);
        //    var retval = await obj.GetApp(appId);
        //    return Ok(retval);
        //}
    }
}
