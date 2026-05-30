using System.Linq;
using ArchUnitNET.Domain.Dependencies;
using ArchUnitNET.Loader;
using Lighthouse.Backend.Services.Implementation;
using Microsoft.Extensions.DependencyInjection;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class DomainEventDispatcherSeamArchUnitTest
    {
        private const string DomainEventsImplementationNamespaceFragment = ".Services.Implementation.DomainEvents";
        private const string GetRequiredServiceMemberPrefix = "GetRequiredService(";

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
    }
}
