using BeyeCEO.Domain.Auth.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens", "auth");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(e => e.Token)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(e => e.IsRevoked)
                .HasDefaultValue(false);

            builder.Property(e => e.DeviceInfo)
                .HasMaxLength(200)
                .HasDefaultValue(string.Empty);

            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(e => e.Token)
                .IsUnique();

            builder.HasIndex(e => e.UserId)
                .HasFilter("[IsRevoked] = 0");

            builder.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
