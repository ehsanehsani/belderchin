using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AuthenticatedOnly")]
public class UsersController : ControllerBase
{
    private readonly IMemoryService _memoryService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IMemoryService memoryService, ILogger<UsersController> logger)
    {
        _memoryService = memoryService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's profile
    /// </summary>
    [HttpGet("me/profile")]
    public async Task<IActionResult> GetUserProfile()
    {
        var userId = User.FindFirst("userId")?.Value;
        _logger.LogInformation("User {UserId} requested their profile", userId);

        var user = await _memoryService.GetUserAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found. Please complete authentication in Auth-Bridge first." });
        }
        
        var profile = await _memoryService.GetUserProfileAsync(userId);
        if (profile == null)
        {
            // Create default profile if it doesn't exist
            profile = new Backend.Models.UserProfile
            {
                UserId = userId,
                DisplayName = user.PhoneNumber, // Use phone as default display name
                Avatar = "",
                Preferences = new Dictionary<string, object>(),
                Statistics = new Backend.Models.UserStatistics(),
                CreatedAt = user.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };
        }
        
        return Ok(new
        {
            message = "User profile retrieved successfully",
            user = new UserDto
            {
                Id = user.Id,
                PhoneNumber = user.PhoneNumber,
                UserType = user.UserType,
                CreatedAt = user.CreatedAt
            },
            profile = profile,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Update current user's profile
    /// </summary>
    [HttpPut("me/profile")]
    public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = User.FindFirst("userId")?.Value;
        _logger.LogInformation("User {UserId} updating their profile", userId);

        var profile = await _memoryService.UpdateUserProfileAsync(userId, request);
        
        return Ok(new
        {
            message = "Profile updated successfully",
            profile = profile,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get current user's progress
    /// </summary>
    [HttpGet("me/progress")]
    public async Task<IActionResult> GetUserProgress()
    {
        var userId = User.FindFirst("userId")?.Value;
        _logger.LogInformation("User {UserId} requested their progress", userId);

        // Check if user exists in Auth-Bridge
        var user = await _memoryService.GetUserAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found. Please complete authentication in Auth-Bridge first." });
        }

        var progress = await _memoryService.GetUserProgressAsync(userId);
        
        return Ok(new
        {
            message = "User progress retrieved successfully",
            progress = progress,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Submit progress for a course
    /// </summary>
    [HttpPost("me/progress/{courseId}")]
    public async Task<IActionResult> SubmitProgress(string courseId, [FromBody] SubmitProgressRequest request)
    {
        var userId = User.FindFirst("userId")?.Value;
        _logger.LogInformation("User {UserId} submitting progress for course {CourseId}", userId, courseId);

        // Check if user exists in Auth-Bridge
        var user = await _memoryService.GetUserAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found. Please complete authentication in Auth-Bridge first." });
        }

        var progress = await _memoryService.UpdateUserProgressAsync(userId, courseId, request.Answers, request.Score);
        
        return Ok(new
        {
            message = "Progress submitted successfully",
            progress = progress,
            courseId = courseId,
            timestamp = DateTime.UtcNow
        });
    }
}
