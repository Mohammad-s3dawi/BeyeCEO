using BeyeCEO.Domain.MarketData.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeyeCEO.Infrastructure.Persistence.Configurations
{
    public class BankAdConfiguration : IEntityTypeConfiguration<BankAd>
    {
        public void Configure(EntityTypeBuilder<BankAd> builder)
        {
            builder.ToTable("BankAds", "data");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.BankName).HasMaxLength(100).IsRequired();
            builder.Property(e => e.BankNameAR).HasMaxLength(100);
            builder.Property(e => e.CountryCode).HasMaxLength(5).IsRequired();
            builder.Property(e => e.ImageUrl).HasMaxLength(1000).IsRequired();
            builder.Property(e => e.AltText).HasMaxLength(500);
            builder.Property(e => e.SourceUrl).HasMaxLength(500);

            builder.Property(e => e.ScrapedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(e => e.IsActive).HasDefaultValue(true);
            builder.Property(e => e.IsDeleted).HasDefaultValue(false);

            // الجدول الحالي ما فيه أعمدة CreatedAt / UpdatedAt — بنستخدم ScrapedAt بدالها
            builder.Ignore(e => e.CreatedAt);
            builder.Ignore(e => e.UpdatedAt);

            builder.HasIndex(e => e.CountryCode)
                .HasDatabaseName("IX_BankAds_Country");
        }
    }
}
