using StackExchange.Redis;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Auth Bridge API", 
        Version = "v1",
        Description = "Authentication Bridge Service for Belderchin IDP"
    });
});

// Add Redis configuration
var redisConfig = builder.Configuration.GetSection("Redis");
var redisConnectionString = redisConfig["ConnectionString"] ?? "localhost:6379";

// Register Redis as a singleton
builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    return ConnectionMultiplexer.Connect(redisConnectionString);
});

// Register IDatabase as a scoped service
builder.Services.AddScoped<IDatabase>(provider =>
{
    var multiplexer = provider.GetRequiredService<IConnectionMultiplexer>();
    return multiplexer.GetDatabase();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
