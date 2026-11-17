using Ewallet.Data;
using Ewallet.Dtos.AuditLogs;
using Ewallet.Dtos.Common;
using Ewallet.Models;
using Microsoft.EntityFrameworkCore;

namespace Ewallet.Repositories;

public interface IAuditLogRepository
{
  void Add(AuditLog log);
  Task<PagedResponse<AuditLogResponse>> GetPagedAsync(AuditLogQuery query, CancellationToken ct = default);
}

public class AuditLogRepository(AppDbContext db) : IAuditLogRepository
{
  public void Add(AuditLog log) => db.AuditLogs.Add(log);

  public Task<PagedResponse<AuditLogResponse>> GetPagedAsync(AuditLogQuery q, CancellationToken ct = default)
  {
    var query = db.AuditLogs.AsNoTracking().AsQueryable();

    if (!string.IsNullOrWhiteSpace(q.Action))
    {
      var action = q.Action.Trim().ToUpperInvariant();
      query = query.Where(a => a.Action == action);
    }
    if (q.Outcome is { } outcome) query = query.Where(a => a.Outcome == outcome);
    if (q.ActorUserId is { } actor) query = query.Where(a => a.ActorUserId == actor);
    if (q.TransactionId is { } trx) query = query.Where(a => a.TransactionId == trx);
    if (q.From is { } from) query = query.Where(a => a.CreatedAtUtc >= from);
    if (q.To is { } to)
    {
      var (bound, exclusive) = QueryableExtensions.UpperBound(to);
      query = exclusive ? query.Where(a => a.CreatedAtUtc < bound) : query.Where(a => a.CreatedAtUtc <= bound);
    }

    return query
      .OrderByDescending(a => a.CreatedAtUtc).ThenByDescending(a => a.Id)
      .Select(a => new AuditLogResponse(
        a.Id, a.ActorUserId, a.ActorUser != null ? a.ActorUser.Username : null, a.TransactionId,
        a.Action, a.EntityType, a.EntityId, a.Outcome, a.Description, a.CorrelationId, a.CreatedAtUtc))
      .ToPagedAsync(q.Page, q.PageSize, ct);
  }
}
