using BeyeCEO.Domain.Auth.Interfaces;
using BeyeCEO.Domain.KPIs.Interfaces;
using BeyeCEO.Domain.MarketData.Interfaces;
using BeyeCEO.Domain.News.Interfaces;
using BeyeCEO.Infrastructure.BackgroundJobs;
using BeyeCEO.Infrastructure.ExternalServices;
using BeyeCEO.Infrastructure.Identity;
using BeyeCEO.Infrastructure.NewsAggregation;
using BeyeCEO.Infrastructure.Persistence;
using BeyeCEO.Infrastructure.Persistence.Repositories;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// ?? Serilog ???????????????????????????????????????????????
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/beyeceo-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });



    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme. \r\n\r\n 
                      Enter 'Bearer' [space] and then your token in the text input below.
                      \r\n\r\nExample: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement()
      {
        {
          new OpenApiSecurityScheme
          {
            Reference = new OpenApiReference
              {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
              },
              Scheme = "oauth2",
              Name = "Bearer",
              In = ParameterLocation.Header,

            },
            new List<string>()
          }
        });

});
// ?? Database ??????????????????????????????????????????????
builder.Services.AddDbContext<BeyeCeoDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("BeyeCeoDB"),
        sqlOptions =>
        {
            sqlOptions.MigrationsAssembly("BeyeCEO.Infrastructure");
            sqlOptions.CommandTimeout(30);
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        }));

// ?? Repositories ??????????????????????????????????????????
builder.Services.AddScoped<IMarketDataRepository, MarketDataRepository>();
builder.Services.AddScoped<INewsRepository, NewsRepository>();
builder.Services.AddScoped<IKpiRepository, KpiRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<GlobalMarketsJob>();
builder.Services.AddScoped<CommoditiesJob>();
builder.Services.AddScoped<InterestRatesJob>();
builder.Services.AddScoped<LocalStockExchangeJob>();
builder.Services.AddScoped<LocalIndicatorsJob>();
builder.Services.AddScoped<NewsJob>();
builder.Services.AddScoped<CbjCircularsJob>();
builder.Services.AddScoped<CBJCircularsScraper>();
builder.Services.AddScoped<CurrencyRatesJob>();
builder.Services.AddScoped<BankAdsScraper>();
builder.Services.AddScoped<BankAdsJob>();
builder.Services.AddScoped<BankPerformanceJob>();
////////////////////////////////////
builder.Services.AddHttpClient<AlphaVantageClient>();
builder.Services.AddHttpClient<FredClient>();
builder.Services.AddHttpClient<EiaClient>();
builder.Services.AddHttpClient<ExchangeRateClient>();
builder.Services.AddHttpClient<MetalsClient>();
builder.Services.AddHttpClient<BeyePerformanceClient>(c =>
    c.DefaultRequestHeaders.Add("User-Agent", "BeyeCEO/1.0"))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        // ⚠️ بيقبل أي شهادة SSL — Beye على بورت غير قياسي وشهادة مش موثوقة غالباً (بيئة تطوير فقط)
        ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });
// Scrapers
builder.Services.AddHttpClient<ASEScraper>();
builder.Services.AddHttpClient<ASEClient>();
builder.Services.AddHttpClient<CBJScraper>();

// Step 11b — one honest, configurable UA for every news-pipeline HTTP call
// (feeds, Guardian, og:image lookups). No spoofed browser UA. Set the real
// contact URL/email in News:Http:UserAgent per environment — see appsettings.json.
var newsUserAgent = builder.Configuration.GetValue<string>("News:Http:UserAgent")
    ?? "BeyeCEO-NewsReader/1.0";

builder.Services.AddHttpClient<GuardianClient>(c =>
    c.DefaultRequestHeaders.Add("User-Agent", newsUserAgent));
builder.Services.AddHttpClient<NewsApiClient>(c =>
    c.DefaultRequestHeaders.Add("User-Agent", "BeyeCEO/1.0"));
builder.Services.AddHttpClient<RssNewsClient>(c =>
    c.DefaultRequestHeaders.Add("User-Agent", "BeyeCEO/1.0"));
builder.Services.AddHttpClient<TranslationService>();
// News source aggregation (config-driven feeds — see News:Feeds, News:Storage:Enabled)
builder.Services.AddSingleton<BankingKeywordFilter>();
builder.Services.AddSingleton<SourcesHealthTracker>();
builder.Services.AddHttpClient<FeedFetcherService>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(15);
    c.DefaultRequestHeaders.Add("User-Agent", newsUserAgent);
});
builder.Services.AddScoped<NewsPipelineRunner>();
builder.Services.AddScoped<NewsFeedJobs>();
builder.Services.AddScoped<OldRowsClassificationService>();

