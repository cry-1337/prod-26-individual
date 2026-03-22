using System.Data.Common;
using LottyAB.Application.Interfaces;
using LottyAB.Infrastructure.Interfaces;
using LottyAB.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Tests;

public class BaseTestFactory : WebApplicationFactory<Program>
{
    private DbConnection? m_Connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        m_Connection = new SqliteConnection("DataSource=:memory:");
        m_Connection.Open();

        builder.ConfigureServices(services =>
        {
            services.AddDistributedMemoryCache();

            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<IApplicationDbContext, AppDbContext>(options =>
            {
                options.UseSqlite(m_Connection);
            });

            var hostedServices = services
                .Where(s => s.ServiceType == typeof(IHostedService))
                .ToList();
            foreach (var service in hostedServices)
            {
                services.Remove(service);
            }
        });

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "test-secret-key-that-is-long-enough-for-hmac-sha256-algorithm"
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        dbInitializer.InitializeAsync().GetAwaiter().GetResult();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        m_Connection?.Close();
        m_Connection?.Dispose();
    }
}