using Ewallet.Data;
using Ewallet.Dtos.Common;
using Ewallet.Dtos.History;
using Ewallet.Models;
using Microsoft.EntityFrameworkCore;

namespace Ewallet.Repositories;

public interface IWalletMutationRepository
{
  void Add(WalletMutation mutation);
  Task<PagedResponse<MutationResponse>> GetHistoryAsync(long walletId, HistoryQuery query, CancellationToken ct = default);
}

public class WalletMutationRepository(AppDbContext db) : IWalletMutationRepository
{
  public void Add(WalletMutation mutation) => db.WalletMutations.Add(mutation);

  public Task<PagedResponse<MutationResponse>> GetHistoryAsync(long walletId, HistoryQuery q, CancellationToken ct = default)
  {
    var query = db.WalletMutations.AsNoTracking().Where(m => m.WalletId == walletId);

    if (!string.IsNullOrWhiteSpace(q.Search))
    {
      var search = q.Search.Trim();
      query = query.Where(m => m.Transaction.ReferenceNo.Contains(search));
    }
    if (q.Type is { } type) query = query.Where(m => m.Transaction.Type == type);
    if (q.Status is { } status) query = query.Where(m => m.Transaction.Status == status);
    if (q.Direction is { } direction) query = query.Where(m => m.Direction == direction);
    if (q.From is { } from) query = query.Where(m => m.CreatedAtUtc >= from);
    if (q.To is { } to)
    {
      var (bound, exclusive) = QueryableExtensions.UpperBound(to);
      query = exclusive ? query.Where(m => m.CreatedAtUtc < bound) : query.Where(m => m.CreatedAtUtc <= bound);
    }

    // default order matches index IX_WalletMutations_WalletId_CreatedAtUtc_Id (WalletId, CreatedAtUtc DESC, Id DESC)
    var desc = q.SortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);
    query = (q.SortBy.ToLowerInvariant(), desc) switch
    {
      ("amount", true) => query.OrderByDescending(m => m.Amount).ThenByDescending(m => m.Id),
      ("amount", false) => query.OrderBy(m => m.Amount).ThenBy(m => m.Id),
      (_, true) => query.OrderByDescending(m => m.CreatedAtUtc).ThenByDescending(m => m.Id),
      (_, false) => query.OrderBy(m => m.CreatedAtUtc).ThenBy(m => m.Id),
    };

    return query
      .Select(m => new MutationResponse(
        m.Id, m.TransactionId, m.Transaction.ReferenceNo, m.Transaction.Type,
        m.Direction, m.Amount, m.BalanceBefore, m.BalanceAfter, m.CreatedAtUtc))
      .ToPagedAsync(q.Page, q.PageSize, ct);
  }
}
