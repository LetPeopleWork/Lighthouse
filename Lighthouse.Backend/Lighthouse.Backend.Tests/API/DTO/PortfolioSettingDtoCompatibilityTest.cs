using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;
using NUnit.Framework;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.API.DTO
{
    /// <summary>
    /// A Portfolio's settings are saved by payloads written before the settings in them existed - the
    /// end-to-end helpers, the command line client, and whatever anyone has scripted against the API.
    /// Every one of those omits any field added since, so a field the server insists on is a field that
    /// turns a working payload into a 400, and the caller learns nothing about which one.
    ///
    /// The guard is deliberately blunt: it saves the payload with EVERY new field left out, rather than
    /// naming them, because the field that breaks the next caller is the one nobody thought to name.
    /// </summary>
    [TestFixture]
    public class PortfolioSettingDtoCompatibilityTest
    {
        /// <summary>
        /// The shape a client that predates every dependency setting sends: no dependency field, no ignore
        /// switch, and nothing else added since either.
        /// </summary>
        private const string WhatAnOlderClientSends = """
            {
              "id": 0,
              "name": "Platform",
              "workItemTypes": ["Epic"],
              "toDoStates": ["New"],
              "doingStates": ["Active"],
              "doneStates": ["Done"],
              "overrideRealChildCountStates": [],
              "dataRetrievalValue": "[System.WorkItemType] = 'Epic'",
              "usePercentileToCalculateDefaultAmountOfWorkItems": false,
              "defaultWorkItemPercentile": 85,
              "percentileHistoryInDays": 90,
              "defaultAmountOfWorkItemsPerFeature": 10,
              "owningTeam": null,
              "workTrackingSystemConnectionId": 1,
              "serviceLevelExpectationProbability": 80,
              "serviceLevelExpectationRange": 25,
              "systemWIPLimit": 2,
              "stalenessThresholdDays": 0,
              "blockedStalenessThresholdDays": 0,
              "doneItemsCutoffDays": 365,
              "stateMappings": [],
              "cycleTimeDefinitions": [],
              "waitStates": [],
              "estimationCategoryValues": [],
              "involvedTeams": []
            }
            """;

        [Test]
        public void APayloadFromBeforeTheseSettingsExisted_IsStillAccepted()
        {
            Assert.DoesNotThrow(() => Deserialize(),
                "A field the server requires is a field that turns every saved payload into a 400.");
        }

        [Test]
        public void APayloadThatSaysNothingAboutDependencies_LeavesThePortfolioActingOnThem()
        {
            var portfolio = new Portfolio { Name = "Platform" };

            portfolio.SyncWithPortfolioSettings(Deserialize(), Mock.Of<IRepository<Team>>());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(portfolio.IgnoreDependencies, Is.False,
                    "Saying nothing has to mean what a Portfolio did before the setting was there.");
                Assert.That(portfolio.DependencyOverrideAdditionalFieldDefinitionId, Is.Null);
            }
        }

        [Test]
        public void APayloadThatDoesSayWhatItWants_IsTakenAtItsWord()
        {
            var portfolio = new Portfolio { Name = "Platform" };
            var settings = Deserialize();
            settings.IgnoreDependencies = true;
            settings.DependencyOverrideAdditionalFieldDefinitionId = 7;

            portfolio.SyncWithPortfolioSettings(settings, Mock.Of<IRepository<Team>>());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(portfolio.IgnoreDependencies, Is.True);
                Assert.That(portfolio.DependencyOverrideAdditionalFieldDefinitionId, Is.EqualTo(7));
            }
        }

        private static PortfolioSettingDto Deserialize()
            => JsonSerializer.Deserialize<PortfolioSettingDto>(
                WhatAnOlderClientSends,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
}
