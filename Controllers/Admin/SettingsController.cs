using DCElectricWebAPI.Models;
using DCElectricWebAPI.Modules;
using Microsoft.AspNetCore.Mvc;

namespace DCElectricWebAPI.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        // Sent to the client in place of the stored token and recognized on
        // save as "unchanged" — the real token never leaves the server.
        private const string TokenMask = "**********";

        private const string KeyHostName = "quickbase.hostname";
        private const string KeyAppId = "quickbase.appid";
        private const string KeyToken = "quickbase.token";

        DataLayerBase dl;
        public SettingsController(DataLayerBase _dl)
        {
            dl = _dl;
        }

        private User? GetAdmin(string sid)
        {
            var executor = dl.Query<User>(
                "SELECT * FROM fn_security_user_by_session_id(@p_session_id)",
                new { p_session_id = sid ?? "" }).FirstOrDefault();
            return executor != null && executor.UserLevel == "Admin" ? executor : null;
        }

        [HttpGet]
        public IActionResult Get([FromQuery] string sid)
        {
            if (GetAdmin(sid) == null) return Unauthorized();

            var rows = dl.Query<SettingRow>("SELECT key, value FROM settings")
                .ToDictionary(r => r.Key, r => r.Value);

            return Ok(new ExternalSettings
            {
                QuickbaseHostName = rows.GetValueOrDefault(KeyHostName, ""),
                QuickbaseAppId = rows.GetValueOrDefault(KeyAppId, ""),
                QuickbaseToken = string.IsNullOrEmpty(rows.GetValueOrDefault(KeyToken, "")) ? "" : TokenMask
            });
        }

        [HttpPut]
        public async Task<IActionResult> Save([FromBody] ExternalSettings settings, [FromQuery] string sid)
        {
            var admin = GetAdmin(sid);
            if (admin == null) return Unauthorized();

            try
            {
                await Upsert(KeyHostName, settings.QuickbaseHostName ?? "", admin.UserId);
                await Upsert(KeyAppId, settings.QuickbaseAppId ?? "", admin.UserId);

                // Password-style semantics: empty or still-masked means unchanged.
                var token = settings.QuickbaseToken ?? "";
                if (token.Length > 0 && token.Trim('*').Length > 0)
                    await Upsert(KeyToken, token, admin.UserId);

                return Ok(new { success = true, message = "Settings saved" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private Task<int> Upsert(string key, string value, int userId)
        {
            return dl.ExecuteAsync(@"INSERT INTO settings (key, value, updateddate, updatedbyid)
                VALUES (@Key, @Value, NOW(), @UserId)
                ON CONFLICT (key) DO UPDATE
                SET value = EXCLUDED.value, updateddate = NOW(), updatedbyid = EXCLUDED.updatedbyid",
                new { Key = key, Value = value, UserId = userId });
        }

        public class SettingRow
        {
            public string Key { get; set; } = "";
            public string Value { get; set; } = "";
        }

        public class ExternalSettings
        {
            public string? QuickbaseHostName { get; set; }
            public string? QuickbaseAppId { get; set; }
            public string? QuickbaseToken { get; set; }
        }
    }
}
