using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Modules.Reports.Domain;

namespace Lighthouse.Backend.Tests.API.Integration.Reports
{
    /// <summary>
    /// A writer adds a note to a report and the report lists it, newest first, with the day it was written on the
    /// instance's calendar and the name of whoever wrote it - no name when sign-in is off. A note without text is
    /// not saved, a note nobody can be named for is refused while sign-in is on, and deleting a report, alone, in
    /// bulk or with its owner, deletes its notes.
    ///
    /// Driving ports: adding a note to a report and listing a report's notes, with no web host.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    // The test project has no property library: the two generated checks below draw from a seeded Random, print the
    // seed in every failure message and are replayed by running again with that seed. They do not shrink a failing case.
    public class ReportNoteScenarios : ReportsAtThePortTest
    {
        private const string Ann = "Ann";

        private const string TeamGrew = "Team grew\nto 9";

        private const string Training = "Training took place";

        private ReportNoteCaller? ann;

        // Ann is a stored profile of this scenario's own database, because a note's author is a link to a profile.
        private ReportNoteCaller SignedInAsAnn => ann ??= new ReportNoteCaller.Signed(ASignedInProfile(Ann), Ann);

        [SetUp]
        public void ForgetTheSignerOfTheLastScenario() => ann = null;

        private static readonly ReportNoteCaller SignInIsOff = new ReportNoteCaller.Anonymous();

        private static readonly ReportNoteCaller SignedInButNotKnown = new ReportNoteCaller.Unresolved();

        // 09:00 on Friday 2 Oct 2026 in Zurich, and 01:30 on Saturday 3 Oct there: the second is still the 2nd in UTC.
        private static readonly DateTime MorningOnThe2nd = new(2026, 10, 2, 7, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime LateEveningUtcOnThe2nd = new(2026, 10, 2, 23, 30, 0, DateTimeKind.Utc);

        private static readonly DateOnly The2nd = new(2026, 10, 2);

        private static readonly DateOnly The3rd = new(2026, 10, 3);

        [TestCase("team")]
        [TestCase("portfolio")]
        public async Task A_signed_in_writer_adds_notes_and_the_report_lists_them_newest_first_with_the_day_written_and_their_name(string ownerKind)
        {
            var owner = AnOwner(ownerKind);
            var report = await AReportOf(owner);

            var first = await AddANote(owner, report, SignedInAsAnn, $"  {TeamGrew}\n ", MorningOnThe2nd);
            var second = await AddANote(owner, report, SignedInAsAnn, Training, LateEveningUtcOnThe2nd);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.Status, Is.EqualTo(ReportOutcomeStatus.Done), $"the first note was not saved: {first.Code}");
                Assert.That(second.Status, Is.EqualTo(ReportOutcomeStatus.Done), $"the second note was not saved: {second.Code}");
                Assert.That(TheNotesListed(owner, report), Is.EqualTo(new[]
                {
                    new ShownNote(Training, The3rd, null, Ann),
                    new ShownNote(TeamGrew, The2nd, null, Ann),
                }), "newest first, text trimmed of its outer blanks, written day on the instance's calendar, author as signed");
            }
        }

