// Services/UserService.cs
using Ewallet.Dtos.Users;
using Ewallet.Exceptions;
using Ewallet.Models;
using Ewallet.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Ewallet.Services;

public interface IUserService
{
    Task<List<UserResponse>> GetAllAsync(CancellationToken ct = default);
    Task<UserResponse> GetByIdAsync(long id, CancellationToken ct = default);
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task UpdateAsync(long id, UpdateUserRequest request, CancellationToken ct = default);
}

public class UserService(IUserRepository users, IWalletRepository wallets, IUnitOfWork uow) : IUserService
{
  public Task<List<UserResponse>> GetAllAsync(CancellationToken ct = default)
      => users.GetAllAsync(ct);

  public async Task<UserResponse> GetByIdAsync(long id, CancellationToken ct = default)
      => await users.GetResponseByIdAsync(id, ct) ?? throw new UserNotFoundException();

  public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
  {
    var username = request.Username.Trim().ToLowerInvariant();
    var email = request.Email.Trim().ToLowerInvariant();
    var phone = request.Phone.Trim();

    if (await users.UsernameExistsAsync(username, ct)) throw new ConflictException("Username sudah digunakan.");
    if (await users.EmailExistsAsync(email, null, ct)) throw new ConflictException("Email sudah digunakan.");
    if (await users.PhoneExistsAsync(phone, null, ct)) throw new ConflictException("Nomor telepon sudah digunakan.");

    var now = DateTime.UtcNow;
    var user = new User
    {
      Username = username,
      FullName = request.FullName.Trim(),
      Email = email,
      Phone = phone,
      Status = UserStatus.Active,
      CreatedAtUtc = now,
      Wallet = new Wallet
      {
        WalletNumber = await GenerateWalletNumberAsync(ct),
        Balance = 0m,
        Status = WalletStatus.Active,
        CreatedAtUtc = now,
      },
    };

    users.Add(user);
    await SaveOrConflictAsync(ct);

    return await users.GetResponseByIdAsync(user.Id, ct) ?? throw new UserNotFoundException();
  }

  public async Task UpdateAsync(long id, UpdateUserRequest request, CancellationToken ct = default)
  {
    var user = await users.GetByIdAsync(id, ct) ?? throw new UserNotFoundException();

    var email = request.Email.Trim().ToLowerInvariant();
    var phone = request.Phone.Trim();

    if (await users.EmailExistsAsync(email, id, ct)) throw new ConflictException("Email sudah digunakan.");
    if (await users.PhoneExistsAsync(phone, id, ct)) throw new ConflictException("Nomor telepon sudah digunakan.");

    user.FullName = request.FullName.Trim();
    user.Email = email;
    user.Phone = phone;
    user.UpdatedAtUtc = DateTime.UtcNow;

    await SaveOrConflictAsync(ct);
  }

  private async Task<string> GenerateWalletNumberAsync(CancellationToken ct)
  {
    for (var attempt = 0; attempt < 5; attempt++)
    {
      var number = $"EW{Random.Shared.NextInt64(1_000_000_000L, 10_000_000_000L)}";
      if (!await wallets.WalletNumberExistsAsync(number, ct)) return number;
    }
    throw new InvalidOperationException("Gagal membuat nomor wallet unik.");
  }

  private async Task SaveOrConflictAsync(CancellationToken ct)
  {
    try
    {
      await uow.SaveChangesAsync(ct);
    }
    catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
    {
      throw new ConflictException("Username, email, nomor telepon, atau nomor wallet sudah digunakan.");
    }
  }
}