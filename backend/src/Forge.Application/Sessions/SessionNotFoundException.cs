namespace Forge.Application.Sessions;

public class SessionNotFoundException : Exception
{
    public SessionNotFoundException(Guid sessionId)
        : base($"Session {sessionId} not found.") { }
}