        [Test]
        public async Task A_note_written_with_sign_in_off_is_listed_with_its_text_and_day_and_no_author()
        {
            var owner = ATeam("Lightspeed");
            var report = await AReportOf(owner);

            var added = await AddANote(owner, report, SignInIsOff, Training, MorningOnThe2nd);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(added.Status, Is.EqualTo(ReportOutcomeStatus.Done), $"the note was not saved: {added.Code}");
                Assert.That(TheNotesListed(owner, report), Is.EqualTo(new[] { new ShownNote(Training, The2nd, null, null) }));
            }
        }

        [Test]
        public async Task A_note_is_refused_when_sign_in_is_on_and_nobody_can_be_named_and_the_notes_stay_as_they_were()
        {
            var owner = ATeam("Lightspeed");
            var report = await AReportOf(owner);
            await AddANote(owner, report, SignInIsOff, Training, MorningOnThe2nd);

            var refused = await AddANote(owner, report, SignedInButNotKnown, TeamGrew, LateEveningUtcOnThe2nd);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.Status, Is.EqualTo(ReportOutcomeStatus.Refused));
                Assert.That(refused.Code, Is.EqualTo(ReportRefusalCodes.NoteAuthorUnknown));
                Assert.That(TheNotesListed(owner, report), Is.EqualTo(new[] { new ShownNote(Training, The2nd, null, null) }));
            }
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\t \r\n ")]
        [TestCase(null)]
        public async Task A_note_without_text_is_not_saved_and_the_notes_stay_as_they_were(string? text)
        {
            var owner = ATeam("Lightspeed");
            var report = await AReportOf(owner);
            await AddANote(owner, report, SignedInAsAnn, Training, MorningOnThe2nd);

            var refused = await AddANote(owner, report, SignedInAsAnn, text, LateEveningUtcOnThe2nd);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.Status, Is.EqualTo(ReportOutcomeStatus.Refused));
                Assert.That(refused.Code, Is.EqualTo(ReportRefusalCodes.NoteTextMissing));
                Assert.That(TheNotesListed(owner, report), Is.EqualTo(new[] { new ShownNote(Training, The2nd, null, Ann) }));
            }
        }

        [Test]
        public async Task A_note_of_more_than_2000_characters_is_not_saved_for_that_reason_while_one_of_exactly_2000_between_blanks_is()
        {
            var owner = ATeam("Lightspeed");
            var report = await AReportOf(owner);
            var longest = new string('x', ReportNote.LongestText);

            var kept = await AddANote(owner, report, SignedInAsAnn, $" {longest}\n", MorningOnThe2nd);
            var refused = await AddANote(owner, report, SignedInAsAnn, longest + "x", LateEveningUtcOnThe2nd);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(kept.Status, Is.EqualTo(ReportOutcomeStatus.Done), $"2000 characters must be kept: {kept.Code}");
                Assert.That(refused.Status, Is.EqualTo(ReportOutcomeStatus.Refused));
                Assert.That(refused.Code, Is.EqualTo(ReportRefusalCodes.NoteTextTooLong));
                Assert.That(TheNotesListed(owner, report), Is.EqualTo(new[] { new ShownNote(longest, The2nd, null, Ann) }));
            }
        }

        [Test]
        public async Task A_report_the_owner_does_not_hold_has_no_notes_to_add_to_or_list_and_it_is_found_missing_before_anything_else_is_judged()
        {
            var lightspeed = ATeam("Lightspeed");
            var zenith = ATeam("Zenith");
            var report = await AReportOf(lightspeed);

            var added = await AddANote(zenith, report, SignedInButNotKnown, "", MorningOnThe2nd);
            var listed = ReadTheNotes(zenith, report);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(added.Status, Is.EqualTo(ReportOutcomeStatus.NotFound));
                Assert.That(listed.Status, Is.EqualTo(ReportOutcomeStatus.NotFound));
                Assert.That(TheNotesListed(lightspeed, report), Is.Empty);
            }
        }

        [Test]
        public async Task Who_is_writing_is_judged_before_what_is_written()
        {
            var owner = ATeam("Lightspeed");
            var report = await AReportOf(owner);

            var refused = await AddANote(owner, report, SignedInButNotKnown, "   ", MorningOnThe2nd);

            Assert.That(refused.Code, Is.EqualTo(ReportRefusalCodes.NoteAuthorUnknown),
                "somebody nobody can name is refused for that, even before a missing text is looked at");
        }

        [Test]
        public async Task Deleting_a_report_deletes_its_notes_and_leaves_the_notes_of_other_reports()
        {
            var lightspeed = ATeam("Lightspeed");
            var zenith = ATeam("Zenith");
            var deletedReport = await AReportOf(lightspeed);
            var keptReport = await AReportOf(zenith);
            await AddANote(lightspeed, deletedReport, SignedInAsAnn, Training, MorningOnThe2nd);
            await AddANote(lightspeed, deletedReport, SignedInAsAnn, TeamGrew, LateEveningUtcOnThe2nd);
            await AddANote(zenith, keptReport, SignedInAsAnn, Training, MorningOnThe2nd);

            var deleted = await DeleteTheReport(lightspeed, deletedReport);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(deleted, Is.EqualTo(ReportOutcomeStatus.Done));
                Assert.That(ReadTheNotes(lightspeed, deletedReport).Status, Is.EqualTo(ReportOutcomeStatus.NotFound), "the notes of a deleted report cannot be read");
                Assert.That((await AddANote(lightspeed, deletedReport, SignedInAsAnn, Training, MorningOnThe2nd)).Status, Is.EqualTo(ReportOutcomeStatus.NotFound));
                Assert.That(TheNotesListed(zenith, keptReport), Is.EqualTo(new[] { new ShownNote(Training, The2nd, null, Ann) }));
            }
        }

        [Test]
        public async Task Deleting_reports_in_bulk_deletes_the_notes_of_each_and_leaves_the_notes_of_a_report_not_deleted()
        {
            var lightspeed = ATeam("Lightspeed");
            var zenith = ATeam("Zenith");
            var gravity = ATeam("Gravity");
            var firstDeleted = await AReportOf(lightspeed);
            var secondDeleted = await AReportOf(zenith);
            var kept = await AReportOf(gravity);
            await AddANote(lightspeed, firstDeleted, SignedInAsAnn, Training, MorningOnThe2nd);
            await AddANote(zenith, secondDeleted, SignedInAsAnn, TeamGrew, MorningOnThe2nd);
            await AddANote(gravity, kept, SignedInAsAnn, Training, MorningOnThe2nd);

            var outcomes = await RemoveTheReportsTogether(firstDeleted, secondDeleted);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcomes.Select(outcome => outcome.Status), Is.All.EqualTo(ReportRemovalStatus.Removed));
                Assert.That(ReadTheNotes(lightspeed, firstDeleted).Status, Is.EqualTo(ReportOutcomeStatus.NotFound));
                Assert.That(ReadTheNotes(zenith, secondDeleted).Status, Is.EqualTo(ReportOutcomeStatus.NotFound));
                Assert.That(TheNotesListed(gravity, kept), Is.EqualTo(new[] { new ShownNote(Training, The2nd, null, Ann) }));
            }
        }

        [Test]
        public async Task Deleting_the_Team_that_owns_a_report_deletes_the_report_and_its_notes_and_leaves_another_Teams_notes()
        {
            var lightspeed = ATeam("Lightspeed");
            var zenith = ATeam("Zenith");
            var lightspeedReport = await AReportOf(lightspeed);
            var zenithReport = await AReportOf(zenith);
            await AddANote(lightspeed, lightspeedReport, SignedInAsAnn, Training, MorningOnThe2nd);
            await AddANote(zenith, zenithReport, SignedInAsAnn, Training, MorningOnThe2nd);

            DeleteTheTeam(lightspeed);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ReadTheNotes(lightspeed, lightspeedReport).Status, Is.EqualTo(ReportOutcomeStatus.NotFound));
                Assert.That(TheNotesListed(zenith, zenithReport), Is.EqualTo(new[] { new ShownNote(Training, The2nd, null, Ann) }));
            }
        }

        /// <summary>Law L1: for any raw text a note keeps it trimmed when its trimmed length is 1 to 2000 and is refused otherwise.</summary>
        [Test]
        public void A_note_keeps_its_text_without_outer_blanks_when_it_has_1_to_2000_characters_and_is_refused_otherwise()
        {
            var cases = new NoteTexts(Seed);

            foreach (var text in cases.Generate(Cases))
            {
                var written = ReportNote.Write(5, text.Raw, 7, Ann, MorningOnThe2nd);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(written.Status, Is.EqualTo(text.ExpectedStatus), $"seed {Seed}, {text}: {written.Code}");
                    Assert.That(written.Code, Is.EqualTo(text.ExpectedCode), $"seed {Seed}, {text}");
                    Assert.That(written.Value?.Text, Is.EqualTo(text.ExpectedStoredText), $"seed {Seed}, {text}: what is stored is the text without its outer blanks");
                    Assert.That(written.Value?.LastEditedAt, Is.Null, $"seed {Seed}, {text}: a note just written has not been edited");
                    Assert.That(written.Value?.AuthorDisplayName, Is.EqualTo(text.ExpectedStatus == ReportOutcomeStatus.Done ? Ann : null), $"seed {Seed}, {text}");
                }
            }
        }

        /// <summary>Law L2: after any sequence of adds, a report lists exactly its accepted notes, newest first by the instant written then by order of adding, and no note of another report or owner.</summary>
        [Test]
        public async Task A_report_lists_exactly_its_own_accepted_notes_newest_first_whatever_was_added_before()
        {
            var lightspeed = ATeam("Lightspeed");
            var zenith = ATeam("Zenith");
            var lightspeedReport = await AReportOf(lightspeed);
            var zenithReport = await AReportOf(zenith);
            var reports = new[] { (lightspeed, lightspeedReport), (zenith, zenithReport) };
            var model = new NotesOfEveryReport(reports, SignedInAsAnn);
            var random = new Random(Seed);

            for (var run = 0; run < Runs; run++)
            {
                foreach (var add in model.RandomAdds(random, random.Next(0, MostAdds + 1)))
                {
                    var outcome = await AddANote(add.Owner, add.ReportId, add.Caller, add.Text, add.WrittenAt);
                    Assert.That(outcome.Status, Is.EqualTo(add.ExpectedStatus), $"seed {Seed}, run {run}, {add}: {outcome.Code}");
                    model.Accepts(add);
                }

                foreach (var (owner, reportId) in reports)
                {
                    var listed = ReadTheNotes(owner, reportId);
                    using (Assert.EnterMultipleScope())
                    {
                        Assert.That(listed.Status, Is.EqualTo(ReportOutcomeStatus.Done), $"seed {Seed}, run {run}");
                        Assert.That(listed.Value?.Select(note => (note.Text, note.CreatedAt.Ticks)), Is.EqualTo(model.NewestFirst(reportId)),
                            $"seed {Seed}, run {run}: report {reportId} must list its own accepted notes, newest first by instant then by order of adding");
                    }
                }
            }
        }

        private const int Seed = 4711;

        private const int Cases = 200;

        private const int Runs = 100;

        private const int MostAdds = 12;

        private ReportOwner AnOwner(string ownerKind)
            => ownerKind == "team" ? ATeam("Team Lightspeed") : APortfolio("Project Apollo");
    }

    /// <summary>
    /// Generated note texts of 0 to 2200 characters over letters, digits, blanks, line breaks and markup characters.
    /// Each is built from a core that starts and ends with a visible character, plus blanks around it, so the text
    /// a note must keep is known from how the text was built and never from trimming it.
    /// </summary>
    internal sealed class NoteTexts(int seed)
    {
        private const string Visible = "abcXYZ019<>&\"'/ ";

        private const string Blanks = " \t\r\n";

        public IEnumerable<NoteText> Generate(int howMany)
        {
            var random = new Random(seed);
            yield return new NoteText(null, 0, null);
            yield return new NoteText("", 0, null);
            yield return new NoteText(Blanks, 0, null);
            yield return Around(random, Core(random, 1));
            yield return Around(random, Core(random, ReportNote.LongestText));
            yield return Around(random, Core(random, ReportNote.LongestText + 1));
            yield return new NoteText(new string('x', ReportNote.LongestText), ReportNote.LongestText, new string('x', ReportNote.LongestText));
            yield return new NoteText($"{Blanks}a\n\nb{Blanks}", 4, "a\n\nb");

            for (var i = 0; i < howMany; i++)
            {
                yield return random.Next(0, 4) switch
                {
                    0 => new NoteText(Blanks[..random.Next(0, Blanks.Length + 1)], 0, null),
                    1 => Around(random, Core(random, random.Next(1, 40))),
                    2 => Around(random, Core(random, random.Next(ReportNote.LongestText - 2, ReportNote.LongestText + 3))),
                    _ => Around(random, Core(random, random.Next(1, ReportNote.LongestText + 2))),
                };
            }
        }

        private static string Core(Random random, int length)
        {
            var letters = new char[length];
            for (var i = 0; i < length; i++)
            {
                letters[i] = i == 0 || i == length - 1 ? 'k' : Visible[random.Next(Visible.Length)];
            }

            if (length > 2 && random.Next(0, 2) == 0)
            {
                letters[random.Next(1, length - 1)] = '\n';
            }

            return new string(letters);
        }

        private static NoteText Around(Random random, string core)
            => new(PadWithBlanks(random) + core + PadWithBlanks(random), core.Length, core);

        private static string PadWithBlanks(Random random)
            => new(Enumerable.Range(0, random.Next(0, 8)).Select(_ => Blanks[random.Next(Blanks.Length)]).ToArray());
    }

    internal sealed record NoteText(string? Raw, int CoreLength, string? Core)
    {
        public string? ExpectedStoredText => ExpectedStatus == ReportOutcomeStatus.Done ? Core : null;

        public ReportOutcomeStatus ExpectedStatus => CoreLength is >= 1 and <= ReportNote.LongestText ? ReportOutcomeStatus.Done : ReportOutcomeStatus.Refused;

        public string? ExpectedCode => CoreLength switch
        {
            0 => ReportRefusalCodes.NoteTextMissing,
            > ReportNote.LongestText => ReportRefusalCodes.NoteTextTooLong,
            _ => null,
        };

        public override string ToString() => $"a text of {Raw?.Length ?? 0} characters with {CoreLength} left once its outer blanks are taken off";

    }

    /// <summary>What every report should list after the adds so far: an independent model sorted by the instant written, then by order of adding.</summary>
    internal sealed class NotesOfEveryReport(IReadOnlyList<(ReportOwner Owner, int ReportId)> reports, ReportNoteCaller signedIn)
    {
        private static readonly DateTime[] Instants =
        [
            new(2026, 10, 2, 7, 0, 0, DateTimeKind.Utc),
            new(2026, 10, 2, 23, 30, 0, DateTimeKind.Utc),
            new(2026, 10, 3, 7, 0, 0, DateTimeKind.Utc),
        ];

        private readonly Dictionary<int, List<(string Text, long Ticks, int Order)>> accepted = reports.ToDictionary(report => report.ReportId, _ => new List<(string, long, int)>());

        private int added;

        public IEnumerable<PlannedAdd> RandomAdds(Random random, int howMany)
        {
            for (var i = 0; i < howMany; i++)
            {
                var report = reports[random.Next(reports.Count)];
                var heldBy = random.Next(0, 5) == 0 ? reports[(reports.ToList().IndexOf(report) + 1) % reports.Count].Owner : report.Owner;
                var blank = random.Next(0, 4) == 0;
                var caller = random.Next(0, 2) == 0 ? (ReportNoteCaller)new ReportNoteCaller.Anonymous() : signedIn;
                yield return new PlannedAdd(heldBy, report.ReportId, caller, blank ? "  \t " : $"note {++added}", Instants[random.Next(Instants.Length)], heldBy == report.Owner, blank);
            }
        }

        public void Accepts(PlannedAdd add)
        {
            if (add.ExpectedStatus == ReportOutcomeStatus.Done)
            {
                accepted[add.ReportId].Add((add.Text!, add.WrittenAt.Ticks, accepted.Values.Sum(list => list.Count)));
            }
        }

        public IEnumerable<(string Text, long Ticks)> NewestFirst(int reportId)
            => accepted[reportId].OrderByDescending(note => note.Ticks).ThenByDescending(note => note.Order).Select(note => (note.Text, note.Ticks));
    }

    internal sealed record PlannedAdd(ReportOwner Owner, int ReportId, ReportNoteCaller Caller, string? Text, DateTime WrittenAt, bool OwnerHoldsTheReport, bool IsBlank)
    {
        public ReportOutcomeStatus ExpectedStatus => !OwnerHoldsTheReport ? ReportOutcomeStatus.NotFound : IsBlank ? ReportOutcomeStatus.Refused : ReportOutcomeStatus.Done;

        public override string ToString() => $"adding \"{Text?.Trim()}\" at {WrittenAt:O} to report {ReportId} as {(OwnerHoldsTheReport ? "its owner" : "another owner")}";
    }
}
