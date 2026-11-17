using Ewallet.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ewallet.Repositories;

public interface IUnitOfWork
{
  Task<int> SaveChangesAsync(CancellationToken ct = default);
  Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
  /// <summary>Discard everything tracked (used after a rollback so stale changes are not saved later).</summary>
  void ClearChangeTracker();
}

public class UnitOfWork(AppDbContext db) : IUnitOfWork
{
  public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
    => db.Database.BeginTransactionAsync(ct);

  public Task<int> SaveChangesAsync(CancellationToken ct = default)
    => db.SaveChangesAsync(ct);

  public void ClearChangeTracker() => db.ChangeTracker.Clear();
}
