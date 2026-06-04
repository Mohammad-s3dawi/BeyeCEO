using BeyeCEO.Domain.KPIs.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.Persistence.Configurations
{
    public class KpiSnapshotConfiguration : IEntityTypeConfiguration<KpiSnapshot>
    {
        public void Configure(EntityTypeBuilder<KpiSnapshot> builder)
        {
            builder.ToTable("KpiSnapshots", "data");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(e => e.KpiCode)
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(e => e.Value)
                .HasPrecision(18, 4)
                .IsRequired();

            builder.Property(e => e.TargetValue)
                .HasPrecision(18, 4);

            builder.Property(e => e.PreviousValue)
                .HasPrecision(18, 4);

            builder.Property(e => e.PeriodType)
                .HasConversion<string>()
                .HasMaxLength(10)
                .HasDefaultValue("Monthly");

            builder.Property(e => e.Source)
                .HasMaxLength(20)
                .HasDefaultValue("ETL");

            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false);

            builder.HasIndex(e => new { e.BankCountryId, e.KpiCode, e.PeriodDate, e.PeriodType })
                .IsUnique();

            builder.HasIndex(e => new { e.BankCountryId, e.KpiCode, e.PeriodDate })
                .HasFilter("[IsDeleted] = 0");

            builder.HasOne(e => e.BankCountry)
                .WithMany()
                .HasForeignKey(e => e.BankCountryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
