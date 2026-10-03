using ArchUnitNET.Domain.Dependencies;
using ArchUnitNET.NUnit;
using Lighthouse.Backend.API.Helpers;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class RefinementModuleArchUnitTest
    {
        private const string ServicesImplementationPattern = @"^Lighthouse\.Backend\.Services\.Implementation($|\..*)";

        private const string ApiLayerPattern = @"^Lighthouse\.Backend\.API($|\..*)";

        private const string CompositionRootPattern = @"^Lighthouse\.Backend\.Program($|\+.*)";

        private const string RefinementImplementationNamespace = "Lighthouse.Backend.Services.Implementation.Refinement";

        private const string RepositoriesNamespace = "Lighthouse.Backend.Services.Interfaces.Repositories";

        private static readonly string[] RepositoryWriters = ["Save", "Add", "Update", "Remove", "Delete", "Apply"];

        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        // The rules that decide what a Team may save as its refinement setup are plain functions of the values
        // being saved. Kept that way, every rule can be tested by handing it values, and none of them can quietly
        // start reading the database or the trackers while it judges a save.
        [Test]
        public void TheRefinementSettingsValidator_IsStatic()
        {
            var validator = typeof(RefinementSettingsValidator);

            Assert.That(validator.IsAbstract && validator.IsSealed, Is.True,
                "The refinement settings validator must stay a static class: it judges the values it is given and holds nothing.");
        }

        [Test]
        public void TheRefinementSettingsValidator_ReferencesNothingInServicesImplementation()
        {
            Types().That().HaveFullName(typeof(RefinementSettingsValidator).FullName!)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(ServicesImplementationPattern))
                .Because("a validator that reaches into a service can no longer be tested by handing it values.")
                .Check(Architecture);
        }

        [Test]
        public void Nothing_but_the_API_and_the_composition_root_depends_on_the_Refinement_module()
        {
            Types().That().DoNotResideInNamespaceMatching(ModuleBoundariesArchUnitTest.RefinementPattern)
                .And().DoNotResideInNamespaceMatching(ApiLayerPattern)
                .And().DoNotHaveFullNameMatching(CompositionRootPattern)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(ModuleBoundariesArchUnitTest.RefinementPattern))
                .Because("refinement sits on top of the Team and its Work Items; anything below reaching up into it would tie forecasting and syncing to a coaching screen.")
                .Check(Architecture);
        }

        [Test]
        public void The_Refinement_module_reaches_neither_the_trackers_nor_the_background_updates()
        {
            Types().That().ResideInNamespaceMatching(ModuleBoundariesArchUnitTest.RefinementPattern)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(ModuleBoundariesArchUnitTest.WorkTrackingIntegrationPattern)
                    .Or().ResideInNamespaceMatching(ModuleBoundariesArchUnitTest.PortfolioDeliveryPattern))
                .Because("refinement reads what Lighthouse already holds; it never asks a tracker or starts an update to answer.")
                .Check(Architecture);
        }

        // Read off the call dependencies directly: the fluent call rule does not match a method declared on the
        // generic repository interface, so it would pass a read that saves.
        [Test]
        public void Reading_the_Refinement_tab_calls_no_repository_member_that_writes()
        {
            var readPath = Architecture.Types
                .Where(type => type.Namespace.FullName == RefinementImplementationNamespace)
                .ToList();
            var writes = readPath
                .SelectMany(type => type.Dependencies.OfType<MethodCallDependency>())
                .Where(call => call.TargetMember.DeclaringType.Namespace.FullName == RepositoriesNamespace
                    && RepositoryWriters.Any(writer => call.TargetMember.Name.StartsWith(writer, StringComparison.Ordinal)))
                .Select(call => $"{call.Origin.FullName} calls {call.TargetMember.DeclaringType.FullName}.{call.TargetMember.Name}")
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(readPath.Select(type => type.Name), Does.Contain("RefinementViewQuery"),
                    "the rule finds the read path by its namespace; without it the rule guards nothing");
                Assert.That(writes, Is.Empty,
                    "opening the Refinement tab is a read; saving, adding, updating or removing through a repository would make it write");
            }
        }
    }
}
