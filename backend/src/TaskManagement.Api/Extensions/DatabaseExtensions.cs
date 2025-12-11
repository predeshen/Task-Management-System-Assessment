using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskManagement.Api.Configuration;
using TaskManagement.Api.Services;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Api.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseSettings = configuration.GetDatabaseSettings();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("DefaultConnection connection string is not configured");
        }
        
        services.AddDbContext<TaskManagementDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.CommandTimeout(databaseSettings.CommandTimeout);
                sqlOptions.EnableRetryOnFailure(databaseSettings.MaxRetryCount);
            });
            
            if (databaseSettings.EnableSensitiveDataLogging)
            {
                options.EnableSensitiveDataLogging();
            }
            
            if (databaseSettings.EnableDetailedErrors)
            {
                options.EnableDetailedErrors();
            }
        });
        
        services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();
        
        return services;
    }
    
    public static async Task<IApplicationBuilder> UseDatabaseMigrationAndSeeding(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        try
        {
            var context = services.GetRequiredService<TaskManagementDbContext>();
            await context.Database.MigrateAsync();
            
            logger.LogInformation("Database migration completed");
            
            var seeder = services.GetRequiredService<IDatabaseSeeder>();
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating or seeding the database");
            throw;
        }
        
        return app;
    }
}