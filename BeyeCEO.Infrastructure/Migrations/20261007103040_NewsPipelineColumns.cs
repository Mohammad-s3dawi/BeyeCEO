using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeyeCEO.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NewsPipelineColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // (a) 7 new columns on data.NewsArticles
            migrationBuilder.AddColumn<string>(
                name: "SummaryEN",
                schema: "data",
                table: "NewsArticles",
                type: "NVARCHAR(1000)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SummaryAR",
                schema: "data",
                table: "NewsArticles",
                type: "NVARCHAR(1000)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalLanguage",
                schema: "data",
                table: "NewsArticles",
                type: "CHAR(2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TranslationStatus",
                schema: "data",
                table: "NewsArticles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NotRequested");

            migrationBuilder.AddColumn<bool>(
                name: "IsBankingRelated",
                schema: "data",
                table: "NewsArticles",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOfficialSource",
                schema: "data",
                table: "NewsArticles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FeedName",
                schema: "data",
                table: "NewsArticles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            // (b) Dedupe soft-delete — keep the most complete row per SourceUrl
            // group (has image, then both titles, then longest summary, then
            // oldest CreatedAt as final tiebreak). Idempotent: filters on
            // IsDeleted = 0, so a second run finds no groups left to touch.
            migrationBuilder.Sql(@"
;WITH Ranked AS (
    SELECT
        Id,
        SourceUrl,
        ROW_NUMBER() OVER (
            PARTITION BY SourceUrl
            ORDER BY
                CASE WHEN LEN(ImageUrl) > 0 THEN 1 ELSE 0 END DESC,
                CASE WHEN LEN(TitleEN) > 0 AND LEN(TitleAR) > 0 THEN 1 ELSE 0 END DESC,
                LEN(Summary) DESC,
                CreatedAt ASC
        ) AS rn
    FROM data.NewsArticles
    WHERE IsDeleted = 0 AND SourceUrl IS NOT NULL AND SourceUrl <> ''
)
UPDATE na
    SET na.IsDeleted = 1
FROM data.NewsArticles na
INNER JOIN Ranked r ON r.Id = na.Id
WHERE r.rn > 1;
");

            // (c) Unique filtered index — created only after (b), so the
            // existing duplicates (now soft-deleted) can't violate it.
            migrationBuilder.CreateIndex(
                name: "UX_NewsArticles_SourceUrl_Active",
                schema: "data",
                table: "NewsArticles",
                column: "SourceUrl",
                unique: true,
                filter: "[IsDeleted] = 0 AND [SourceUrl] IS NOT NULL AND [SourceUrl] <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_NewsArticles_SourceUrl_Active",
                schema: "data",
                table: "NewsArticles");

            // NOTE: intentionally does NOT un-soft-delete the 108 rows the
            // Up() dedupe step marked IsDeleted = 1 — once that runs there is
            // no reliable way to distinguish those rows from ones soft-deleted
            // for any other reason afterward. Restore from the pre-migration
            // backup if the dedupe itself needs to be undone.
            migrationBuilder.DropColumn(
                name: "FeedName",
                schema: "data",
                table: "NewsArticles");

            migrationBuilder.DropColumn(
                name: "IsOfficialSource",
                schema: "data",
                table: "NewsArticles");

            migrationBuilder.DropColumn(
                name: "IsBankingRelated",
                schema: "data",
                table: "NewsArticles");

            migrationBuilder.DropColumn(
                name: "TranslationStatus",
                schema: "data",
                table: "NewsArticles");

            migrationBuilder.DropColumn(
                name: "OriginalLanguage",
                schema: "data",
                table: "NewsArticles");

            migrationBuilder.DropColumn(
                name: "SummaryAR",
                schema: "data",
                table: "NewsArticles");

            migrationBuilder.DropColumn(
                name: "SummaryEN",
                schema: "data",
                table: "NewsArticles");
        }
    }
}
