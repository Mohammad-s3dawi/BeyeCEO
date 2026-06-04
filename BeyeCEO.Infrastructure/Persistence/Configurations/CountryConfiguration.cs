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
    public class CountryConfiguration : IEntityTypeConfiguration<Country>
    {
        public void Configure(EntityTypeBuilder<Country> builder)
        {
            builder.ToTable("Countries", "config");

            builder.HasKey(e => e.CountryCode);

            builder.Property(e => e.CountryCode)
                .HasColumnType("CHAR(2)")
                .IsRequired();

            builder.Property(e => e.NameEN)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.NameAR)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.CurrencyCode)
                .HasColumnType("CHAR(3)")
                .IsRequired();

            builder.Property(e => e.CentralBank)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.StockExchange)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.Region)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.FlagUrl)
                .HasMaxLength(500)
                .HasDefaultValue(string.Empty);

            builder.Property(e => e.IsActive)
                .HasDefaultValue(true);
        }
    }
}
