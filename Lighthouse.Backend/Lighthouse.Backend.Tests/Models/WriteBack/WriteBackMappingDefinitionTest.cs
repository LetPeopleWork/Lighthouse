using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;

namespace Lighthouse.Backend.Tests.Models.WriteBack
{
    public class WriteBackMappingDefinitionTest
    {
        [Test]
        public void DefaultTargetValueType_IsDate()
        {
            var subject = new WriteBackMappingDefinition();

            Assert.That(subject.TargetValueType, Is.EqualTo(WriteBackTargetValueType.Date));
        }

        [Test]
        public void DateFormat_IsNullByDefault()
        {
            var subject = new WriteBackMappingDefinition();

            Assert.That(subject.DateFormat, Is.Null);
        }

        [Test]
        public void TargetFieldReference_IsEmptyByDefault()
        {
            var subject = new WriteBackMappingDefinition();

            Assert.That(subject.TargetFieldReference, Is.EqualTo(string.Empty));
        }

        [Test]
        public void ValueSource_CanBeSet()
        {
            var subject = new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.CycleTime
            };

            Assert.That(subject.ValueSource, Is.EqualTo(WriteBackValueSource.CycleTime));
        }

        [Test]
        public void AppliesTo_CanBeSet()
        {
            var subject = new WriteBackMappingDefinition
            {
                AppliesTo = WriteBackAppliesTo.Portfolio
            };

            Assert.That(subject.AppliesTo, Is.EqualTo(WriteBackAppliesTo.Portfolio));
        }

        [Test]
        public void StableId_RemainsUnchanged_AfterPropertyEdits()
        {
            var subject = new WriteBackMappingDefinition
            {
                Id = 42,
                ValueSource = WriteBackValueSource.WorkItemAge,
                TargetFieldReference = "original.field"
            };

            var originalStableId = subject.Id;

            subject.ValueSource = WriteBackValueSource.CycleTime;
            subject.TargetFieldReference = "changed.field";

            Assert.That(subject.Id, Is.EqualTo(originalStableId));
        }
    }

    public class WorkTrackingSystemConnectionWriteBackTest
    {
        [Test]
        public void WriteBackMappingDefinitions_EmptyByDefault()
        {
            var subject = new WorkTrackingSystemConnection();

            Assert.That(subject.WriteBackMappingDefinitions, Is.Empty);
        }

        [Test]
        public void WriteBackMappingDefinitions_CanAddMappings()
        {
            var subject = new WorkTrackingSystemConnection();
            var mapping = new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.WorkItemAge,
                AppliesTo = WriteBackAppliesTo.Team,
                TargetFieldReference = "Custom.WorkItemAge"
            };

            subject.WriteBackMappingDefinitions.Add(mapping);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.WriteBackMappingDefinitions, Has.Count.EqualTo(1));
                Assert.That(subject.WriteBackMappingDefinitions[0].ValueSource, Is.EqualTo(WriteBackValueSource.WorkItemAge));
                Assert.That(subject.WriteBackMappingDefinitions[0].AppliesTo, Is.EqualTo(WriteBackAppliesTo.Team));
                Assert.That(subject.WriteBackMappingDefinitions[0].TargetFieldReference, Is.EqualTo("Custom.WorkItemAge"));
            }
        }

        [Test]
        public void WriteBackMappingDefinitions_MappingIdIsStable_AfterValueSourceEdit()
        {
            var subject = new WorkTrackingSystemConnection();
            var mapping = new WriteBackMappingDefinition
            {
                Id = 42,
                ValueSource = WriteBackValueSource.WorkItemAge,
                TargetFieldReference = "Custom.WorkItemAge"
            };
            subject.WriteBackMappingDefinitions.Add(mapping);

            subject.WriteBackMappingDefinitions[0].ValueSource = WriteBackValueSource.CycleTime;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.WriteBackMappingDefinitions[0].Id, Is.EqualTo(42));
            }
        }

        [Test]
        public void WriteBackMappingDefinitions_AreSeparateFromAdditionalFieldDefinitions()
        {
            var subject = new WorkTrackingSystemConnection();

            subject.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
            {
                DisplayName = "Iteration Path",
                Reference = "System.IterationPath"
            });

            subject.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.WorkItemAge,
                TargetFieldReference = "Custom.WorkItemAge"
            });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.AdditionalFieldDefinitions, Has.Count.EqualTo(1));
                Assert.That(subject.WriteBackMappingDefinitions, Has.Count.EqualTo(1));
            }
        }
    }
}
