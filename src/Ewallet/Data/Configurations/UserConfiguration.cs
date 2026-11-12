using Ewallet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ewallet.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
  public void Configure(EntityTypeBuilder<User> b)
  {
    b.ToTable(t => t.HasCheckConstraint("CK_Users_Status", "[Status] IN ('ACTIVE', 'SUSPENDED)"));
    
    b.Property(u => u.Username).IsRequired().HasMaxLength(50);
    b.Property(u => u.FullName).IsRequired().HasMaxLength(100);
    b.Property(u => u.Email).IsRequired().HasMaxLength(150);
    b.Property(u => u.Phone).IsRequired().HasMaxLength(20);
    b.Property(u => u.PasswordHash).HasMaxLength(500);

    b.HasIndex(u => u.Username).IsUnique();
    b.HasIndex(u => u.Email).IsUnique();
    b.HasIndex(u => u.Phone).IsUnique();
  }
}