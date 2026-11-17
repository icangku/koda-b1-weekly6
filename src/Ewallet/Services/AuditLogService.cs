using Ewallet.Dtos.AuditLogs;
using Ewallet.Dtos.Common;
using Ewallet.Models;
using Ewallet.Repositories;

namespace Ewallet.Services;

public record AuditEntry(
    string Action,
    AuditOutcome Outcome,
    string EntityType,
    string? EntityId,
    long? ActorUserId,
    long? TransactionId,
    string? Description);

public interface IAuditLogService
{
  /// <summary>Adds the log to the current unit of work; it is saved (or rolled back) together with the business change.</summary>
  void Record(AuditEntry entry);
  /// <summary>Writes the log on its own, outside any rolled-back transaction (for rejected/failed operations).</summary>
  Task RecordIndependentAsync(AuditEntry entry);
  Task<PagedResponse<AuditLogResponse>> GetAsync(AuditLogQuery query, CancellationToken ct = default);
}

public class AuditLogService(IAuditLogRepository logs, IUnitOfWork uow, IHttpContextAccessor http) : IAuditLogService
{
  public void Record(AuditEntry e) => logs.Add(new AuditLog
  {
    Action = e.Action,
    Outcome = e.Outcome,
    EntityType = e.EntityType,
    EntityId = e.EntityId,
    ActorUserId = e.ActorUserId,
    TransactionId = e.TransactionId,
    // Description must only contain safe text: no email/phone/password/token
    Description = e.Description is { Length: > 500 } d ? d[..500] : e.Description,
    CorrelationId = http.HttpContext?.TraceIdentifier,
    CreatedAtUtc = DateTime.UtcNow,
  });

  public async Task RecordIndependentAsync(AuditEntry entry)
  {
    uow.ClearChangeTracker();          // drop the rolled-back entities so they are not re-saved
    Record(entry);
    await uow.SaveChangesAsync(CancellationToken.None);   // persist even if the request was cancelled
  }

  public Task<PagedResponse<AuditLogResponse>> GetAsync(AuditLogQuery query, CancellationToken ct = default)
    => logs.GetPagedAsync(query, ct);
}
