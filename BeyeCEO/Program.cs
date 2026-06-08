using BeyeCEO.Domain.Auth.Interfaces;
using BeyeCEO.Domain.KPIs.Interfaces;
using BeyeCEO.Domain.MarketData.Interfaces;
using BeyeCEO.Domain.News.Interfaces;
using BeyeCEO.Infrastructure.Identity;
using BeyeCEO.Infrastructure.Persistence;
using BeyeCEO.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
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
builder.Services.AddSwaggerGen();
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
app.MapControllers();

app.Run();