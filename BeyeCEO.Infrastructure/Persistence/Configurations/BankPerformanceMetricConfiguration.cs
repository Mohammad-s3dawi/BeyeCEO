using BeyeCEO.Domain.KPIs.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeyeCEO.Infrastructure.Persistence.Configurations
{
    public class BankPerformanceMetricConfiguration
        : IEntityTypeConfiguration<BankPerformanceMetric>
    {
        public void Configure(EntityTypeBuilder<BankPerformanceMetric> builder)
        {
            builder.ToTable("BankPerformanceMetrics", "data");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Section).HasMaxLength(20).IsRequired();
            builder.Property(e => e.GroupName).HasMaxLength(50).IsRequired();
            builder.Property(e => e.KpiName).HasMaxLength(100).IsRequired();
            builder.Property(e => e.KpiAlias).HasMaxLength(100).IsRequired();
            builder.Property(e => e.KpiType).HasMaxLength(20).IsRequired();

            builder.Property(e => e.CurrentValue).HasPrecision(20, 4);
            builder.Property(e => e.GrowthYTDValue).HasPrecision(20, 4);
            builder.Property(e => e.GrowthYTDPct).HasPrecision(10, 4);
            builder.Property(e => e.GrowthYTDIcon).HasMaxLength(20);
            builder.Property(e => e.GrowthMTDValue).HasPrecision(20, 4);
            builder.Property(e => e.GrowthMTDPct).HasPrecision(10, 4);
            builder.Property(e => e.GrowthMTDIcon).HasMaxLength(20);
            builder.Property(e => e.BudgetYTDPct).HasPrecision(10, 4);
            builder.Property(e => e.BudgetYTDIcon).HasMaxLength(20);

            // JSON مخزن كنص كما هو — بدون تحويل
            builder.Property(e => e.TrendData)
                .HasColumnType("NVARCHAR(MAX)");

            builder.Property(e => e.Source).HasMaxLength(20).IsRequired();

            builder.Property(e => e.RecordedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(e => e.SortOrder).HasDefaultValue(0);
            builder.Property(e => e.IsDeleted).HasDefaultValue(false);

            // الجدول الحالي ما فيه أعمدة CreatedAt / UpdatedAt — بنستخدم RecordedAt بدالها
            builder.Ignore(e => e.CreatedAt);
            builder.Ignore(e => e.UpdatedAt);

            builder.HasIndex(e => new { e.BankId, e.Section, e.AsOfDate })
                .HasDatabaseName("IX_BankPerf_Bank_Section");
        }
    }
}
