using Microsoft.Extensions.Logging;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// An ILogger that keeps what was written to it, for the handful of behaviours whose whole point is
    /// that an operator gets told something. Verifying a mocked ILogger works, but it asserts on the
    /// shape of a call to a logging API rather than on the sentence anyone reads.
    /// </summary>
    public sealed class RecordingLogger<T> : ILogger<T>
    {
        private readonly List<(LogLevel Level, string Message)> entries = [];

        public IReadOnlyList<string> Warnings => Written(LogLevel.Warning);

        public IReadOnlyList<string> Written(LogLevel level)
            => entries.FindAll(entry => entry.Level == level).ConvertAll(entry => entry.Message);

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NothingIsHeld.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);

            entries.Add((logLevel, formatter(state, exception)));
        }
    }

    internal sealed class NothingIsHeld : IDisposable
    {
        public static readonly NothingIsHeld Instance = new();

        public void Dispose()
        {
            // A scope this logger opens holds nothing, so there is nothing to release.
        }
    }
}
