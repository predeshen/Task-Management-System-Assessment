using TaskManagement.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add application configuration
builder.Services.AddApplicationConfiguration(builder.Configuration);

// Add database configuration
builder.Services.AddDatabaseConfiguration(builder.Configuration);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Automatically handle database migrations and seeding on startup
await app.UseDatabaseMigrationAndSeeding();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
