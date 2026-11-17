using Ewallet.Models;

namespace Ewallet.Dtos.Wallets;

public record WalletResponse(
    long Id,
    string WalletNumber,
    long UserId,
    string OwnerUsername,
    string OwnerName,
    decimal Balance,
    WalletStatus Status,
    /// <summary>Base64 concurrency token; send it back as SenderRowVersion to detect conflicting updates.</summary>
    string RowVersion,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
