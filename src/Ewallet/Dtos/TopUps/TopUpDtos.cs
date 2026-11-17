using System.ComponentModel.DataAnnotations;
using Ewallet.Models;

namespace Ewallet.Dtos.TopUps;

public record CreateTopUpRequest(
    [Range(1, long.MaxValue)] long WalletId,
    [Range(1, int.MaxValue)] int PaymentMethodId,
    decimal Amount);

public record TopUpResponse(
    long Id,
    long TransactionId,
    string ReferenceNo,
    long WalletId,
    string WalletNumber,
    string PaymentMethodCode,
    decimal Amount,
    TopUpStatus Status,
    TransactionStatus TransactionStatus,
    string? ProviderReference,
    DateTime CreatedAtUtc,
    DateTime? ExpiresAtUtc,
    DateTime? PaidAtUtc,
    decimal WalletBalance);
