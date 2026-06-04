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
    public class CurrencyRateConfiguration : IEntityTypeConfiguration<CurrencyRate>
    {
        public void Configure(EntityTypeBuilder<CurrencyRate> builder)
        {
            builder.ToTable("CurrencyRates", "data");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(e => e.BaseCurrency)
                .HasMaxLength(10)
                .IsRequired();

            builder.Property(e => e.QuoteCurrency)
                .HasMaxLength(10)
                .IsRequired();

            builder.Property(e => e.Rate)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(e => e.Source)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false);

            builder.HasIndex(e => new { e.BaseCurrency, e.QuoteCurrency, e.RecordedAt })
                .HasFilter("[IsDeleted] = 0");
        }
    }
}
