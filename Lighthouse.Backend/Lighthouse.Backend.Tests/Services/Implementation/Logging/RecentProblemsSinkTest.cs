using Lighthouse.Backend.Models.Logging;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.Logging;
using Serilog.Events;
using Serilog.Parsing;

namespace Lighthouse.Backend.Tests.Services.Implementation.Logging
{
    /// <summary>
    /// The buffer on its own, driven through the sink method the log pipeline calls.
    ///
    /// The acceptance scenarios in <c>Slice06TheWarningsWithoutTheLog*</c> provoke real failures and prove
    /// that the oldest problem is the one that goes. What they do not say is how many are left afterwards,
    /// and a buffer that evicted two at a time, or that kept one more than it promised, satisfies every one
    /// of them. That count is a data-structure invariant and is cheaper to pin here.
    /// </summary>
    [Category("epic-5511-task-manager")]
    [Category("slice-06")]
    public class RecentProblemsSinkTest
    {
        /// <summary>
        /// Small on purpose: rolling over is the subject, and provoking the shipped default of two hundred
        /// would say nothing the fourth event does not.
        /// </summary>
        private const int RoomForThree = 3;

        private const string SourceContextProperty = "SourceContext";

        private const string TheQueueThatRefreshes =
            "Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.UpdateQueueService";

        private const string AComplaint = "Error processing update task for Team with ID 7";

        /// <summary>
        /// Who an event that never named a logger is attributed to. Written out rather than read from the
        /// sink, so that blanking it there has to turn these red.
        /// </summary>
        private const string TheApplicationItself = "Lighthouse";

        /// <summary>
        /// A source context with nothing after its last separator. Serilog will not produce one, but the
        /// trimming does not know that, and the answer it gives has to be a word rather than a blank.
        /// </summary>
        private const string ANamespaceWithNothingAfterIt = "Lighthouse.Backend.Services.";

        private static readonly MessageTemplateParser TemplateParser = new();

        private static readonly string[] TheThreeNewestOfFiveNewestFirst =
            ["problem 5", "problem 4", "problem 3"];

        // The rolled-over count, which is the thing the acceptance scenarios cannot see. Five events into
        // room for three: a buffer that grew by one holds four, one that evicted in pairs holds two.
        [Test]
        public void Emit_MoreProblemsThanThereIsRoomFor_HoldsExactlyAsManyAsItPromised()
        {
            var sink = new RecentProblemsSink(RoomForThree);

            EmitNumberedProblems(sink, howMany: 5);

            Assert.That(sink.MostRecentFirst(), Has.Count.EqualTo(RoomForThree));
        }

        // Which three survive, and in which order. Holding the right number of the wrong problems - or the
        // right problems upside down - passes the count on its own.
        [Test]
        public void Emit_MoreProblemsThanThereIsRoomFor_KeepsTheNewestOnesAndReadsThemNewestFirst()
        {
            var sink = new RecentProblemsSink(RoomForThree);

            EmitNumberedProblems(sink, howMany: 5);

            Assert.That(
                sink.MostRecentFirst().Select(problem => problem.Message),
                Is.EqualTo(TheThreeNewestOfFiveNewestFirst));
        }

        // The host writes some of its startup lines without saying which logger wrote them. Those are still
        // Lighthouse complaining, and a blank column would read as a bug in the section rather than as an
        // event nobody signed.
        [Test]
        public void Emit_AProblemThatNeverSaidWhichLoggerWroteIt_IsStillAttributedToSomething()
        {
            var sink = new RecentProblemsSink(RoomForThree);

            sink.Emit(AProblemSaying(AComplaint));

            Assert.That(TheOnlyProblemIn(sink).Source, Is.EqualTo(TheApplicationItself));
        }

