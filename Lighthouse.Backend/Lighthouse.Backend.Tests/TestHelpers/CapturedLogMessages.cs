using Serilog.Core;
using Serilog.Events;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// ADR-137 D72: <c>Program.cs</c> calls <c>UseSerilog(logger, true)</c> — the positional argument
    /// is <c>dispose</c>, and <c>writeToProviders</c> defaults to <c>false</c>, so every
    /// <see cref="Microsoft.Extensions.Logging.ILoggerProvider"/> added through <c>AddProvider</c> is
    /// dropped. Serilog is the pipeline, so a test that wants log messages has to attach a sink.
    /// </summary>
    public sealed class CapturedLogMessages : ILogEventSink
    {
        private readonly List<string> messages = [];
        private readonly Lock gate = new();

        public int Count
        {
            get
            {
                lock (gate)
                {
                    return messages.Count;
                }
            }
        }

        public void Emit(LogEvent logEvent)
        {
            ArgumentNullException.ThrowIfNull(logEvent);

            var message = logEvent.RenderMessage();
            if (logEvent.Exception is not null)
            {
                message = $"{message} :: {logEvent.Exception.GetType().FullName}: {logEvent.Exception.Message}";
            }

            lock (gate)
            {
                messages.Add(message);
            }
        }

        public bool ContainsMessageFragment(string fragment)
        {
            lock (gate)
            {
                return messages.Exists(message => message.Contains(fragment, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Asserts the fragment was never logged — and, first, that anything was logged at all.
        /// The positive control is the point: a capture that silently stops working turns every
        /// "this was not logged" assertion into a tautology, which is how the inert
        /// <c>ILoggerProvider</c> this class replaces went unnoticed.
        /// </summary>
        public void AssertNothingLoggedMatching(string fragment)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(Count, Is.GreaterThan(0),
                    "positive control: the capture saw no log message at all, so the assertion below cannot fail");
                Assert.That(ContainsMessageFragment(fragment), Is.False,
                    $"'{fragment}' was logged");
            }
        }
    }
}
