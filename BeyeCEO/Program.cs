using BeyeCEO.Domain.Auth.Interfaces;
using BeyeCEO.Domain.KPIs.Interfaces;
using BeyeCEO.Domain.MarketData.Interfaces;
using BeyeCEO.Domain.News.Interfaces;
using BeyeCEO.Infrastructure.BackgroundJobs;
using BeyeCEO.Infrastructure.ExternalServices;
using BeyeCEO.Infrastructure.Identity;
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
////////////////////////////////////
builder.Services.AddHttpClient<AlphaVantageClient>();
builder.Services.AddHttpClient<FredClient>();
builder.Services.AddHttpClient<EiaClient>();
// Scrapers
builder.Services.AddHttpClient<ASEScraper>();
builder.Services.AddHttpClient<ASEClient>();
builder.Services.AddHttpClient<CBJScraper>();
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
builder.Services.AddCors(options =>
{
    options.AddPolicy("BeyeCEOPolicy", policy =>
    {
        policy
            .WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins")
                    .Get<string[]>() ?? Array.Empty<string>())
            .AllowAnyHeader()
            .AllowAnyMethod()
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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors("BeyeCEOPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire");
// ?? Register Background Jobs ??????????????????????????????


// ?? 15 ????? — Global Markets
RecurringJob.AddOrUpdate<GlobalMarketsJob>(
    "global-markets",
    job => job.ExecuteAsync(),
    "*/15 * * * *");

// ?? 15 ????? — Commodities
RecurringJob.AddOrUpdate<CommoditiesJob>(
    "commodities",
    job => job.ExecuteAsync(),
    "*/15 * * * *");

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
app.MapControllers();

app.Run();