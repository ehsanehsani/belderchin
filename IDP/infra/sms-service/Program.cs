using Kavenegar;
using Kavenegar.Models;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/sms/send", (SmsSendRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.To))
        return Results.BadRequest(new { error = "'to' is required" });

    if (string.IsNullOrWhiteSpace(req.Message))
        return Results.BadRequest(new { error = "'message' is required" });

    var apiKey = Environment.GetEnvironmentVariable("KAVENEGAR_API_KEY");
    var sender = Environment.GetEnvironmentVariable("KAVENEGAR_SENDER");

    if (string.IsNullOrWhiteSpace(apiKey))
        return Results.Problem("KAVENEGAR_API_KEY is not configured", statusCode: 500);

    try
    {
        var client = new KavenegarApi(apiKey);

        var result = client.Send(sender ?? string.Empty, req.To, req.Message);


        return Results.Ok(new
        {
            ok = true,
            messageId = result.Messageid,
            status = result.Status
        });
    }
    catch (Kavenegar.Exceptions.ApiException ex)
    {
        return Results.Problem($"Kavenegar API error: {ex.Message}", statusCode: 502);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 502);
    }
});

app.Run();

public record SmsSendRequest(string To, string Message);
