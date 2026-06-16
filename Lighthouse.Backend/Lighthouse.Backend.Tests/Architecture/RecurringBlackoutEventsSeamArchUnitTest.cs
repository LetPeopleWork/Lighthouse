using ArchUnitNET.NUnit;
using Lighthouse.Backend.Services.Implementation;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class RecurringBlackoutEventsSeamArchUnitTest
    {
        private const string RecurringBlackoutRuleFullName = "Lighthouse.Backend.Models.RecurringBlackoutRule";
        private const string RepositoriesNamespace = "Lighthouse.Backend.Services.Interfaces.Repositories";
        private const string ServiceImplementationNamespace = "Lighthouse.Backend.Services.Implementation";
        private const string ForecastChartControllersPattern = "Lighthouse.Backend.API";
        private const string BlackoutPeriodServiceInterfaceFullName = "Lighthouse.Backend.Services.Interfaces.IBlackoutPeriodService";

        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        [Test]
        public void RecurringBlackoutRule_DoesNotDependOnRepositories()
        {
            Classes().That().HaveFullName(RecurringBlackoutRuleFullName)
                .Should().NotDependOnAny(Types().That().ResideInNamespace(RepositoriesNamespace))
                .Because(
                    "ADR-060: the recurring-rule entity must not acquire IRepository<RecurringBlackoutRule> or any " +
                    "repository. The union service fetches the rules and expands them into blackout days; the entity " +
                    "stays a pure data shape.")
                .Check(Architecture);
        }

        [Test]
        public void RecurringBlackoutRule_DoesNotDependOnServiceImplementations()
        {
            Classes().That().HaveFullName(RecurringBlackoutRuleFullName)
                .Should().NotDependOnAny(Types().That().ResideInNamespace(ServiceImplementationNamespace))
                .Because(
                    "ADR-060 Models ↛ Services: the recurring-rule entity must not depend on the BlackoutPeriodService " +
                    "or any service that evaluates it. Day expansion lives in an extension method that depends on the " +
                    "entity, never the reverse — the entity carries only its data and the IEntity marker.")
                .Check(Architecture);
        }

        [Test]
        public void ForecastAndChartControllers_DependOnTheUnionService()
        {
            Classes().That().ResideInNamespace(ForecastChartControllersPattern)
                .And().HaveNameEndingWith("MetricsController")
                .Should().DependOnAny(Types().That().HaveFullName(BlackoutPeriodServiceInterfaceFullName))
                .Because(
                    "ADR-059 architectural enforcement: every forecast/chart eval path resolves blackout days through " +
                    "IBlackoutPeriodService.GetEffectiveBlackoutDays — the single union of one-off periods and expanded " +
                    "recurring rules — never the raw IRepository<BlackoutPeriod>. Pinning the dependency on the union " +
                    "service stops a new eval site from reverting to GetAll().")
                .Check(Architecture);
        }
    }
}
