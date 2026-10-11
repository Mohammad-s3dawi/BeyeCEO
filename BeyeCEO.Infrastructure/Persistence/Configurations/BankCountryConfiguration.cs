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

            // WithMany() with no argument left Country.BankCountries as an
            // unmatched convention nav, so EF created a second, phantom
            // relationship with a shadow "CountryCode1" FK that doesn't exist
            // in the database — any query against BankCountries then failed
            // with "Invalid column name 'CountryCode1'". Wiring the actual
            // inverse nav collapses it back to the one real relationship.
            builder.HasOne(e => e.Country)
                .WithMany(c => c.BankCountries)
                .HasForeignKey(e => e.CountryCode)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
