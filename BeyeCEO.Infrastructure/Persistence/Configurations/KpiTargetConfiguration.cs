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
    public class KpiTargetConfiguration : IEntityTypeConfiguration<KpiTarget>
    {
        public void Configure(EntityTypeBuilder<KpiTarget> builder)
        {
            builder.ToTable("KpiTargets", "config");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(e => e.KpiCode)
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(e => e.TargetValue)
                .HasPrecision(18, 4)
                .IsRequired();

            builder.Property(e => e.Year)
                .IsRequired();

            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            // BankCountry + KPI + Year + Quarter = unique
            builder.HasIndex(e => new { e.BankCountryId, e.KpiCode, e.Year, e.Quarter })
                .IsUnique();

            builder.HasOne(e => e.BankCountry)
                .WithMany()
                .HasForeignKey(e => e.BankCountryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
