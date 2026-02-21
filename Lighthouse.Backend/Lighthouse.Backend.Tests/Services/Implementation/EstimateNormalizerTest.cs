using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class EstimateNormalizerTest
    {
        // --- Numeric mode: parse numeric strings to double values ---

        [Test]
        public void Normalize_NumericMode_ParsesInteger()
        {
            var result = EstimateNormalizer.Normalize("5", useNonNumeric: false, []);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(EstimateNormalizationStatus.Mapped));
                Assert.That(result.NumericValue, Is.EqualTo(5.0));
                Assert.That(result.DisplayValue, Is.EqualTo("5"));
            }
        }

        [Test]
        public void Normalize_NumericMode_ParsesDecimal()
        {
            var result = EstimateNormalizer.Normalize("3.5", useNonNumeric: false, []);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(EstimateNormalizationStatus.Mapped));
                Assert.That(result.NumericValue, Is.EqualTo(3.5));
                Assert.That(result.DisplayValue, Is.EqualTo("3.5"));
            }
        }

        [Test]
        public void Normalize_NumericMode_PreservesDecimalPrecision()
        {
            var result = EstimateNormalizer.Normalize("0.25", useNonNumeric: false, []);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(EstimateNormalizationStatus.Mapped));
                Assert.That(result.NumericValue, Is.EqualTo(0.25));
            }
        }

        [Test]
        public void Normalize_NumericMode_ParsesZero()
        {
            var result = EstimateNormalizer.Normalize("0", useNonNumeric: false, []);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(EstimateNormalizationStatus.Mapped));
                Assert.That(result.NumericValue, Is.Zero);
            }
        }

        [Test]
        public void Normalize_NumericMode_NonNumericString_ReturnsInvalid()
        {
            var result = EstimateNormalizer.Normalize("XL", useNonNumeric: false, []);

            Assert.That(result.Status, Is.EqualTo(EstimateNormalizationStatus.Invalid));
        }

        [Test]
        public void Normalize_NumericMode_NullEstimate_ReturnsInvalid()
        {
            var result = EstimateNormalizer.Normalize(null, useNonNumeric: false, []);

            Assert.That(result.Status, Is.EqualTo(EstimateNormalizationStatus.Invalid));
        }

        [Test]
        public void Normalize_NumericMode_EmptyString_ReturnsInvalid()
        {
            var result = EstimateNormalizer.Normalize("", useNonNumeric: false, []);

            Assert.That(result.Status, Is.EqualTo(EstimateNormalizationStatus.Invalid));
        }

        [Test]
        public void Normalize_NumericMode_WhitespaceString_ReturnsInvalid()
        {
            var result = EstimateNormalizer.Normalize("  ", useNonNumeric: false, []);

            Assert.That(result.Status, Is.EqualTo(EstimateNormalizationStatus.Invalid));
        }

        [Test]
        public void Normalize_NumericMode_NegativeNumber_Parses()
        {
            var result = EstimateNormalizer.Normalize("-1", useNonNumeric: false, []);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(EstimateNormalizationStatus.Mapped));
                Assert.That(result.NumericValue, Is.EqualTo(-1.0));
            }
        }

        // --- Non-numeric mode: categorical-to-ordinal mapping ---

        [Test]
        public void Normalize_NonNumericMode_MapsToOrdinalPosition()
        {
            var categories = new List<string> { "XS", "S", "M", "L", "XL" };

            var result = EstimateNormalizer.Normalize("M", useNonNumeric: true, categories);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(EstimateNormalizationStatus.Mapped));
                Assert.That(result.NumericValue, Is.EqualTo(2.0));
                Assert.That(result.DisplayValue, Is.EqualTo("M"));
            }
        }

        [Test]
        public void Normalize_NonNumericMode_FirstCategory_MapsToZero()
        {
            var categories = new List<string> { "XS", "S", "M", "L", "XL" };

            var result = EstimateNormalizer.Normalize("XS", useNonNumeric: true, categories);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(EstimateNormalizationStatus.Mapped));
                Assert.That(result.NumericValue, Is.Zero);
            }
        }

        [Test]
        public void Normalize_NonNumericMode_LastCategory_MapsToLastIndex()
        {
            var categories = new List<string> { "XS", "S", "M", "L", "XL" };

            var result = EstimateNormalizer.Normalize("XL", useNonNumeric: true, categories);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(EstimateNormalizationStatus.Mapped));
                Assert.That(result.NumericValue, Is.EqualTo(4.0));
            }
        }

        [Test]
        public void Normalize_NonNumericMode_UnmappedValue_ReturnsUnmapped()
        {
            var categories = new List<string> { "XS", "S", "M", "L", "XL" };

            var result = EstimateNormalizer.Normalize("XXL", useNonNumeric: true, categories);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(EstimateNormalizationStatus.Unmapped));
                Assert.That(result.DisplayValue, Is.EqualTo("XXL"));
            }
        }

        [Test]
        public void Normalize_NonNumericMode_NullEstimate_ReturnsInvalid()
        {
            var categories = new List<string> { "S", "M", "L" };

            var result = EstimateNormalizer.Normalize(null, useNonNumeric: true, categories);

            Assert.That(result.Status, Is.EqualTo(EstimateNormalizationStatus.Invalid));
        }

        [Test]
        public void Normalize_NonNumericMode_EmptyString_ReturnsInvalid()
        {
            var categories = new List<string> { "S", "M", "L" };

            var result = EstimateNormalizer.Normalize("", useNonNumeric: true, categories);

            Assert.That(result.Status, Is.EqualTo(EstimateNormalizationStatus.Invalid));
        }

        [Test]
        public void Normalize_NonNumericMode_EmptyCategoryList_ReturnsUnmapped()
        {
            var result = EstimateNormalizer.Normalize("M", useNonNumeric: true, []);

            Assert.That(result.Status, Is.EqualTo(EstimateNormalizationStatus.Unmapped));
        }

        [Test]
        public void Normalize_NonNumericMode_CaseInsensitiveMatch()
        {
            var categories = new List<string> { "Small", "Medium", "Large" };

            var result = EstimateNormalizer.Normalize("medium", useNonNumeric: true, categories);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(EstimateNormalizationStatus.Mapped));
                Assert.That(result.NumericValue, Is.EqualTo(1.0));
                Assert.That(result.DisplayValue, Is.EqualTo("Medium"));
            }
        }

        [Test]
        public void Normalize_NonNumericMode_TrimsWhitespace()
        {
            var categories = new List<string> { "S", "M", "L" };

            var result = EstimateNormalizer.Normalize("  M  ", useNonNumeric: true, categories);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(EstimateNormalizationStatus.Mapped));
                Assert.That(result.NumericValue, Is.EqualTo(1.0));
            }
        }

        // --- NormalizeBatch: batch normalization with diagnostics ---

        [Test]
        public void NormalizeBatch_NumericMode_ReturnsDiagnosticCounts()
        {
            var estimates = new[] { "1", "2", "abc", null, "5" };

            var result = EstimateNormalizer.NormalizeBatch(estimates, useNonNumeric: false, []);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.TotalCount, Is.EqualTo(5));
                Assert.That(result.MappedCount, Is.EqualTo(3));
                Assert.That(result.UnmappedCount, Is.Zero);
                Assert.That(result.InvalidCount, Is.EqualTo(2));
                Assert.That(result.Results, Has.Count.EqualTo(5));
            }
        }

        [Test]
        public void NormalizeBatch_NonNumericMode_ReturnsDiagnosticCounts()
        {
            var categories = new List<string> { "S", "M", "L" };
            var estimates = new[] { "S", "M", "XXL", null, "L" };

            var result = EstimateNormalizer.NormalizeBatch(estimates, useNonNumeric: true, categories);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.TotalCount, Is.EqualTo(5));
                Assert.That(result.MappedCount, Is.EqualTo(3));
                Assert.That(result.UnmappedCount, Is.EqualTo(1));
                Assert.That(result.InvalidCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void NormalizeBatch_EmptyInput_ReturnsZeroCounts()
        {
            var result = EstimateNormalizer.NormalizeBatch([], useNonNumeric: false, []);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.TotalCount, Is.Zero);
                Assert.That(result.MappedCount, Is.Zero);
                Assert.That(result.UnmappedCount, Is.Zero);
                Assert.That(result.InvalidCount, Is.Zero);
                Assert.That(result.Results, Is.Empty);
            }
        }

        [Test]
        public void NormalizeBatch_PreservesOrderOfResults()
        {
            var categories = new List<string> { "S", "M", "L" };
            var estimates = new[] { "L", "S", "M" };

            var result = EstimateNormalizer.NormalizeBatch(estimates, useNonNumeric: true, categories);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Results[0].NumericValue, Is.EqualTo(2.0));
                Assert.That(result.Results[1].NumericValue, Is.Zero);
                Assert.That(result.Results[2].NumericValue, Is.EqualTo(1.0));
            }
        }
    }
}
