using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models.WriteBack;

namespace Lighthouse.Backend.Tests.API.Helpers
{
    public class WriteBackMappingValidatorTest
    {
        [Test]
        public void Validate_EmptyList_ReturnsValid()
        {
            var mappings = new List<WriteBackMappingDefinition>();

            var result = WriteBackMappingValidator.Validate(mappings);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_ValidDateMapping_ReturnsValid()
        {
            var mappings = new List<WriteBackMappingDefinition>
            {
                new()
                {
                    ValueSource = WriteBackValueSource.ForecastPercentile85,
                    AppliesTo = WriteBackAppliesTo.Portfolio,
                    TargetFieldReference = "Custom.Forecast85",
                    TargetValueType = WriteBackTargetValueType.Date,
                    DateFormat = null
                }
            };

            var result = WriteBackMappingValidator.Validate(mappings);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_ValidFormattedTextMapping_WithDateFormat_ReturnsValid()
        {
            var mappings = new List<WriteBackMappingDefinition>
            {
                new()
                {
                    ValueSource = WriteBackValueSource.ForecastPercentile85,
                    AppliesTo = WriteBackAppliesTo.Portfolio,
                    TargetFieldReference = "Custom.Forecast85",
                    TargetValueType = WriteBackTargetValueType.FormattedText,
                    DateFormat = "yyyy-MM-dd"
                }
            };

            var result = WriteBackMappingValidator.Validate(mappings);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_FormattedTextMapping_MissingDateFormat_ReturnsInvalid()
        {
            var mappings = new List<WriteBackMappingDefinition>
            {
                new()
                {
                    ValueSource = WriteBackValueSource.ForecastPercentile85,
                    AppliesTo = WriteBackAppliesTo.Portfolio,
                    TargetFieldReference = "Custom.Forecast85",
                    TargetValueType = WriteBackTargetValueType.FormattedText,
                    DateFormat = null
                }
            };

            var result = WriteBackMappingValidator.Validate(mappings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.Errors[0], Does.Contain("DateFormat"));
            }
        }

        [Test]
        public void Validate_FormattedTextMapping_EmptyDateFormat_ReturnsInvalid()
        {
            var mappings = new List<WriteBackMappingDefinition>
            {
                new()
                {
                    ValueSource = WriteBackValueSource.ForecastPercentile50,
                    AppliesTo = WriteBackAppliesTo.Portfolio,
                    TargetFieldReference = "Custom.Forecast50",
                    TargetValueType = WriteBackTargetValueType.FormattedText,
                    DateFormat = ""
                }
            };

            var result = WriteBackMappingValidator.Validate(mappings);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void Validate_MissingTargetFieldReference_ReturnsInvalid()
        {
            var mappings = new List<WriteBackMappingDefinition>
            {
                new()
                {
                    ValueSource = WriteBackValueSource.WorkItemAge,
                    AppliesTo = WriteBackAppliesTo.Team,
                    TargetFieldReference = "",
                }
            };

            var result = WriteBackMappingValidator.Validate(mappings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.Errors[0], Does.Contain("TargetFieldReference"));
            }
        }

        [Test]
        public void Validate_MultipleErrors_ReturnsAllErrors()
        {
            var mappings = new List<WriteBackMappingDefinition>
            {
                new()
                {
                    ValueSource = WriteBackValueSource.ForecastPercentile85,
                    AppliesTo = WriteBackAppliesTo.Portfolio,
                    TargetFieldReference = "",
                    TargetValueType = WriteBackTargetValueType.FormattedText,
                    DateFormat = null
                }
            };

            var result = WriteBackMappingValidator.Validate(mappings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Count.EqualTo(2));
            }
        }

        [Test]
        public void Validate_DuplicateTargetFieldReference_ReturnsInvalid()
        {
            var mappings = new List<WriteBackMappingDefinition>
            {
                new()
                {
                    ValueSource = WriteBackValueSource.WorkItemAge,
                    AppliesTo = WriteBackAppliesTo.Team,
                    TargetFieldReference = "Custom.Field",
                },
                new()
                {
                    ValueSource = WriteBackValueSource.CycleTime,
                    AppliesTo = WriteBackAppliesTo.Team,
                    TargetFieldReference = "Custom.Field",
                }
            };

            var result = WriteBackMappingValidator.Validate(mappings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors.Any(e => e.Contains("duplicate", StringComparison.OrdinalIgnoreCase)), Is.True);
            }
        }

        [Test]
        public void Validate_NonForecastValueSource_IgnoresTargetValueTypeAndDateFormat()
        {
            var mappings = new List<WriteBackMappingDefinition>
            {
                new()
                {
                    ValueSource = WriteBackValueSource.WorkItemAge,
                    AppliesTo = WriteBackAppliesTo.Team,
                    TargetFieldReference = "Custom.WorkItemAge",
                    TargetValueType = WriteBackTargetValueType.FormattedText,
                    DateFormat = null
                }
            };

            var result = WriteBackMappingValidator.Validate(mappings);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        [TestCase(WriteBackValueSource.WorkItemAge)]
        [TestCase(WriteBackValueSource.CycleTime)]
        [TestCase(WriteBackValueSource.FeatureSize)]
        public void Validate_NumericValueSource_AlwaysValidRegardlessOfDateSettings(WriteBackValueSource source)
        {
            var mappings = new List<WriteBackMappingDefinition>
            {
                new()
                {
                    ValueSource = source,
                    AppliesTo = WriteBackAppliesTo.Team,
                    TargetFieldReference = $"Custom.{source}",
                    TargetValueType = WriteBackTargetValueType.FormattedText,
                    DateFormat = ""
                }
            };

            var result = WriteBackMappingValidator.Validate(mappings);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_MultipleMappings_MixedValidity_ReportsOnlyInvalid()
        {
            var mappings = new List<WriteBackMappingDefinition>
            {
                new()
                {
                    ValueSource = WriteBackValueSource.WorkItemAge,
                    AppliesTo = WriteBackAppliesTo.Team,
                    TargetFieldReference = "Custom.WorkItemAge",
                },
                new()
                {
                    ValueSource = WriteBackValueSource.ForecastPercentile85,
                    AppliesTo = WriteBackAppliesTo.Portfolio,
                    TargetFieldReference = "Custom.Forecast",
                    TargetValueType = WriteBackTargetValueType.FormattedText,
                    DateFormat = null
                }
            };

            var result = WriteBackMappingValidator.Validate(mappings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Count.EqualTo(1));
            }
        }
    }
}
