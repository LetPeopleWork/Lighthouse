using Lighthouse.Backend.Models;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The two handlers that react to a Portfolio's forecasts being updated, the Delivery metric snapshot recording and the
    /// Delivery forecast publishing, live in the Promises module's Application policies, and none is left under the domain-event
    /// folders of Services. Their two unit-test classes sit in the matching test folder. Each type is named as a string and read from
    /// the compiled assembly, so a type that is missing, renamed or left behind is a failure that names it.
    /// </summary>
    [TestFixture]
    public class PromisesPoliciesLayoutArchUnitTest
    {
        private const string PoliciesNamespace = "Lighthouse.Backend.Modules.Promises.Application.Policies";

        private const string TestPoliciesNamespace = "Lighthouse.Backend.Tests.Modules.Promises.Application.Policies";

        private const string WhereTheHandlersLivedBefore = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)(\.DomainEvents)?(\.|$)";

        private const string WhereTheHandlerTestsLivedBefore = @"^Lighthouse\.Backend\.Tests\.Services(\.|$)";

        private static readonly string[] ForecastsUpdatedHandlers =
        [
            "DeliveryMetricSnapshotRecordingHandler",
            "DeliveryForecastPublishingHandler",
        ];

        private static readonly string[] ForecastsUpdatedHandlerTests =
        [
            "DeliveryMetricSnapshotRecordingHandlerTest",
            "DeliveryForecastPublishingHandlerTest",
        ];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(PromisesPoliciesLayoutArchUnitTest).Assembly));

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void TheTwoForecastsUpdatedHandlers_LiveInThePromisesApplicationPolicies_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, PoliciesNamespace, ForecastsUpdatedHandlers, WhereTheHandlersLivedBefore).Check();
        }

        [Test]
        public void TheUnitTestsOfTheTwoForecastsUpdatedHandlers_LiveInThePromisesPoliciesTestFolder_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(TestModule.Value, TestPoliciesNamespace, ForecastsUpdatedHandlerTests, WhereTheHandlerTestsLivedBefore).Check();
        }

        [Test]
        public void AHandlerNameThatNoTypeMatches_FailsTheLayoutRuleAsMissing_InsteadOfPassingUnseen()
        {
            const string anInventedName = "DeliveryForecastPublishingHandlerThatDoesNotExist";

            var verdict = ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, PoliciesNamespace, [anInventedName], WhereTheHandlersLivedBefore);

            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                "A handler name that no type in the assembly matches passed the layout rule. Why it matters: a list that has drifted from the code " +
                "stays green forever and guards nothing. How to fix: the rule must fail for a name whose type is not in the layer.");

            Assert.That(failure!.Message, Does.Contain($"{PoliciesNamespace}.{anInventedName}"), "The failure must name the missing type, so the reader knows which list entry to correct.");
        }
    }
}
