using Ewallet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ewallet.Data.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
  public void Configure(EntityTypeBuilder<Wallet> b)
  {
    b.ToTable(t =>
    {
      t.HasCheckConstraint("CK_Wallets_Balance", "[Balance] >= 0");
      t.HasCheckConstraint("CK_Wallets_Status", "[Status] IN ('ACTIVE', 'FROZEN', 'CLOSED')");
    });

    b.Property(w => w.WalletNumber).IsRequired().HasMaxLength(30);
    b.Property(w => w.RowVersion).IsRowVersion();   // concurrency token: UPDATE ... WHERE RowVersion = @original

    b.HasIndex(w => w.WalletNumber).IsUnique();

    // one user -> one wallet; EF creates a UNIQUE index on Wallets.UserId
    b.HasOne(w => w.User)
      .WithOne(u => u.Wallet)
      .HasForeignKey<Wallet>(w => w.UserId)
      .OnDelete(DeleteBehavior.Restrict);
  }
}
