using Microsoft.AspNetCore.Mvc;

namespace AuthBridge.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Health check endpoint (public)
    /// </summary>
    [HttpGet]
    public IActionResult HealthCheck()
    {
        return Ok(new { 
            status = "ok", 
            service = "auth-bridge",
            timestamp = DateTime.UtcNow
        });
    }
}
