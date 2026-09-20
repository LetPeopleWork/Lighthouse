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
                    AdditionalFieldDefinitionId = 1,
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
                    AdditionalFieldDefinitionId = 1,
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
                    AdditionalFieldDefinitionId = 1,
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

        /// <summary>
        /// The format box takes free text, so "not empty" was never enough.
        ///
        /// Only a narrow class of text actually breaks: .NET copies characters it does not recognise through
        /// as literals, so most nonsense renders as nonsense rather than throwing. What throws is a lone
        /// character that is not a standard specifier, and a quote that is never closed. Those are the ones
        /// worth catching, because a format that throws cannot be handled by the write-back round at all -
        /// it is caught here instead, while the admin is still looking at the box they typed it into.
        /// </summary>
        [Test]
        [TestCase("x")]
        [TestCase("dd 'of MMM")]
        public void Validate_FormattedTextMapping_UnusableDateFormat_ReturnsInvalid(string dateFormat)
        {
            var mappings = new List<WriteBackMappingDefinition>
            {
                new()
                {
                    ValueSource = WriteBackValueSource.ForecastedStartPercentile85,
                    AppliesTo = WriteBackAppliesTo.Portfolio,
                    AdditionalFieldDefinitionId = 1,
                    TargetValueType = WriteBackTargetValueType.FormattedText,
                    DateFormat = dateFormat
                }
            };

            var result = WriteBackMappingValidator.Validate(mappings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors[0], Does.Contain(dateFormat));
            }
        }

        /// <summary>
        /// The formats the screen itself offers must all survive the check above, or the presets would be
        /// unsaveable.
        /// </summary>
        [Test]
        [TestCase("yyyy-MM-dd")]
        [TestCase("MM/dd/yyyy")]
        [TestCase("dd.MM.yyyy")]
        [TestCase("dd MMM yyyy")]
        [TestCase("dd 'of' MMM yyyy")]
        public void Validate_FormattedTextMapping_AFormatTheScreenOffers_ReturnsValid(string dateFormat)
        {
            var mappings = new List<WriteBackMappingDefinition>
            {
                new()
                {
                    ValueSource = WriteBackValueSource.ForecastedStartPercentile85,
                    AppliesTo = WriteBackAppliesTo.Portfolio,
                    AdditionalFieldDefinitionId = 1,
                    TargetValueType = WriteBackTargetValueType.FormattedText,
                    DateFormat = dateFormat
                }
            };

            var result = WriteBackMappingValidator.Validate(mappings);

            Assert.That(result.IsValid, Is.True);
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
                    AdditionalFieldDefinitionId = 1,
                    TargetValueType = WriteBackTargetValueType.FormattedText,
                    DateFormat = ""
                }
            };

            var result = WriteBackMappingValidator.Validate(mappings);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void Validate_MissingAdditionalFieldDefinitionId_ReturnsInvalid()
        {
            var mappings = new List<WriteBackMappingDefinition>
            {
                new()
                {
                    ValueSource = WriteBackValueSource.WorkItemAgeCycleTime,
                    AppliesTo = WriteBackAppliesTo.Team,
                    AdditionalFieldDefinitionId = null,
                }
            };

            var result = WriteBackMappingValidator.Validate(mappings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Count.EqualTo(1));
                Assert.That(result.Errors[0], Does.Contain("additional field"));
            }
        }

        [Test]
        public void Validate_ZeroAdditionalFieldDefinitionId_ReturnsInvalid()
        {
            var mappings = new List<WriteBackMappingDefinition>
            {
                new()
                {
                    ValueSource = WriteBackValueSource.WorkItemAgeCycleTime,
                    AppliesTo = WriteBackAppliesTo.Team,
                    AdditionalFieldDefinitionId = 0,
                }
            };

            var result = WriteBackMappingValidator.Validate(mappings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Errors, Has.Count.EqualTo(1));
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
                    AdditionalFieldDefinitionId = null,
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
        public void Validate_DuplicateAdditionalFieldDefinitionId_SameAppliesTo_ReturnsInvalid()
        {
            var mappings = new List<WriteBackMappingDefinition>
            {
                new()
                {
                    ValueSource = WriteBackValueSource.WorkItemAgeCycleTime,
                    AppliesTo = WriteBackAppliesTo.Team,
                    AdditionalFieldDefinitionId = 1,
                },
                new()
                {
                    ValueSource = WriteBackValueSource.FeatureSize,
                    AppliesTo = WriteBackAppliesTo.Team,
                    AdditionalFieldDefinitionId = 1,
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
        public void Validate_SameAdditionalFieldDefinitionId_DifferentAppliesTo_ReturnsValid()
        {
            var mappings = new List<WriteBackMappingDefinition>
            {
                new()
                {
                    ValueSource = WriteBackValueSource.WorkItemAgeCycleTime,
                    AppliesTo = WriteBackAppliesTo.Team,
                    AdditionalFieldDefinitionId = 1,
                },
                new()
                {
                    ValueSource = WriteBackValueSource.FeatureSize,
                    AppliesTo = WriteBackAppliesTo.Portfolio,
                    AdditionalFieldDefinitionId = 1,
                }
            };

            var result = WriteBackMappingValidator.Validate(mappings);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_NonForecastValueSource_IgnoresTargetValueTypeAndDateFormat()
        {
            var mappings = new List<WriteBackMappingDefinition>
            {
                new()
                {
                    ValueSource = WriteBackValueSource.WorkItemAgeCycleTime,
                    AppliesTo = WriteBackAppliesTo.Team,
                    AdditionalFieldDefinitionId = 1,
                    TargetValueType = WriteBackTargetValueType.FormattedText,
                    DateFormat = null
                }
            };

            var result = WriteBackMappingValidator.Validate(mappings);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        [TestCase(WriteBackValueSource.WorkItemAgeCycleTime)]
        [TestCase(WriteBackValueSource.FeatureSize)]
        public void Validate_NumericValueSource_AlwaysValidRegardlessOfDateSettings(WriteBackValueSource source)
        {
            var mappings = new List<WriteBackMappingDefinition>
            {
                new()
                {
                    ValueSource = source,
                    AppliesTo = WriteBackAppliesTo.Team,
                    AdditionalFieldDefinitionId = (int)source + 1,
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
                    ValueSource = WriteBackValueSource.WorkItemAgeCycleTime,
                    AppliesTo = WriteBackAppliesTo.Team,
                    AdditionalFieldDefinitionId = 1,
                },
                new()
                {
                    ValueSource = WriteBackValueSource.ForecastPercentile85,
                    AppliesTo = WriteBackAppliesTo.Portfolio,
                    AdditionalFieldDefinitionId = 2,
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
