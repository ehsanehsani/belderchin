using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace AuthBridge.Controllers;

[ApiController]
[Route("sessions")]
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

    [HttpGet("validate")]
    public IActionResult ValidateSession()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Unauthorized();
        }
        
        var token = authHeader.Substring("Bearer ".Length);
        
        if (token.StartsWith("auth_bridge_session_") && token.Length > 20)
        {
            return Ok(new { 
                valid = true,
                message = "Session is valid",
                token_type = "auth_bridge"
            });
        }
        
        return Unauthorized();
    }

    [HttpGet("whoami")]
    public async Task<IActionResult> GetSessionInfo()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Unauthorized();
        }
        
        var token = authHeader.Substring("Bearer ".Length);
        
        if (token.StartsWith("auth_bridge_session_") && token.Length > 20)
        {
            var sessionId = token.Substring("auth_bridge_session_".Length);
            
            var sessionDataElement = await GetSessionAsync(sessionId);
            if (sessionDataElement.HasValue)
            {
                var sessionData = sessionDataElement.Value;
                if (sessionData.ValueKind == JsonValueKind.Object)
                {
                    var response = new
                    {
                        active = sessionData.GetProperty("active").GetBoolean(),
                        identity = new
                        {
                            id = sessionData.GetProperty("identity_id").GetString(),
                            traits = new
                            {
                                phone = sessionData.GetProperty("phone").GetString(),
                                email = sessionData.GetProperty("email").GetString()
                            }
                        },
                        authenticator_assurance_level = sessionData.GetProperty("aal").GetString(),
                        expires_at = sessionData.GetProperty("expires_at").GetString(),
                        issued_at = sessionData.GetProperty("created_at").GetString(),
                        authentication_methods = new[]
                        {
                            new
                            {
                                method = sessionData.GetProperty("authentication_method").GetString(),
                                completed_at = sessionData.GetProperty("authenticated_at").GetString()
                            }
                        }
                    };
                    
                    return Ok(response);
                }
            }
        }
        
        return Unauthorized();
    }

    private async Task<JsonElement?> GetSessionAsync(string sessionId)
    {
        try
        {
            var json = await _redisDb.StringGetAsync($"session:{sessionId}");
            return json.HasValue ? JsonSerializer.Deserialize<JsonElement>(json) : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to get session from Redis: {ex.Message}");
            return null;
        }
    }
}
