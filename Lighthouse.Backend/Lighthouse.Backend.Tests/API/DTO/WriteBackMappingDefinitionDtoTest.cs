using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class WriteBackMappingDefinitionDtoTest
    {
        [Test]
        public void Constructor_FromModel_MapsAllProperties()
        {
            var model = new WriteBackMappingDefinition
            {
                Id = 42,
                ValueSource = WriteBackValueSource.CycleTime,
                AppliesTo = WriteBackAppliesTo.Team,
                TargetFieldReference = "Custom.CycleTime",
                TargetValueType = WriteBackTargetValueType.FormattedText,
                DateFormat = "yyyy-MM-dd"
            };

            var subject = new WriteBackMappingDefinitionDto(model);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.Id, Is.EqualTo(42));
                Assert.That(subject.ValueSource, Is.EqualTo(WriteBackValueSource.CycleTime));
                Assert.That(subject.AppliesTo, Is.EqualTo(WriteBackAppliesTo.Team));
                Assert.That(subject.TargetFieldReference, Is.EqualTo("Custom.CycleTime"));
                Assert.That(subject.TargetValueType, Is.EqualTo(WriteBackTargetValueType.FormattedText));
                Assert.That(subject.DateFormat, Is.EqualTo("yyyy-MM-dd"));
            }
        }

        [Test]
        public void ToModel_MapsAllProperties()
        {
            var subject = new WriteBackMappingDefinitionDto
            {
                Id = 42,
                ValueSource = WriteBackValueSource.ForecastPercentile85,
                AppliesTo = WriteBackAppliesTo.Portfolio,
                TargetFieldReference = "Custom.Forecast85",
                TargetValueType = WriteBackTargetValueType.Date,
                DateFormat = null
            };

            var result = subject.ToModel();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Id, Is.EqualTo(42));
                Assert.That(result.ValueSource, Is.EqualTo(WriteBackValueSource.ForecastPercentile85));
                Assert.That(result.AppliesTo, Is.EqualTo(WriteBackAppliesTo.Portfolio));
                Assert.That(result.TargetFieldReference, Is.EqualTo("Custom.Forecast85"));
                Assert.That(result.TargetValueType, Is.EqualTo(WriteBackTargetValueType.Date));
                Assert.That(result.DateFormat, Is.Null);
            }
        }

        [Test]
        public void ToModel_NegativeId_ResetsToZero()
        {
            var subject = new WriteBackMappingDefinitionDto
            {
                Id = -1,
                ValueSource = WriteBackValueSource.WorkItemAge,
                AppliesTo = WriteBackAppliesTo.Team,
                TargetFieldReference = "Custom.Age"
            };

            var result = subject.ToModel();

            Assert.That(result.Id, Is.Zero);
        }
    }

    public class WorkTrackingSystemConnectionDtoWriteBackTest
    {
        [Test]
        public void Create_SetsWriteBackMappingDefinitions()
        {
            var connection = new WorkTrackingSystemConnection();
            connection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                Id = 42,
                ValueSource = WriteBackValueSource.WorkItemAge,
                AppliesTo = WriteBackAppliesTo.Team,
                TargetFieldReference = "Custom.WorkItemAge",
                TargetValueType = WriteBackTargetValueType.FormattedText,
                DateFormat = "dd/MM/yyyy"
            });

            var subject = new WorkTrackingSystemConnectionDto(connection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.WriteBackMappingDefinitions, Has.Count.EqualTo(1));
                Assert.That(subject.WriteBackMappingDefinitions[0].Id, Is.EqualTo(42));
                Assert.That(subject.WriteBackMappingDefinitions[0].ValueSource, Is.EqualTo(WriteBackValueSource.WorkItemAge));
                Assert.That(subject.WriteBackMappingDefinitions[0].TargetFieldReference, Is.EqualTo("Custom.WorkItemAge"));
            }
        }

        [Test]
        public void Create_NoWriteBackMappings_EmptyList()
        {
            var connection = new WorkTrackingSystemConnection();

            var subject = new WorkTrackingSystemConnectionDto(connection);

            Assert.That(subject.WriteBackMappingDefinitions, Is.Empty);
        }
    }
}
