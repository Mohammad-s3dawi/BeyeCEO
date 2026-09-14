using BeyeCEO.Domain.KPIs.Entites;
using BeyeCEO.Domain.MarketData.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeyeCEO.Infrastructure.Persistence.Configurations
{
    public class BeyeViewConfiguration : IEntityTypeConfiguration<BeyeView>
    {
        public void Configure(EntityTypeBuilder<BeyeView> builder)
        {
            builder.ToTable("BeyeViews", "config");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.ViewKeyName).HasMaxLength(100).IsRequired();
            builder.Property(e => e.Section).HasMaxLength(20).IsRequired();

            builder.Property(e => e.IsActive).HasDefaultValue(true);
            builder.Property(e => e.SortOrder).HasDefaultValue(0);

            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // الجدول الحالي ما فيه عمود UpdatedAt
            builder.Ignore(e => e.UpdatedAt);

            builder.HasIndex(e => new { e.BankId, e.IsActive })
                .HasDatabaseName("IX_BeyeViews_Bank");

            builder.HasOne<Bank>()
                .WithMany()
                .HasForeignKey(e => e.BankId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
