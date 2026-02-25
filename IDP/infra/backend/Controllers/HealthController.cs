using Microsoft.AspNetCore.Mvc;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IMemoryService _memoryService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(IMemoryService memoryService, ILogger<HealthController> logger)
    {
        _memoryService = memoryService;
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint (public)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> HealthCheck()
    {
        var isHealthy = await _memoryService.IsHealthyAsync();
        
        return Ok(new
        {
            status = isHealthy ? "ok" : "error",
            service = "belderchin-backend",
            database = "memory",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
}
