using TaskManagement.Api.Configuration;

namespace TaskManagement.Api.Extensions;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddApplicationConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind configuration sections to strongly typed classes
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<ApiSettings>(configuration.GetSection(ApiSettings.SectionName));
        services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));
        
        // Validate configuration on startup
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
            
        services.AddOptions<ApiSettings>()
            .Bind(configuration.GetSection(ApiSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
            
        services.AddOptions<DatabaseSettings>()
            .Bind(configuration.GetSection(DatabaseSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        return services;
    }
    
    public static JwtSettings GetJwtSettings(this IConfiguration configuration)
    {
        var jwtSettings = new JwtSettings();
        configuration.GetSection(JwtSettings.SectionName).Bind(jwtSettings);
        return jwtSettings;
    }
    
    public static ApiSettings GetApiSettings(this IConfiguration configuration)
    {
        var apiSettings = new ApiSettings();
        configuration.GetSection(ApiSettings.SectionName).Bind(apiSettings);
        return apiSettings;
    }
    
    public static DatabaseSettings GetDatabaseSettings(this IConfiguration configuration)
    {
        var databaseSettings = new DatabaseSettings();
        configuration.GetSection(DatabaseSettings.SectionName).Bind(databaseSettings);
        return databaseSettings;
    }
}