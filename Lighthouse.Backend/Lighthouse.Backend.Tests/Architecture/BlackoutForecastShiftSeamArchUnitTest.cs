using ArchUnitNET.NUnit;
using Lighthouse.Backend.Services.Implementation;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class BlackoutForecastShiftSeamArchUnitTest
    {
        private const string ForecastModelsNamespace = "Lighthouse.Backend.Models.Forecast";
        private const string RepositoriesNamespace = "Lighthouse.Backend.Services.Interfaces.Repositories";

        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        [Test]
        public void ForecastModels_DoNotDependOnRepositories()
        {
            Classes().That().ResideInNamespace(ForecastModelsNamespace)
                .Should().NotDependOnAny(Types().That().ResideInNamespace(RepositoriesNamespace))
                .Because(
                    "ADR-058 / DC-2 (A1): the day↔date blackout translation is threaded into the forecast models as an " +
                    "IReadOnlyList<BlackoutPeriod> parameter, never a repository dependency. Forecast models must not " +
                    "acquire IRepository<BlackoutPeriod> — the assembly layer fetches the global periods and passes them inward.")
                .Check(Architecture);
        }

        [Test]
        public void FeatureAndDeliveryModels_DoNotDependOnRepositories()
        {
            Classes().That().HaveFullName("Lighthouse.Backend.Models.Feature").Or().HaveFullName("Lighthouse.Backend.Models.Delivery")
                .Should().NotDependOnAny(Types().That().ResideInNamespace(RepositoriesNamespace))
                .Because(
                    "ADR-058 enforcement: Feature.GetLikelhoodForDate and Delivery.CalculateMetrics receive the blackout periods " +
                    "as a parameter (A1) and must not depend on IRepository<BlackoutPeriod> (upholds the brief's Models ↛ Repositories invariant).")
                .Check(Architecture);
        }
    }
}
