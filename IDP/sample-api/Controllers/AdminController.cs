using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sample_api.Models;
using sample_api.Services;

namespace sample_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")] // All endpoints require admin role
public class AdminController : ControllerBase
{
    private readonly ILogger<AdminController> _logger;
    private readonly IUserService _userService;

    public AdminController(ILogger<AdminController> logger, IUserService userService)
    {
        _logger = logger;
        _userService = userService;
    }

    /// <summary>
    /// Get all users - admin only
    /// </summary>
    [HttpGet("users")]
    public IActionResult GetAllUsers()
    {
        var adminId = User.FindFirst("sub")?.Value;
        _logger.LogInformation($"Admin {adminId} requested all users list");

        var users = _userService.GetAllUsers();
        
        return Ok(new
        {
            message = "All users retrieved successfully",
            count = users.Count,
            users = users,
            requestedBy = adminId,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get system statistics - admin only
    /// </summary>
    [HttpGet("statistics")]
    public IActionResult GetSystemStatistics()
    {
        var adminId = User.FindFirst("sub")?.Value;
        _logger.LogInformation($"Admin {adminId} requested system statistics");

        var stats = _userService.GetSystemStatistics();
        
        return Ok(new
        {
            message = "System statistics retrieved successfully",
            statistics = stats,
            requestedBy = adminId,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Update user role - admin only
    /// </summary>
    [HttpPut("users/{userId}/role")]
    public IActionResult UpdateUserRole(string userId, [FromBody] UpdateRoleRequest request)
    {
        var adminId = User.FindFirst("sub")?.Value;
        _logger.LogInformation($"Admin {adminId} updating role for user {userId} to {request.Role}");

        var result = _userService.UpdateUserRole(userId, request.Role);
        
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new
        {
            message = "User role updated successfully",
            userId = userId,
            newRole = request.Role,
            updatedBy = adminId,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Delete user - admin only
    /// </summary>
    [HttpDelete("users/{userId}")]
    public IActionResult DeleteUser(string userId)
    {
        var adminId = User.FindFirst("sub")?.Value;
        _logger.LogInformation($"Admin {adminId} deleting user {userId}");

        var result = _userService.DeleteUser(userId);
        
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new
        {
            message = "User deleted successfully",
            userId = userId,
            deletedBy = adminId,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get admin dashboard data - admin only
    /// </summary>
    [HttpGet("dashboard")]
    public IActionResult GetAdminDashboard()
    {
        var adminId = User.FindFirst("sub")?.Value;
        _logger.LogInformation($"Admin {adminId} requested dashboard data");

        var dashboard = _userService.GetAdminDashboard();
        
        return Ok(new
        {
            message = "Admin dashboard data retrieved successfully",
            dashboard = dashboard,
            requestedBy = adminId,
            timestamp = DateTime.UtcNow
        });
    }
}
