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
                ValueSource = WriteBackValueSource.WorkItemAgeCycleTime,
                AppliesTo = WriteBackAppliesTo.Team,
                AdditionalFieldDefinitionId = 10,
                TargetValueType = WriteBackTargetValueType.FormattedText,
                DateFormat = "yyyy-MM-dd"
            };

            var subject = new WriteBackMappingDefinitionDto(model);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.Id, Is.EqualTo(42));
                Assert.That(subject.ValueSource, Is.EqualTo(WriteBackValueSource.WorkItemAgeCycleTime));
                Assert.That(subject.AppliesTo, Is.EqualTo(WriteBackAppliesTo.Team));
                Assert.That(subject.AdditionalFieldDefinitionId, Is.EqualTo(10));
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
                AdditionalFieldDefinitionId = 20,
                TargetValueType = WriteBackTargetValueType.Date,
                DateFormat = null
            };

            var result = subject.ToModel();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Id, Is.EqualTo(42));
                Assert.That(result.ValueSource, Is.EqualTo(WriteBackValueSource.ForecastPercentile85));
                Assert.That(result.AppliesTo, Is.EqualTo(WriteBackAppliesTo.Portfolio));
                Assert.That(result.AdditionalFieldDefinitionId, Is.EqualTo(20));
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
                ValueSource = WriteBackValueSource.WorkItemAgeCycleTime,
                AppliesTo = WriteBackAppliesTo.Team,
                AdditionalFieldDefinitionId = 30
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
                ValueSource = WriteBackValueSource.WorkItemAgeCycleTime,
                AppliesTo = WriteBackAppliesTo.Team,
                AdditionalFieldDefinitionId = 40,
                TargetValueType = WriteBackTargetValueType.FormattedText,
                DateFormat = "dd/MM/yyyy"
            });

            var subject = new WorkTrackingSystemConnectionDto(connection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.WriteBackMappingDefinitions, Has.Count.EqualTo(1));
                Assert.That(subject.WriteBackMappingDefinitions[0].Id, Is.EqualTo(42));
                Assert.That(subject.WriteBackMappingDefinitions[0].ValueSource, Is.EqualTo(WriteBackValueSource.WorkItemAgeCycleTime));
                Assert.That(subject.WriteBackMappingDefinitions[0].AdditionalFieldDefinitionId, Is.EqualTo(40));
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
