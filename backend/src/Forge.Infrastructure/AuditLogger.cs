using System.Text.Json;
using System.Text.Json.Nodes;
using Forge.Application.Abstractions;
using Forge.Domain;
using Microsoft.AspNetCore.Http;

namespace Forge.Infrastructure;

public class AuditLogger : IAuditLogger
{
    private static readonly HashSet<string> RedactedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "newPassword",
        "currentPassword",
        "accessToken",
        "refreshToken",
        "passwordResetToken",
        "token",
        "authorization"
    };

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly IHttpContextAccessor _http;

    public AuditLogger(IAppDbContext db, IClock clock, IHttpContextAccessor http)
    {
        _db = db;
        _clock = clock;
        _http = http;
    }

    public async Task WriteAsync(
        string @event,
        Guid? userId,
        object? payload,
        CancellationToken cancellationToken)
    {
        var ctx = _http.HttpContext;
        var ip = ctx?.Connection.RemoteIpAddress?.ToString();
        var userAgent = ctx?.Request.Headers.UserAgent.ToString();

        var entry = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Event = @event,
            IpAddress = string.IsNullOrEmpty(ip) ? null : ip,
            UserAgent = string.IsNullOrEmpty(userAgent) ? null : userAgent,
            OccurredAt = _clock.UtcNow,
            PayloadJson = Redact(payload)
        };

        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string? Redact(object? payload)
    {
        if (payload is null)
        {
            return null;
        }

        var node = JsonSerializer.SerializeToNode(payload, SerializerOptions);
        if (node is null)
        {
            return null;
        }

        RedactRecursive(node);
        return node.ToJsonString(SerializerOptions);
    }

    private static void RedactRecursive(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(kvp => kvp.Key).ToList())
                {
                    if (RedactedKeys.Contains(key))
                    {
                        obj[key] = "[REDACTED]";
                    }
                    else if (obj[key] is JsonNode child)
                    {
                        RedactRecursive(child);
                    }
                }
                break;
            case JsonArray arr:
                foreach (var item in arr)
                {
                    if (item is not null)
                    {
                        RedactRecursive(item);
                    }
                }
                break;
        }
    }
}
