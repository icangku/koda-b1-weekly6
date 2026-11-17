using Ewallet.Dtos.AuditLogs;
using Ewallet.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ewallet.Controllers;

[ApiController]
[Route("api/audit-logs")]
public class AuditLogsController(IAuditLogService service) : ControllerBase
{
    /// <summary>Audit logs, newest first. Filter by action, outcome, actorUserId, transactionId, date range.</summary>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] AuditLogQuery query, CancellationToken ct) => Ok(await service.GetAsync(query, ct));
}
