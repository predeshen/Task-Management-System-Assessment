using Microsoft.Extensions.Options;
using TaskManagement.Api.Configuration;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Api.Services;

public interface IDatabaseSeeder
{
    Task SeedAsync();
}

public class DatabaseSeeder : IDatabaseSeeder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DatabaseSettings _databaseSettings;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        IServiceProvider serviceProvider,
        IOptions<DatabaseSettings> databaseSettings,
        ILogger<DatabaseSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _databaseSettings = databaseSettings.Value;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (!_databaseSettings.EnableSeeding || _databaseSettings.SeedData == null)
        {
            _logger.LogInformation("Database seeding is disabled");
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
        
        try
        {
            _logger.LogInformation("Starting database seeding...");
            
            await DbSeeder.SeedAsync(context);
            
            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }
}