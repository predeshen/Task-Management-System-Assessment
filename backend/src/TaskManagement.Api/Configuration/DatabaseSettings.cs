namespace TaskManagement.Api.Configuration;

public class DatabaseSettings
{
    public const string SectionName = "DatabaseSettings";
    
    public bool EnableSensitiveDataLogging { get; set; } = false;
    public bool EnableDetailedErrors { get; set; } = false;
    public int CommandTimeout { get; set; } = 30;
    public int MaxRetryCount { get; set; } = 3;
    public bool EnableSeeding { get; set; } = false;
    public SeedDataSettings? SeedData { get; set; }
}

public class SeedDataSettings
{
    public bool CreateTestUsers { get; set; } = false;
    public bool CreateSampleTasks { get; set; } = false;
    public int TestUserCount { get; set; } = 3;
    public int TasksPerUser { get; set; } = 5;
}