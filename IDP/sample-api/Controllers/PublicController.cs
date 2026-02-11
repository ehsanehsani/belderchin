using Microsoft.AspNetCore.Mvc;

namespace sample_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PublicController : ControllerBase
{
    private readonly ILogger<PublicController> _logger;

    public PublicController(ILogger<PublicController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get public information - no authentication required
    /// </summary>
    [HttpGet("info")]
    public IActionResult GetPublicInfo()
    {
        return Ok(new
        {
            message = "This is a public endpoint - no authentication required",
            timestamp = DateTime.UtcNow,
            server = Environment.MachineName,
            version = "1.0.0"
        });
    }

    /// <summary>
    /// Get system status - no authentication required
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetSystemStatus()
    {
        return Ok(new
        {
            status = "operational",
            uptime = DateTime.UtcNow,
            services = new
            {
                database = "connected",
                authentication = "ORY IDP",
                authorization = "Role-based"
            }
        });
    }

    /// <summary>
    /// Echo endpoint for testing - no authentication required
    /// </summary>
    [HttpPost("echo")]
    public IActionResult Echo([FromBody] object data)
    {
        return Ok(new
        {
            received = data,
            timestamp = DateTime.UtcNow,
            message = "Echo successful - this is a public endpoint"
        });
    }
}
