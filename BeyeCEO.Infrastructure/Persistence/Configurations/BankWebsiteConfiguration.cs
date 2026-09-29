using BeyeCEO.Domain.KPIs.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeyeCEO.Infrastructure.Persistence.Configurations
{
    public class BankWebsiteConfiguration : IEntityTypeConfiguration<BankWebsite>
    {
        public void Configure(EntityTypeBuilder<BankWebsite> builder)
        {
            builder.ToTable("BankWebsites", "config");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.BankName).HasMaxLength(100).IsRequired();
            builder.Property(e => e.BankNameAR).HasMaxLength(100);
            builder.Property(e => e.CountryCode).HasMaxLength(5).HasDefaultValue("JO").IsRequired();
            builder.Property(e => e.WebsiteUrl).HasMaxLength(500).IsRequired();
            builder.Property(e => e.ScrapingStrategy).HasMaxLength(50).IsRequired();

            builder.Property(e => e.IsActive).HasDefaultValue(true);
            builder.Property(e => e.IsDeleted).HasDefaultValue(false);

            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // الجدول الحالي ما فيه عمود UpdatedAt
            builder.Ignore(e => e.UpdatedAt);

            builder.HasIndex(e => new { e.CountryCode, e.IsActive })
                .HasDatabaseName("IX_BankWebsites_Country");

            // BankId بدون FK — الجدول الحالي config.Banks فيه صف واحد بس (Test Bank)
            // مش دليل بنوك حقيقي، فما في معنى لربط الـ 5 بنوك الأردنية فيه
        }
    }
}
