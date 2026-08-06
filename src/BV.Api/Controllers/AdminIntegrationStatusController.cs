using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BV.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/integrations")]
public sealed class AdminIntegrationStatusController(IConfiguration configuration) : ControllerBase
{
    [HttpGet("status")]
    public IActionResult Status()
    {
        var netGsmEnabled = configuration.GetValue<bool>("NetGsm:Enabled");
        var netGsmConfigured = netGsmEnabled
            && HasValue("NetGsm:UserCode")
            && HasValue("NetGsm:Password")
            && HasValue("NetGsm:Header");

        var emailEnabled = configuration.GetValue<bool>("Email:Enabled");
        var emailConfigured = emailEnabled
            && HasValue("Email:Host")
            && configuration.GetValue<int>("Email:Port") > 0
            && HasValue("Email:FromAddress");

        var mikroEnabled = configuration.GetValue<bool>("Mikro:Enabled");
        var mikroConfigured = mikroEnabled
            && HasValue("Mikro:ConnectionString")
            && HasValue("Mikro:CompanyCode");

        return Ok(new
        {
            netGsm = BuildStatus(netGsmEnabled, netGsmConfigured, "SMS"),
            email = BuildStatus(emailEnabled, emailConfigured, "E-posta"),
            mikro = BuildStatus(mikroEnabled, mikroConfigured, "Mikro ERP")
        });
    }

    private bool HasValue(string key) => !string.IsNullOrWhiteSpace(configuration[key]);

    private static object BuildStatus(bool enabled, bool configured, string name) => new
    {
        name,
        enabled,
        configured,
        ready = enabled && configured,
        message = !enabled
            ? "Devre dışı"
            : configured
                ? "Kullanıma hazır"
                : "Eksik yapılandırma"
    };
}
