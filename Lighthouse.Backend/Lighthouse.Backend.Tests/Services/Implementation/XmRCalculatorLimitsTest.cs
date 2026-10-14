using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class XmRCalculatorLimitsTest
    {
        private const int RandomSeed = 5878;
        private const int Runs = 500;

        [Test]
        public void Calculate_is_Limits_rounded_at_its_own_boundary()
        {
            var random = new Random(RandomSeed);

            for (var run = 0; run < Runs; run++)
            {
                var values = RandomValues(random);

                var limits = XmRCalculator.Limits(values);
                var result = XmRCalculator.Calculate(values, values);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(result.Average, Is.EqualTo((int)Math.Round(limits.Average)), Describe(values));
                    Assert.That(result.UpperNaturalProcessLimit, Is.EqualTo((int)Math.Round(limits.Upper)), Describe(values));
                    Assert.That(result.LowerNaturalProcessLimit, Is.EqualTo((int)Math.Round(limits.Lower)), Describe(values));
                    Assert.That(limits.PointCount, Is.EqualTo(values.Length), Describe(values));
                    Assert.That(limits.Lower, Is.GreaterThanOrEqualTo(0.0), Describe(values));
                    Assert.That(limits.Upper, Is.GreaterThanOrEqualTo(limits.Average), Describe(values));
                    Assert.That(limits.LowerClamped ? limits.Lower : 0.0, Is.Zero, Describe(values));
                }
            }
        }

        [Test]
        public void Limits_keeps_a_fractional_average_that_Calculate_rounds_away()
        {
            int[] values = [0, 1, 1, 0, 1];

            var limits = XmRCalculator.Limits(values);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(limits.Average, Is.EqualTo(0.6).Within(1e-9));
                Assert.That(XmRCalculator.Calculate(values, values).Average, Is.EqualTo(1));
            }
        }

        private static int[] RandomValues(Random random)
        {
            var length = random.Next(0, 40);
            var ceiling = random.Next(1, 60);
            var values = new int[length];
            for (var i = 0; i < length; i++)
            {
                values[i] = random.Next(0, ceiling);
            }

            return values;
        }

        private static string Describe(int[] values) => $"values: [{string.Join(", ", values)}]";
    }
}
