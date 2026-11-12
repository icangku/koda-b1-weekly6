using Ewallet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ewallet.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
  public void Configure(EntityTypeBuilder<AuditLog> b)
  {
    b.ToTable(t => t.HasCheckConstraint("CK_AuditLogs_Outcome", "[Outcome] IN ('SUCCESS','REJECTED','FAILED')"));

    b.Property(a => a.Action).IsRequired().HasMaxLength(60);
    b.Property(a => a.EntityType).IsRequired().HasMaxLength(60);
    b.Property(a => a.EntityId).HasMaxLength(60);
    b.Property(a => a.Description).HasMaxLength(500);
    b.Property(a => a.CorrelationId).HasMaxLength(100);

    b.HasIndex(a => new { a.ActorUserId, a.CreatedAtUtc }).IsDescending(false, true);

    b.HasOne(a => a.ActorUser).WithMany(u => u.AuditLogs)
      .HasForeignKey(a => a.ActorUserId).OnDelete(DeleteBehavior.Restrict);

    b.HasOne(a => a.Transaction).WithMany()
      .HasForeignKey(a => a.TransactionId).OnDelete(DeleteBehavior.Restrict);
  }
}