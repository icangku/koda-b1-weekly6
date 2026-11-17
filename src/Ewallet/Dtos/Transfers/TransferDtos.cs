using System.ComponentModel.DataAnnotations;
using Ewallet.Models;

namespace Ewallet.Dtos.Transfers;

public record TransferRequest(
    [Range(1, long.MaxValue)] long SenderWalletId,
    [Required] string ReceiverWalletNumber,
    decimal Amount,
    [StringLength(255)] string? Description,
    /// <summary>Optional base64 RowVersion of the sender wallet as last read by the client
    /// (from GET /api/wallets/{id}). If the wallet changed since then the transfer returns 409.</summary>
    string? SenderRowVersion);

public record TransferResponse(
    long TransactionId,
    string ReferenceNo,
    string SenderWalletNumber,
    string ReceiverWalletNumber,
    decimal Amount,
    TransactionStatus Status,
    decimal SenderBalanceBefore,
    decimal SenderBalanceAfter,
    DateTime CompletedAtUtc);
