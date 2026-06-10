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

    public class StockExchangeHistoryConfiguration
        : IEntityTypeConfiguration<StockExchangeHistory>
    {
        public void Configure(EntityTypeBuilder<StockExchangeHistory> builder)
        {
            builder.ToTable("StockExchangeHistory", "data");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(e => e.CountryCode)
                .HasColumnType("CHAR(2)").IsRequired();

            builder.Property(e => e.Exchange)
                .HasMaxLength(20).IsRequired();

            builder.Property(e => e.IndexValue)
                .HasPrecision(10, 4).IsRequired();

            builder.Property(e => e.PeriodType)
                .HasMaxLength(10).IsRequired();

            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false);

            builder.HasIndex(e => new
            { e.Exchange, e.PeriodType, e.RecordedAt })
                .HasFilter("[IsDeleted] = 0");

            builder.HasOne(e => e.Country)
                .WithMany()
                .HasForeignKey(e => e.CountryCode)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
