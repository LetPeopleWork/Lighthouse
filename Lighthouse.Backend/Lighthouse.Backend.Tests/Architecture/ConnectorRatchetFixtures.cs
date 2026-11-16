using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;

// Made-up connectors for the connector ratchet. Each one implements the one-member connector-port interface the
// way a real connector would, and each sits in a namespace of its own, so the ratchet can be pointed at one case
// at a time. None of them is ever called: the ratchet reads their compiled code, it does not run it.

namespace Lighthouse.Backend.Tests.Architecture.ConnectorRatchetFixtures.DirectThrow
{
    public sealed class AStrategyThatRefusesDirectly : IWorkTrackingAuthStrategy
    {
        public Task ApplyAsync(HttpRequestMessage request, WorkTrackingSystemConnection connection, CancellationToken cancellationToken)
            => throw new NotSupportedException("This made-up strategy cannot sign a request.");
    }
}

namespace Lighthouse.Backend.Tests.Architecture.ConnectorRatchetFixtures.PrivateHelper
{
    public sealed class AStrategyThatRefusesInAHelper : IWorkTrackingAuthStrategy
    {
        public Task ApplyAsync(HttpRequestMessage request, WorkTrackingSystemConnection connection, CancellationToken cancellationToken)
            => Refuse();

        private static Task Refuse()
            => throw new NotSupportedException("This made-up strategy cannot sign a request.");
    }
}

namespace Lighthouse.Backend.Tests.Architecture.ConnectorRatchetFixtures.AsyncBody
{
    public sealed class AStrategyThatRefusesAfterAnAwait : IWorkTrackingAuthStrategy
    {
        public async Task ApplyAsync(HttpRequestMessage request, WorkTrackingSystemConnection connection, CancellationToken cancellationToken)
        {
            await Task.Yield();

            throw new NotSupportedException("This made-up strategy cannot sign a request.");
        }
    }
}

namespace Lighthouse.Backend.Tests.Architecture.ConnectorRatchetFixtures.Lambda
{
    public sealed class AStrategyThatRefusesInALambda : IWorkTrackingAuthStrategy
    {
        public Task ApplyAsync(HttpRequestMessage request, WorkTrackingSystemConnection connection, CancellationToken cancellationToken)
        {
            Func<Exception> refusal = () => new NotSupportedException("This made-up strategy cannot sign a request.");

            return Task.FromException(refusal());
        }
    }
}

namespace Lighthouse.Backend.Tests.Architecture.ConnectorRatchetFixtures.DerivedException
{
    public sealed class AStrategyThatRefusesOnThisPlatform : IWorkTrackingAuthStrategy
    {
        public Task ApplyAsync(HttpRequestMessage request, WorkTrackingSystemConnection connection, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException("This made-up strategy cannot sign a request on this platform.");
    }
}

namespace Lighthouse.Backend.Tests.Architecture.ConnectorRatchetFixtures.NotAConnector
{
    // Shaped exactly like the connector-port interface, but declared outside the port, so its implementer is no
    // connector and is outside the ratchet's population.
    public interface IALookalikeStrategy
    {
        Task ApplyAsync(HttpRequestMessage request, WorkTrackingSystemConnection connection, CancellationToken cancellationToken);
    }

    public sealed class ALookalikeThatRefuses : IALookalikeStrategy
    {
        public Task ApplyAsync(HttpRequestMessage request, WorkTrackingSystemConnection connection, CancellationToken cancellationToken)
            => throw new NotSupportedException("This made-up type is no connector.");
    }
}

namespace Lighthouse.Backend.Tests.Architecture.ConnectorRatchetFixtures.OtherException
{
    public sealed class AStrategyThatFailsForAnotherReason : IWorkTrackingAuthStrategy
    {
        public Task ApplyAsync(HttpRequestMessage request, WorkTrackingSystemConnection connection, CancellationToken cancellationToken)
            => throw new InvalidOperationException("This made-up strategy was called in the wrong state.");
    }
}