        // The other road to the same answer, and the one nothing was taking. The case above never reaches
        // the trimming at all; this one reaches it and gets nothing back, because there is nothing after
        // the last separator. A blank column there would read as a bug in the section rather than as an
        // event nobody signed - which is the whole reason the fallback exists.
        [Test]
        public void Emit_AProblemWhoseSourceEndsWhereTheTypeNameShouldStart_IsStillAttributedToSomething()
        {
            var sink = new RecentProblemsSink(RoomForThree);

            sink.Emit(AProblemSaying(AComplaint, from: ANamespaceWithNothingAfterIt));

            Assert.That(TheOnlyProblemIn(sink).Source, Is.EqualTo(TheApplicationItself));
        }

        // The sink sits inside the log pipeline, so an event that is not there means the pipeline handing
        // it over is broken. Refusing says so at the point it happened; swallowing it would put a row in
        // the popover that says nothing, some distance from whatever actually went wrong.
        [Test]
        public void Emit_NoEventAtAll_IsRefusedAndNamesWhatWasMissing()
        {
            var sink = new RecentProblemsSink(RoomForThree);

            Assert.That(
                () => sink.Emit(null!),
                Throws.ArgumentNullException
                    .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("logEvent"));
        }

        // The same thing the console template already shows an operator. The namespace in front of it is
        // near enough identical on every line and would push the part that differs off the end of the row.
        [Test]
        public void Emit_AProblemFromALoggerForAType_NamesTheTypeRatherThanItsNamespace()
        {
            var sink = new RecentProblemsSink(RoomForThree);

            sink.Emit(AProblemSaying(AComplaint, from: TheQueueThatRefreshes));

            Assert.That(TheOnlyProblemIn(sink).Source, Is.EqualTo("UpdateQueueService"));
        }

        // Most warnings are nobody throwing anything. A row that named a type here would be inventing one.
        [Test]
        public void Emit_AProblemWhereNothingThrew_SaysNothingBroke()
        {
            var sink = new RecentProblemsSink(RoomForThree);

            sink.Emit(AProblemSaying(AComplaint));

            Assert.That(TheOnlyProblemIn(sink).ExceptionType, Is.Null);
        }

        // The positive control: without it, a projection that never reads the exception at all satisfies the
        // scenario above. Short, for the same reason the source is short.
        [Test]
        public void Emit_AProblemWhereSomethingThrew_NamesWhatBroke()
        {
            var sink = new RecentProblemsSink(RoomForThree);

            sink.Emit(AProblemSaying(AComplaint, thrown: new InvalidOperationException("the tracker said no")));

            Assert.That(TheOnlyProblemIn(sink).ExceptionType, Is.EqualTo(nameof(InvalidOperationException)));
        }

        // A sentence that opens by naming what it is about. The words standing for the thing being
        // refreshed start at the very first one and run to the number, and a reader that takes any less
        // than that stretch leaves part of the id sitting in the row with a name in front of it.
        [Test]
        public void Emit_AProblemThatOpensByNamingWhatItIsAbout_KnowsWhichWordsStandForIt()
        {
            var sink = new RecentProblemsSink(RoomForThree);

            sink.Emit(AProblemCarrying(
                "{UpdateType} with ID {Id} could not be refreshed",
                TheKindOfWork(UpdateType.Team),
                TheNumber(7)));

            Assert.That(
                TheOnlyProblemIn(sink).Refresh?.AsWritten,
                Is.EqualTo("Team with ID 7"),
                "A name is put into the sentence by standing in for the words that pointed at the thing "
                + "without naming it. Take in too little and the row keeps half the number; take in too "
                + "much and it loses the half that said anything went wrong.");
        }

