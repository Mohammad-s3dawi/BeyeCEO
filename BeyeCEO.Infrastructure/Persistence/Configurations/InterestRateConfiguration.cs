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
    public class InterestRateConfiguration : IEntityTypeConfiguration<InterestRate>
    {
        public void Configure(EntityTypeBuilder<InterestRate> builder)
        {
            builder.ToTable("InterestRates", "data");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(e => e.Institution)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(e => e.CountryCode)
                .HasColumnType("CHAR(2)")
                .IsRequired();

            builder.Property(e => e.RateType)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.Rate)
                .HasPrecision(8, 4)
                .IsRequired();

            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false);

            builder.HasIndex(e => new { e.CountryCode, e.Institution, e.EffectiveDate })
                .HasFilter("[IsDeleted] = 0");

            builder.HasOne(e => e.Country)
                .WithMany()
                .HasForeignKey(e => e.CountryCode)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
