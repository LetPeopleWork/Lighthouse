using System.Text;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Csv;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Csv
{
    /// <summary>
    /// Epic #5511 slice 04, AC-04.2 - the number, for CSV, where there is no number of the usual kind. This
    /// connector makes no remote calls at all: the upload is carried on the entity and parsed in memory, so
    /// there are no round trips to count and nothing an operator's rate limit pays for.
    ///
    /// What is left is still worth honouring. A large upload takes real time to parse and holds the update
    /// slot while it does, and the promise has to hold uniformly - a Cancel button that stops four connectors
    /// and quietly finishes the fifth is a button nobody can trust. The failure mode is the worst one there
    /// is: removal is computed as stored minus fetched, so a parse that handed back the rows it managed
    /// before stopping would delete every record on the rows it never reached.
    /// </summary>
    [TestFixture]
    [Category("epic-5511-task-manager")]
    [Category("slice-04")]
    public class CsvCancellationGranularityTest
    {
        /// <summary>
        /// About 229 KB, which CsvHelper pulls in roughly fifty buffer-sized bites. Enough that a parse can
        /// be stopped well inside the rows rather than at a boundary it was about to reach anyway.
        /// </summary>
        private const int RowsTheUploadHolds = 5000;

        /// <summary>
        /// Deep enough into the upload that parsing is unambiguously under way - the header is long gone by
        /// the third bite - and far enough from the end that stopping is distinguishable from finishing.
        /// </summary>
        private const int TheBiteTheCancelArrivesOn = 3;

        [Test]
        public async Task GetWorkItemsForTeam_CancelledBeforeItStarts_NeverOpensTheUpload()
        {
            var subject = AConnectorReadingAnUpload();

            using var alreadyCancelled = new CancellationTokenSource();
            await alreadyCancelled.CancelAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    async () => await subject.GetWorkItemsForTeam(ATeamWithAnUpload(), alreadyCancelled.Token),
                    Throws.InstanceOf<OperationCanceledException>());

                Assert.That(subject.Uploads, Is.Empty,
                    "A refresh cancelled before it began should not open the upload at all. Parsing it and "
                    + "then throwing the result away is work nobody asked for.");
            }
        }

        [Test]
        public async Task GetFeaturesForProject_CancelledBeforeItStarts_NeverOpensTheUpload()
        {
            var subject = AConnectorReadingAnUpload();

            using var alreadyCancelled = new CancellationTokenSource();
            await alreadyCancelled.CancelAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    async () => await subject.GetFeaturesForProject(APortfolioWithAnUpload(), alreadyCancelled.Token),
                    Throws.InstanceOf<OperationCanceledException>());

                Assert.That(subject.Uploads, Is.Empty,
                    "The portfolio half opens its own reader over the same upload, so it can start work a "
                    + "cancelled refresh should never have begun.");
            }
        }

        [Test]
        public void GetWorkItemsForTeam_CancelledWhileItParses_StopsWithoutReadingTheRest()
        {
            using var stopPartWayIn = new CancellationTokenSource();
            var subject = AConnectorReadingAnUpload();
            subject.OnBite = () => StopOnceParsingIsUnderWay(subject, stopPartWayIn);

            Assert.That(
                async () => await subject.GetWorkItemsForTeam(ATeamWithAnUpload(), stopPartWayIn.Token),
                Throws.InstanceOf<OperationCanceledException>(),
                "Throwing is the whole point. Removal is stored minus fetched, so answering with the rows it "
                + "managed to parse would delete every record on the rows it never reached.");

            AssertItStoppedMidUpload(subject);
        }

        [Test]
        public void GetFeaturesForProject_CancelledWhileItParses_StopsWithoutReadingTheRest()
        {
            using var stopPartWayIn = new CancellationTokenSource();
            var subject = AConnectorReadingAnUpload();
            subject.OnBite = () => StopOnceParsingIsUnderWay(subject, stopPartWayIn);

            Assert.That(
                async () => await subject.GetFeaturesForProject(APortfolioWithAnUpload(), stopPartWayIn.Token),
                Throws.InstanceOf<OperationCanceledException>());

            AssertItStoppedMidUpload(subject);
        }

        [Test]
        public async Task GetParentFeaturesDetails_CancelledBeforeItStarts_StopsRatherThanAnsweringWithNone()
        {
            var subject = AConnectorReadingAnUpload();

            using var alreadyCancelled = new CancellationTokenSource();
            await alreadyCancelled.CancelAsync();

            Assert.That(
                async () => await subject.GetParentFeaturesDetails(APortfolioWithAnUpload(), ["EPIC-001"], alreadyCancelled.Token),
                Throws.InstanceOf<OperationCanceledException>(),
                "A CSV upload has no parent features to fetch, so this answers with none - and answering "
                + "normally lets a cancelled refresh walk on to its next step, which is what the operator "
                + "pressed the button to prevent.");
        }

        private static void StopOnceParsingIsUnderWay(RecordedCsvConnector subject, CancellationTokenSource stopping)
        {
            if (subject.Uploads.Count == 1 && subject.Uploads[0].Bites == TheBiteTheCancelArrivesOn)
            {
                stopping.Cancel();
            }
        }

        private static void AssertItStoppedMidUpload(RecordedCsvConnector subject)
        {
            var upload = subject.Uploads.Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(upload.Bites, Is.GreaterThan(1),
                    "The cancel has to land while the parse is running, not before it started - otherwise "
                    + "this says nothing the cancelled-before-it-starts test does not already say.");

                Assert.That(upload.CharactersHandedOver, Is.LessThan(upload.CharactersTheUploadHolds),
                    "A parse told to stop must stop pulling the upload. Draining the rest and discarding it "
                    + "spends the same time the operator cancelled to get back.");
            }
        }

        private static RecordedCsvConnector AConnectorReadingAnUpload()
        {
            var csv = new StringBuilder("ID,Name,State,Type,Started Date,Closed Date\n");

            for (var row = 1; row <= RowsTheUploadHolds; row++)
            {
                csv.Append($"ITEM-{row:D5},Item {row},Active,Story,2025-01-20,\n");
            }

            return new RecordedCsvConnector(Mock.Of<ILogger<CsvWorkTrackingConnector>>(), csv.ToString());
        }

        private static Team ATeamWithAnUpload()
        {
            var team = new Team
            {
                Id = 1,
                Name = "TestTeam",
                DataRetrievalValue = string.Empty,
                WorkTrackingSystemConnection = ACsvConnection(),
            };

            team.WorkItemTypes.Clear();
            team.WorkItemTypes.Add("Story");
            SetTheStatesOf(team);

            return team;
        }

        private static Portfolio APortfolioWithAnUpload()
        {
            var portfolio = new Portfolio
            {
                Id = 1,
                Name = "TestPortfolio",
                DataRetrievalValue = string.Empty,
                WorkTrackingSystemConnection = ACsvConnection(),
            };

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("Story");
            SetTheStatesOf(portfolio);

            return portfolio;
        }

        private static void SetTheStatesOf(WorkTrackingSystemOptionsOwner owner)
        {
            owner.ToDoStates.Clear();
            owner.ToDoStates.Add("New");
            owner.DoingStates.Clear();
            owner.DoingStates.Add("Active");
            owner.DoneStates.Clear();
            owner.DoneStates.Add("Done");
        }

        /// <summary>
        /// Built by the factory production uses, not by hand. Every option is looked up with Single, so a
        /// connection assembled from the keys this test happens to know about fails on the first option
        /// somebody adds - and fails as a missing element, which reads nothing like a cancellation defect.
        /// </summary>
        private static WorkTrackingSystemConnection ACsvConnection()
            => new WorkTrackingSystemFactory(Mock.Of<ILogger<WorkTrackingSystemFactory>>())
                .CreateDefaultConnectionForWorkTrackingSystem(WorkTrackingSystems.Csv);

        /// <summary>
        /// The real connector over uploads the test can watch. A fresh reader per call, as production hands
        /// out, so a second read of the same entity cannot be handed a spent one - and the list of them is
        /// itself an assertion, because "never opened" and "opened and read nothing" are different answers.
        /// </summary>
        private sealed class RecordedCsvConnector(ILogger<CsvWorkTrackingConnector> logger, string content)
            : CsvWorkTrackingConnector(logger)
        {
            private readonly List<AnUpload> uploads = [];

            public Action? OnBite { get; set; }

            public List<AnUpload> Uploads => uploads;

            internal override TextReader ContentOf(IWorkItemQueryOwner owner)
            {
                var upload = new AnUpload(content) { OnBite = OnBite };
                uploads.Add(upload);

                return upload;
            }
        }

        /// <summary>
        /// A CSV that says how much of itself has actually been pulled. CsvHelper reads in buffer-sized
        /// bites rather than row by row, so this counts bites and characters - the question it answers is
        /// not "which row" but "did the parse keep going after it was told to stop".
        /// </summary>
        private sealed class AnUpload(string content) : TextReader
        {
            private readonly StringReader inner = new(content);

            public int CharactersHandedOver { get; private set; }

            public int Bites { get; private set; }

            public int CharactersTheUploadHolds { get; } = content.Length;

            public Action? OnBite { get; set; }

            public override int Read(char[] buffer, int index, int count)
            {
                var handedOver = inner.Read(buffer, index, count);

                if (handedOver > 0)
                {
                    CharactersHandedOver += handedOver;
                    Bites++;
                    OnBite?.Invoke();
                }

                return handedOver;
            }

            public override int Read()
            {
                var character = inner.Read();

                if (character >= 0)
                {
                    CharactersHandedOver++;
                    Bites++;
                    OnBite?.Invoke();
                }

                return character;
            }

            public override int Peek() => inner.Peek();

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    inner.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}
