namespace TaskManagement.Api.Configuration;

public class ApiSettings
{
    public const string SectionName = "ApiSettings";
    
    public string BaseUrl { get; set; } = string.Empty;
    public string Version { get; set; } = "v1";
    public bool EnableSwagger { get; set; } = false;
    public bool EnableCors { get; set; } = true;
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}