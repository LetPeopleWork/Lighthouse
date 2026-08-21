using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Linear
{
    /// <summary>
    /// The relation mapping against the real demo workspace, rather than against a payload written here.
    ///
    /// Four Projects carry dependencies between them, arranged the way the demo Jira project is: one
    /// waiting on two others, one of those waiting on a third, and two that only ever block. The last
    /// pair is the point - they must read empty, because a mapper that read a Project's own relations
    /// instead of its inverse ones would point every edge backwards and still produce a plausible count.
    /// </summary>
    [Category("Integration")]
    [Category("LinearIntegration")]
    public class LinearDependencyDogfoodTest
    {
        private const string Epsilon960 = "7b822efd-fae2-4c9f-b731-47b489efe75d";
        private const string Zeta361 = "eb8f471d-18f8-456b-9385-b7a6fb43a432";
        private const string Gamma767 = "37277f52-e884-4a7f-b422-d9e50336a23c";
        private const string Zeta797 = "00c1acac-f18e-4bd8-9919-25099d648011";

        [Test]
        public async Task GetFeaturesForProject_ReadsTheDependenciesTheDemoWorkspaceReallyHas()
        {
            var waitedOnBy = await WhatEachDemoProjectWaitsOn();

            var zeta797WaitsOnBoth = new[] { Epsilon960, Zeta361 }.Order().ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(waitedOnBy[Zeta797], Is.EqualTo(zeta797WaitsOnBoth),
                    "Zeta-797 is blocked by both Epsilon-960 and Zeta-361 in the demo workspace.");
                Assert.That(waitedOnBy[Zeta361], Is.EqualTo(new[] { Gamma767 }),
                    "Zeta-361 is blocked by Gamma-767.");

                Assert.That(waitedOnBy[Epsilon960], Is.Empty,
                    "Epsilon-960 only blocks another Project. Reading a Project's own relations rather than its "
                    + "inverse ones would make this the busiest waiter in the workspace.");
                Assert.That(waitedOnBy[Gamma767], Is.Empty,
                    "Same for Gamma-767: it blocks Zeta-361, and waits on nothing itself.");
            }
        }

        /// <summary>
        /// The reference the relation yields has to be the very string the other Feature is keyed by.
        /// Anything else resolves to nothing, and a Portfolio whose links all resolve to nothing looks
        /// exactly like one that has no dependencies.
        /// </summary>
        [Test]
        public async Task GetFeaturesForProject_EveryReferenceNamesAProjectTheWorkspaceAlsoReturned()
        {
            var features = await TheDemoProjectsAsLighthouseReadsThem();

            var idsHeld = features.ConvertAll(feature => feature.ReferenceId);
            var referenced = features
                .SelectMany(feature => feature.DependsOnReferences.Select(reference => reference.ReferenceId))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(referenced, Is.Not.Empty, "A workspace that read no dependency at all would pass the next line for free.");
                Assert.That(referenced, Is.SubsetOf(idsHeld));
            }
        }

        private static async Task<Dictionary<string, List<string>>> WhatEachDemoProjectWaitsOn()
        {
            var features = await TheDemoProjectsAsLighthouseReadsThem();

            return features.ToDictionary(
                feature => feature.ReferenceId,
                feature => feature.DependsOnReferences.Select(reference => reference.ReferenceId).Order().ToList());
        }

        private static async Task<List<Feature>> TheDemoProjectsAsLighthouseReadsThem()
        {
            var subject = new LinearWorkTrackingConnector(
                Mock.Of<ILogger<LinearWorkTrackingConnector>>(), new FakeCryptoService());

            return await subject.GetFeaturesForProject(TheDemoPortfolio());
        }

        private static Portfolio TheDemoPortfolio()
        {
            var apiKey = Environment.GetEnvironmentVariable("LinearAPIKey")
                ?? throw new NotSupportedException("Can run test only if Environment Variable 'LinearAPIKey' is set!");

            var connection = new WorkTrackingSystemConnection
            {
                WorkTrackingSystem = WorkTrackingSystems.Linear,
                Name = "Demo Linear",
            };

            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = LinearWorkTrackingOptionNames.ApiKey,
                Value = apiKey,
                IsSecret = true,
            });

            // No state filter: the four Projects this reads are wherever the demo workspace left them, and
            // narrowing by state would drop one the day somebody moves it.
            var portfolio = new Portfolio
            {
                Name = "Demo Portfolio",
                WorkTrackingSystemConnection = connection,
            };

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("Project");
            portfolio.ToDoStates.Clear();
            portfolio.DoingStates.Clear();
            portfolio.DoneStates.Clear();

            return portfolio;
        }
    }
}
