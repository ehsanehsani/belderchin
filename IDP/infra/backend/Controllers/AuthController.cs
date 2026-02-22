using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using Backend.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMemoryService _memoryService;
    private readonly string _jwtKey;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;

    public AuthController(IMemoryService memoryService, IConfiguration configuration)
    {
        _memoryService = memoryService;
        _jwtKey = configuration["Jwt:Key"] ?? "belderchin-secret-key-1234567890-abcdefghijklmnopqrstuvwxyz-12";
        _jwtIssuer = configuration["Jwt:Issuer"] ?? "belderchin";
        _jwtAudience = configuration["Jwt:Audience"] ?? "belderchin-users";
    }

    /// <summary>
    /// Login with phone number and get JWT token
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Validate session token with Auth-Bridge (simplified for demo)
        var user = await _memoryService.GetUserByPhoneNumberAsync(request.PhoneNumber);
        
        if (user == null)
        {
            // Auto-create user if not exists (for demo purposes)
            user = new User
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = request.PhoneNumber,
                UserType = UserType.Regular, // Default to regular user
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            await _memoryService.CreateUserAsync(user);
        }
        
        // Generate JWT token
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("userId", user.Id),
                new Claim("phoneNumber", user.PhoneNumber),
                new Claim("userType", user.UserType.ToString()),
                new Claim(ClaimTypes.Role, user.UserType.ToString())
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = _jwtIssuer,
            Audience = _jwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);
        
        return Ok(new LoginResponse
        {
            Token = tokenString,
            User = new UserDto
            {
                Id = user.Id,
                PhoneNumber = user.PhoneNumber,
                UserType = user.UserType,
                CreatedAt = user.CreatedAt
            }
        });
    }
}
