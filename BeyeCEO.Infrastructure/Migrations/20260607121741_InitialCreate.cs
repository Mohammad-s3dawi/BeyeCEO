using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeyeCEO.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.EnsureSchema(
                name: "config");

            migrationBuilder.EnsureSchema(
                name: "data");

            migrationBuilder.CreateTable(
                name: "AuditLog",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BankId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Resource = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    CountryCode = table.Column<string>(type: "CHAR(2)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false, defaultValue: ""),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ErrorMsg = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Banks",
                schema: "config",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    NameEN = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAR = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Banks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Commodities",
                schema: "data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Symbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    ChangePct = table.Column<decimal>(type: "decimal(8,4)", precision: 8, scale: 4, nullable: false, defaultValue: 0m),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commodities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Countries",
                schema: "config",
                columns: table => new
                {
                    CountryCode = table.Column<string>(type: "CHAR(2)", nullable: false),
                    NameEN = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameAR = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CurrencyCode = table.Column<string>(type: "CHAR(3)", nullable: false),
                    CurrencyNameEN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrencyNameAR = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CentralBank = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StockExchange = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Region = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FlagUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.CountryCode);
                });

            migrationBuilder.CreateTable(
                name: "CurrencyRates",
                schema: "data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    BaseCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    QuoteCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GlobalIndices",
                schema: "data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Symbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Change = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    ChangePct = table.Column<decimal>(type: "decimal(8,4)", precision: 8, scale: 4, nullable: false, defaultValue: 0m),
                    Region = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalIndices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KpiDefinitions",
                schema: "config",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NameEN = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameAR = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DescriptionEN = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    DescriptionAR = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    WarningThreshold = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    AlertThreshold = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    IsHigherBetter = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiDefinitions", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "BankCountries",
                schema: "config",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    BankId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CountryCode = table.Column<string>(type: "CHAR(2)", nullable: false),
                    LocalBankNameEN = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    LocalBankNameAR = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    IsHeadquarters = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CountryCode1 = table.Column<string>(type: "CHAR(2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankCountries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankCountries_Banks_BankId",
                        column: x => x.BankId,
                        principalSchema: "config",
                        principalTable: "Banks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BankCountries_Countries_CountryCode",
                        column: x => x.CountryCode,
                        principalSchema: "config",
                        principalTable: "Countries",
                        principalColumn: "CountryCode",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BankCountries_Countries_CountryCode1",
                        column: x => x.CountryCode1,
                        principalSchema: "config",
                        principalTable: "Countries",
                        principalColumn: "CountryCode");
                });

            migrationBuilder.CreateTable(
                name: "InterestRates",
                schema: "data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Institution = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CountryCode = table.Column<string>(type: "CHAR(2)", nullable: false),
                    RateType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(8,4)", precision: 8, scale: 4, nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterestRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterestRates_Countries_CountryCode",
                        column: x => x.CountryCode,
                        principalSchema: "config",
                        principalTable: "Countries",
                        principalColumn: "CountryCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LocalIndicators",
                schema: "data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    CountryCode = table.Column<string>(type: "CHAR(2)", nullable: false),
                    IndicatorCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IndicatorNameEN = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IndicatorNameAR = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    Value = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PeriodDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalIndicators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalIndicators_Countries_CountryCode",
                        column: x => x.CountryCode,
                        principalSchema: "config",
                        principalTable: "Countries",
                        principalColumn: "CountryCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NewsArticles",
                schema: "data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TitleEN = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TitleAR = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    ContentEN = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false),
                    ContentAR = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false, defaultValue: ""),
                    Summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    SourceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SourceLogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    SourceUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    ImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "General"),
                    CountryCode = table.Column<string>(type: "CHAR(2)", nullable: true),
                    Scope = table.Column<byte>(type: "TINYINT", nullable: false),
                    ReadTimeMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsArticles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsArticles_Countries_CountryCode",
                        column: x => x.CountryCode,
                        principalSchema: "config",
                        principalTable: "Countries",
                        principalColumn: "CountryCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockExchangeData",
                schema: "data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    CountryCode = table.Column<string>(type: "CHAR(2)", nullable: false),
                    Exchange = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TradingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    TradingVolume = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    Transactions = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    BankingIndex = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false, defaultValue: 0m),
                    GeneralIndex = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false, defaultValue: 0m),
                    TradeDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockExchangeData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockExchangeData_Countries_CountryCode",
                        column: x => x.CountryCode,
                        principalSchema: "config",
                        principalTable: "Countries",
                        principalColumn: "CountryCode",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    BankId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FullNameEN = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FullNameAR = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuthType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "Local"),
                    WindowsSid = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "CEO"),
                    DefaultCountryCode = table.Column<string>(type: "CHAR(2)", nullable: true),
                    PreferredLanguage = table.Column<string>(type: "CHAR(2)", nullable: false, defaultValue: "EN"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DefaultCountryCountryCode = table.Column<string>(type: "CHAR(2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Banks_BankId",
                        column: x => x.BankId,
                        principalSchema: "config",
                        principalTable: "Banks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Countries_DefaultCountryCountryCode",
                        column: x => x.DefaultCountryCountryCode,
                        principalSchema: "config",
                        principalTable: "Countries",
                        principalColumn: "CountryCode");
                });

            migrationBuilder.CreateTable(
                name: "KpiSnapshots",
                schema: "data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    BankCountryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KpiCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TargetValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    PreviousValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "Monthly"),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "ETL"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KpiSnapshots_BankCountries_BankCountryId",
                        column: x => x.BankCountryId,
                        principalSchema: "config",
                        principalTable: "BankCountries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KpiTargets",
                schema: "config",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    BankCountryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KpiCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TargetValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Year = table.Column<short>(type: "smallint", nullable: false),
                    Quarter = table.Column<byte>(type: "tinyint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KpiTargets_BankCountries_BankCountryId",
                        column: x => x.BankCountryId,
                        principalSchema: "config",
                        principalTable: "BankCountries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeviceInfo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_Timestamp",
                schema: "auth",
                table: "AuditLog",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_UserId_Timestamp",
                schema: "auth",
                table: "AuditLog",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_BankCountries_BankId",
                schema: "config",
                table: "BankCountries",
                column: "BankId",
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_BankCountries_BankId_CountryCode",
                schema: "config",
                table: "BankCountries",
                columns: new[] { "BankId", "CountryCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankCountries_CountryCode",
                schema: "config",
                table: "BankCountries",
                column: "CountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_BankCountries_CountryCode1",
                schema: "config",
                table: "BankCountries",
                column: "CountryCode1");

            migrationBuilder.CreateIndex(
                name: "IX_Commodities_Symbol_RecordedAt",
                schema: "data",
                table: "Commodities",
                columns: new[] { "Symbol", "RecordedAt" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyRates_BaseCurrency_QuoteCurrency_RecordedAt",
                schema: "data",
                table: "CurrencyRates",
                columns: new[] { "BaseCurrency", "QuoteCurrency", "RecordedAt" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalIndices_Symbol_RecordedAt",
                schema: "data",
                table: "GlobalIndices",
                columns: new[] { "Symbol", "RecordedAt" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_InterestRates_CountryCode_Institution_EffectiveDate",
                schema: "data",
                table: "InterestRates",
                columns: new[] { "CountryCode", "Institution", "EffectiveDate" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_KpiSnapshots_BankCountryId_KpiCode_PeriodDate",
                schema: "data",
                table: "KpiSnapshots",
                columns: new[] { "BankCountryId", "KpiCode", "PeriodDate" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_KpiSnapshots_BankCountryId_KpiCode_PeriodDate_PeriodType",
                schema: "data",
                table: "KpiSnapshots",
                columns: new[] { "BankCountryId", "KpiCode", "PeriodDate", "PeriodType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KpiTargets_BankCountryId_KpiCode_Year_Quarter",
                schema: "config",
                table: "KpiTargets",
                columns: new[] { "BankCountryId", "KpiCode", "Year", "Quarter" },
                unique: true,
                filter: "[Quarter] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LocalIndicators_CountryCode_IndicatorCode_PeriodDate",
                schema: "data",
                table: "LocalIndicators",
                columns: new[] { "CountryCode", "IndicatorCode", "PeriodDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticles_Category_PublishedAt",
                schema: "data",
                table: "NewsArticles",
                columns: new[] { "Category", "PublishedAt" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticles_CountryCode",
                schema: "data",
                table: "NewsArticles",
                column: "CountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticles_Scope_CountryCode_PublishedAt",
                schema: "data",
                table: "NewsArticles",
                columns: new[] { "Scope", "CountryCode", "PublishedAt" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                schema: "auth",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                schema: "auth",
                table: "RefreshTokens",
                column: "UserId",
                filter: "[IsRevoked] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_StockExchangeData_CountryCode_TradeDate",
                schema: "data",
                table: "StockExchangeData",
                columns: new[] { "CountryCode", "TradeDate" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_StockExchangeData_Exchange_TradeDate",
                schema: "data",
                table: "StockExchangeData",
                columns: new[] { "Exchange", "TradeDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_BankId",
                schema: "auth",
                table: "Users",
                column: "BankId",
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DefaultCountryCountryCode",
                schema: "auth",
                table: "Users",
                column: "DefaultCountryCountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                schema: "auth",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLog",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "Commodities",
                schema: "data");

            migrationBuilder.DropTable(
                name: "CurrencyRates",
                schema: "data");

            migrationBuilder.DropTable(
                name: "GlobalIndices",
                schema: "data");

            migrationBuilder.DropTable(
                name: "InterestRates",
                schema: "data");

            migrationBuilder.DropTable(
                name: "KpiDefinitions",
                schema: "config");

            migrationBuilder.DropTable(
                name: "KpiSnapshots",
                schema: "data");

            migrationBuilder.DropTable(
                name: "KpiTargets",
                schema: "config");

            migrationBuilder.DropTable(
                name: "LocalIndicators",
                schema: "data");

            migrationBuilder.DropTable(
                name: "NewsArticles",
                schema: "data");

            migrationBuilder.DropTable(
                name: "RefreshTokens",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "StockExchangeData",
                schema: "data");

            migrationBuilder.DropTable(
                name: "BankCountries",
                schema: "config");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "Banks",
                schema: "config");

            migrationBuilder.DropTable(
                name: "Countries",
                schema: "config");
        }
    }
}
