using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Tests.API.Integration.ForecastRealityCheck;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Contracts.PortFixtures
{
    /// <summary>
    /// A port fixture builds its services by hand, so it can drift from the host: a service the host makes per
    /// request, given one instance for the whole scenario, shares state the product never shares, and a scenario
    /// passes on it that a real caller could never see. Each port fixture's registrations are compared with the
    /// host's, by service type and lifetime, and by what is built for them. The stand-ins a port fixture puts in
    /// on purpose (the licence, the settings, the scripted forecast, the clocks and the pinned random draws) are
    /// compared by lifetime only.
    /// </summary>
    public class PortFixtureLifetimesWireContractTest : WireContractTest
    {
        private static readonly Type[] StandInsBuiltOnPurpose =
        [
            typeof(ILicenseService),
            typeof(IAppSettingService),
            typeof(IForecastService),
            typeof(ILighthouseClock),
            typeof(TimeProvider),
            typeof(IDrawStreamFactory),
        ];

        private List<ServiceDescriptor> hostRegistrations = [];

        protected override WebApplicationFactory<Program> AdjustTheHost(TestWebApplicationFactory<Program> unchanged)
            => unchanged.WithWebHostBuilder(builder => builder.ConfigureTestServices(services => hostRegistrations = [.. services]));

        private static IEnumerable<TestCaseData> PortFixtures()
        {
            yield return PortFixture<Slice01OneSentenceAboutYourSamplingWindowTest>();
            yield return PortFixture<Slice04TheAnswerOpensInADialogTest>();
        }

        private static TestCaseData PortFixture<TFixture>() where TFixture : RealityCheckAtThePortTest, new()
            => new TestCaseData(new TFixture()).SetArgDisplayNames(typeof(TFixture).Name);

        [TestCaseSource(nameof(PortFixtures))]
        public void Every_service_a_port_fixture_registers_lives_as_long_as_it_does_on_the_host(RealityCheckAtThePortTest portFixture)
        {
            var differences = portFixture.WhatItRegisters
                .Select(HowItDiffersFromTheHost)
                .OfType<string>()
                .ToList();

            Assert.That(differences, Is.Empty, $"{portFixture.GetType().Name} registers services unlike the host");
        }

        private string? HowItDiffersFromTheHost(ServiceDescriptor port)
        {
            var onTheHost = hostRegistrations.LastOrDefault(host => !host.IsKeyedService && host.ServiceType == port.ServiceType);
            if (onTheHost == null)
            {
                return $"{port.ServiceType.Name} is not registered on the host";
            }

            if (onTheHost.Lifetime != port.Lifetime)
            {
                return $"{port.ServiceType.Name} lives as {port.Lifetime} at the port and as {onTheHost.Lifetime} on the host";
            }

            if (StandInsBuiltOnPurpose.Contains(port.ServiceType))
            {
                return null;
            }

            var builtAtThePort = WhatIsBuilt(port);
            var builtOnTheHost = WhatIsBuilt(onTheHost);
            return builtAtThePort == builtOnTheHost
                ? null
                : $"{port.ServiceType.Name} is built as {Named(builtAtThePort)} at the port and as {Named(builtOnTheHost)} on the host";
        }

        private static Type? WhatIsBuilt(ServiceDescriptor registration)
            => registration.ImplementationType ?? registration.ImplementationInstance?.GetType();

        private static string Named(Type? built) => built?.Name ?? "whatever a factory returns";
    }
}
