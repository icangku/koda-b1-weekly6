using Ewallet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ewallet.Data.Configurations;

public class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
  public void Configure(EntityTypeBuilder<PaymentMethod> b)
  {
    b.Property(p => p.Code).IsRequired().HasMaxLength(30);
    b.Property(p => p.Name).IsRequired().HasMaxLength(100);
    b.Property(p => p.ProviderName).HasMaxLength(100);
    b.HasIndex(p => p.Code).IsUnique();
  }
}