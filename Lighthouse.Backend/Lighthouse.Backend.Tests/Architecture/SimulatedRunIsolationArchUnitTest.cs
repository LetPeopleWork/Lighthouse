using System.Reflection;
using ArchUnitNET.NUnit;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Forecast;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Ten thousand simulated runs of a forecast happen at once, and they are safe together for one reason:
    /// what a run works with belongs to that run, and what they all read is never written to. That is not a
    /// property a type can hold on its own, so it is asserted here.
    ///
    /// The counts a run works with used to live on the rows the forecast reports, which was safe only for as
    /// long as each Team ran on its own. Putting them back there would be a defect nothing else would catch:
    /// two runs sharing one count produces dates that look perfectly ordinary.
    /// </summary>
    [TestFixture]
    [Category("epic-5792-dependency-aware-forecasting")]
    [Category("slice-02")]
    public class SimulatedRunIsolationArchUnitTest
    {
        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        private const string TheForecastComponents = "Lighthouse.Backend.Services.Implementation.Forecast";

        [Test]
        public void WhatEverySimulatedRunReads_CannotBeWrittenTo()
        {
            var writable = typeof(ForecastRunPlan)
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(field => !field.IsInitOnly)
                .Select(field => field.Name)
                .ToList();

            Assert.That(writable, Is.Empty,
                $"{nameof(ForecastRunPlan)} is read by every simulated run at once, so nothing on it may be " +
                "written to after it is built. A field that can be assigned is a race between runs that " +
                "produces perfectly ordinary looking dates. Fields that can still be written: " +
                string.Join(", ", writable));
        }

        [Test]
        public void WhatAForecastReports_CarriesNoCountsARunWorksWith()
        {
            var writable = typeof(SimulationResult)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.SetMethod is not null && property.SetMethod.IsPublic)
                .Select(property => property.Name)
                .ToList();

            Assert.That(writable, Is.Empty,
                $"{nameof(SimulationResult)} is what a forecast reports, and every simulated run can see it. " +
                "A property a run can write to is the shared scratchpad this epic removed, put back. What a " +
                $"run works with belongs in {nameof(TrialState)}. Properties that can be written: " +
                string.Join(", ", writable));
        }

        [Test]
        public void WhatOneSimulatedRunKnows_IsReachableFromNowhereElse()
        {
            Types().That()
                .DoNotResideInNamespace(TheForecastComponents)
                .Should().NotDependOnAny(Types().That().Are(typeof(TrialState), typeof(TrialCompletions)))
                .Because(
                    "What one simulated run has left to do, and what it has finished, belong to that run. " +
                    "Anything outside the forecast holding one of them is holding the working state of a " +
                    "single run out of ten thousand, and would be reading it while the others are still " +
                    "going.")
                .Check(Architecture);
        }
    }
}