        // "Id" is a name any logger might use, and "UpdateType" is not far behind. A line that uses them
        // for something that is not a refresh - a work item's reference, say - is not about a refresh, and
        // treating it as one has the section replace a perfectly good sentence with the name of team nought.
        [Test]
        public void Emit_AProblemWhoseIdIsNotARefreshsAtAll_IsNotTakenForARefresh()
        {
            var sink = new RecentProblemsSink(RoomForThree);

            sink.Emit(AProblemCarrying(
                "Error processing update task for {UpdateType} with ID {Id}",
                TheKindOfWork("Feature"),
                TheNumber("PROJ-42")));

            Assert.That(
                TheOnlyProblemIn(sink).Refresh,
                Is.Null,
                "Nothing about this line is a refresh; only the two words it happens to have borrowed. A "
                + "row built from it would be looked up, found to be nobody, and rewritten to say so.");
        }

        // One shape of this is deliberately not pinned, and it is worth saying why rather than leaving a
        // gap somebody later fills with an assertion about the inside of the sink. A line that gives the
        // number before it says what kind of thing it is has no run of words leading from one to the
        // other, and a line with nothing for a name to stand in for is left exactly as it was written. So
        // whether such a line is turned away early or simply found to have nothing to offer, the row an
        // operator ends up reading is the same one, and no test can tell the two apart.
        //
        // The two values can reach the sink without the sentence mentioning either: an enricher attaches
        // them to everything written while a refresh is running, including lines about something else. There
        // is then nowhere in the sentence for a name to go, and the line has to be left exactly as written
        // rather than the sink hunting for words that are not there.
        [Test]
        public void Emit_AProblemCarryingARefreshsValuesInASentenceThatNamesNeither_IsNotTakenForARefresh()
        {
            var sink = new RecentProblemsSink(RoomForThree);

            sink.Emit(AProblemCarrying(
                "The update queue drain exceeded the shutdown timeout",
                TheKindOfWork(UpdateType.Team),
                TheNumber(7)));

            Assert.That(
                TheOnlyProblemIn(sink).Refresh,
                Is.Null,
                "The sentence says nothing about which team, so there is no stretch of it a name could "
                + "replace. Looking for one anyway runs off the front of the line.");
        }

        // Room for no problems is not a smaller buffer, it is a buffer that cannot hold the event it was
        // just handed. The sink refuses to be built that way, and Program turns a misconfigured number into
        // the default rather than letting it reach here - see RecentProblemsCapacityConfigurationTest.
        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_RoomForNoProblemsAtAll_IsRefused(int capacity)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RecentProblemsSink(capacity));
        }

        private static void EmitNumberedProblems(RecentProblemsSink sink, int howMany)
        {
            for (var number = 1; number <= howMany; number++)
            {
                sink.Emit(AProblemSaying($"problem {number}"));
            }
        }

        private static RecentProblem TheOnlyProblemIn(RecentProblemsSink sink)
        {
            return sink.MostRecentFirst().Single();
        }

        /// <summary>
        /// A complaint that carries structured values alongside its sentence, which is how the queue's own
        /// line reaches the sink. Handed over separately from the wording on purpose: a Serilog enricher
        /// can attach them to a line that mentions neither, and that is a case the sink has to survive.
        /// </summary>
        private static LogEvent AProblemCarrying(string message, params LogEventProperty[] values)
        {
            return new LogEvent(
                DateTimeOffset.UtcNow,
                LogEventLevel.Error,
                null,
                TemplateParser.Parse(message),
                values);
        }

        private static LogEventProperty TheKindOfWork(object value)
        {
            return new LogEventProperty("UpdateType", new ScalarValue(value));
        }

        private static LogEventProperty TheNumber(object value)
        {
            return new LogEventProperty("Id", new ScalarValue(value));
        }

        private static LogEvent AProblemSaying(string message, string? from = null, Exception? thrown = null)
        {
            var properties = new List<LogEventProperty>();

            if (from is not null)
            {
                properties.Add(new LogEventProperty(SourceContextProperty, new ScalarValue(from)));
            }

            return new LogEvent(
                DateTimeOffset.UtcNow,
                LogEventLevel.Error,
                thrown,
                TemplateParser.Parse(message),
                properties);
        }
    }
}
