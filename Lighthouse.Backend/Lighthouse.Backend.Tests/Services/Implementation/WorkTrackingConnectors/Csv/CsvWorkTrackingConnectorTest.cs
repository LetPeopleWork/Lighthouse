using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Csv;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Csv
{
    public class CsvWorkTrackingConnectorTest
    {
        [Test]
        [TestCase("empty-file.txt", false)]
        [TestCase("invalid-missing-required.csv", false)]
        [TestCase("invalid-not-csv.csv", false)]
        [TestCase("invalid-wrong-csv.csv", false)]
        [TestCase("valid-all-optional.csv", true)]
        [TestCase("valid-required-only.csv", true)]
        [TestCase("valid-with-optional.csv", true)]
        public async Task Validate_ChecksIfValidCsv(string csvFileName, bool expectedResult)
        {
            var subject = CreateSubject();
            var team = new Team
            {
                WorkItemQuery = LoadCsvFile(csvFileName),
            };

            var isValid = await subject.ValidateTeamSettings(team);

            Assert.That(isValid, Is.EqualTo(expectedResult));
        }

        private string LoadCsvFile(string csvFileName)
        {
            var csvFileContent = File.ReadAllText($"Services/Implementation/WorkTrackingConnectors/Csv/{csvFileName}");
            return csvFileContent;
        }

        private CsvWorkTrackingConnector CreateSubject()
        {
            return new CsvWorkTrackingConnector(Mock.Of<ILogger<CsvWorkTrackingConnector>>());
        }
    }
}
