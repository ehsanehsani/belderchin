using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AuthBridge.Controllers;

[ApiController]
[Route("verification")]
public class VerificationController : ControllerBase
{
    private readonly IDatabase _redisDb;
    private readonly IConfiguration _configuration;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _smtpFrom;
    private readonly string _kratosAdminUrl;
    private readonly string _kratosPublicUrl;
    private readonly string _kratosSchemaId;
    private readonly string _jwtKey;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;

    public VerificationController(
        IDatabase redisDb,
        IConfiguration configuration)
    {
        _redisDb = redisDb;
        _configuration = configuration;
        
        var smtpConfig = configuration.GetSection("Smtp");
        _smtpHost = smtpConfig["Host"] ?? "localhost";
        _smtpPort = int.Parse(smtpConfig["Port"] ?? "1025");
        _smtpUsername = smtpConfig["Username"] ?? "";
        _smtpPassword = smtpConfig["Password"] ?? "";
        _smtpFrom = smtpConfig["From"] ?? "noreply@belderchin.local";
        
        var kratosConfig = configuration.GetSection("Kratos");
        _kratosAdminUrl = kratosConfig["AdminUrl"] ?? "http://kratos:4434";
        _kratosPublicUrl = kratosConfig["PublicUrl"] ?? "http://kratos:4433";
        _kratosSchemaId = kratosConfig["IdentitySchemaId"] ?? "phone_v1";
        
        var jwtConfig = configuration.GetSection("Jwt");
        _jwtKey = jwtConfig["Key"] ?? "belderchin-secret-key-1234567890-abcdefghijklmnopqrstuvwxyz-12";
        _jwtIssuer = jwtConfig["Issuer"] ?? "belderchin";
        _jwtAudience = jwtConfig["Audience"] ?? "belderchin-users";
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartVerification([FromBody] StartVerificationRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Phone) && string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { error = "phone or email is required" });

        var code = GenerateNumericCode(6);
        var id = Guid.NewGuid().ToString("N");

        var record = new CodeRecord(
            Id: id,
            Purpose: CodePurpose.Verification,
            Phone: req.Phone,
            Email: req.Email,
            Code: Hash(code),
            CreatedAt: DateTimeOffset.UtcNow,
            ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(10),
            Attempts: 0,
            MaxAttempts: 5,
            UserType: "Regular"
        );

        // Store in Redis with 10-minute expiration
        await _redisDb.StringSetAsync($"verification:{id}", JsonSerializer.Serialize(record), TimeSpan.FromMinutes(10));

        var smsOk = false;
        string? smsError = null;

        if (!string.IsNullOrWhiteSpace(req.Phone))
        {
            (smsOk, smsError) = await TrySendSms(req.Phone!, $"Your verification code is: {code}");
        }

        if (!smsOk)
        {
            if (string.IsNullOrWhiteSpace(req.Email))
            {
                return Problem(smsError ?? "SMS delivery failed and no email provided", statusCode: 502);
            }

            var emailOk = await TrySendEmail(req.Email!, $"Your verification code is: {code}", "Belderchin Verification", _smtpHost, _smtpPort, _smtpUsername, _smtpPassword, _smtpFrom);
            if (!emailOk)
            {
                return Problem("Both SMS and email delivery failed", statusCode: 502);
            }

            return Ok(new
            {
                id,
                delivery = "email",
                message = "SMS failed; verification code sent via email.",
                email = req.Email
            });
        }

