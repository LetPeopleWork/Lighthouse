using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lighthouse.Backend.Tests.Startup
{
    public class HostRunTest
    {
        // Long enough for an empty host to start and stop, short enough that a host which never
        // reports its failure fails this test instead of hanging the suite.
        private static readonly TimeSpan HostDeadline = TimeSpan.FromSeconds(5);

        [Test]
        public async Task RunHostAsync_StartFails_ThrowsTheStartupFailure()
        {
            await using var app = BuildEmptyApp(services => services.AddHostedService<FailingHostedService>());

            // A host that swallows its failure never ends on its own; stopping it releases the test.
            // Taken before running, because a host that does end disposes its services.
            var lifetime = app.Lifetime;

            try
            {
                var run = Backend.Program.RunHostAsync(app, () => { });

                Assert.That(async () => await run.WaitAsync(HostDeadline), Throws.InvalidOperationException);
            }
            finally
            {
                lifetime.StopApplication();
            }
        }

        [Test]
        public async Task RunHostAsync_StartSucceeds_CallsOnStarted()
        {
            await using var app = BuildEmptyApp(_ => { });
            var started = false;

            await Backend.Program.RunHostAsync(app, () =>
            {
                started = true;
                app.Lifetime.StopApplication();
            }).WaitAsync(HostDeadline);

            Assert.That(started, Is.True);
        }

        private static WebApplication BuildEmptyApp(Action<IServiceCollection> configureServices)
        {
            var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
            builder.WebHost.UseKestrelCore();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            configureServices(builder.Services);
            return builder.Build();
        }

        private sealed class FailingHostedService : IHostedService
        {
            public Task StartAsync(CancellationToken cancellationToken) =>
                throw new InvalidOperationException("hosted service failed to start");

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }
    }
}