// Step 11b — polite og:image fallback. Named client so singleton services
// (robots cache) can create short-lived HttpClients via the factory
// without becoming "captive dependencies."
builder.Services.AddHttpClient("NewsImageFallback", c =>
{
    c.Timeout = TimeSpan.FromSeconds(10);
    c.DefaultRequestHeaders.Add("User-Agent", newsUserAgent);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = true,
    MaxAutomaticRedirections = 2
});
builder.Services.AddSingleton<BeyeCEO.Infrastructure.NewsAggregation.ImageFallback.IRobotsTxtCache,
    BeyeCEO.Infrastructure.NewsAggregation.ImageFallback.RobotsTxtCache>();
builder.Services.AddSingleton<BeyeCEO.Infrastructure.NewsAggregation.ImageFallback.IDomainRateLimiter,
    BeyeCEO.Infrastructure.NewsAggregation.ImageFallback.DomainRateLimiter>();
builder.Services.AddSingleton<BeyeCEO.Infrastructure.NewsAggregation.ImageFallback.IImageFetchFailureCache,
    BeyeCEO.Infrastructure.NewsAggregation.ImageFallback.ImageFetchFailureCache>();
builder.Services.AddScoped<BeyeCEO.Infrastructure.NewsAggregation.ImageFallback.IPageMetadataFetcher,
    BeyeCEO.Infrastructure.NewsAggregation.ImageFallback.PageMetadataFetcher>();
///
// ?? Hangfire ??????????????????????????????????????????????
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(
        builder.Configuration.GetConnectionString("BeyeCeoDB"),
        new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));

builder.Services.AddHangfireServer();
// ?? MediatR ???????????????????????????????????????????????
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(BeyeCEO.Application.AssemblyReference).Assembly));
// ?? CORS ??????????????????????????????????????????????????
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// ?? JWT Authentication ????????????????????????????????????
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(
                    builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

// ?????????????????????????????????????????????????????????
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire");
// ?? Register Background Jobs ??????????????????????????????


// 1x/day — Global Markets (AlphaVantage quota: 25/day, 16 symbols x 1 = 16)
RecurringJob.AddOrUpdate<GlobalMarketsJob>(
    "global-markets",
    job => job.ExecuteAsync(),
    "0 8 * * *");

// 3x/day — Commodities
RecurringJob.AddOrUpdate<CommoditiesJob>(
    "commodities",
    job => job.ExecuteAsync(),
    "0 8,14,20 * * *");

// Every 4h — Currency Rates (free API, no quota limit)
RecurringJob.AddOrUpdate<CurrencyRatesJob>(
    "currency-rates",
    job => job.ExecuteAsync(),
    "0 */4 * * *");

// ?? ???? — Interest Rates
RecurringJob.AddOrUpdate<InterestRatesJob>(
    "interest-rates",
    job => job.ExecuteAsync(),
    "0 * * * *");
RecurringJob.AddOrUpdate<LocalStockExchangeJob>(
    "local-stock",
    job => job.ExecuteAsync(),
    "0 15 * * 0-4");  // يومياً 3 PM (بعد إغلاق السوق 1:30 PM)

RecurringJob.AddOrUpdate<LocalIndicatorsJob>(
    "local-indicators",
    job => job.ExecuteAsync(),
    "0 8 * * 0-4");   // يومياً 8 AM
// Replaced by news-local-rss / news-international below.
RecurringJob.RemoveIfExists("news");

// Every 30 min — local JO RSS/Atom feeds (high publish frequency)
RecurringJob.AddOrUpdate<NewsFeedJobs>(
    "news-local-rss",
    job => job.RunLocalRssAsync(),
    "*/30 * * * *");

// Every 2h — international feeds (BoE/BIS/Fed/ECB/trade press) + Guardian
RecurringJob.AddOrUpdate<NewsFeedJobs>(
    "news-international",
    job => job.RunInternationalAsync(),
    "0 */2 * * *");
RecurringJob.AddOrUpdate<CbjCircularsJob>(
    "cbj-circulars",
    job => job.ExecuteAsync(),
    "0 9 * * *");
RecurringJob.AddOrUpdate<BankAdsJob>(
    "bank-ads",
    job => job.ExecuteAsync(),
    "0 10 * * *");
RecurringJob.AddOrUpdate<BankPerformanceJob>(
    "bank-performance",
    job => job.ExecuteAsync(),
    "0 3 * * *");

// ?? News sources enabled at startup ?????????????????????????
{
    var newsApiEnabled = builder.Configuration.GetValue("News:NewsApi:Enabled", false);
    var guardianEnabled = builder.Configuration.GetValue("ExternalApis:Guardian:Enabled", true);
    var enabledFeedNames = (builder.Configuration.GetSection("News:Feeds").Get<List<FeedOptions>>() ?? [])
        .Where(f => f.Enabled)
        .Select(f => f.Name)
        .ToList();

    Log.Information(
        "News sources enabled at startup: NewsApiClient={NewsApiEnabled}, Guardian={GuardianEnabled}, " +
        "ConfigFeeds=[{FeedCount}] {FeedNames}",
        newsApiEnabled, guardianEnabled, enabledFeedNames.Count, string.Join(", ", enabledFeedNames));
}

app.MapControllers();

app.Run();