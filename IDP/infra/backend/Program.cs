using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Couchbase;
using Couchbase.KeyValue;
using Couchbase.Management.Collections;
using Backend.Services;
using Backend.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Belderchin Backend API", 
        Version = "v1",
        Description = "Backend API for Belderchin Learning Platform with JWT Authentication"
    });
    
    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configuration
var configuration = builder.Configuration;

// Couchbase Configuration
var couchbaseConnectionString = configuration["Couchbase:ConnectionString"] ?? "couchbase://localhost";
var couchbaseUsername = configuration["Couchbase:Username"] ?? "admin";
var couchbasePassword = configuration["Couchbase:Password"] ?? "password";
var couchbaseBucketName = configuration["Couchbase:BucketName"] ?? "belderchin";

// Register simple in-memory database for now (instead of Couchbase)
var courses = new List<Course>();
var questions = new Dictionary<string, List<Question>>();
var users = new Dictionary<string, User>();
var userProfiles = new Dictionary<string, UserProfile>();
var userProgress = new Dictionary<string, List<UserProgress>>();

builder.Services.AddSingleton<IMemoryService, MemoryService>();
builder.Services.AddSingleton<ISeedingService, SeedingService>();

// Add Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("BackofficeOnly", policy => policy.RequireClaim("userType", "Backoffice"));
    options.AddPolicy("RegularOnly", policy => policy.RequireClaim("userType", "Regular"));
    options.AddPolicy("AuthenticatedOnly", policy => policy.RequireAuthenticatedUser());
});

// JWT Authentication
var jwtKey = configuration["Jwt:Key"] ?? "belderchin-secret-key-1234567890-abcdefghijklmnopqrstuvwxyz-12";
var jwtIssuer = configuration["Jwt:Issuer"] ?? "belderchin";
var jwtAudience = configuration["Jwt:Audience"] ?? "belderchin-users";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Middleware
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
