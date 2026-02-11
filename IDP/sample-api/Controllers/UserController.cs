using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sample_api.Models;
using sample_api.Services;

namespace sample_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // All endpoints in this controller require authentication
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;
    private readonly IUserService _userService;

    public UserController(ILogger<UserController> logger, IUserService userService)
    {
        _logger = logger;
        _userService = userService;
    }

    /// <summary>
    /// Get current user profile - requires authentication
    /// </summary>
    [HttpGet("profile")]
    public IActionResult GetUserProfile()
    {
        var userId = User.FindFirst("sub")?.Value;
        var phone = User.FindFirst("phone")?.Value;
        var email = User.FindFirst("email")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var userProfile = _userService.GetUserProfile(userId);
        
        return Ok(new
        {
            message = "User profile retrieved successfully",
            user = userProfile,
            tokenInfo = new
            {
                userId = userId,
                phone = phone,
                email = email,
                isAuthenticated = User.Identity?.IsAuthenticated ?? false
            }
        });
    }

    /// <summary>
    /// Get user data - requires authentication
    /// </summary>
    [HttpGet("data")]
    public IActionResult GetUserData()
    {
        var userId = User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var userData = _userService.GetUserData(userId);
        
        return Ok(new
        {
            message = "User data retrieved successfully",
            data = userData,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Update user profile - requires authentication
    /// </summary>
    [HttpPut("profile")]
    public IActionResult UpdateUserProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var updatedProfile = _userService.UpdateUserProfile(userId, request);
        
        return Ok(new
        {
            message = "Profile updated successfully",
            profile = updatedProfile,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get user preferences - requires authentication
    /// </summary>
    [HttpGet("preferences")]
    public IActionResult GetUserPreferences()
    {
        var userId = User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var preferences = _userService.GetUserPreferences(userId);
        
        return Ok(new
        {
            message = "User preferences retrieved successfully",
            preferences = preferences
        });
    }
}
