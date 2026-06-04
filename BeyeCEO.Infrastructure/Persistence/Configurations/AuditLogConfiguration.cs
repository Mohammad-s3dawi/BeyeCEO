using BeyeCEO.Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.Persistence.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLog", "auth");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .UseIdentityColumn();  // BIGINT IDENTITY

            builder.Property(e => e.Action)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.Resource)
                .HasMaxLength(200)
                .HasDefaultValue(string.Empty);

            builder.Property(e => e.CountryCode)
                .HasColumnType("CHAR(2)");

            builder.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .HasDefaultValue(string.Empty);

            builder.Property(e => e.UserAgent)
                .HasMaxLength(500)
                .HasDefaultValue(string.Empty);

            builder.Property(e => e.IsSuccess)
                .HasDefaultValue(true);

            builder.Property(e => e.Timestamp)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(e => new { e.UserId, e.Timestamp });
            builder.HasIndex(e => e.Timestamp);
        }
    }
}
