using Ewallet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ewallet.Data.Configurations;

public class TopUpRequestConfiguration : IEntityTypeConfiguration<TopUpRequest>
{
  public void Configure(EntityTypeBuilder<TopUpRequest> b)
  {
    b.ToTable(t =>
    {
      t.HasCheckConstraint("CK_TopUpRequests_Amount", "[Amount] > 0");
      t.HasCheckConstraint("CK_TopUpRequests_Status", "[Status] IN ('PENDING','PAID','FAILED','EXPIRED')");
    });

    b.Property(r => r.ProviderReference).HasMaxLength(100);

    b.HasOne(r => r.Transaction).WithOne(t => t.TopUpRequest)
      .HasForeignKey<TopUpRequest>(r => r.TransactionId).OnDelete(DeleteBehavior.Restrict);

    b.HasOne(r => r.Wallet).WithMany()
      .HasForeignKey(r => r.WalletId).OnDelete(DeleteBehavior.Restrict);

    b.HasOne(r => r.PaymentMethod).WithMany()
      .HasForeignKey(r => r.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);
  }
}