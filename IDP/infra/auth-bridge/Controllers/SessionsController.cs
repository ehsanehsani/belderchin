using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace AuthBridge.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly IDatabase _redisDb;
    private readonly string _kratosAdminUrl;
    private readonly string _kratosPublicUrl;

    public SessionsController(
        IDatabase redisDb,
        IConfiguration configuration)
    {
        _redisDb = redisDb;
        
        var kratosConfig = configuration.GetSection("Kratos");
        _kratosAdminUrl = kratosConfig["AdminUrl"] ?? "http://kratos:4434";
        _kratosPublicUrl = kratosConfig["PublicUrl"] ?? "http://kratos:4433";
    }

    /// <summary>
    /// Validate session token
    /// </summary>
    [HttpGet("validate")]
    public IActionResult ValidateSession()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return Unauthorized(new { error = "missing or invalid authorization header" });

        var token = authHeader["Bearer ".Length..];
        
        try
        {
            // Get session from Redis
            var sessionData = _redisDb.StringGet($"session:{token}");
            if (sessionData.HasValue)
            {
                var session = JsonSerializer.Deserialize<JsonElement>(sessionData);
                return Ok(new 
                { 
                    valid = true,
                    session = session,
                    expiresAt = session.GetProperty("expiresAt").GetDateTime()
                });
            }

            return Unauthorized(new { error = "session not found or expired" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Session validation error: {ex.Message}");
            return StatusCode(500, new { error = "session validation failed" });
        }
    }

    /// <summary>
    /// Get current session information
    /// </summary>
    [HttpGet("whoami")]
    public async Task<IActionResult> GetSessionInfo()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return Unauthorized(new { error = "missing or invalid authorization header" });

        var token = authHeader["Bearer ".Length..];
        
        try
        {
            // Get session from Redis
            var sessionData = await _redisDb.StringGetAsync($"session:{token}");
            if (!sessionData.HasValue)
                return Unauthorized(new { error = "session not found or expired" });

            var session = JsonSerializer.Deserialize<JsonElement>(sessionData);
            var userId = session.GetProperty("userId").GetString();
            
            // Get user identity from Kratos
            using var httpClient = new HttpClient();
            var identityResponse = await httpClient.GetAsync($"{_kratosAdminUrl}/identities/{userId}");
            
            if (!identityResponse.IsSuccessStatusCode)
            {
                return NotFound(new { error = "user not found" });
            }

            var identityJson = await identityResponse.Content.ReadAsStringAsync();
            var identity = JsonSerializer.Deserialize<JsonElement>(identityJson);
            
            // Extract user data
            var traits = identity.GetProperty("traits");
            var phone = traits.GetProperty("phone").GetString();
            var email = traits.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
            
            return Ok(new 
            { 
                userId = userId,
                phone = phone,
                email = email,
                session = session,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Session whoami error: {ex.Message}");
            return StatusCode(500, new { error = "failed to get session info" });
        }
    }

    /// <summary>
    /// Create auth bridge session
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
    {
        try
        {
            // Validate the Kratos session
            using var httpClient = new HttpClient();
            var sessionResponse = await httpClient.GetAsync($"{_kratosPublicUrl}/sessions/whoami");
            
            if (!sessionResponse.IsSuccessStatusCode)
            {
                return Unauthorized(new { error = "invalid kratos session" });
            }

            var sessionJson = await sessionResponse.Content.ReadAsStringAsync();
            var kratosSession = JsonSerializer.Deserialize<JsonElement>(sessionJson);
            
            var userId = kratosSession.GetProperty("identity").GetProperty("id").GetString();
            var traits = kratosSession.GetProperty("identity").GetProperty("traits");
            var phone = traits.GetProperty("phone").GetString();
            
            // Create bridge session token
            var bridgeToken = Guid.NewGuid().ToString();
            
            // Store in Redis with 24h expiry
            var bridgeSession = new
            {
                userId = userId,
                phone = phone,
                email = request.Email,
                createdAt = DateTime.UtcNow,
                expiresAt = DateTime.UtcNow.AddHours(24),
                kratosSession = kratosSession
            };
            
            await _redisDb.StringSetAsync($"session:{bridgeToken}", 
                JsonSerializer.Serialize(bridgeSession), 
                TimeSpan.FromHours(24));

            return Ok(new 
            { 
                token = bridgeToken,
                userId = userId,
                phone = phone,
                expiresAt = DateTime.UtcNow.AddHours(24)
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Session creation error: {ex.Message}");
            return StatusCode(500, new { error = "failed to create session" });
        }
    }

    /// <summary>
    /// Logout and invalidate session
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return BadRequest(new { error = "missing authorization header" });

        var token = authHeader["Bearer ".Length..];
        
        try
        {
            // Remove session from Redis
            await _redisDb.KeyDeleteAsync($"session:{token}");
            
            return Ok(new { message = "logged out successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Logout error: {ex.Message}");
            return StatusCode(500, new { error = "failed to logout" });
        }
    }
}

public record CreateSessionRequest(string? Email);
