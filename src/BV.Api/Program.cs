using System.Text;
using BV.Api.Middleware;
using BV.Application.Abstractions.Admin;
using BV.Application.Abstractions.Authentication;
using BV.Application.Abstractions.Customers;
using BV.Application.Abstractions.Notifications;
using BV.Application.Abstractions.Quotes;
using BV.Application.Abstractions.Users;
using BV.Infrastructure.Authentication;
using BV.Infrastructure.Integrations;
using BV.Infrastructure.Notifications;
using BV.Persistence;
using BV.Persistence.Queries;
using BV.Persistence.Repositories;
using BV.Persistence.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT access token girin."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        }] = Array.Empty<string>()
    });
});
builder.Services.AddHealthChecks();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<NetGsmOptions>(builder.Configuration.GetSection(NetGsmOptions.SectionName));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.Configure<MikroOptions>(builder.Configuration.GetSection(MikroOptions.SectionName));
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IOtpCodeRepository, OtpCodeRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICustomerProfileRepository, CustomerProfileRepository>();
builder.Services.AddScoped<IQuoteRequestRepository, QuoteRequestRepository>();
builder.Services.AddScoped<IQuoteResponseRepository, QuoteResponseRepository>();
builder.Services.AddScoped<IAdminDashboardQuery, AdminDashboardQuery>();
builder.Services.AddScoped<IAdminQuoteOperations, AdminQuoteOperations>();
builder.Services.AddScoped<IAdminNotificationService, AdminNotificationService>();
builder.Services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();

var smtpOptions = builder.Configuration.GetSection(SmtpOptions.SectionName).Get<SmtpOptions>() ?? new();
if (smtpOptions.Enabled)
    builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
else
    builder.Services.AddScoped<IEmailSender, DevelopmentEmailSender>();

var netGsmOptions = builder.Configuration.GetSection(NetGsmOptions.SectionName).Get<NetGsmOptions>() ?? new();
if (netGsmOptions.Enabled)
{
    builder.Services.AddHttpClient<ISmsSender, NetGsmSmsSender>(client =>
    {
        client.BaseAddress = new Uri(netGsmOptions.BaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(30);
    });
}
else
{
    builder.Services.AddScoped<ISmsSender, DevelopmentSmsSender>();
}

var mikroOptions = builder.Configuration.GetSection(MikroOptions.SectionName).Get<MikroOptions>() ?? new();
builder.Services.AddHttpClient("MikroBridge", client =>
{
    if (!string.IsNullOrWhiteSpace(mikroOptions.BaseUrl))
        client.BaseAddress = new Uri(mikroOptions.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromMinutes(2);
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is not configured.");

builder.Services.AddDbContext<BVPortalDbContext>(options => options.UseSqlServer(connectionString));

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");

if (jwtOptions.Key.Length < 32)
    throw new InvalidOperationException("JWT key must contain at least 32 characters.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuditLoggingMiddleware>();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
