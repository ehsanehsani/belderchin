using Kavenegar;
using Kavenegar.Models;
using Microsoft.AspNetCore.Mvc;

namespace SMS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SMSController : ControllerBase
{
    private readonly ILogger<SMSController> _logger;

    public SMSController(ILogger<SMSController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Send SMS message
    /// </summary>
    [HttpPost("send")]
    public async Task<IActionResult> SendSMS([FromBody] SmsSendRequest req)
    {
        _logger.LogInformation("SMS send request to {To}", req.To);

        if (string.IsNullOrWhiteSpace(req.To))
            return BadRequest(new { error = "'to' is required" });

        if (string.IsNullOrWhiteSpace(req.Message))
            return BadRequest(new { error = "'message' is required" });

        var apiKey = Environment.GetEnvironmentVariable("KAVENEGAR_API_KEY");
        var sender = Environment.GetEnvironmentVariable("KAVENEGAR_SENDER");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("KAVENEGAR_API_KEY is not configured");
            return Problem("KAVENEGAR_API_KEY is not configured", statusCode: 500);
        }

        try
        {
            var client = new KavenegarApi(apiKey);
            var result = client.Send(sender ?? string.Empty, req.To, req.Message);

            _logger.LogInformation("SMS sent successfully. MessageId: {MessageId}, Status: {Status}", 
                result.Messageid, result.Status);

            return Ok(new
            {
                ok = true,
                messageId = result.Messageid,
                status = result.Status,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Kavenegar.Exceptions.ApiException ex)
        {
            _logger.LogError(ex, "Kavenegar API error");
            return Problem($"Kavenegar API error: {ex.Message}", statusCode: 502);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending SMS");
            return Problem(ex.Message, statusCode: 502);
        }
    }

    /// <summary>
    /// Get SMS status
    /// </summary>
    [HttpGet("status/{messageId}")]
    public async Task<IActionResult> GetSMSStatus(string messageId)
    {
        _logger.LogInformation("SMS status request for MessageId: {MessageId}", messageId);

        var apiKey = Environment.GetEnvironmentVariable("KAVENEGAR_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Problem("KAVENEGAR_API_KEY is not configured", statusCode: 500);
        }

        try
        {
            var client = new KavenegarApi(apiKey);
            var result = client.Status(messageId);

            return Ok(new
            {
                messageId = messageId,
                status = result.Status,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Kavenegar.Exceptions.ApiException ex)
        {
            _logger.LogError(ex, "Kavenegar API error getting status");
            return Problem($"Kavenegar API error: {ex.Message}", statusCode: 502);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting SMS status");
            return Problem(ex.Message, statusCode: 502);
        }
    }
}

public record SmsSendRequest(string To, string Message);
