using Ewallet.Dtos.Common;
using Ewallet.Models;

namespace Ewallet.Dtos.History;

/// <summary>Query string for /api/wallets/{id}/transactions and /api/wallets/{id}/mutations.</summary>
public class HistoryQuery : PageQuery
{
  /// <summary>Search by ReferenceNo (contains).</summary>
  public string? Search { get; set; }
  public TransactionType? Type { get; set; }
  public TransactionStatus? Status { get; set; }
  /// <summary>IN = money received by this wallet, OUT = money sent by this wallet.</summary>
  public MutationDirection? Direction { get; set; }
  /// <summary>Inclusive, UTC.</summary>
  public DateTime? From { get; set; }
  /// <summary>Inclusive, UTC. A date without time includes the whole day.</summary>
  public DateTime? To { get; set; }
  /// <summary>createdAt | amount</summary>
  public string SortBy { get; set; } = "createdAt";
  /// <summary>asc | desc</summary>
  public string SortDir { get; set; } = "desc";
}

public record TransactionResponse(
    long Id,
    string ReferenceNo,
    TransactionType Type,
    TransactionStatus Status,
    MutationDirection Direction,
    decimal Amount,
    string? SenderWalletNumber,
    string ReceiverWalletNumber,
    string? Description,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc);

public record MutationResponse(
    long Id,
    long TransactionId,
    string ReferenceNo,
    TransactionType TransactionType,
    MutationDirection Direction,
    decimal Amount,
    decimal BalanceBefore,
    decimal BalanceAfter,
    DateTime CreatedAtUtc);
