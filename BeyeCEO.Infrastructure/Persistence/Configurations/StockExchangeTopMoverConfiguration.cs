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
    public class StockExchangeTopMoverConfiguration
     : IEntityTypeConfiguration<StockExchangeTopMover>
    {
        public void Configure(EntityTypeBuilder<StockExchangeTopMover> builder)
        {
            builder.ToTable("StockExchangeTopMovers", "data");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(e => e.CountryCode)
                .HasColumnType("CHAR(2)").IsRequired();

            builder.Property(e => e.Exchange)
                .HasMaxLength(20).IsRequired();

            builder.Property(e => e.CompanyName)
                .HasMaxLength(200).IsRequired();

            builder.Property(e => e.Symbol)
                .HasMaxLength(20).IsRequired();

            builder.Property(e => e.Price)
                .HasPrecision(10, 4).HasDefaultValue(0m);

            builder.Property(e => e.ChangePct)
                .HasPrecision(8, 4).HasDefaultValue(0m);

            builder.Property(e => e.MoverType)
                .HasMaxLength(10).IsRequired();

            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false);

            builder.HasIndex(e => new { e.Exchange, e.TradeDate })
                .HasFilter("[IsDeleted] = 0");

            builder.HasIndex(e => new
            { e.Exchange, e.MoverType, e.Rank, e.TradeDate })
                .IsUnique();

            builder.HasOne(e => e.Country)
                .WithMany()
                .HasForeignKey(e => e.CountryCode)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
