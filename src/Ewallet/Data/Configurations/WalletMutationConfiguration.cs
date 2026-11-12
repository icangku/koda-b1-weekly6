using Ewallet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ewallet.Data.Configurations;

public class WalletMutationConfiguration : IEntityTypeConfiguration<WalletMutation>
{
  public void Configure(EntityTypeBuilder<WalletMutation> b)
  {
    b.ToTable(t =>
    {
      t.HasCheckConstraint("CK_WalletMutations_Amount", "[Amount] > 0");
      t.HasCheckConstraint("CK_WalletMutations_Direction", "[Direction] IN ('IN','OUT')");
      t.HasCheckConstraint("CK_WalletMutations_Balance",
          "([Direction] = 'IN' AND [BalanceAfter] = [BalanceBefore] + [Amount]) " +
          "OR ([Direction] = 'OUT' AND [BalanceAfter] = [BalanceBefore] - [Amount])");
    });

    b.HasIndex(m => new { m.TransactionId, m.WalletId }).IsUnique();
    b.HasIndex(m => new { m.WalletId, m.CreatedAtUtc, m.Id }).IsDescending(false, true, true);

    b.HasOne(m => m.Transaction).WithMany(t => t.Mutations)
      .HasForeignKey(m => m.TransactionId).OnDelete(DeleteBehavior.Restrict);

    b.HasOne(m => m.Wallet).WithMany(w => w.Mutations)
      .HasForeignKey(m => m.WalletId).OnDelete(DeleteBehavior.Restrict);
  }
}