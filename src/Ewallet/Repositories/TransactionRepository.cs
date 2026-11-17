using Ewallet.Data;
using Ewallet.Dtos.Common;
using Ewallet.Dtos.History;
using Ewallet.Models;
using Microsoft.EntityFrameworkCore;

namespace Ewallet.Repositories;

public interface ITransactionRepository
{
  void Add(Transaction transaction);
  Task<PagedResponse<TransactionResponse>> GetHistoryAsync(long walletId, HistoryQuery query, CancellationToken ct = default);
}

public class TransactionRepository(AppDbContext db) : ITransactionRepository
{
  public void Add(Transaction transaction) => db.Transactions.Add(transaction);

  public Task<PagedResponse<TransactionResponse>> GetHistoryAsync(long walletId, HistoryQuery q, CancellationToken ct = default)
  {
    // read-only: AsNoTracking + projection, so EF does not build change-tracking snapshots
    var query = db.Transactions.AsNoTracking()
      .Where(t => t.SenderWalletId == walletId || t.ReceiverWalletId == walletId);

    if (!string.IsNullOrWhiteSpace(q.Search))
    {
      var search = q.Search.Trim();
      query = query.Where(t => t.ReferenceNo.Contains(search));
    }
    if (q.Type is { } type) query = query.Where(t => t.Type == type);
    if (q.Status is { } status) query = query.Where(t => t.Status == status);
    if (q.Direction == MutationDirection.Out) query = query.Where(t => t.SenderWalletId == walletId);
    if (q.Direction == MutationDirection.In) query = query.Where(t => t.ReceiverWalletId == walletId);
    if (q.From is { } from) query = query.Where(t => t.CreatedAtUtc >= from);
    if (q.To is { } to)
    {
      var (bound, exclusive) = QueryableExtensions.UpperBound(to);
      query = exclusive ? query.Where(t => t.CreatedAtUtc < bound) : query.Where(t => t.CreatedAtUtc <= bound);
    }

    var desc = q.SortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);
    query = (q.SortBy.ToLowerInvariant(), desc) switch
    {
      ("amount", true) => query.OrderByDescending(t => t.Amount).ThenByDescending(t => t.Id),
      ("amount", false) => query.OrderBy(t => t.Amount).ThenBy(t => t.Id),
      (_, true) => query.OrderByDescending(t => t.CreatedAtUtc).ThenByDescending(t => t.Id),
      (_, false) => query.OrderBy(t => t.CreatedAtUtc).ThenBy(t => t.Id),
    };

    return query
      .Select(t => new TransactionResponse(
        t.Id,
        t.ReferenceNo,
        t.Type,
        t.Status,
        t.SenderWalletId == walletId ? MutationDirection.Out : MutationDirection.In,
        t.Amount,
        t.SenderWallet != null ? t.SenderWallet.WalletNumber : null,
        t.ReceiverWallet.WalletNumber,
        t.Description,
        t.CreatedAtUtc,
        t.CompletedAtUtc))
      .ToPagedAsync(q.Page, q.PageSize, ct);
  }
}
