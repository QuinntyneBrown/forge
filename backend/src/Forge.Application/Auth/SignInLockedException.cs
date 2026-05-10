namespace Forge.Application.Auth;

public class SignInLockedException : Exception
{
    public SignInLockedException(TimeSpan retryAfter)
        : base($"Sign-in is temporarily locked. Try again in {retryAfter.TotalMinutes:F0} minutes.")
    {
        RetryAfter = retryAfter;
    }

    public TimeSpan RetryAfter { get; }
}
