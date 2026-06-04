using BeyeCEO.Domain.MarketData.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.Persistence.Configurations
{
    public class BankConfiguration : IEntityTypeConfiguration<Bank>
    {
        public void Configure(EntityTypeBuilder<Bank> builder)
        {
            builder.ToTable("Banks", "config");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(e => e.NameEN)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(e => e.NameAR)
                .HasMaxLength(200)
                .HasDefaultValue(string.Empty);

            builder.Property(e => e.LogoUrl)
                .HasMaxLength(500)
                .HasDefaultValue(string.Empty);

            builder.Property(e => e.WebsiteUrl)
                .HasMaxLength(500)
                .HasDefaultValue(string.Empty);

            builder.Property(e => e.IsActive)
                .HasDefaultValue(true);

            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            // علاقة One-to-Many مع BankCountries
            builder.HasMany(e => e.BankCountries)
                .WithOne(e => e.Bank)
                .HasForeignKey(e => e.BankId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
