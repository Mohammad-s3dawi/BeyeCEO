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

    public class BankCountryConfiguration : IEntityTypeConfiguration<BankCountry>
    {
        public void Configure(EntityTypeBuilder<BankCountry> builder)
        {
            builder.ToTable("BankCountries", "config");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(e => e.CountryCode)
                .HasColumnType("CHAR(2)")
                .IsRequired();

            builder.Property(e => e.LocalBankNameEN)
                .HasMaxLength(200)
                .HasDefaultValue(string.Empty);

            builder.Property(e => e.LocalBankNameAR)
                .HasMaxLength(200)
                .HasDefaultValue(string.Empty);

            builder.Property(e => e.IsHeadquarters)
                .HasDefaultValue(false);

            builder.Property(e => e.IsActive)
                .HasDefaultValue(true);

            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(e => new { e.BankId, e.CountryCode })
                .IsUnique();

            builder.HasIndex(e => e.BankId)
                .HasFilter("[IsActive] = 1");

            builder.HasOne(e => e.Country)
                .WithMany()
                .HasForeignKey(e => e.CountryCode)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
