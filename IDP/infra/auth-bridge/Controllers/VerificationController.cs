using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace AuthBridge.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VerificationController : ControllerBase
{
    private readonly IDatabase _redisDb;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _smtpFrom;
    private readonly string _kratosAdminUrl;
    private readonly string _kratosPublicUrl;
    private readonly string _kratosSchemaId;
    private readonly ConcurrentDictionary<string, CodeRecord> _store;

    public VerificationController(
        IDatabase redisDb,
        IConfiguration configuration)
    {
        _redisDb = redisDb;
        
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
        
        _store = new ConcurrentDictionary<string, CodeRecord>();
    }

    /// <summary>
    /// Start phone/email verification
    /// </summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartVerification([FromBody] StartVerificationRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Phone) && string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { error = "phone or email is required" });

        var target = !string.IsNullOrWhiteSpace(req.Phone) ? req.Phone : req.Email;
        var delivery = !string.IsNullOrWhiteSpace(req.Phone) ? "sms" : "email";

        // Generate 6-digit code
        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var id = Guid.NewGuid().ToString();

        // Store in Redis (24h expiry)
        await _redisDb.StringSetAsync($"code:{id}", JsonSerializer.Serialize(new CodeRecord
        {
            Target = target,
            Code = code,
            Purpose = CodePurpose.Verification,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        }), TimeSpan.FromHours(24));

        // Send via SMS (using external service)
        if (delivery == "sms")
        {
            // TODO: Integrate with SMS service
            Console.WriteLine($"📱 SMS to {target}: Your verification code is {code}");
        }
        else
        {
            // Send via email
            try
            {
                using var client = new SmtpClient(_smtpHost, _smtpPort)
                {
                    EnableSsl = false,
                    Credentials = new NetworkCredential(_smtpUsername, _smtpPassword)
                };

                var mail = new MailMessage(_smtpFrom, target, "Verify your account",
                    $"Your verification code is: {code}");
                await client.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to send email: {ex.Message}");
                return StatusCode(500, new { error = "Failed to send verification code" });
            }
        }

        return Ok(new { id, delivery });
    }

    /// <summary>
    /// Confirm verification code
    /// </summary>
    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmVerification([FromBody] ConfirmCodeRequest req)
    {
        if (!_store.TryGetValue(req.Id, out var record) || record.Purpose != CodePurpose.Verification)
            return NotFound(new { error = "verification request not found" });

        if (record.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new { error = "code expired" });

        if (record.Code != req.Code)
            return BadRequest(new { error = "invalid code" });

        // Mark as verified
        record.VerifiedAt = DateTime.UtcNow;
        await _redisDb.StringSetAsync($"code:{req.Id}", JsonSerializer.Serialize(record), TimeSpan.FromHours(24));

        return Ok(new { message = "verified" });
    }

    /// <summary>
    /// Start 2FA authentication
    /// </summary>
    [HttpPost("2fa/start")]
    public async Task<IActionResult> StartTwoFactor([FromBody] Start2FaRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Phone) && string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { error = "phone or email is required" });

        var target = !string.IsNullOrWhiteSpace(req.Phone) ? req.Phone : req.Email;
        var delivery = !string.IsNullOrWhiteSpace(req.Phone) ? "sms" : "email";

        // Generate 6-digit code
        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var id = Guid.NewGuid().ToString();

        // Store in Redis (5min expiry for 2FA)
        await _redisDb.StringSetAsync($"code:{id}", JsonSerializer.Serialize(new CodeRecord
        {
            Target = target,
            Code = code,
            Purpose = CodePurpose.TwoFactor,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        }), TimeSpan.FromMinutes(5));

        // Send via SMS or Email (reuse logic from above)
        if (delivery == "sms")
        {
            Console.WriteLine($"📱 2FA SMS to {target}: Your 2FA code is {code}");
        }
        else
        {
            try
            {
                using var client = new SmtpClient(_smtpHost, _smtpPort)
                {
                    EnableSsl = false,
                    Credentials = new NetworkCredential(_smtpUsername, _smtpPassword)
                };

                var mail = new MailMessage(_smtpFrom, target, "Your 2FA code",
                    $"Your 2FA code is: {code}");
                await client.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to send 2FA email: {ex.Message}");
                return StatusCode(500, new { error = "Failed to send 2FA code" });
            }
        }

        return Ok(new { id, delivery });
    }

    /// <summary>
    /// Confirm 2FA code
    /// </summary>
    [HttpPost("2fa/confirm")]
    public async Task<IActionResult> ConfirmTwoFactor([FromBody] ConfirmCodeRequest req)
    {
        if (!_store.TryGetValue(req.Id, out var record) || record.Purpose != CodePurpose.TwoFactor)
            return NotFound(new { error = "2fa request not found" });

        if (record.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new { error = "code expired" });

        if (record.Code != req.Code)
            return BadRequest(new { error = "invalid code" });

        // Mark as verified
        record.VerifiedAt = DateTime.UtcNow;
        await _redisDb.StringSetAsync($"code:{req.Id}", JsonSerializer.Serialize(record), TimeSpan.FromMinutes(5));

        return Ok(new { message = "2fa verified" });
    }
}

// Supporting types
public class CodeRecord
{
    public string Target { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public CodePurpose Purpose { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
}

public enum CodePurpose
{
    Verification,
    TwoFactor
}

public record StartVerificationRequest(string? Phone, string? Email);
public record ConfirmCodeRequest(string Id, string Code);
public record Start2FaRequest(string? Phone, string? Email);
