using BeyeCEO.Domain.MarketData.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BeyeCEO.Infrastructure.Persistence.Configurations
{
    public class CentralBankCircularConfiguration
        : IEntityTypeConfiguration<CentralBankCircular>
    {
        public void Configure(EntityTypeBuilder<CentralBankCircular> builder)
        {
            builder.ToTable("CentralBankCirculars", "data");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.CountryCode)
                .HasMaxLength(5)
                .IsRequired();

            builder.Property(e => e.TitleAR).HasMaxLength(500);
            builder.Property(e => e.TitleEN).HasMaxLength(500);
            builder.Property(e => e.CircularNumber).HasMaxLength(50);
            builder.Property(e => e.PdfUrl).HasMaxLength(1000);
            builder.Property(e => e.FileSize).HasMaxLength(20);

            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false);

            // الجدول الحالي ما فيه عمود UpdatedAt
            builder.Ignore(e => e.UpdatedAt);

            builder.HasIndex(e => e.CountryCode)
                .HasDatabaseName("IX_Circulars_Country");
        }
    }
}
