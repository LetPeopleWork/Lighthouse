using System.Text.Json.Nodes;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Reports.Metrics;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Lighthouse.Backend.Tests.Contracts.PortFixtures;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// A port fixture that registers a service the way the host does gives no difference; a registration that only
    /// looks like the host's (the same service built from another type, a forward target missing, a metric the host
    /// never registers) gives exactly one difference, naming the service.
    /// </summary>
    [TestFixture]
    public class PortFixtureLifetimesTest
    {
        private static readonly Type[] TheFourReportMetrics =
            [typeof(CycleTimeReportMetric), typeof(ThroughputReportMetric), typeof(WipReportMetric), typeof(WorkItemAgeReportMetric)];

        [Test]
        public void A_port_registering_the_services_as_the_host_does_differs_in_nothing()
        {
            var differences = HowThePortDiffers(plant: _ => { });

            Assert.That(differences, Is.Empty,
                "a port that registers its services as the host does is the case the check must let through, or no port fixture could pass it");
        }

        [TestCaseSource(nameof(TheLookalikeRegistrations))]
        public void A_registration_that_only_looks_like_the_hosts_is_reported_by_service(Lookalike lookalike)
        {
            var differences = HowThePortDiffers(lookalike.Plant);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(differences, Has.Count.EqualTo(1),
                    $"{lookalike} must give one difference, or a port could drift from the host unseen. Differences given: [{string.Join("; ", differences)}]");
                Assert.That(string.Join("; ", differences), Does.Contain(lookalike.Service.Name),
                    "the difference must name the service, so a reader knows which registration to fix");
            }
        }

        private static IEnumerable<TestCaseData> TheLookalikeRegistrations()
        {
            yield return Row(new Lookalike("IRepository<Portfolio> registered as the type PortfolioRepository", typeof(IRepository<Portfolio>),
                port => Replace(port, typeof(IRepository<Portfolio>), ServiceDescriptor.Scoped<IRepository<Portfolio>, PortfolioRepository>())));
            yield return Row(new Lookalike("IReportReader registered as the type ReportRepository", typeof(IReportReader),
                port => Replace(port, typeof(IReportReader), ServiceDescriptor.Scoped<IReportReader, ReportRepository>())));
            yield return Row(new Lookalike("IRepository<Portfolio> factory with IPortfolioRepository missing at the port", typeof(IRepository<Portfolio>),
                port => Replace(port, typeof(IPortfolioRepository))));
            yield return Row(new Lookalike("a report metric type the host does not register", typeof(IReportMetric),
                port => port.AddScoped<IReportMetric, AMetricTheHostNeverRegisters>()));
        }

        private static TestCaseData Row(Lookalike lookalike)
            => new TestCaseData(lookalike).SetName($"A_registration_that_only_looks_like_the_hosts_is_reported_by_service({lookalike})");

        private static List<string> HowThePortDiffers(Action<IServiceCollection> plant)
        {
            var port = RegisteredAsProgramDoes();
            plant(port);
            return new PortFixtureLifetimes(RegisteredAsProgramDoes().ToList()).HowItDiffersFromTheHost(port);
        }

        /// <summary>The registrations the Reports services and what they read have on the host, built without starting it.</summary>
        private static ServiceCollection RegisteredAsProgramDoes()
        {
            var services = new ServiceCollection();
            services.AddScoped<IPortfolioRepository, PortfolioRepository>();
            services.AddScoped<IRepository<Portfolio>>(provider => provider.GetRequiredService<IPortfolioRepository>());
            services.AddScoped<ReportRepository>();
            services.AddScoped<IReportReader>(provider => provider.GetRequiredService<ReportRepository>());
            services.AddScoped<IReportWriter>(provider => provider.GetRequiredService<ReportRepository>());
            foreach (var metric in TheFourReportMetrics)
            {
                services.AddScoped(typeof(IReportMetric), metric);
            }

            return services;
        }

        private static void Replace(IServiceCollection services, Type serviceType, ServiceDescriptor? replacement = null)
        {
            services.Remove(services.Single(registration => registration.ServiceType == serviceType));
            if (replacement != null)
            {
                services.Add(replacement);
            }
        }

        public sealed record Lookalike(string Name, Type Service, Action<IServiceCollection> Plant)
        {
            public override string ToString() => Name;
        }

        private sealed class AMetricTheHostNeverRegisters : IReportMetric
        {
            public string Key => "a-stray-metric";

            public bool AppliesTo(ReportOwnerKind ownerKind) => false;

            public JsonObject CaptureThen(ReportOwner owner, ReportWindow thenWindow) => throw new NotSupportedException();

            public MetricRead Read(JsonObject? capturedThen, ReportOwner owner, ReportWindow nowWindow) => throw new NotSupportedException();
        }
    }
}
