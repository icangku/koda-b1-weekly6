using Ewallet.Dtos.TopUps;
using Ewallet.Exceptions;
using Ewallet.Models;
using Ewallet.Repositories;
using Ewallet.Utils;

namespace Ewallet.Services;

public interface ITopUpService
{
  Task<TopUpResponse> RequestAsync(CreateTopUpRequest request, CancellationToken ct = default);
  Task<TopUpResponse> ConfirmAsync(long topUpId, CancellationToken ct = default);
  Task<TopUpResponse> GetByIdAsync(long topUpId, CancellationToken ct = default);
}

public class TopUpService(
    IWalletRepository wallets,
    IPaymentMethodRepository paymentMethods,
    ITransactionRepository transactions,
    ITopUpRequestRepository topUps,
    IWalletMutationRepository mutations,
    IAuditLogService audit,
    IUnitOfWork uow) : ITopUpService
{
  public static readonly TimeSpan PaymentWindow = TimeSpan.FromHours(24);

  public async Task<TopUpResponse> RequestAsync(CreateTopUpRequest request, CancellationToken ct = default)
  {
    WalletGuard.EnsureValidAmount(request.Amount);
    var wallet = await wallets.GetByIdAsync(request.WalletId, ct) ?? throw new WalletNotFoundException();
    WalletGuard.EnsureActive(wallet, "tujuan");
    var method = await paymentMethods.GetActiveByIdAsync(request.PaymentMethodId, ct) ?? throw new PaymentMethodNotFoundException();

    var now = DateTime.UtcNow;
    var trx = new Transaction
    {
      ReferenceNo = ReferenceNumber.New("TOP", now),
      SenderWalletId = null,                       // TOPUP has no sender
      ReceiverWalletId = wallet.Id,
      Type = TransactionType.TopUp,
      Amount = request.Amount,
      Status = TransactionStatus.Pending,
      Description = $"Top up via {method.Name}",
      CreatedAtUtc = now,
    };
    var topUp = new TopUpRequest
    {
      Transaction = trx,
      WalletId = wallet.Id,
      PaymentMethodId = method.Id,
      Amount = request.Amount,
      Status = TopUpStatus.Pending,
      ProviderReference = $"SIM-{method.Code}-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}",
      CreatedAtUtc = now,
      ExpiresAtUtc = now.Add(PaymentWindow),
    };

    // Transaction + TopUpRequest + audit are created together or not at all. Balance is NOT touched.
    await using var dbTx = await uow.BeginTransactionAsync(ct);
    transactions.Add(trx);
    topUps.Add(topUp);
    await uow.SaveChangesAsync(ct);                // generates Ids for the audit log

    audit.Record(new AuditEntry(AuditActions.TopUpRequested, AuditOutcome.Success, nameof(TopUpRequest), topUp.Id.ToString(),
      wallet.UserId, trx.Id, $"Top up {trx.ReferenceNo} sebesar {CurrencyFormatter.Rupiah(trx.Amount)} via {method.Code} menunggu pembayaran."));
    await uow.SaveChangesAsync(ct);
    await dbTx.CommitAsync(ct);

    return Map(topUp, trx, wallet, method.Code);
  }

  public async Task<TopUpResponse> ConfirmAsync(long topUpId, CancellationToken ct = default)
  {
    await using var dbTx = await uow.BeginTransactionAsync(ct);

    var topUp = await topUps.GetForUpdateAsync(topUpId, ct) ?? throw new TopUpNotFoundException();
    var trx = topUp.Transaction;
    var wallet = topUp.Wallet;
    var now = DateTime.UtcNow;

    if (topUp.Status == TopUpStatus.Paid)
      throw new ConflictException("Top up sudah PAID dan tidak dapat diproses ulang.");
    if (topUp.Status != TopUpStatus.Pending)
      throw new WalletException($"Top up berstatus {topUp.Status.ToString().ToUpperInvariant()} tidak dapat dikonfirmasi.");

    if (topUp.ExpiresAtUtc is { } expires && expires < now)
    {
      topUp.Status = TopUpStatus.Expired;
      trx.Status = TransactionStatus.Failed;
      trx.CompletedAtUtc = now;
      await uow.SaveChangesAsync(ct);
      await dbTx.CommitAsync(ct);
      throw new WalletException("Top up sudah kedaluwarsa.");
    }

    // cross-table validation: TopUpRequest must point to a matching pending TOPUP transaction
    if (trx.Type != TransactionType.TopUp || trx.SenderWalletId is not null || trx.ReceiverWalletId != topUp.WalletId
        || trx.Amount != topUp.Amount || trx.Status != TransactionStatus.Pending)
      throw new ConflictException("Data top up tidak konsisten dengan transaksinya.");

    WalletGuard.EnsureActive(wallet, "tujuan");

    var before = wallet.Balance;
    wallet.Balance = before + topUp.Amount;
    wallet.UpdatedAtUtc = now;

    topUp.Status = TopUpStatus.Paid;
    topUp.PaidAtUtc = now;
    trx.Status = TransactionStatus.Success;
    trx.CompletedAtUtc = now;

    mutations.Add(new WalletMutation
    {
      TransactionId = trx.Id,
      WalletId = wallet.Id,
      Direction = MutationDirection.In,
      Amount = topUp.Amount,
      BalanceBefore = before,
      BalanceAfter = wallet.Balance,
      CreatedAtUtc = now,
    });

    audit.Record(new AuditEntry(AuditActions.TopUpPaid, AuditOutcome.Success, nameof(TopUpRequest), topUp.Id.ToString(),
      wallet.UserId, trx.Id, $"Top up {trx.ReferenceNo} sebesar {CurrencyFormatter.Rupiah(trx.Amount)} berhasil dibayar."));

    // A concurrent balance change makes the wallet UPDATE match 0 rows (RowVersion) ->
    // DbUpdateConcurrencyException -> dbTx is disposed without commit -> full rollback -> 409.
    await uow.SaveChangesAsync(ct);
    await dbTx.CommitAsync(ct);

    return Map(topUp, trx, wallet, topUp.PaymentMethod.Code);
  }

  public async Task<TopUpResponse> GetByIdAsync(long topUpId, CancellationToken ct = default)
  {
    var t = await topUps.GetDetailAsync(topUpId, ct) ?? throw new TopUpNotFoundException();
    return Map(t, t.Transaction, t.Wallet, t.PaymentMethod.Code);
  }

  private static TopUpResponse Map(TopUpRequest t, Transaction trx, Wallet w, string methodCode) => new(
    t.Id, trx.Id, trx.ReferenceNo, w.Id, w.WalletNumber, methodCode, t.Amount, t.Status, trx.Status,
    t.ProviderReference, t.CreatedAtUtc, t.ExpiresAtUtc, t.PaidAtUtc, w.Balance);
}
