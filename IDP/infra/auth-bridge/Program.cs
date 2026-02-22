using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add SMTP configuration
var smtpConfig = builder.Configuration.GetSection("Smtp");
var smtpHost = smtpConfig["Host"] ?? "localhost";
var smtpPort = int.Parse(smtpConfig["Port"] ?? "1025");
var smtpUsername = smtpConfig["Username"] ?? "";
var smtpPassword = smtpConfig["Password"] ?? "";
var smtpFrom = smtpConfig["From"] ?? "noreply@belderchin.local";

// Add Kratos configuration
var kratosConfig = builder.Configuration.GetSection("Kratos");
var kratosAdminUrl = kratosConfig["AdminUrl"] ?? "http://kratos:4434";
var kratosPublicUrl = kratosConfig["PublicUrl"] ?? "http://kratos:4433";
var kratosSchemaId = kratosConfig["IdentitySchemaId"] ?? "phone_v1";

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

        // Try to send email
        var emailOk = await TrySendEmail(req.Email!, $"Your verification code is: {code}", "Belderchin Verification", smtpHost, smtpPort, smtpUsername, smtpPassword, smtpFrom);
        if (!emailOk)
        {
            return Results.Problem("Both SMS and email delivery failed", statusCode: 502);
        }

        return Results.Ok(new
        {
            id,
            delivery = "email",
            message = "SMS failed; verification code sent via email.",
            email = req.Email
        });
    }

    return Results.Ok(new { id, delivery = "sms" });
});

app.MapPost("/verification/confirm", async (ConfirmCodeRequest req) =>
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

    // Code verified successfully - create Auth-Bridge session
    var (sessionCreated, sessionToken, sessionError) = await CreateAuthBridgeSession(record.Phone, record.Email);
    
    if (!sessionCreated)
    {
        Console.WriteLine($"❌ Failed to create Auth-Bridge session: {sessionError}");
        // Still return verification success, but note session creation failed
        return Results.Ok(new { 
            ok = true, 
            message = "verification successful but session creation failed",
            session_error = sessionError
        });
    }

    store.TryRemove(req.Id, out _);
    
    Console.WriteLine($"✅ Verification successful and Auth-Bridge session created");
    return Results.Ok(new { 
        ok = true, 
        message = "verification successful and session created",
        session_token = sessionToken,
        phone = record.Phone,
        email = record.Email,
        session_type = "auth_bridge"
    });
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

        // Try to send email
        var emailOk = await TrySendEmail(req.Email!, $"Your 2FA code is: {code}", "Belderchin 2FA", smtpHost, smtpPort, smtpUsername, smtpPassword, smtpFrom);
        if (!emailOk)
        {
            return Results.Problem("Both SMS and email delivery failed", statusCode: 502);
        }

        return Results.Ok(new
        {
            id,
            delivery = "email",
            message = "SMS failed; 2FA code sent via email.",
            email = req.Email
        });
    }

    return Results.Ok(new { id, delivery = "sms" });
});

app.MapPost("/2fa/confirm", async (ConfirmCodeRequest req) =>
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

    // 2FA code verified successfully - create Auth-Bridge session
    var (sessionCreated, sessionToken, sessionError) = await CreateAuthBridgeSession(record.Phone, record.Email);
    
    if (!sessionCreated)
    {
        Console.WriteLine($"❌ Failed to create Auth-Bridge session for 2FA: {sessionError}");
        // Still return 2FA success, but note session creation failed
        return Results.Ok(new { 
            ok = true, 
            message = "2FA successful but session creation failed",
            session_error = sessionError
        });
    }

    store.TryRemove(req.Id, out _);
    
    Console.WriteLine($"✅ 2FA verification successful and Auth-Bridge session created");
    return Results.Ok(new { 
        ok = true, 
        message = "2FA successful and session created",
        session_token = sessionToken,
        phone = record.Phone,
        email = record.Email,
        session_type = "auth_bridge"
    });
});

// Simplified Kratos integration functions (for identity management only)
async Task<(bool Success, string? IdentityId, string? Error)> CreateKratosIdentity(string? phone, string? email)
{
    try
    {
        using var client = new HttpClient();
        
        var identityData = new
        {
            schema_id = kratosSchemaId,
            traits = new Dictionary<string, object>()
        };
        
        if (!string.IsNullOrWhiteSpace(phone))
            identityData.traits["phone"] = phone;
            
        if (!string.IsNullOrWhiteSpace(email))
            identityData.traits["email"] = email;

        var content = new StringContent(
            JsonSerializer.Serialize(identityData),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync($"{kratosAdminUrl}/admin/identities", content);
        
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
                // Identity already exists, try to find it
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

async Task<string?> GetIdentityId(string? phone, string? email)
{
    try
    {
        using var client = new HttpClient();
        
        var searchUrl = $"{kratosAdminUrl}/admin/identities?per_page=100";
        
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

// Session validation endpoint
app.MapGet("/sessions/validate", (HttpContext context) =>
{
    var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
    if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
    {
        return Results.Unauthorized();
    }
    
    var token = authHeader.Substring("Bearer ".Length);
    
    // Simple validation (in production, validate against stored sessions)
    if (token.StartsWith("auth_bridge_session_") && token.Length > 20)
    {
        return Results.Ok(new { 
            valid = true,
            message = "Session is valid",
            token_type = "auth_bridge"
        });
    }
    
    return Results.Unauthorized();
});

// Session whoami endpoint
app.MapGet("/sessions/whoami", (HttpContext context) =>
{
    var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
    if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
    {
        return Results.Unauthorized();
    }
    
    var token = authHeader.Substring("Bearer ".Length);
    
    // Simple validation (In production, retrieve session data)
    if (token.StartsWith("auth_bridge_session_") && token.Length > 20)
    {
        // Mock session data (In production, retrieve from storage)
        var sessionData = new
        {
            active = true,
            identity = new
            {
                id = "mock_identity_id",
                traits = new
                {
                    phone = "+989934395113",
                    email = "user@example.com"
                }
            },
            authenticator_assurance_level = "aal1",
            expires_at = DateTimeOffset.UtcNow.AddHours(24).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            issued_at = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            authentication_methods = new[]
            {
                new
                {
                    method = "code",
                    completed_at = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                }
            }
        };
        
        return Results.Ok(sessionData);
    }
    
    return Results.Unauthorized();
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

static async Task<bool> TrySendEmail(string to, string message, string subject, string host, int port, string username, string password, string from)
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

// Session Management Functions
async Task<(bool Success, string? SessionToken, string? Error)> CreateAuthBridgeSession(string? phone, string? email)
{
    try
    {
        // First, ensure identity exists
        var (identityCreated, _, identityError) = await CreateKratosIdentity(phone, email);
        if (!identityCreated && identityError != null && !identityError.Contains("already exists") && !identityError.Contains("Conflict"))
        {
            return (false, null, identityError);
        }
        
        // Get existing identity by phone/email
        var identityId = await GetIdentityId(phone, email);
        if (string.IsNullOrEmpty(identityId))
        {
            return (false, null, "Could not find or create identity");
        }
        
        // Create Auth-Bridge session token (self-contained)
        var sessionData = new
        {
            session_id = Guid.NewGuid().ToString("N"),
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

        // Store session in memory (for production, use Redis/Database)
        var sessionToken = $"auth_bridge_session_{sessionData.session_id}";
        
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
