using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Contracts.PortFixtures
{
    /// <summary>
    /// A port fixture builds its services by hand, so it can drift from the host: a service the host makes per
    /// request, given one instance for the whole scenario, shares state the product never shares, and a scenario
    /// passes on it that a real caller could never see. This compares a port fixture's registrations with the
    /// host's, by service type and lifetime, and by what is built for them. The stand-ins a port fixture puts in
    /// on purpose (the licence, the settings, the sign-in mode, the scripted forecast, the clocks and the pinned random draws) are
    /// compared by lifetime only. Any other service a factory builds on both sides is reported, as what two
    /// factories return cannot be told apart without running them.
    /// </summary>
    internal sealed class PortFixtureLifetimes(IReadOnlyList<ServiceDescriptor> hostRegistrations)
    {
        private static readonly Type[] StandInsBuiltOnPurpose =
        [
            typeof(ILicenseService),
            typeof(IAppSettingService),
            typeof(IForecastService),
            typeof(ILighthouseClock),
            typeof(TimeProvider),
            typeof(IDrawStreamFactory),
            typeof(IAuthModeResolver),
        ];

        // Registered on the host as a factory that hands back another registration of the host, one unit of work
        // per request; the lifetime is what a port fixture must match, as what the factory returns is that registration.
        private static readonly Type[] ForwardedToTheirConcreteService =
        [
            typeof(IRepository<Portfolio>),
            typeof(IReportReader),
            typeof(IReportWriter),
        ];

        private IEnumerable<Type?> BuiltOnTheHostForAnyRegistrationOf(Type serviceType)
            => hostRegistrations.Where(host => !host.IsKeyedService && host.ServiceType == serviceType).Select(WhatIsBuilt);

        public ServiceDescriptor? OnTheHost(Type serviceType)
            => hostRegistrations.LastOrDefault(host => !host.IsKeyedService && host.ServiceType == serviceType);

        public List<string> HowItDiffersFromTheHost(IServiceCollection whatThePortFixtureRegisters)
            => whatThePortFixtureRegisters
                .Select(HowItDiffersFromTheHost)
                .OfType<string>()
                .ToList();

        private string? HowItDiffersFromTheHost(ServiceDescriptor port)
        {
            var onTheHost = OnTheHost(port.ServiceType);
            if (onTheHost == null)
            {
                return $"{port.ServiceType.Name} is not registered on the host";
            }

            if (onTheHost.Lifetime != port.Lifetime)
            {
                return $"{port.ServiceType.Name} lives as {port.Lifetime} at the port and as {onTheHost.Lifetime} on the host";
            }

            if (StandInsBuiltOnPurpose.Contains(port.ServiceType) || ForwardedToTheirConcreteService.Contains(port.ServiceType))
            {
                return null;
            }

            var builtAtThePort = WhatIsBuilt(port);
            var builtOnTheHost = WhatIsBuilt(onTheHost);
            if (builtAtThePort == null && builtOnTheHost == null)
            {
                return $"{port.ServiceType.Name} cannot be compared: a factory builds it on both sides";
            }

            // A service several types are registered for (every report metric) is read as a list, so the port
            // is right when the host registers the same type among them.
            if (builtAtThePort != null && BuiltOnTheHostForAnyRegistrationOf(port.ServiceType).Contains(builtAtThePort))
            {
                return null;
            }

            return builtAtThePort == builtOnTheHost
                ? null
                : $"{port.ServiceType.Name} is built as {Named(builtAtThePort)} at the port and as {Named(builtOnTheHost)} on the host";
        }

        public static Type? WhatIsBuilt(ServiceDescriptor registration)
            => registration.ImplementationType ?? registration.ImplementationInstance?.GetType();

        private static string Named(Type? built) => built?.Name ?? "whatever a factory returns";
    }
}
