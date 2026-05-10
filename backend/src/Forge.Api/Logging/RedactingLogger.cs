using Microsoft.Extensions.Logging;

namespace Forge.Api.Logging;

internal class RedactingLogger : ILogger
{
    private const string Mask = "***";

    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "accessToken",
        "refreshToken",
        "passwordResetToken",
        "Authorization",
        "Token"
    };

    private readonly ILogger _inner;

    public RedactingLogger(ILogger inner) => _inner = inner;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _inner.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (state is IReadOnlyList<KeyValuePair<string, object?>> kvs)
        {
            var redactedPairs = new List<KeyValuePair<string, object?>>(kvs.Count);
            var replacements = new List<string>();
            foreach (var kv in kvs)
            {
                if (SensitiveKeys.Contains(kv.Key))
                {
                    if (kv.Value is not null)
                    {
                        var raw = kv.Value.ToString();
                        if (!string.IsNullOrEmpty(raw))
                        {
                            replacements.Add(raw);
                        }
                    }
                    redactedPairs.Add(new KeyValuePair<string, object?>(kv.Key, Mask));
                }
                else
                {
                    redactedPairs.Add(kv);
                }
            }

            var originalFormatted = formatter(state, exception);
            var redactedFormatted = originalFormatted;
            foreach (var raw in replacements)
            {
                redactedFormatted = redactedFormatted.Replace(raw, Mask);
            }

            var newState = new RedactedState(redactedPairs, redactedFormatted);
            _inner.Log(logLevel, eventId, newState, exception, RedactedState.Formatter);
            return;
        }

        _inner.Log(logLevel, eventId, state, exception, formatter);
    }

    private class RedactedState : IReadOnlyList<KeyValuePair<string, object?>>
    {
        public static readonly Func<RedactedState, Exception?, string> Formatter =
            (state, _) => state._formatted;

        private readonly IReadOnlyList<KeyValuePair<string, object?>> _pairs;
        private readonly string _formatted;

        public RedactedState(IReadOnlyList<KeyValuePair<string, object?>> pairs, string formatted)
        {
            _pairs = pairs;
            _formatted = formatted;
        }

        public KeyValuePair<string, object?> this[int index] => _pairs[index];
        public int Count => _pairs.Count;
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _pairs.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString() => _formatted;
    }
}
