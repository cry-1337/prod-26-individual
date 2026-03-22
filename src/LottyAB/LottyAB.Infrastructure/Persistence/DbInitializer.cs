using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using LottyAB.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Infrastructure.Persistence;

public class DbInitializer(AppDbContext context) : IDbInitializer
{
    public async Task InitializeAsync()
    {
        if (context.Database.IsSqlite())
            await context.Database.EnsureCreatedAsync();
        else if ((await context.Database.GetPendingMigrationsAsync()).Any())
            await context.Database.MigrateAsync();

        var adminId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        if (!await context.Users.AnyAsync(u => u.Id == adminId))
        {
            context.Users.Add(new UserEntity
            {
                Id = adminId,
                Email = "admin@lottyab.com",
                Name = "admin",
                Role = EUserRole.Admin,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("lottyab"),
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
            await context.SaveChangesAsync();
        }

        if (!await context.EventTypes.AnyAsync())
        {
            var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            context.EventTypes.AddRange(
                new EventTypeEntity
                {
                    EventKey = "exposure",
                    DisplayName = "Exposure",
                    Description = "User was exposed to a variant",
                    RequiresExposure = false,
                    IsExposureEvent = true,
                    CreatedAt = baseTime,
                    UpdatedAt = baseTime
                },
                new EventTypeEntity
                {
                    EventKey = "click",
                    DisplayName = "Click",
                    Description = "User clicked on an element",
                    RequiresExposure = true,
                    IsExposureEvent = false,
                    CreatedAt = baseTime,
                    UpdatedAt = baseTime
                },
                new EventTypeEntity
                {
                    EventKey = "conversion",
                    DisplayName = "Conversion",
                    Description = "User completed a conversion action",
                    RequiresExposure = true,
                    IsExposureEvent = false,
                    CreatedAt = baseTime,
                    UpdatedAt = baseTime
                },
                new EventTypeEntity
                {
                    EventKey = "error",
                    DisplayName = "Error",
                    Description = "An error occurred",
                    RequiresExposure = true,
                    IsExposureEvent = false,
                    CreatedAt = baseTime,
                    UpdatedAt = baseTime
                }
            );

            await context.SaveChangesAsync();
        }

        if (!await context.MetricDefinitions.AnyAsync())
        {
            var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            context.MetricDefinitions.AddRange(
                new MetricDefinitionEntity
                {
                    MetricKey = "conversion_rate",
                    DisplayName = "Conversion Rate",
                    Description = "Percentage of exposures that resulted in conversion",
                    AggregationType = "rate",
                    EventTypeKeys = "exposure,conversion",
                    CreatedAt = baseTime,
                    UpdatedAt = baseTime
                },
                new MetricDefinitionEntity
                {
                    MetricKey = "click_through_rate",
                    DisplayName = "Click Through Rate",
                    Description = "Percentage of exposures that resulted in click",
                    AggregationType = "rate",
                    EventTypeKeys = "exposure,click",
                    CreatedAt = baseTime,
                    UpdatedAt = baseTime
                },
                new MetricDefinitionEntity
                {
                    MetricKey = "error_rate",
                    DisplayName = "Error Rate",
                    Description = "Percentage of exposures that resulted in error",
                    AggregationType = "rate",
                    EventTypeKeys = "exposure,error",
                    CreatedAt = baseTime,
                    UpdatedAt = baseTime
                },
                new MetricDefinitionEntity
                {
                    MetricKey = "total_exposures",
                    DisplayName = "Total Exposures",
                    Description = "Total number of exposures",
                    AggregationType = "count",
                    EventTypeKeys = "exposure",
                    CreatedAt = baseTime,
                    UpdatedAt = baseTime
                }
            );

            await context.SaveChangesAsync();
        }
    }
}