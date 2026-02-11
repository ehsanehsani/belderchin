using System.Collections.Concurrent;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// App-driven verification + 2FA code issuing.
// This is a minimal implementation using in-memory storage.
// For production: replace with persistent store (Postgres/Redis), rate limits, audit logs.

var store = new ConcurrentDictionary<string, CodeRecord>();

app.MapPost("/verification/start", async (StartVerificationRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.Phone) && string.IsNullOrWhiteSpace(req.Email))
        return Results.BadRequest(new { error = "phone or email is required" });

    var code = GenerateNumericCode(6);
    var id = Guid.NewGuid().ToString("N");

    store[id] = new CodeRecord(
        Id: id,
        Purpose: CodePurpose.Verification,
        Phone: req.Phone,
        Email: req.Email,
        CodeHash: Hash(code),
        ExpiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(10),
        AttemptsLeft: 5);

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
            return Results.Problem(smsError ?? "SMS delivery failed and no email provided", statusCode: 502);
        }

        return Results.Ok(new
        {
            id,
            delivery = "email",
            message = "SMS failed; use email fallback. Trigger Kratos verification flow for email delivery.",
            email = req.Email
        });
    }

    return Results.Ok(new { id, delivery = "sms" });
});

app.MapPost("/verification/confirm", (ConfirmCodeRequest req) =>
{
    if (!store.TryGetValue(req.Id, out var record) || record.Purpose != CodePurpose.Verification)
        return Results.NotFound(new { error = "verification request not found" });

    var now = DateTimeOffset.UtcNow;
    if (record.ExpiresAtUtc <= now)
        return Results.Problem("code expired", statusCode: 410);

    if (record.AttemptsLeft <= 0)
        return Results.Problem("too many attempts", statusCode: 429);

    if (!FixedTimeEquals(record.CodeHash, Hash(req.Code)))
    {
        store[req.Id] = record with { AttemptsLeft = record.AttemptsLeft - 1 };
        return Results.Unauthorized();
    }

    store.TryRemove(req.Id, out _);
    return Results.Ok(new { ok = true });
});

app.MapPost("/2fa/start", async (Start2FaRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.Phone) && string.IsNullOrWhiteSpace(req.Email))
        return Results.BadRequest(new { error = "phone or email is required" });

    var code = GenerateNumericCode(6);
    var id = Guid.NewGuid().ToString("N");

    store[id] = new CodeRecord(
        Id: id,
        Purpose: CodePurpose.TwoFactor,
        Phone: req.Phone,
        Email: req.Email,
        CodeHash: Hash(code),
        ExpiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(5),
        AttemptsLeft: 5);

    var smsOk = false;
    string? smsError = null;

    if (!string.IsNullOrWhiteSpace(req.Phone))
    {
        (smsOk, smsError) = await TrySendSms(req.Phone!, $"Your 2FA code is: {code}");
    }

    if (!smsOk)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            return Results.Problem(smsError ?? "SMS delivery failed and no email provided", statusCode: 502);

        return Results.Ok(new
        {
            id,
            delivery = "email",
            message = "SMS failed; use email fallback.",
            email = req.Email
        });
    }

    return Results.Ok(new { id, delivery = "sms" });
});

app.MapPost("/2fa/confirm", (ConfirmCodeRequest req) =>
{
    if (!store.TryGetValue(req.Id, out var record) || record.Purpose != CodePurpose.TwoFactor)
        return Results.NotFound(new { error = "2fa request not found" });

    var now = DateTimeOffset.UtcNow;
    if (record.ExpiresAtUtc <= now)
        return Results.Problem("code expired", statusCode: 410);

    if (record.AttemptsLeft <= 0)
        return Results.Problem("too many attempts", statusCode: 429);

    if (!FixedTimeEquals(record.CodeHash, Hash(req.Code)))
    {
        store[req.Id] = record with { AttemptsLeft = record.AttemptsLeft - 1 };
        return Results.Unauthorized();
    }

    store.TryRemove(req.Id, out _);
    return Results.Ok(new { ok = true });
});

app.Run();

static string GenerateNumericCode(int digits)
{
    var max = (int)Math.Pow(10, digits);
    var n = RandomNumberGenerator.GetInt32(0, max);
    return n.ToString().PadLeft(digits, '0');
}

static byte[] Hash(string input)
{
    using var sha = SHA256.Create();
    return sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
}

static bool FixedTimeEquals(byte[] a, byte[] b)
{
    if (a.Length != b.Length) return false;
    return CryptographicOperations.FixedTimeEquals(a, b);
}

static async Task<(bool ok, string? error)> TrySendSms(string to, string message)
{
    try
    {
        using var http = new HttpClient();
        var smsUrl = Environment.GetEnvironmentVariable("SMS_SERVICE_URL") ?? "http://sms-service:8080";
        var resp = await http.PostAsJsonAsync($"{smsUrl.TrimEnd('/')}/sms/send", new { To = to, Message = message });
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

public record StartVerificationRequest(string? Phone, string? Email);
public record Start2FaRequest(string? Phone, string? Email);
public record ConfirmCodeRequest(string Id, string Code);

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
    byte[] CodeHash,
    DateTimeOffset ExpiresAtUtc,
    int AttemptsLeft);