        return Ok(new { id, delivery = "sms" });
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmVerification([FromBody] ConfirmCodeRequest req)
    {
        // Get verification record from Redis
        var recordJson = await _redisDb.StringGetAsync($"verification:{req.Id}");
        if (!recordJson.HasValue)
            return NotFound(new { error = "verification request not found" });

        var record = JsonSerializer.Deserialize<CodeRecord>(recordJson.ToString());
        if (record == null || record.Purpose != CodePurpose.Verification)
            return NotFound(new { error = "verification request not found" });

        var now = DateTimeOffset.UtcNow;
        if (record.ExpiresAt <= now)
        {
            // Remove expired record
            await _redisDb.KeyDeleteAsync($"verification:{req.Id}");
            return Problem("code expired", statusCode: 410);
        }

        if (record.Attempts >= record.MaxAttempts)
        {
            // Remove record with no attempts left
            await _redisDb.KeyDeleteAsync($"verification:{req.Id}");
            return Problem("too many attempts", statusCode: 429);
        }

        if (!FixedTimeEquals(record.Code, Hash(req.Code)))
        {
            // Update attempts in Redis
            var updatedRecord = record with { Attempts = record.Attempts + 1 };
            await _redisDb.StringSetAsync($"verification:{req.Id}", JsonSerializer.Serialize(updatedRecord), TimeSpan.FromMinutes(10));
            return Unauthorized();
        }

        // Code verified successfully - create/get user and issue JWT
        var (identityCreated, identityId, identityError) = await CreateKratosIdentity(record.Phone, record.Email);
        
        if (!identityCreated && identityError != null && !identityError.Contains("already exists"))
        {
            return Problem($"Failed to create user: {identityError}", statusCode: 500);
        }
        
        if (string.IsNullOrEmpty(identityId))
        {
            identityId = await GetIdentityId(record.Phone, record.Email);
        }
        
        if (string.IsNullOrEmpty(identityId))
        {
            return Problem("Could not find or create user", statusCode: 500);
        }

        // Store user data in Redis for backend services
        var userData = new
        {
            id = identityId,
            phone = record.Phone,
            email = record.Email,
            userType = record.UserType, // Keep for backward compatibility
            roles = record.UserType == "Backoffice" ? new[] { "Regular", "Backoffice" } : new[] { "Regular" }, // Multi-role support
            createdAt = DateTimeOffset.UtcNow,
            updatedAt = DateTimeOffset.UtcNow
        };
        
        await _redisDb.StringSetAsync($"user:{identityId}", JsonSerializer.Serialize(userData), TimeSpan.FromDays(365));

        // Generate JWT token
        var roles = record.UserType == "Backoffice" ? new[] { "Regular", "Backoffice" } : new[] { "Regular" };
        var jwtToken = GenerateJwtToken(identityId, record.Phone, record.Email, record.UserType, roles);

        // Remove verification record from Redis
        await _redisDb.KeyDeleteAsync($"verification:{req.Id}");
        
        Console.WriteLine($"✅ Verification successful and JWT token issued for user: {identityId}");
        return Ok(new { 
            ok = true, 
            message = "verification successful",
            jwt_token = jwtToken,
            token_type = "jwt",
            user = new
            {
                id = identityId,
                phone = record.Phone,
                email = record.Email,
                userType = record.UserType
            }
        });
    }

