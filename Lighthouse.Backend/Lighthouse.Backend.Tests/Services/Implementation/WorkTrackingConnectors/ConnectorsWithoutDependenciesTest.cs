using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Csv;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors
{
    /// <summary>
    /// The two work tracking systems with no way to say that one Feature waits on another. Neither has a
    /// standard field for it, so neither yields an edge and neither treats the absence as a failure -
    /// inventing a convention for them is a different piece of work, and refusing a refresh over a field
    /// that was never there would take the whole Portfolio down with it.
    /// </summary>
    public class ConnectorsWithoutDependenciesTest : IntegrationTestBase
    {
        [Test]
        public async Task GetFeaturesForProject_ACsvFeatureIsWaitingOnNothingAndSaysSoWithoutComplaint()
        {
            var subject = ServiceProvider.GetService<CsvWorkTrackingConnector>()
                ?? throw new InvalidOperationException("Could not resolve the CSV connector");

            var features = await subject.GetFeaturesForProject(APortfolioReadFromCsv());

            Assert.That(features.TrueForAll(feature => feature.DependsOnReferences.Count == 0), Is.True);
        }

        /// <summary>
        /// ServiceNow returns no Features at all, so there is nothing for a dependency to be between. The
        /// point of asserting it is that the refusal is unchanged - reading dependencies did not turn a
        /// clear "this connector has no Features" into some other failure.
        /// </summary>
        [Test]
        public void GetFeaturesForProject_ServiceNowStillRefusesToReturnFeaturesAtAll()
        {
            var subject = new ServiceNowWorkTrackingConnector(
                Mock.Of<ILogger<ServiceNowWorkTrackingConnector>>(),
                Mock.Of<IWorkTrackingAuthStrategyFactory>());

            Assert.That(
                async () => await subject.GetFeaturesForProject(new Portfolio()),
                Throws.TypeOf<NotSupportedException>());
        }

        private Portfolio APortfolioReadFromCsv()
        {
            var factory = ServiceProvider.GetService<IWorkTrackingSystemFactory>()
                ?? throw new InvalidOperationException("Could not resolve the Work Tracking System factory");

            var connection = factory.CreateDefaultConnectionForWorkTrackingSystem(WorkTrackingSystems.Csv);
            AdjustOption(connection, CsvWorkTrackingOptionNames.DateTimeFormat, "yyyy-MM-dd");

            var portfolio = new Portfolio
            {
                Name = "A Portfolio from a spreadsheet",
                WorkTrackingSystemConnection = connection,
                DataRetrievalValue = File.ReadAllText("Services/Implementation/WorkTrackingConnectors/Csv/project-valid-required-only.csv"),
            };

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("Epic");
            portfolio.ToDoStates.Clear();
            portfolio.ToDoStates.Add("To Do");
            portfolio.DoingStates.Clear();
            portfolio.DoingStates.Add("In Progress");
            portfolio.DoneStates.Clear();
            portfolio.DoneStates.Add("Done");

            return portfolio;
        }

        private static void AdjustOption(WorkTrackingSystemConnection connection, string key, string value)
            => connection.Options.Single(option => option.Key == key).Value = value;
    }
}
