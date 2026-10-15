using Lighthouse.Backend.Models;
using Lighthouse.Backend.Tests.API.Integration.OnlyFeaturesAsParentCandidates;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// The pick that places a record whose links name several issues, asked directly. It reads nothing and
    /// writes nothing, so its rule can be held to properties over many inputs here, while the acceptance
    /// scenarios prove a refresh actually asks it.
    ///
    /// The properties are checked over every ordering of a candidate set, and over a fixed, seeded spread
    /// of generated sets, so a failure names an input that can be replayed.
    /// </summary>
    [TestFixture]
    [Category("story-6301-only-features-as-parent-candidates")]
    public class ParentResolutionPlacementTest
    {
        private const string AFeature = "PAY-88";

        private const string AnotherFeature = "PAY-91";

        private const string AnIncident = "INC-5531";

        private const string AnotherIncident = "INC-5602";

        private static readonly DateTime March4 = new(2026, 3, 4, 9, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime June17 = new(2026, 6, 17, 9, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime November2 = new(2025, 11, 2, 9, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime January15 = new(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc);

        private static readonly ParentCandidate[] NoCandidates = [];

        private static readonly ParentNarrowing[] BothNarrowings = [ParentNarrowing.Narrow, ParentNarrowing.DoNotNarrow];

        private static readonly string[] ThreeLinkedKeys = [AnotherFeature, AnIncident, AFeature];

        // ----- Narrowing to the Features this instance holds ---------------------------------------

        [Test]
        [Category("slice-01")]
        [Category("edge")]
        public void One_candidate_is_not_a_question_for_the_pick()
        {
            var lone = ParentResolution.From([Undated(AnIncident)]).Place(Holding((AFeature, November2)), ParentNarrowing.Narrow);
            var none = ParentResolution.From(NoCandidates).Place(Holding((AFeature, November2)), ParentNarrowing.Narrow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(lone.Outcome, Is.EqualTo(ParentPlacementOutcome.NotAsked),
                    "A lone link is stored as it is, Feature or not, so rules written against that value keep matching.");
                Assert.That(none.Outcome, Is.EqualTo(ParentPlacementOutcome.NotAsked),
                    "No link names nothing to choose from.");

                foreach (var notAsked in new[] { lone, none })
                {
                    Assert.That(notAsked.DecidedBy, Is.EqualTo(ParentPlacementRule.None),
                        "No rule settled a question that was never asked.");
                    Assert.That(notAsked.Key, Is.Empty,
                        "The pick gives no key when it was not asked; the lone link is stored by the caller as it is.");
                    Assert.That(notAsked.SetAside, Is.Empty,
                        "Nothing is set aside when there was nothing to choose between.");
                }
            }
        }

        [Test]
        [Category("slice-01")]
        public void Narrowing_to_the_one_stored_Feature_places_the_record_under_it()
        {
            var placement = ParentResolution.From([Undated(AnIncident), Undated(AFeature)])
                .Place(Holding((AFeature, November2)), ParentNarrowing.Narrow);

            Assert.That(Summary(placement), Is.EqualTo($"Placed {AFeature} by OnlyFeatureAmongLinks, set aside {AnIncident}"),
                "The incident is a cause, not a place to hang work, and it is named as set aside so the line can say so.");
        }

        [Test]
        [Category("slice-01")]
        [Category("error")]
        public void No_stored_Feature_among_the_candidates_places_nothing_and_says_why()
        {
            var placement = ParentResolution.From([Undated(AnIncident), Undated(AnotherIncident)])
                .Place(Holding((AFeature, November2)), ParentNarrowing.Narrow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(placement.Outcome, Is.EqualTo(ParentPlacementOutcome.NotPlaced),
                    "Neither candidate is somewhere a Team's Work Item can hang.");
                Assert.That(placement.WhyNotPlaced, Is.EqualTo(ParentNotPlacedReason.NoneIsAStoredFeature),
                    "The reason is what tells the administrator the links are not wrong, only not Features Lighthouse holds.");
                Assert.That(placement.Key, Is.Empty,
                    "A record that was not placed gets no key from the pick.");
            }
        }

        /// <summary>
        /// Two readings of the same links are the same answer, whatever order Jira listed them in. Comparing
        /// in listing order would make an unchanged item look changed on every refresh that reorders.
        /// </summary>
        [Test]
        [Category("slice-01")]
        [Category("property")]
        public void Two_readings_of_the_same_links_in_another_order_are_the_same_answer()
        {
            var asListed = ParentResolution.From(ThreeLinkedKeys);

            var differing = EveryOrderOf(ThreeLinkedKeys)
                .Select(order => ParentResolution.From(order))
                .Where(reading => !reading.Equals(asListed) || reading.GetHashCode() != asListed.GetHashCode())
                .Select(reading => string.Join(",", reading.Candidates))
                .ToList();

            Assert.That(differing, Is.Empty,
                "Every ordering of the same candidates has to be the same answer, hash included, or an unchanged item looks changed.");
        }

        [TestCaseSource(nameof(SetsNarrowingAloneDecides))]
        [Category("slice-01")]
        [Category("property")]
        public void Every_order_of_the_same_candidates_gives_the_same_placement_when_narrowing_decides(ParentCandidate[] candidates, StoredFeatures stored)
        {
            var placements = EveryOrderOf(candidates)
                .Select(order => Summary(ParentResolution.From(order).Place(stored, ParentNarrowing.Narrow)))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            Assert.That(placements, Has.Count.EqualTo(1),
                $"Every ordering of the same links has to give one answer. They gave: {string.Join(" | ", placements)}");
        }

        // ----- The oldest link, then the oldest Feature, then the lowest key -------------------------

        [Test]
        [Category("slice-02")]
        public void The_oldest_dated_link_wins()
        {
            var placement = ParentResolution.From([LinkedOn(AnotherFeature, June17), LinkedOn(AFeature, March4)])
                .Place(Holding((AFeature, January15), (AnotherFeature, November2)), ParentNarrowing.Narrow);

            Assert.That(Summary(placement), Is.EqualTo($"Placed {AFeature} by OldestLink, set aside {AnotherFeature}@2026-06-17"),
                "The link date decides before the Features' own ages, even when the losing Feature is the older one.");
        }

        [Test]
        [Category("slice-02")]
        [Category("edge")]
        public void A_dated_link_beats_an_undated_one()
        {
            var placement = ParentResolution.From([Undated(AFeature), LinkedOn(AnotherFeature, June17)])
                .Place(Holding((AFeature, November2), (AnotherFeature, January15)), ParentNarrowing.Narrow);

            Assert.That(Summary(placement), Is.EqualTo($"Placed {AnotherFeature} by OldestLink, set aside {AFeature}"),
                "An undated link was made at some unknown time, so only the dated one can be called the older link.");
        }

        [TestCaseSource(nameof(UndatedLinksDecidedByCreatedDate))]
        [Category("slice-02")]
        [Category("edge")]
        public void Among_undated_links_the_earliest_known_created_date_wins(DateTime? createdOfTheFirst, DateTime? createdOfTheSecond, string expectedKey, ParentPlacementRule expectedRule)
        {
            var placement = ParentResolution.From([Undated(AFeature), Undated(AnotherFeature)])
                .Place(Holding((AFeature, createdOfTheFirst), (AnotherFeature, createdOfTheSecond)), ParentNarrowing.Narrow);

            Assert.That((placement.Key, placement.DecidedBy), Is.EqualTo((expectedKey, expectedRule)),
                "A missing created date, or the empty one Jira sends for some migrated issues, is unknown. Read as the earliest date there is, it would always win.");
        }

        /// <summary>
        /// Two links made at the same instant, as a bulk edit makes them, are tied on the link date, so the
        /// line must name the rule that broke the tie rather than credit the oldest link with a choice it
        /// did not make.
        /// </summary>
        [TestCaseSource(nameof(LinksMadeAtTheSameInstant))]
        [Category("slice-02")]
        [Category("edge")]
        public void Links_made_at_the_same_instant_are_decided_by_the_next_rule(
            DateTime? createdOfTheFirst, DateTime? createdOfTheSecond, string expectedKey, ParentPlacementRule expectedRule, DateTime? expectedDecidedOn)
        {
            var placement = ParentResolution.From([LinkedOn(AFeature, March4), LinkedOn(AnotherFeature, March4)])
                .Place(Holding((AFeature, createdOfTheFirst), (AnotherFeature, createdOfTheSecond)), ParentNarrowing.Narrow);

            Assert.That((placement.Key, placement.DecidedBy, placement.DecidedOn), Is.EqualTo((expectedKey, expectedRule, expectedDecidedOn)),
                "The link dates tie, so the created dates decide when they differ and the key when they do not.");
        }

        [Test]
        [Category("slice-02")]
        [Category("edge")]
        public void When_everything_else_ties_the_lowest_key_wins_compared_character_by_character()
        {
            var placement = ParentResolution.From([Undated("OPS-9"), Undated("OPS-12")])
                .Place(Holding(("OPS-9", November2), ("OPS-12", November2)), ParentNarrowing.Narrow);

            Assert.That(Summary(placement), Is.EqualTo("Placed OPS-12 by LowestKey, set aside OPS-9"),
                "Comparing keys character by character gives the same answer on every machine. OPS-12 sorts before OPS-9 that way.");
        }

        [Test]
        [Category("slice-02")]
        public void Without_narrowing_nothing_is_set_aside_first_and_the_pick_always_ends_somewhere()
        {
            var placement = ParentResolution.From([LinkedOn("PRB-130", March4), LinkedOn("PAY-8", June17)])
                .Place(Holding(), ParentNarrowing.DoNotNarrow);

            Assert.That(Summary(placement), Is.EqualTo("Placed PRB-130 by OldestLink, set aside PAY-8@2026-06-17"),
                "The level above a Feature is never one of the Portfolio's own Features, so nothing can be set aside for not being one.");
        }

        [TestCaseSource(nameof(SetsTheDatesAndKeysDecide))]
        [Category("slice-02")]
        [Category("property")]
        [Category("kpi")]
        public void Every_order_of_the_same_candidates_gives_the_same_placement(ParentCandidate[] candidates, StoredFeatures stored)
        {
            var placements = EveryOrderOf(candidates)
                .SelectMany(order => BothNarrowings.Select(narrowing => $"{narrowing}: {Summary(ParentResolution.From(order).Place(stored, narrowing))}"))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            Assert.That(placements, Has.Count.EqualTo(2),
                $"Each kind of narrowing has to give one answer across every ordering of the same links. They gave: {string.Join(" | ", placements)}");
        }

        /// <summary>
        /// Over a seeded spread of generated sets: a record with two or more candidates is placed or not
        /// placed, a placement names one of its candidates and sets aside exactly the others, only narrowing
        /// can refuse, and it refuses only when no candidate is a stored Feature.
        /// </summary>
        [Test]
        [Category("slice-02")]
        [Category("property")]
        [Category("kpi")]
        public void Every_record_with_two_or_more_candidates_is_either_placed_among_them_or_refused_for_holding_no_Feature()
        {
            var violations = new List<string>();
            var random = new Random(6301);

            for (var sample = 0; sample < 400; sample++)
            {
                var (candidates, stored, storedKeys) = AGeneratedSet(random);

                foreach (var narrowing in BothNarrowings)
                {
                    var placement = ParentResolution.From(candidates).Place(stored, narrowing);
                    violations.AddRange(WhatThisPlacementBreaks(sample, candidates, storedKeys, narrowing, placement));
                }
            }

            Assert.That(violations, Is.Empty, "The placement broke its contract for these generated sets.");
        }

        // ----- Inputs ------------------------------------------------------------------------------------------

        private static readonly object[] SetsNarrowingAloneDecides =
        [
            new object[] { new[] { Undated(AnIncident), Undated(AFeature), Undated(AnotherIncident) }, Holding((AFeature, November2)) },
            new object[] { new[] { Undated(AnIncident), Undated(AnotherIncident), Undated("INC-5610") }, Holding((AFeature, November2)) },
        ];

        private static readonly object[] UndatedLinksDecidedByCreatedDate =
        [
            new object?[] { November2, January15, AFeature, ParentPlacementRule.EarliestCreated },
            new object?[] { January15, November2, AnotherFeature, ParentPlacementRule.EarliestCreated },
            new object?[] { null, January15, AnotherFeature, ParentPlacementRule.EarliestCreated },
            new object?[] { DateTime.MinValue, January15, AnotherFeature, ParentPlacementRule.EarliestCreated },
            new object?[] { null, DateTime.MinValue, AFeature, ParentPlacementRule.LowestKey },
            new object?[] { November2, November2, AFeature, ParentPlacementRule.LowestKey },
        ];

        private static readonly object[] LinksMadeAtTheSameInstant =
        [
            new object?[] { January15, November2, AnotherFeature, ParentPlacementRule.EarliestCreated, November2 },
            new object?[] { November2, November2, AFeature, ParentPlacementRule.LowestKey, null },
            new object?[] { null, null, AFeature, ParentPlacementRule.LowestKey, null },
        ];

        private static readonly object[] SetsTheDatesAndKeysDecide =
        [
            new object[] { new[] { LinkedOn(AnotherFeature, June17), LinkedOn(AFeature, March4), Undated("PAY-95"), Undated(AnIncident) }, Holding((AFeature, January15), (AnotherFeature, November2), ("PAY-95", November2)) },
            new object[] { new[] { Undated("PAY-95"), Undated(AFeature), Undated(AnotherFeature), LinkedOn(AnIncident, March4) }, Holding((AFeature, January15), (AnotherFeature, November2), ("PAY-95", November2)) },
            new object[] { new[] { Undated("OPS-9"), Undated("OPS-12"), Undated("OPS-100"), Undated(AnIncident) }, Holding(("OPS-9", November2), ("OPS-12", November2), ("OPS-100", DateTime.MinValue)) },
            new object[] { new[] { LinkedOn(AFeature, March4), LinkedOn(AnotherFeature, March4), Undated("PAY-95") }, Holding((AFeature, January15), (AnotherFeature, November2)) },
        ];

        private static readonly string[] KeysToGenerateFrom = ["PAY-1", "PAY-12", "PAY-2", "PAY-88", "PAY-9", "INC-1", "INC-20", "PRB-3"];

        private static (ParentCandidate[] Candidates, StoredFeatures Stored, HashSet<string> StoredKeys) AGeneratedSet(Random random)
        {
            var count = random.Next(2, 6);
            var keys = KeysToGenerateFrom.OrderBy(_ => random.Next()).Take(count).ToList();
            var candidates = keys.ConvertAll(key => new ParentCandidate(key, ADateOrNone(random))).ToArray();

            var stored = KeysToGenerateFrom
                .Where(_ => random.Next(2) == 0)
                .ToDictionary(key => key, _ => random.Next(4) switch
                {
                    0 => (DateTime?)null,
                    1 => DateTime.MinValue,
                    _ => ADateOrNone(random) ?? November2,
                });

            return (candidates, new StoredFeatures(stored), [.. stored.Keys]);
        }

        private static DateTime? ADateOrNone(Random random)
            => random.Next(3) == 0 ? null : new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(random.Next(0, 4) * 30);

        private static List<string> WhatThisPlacementBreaks(
            int sample, ParentCandidate[] candidates, HashSet<string> storedKeys, ParentNarrowing narrowing, ParentPlacement placement)
        {
            var broken = new List<string>();
            var keys = candidates.Select(candidate => candidate.Key).ToHashSet(StringComparer.Ordinal);
            var setAside = placement.SetAside.Select(candidate => candidate.Key).ToHashSet(StringComparer.Ordinal);
            var where = $"sample {sample} ({narrowing}, candidates {string.Join(",", keys)}, stored {string.Join(",", storedKeys)})";

            switch (placement.Outcome)
            {
                case ParentPlacementOutcome.Placed:
                    if (!keys.Contains(placement.Key))
                    {
                        broken.Add($"{where}: placed under {placement.Key}, which no link named");
                    }

                    if (!setAside.SetEquals(keys.Where(key => key != placement.Key)))
                    {
                        broken.Add($"{where}: set aside {string.Join(",", setAside)}, not every other candidate");
                    }

                    if (narrowing == ParentNarrowing.Narrow && !storedKeys.Contains(placement.Key))
                    {
                        broken.Add($"{where}: placed under {placement.Key}, which is not a stored Feature");
                    }

                    break;
                case ParentPlacementOutcome.NotPlaced:
                    if (narrowing == ParentNarrowing.DoNotNarrow || keys.Overlaps(storedKeys))
                    {
                        broken.Add($"{where}: refused although a candidate could be placed");
                    }

                    break;
                default:
                    broken.Add($"{where}: two or more candidates were not asked about at all");
                    break;
            }

            return broken;
        }

        private static IEnumerable<T[]> EveryOrderOf<T>(T[] candidates)
        {
            if (candidates.Length <= 1)
            {
                yield return candidates;
                yield break;
            }

            for (var first = 0; first < candidates.Length; first++)
            {
                var rest = candidates.Where((_, index) => index != first).ToArray();
                foreach (var tail in EveryOrderOf(rest))
                {
                    yield return [candidates[first], .. tail];
                }
            }
        }

        /// <summary>The placement in one comparable line, set-aside dates included, so two placements compare by what they say.</summary>
        private static string Summary(ParentPlacement placement)
        {
            var setAside = string.Join(", ", placement.SetAside.Select(candidate => candidate.LinkedAt is { } at
                ? $"{candidate.Key}@{at:yyyy-MM-dd}"
                : candidate.Key));

            return placement.Outcome == ParentPlacementOutcome.Placed
                ? $"Placed {placement.Key} by {placement.DecidedBy}, set aside {setAside}"
                : $"{placement.Outcome} ({placement.WhyNotPlaced}), set aside {setAside}";
        }

        private static ParentCandidate Undated(string key) => new(key, null);

        private static ParentCandidate LinkedOn(string key, DateTime at) => new(key, at);

        private static StoredFeatures Holding(params (string Key, DateTime? Created)[] features)
            => new(features.ToDictionary(feature => feature.Key, feature => feature.Created, StringComparer.Ordinal));
    }
}
