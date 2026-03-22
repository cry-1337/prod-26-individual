using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using LottyAB.Api.Middleware;
using LottyAB.Application.Behaviors;
using LottyAB.Application.Commands;
using LottyAB.Application.Interfaces;
using LottyAB.Application.Services;
using LottyAB.Application.Targeting;
using LottyAB.Infrastructure.Interfaces;
using LottyAB.Infrastructure.Persistence;
using LottyAB.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Prometheus;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, config) =>
    config
        .ReadFrom.Configuration(ctx.Configuration)
        .WriteTo.Console(new RenderedCompactJsonFormatter()));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new()
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!)),
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("ADMIN", policy => policy.RequireRole("ADMIN"))
    .AddPolicy("EXPERIMENTER", policy => policy.RequireRole("EXPERIMENTER", "ADMIN"))
    .AddPolicy("APPROVER", policy => policy.RequireRole("APPROVER", "ADMIN"))
    .AddPolicy("VIEWER", policy => policy.RequireRole("VIEWER", "EXPERIMENTER", "APPROVER", "ADMIN"));

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(DecideCommand).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(typeof(DecideCommand).Assembly);

if (builder.Environment.EnvironmentName != "Testing")
{
    builder.Services.AddDbContext<IApplicationDbContext, AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
}

builder.Services.AddScoped<IHashVariantSelector, HashVariantSelector>();

builder.Services.AddSingleton<IValueComparer, ValueComparer>();
builder.Services.AddScoped<ITargetingParser, TargetingParser>();
builder.Services.AddScoped<ITargetingEvaluator, TargetingEvaluatorService>();

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IMetricCalculator, MetricCalculator>();
builder.Services.AddScoped<IValueTypeConverter, ValueTypeConverter>();

if (builder.Environment.EnvironmentName != "Testing")
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis");
        options.InstanceName = "lottyab:";
    });
}

builder.Services.AddScoped<IDbInitializer, DbInitializer>();

builder.Services.AddHttpClient();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddHostedService<EventAttributionService>();
builder.Services.AddHostedService<GuardrailMonitoringService>();
builder.Services.AddHostedService<AutopilotRampService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddHealthChecks();

builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "LottyAB API";
        options.Authentication = new ScalarAuthenticationOptions
        {
            PreferredSecuritySchemes = ["Bearer"]
        };
    });
}

if (app.Environment.EnvironmentName != "Testing")
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<IDbInitializer>().InitializeAsync();
}

app.UseExceptionHandler();

app.UseSerilogRequestLogging();
app.UseHttpMetrics();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");
app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapMetrics();

app.Run();

public partial class Program { }