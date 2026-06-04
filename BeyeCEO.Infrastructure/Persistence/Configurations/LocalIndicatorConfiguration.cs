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
    public class LocalIndicatorConfiguration : IEntityTypeConfiguration<LocalIndicator>
    {
        public void Configure(EntityTypeBuilder<LocalIndicator> builder)
        {
            builder.ToTable("LocalIndicators", "data");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(e => e.CountryCode)
                .HasColumnType("CHAR(2)")
                .IsRequired();

            builder.Property(e => e.IndicatorCode)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.IndicatorNameEN)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(e => e.IndicatorNameAR)
                .HasMaxLength(200)
                .HasDefaultValue(string.Empty);

            builder.Property(e => e.Value)
                .HasPrecision(18, 4)
                .IsRequired();

            builder.Property(e => e.Unit)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false);

            builder.HasIndex(e => new { e.CountryCode, e.IndicatorCode, e.PeriodDate })
                .IsUnique();

            builder.HasOne(e => e.Country)
                .WithMany()
                .HasForeignKey(e => e.CountryCode)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
