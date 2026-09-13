namespace Lighthouse.Backend.Configuration
{
    public class UsageDataConfiguration
    {
        public const string SectionName = "UsageData";

        /// <summary>
        /// How long a browser's consent keeps counting after it was last seen. Clearing browser
        /// storage sends nothing, so a browser that has gone away can only be noticed by its
        /// silence - this is how long that silence has to last.
        /// </summary>
        public int ConsentLivenessWindowDays { get; set; } = 30;

        /// <summary>
        /// How long an instance has to have been installed before anybody is asked about usage data.
        /// The question is meant to arrive after somebody has actually used Lighthouse: asked on
        /// first launch it is dismissed reflexively, and a reflex is not an answer worth counting.
        /// </summary>
        public int AskAfterInstallDays { get; set; } = 3;

        /// <summary>
        /// How long a browser that was asked and did not agree is left alone before the question
        /// comes back. The dialog tells the reader this is coming, so the number and the sentence
        /// are one promise - shortening it without changing the copy breaks the promise rather than
        /// tuning a schedule.
        /// </summary>
        public int ReAskAfterDays { get; set; } = 90;

        /// <summary>
        /// How long a browser that has stopped visiting is remembered. Clearing browser storage
        /// sends nothing, so rows for browsers that will never return can only be recognised by
        /// their silence, and without this the table grows by one row for every browser ever shown
        /// the dialog - refusals included.
        ///
        /// Never applied to a row that is still owed a question: see the repository's prune, where
        /// the re-ask date is a floor under this number rather than a value compared against it.
        /// </summary>
        public int ConsentRetentionDays { get; set; } = 180;

        /// <summary>
        /// Where usage data goes, when it should not go where it goes by default. Left empty, a
        /// published release sends to the collector this product ships with, and a build nobody
        /// published sends nothing at all - which keeps every copy run from source, this test suite
        /// included, out of the figures real usage is counted in.
        ///
        /// Naming somewhere lifts that: whatever this build is, it sends there. That is how a fork
        /// points at a collector of its own, and how anyone watches the whole path end to end
        /// without a release in hand.
        /// </summary>
        public string? CollectorBaseUrl { get; set; }

        /// <summary>
        /// How many events one instance may forward in a day. The allowance being protected is not
        /// this instance's - it is a single shared one, paid for once and drawn on by every instance
        /// in the world, so forty honest instances can empty it without any of them misbehaving and
        /// no per-browser limit can see that happening. Past this number the day's events are thrown
        /// away rather than refused: a refusal only tells a browser to try again, and trying again is
        /// the last thing an exhausted allowance needs.
        ///
        /// A thousand is roughly twenty busy people's day on one instance, so a real deployment never
        /// meets it, while a runaway one costs at most thirty thousand a month instead of everything.
        /// Unset must not mean unlimited, which is why there is a number here rather than a null.
        /// </summary>
        public int DailyEventBudget { get; set; } = 1000;

        /// <summary>
        /// What the collector recognises this product's own project by. It travels in each message
        /// rather than in a header, because that is the shape the collector reads it in.
        /// </summary>
        public string? ProjectApiKey { get; set; }
    }
}
