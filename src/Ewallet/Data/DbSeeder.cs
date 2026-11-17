using Ewallet.Models;
using Microsoft.EntityFrameworkCore;

namespace Ewallet.Data;

public static class DbSeeder
{
  public static async Task SeedAsync(AppDbContext db)
  {
    if(await db.Users.AnyAsync()) return;

    await using var dbTx = await db.Database.BeginTransactionAsync();
    var audits = new List<Func<AuditLog>>();   // built after the first save, when Ids exist

    var now = DateTime.UtcNow;
    var clock = now.AddDays(-7);

    DateTime Tick() => clock = clock.AddHours(5);
    var seq = 0;

    var bank = new PaymentMethod { Code = "BANK_TRANSFER", Name = "Bank Transfer", ProviderName = "Simulated Bank", IsActive = true, CreatedAtUtc = now};
    var qris = new PaymentMethod { Code = "QRIS", Name = "QRIS", ProviderName = "Simulated QRIS", IsActive = true, CreatedAtUtc = now};
    var credit_card = new PaymentMethod { Code = "CREDIT_CARD", Name = "Credit Card", ProviderName = "Simulated Card", IsActive = true, CreatedAtUtc = now};

    db.PaymentMethods.AddRange(bank, qris, credit_card);

    var people = new[]
    {
      ("budi", "Budi Santoso"), ("siti", "Siti Rahma"), ("andi", "Andi Wijaya"),
      ("dewi", "Dewi Lestari"), ("rizky", "Rizky Ramadhan"), ("putri", "Putri Anggraini"),
      ("agus", "Agus Setiawan"), ("maya", "Maya Sari"), ("dimas", "Dimas Prakoso"), ("nadia", "Nadia Kusuma"),
    };

    var wallets = new List<Wallet>();
    for (var i = 0; i < people.Length; i++)
    {
      var (username, fullName) = people[i];
      var user = new User
      {
          Username = username,
          FullName = fullName,
          Email = $"{username}@example.com",
          Phone = $"0812000000{i + 1:D2}",
          Status = UserStatus.Active,
          CreatedAtUtc = now.AddDays(-8),
      };
      var wallet = new Wallet
      {
          User = user,
          WalletNumber = $"EW{1_000_000_001L + i}",
          Balance = 0m,
          Status = WalletStatus.Active,
          CreatedAtUtc = user.CreatedAtUtc,
      };
      db.Wallets.Add(wallet);
      wallets.Add(wallet);
    }

    void Apply(Transaction trx, Wallet w, MutationDirection dir, DateTime at)
    {
      var before = w.Balance;
      w.Balance = dir == MutationDirection.In ? before + trx.Amount : before - trx.Amount;
      if (w.Balance < 0) throw new InvalidOperationException($"Seed data would overdraw {w.WalletNumber}");
      w.UpdatedAtUtc = at;

      db.WalletMutations.Add(new WalletMutation
      {
          Transaction = trx,
          Wallet = w,
          Direction = dir,
          Amount = trx.Amount,
          BalanceBefore = before,
          BalanceAfter = w.Balance,
          CreatedAtUtc = at,
      });
    }

    void TopUp(Wallet w, decimal amount, PaymentMethod pm, bool paid)
    {
        var n = ++seq;
        var created = Tick();
        var done = created.AddMinutes(2);

        var trx = new Transaction
        {
            ReferenceNo = $"TOP-SEED-{n:D4}",
            ReceiverWallet = w,                            // SenderWallet stays null for TOPUP
            Type = TransactionType.TopUp,
            Amount = amount,
            Status = paid ? TransactionStatus.Success : TransactionStatus.Pending,
            Description = $"Top up via {pm.Name}",
            CreatedAtUtc = created,
            CompletedAtUtc = paid ? done : null,
        };
        db.Transactions.Add(trx);

        var topUp = new TopUpRequest
        {
            Transaction = trx,
            Wallet = w,
            PaymentMethod = pm,
            Amount = amount,
            Status = paid ? TopUpStatus.Paid : TopUpStatus.Pending,
            ProviderReference = $"SIM-{n:D4}",
            CreatedAtUtc = created,
            ExpiresAtUtc = paid ? created.AddHours(24) : now.AddHours(24),
            PaidAtUtc = paid ? done : null,
        };
        db.TopUpRequests.Add(topUp);

        audits.Add(() => Audit(AuditActions.TopUpRequested, nameof(TopUpRequest), topUp.Id, w.UserId, trx, created,
            $"Top up {trx.ReferenceNo} sebesar {Rp(amount)} via {pm.Code} menunggu pembayaran."));

        if (paid)
        {
            Apply(trx, w, MutationDirection.In, done);   // a PENDING top-up must not touch the balance
            audits.Add(() => Audit(AuditActions.TopUpPaid, nameof(TopUpRequest), topUp.Id, w.UserId, trx, done,
                $"Top up {trx.ReferenceNo} sebesar {Rp(amount)} berhasil dibayar."));
        }
    }

    void Transfer(Wallet from, Wallet to, decimal amount)
    {
        var created = Tick();
        var trx = new Transaction
        {
            ReferenceNo = $"TRF-SEED-{++seq:D4}",
            SenderWallet = from,
            ReceiverWallet = to,
            Type = TransactionType.Transfer,
            Amount = amount,
            Status = TransactionStatus.Success,
            Description = "Seed transfer",
            CreatedAtUtc = created,
            CompletedAtUtc = created.AddSeconds(5),
        };
        db.Transactions.Add(trx);

        Apply(trx, from, MutationDirection.Out, created);      // one transaction, two mutations
        Apply(trx, to, MutationDirection.In, created);
        audits.Add(() => Audit(AuditActions.TransferCreated, nameof(Transaction), trx.Id, from.UserId, trx, created,
            $"Transfer {trx.ReferenceNo} sebesar {Rp(amount)} dari {from.WalletNumber} ke {to.WalletNumber}."));
    }

    static string Rp(decimal v) => Ewallet.Utils.CurrencyFormatter.Rupiah(v);

    static AuditLog Audit(string action, string entityType, long entityId, long userId, Transaction trx, DateTime at, string desc) => new()
    {
        Action = action, EntityType = entityType, EntityId = entityId.ToString(), ActorUserId = userId,
        TransactionId = trx.Id, Outcome = AuditOutcome.Success, Description = desc, CorrelationId = "seed", CreatedAtUtc = at,
    };

    // ---- sample activity ----
    TopUp(wallets[0], 500_000m, bank, paid: true);
    TopUp(wallets[1], 300_000m, qris, paid: true);
    TopUp(wallets[2], 1_000_000m, bank, paid: true);
    TopUp(wallets[3], 250_000m, qris, paid: true);
    TopUp(wallets[4], 750_000m, bank, paid: true);
    TopUp(wallets[5], 100_000m, qris, paid: true);
    TopUp(wallets[6], 150_000m, qris, paid: false);

    Transfer(wallets[0], wallets[1], 100_000m);
    Transfer(wallets[2], wallets[3], 250_000m);
    Transfer(wallets[2], wallets[4], 50_000m);
    Transfer(wallets[4], wallets[0], 200_000m);
    Transfer(wallets[1], wallets[5], 75_000m);

    await db.SaveChangesAsync();

    db.AuditLogs.AddRange(audits.Select(a => a()));
    await db.SaveChangesAsync();
    await dbTx.CommitAsync();
  }
}