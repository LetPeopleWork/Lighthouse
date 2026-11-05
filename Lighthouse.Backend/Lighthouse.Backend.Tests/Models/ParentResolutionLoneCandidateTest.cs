using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// A link entry's only candidate is judged like several are: a Team's items hang under Features, so a lone
    /// candidate that is no stored Feature hangs nowhere and lets the next source be read. Only a link naming
    /// nobody is not a question.
    /// </summary>
    [TestFixture]
    public class ParentResolutionLoneCandidateTest
    {
        private static readonly DateTime November2 = new(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc);

        private static StoredFeatures Holding(params string[] keys)
            => new(keys.ToDictionary(key => key, _ => (DateTime?)November2));

        private static ParentResolution LoneLinkTo(string key)
            => ParentResolution.From([new ParentCandidate(key, null)]);

        [Test]
        public void A_Teams_lone_candidate_that_is_a_stored_Feature_is_placed()
        {
            var placement = LoneLinkTo("PAY-7").Place(Holding("PAY-7"), ParentNarrowing.Narrow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(placement.Outcome, Is.EqualTo(ParentPlacementOutcome.Placed));
                Assert.That(placement.Key, Is.EqualTo("PAY-7"));
                Assert.That(placement.DecidedBy, Is.EqualTo(ParentPlacementRule.OnlyFeatureAmongLinks));
            }
        }

        [Test]
        public void A_Teams_lone_candidate_that_is_no_stored_Feature_is_not_placed()
        {
            var placement = LoneLinkTo("INC-88").Place(Holding("PAY-7"), ParentNarrowing.Narrow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(placement.Outcome, Is.EqualTo(ParentPlacementOutcome.NotPlaced),
                    "An incident is not somewhere a Work Item can hang, so the next listed source must be read instead.");
                Assert.That(placement.WhyNotPlaced, Is.EqualTo(ParentNotPlacedReason.NoneIsAStoredFeature));
                Assert.That(placement.Key, Is.Empty);
            }
        }

        [Test]
        public void A_Portfolios_lone_candidate_is_placed_without_being_narrowed_and_never_claims_to_be_the_only_Feature()
        {
            var placement = LoneLinkTo("PRG-2").Place(Holding(), ParentNarrowing.DoNotNarrow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(placement.Outcome, Is.EqualTo(ParentPlacementOutcome.Placed));
                Assert.That(placement.Key, Is.EqualTo("PRG-2"));
                Assert.That(placement.DecidedBy, Is.Not.EqualTo(ParentPlacementRule.OnlyFeatureAmongLinks),
                    "That rule says the only Feature among the links, which nothing checked for a Portfolio, and the log must not say it did.");
            }
        }

        [Test]
        public void A_link_naming_nobody_is_still_not_a_question()
        {
            var placement = ParentResolution.From(Array.Empty<ParentCandidate>()).Place(Holding("PAY-7"), ParentNarrowing.Narrow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(placement.Outcome, Is.EqualTo(ParentPlacementOutcome.NotAsked));
                Assert.That(placement.DecidedBy, Is.EqualTo(ParentPlacementRule.None));
                Assert.That(placement.Key, Is.Empty);
            }
        }
    }
}
