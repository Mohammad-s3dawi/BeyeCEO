using BeyeCEO.Domain.News.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.Persistence.Configurations
{
    public class NewsArticleConfiguration : IEntityTypeConfiguration<NewsArticle>
    {
        public void Configure(EntityTypeBuilder<NewsArticle> builder)
        {
            builder.ToTable("NewsArticles", "data");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(e => e.TitleEN)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(e => e.TitleAR)
                .HasMaxLength(500)
                .HasDefaultValue(string.Empty);

            builder.Property(e => e.ContentEN)
                .HasColumnType("NVARCHAR(MAX)")
                .IsRequired();

            builder.Property(e => e.ContentAR)
                .HasColumnType("NVARCHAR(MAX)")
                .HasDefaultValue(string.Empty);

            builder.Property(e => e.Summary)
                .HasMaxLength(1000)
                .HasDefaultValue(string.Empty);

            builder.Property(e => e.SourceName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(e => e.SourceLogoUrl)
                .HasMaxLength(500)
                .HasDefaultValue(string.Empty);

            builder.Property(e => e.SourceUrl)
                .HasMaxLength(1000)
                .HasDefaultValue(string.Empty);

            builder.Property(e => e.ImageUrl)
                .HasMaxLength(1000)
                .HasDefaultValue(string.Empty);

            builder.Property(e => e.Category)
                .HasMaxLength(100)
                .HasDefaultValue("General");

            builder.Property(e => e.CountryCode)
                .HasColumnType("CHAR(2)");

            builder.Property(e => e.Scope)
                .HasColumnType("TINYINT")
                .IsRequired();

            builder.Property(e => e.ReadTimeMinutes)
                .HasDefaultValue(1);

            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(e => e.IsDeleted)
                .HasDefaultValue(false);

            builder.Property(e => e.SummaryEN)
                .HasColumnType("NVARCHAR(1000)");

            builder.Property(e => e.SummaryAR)
                .HasColumnType("NVARCHAR(1000)");

            builder.Property(e => e.OriginalLanguage)
                .HasColumnType("CHAR(2)");

            builder.Property(e => e.TranslationStatus)
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue("NotRequested");

            builder.Property(e => e.IsBankingRelated)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(e => e.IsOfficialSource)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(e => e.FeedName)
                .HasMaxLength(200);

            builder.HasIndex(e => new { e.Scope, e.CountryCode, e.PublishedAt })
                .HasFilter("[IsDeleted] = 0");

            builder.HasIndex(e => new { e.Category, e.PublishedAt })
                .HasFilter("[IsDeleted] = 0");

            // De-dupe guard — see NewsPipelineColumns migration for the one-time
            // soft-delete of existing duplicates that runs before this index is created.
            builder.HasIndex(e => e.SourceUrl)
                .IsUnique()
                .HasDatabaseName("UX_NewsArticles_SourceUrl_Active")
                .HasFilter("[IsDeleted] = 0 AND [SourceUrl] IS NOT NULL AND [SourceUrl] <> ''");

            // FK nullable — الأخبار الدولية ما عندها CountryCode
            builder.HasOne(e => e.Country)
                .WithMany()
                .HasForeignKey(e => e.CountryCode)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
