using Microsoft.Extensions.Logging;

namespace Forge.Api.Logging;

/// <summary>
/// Wraps an inner ILoggerFactory so every emitted log entry has known-
/// sensitive structured parameters scrubbed (password, accessToken,
/// refreshToken, passwordResetToken, Authorization, Token). Per L2-051: no
/// secrets ever land in log output, even if a careless caller passes one
/// through a structured template.
/// </summary>
public class RedactingLoggerFactory : ILoggerFactory
{
    private readonly ILoggerFactory _inner;

    public RedactingLoggerFactory(ILoggerFactory inner)
    {
        _inner = inner;
    }

    public void AddProvider(ILoggerProvider provider) => _inner.AddProvider(provider);

    public ILogger CreateLogger(string categoryName) => new RedactingLogger(_inner.CreateLogger(categoryName));

    public void Dispose() => _inner.Dispose();
}
