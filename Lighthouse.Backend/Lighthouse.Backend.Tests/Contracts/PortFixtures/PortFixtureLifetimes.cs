using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.Extensions.DependencyInjection;
using Lighthouse.Backend.Modules.Forecasting.Domain;

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
        // per request. A port fixture matches such a service only by registering it as a factory too, with the
        // host's lifetime, next to its target with the host's lifetime.
        private static readonly Dictionary<Type, Type> ForwardersAndTheirTargets = new Dictionary<Type, Type>
        {
            [typeof(IRepository<Portfolio>)] = typeof(IPortfolioRepository),
            [typeof(IReportReader)] = typeof(ReportRepository),
            [typeof(IReportWriter)] = typeof(ReportRepository),
        };

        private IEnumerable<Type?> BuiltOnTheHostForAnyRegistrationOf(Type serviceType)
            => hostRegistrations.Where(host => !host.IsKeyedService && host.ServiceType == serviceType).Select(WhatIsBuilt);

        public ServiceDescriptor? OnTheHost(Type serviceType)
            => hostRegistrations.LastOrDefault(host => !host.IsKeyedService && host.ServiceType == serviceType);

        public List<string> HowItDiffersFromTheHost(IServiceCollection whatThePortFixtureRegisters)
            => whatThePortFixtureRegisters
                .Select(port => HowItDiffersFromTheHost(port, whatThePortFixtureRegisters))
                .OfType<string>()
                .ToList();

        private string? HowItDiffersFromTheHost(ServiceDescriptor port, IServiceCollection whatThePortFixtureRegisters)
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

            if (StandInsBuiltOnPurpose.Contains(port.ServiceType))
            {
                return null;
            }

            if (ForwardersAndTheirTargets.TryGetValue(port.ServiceType, out var target))
            {
                return IsForwardedLikeOnTheHost(port, target, whatThePortFixtureRegisters)
                    ? null
                    : $"{port.ServiceType.Name} is not a factory forwarding to {target.Name} with the host's lifetimes";
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

        private bool IsForwardedLikeOnTheHost(ServiceDescriptor port, Type target, IServiceCollection whatThePortFixtureRegisters)
        {
            var isFactory = port.ImplementationType == null && port.ImplementationInstance == null && port.ImplementationFactory != null;
            var targetOnTheHost = OnTheHost(target);
            var targetAtThePort = whatThePortFixtureRegisters.LastOrDefault(registration => !registration.IsKeyedService && registration.ServiceType == target);
            return isFactory
                && targetOnTheHost != null
                && targetAtThePort != null
                && targetAtThePort.Lifetime == targetOnTheHost.Lifetime;
        }

        public static Type? WhatIsBuilt(ServiceDescriptor registration)
            => registration.ImplementationType ?? registration.ImplementationInstance?.GetType();

        private static string Named(Type? built) => built?.Name ?? "whatever a factory returns";
    }
}
