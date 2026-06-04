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
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users", "auth");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(e => e.Email)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(e => e.FullNameEN)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(e => e.FullNameAR)
                .HasMaxLength(200)
                .HasDefaultValue(string.Empty);

            builder.Property(e => e.AuthType)
                .HasConversion<string>()
                .HasMaxLength(10)
                .HasDefaultValue("Local");

            builder.Property(e => e.Role)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue("CEO");

            builder.Property(e => e.PreferredLanguage)
                .HasConversion<string>()
                .HasColumnType("CHAR(2)")
                .HasDefaultValue("EN");

            builder.Property(e => e.DefaultCountryCode)
                .HasColumnType("CHAR(2)");

            builder.Property(e => e.IsActive)
                .HasDefaultValue(true);

            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(e => e.Email)
                .IsUnique();

            builder.HasIndex(e => e.BankId)
                .HasFilter("[IsActive] = 1");

            builder.HasOne(e => e.Bank)
                .WithMany()
                .HasForeignKey(e => e.BankId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