    [HttpPost("2fa/start")]
    public async Task<IActionResult> StartTwoFactor([FromBody] Start2FaRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Phone) && string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { error = "phone or email is required" });

        var code = GenerateNumericCode(6);
        var id = Guid.NewGuid().ToString("N");

        var record = new CodeRecord(
            Id: id,
            Purpose: CodePurpose.TwoFactor,
            Phone: req.Phone,
            Email: req.Email,
            Code: Hash(code),
            CreatedAt: DateTimeOffset.UtcNow,
            ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(5),
            Attempts: 0,
            MaxAttempts: 5,
            UserType: "Regular" // Default for 2FA (existing user)
        );

        // Store 2FA record in Redis with 5-minute expiration
        await _redisDb.StringSetAsync($"2fa:{id}", JsonSerializer.Serialize(record), TimeSpan.FromMinutes(5));

        var smsOk = false;
        string? smsError = null;

        if (!string.IsNullOrWhiteSpace(req.Phone))
        {
            (smsOk, smsError) = await TrySendSms(req.Phone!, $"Your 2FA code is: {code}");
        }

        if (!smsOk)
        {
            if (string.IsNullOrWhiteSpace(req.Email))
                return Problem(smsError ?? "SMS delivery failed and no email provided", statusCode: 502);

            var emailOk = await TrySendEmail(req.Email!, $"Your 2FA code is: {code}", "Belderchin 2FA", _smtpHost, _smtpPort, _smtpUsername, _smtpPassword, _smtpFrom);
            if (!emailOk)
            {
                return Problem("Both SMS and email delivery failed", statusCode: 502);
            }

            return Ok(new
            {
                id,
                delivery = "email",
                message = "SMS failed; 2FA code sent via email.",
                email = req.Email
            });
        }

        return Ok(new { id, delivery = "sms" });
    }

    [HttpPost("2fa/confirm")]
    public async Task<IActionResult> ConfirmTwoFactor([FromBody] ConfirmCodeRequest req)
    {
        // Get 2FA record from Redis
        var recordJson = await _redisDb.StringGetAsync($"2fa:{req.Id}");
        if (!recordJson.HasValue)
            return NotFound(new { error = "2fa request not found" });

        var record = JsonSerializer.Deserialize<CodeRecord>(recordJson.ToString());
        if (record == null || record.Purpose != CodePurpose.TwoFactor)
            return NotFound(new { error = "2fa request not found" });

        var now = DateTimeOffset.UtcNow;
        if (record.ExpiresAt <= now)
        {
            // Remove expired record
            await _redisDb.KeyDeleteAsync($"2fa:{req.Id}");
            return Problem("code expired", statusCode: 410);
        }

        if (record.Attempts >= record.MaxAttempts)
        {
            // Remove record with no attempts left
            await _redisDb.KeyDeleteAsync($"2fa:{req.Id}");
            return Problem("too many attempts", statusCode: 429);
        }

        if (!FixedTimeEquals(record.Code, Hash(req.Code)))
        {
            // Update attempts in Redis
            var updatedRecord = record with { Attempts = record.Attempts + 1 };
            await _redisDb.StringSetAsync($"2fa:{req.Id}", JsonSerializer.Serialize(updatedRecord), TimeSpan.FromMinutes(5));
            return Unauthorized();
        }

        // 2FA verified - get existing user and issue JWT
        var identityId = await GetIdentityId(record.Phone, record.Email);
        
        if (string.IsNullOrEmpty(identityId))
        {
            return Problem("User not found. Please complete verification first.", statusCode: 404);
        }

        // Get existing user data
        var userDataJson = await _redisDb.StringGetAsync($"user:{identityId}");
        if (!userDataJson.HasValue)
        {
            return Problem("User data not found", statusCode: 404);
        }

        var userData = JsonSerializer.Deserialize<JsonElement>(userDataJson.ToString());
        var userType = userData.GetProperty("userType").GetString() ?? "Regular";
        
        // Get roles from user data
        var roles = new List<string>();
        if (userData.TryGetProperty("roles", out var rolesProperty))
        {
            foreach (var role in rolesProperty.EnumerateArray())
            {
                roles.Add(role.GetString() ?? "Regular");
            }
        }
        else
        {
            // Fallback to single role for backward compatibility
            roles.Add(userType);
        }

        // Generate JWT token
        var jwtToken = GenerateJwtToken(identityId, record.Phone, record.Email, userType, roles.ToArray());

        // Remove 2FA record from Redis
        await _redisDb.KeyDeleteAsync($"2fa:{req.Id}");
        
        Console.WriteLine($"✅ 2FA verification successful and JWT token issued for user: {identityId}");
        return Ok(new { 
            ok = true, 
            message = "2FA successful",
            jwt_token = jwtToken,
            token_type = "jwt",
            user = new
            {
                id = identityId,
                phone = record.Phone,
                email = record.Email,
                userType = userType
            }
        });
    }

    private string GenerateJwtToken(string userId, string phone, string? email, string userType, string[] roles)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtKey);
        
        var claims = new List<Claim>
        {
            new Claim("userId", userId),
            new Claim("phoneNumber", phone),
            new Claim("userType", userType),
            new Claim("email", email ?? ""),
            new Claim("tokenSource", "auth-bridge")
        };
        
        // Add multiple role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
        
        // Also add roles as a custom claim for easier access
        claims.Add(new Claim("roles", string.Join(",", roles)));
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = _jwtIssuer,
            Audience = _jwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private string GenerateJwtToken(string userId, string phone, string? email, string userType)
    {
        // Backward compatibility - single role
        var roles = userType == "Backoffice" ? new[] { "Regular", "Backoffice" } : new[] { "Regular" };
        return GenerateJwtToken(userId, phone, email, userType, roles);
    }

    /// <summary>
    /// Get user profile by JWT token
    /// </summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetUserProfile()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Unauthorized();
        }
        
        var token = authHeader.Substring("Bearer ".Length);
        
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtKey);
            
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _jwtIssuer,
                ValidateAudience = true,
                ValidAudience = _jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };
            
            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            var userId = principal.FindFirst("userId")?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            
            // Get user data from Redis
            var userDataJson = await _redisDb.StringGetAsync($"user:{userId}");
            if (!userDataJson.HasValue)
            {
                return NotFound(new { error = "User not found" });
            }
            
            var userData = JsonSerializer.Deserialize<JsonElement>(userDataJson.ToString());
            
            return Ok(new
            {
                id = userData.GetProperty("id").GetString(),
                phone = userData.GetProperty("phone").GetString(),
                email = userData.GetProperty("email").GetString(),
                userType = userData.GetProperty("userType").GetString(),
                createdAt = userData.GetProperty("createdAt").GetString(),
                updatedAt = userData.GetProperty("updatedAt").GetString()
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Token validation error: {ex.Message}");
            return Unauthorized();
        }
    }

    /// <summary>
    /// Initialize system with first admin user (only works if no admin users exist)
    /// </summary>
    [HttpPost("initialize-admin")]
    public async Task<IActionResult> InitializeAdmin([FromBody] InitializeAdminRequest req)
    {
        // Check if admin users already exist
        var existingAdmins = await FindAdminUsers();
        if (existingAdmins.Any())
        {
            return BadRequest(new { error = "System already initialized. Admin users exist." });
        }

        if (string.IsNullOrWhiteSpace(req.Phone) && string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { error = "phone or email is required" });

        if (string.IsNullOrWhiteSpace(req.InitCode))
            return BadRequest(new { error = "initialization code is required" });

        // Validate initialization code
        if (!IsValidAdminInitCode(req.InitCode))
            return BadRequest(new { error = "invalid initialization code" });

        var verificationId = Guid.NewGuid().ToString();
        var code = GenerateNumericCode(6);

        // Store verification record with Admin user type
        var record = new CodeRecord(
            Id: verificationId,
            Purpose: CodePurpose.Verification,
            Phone: req.Phone,
            Email: req.Email,
            Code: Hash(code),
            CreatedAt: DateTimeOffset.UtcNow,
            ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(10),
            Attempts: 0,
            MaxAttempts: 3,
            UserType: "Admin" // Mark as Admin user
        );

        await _redisDb.StringSetAsync($"verification:{verificationId}", JsonSerializer.Serialize(record), TimeSpan.FromMinutes(10));

        // Send verification code
        if (!string.IsNullOrEmpty(req.Phone))
        {
            await TrySendSms(req.Phone, $"Your Belderchin Admin verification code is: {code}");
        }
        else if (!string.IsNullOrEmpty(req.Email))
        {
            await TrySendEmail(req.Email, $"Your admin verification code is: {code}", "Belderchin Admin Verification", _smtpHost, _smtpPort, _smtpUsername, _smtpPassword, _smtpFrom);
        }

        return Ok(new { 
            id = verificationId,
            message = "Admin verification code sent",
            method = string.IsNullOrEmpty(req.Phone) ? "email" : "sms"
        });
    }

    /// <summary>
    /// Register a new Backoffice user (requires admin approval or special invite code)
    /// </summary>
    [HttpPost("register-backoffice")]
    public async Task<IActionResult> RegisterBackofficeUser([FromBody] RegisterBackofficeRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Phone) && string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { error = "phone or email is required" });

        if (string.IsNullOrWhiteSpace(req.InviteCode))
            return BadRequest(new { error = "invite code is required for backoffice registration" });

        // Validate invite code (you can implement your own invite code logic)
        if (!IsValidBackofficeInviteCode(req.InviteCode))
            return BadRequest(new { error = "invalid invite code" });

        // Generate verification ID
        var verificationId = Guid.NewGuid().ToString();
        var code = GenerateNumericCode(6);

        // Store verification record with Backoffice user type
        var record = new CodeRecord(
            Id: verificationId,
            Purpose: CodePurpose.Verification,
            Phone: req.Phone,
            Email: req.Email,
            Code: Hash(code),
            CreatedAt: DateTimeOffset.UtcNow,
            ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(10),
            Attempts: 0,
            MaxAttempts: 3,
            UserType: "Backoffice" // Mark as Backoffice user
        );

        await _redisDb.StringSetAsync($"verification:{verificationId}", JsonSerializer.Serialize(record), TimeSpan.FromMinutes(10));

        // Send verification code
        var smsOk = false;
        string? smsError = null;
        
        if (!string.IsNullOrEmpty(req.Phone))
        {
            (smsOk, smsError) = await TrySendSms(req.Phone, $"Your Belderchin Backoffice verification code is: {code}");
        }

        if (!smsOk)
        {
            if (string.IsNullOrEmpty(req.Email))
                return Problem(smsError ?? "SMS delivery failed and no email provided", statusCode: 502);
            
            var emailOk = await TrySendEmail(req.Email!, $"Your Belderchin Backoffice verification code is: {code}", "Belderchin Backoffice Verification", _smtpHost, _smtpPort, _smtpUsername, _smtpPassword, _smtpFrom);
            if (!emailOk)
            {
                return Problem("Both SMS and email delivery failed", statusCode: 502);
            }
        }
        else if (!string.IsNullOrEmpty(req.Email))
        {
            // Also send email if provided
            await TrySendEmail(req.Email, $"Your Belderchin Backoffice verification code is: {code}", "Belderchin Backoffice Verification", _smtpHost, _smtpPort, _smtpUsername, _smtpPassword, _smtpFrom);
        }

        return Ok(new { 
            id = verificationId,
            message = "Backoffice verification code sent",
            method = string.IsNullOrEmpty(req.Phone) ? "email" : "sms"
        });
    }

    /// <summary>
    /// Promote existing user to Backoffice (requires existing Backoffice user)
    /// </summary>
    [HttpPut("promote-to-backoffice")]
    public async Task<IActionResult> PromoteToBackoffice([FromBody] PromoteUserRequest req)
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Unauthorized(new { error = "Authorization required" });
        }
        
        var token = authHeader.Substring("Bearer ".Length);
        
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtKey);
            
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _jwtIssuer,
                ValidateAudience = true,
                ValidAudience = _jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };
            
            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            var requesterUserType = principal.FindFirst("userType")?.Value;
            
            // Only existing Backoffice users can promote others
            if (requesterUserType != "Backoffice")
            {
                return Forbid();
            }
            
            // Get target user
            var userDataJson = await _redisDb.StringGetAsync($"user:{req.UserId}");
            if (!userDataJson.HasValue)
            {
                return NotFound(new { error = "User not found" });
            }
            
            var userData = JsonSerializer.Deserialize<JsonElement>(userDataJson.ToString());
            var currentUserType = userData.GetProperty("userType").GetString();
            
            if (currentUserType == "Backoffice")
            {
                return BadRequest(new { error = "User is already Backoffice" });
            }
            
            // Update user to Backoffice
            var updatedUserData = new
            {
                id = req.UserId,
                phone = userData.GetProperty("phone").GetString(),
                email = userData.GetProperty("email").GetString(),
                userType = "Backoffice",
                displayName = userData.GetProperty("displayName").GetString(),
                avatar = userData.GetProperty("avatar").GetString(),
                preferences = JsonSerializer.Deserialize<Dictionary<string, object>>(userData.GetProperty("preferences").GetRawText()),
                createdAt = userData.GetProperty("createdAt").GetString(),
                updatedAt = DateTimeOffset.UtcNow
            };
            
            await _redisDb.StringSetAsync($"user:{req.UserId}", JsonSerializer.Serialize(updatedUserData), TimeSpan.FromDays(365));
            
            return Ok(new { 
                message = "User promoted to Backoffice successfully",
                user = updatedUserData
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Promotion error: {ex.Message}");
            return StatusCode(500, new { error = "Failed to promote user" });
        }
    }

    /// <summary>
    /// Add roles to a user (Backoffice only)
    /// </summary>
    [HttpPut("users/{userId}/roles/add")]
    public async Task<IActionResult> AddUserRoles(string userId, [FromBody] AddRolesRequest req)
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Unauthorized(new { error = "Authorization required" });
        }
        
        var token = authHeader.Substring("Bearer ".Length);
        
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtKey);
            
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _jwtIssuer,
                ValidateAudience = true,
                ValidAudience = _jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };
            
            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            var requesterUserType = principal.FindFirst("userType")?.Value;
            
            // Only Backoffice users can manage roles
            if (requesterUserType != "Backoffice")
            {
                return Forbid();
            }
            
            // Get target user
            var userDataJson = await _redisDb.StringGetAsync($"user:{userId}");
            if (!userDataJson.HasValue)
            {
                return NotFound(new { error = "User not found" });
            }
            
            var userData = JsonSerializer.Deserialize<JsonElement>(userDataJson.ToString());
            var currentRoles = new List<string>();
            
            if (userData.TryGetProperty("roles", out var rolesProperty))
            {
                foreach (var role in rolesProperty.EnumerateArray())
                {
                    currentRoles.Add(role.GetString() ?? "Regular");
                }
            }
            else
            {
                var userType = userData.GetProperty("userType").GetString() ?? "Regular";
                currentRoles.Add(userType);
            }
            
            // Add new roles (avoid duplicates)
            foreach (var newRole in req.Roles)
            {
                if (!currentRoles.Contains(newRole))
                {
                    currentRoles.Add(newRole);
                }
            }
            
            // Update user data
            var updatedUserData = new
            {
                id = userId,
                phone = userData.GetProperty("phone").GetString(),
                email = userData.GetProperty("email").GetString(),
                userType = userData.GetProperty("userType").GetString(),
                roles = currentRoles,
                displayName = userData.GetProperty("displayName").GetString(),
                avatar = userData.GetProperty("avatar").GetString(),
                preferences = JsonSerializer.Deserialize<Dictionary<string, object>>(userData.GetProperty("preferences").GetRawText()),
                createdAt = userData.GetProperty("createdAt").GetString(),
                updatedAt = DateTimeOffset.UtcNow
            };
            
            await _redisDb.StringSetAsync($"user:{userId}", JsonSerializer.Serialize(updatedUserData), TimeSpan.FromDays(365));
            
            return Ok(new { 
                message = "Roles added successfully",
                user = updatedUserData
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Add roles error: {ex.Message}");
            return StatusCode(500, new { error = "Failed to add roles" });
        }
    }

    /// <summary>
    /// Remove roles from a user (Backoffice only)
    /// </summary>
    [HttpPut("users/{userId}/roles/remove")]
    public async Task<IActionResult> RemoveUserRoles(string userId, [FromBody] RemoveRolesRequest req)
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Unauthorized(new { error = "Authorization required" });
        }
        
        var token = authHeader.Substring("Bearer ".Length);
        
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtKey);
            
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _jwtIssuer,
                ValidateAudience = true,
                ValidAudience = _jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };
            
            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            var requesterUserType = principal.FindFirst("userType")?.Value;
            
            // Only Backoffice users can manage roles
            if (requesterUserType != "Backoffice")
            {
                return Forbid();
            }
            
            // Get target user
            var userDataJson = await _redisDb.StringGetAsync($"user:{userId}");
            if (!userDataJson.HasValue)
            {
                return NotFound(new { error = "User not found" });
            }
            
            var userData = JsonSerializer.Deserialize<JsonElement>(userDataJson.ToString());
            var currentRoles = new List<string>();
            
            if (userData.TryGetProperty("roles", out var rolesProperty))
            {
                foreach (var role in rolesProperty.EnumerateArray())
                {
                    currentRoles.Add(role.GetString() ?? "Regular");
                }
            }
            else
            {
                var userType = userData.GetProperty("userType").GetString() ?? "Regular";
                currentRoles.Add(userType);
            }
            
            // Remove specified roles
            foreach (var roleToRemove in req.Roles)
            {
                currentRoles.Remove(roleToRemove);
            }
            
            // Ensure user always has at least one role
            if (currentRoles.Count == 0)
            {
                currentRoles.Add("Regular");
            }
            
            // Update user data
            var updatedUserData = new
            {
                id = userId,
                phone = userData.GetProperty("phone").GetString(),
                email = userData.GetProperty("email").GetString(),
                userType = userData.GetProperty("userType").GetString(),
                roles = currentRoles,
                displayName = userData.GetProperty("displayName").GetString(),
                avatar = userData.GetProperty("avatar").GetString(),
                preferences = JsonSerializer.Deserialize<Dictionary<string, object>>(userData.GetProperty("preferences").GetRawText()),
                createdAt = userData.GetProperty("createdAt").GetString(),
                updatedAt = DateTimeOffset.UtcNow
            };
            
            await _redisDb.StringSetAsync($"user:{userId}", JsonSerializer.Serialize(updatedUserData), TimeSpan.FromDays(365));
            
            return Ok(new { 
                message = "Roles removed successfully",
                user = updatedUserData
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Remove roles error: {ex.Message}");
            return StatusCode(500, new { error = "Failed to remove roles" });
        }
    }

    private bool IsValidAdminInitCode(string initCode)
    {
        // Get valid init codes from configuration
        var validCodes = _configuration.GetSection("AdminSettings:ValidInitCodes")
            .Get<string[]>() ?? new[] { "INIT-ADMIN-2025", "MASTER-INIT-KEY" };
        
        return validCodes.Contains(initCode.ToUpper());
    }

    private async Task<List<JsonElement>> FindAdminUsers()
    {
        try
        {
            var server = _redisDb.Multiplexer.GetServer(_redisDb.Multiplexer.GetEndPoints().First());
            var keys = server.Keys(_redisDb.Database, "user:*");
            
            var adminUsers = new List<JsonElement>();
            
            foreach (var key in keys)
            {
                var userDataJson = await _redisDb.StringGetAsync(key);
                if (userDataJson.HasValue)
                {
                    var userData = JsonSerializer.Deserialize<JsonElement>(userDataJson.ToString());
                    var userType = userData.GetProperty("userType").GetString();
                    var roles = userData.TryGetProperty("roles", out var rolesProp) 
                        ? JsonSerializer.Deserialize<string[]>(rolesProp.GetRawText()) ?? new string[0]
                        : new string[0];
                    
                    if (userType == "Admin" || roles.Contains("Admin"))
                    {
                        adminUsers.Add(userData);
                    }
                }
            }
            
            return adminUsers;
        }
        catch (Exception)
        {
            return new List<JsonElement>();
        }
    }

    private bool IsValidBackofficeInviteCode(string inviteCode)
    {
        // Get valid codes from configuration for better management
        var validCodes = _configuration.GetSection("BackofficeSettings:ValidInviteCodes")
            .Get<string[]>() ?? new[] { "BACKOFFICE2024", "ADMIN123", "BELDERCHIN-ADMIN" };
        
        return validCodes.Contains(inviteCode.ToUpper());
    }

    /// <summary>
    /// Update user profile
    /// </summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateProfileRequest request)
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Unauthorized();
        }
        
        var token = authHeader.Substring("Bearer ".Length);
        
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtKey);
            
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _jwtIssuer,
                ValidateAudience = true,
                ValidAudience = _jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };
            
            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            var userId = principal.FindFirst("userId")?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            
            // Get existing user data
            var userDataJson = await _redisDb.StringGetAsync($"user:{userId}");
            if (!userDataJson.HasValue)
            {
                return NotFound(new { error = "User not found" });
            }
            
            var userData = JsonSerializer.Deserialize<JsonElement>(userDataJson.ToString());
            
            // Update user data
            var updatedUserData = new
            {
                id = userId,
                phone = userData.GetProperty("phone").GetString(),
                email = userData.GetProperty("email").GetString(),
                userType = userData.GetProperty("userType").GetString(),
                displayName = request.DisplayName ?? userData.GetProperty("displayName").GetString(),
                avatar = request.Avatar ?? userData.GetProperty("avatar").GetString(),
                preferences = request.Preferences ?? JsonSerializer.Deserialize<Dictionary<string, object>>(userData.GetProperty("preferences").GetRawText()),
                createdAt = userData.GetProperty("createdAt").GetString(),
                updatedAt = DateTimeOffset.UtcNow
            };
            
            await _redisDb.StringSetAsync($"user:{userId}", JsonSerializer.Serialize(updatedUserData), TimeSpan.FromDays(365));
            
            return Ok(new { message = "Profile updated successfully", user = updatedUserData });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Profile update error: {ex.Message}");
            return StatusCode(500, new { error = "Failed to update profile" });
        }
    }

    private static string GenerateNumericCode(int digits)
    {
        var max = (int)Math.Pow(10, digits);
        var n = RandomNumberGenerator.GetInt32(0, max);
        return n.ToString().PadLeft(digits, '0');
    }

    private static string Hash(string input)
    {
        using var sha = SHA256.Create();
        var hashBytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hashBytes);
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var aBytes = Convert.FromBase64String(a);
        var bBytes = Convert.FromBase64String(b);
        if (aBytes.Length != bBytes.Length) return false;
        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }

    private static async Task<(bool ok, string? error)> TrySendSms(string to, string message)
    {
        try
        {
            using var http = new HttpClient();
            var smsUrl = Environment.GetEnvironmentVariable("SMS_SERVICE_URL") ?? "http://sms-service:8080";
            var resp = await http.PostAsJsonAsync($"{smsUrl.TrimEnd('/')}/api/sms/send", new { To = to, Message = message });
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                return (false, $"SMS service returned {(int)resp.StatusCode}: {body}");
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static async Task<bool> TrySendEmail(string to, string message, string subject, string host, int port, string username, string password, string from)
    {
        try
        {
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = false,
                UseDefaultCredentials = true,
                Credentials = string.IsNullOrEmpty(username) ? null : new NetworkCredential(username, password)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(from),
                Subject = subject,
                Body = message,
                IsBodyHtml = false
            };
            mailMessage.To.Add(to);

            await client.SendMailAsync(mailMessage);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Email sending failed: {ex.Message}");
            return false;
        }
    }

    private async Task<(bool Success, string? IdentityId, string? Error)> CreateKratosIdentity(string? phone, string? email)
    {
        try
        {
            using var client = new HttpClient();
            
            var identityData = new
            {
                schema_id = _kratosSchemaId,
                traits = new Dictionary<string, object>()
            };
            
            if (!string.IsNullOrWhiteSpace(phone))
                identityData.traits["phone"] = phone;
            else if (!string.IsNullOrWhiteSpace(email))
                identityData.traits["phone"] = "+989123456789"; // Use valid Iranian phone pattern for email-only users
                
            if (!string.IsNullOrWhiteSpace(email))
                identityData.traits["email"] = email;

            var content = new StringContent(
                JsonSerializer.Serialize(identityData),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync($"{_kratosAdminUrl}/admin/identities", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var identity = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var identityId = identity.GetProperty("id").GetString();
                
                Console.WriteLine($"✅ Created Kratos identity: {identityId}");
                return (true, identityId, null);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                if (error.Contains("Conflict") || error.Contains("already exists"))
                {
                    var existingId = await GetIdentityId(phone, email);
                    if (!string.IsNullOrEmpty(existingId))
                    {
                        return (true, existingId, null);
                    }
                }
                Console.WriteLine($"❌ Failed to create Kratos identity: {response.StatusCode} - {error}");
                return (false, null, $"Failed to create identity: {error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception creating Kratos identity: {ex.Message}");
            return (false, null, $"Exception: {ex.Message}");
        }
    }

    private async Task<string?> GetIdentityId(string? phone, string? email)
    {
        try
        {
            using var client = new HttpClient();
            
            var searchUrl = $"{_kratosAdminUrl}/admin/identities?per_page=100";
            
            var response = await client.GetAsync(searchUrl);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var identitiesData = JsonSerializer.Deserialize<JsonElement>(content);
                
                JsonElement identitiesArray;
                if (identitiesData.ValueKind == JsonValueKind.Array)
                {
                    identitiesArray = identitiesData;
                }
                else if (identitiesData.TryGetProperty("identities", out var identitiesProp))
                {
                    identitiesArray = identitiesProp;
                }
                else
                {
                    Console.WriteLine($"❌ Unexpected response format: {content}");
                    return null;
                }
                
                foreach (var identity in identitiesArray.EnumerateArray())
                {
                    var traits = identity.GetProperty("traits");
                    
                    if (!string.IsNullOrWhiteSpace(phone) && traits.TryGetProperty("phone", out var phoneProp))
                    {
                        if (phoneProp.GetString() == phone)
                            return identity.GetProperty("id").GetString();
                    }
                    
                    if (!string.IsNullOrWhiteSpace(email) && traits.TryGetProperty("email", out var emailProp))
                    {
                        if (emailProp.GetString() == email)
                            return identity.GetProperty("id").GetString();
                    }
                }
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error finding identity: {ex.Message}");
            return null;
        }
    }

    private async Task<(bool Success, string? SessionToken, string? Error)> CreateAuthBridgeSession(string? phone, string? email)
    {
        try
        {
            var (identityCreated, identityId, identityError) = await CreateKratosIdentity(phone, email);
            if (!identityCreated && identityError != null && !identityError.Contains("already exists") && !identityError.Contains("Conflict"))
            {
                return (false, null, identityError);
            }
            
            if (string.IsNullOrEmpty(identityId))
            {
                identityId = await GetIdentityId(phone, email);
            }
            
            if (string.IsNullOrEmpty(identityId))
            {
                return (false, null, "Could not find or create identity");
            }
            
            var sessionId = Guid.NewGuid().ToString("N");
            var sessionToken = $"auth_bridge_session_{sessionId}";
            
            var sessionData = new
            {
                session_id = sessionId,
                identity_id = identityId,
                phone = phone,
                email = email,
                created_at = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                expires_at = DateTimeOffset.UtcNow.AddHours(24).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                authenticated_at = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                authentication_method = "code",
                aal = "aal1",
                active = true
            };
            
            var json = JsonSerializer.Serialize(sessionData);
            var expiry = TimeSpan.FromHours(24);
            await _redisDb.StringSetAsync($"session:{sessionId}", json, expiry);
            
            Console.WriteLine($"✅ Created Auth-Bridge session for identity: {identityId}");
            Console.WriteLine($"🔐 Session Token: {sessionToken}");
            
            return (true, sessionToken, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception creating Auth-Bridge session: {ex.Message}");
            return (false, null, $"Exception: {ex.Message}");
        }
    }
}

public record StartVerificationRequest(string? Phone, string? Email);
public record Start2FaRequest(string? Phone, string? Email);
public record ConfirmCodeRequest(string Id, string Code);
public record UpdateProfileRequest(string? DisplayName, string? Avatar, Dictionary<string, object>? Preferences);
public record InitializeAdminRequest(string? Phone, string? Email, string InitCode);
public record RegisterBackofficeRequest(string? Phone, string? Email, string InviteCode);
public record PromoteUserRequest(string UserId);
public record AddRolesRequest(string[] Roles);
public record RemoveRolesRequest(string[] Roles);

enum CodePurpose
{
    Verification = 1,
    TwoFactor = 2
}

record CodeRecord(
    string Id,
    CodePurpose Purpose,
    string? Phone,
    string? Email,
    string Code,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    int Attempts,
    int MaxAttempts,
    string UserType = "Regular"
);
