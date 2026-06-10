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
    public class StockExchangeDataConfiguration : IEntityTypeConfiguration<StockExchangeData>
    {
        public void Configure(EntityTypeBuilder<StockExchangeData> builder)
        {
            builder.ToTable("StockExchangeData", "data");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(e => e.CountryCode)
                .HasColumnType("CHAR(2)")
                .IsRequired();

            builder.Property(e => e.Exchange)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(e => e.TradingAmount)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            builder.Property(e => e.TradingVolume)
                .HasDefaultValue(0L);

            builder.Property(e => e.Transactions)
                .HasDefaultValue(0);

            builder.Property(e => e.BankingIndex)
                .HasPrecision(10, 4)
                .HasDefaultValue(0m);

            builder.Property(e => e.GeneralIndex)
                .HasPrecision(10, 4)
                .HasDefaultValue(0m);
            builder.Property(e => e.NoOfSecurities).HasDefaultValue(0);
            builder.Property(e => e.Gainers).HasDefaultValue(0);
            builder.Property(e => e.Losers).HasDefaultValue(0);
            builder.Property(e => e.Unchanged).HasDefaultValue(0);
            builder.Property(e => e.ChangePct).HasPrecision(8, 4).HasDefaultValue(0m);
            builder.Property(e => e.PreviousIndex).HasPrecision(10, 4).HasDefaultValue(0m);

            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false);

            // بورصة + يوم = سجل واحد
            builder.HasIndex(e => new { e.Exchange, e.TradeDate })
                .IsUnique();

            builder.HasIndex(e => new { e.CountryCode, e.TradeDate })
                .HasFilter("[IsDeleted] = 0");

            builder.HasOne(e => e.Country)
                .WithMany()
                .HasForeignKey(e => e.CountryCode)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
