using BeyeCEO.Domain.MarketData.Entities;
using BeyeCEO.Domain.News.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyeCEO.Infrastructure.Persistence
{
    public class BeyeCeoDbContext :DbContext
    {
        public BeyeCeoDbContext(DbContextOptions<BeyeCeoDbContext> options)
       : base(options) { }

        // config schema
        public DbSet<Country> Countries => Set<Country>();
        public DbSet<Bank> Banks => Set<Bank>();
        public DbSet<BankCountry> BankCountries => Set<BankCountry>();
        public DbSet<KpiDefinition> KpiDefinitions => Set<KpiDefinition>();
        public DbSet<KpiTarget> KpiTargets => Set<KpiTarget>();

        // auth schema
        public DbSet<User> Users => Set<User>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        // data schema
        public DbSet<GlobalIndex> GlobalIndices => Set<GlobalIndex>();
        public DbSet<CurrencyRate> CurrencyRates => Set<CurrencyRate>();
        public DbSet<Commodity> Commodities => Set<Commodity>();
        public DbSet<InterestRate> InterestRates => Set<InterestRate>();
        public DbSet<LocalIndicator> LocalIndicators => Set<LocalIndicator>();
        public DbSet<StockExchangeData> StockExchangeData => Set<StockExchangeData>();
        public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();
        public DbSet<KpiSnapshot> KpiSnapshots => Set<KpiSnapshot>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // تطبيق كل الـ Configurations أوتوماتيك
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(BeyeCeoDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}
