using Ewallet.Data;
using Ewallet.Dtos.Users;
using Ewallet.Models;
using Microsoft.EntityFrameworkCore;

namespace Ewallet.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, long? excludeUserId = null, CancellationToken ct = default);
    Task<bool> PhoneExistsAsync(string phone, long? excludeUserId = null, CancellationToken ct = default);
    void Add(User user);

    Task<List<UserResponse>> GetAllAsync(CancellationToken ct = default);
    Task<UserResponse?> GetResponseByIdAsync(long id, CancellationToken ct = default);
}

public class UserRepository(AppDbContext db) : IUserRepository
{
  public Task<User?> GetByIdAsync(long id, CancellationToken ct = default)
      => db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

  public Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default)
      => db.Users.AnyAsync(u => u.Username == username, ct);

  public Task<bool> EmailExistsAsync(string email, long? excludeUserId = null, CancellationToken ct = default)
      => db.Users.AnyAsync(u => u.Email == email && (excludeUserId == null || u.Id != excludeUserId), ct);

  public Task<bool> PhoneExistsAsync(string phone, long? excludeUserId = null, CancellationToken ct = default)
      => db.Users.AnyAsync(u => u.Phone == phone && (excludeUserId == null || u.Id != excludeUserId), ct);

  public void Add(User user) => db.Users.Add(user);

  public Task<List<UserResponse>> GetAllAsync(CancellationToken ct = default)
      => db.Users.AsNoTracking()
          .OrderBy(u => u.Id)
          .Select(u => new UserResponse(
              u.Id, u.Username, u.FullName, u.Email, u.Phone, u.Status,
              u.Wallet != null ? u.Wallet.Id : null,
              u.Wallet != null ? u.Wallet.WalletNumber : null,
              u.Wallet != null ? u.Wallet.Balance : 0m))
          .ToListAsync(ct);

  public Task<UserResponse?> GetResponseByIdAsync(long id, CancellationToken ct = default)
      => db.Users.AsNoTracking()
          .Where(u => u.Id == id)
          .Select(u => new UserResponse(
              u.Id, u.Username, u.FullName, u.Email, u.Phone, u.Status,
              u.Wallet != null ? u.Wallet.Id : null,
              u.Wallet != null ? u.Wallet.WalletNumber : null,
              u.Wallet != null ? u.Wallet.Balance : 0m))
          .FirstOrDefaultAsync(ct);
}