using Ewallet.Dtos.Transfers;
using Ewallet.Exceptions;
using Ewallet.Models;
using Ewallet.Repositories;
using Ewallet.Utils;
using Microsoft.EntityFrameworkCore;

namespace Ewallet.Services;

public interface ITransferService
{
  /// <param name="simulateFailure">Dev/Testing only: throw after all writes, before commit, to prove rollback.</param>
  Task<TransferResponse> TransferAsync(TransferRequest request, bool simulateFailure = false, CancellationToken ct = default);
}

public class TransferService(
    IWalletRepository wallets,
    ITransactionRepository transactions,
    IAuditLogService audit,
    IUnitOfWork uow) : ITransferService
{
  public async Task<TransferResponse> TransferAsync(TransferRequest request, bool simulateFailure = false, CancellationToken ct = default)
  {
    Wallet? sender = null;
    try
    {
      await using var dbTx = await uow.BeginTransactionAsync(ct);

      sender = await wallets.GetByIdAsync(request.SenderWalletId, ct)
        ?? throw new WalletNotFoundException("Wallet pengirim tidak ditemukan.");

      if (!string.IsNullOrWhiteSpace(request.SenderRowVersion))
      {
        var expected = new byte[8];
        if (!Convert.TryFromBase64String(request.SenderRowVersion, expected, out var written) || written != 8)
          throw new WalletException("SenderRowVersion tidak valid.");
        wallets.SetExpectedRowVersion(sender, expected);
      }

      var receiverNumber = request.ReceiverWalletNumber?.Trim();
      if (string.IsNullOrEmpty(receiverNumber)) throw new WalletNotFoundException("Wallet penerima tidak ditemukan.");
      var receiver = await wallets.GetByWalletNumberAsync(receiverNumber, ct)
        ?? throw new WalletNotFoundException("Wallet penerima tidak ditemukan.");

      if (sender.Id == receiver.Id) throw new SelfTransferException();
      WalletGuard.EnsureValidAmount(request.Amount);
      WalletGuard.EnsureActive(sender, "pengirim");
      WalletGuard.EnsureActive(receiver, "penerima");
      if (sender.Balance < request.Amount) throw new InsufficientBalanceException();

      var now = DateTime.UtcNow;
      var senderBefore = sender.Balance;
      var receiverBefore = receiver.Balance;
      sender.Balance -= request.Amount;
      receiver.Balance += request.Amount;
      sender.UpdatedAtUtc = receiver.UpdatedAtUtc = now;

      var trx = new Transaction
      {
        ReferenceNo = ReferenceNumber.New("TRF", now),
        SenderWalletId = sender.Id,
        ReceiverWalletId = receiver.Id,
        Type = TransactionType.Transfer,
        Amount = request.Amount,
        Status = TransactionStatus.Success,
        Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
        CreatedAtUtc = now,
        CompletedAtUtc = now,
        Mutations =
        [
          new WalletMutation { WalletId = sender.Id, Direction = MutationDirection.Out, Amount = request.Amount,
            BalanceBefore = senderBefore, BalanceAfter = sender.Balance, CreatedAtUtc = now },
          new WalletMutation { WalletId = receiver.Id, Direction = MutationDirection.In, Amount = request.Amount,
            BalanceBefore = receiverBefore, BalanceAfter = receiver.Balance, CreatedAtUtc = now },
        ],
      };
      transactions.Add(trx);

      // 2 wallet UPDATEs (checked by RowVersion) + 1 Transaction + 2 WalletMutations
      await uow.SaveChangesAsync(ct);

      audit.Record(new AuditEntry(AuditActions.TransferCreated, AuditOutcome.Success, nameof(Transaction), trx.Id.ToString(),
        sender.UserId, trx.Id,
        $"Transfer {trx.ReferenceNo} sebesar {CurrencyFormatter.Rupiah(trx.Amount)} dari {sender.WalletNumber} ke {receiver.WalletNumber}."));
      await uow.SaveChangesAsync(ct);

      if (simulateFailure) throw new SimulatedFailureException();

      await dbTx.CommitAsync(ct);

      return new TransferResponse(trx.Id, trx.ReferenceNo, sender.WalletNumber, receiver.WalletNumber, trx.Amount,
        trx.Status, senderBefore, sender.Balance, now);
    }
    // dbTx has already been disposed (= rolled back) when these handlers run.
    catch (WalletException ex)
    {
      await LogRejectedAsync(sender, request, AuditOutcome.Rejected, ex.Message);
      throw;
    }
    catch (DbUpdateConcurrencyException)
    {
      await LogRejectedAsync(sender, request, AuditOutcome.Failed, "Konflik concurrency: saldo wallet diubah proses lain. Transfer dibatalkan.");
      throw;
    }
    catch (SimulatedFailureException ex)
    {
      await LogRejectedAsync(sender, request, AuditOutcome.Failed, ex.Message);
      throw;
    }
    catch (DbUpdateException)
    {
      await LogRejectedAsync(sender, request, AuditOutcome.Failed, "Transfer ditolak oleh constraint database dan dibatalkan.");
      throw;
    }
  }

  private Task LogRejectedAsync(Wallet? sender, TransferRequest request, AuditOutcome outcome, string reason)
    => audit.RecordIndependentAsync(new AuditEntry(
      AuditActions.TransferRejected, outcome, nameof(Wallet), request.SenderWalletId.ToString(),
      sender?.UserId, null,
      $"Transfer {CurrencyFormatter.Rupiah(request.Amount)} ke {Truncate(request.ReceiverWalletNumber, 30)} ditolak: {reason}"));

  private static string Truncate(string? s, int max) => s is null ? "-" : s.Length <= max ? s : s[..max];
}
