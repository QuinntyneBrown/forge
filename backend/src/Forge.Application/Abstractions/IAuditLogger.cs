namespace Forge.Application.Abstractions;

public interface IAuditLogger
{
    Task WriteAsync(string @event, Guid? userId, object? payload, CancellationToken cancellationToken);
}
