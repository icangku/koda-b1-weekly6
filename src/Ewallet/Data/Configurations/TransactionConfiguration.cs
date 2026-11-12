using Ewallet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ewallet.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
  public void Configure(EntityTypeBuilder<Transaction> b)
  {
    b.ToTable(t =>
    {
      t.HasCheckConstraint("CK_Transactions_Amount", "[Amount] > 0");
      t.HasCheckConstraint("CK_Transactions_Type", "[Type] IN ('TOPUP','TRANSFER')");
      t.HasCheckConstraint("CK_Transactions_Status", "[Status] IN ('PENDING','SUCCESS','FAILED')");
      t.HasCheckConstraint("CK_Transactions_Parties",
          "([Type] = 'TRANSFER' AND [SenderWalletId] IS NOT NULL AND [SenderWalletId] <> [ReceiverWalletId]) " +
          "OR ([Type] = 'TOPUP' AND [SenderWalletId] IS NULL)");
    });

    b.Property(t => t.ReferenceNo).IsRequired().HasMaxLength(50);
    b.Property(t => t.Description).HasMaxLength(255);

    b.HasIndex(t => t.ReferenceNo).IsUnique();
    b.HasIndex(t => new { t.SenderWalletId, t.CreatedAtUtc}).IsDescending(false, true);
    b.HasIndex(t => new { t.ReceiverWalletId, t.CreatedAtUtc}).IsDescending(false, true);

    b.HasOne(t => t.SenderWallet)
      .WithMany(w => w.SentTransactions)
      .HasForeignKey(t => t.SenderWalletId)
      .OnDelete(DeleteBehavior.Restrict);

    b.HasOne(t => t.ReceiverWallet)
      .WithMany(w => w.ReceivedTransaction)
      .HasForeignKey(t => t.ReceiverWalletId)
      .OnDelete(DeleteBehavior.Restrict);
  }
}