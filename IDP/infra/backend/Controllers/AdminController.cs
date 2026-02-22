using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "BackofficeOnly")]
public class AdminController : ControllerBase
{
    private readonly IMemoryService _memoryService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IMemoryService memoryService, ILogger<AdminController> logger)
    {
        _memoryService = memoryService;
        _logger = logger;
    }

    /// <summary>
    /// Seed sample data (Admin only)
    /// </summary>
    [HttpPost("seed")]
    public async Task<IActionResult> SeedData()
    {
        var userId = User.FindFirst("userId")?.Value;
        _logger.LogInformation("Admin {UserId} requested data seeding", userId);

        try
        {
            var seedingService = new SeedingService(_memoryService, 
                Microsoft.Extensions.Logging.Abstractions.NullLogger<SeedingService>.Instance);
            await seedingService.SeedDataAsync();
            
            return Ok(new
            {
                message = "Sample data seeded successfully",
                seededBy = userId,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding sample data");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get system statistics (Admin only)
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetSystemStatistics()
    {
        var userId = User.FindFirst("userId")?.Value;
        _logger.LogInformation("Admin {UserId} requested system statistics", userId);

        // In a real implementation, this would query the database for statistics
        var stats = new
        {
            totalUsers = 0,
            totalCourses = 0,
            totalQuestions = 0,
            activeUsers = 0,
            completionRate = 0.0
        };
        
        return Ok(new
        {
            message = "System statistics retrieved successfully",
            statistics = stats,
            requestedBy = userId,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get admin dashboard data (Admin only)
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetAdminDashboard()
    {
        var userId = User.FindFirst("userId")?.Value;
        _logger.LogInformation("Admin {UserId} requested dashboard data", userId);

        // In a real implementation, this would aggregate dashboard data
        var dashboard = new
        {
            recentActivity = new object[0],
            userGrowth = new object[0],
            courseCompletion = new object[0],
            systemHealth = new { status = "healthy", uptime = "100%" }
        };
        
        return Ok(new
        {
            message = "Admin dashboard data retrieved successfully",
            dashboard = dashboard,
            requestedBy = userId,
            timestamp = DateTime.UtcNow
        });
    }
}
