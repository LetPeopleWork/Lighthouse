using System.Linq;
using ArchUnitNET.Domain.Dependencies;
using ArchUnitNET.Loader;
using ArchUnitNET.NUnit;
using Lighthouse.Backend.Services.Implementation;
using Microsoft.Extensions.DependencyInjection;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class DomainEventDispatcherSeamArchUnitTest
    {
        private const string DomainEventsImplementationNamespaceFragment = ".Services.Implementation.DomainEvents";
        private const string GetRequiredServiceMemberPrefix = "GetRequiredService(";

        private const string DomainEventsImplementationNamespacePattern = @"^Lighthouse\.Backend\.Services\.Implementation\.DomainEvents($|\..*)";
        private const string DomainEventsInterfaceNamespacePattern = @"^Lighthouse\.Backend\.Services\.Interfaces\.DomainEvents($|\..*)";
        private const string EventsModelNamespacePattern = @"^Lighthouse\.Backend\.Models\.Events($|\..*)";
        private const string ApiNamespacePattern = @"^Lighthouse\.Backend\.API($|\..*)";

        private static readonly ArchitectureModel Architecture = new ArchLoader()
            .LoadAssemblies(
                typeof(TeamMetricsService).Assembly,
                typeof(ServiceProviderServiceExtensions).Assembly)
            .Build();

        [Test]
        public void DomainEventSeamTypes_DoNotResolveViaServiceLocator()
        {
            var offenders = Architecture.MethodMembers
                .Where(member => member.DeclaringType.FullName.Contains(DomainEventsImplementationNamespaceFragment))
                .SelectMany(member => member.MemberDependencies
                    .OfType<MethodCallDependency>()
                    .Where(dependency => dependency.TargetMember.Name.StartsWith(GetRequiredServiceMemberPrefix))
                    .Select(_ => $"{member.DeclaringType.Name}.{member.Name}"))
                .Distinct()
                .ToList();

            Assert.That(
                offenders,
                Is.Empty,
                "ADR-027 D3: the dispatcher and its handlers must resolve collaborators via typed " +
                "IEnumerable<IDomainEventHandler<TEvent>> (IServiceProvider.GetServices), never " +
                "IServiceProvider.GetRequiredService — otherwise the seam re-introduces the service locator it removes. " +
                "Offenders: " + string.Join(", ", offenders));
        }

        [Test]
        public void DomainEventSeamTypes_DoNotDependOnApi()
        {
            Types().That().ResideInNamespaceMatching(DomainEventsImplementationNamespacePattern)
                .Or().ResideInNamespaceMatching(DomainEventsInterfaceNamespacePattern)
                .Or().ResideInNamespaceMatching(EventsModelNamespacePattern)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(ApiNamespacePattern))
                .Because(
                    "ADR-027 D3: domain-event records, the dispatcher port and its implementation sit below the API " +
                    "layer so Services.Implementation does not depend on API through the new seam. This rule is scoped " +
                    "to the slice-01 seam types; the full module-boundary enforcement (Services.Implementation as a " +
                    "whole has pre-existing API references) lands in slice 04 / #5101.")
                .Check(Architecture);
        }
    }
}
