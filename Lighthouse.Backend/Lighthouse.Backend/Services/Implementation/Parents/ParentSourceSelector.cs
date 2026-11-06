using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Implementation.Parents
{
    /// <summary>
    /// Decides which of the sources a Team or a Portfolio lists gave one record its parent.
    ///
    /// The list is read in order and the first position that yields a parent wins, whether it names a field
    /// of the tracker's, one of its link types, or the parent the tracker reports for itself. A tracker only
    /// reads what each position holds; the order, what counts as yielding and the handing over to the next
    /// position exist here once, so that no two trackers can come to mean different things by them. What is
    /// decided is returned, and <c>HangUnder</c> is the one place that applies it to a record.
    ///
    /// This lives apart from any one tracker because the rule belongs to the Team and the Portfolio rather
    /// than to the tracker, and apart from the sibling that decides the same thing for dependencies
    /// because the two encode different knowledge: a record waits on any number of things, only a
    /// Portfolio has them, and a bad entry among them is skipped. A record hangs under at most one thing,
    /// and both grains have one. Links can need the stored Features to be judged, which only the refresh
    /// holds, so a connector walks the list provisionally and the refresh walks it again for what it left open.
    ///
    /// It sits here rather than beside the work item services because a connector has to call it, and a
    /// connector is a leaf input adapter that is not allowed to reach into those - a boundary the
    /// architecture tests enforce.
    /// </summary>
    public static class ParentSourceSelector
    {
        private delegate (string Key, bool NeedsStoredFeatures) LinkJudgement(ParentResolution links);

        /// <summary>
        /// Whether a list asks for the parent the tracker reports for itself to be read at all, wherever in
        /// the list it stands. Asked by anything that has to behave differently without it - what to fetch as
        /// well as what to read it from - so that those answers cannot drift apart from what is actually read.
        /// </summary>
        public static bool ReadsTheTrackersOwnParent(ParentSources list)
            => list.Entries.Contains(ParentSourceEntry.TrackerParent);

        /// <summary>
        /// The final answer, for whoever holds the stored Features: the first position that yields a parent
        /// wins. A field or the tracker's own parent yields whenever it has a value, even one naming no
        /// Feature. Links yield only what placing them decides, so links that fail the Feature test hand over
        /// to the next position.
        /// </summary>
        public static ParentChoice Choose(ParentReadings readings, StoredFeatures storedFeatures, ParentNarrowing narrowing)
            => TheFirstThatYields(readings, links => (links.Place(storedFeatures, narrowing).Key, false));

        /// <summary>
        /// The same walk for a connector, which cannot see the stored Features. It stops at the first links that
        /// need them and says so. The key it returns then is only the best a caller with nothing further to
        /// ask could use: a lone candidate at face value, or nothing when there are several.
        /// </summary>
        public static ParentChoice ChooseProvisionally(ParentReadings readings, ParentNarrowing narrowing)
            => TheFirstThatYields(readings, links => (links.Key, NeedAFeatureTest(links, narrowing)));

        /// <summary>
        /// The links a choice has to answer for when they named several issues: the ones that won, or else the
        /// last ones walked past on the way to the winner. One answer per record, so a record listing two link
        /// entries is counted once. Empty when no such links were walked.
        /// </summary>
        public static ParentResolution TheLinksNamingSeveral(ParentReadings readings, ParentChoice choice)
            => readings.Positions
                .Take(choice.WinnerPosition + 1 ?? readings.Positions.Count)
                .Select(position => position.Reading.Links)
                .LastOrDefault(links => links.IsAmbiguous);

        private static bool NeedAFeatureTest(ParentResolution links, ParentNarrowing narrowing)
            => links.IsAmbiguous || (links.IsResolved && narrowing == ParentNarrowing.Narrow);

        private static ParentChoice TheFirstThatYields(ParentReadings readings, LinkJudgement judgeLinks)
            => readings.Positions
                .Select((position, index) => WhatOnePositionYields(position.Reading, index, judgeLinks))
                .FirstOrDefault(choice => choice.NeedsStoredFeatures || !string.IsNullOrEmpty(choice.Key))
                ?? ParentChoice.NoParent;

        private static ParentChoice WhatOnePositionYields(ParentReading reading, int position, LinkJudgement judgeLinks)
        {
            var (key, needsStoredFeatures) = reading.Kind switch
            {
                ParentReadingKind.FieldValue or ParentReadingKind.TrackersOwn => (reading.Key, false),
                ParentReadingKind.LinkCandidates => judgeLinks(reading.Links),
                _ => (string.Empty, false),
            };

            return new ParentChoice(key, position, needsStoredFeatures);
        }

        /// <summary>
        /// Applies a choice to a record, and is the only thing that does. An empty key clears the parent, so a
        /// provisional key that later fails the Feature test is taken back. A link type has no value of its own
        /// to show, so the winning link entry's field shows the key and every other link entry's field shows
        /// nothing: a key that was not acted on would send somebody looking for a parent that was never stored.
        /// A field entry's value is the tracker's and stays as it is.
        /// </summary>
        public static void HangUnder(WorkItemBase record, ParentReadings readings, ParentChoice choice)
        {
            record.ParentReferenceId = choice.Key;

            for (var position = 0; position < readings.Positions.Count; position++)
            {
                if (readings.Positions[position] is { Reading.Kind: ParentReadingKind.LinkCandidates, Source.AdditionalFieldDefinitionId: { } field })
                {
                    record.AdditionalFieldValues[field] = position == choice.WinnerPosition ? choice.Key : string.Empty;
                }
            }
        }
    }
}
