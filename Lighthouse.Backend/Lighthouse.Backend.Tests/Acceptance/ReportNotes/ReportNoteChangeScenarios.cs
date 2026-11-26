using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Modules.Reports.Domain;
using Lighthouse.Backend.Tests.API.Integration.Reports;

namespace Lighthouse.Backend.Tests.Acceptance.ReportNotes
{
    /// <summary>
    /// The author of a note changes or deletes it and the report shows the change, a note nobody signed may be
    /// changed or deleted by any writer, and a signed note stays its author's, whoever else may write, whether
    /// sign-in is on or off now. Whoever may change a note is told so beside the note, and a direct request is
    /// answered the same way. A note that is gone is found missing before anything else is judged.
    ///
    /// Driving ports: changing, deleting and listing the notes of a report, with no web host.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    // The test project has no property library: the generated check below draws from a seeded Random, prints the
    // seed in every failure message and is replayed by running again with that seed. It does not shrink a failing case.
    public class ReportNoteChangeScenarios : ReportsAtThePortTest
    {
        private const string Ann = "Ann";

        private const string Bob = "Bob";

        private const string Cy = "Cy";

        private const string TeamGrew = "Team grew\nto 9";

        private const string Training = "Training took place";

        private const string TrainingMoved = "Training moved to Monday";

        private readonly Dictionary<string, ReportNoteCaller> signedIn = [];

        [SetUp]
        public void ForgetTheSignersOfTheLastScenario() => signedIn.Clear();

        // A signer is a stored profile of this scenario's own database, because a note's author is a link to a profile.
        private ReportNoteCaller SignedInAs(string name)
        {
            if (!signedIn.TryGetValue(name, out var caller))
            {
                caller = new ReportNoteCaller.Signed(ASignedInProfile(name), name);
                signedIn[name] = caller;
            }

            return caller;
        }

        // A name is somebody signed in; no name is sign-in being off.
        private ReportNoteCaller Caller(string? name) => name is null ? SignInIsOff : SignedInAs(name);

        private static readonly ReportNoteCaller SignInIsOff = new ReportNoteCaller.Anonymous();

        private static readonly ReportNoteCaller SignedInButNotKnown = new ReportNoteCaller.Unresolved();

        // 09:00 on Friday 2 Oct 2026 in Zurich, 01:30 on Saturday 3 Oct there, and 09:00 on Sunday 4 Oct there.
        private static readonly DateTime MorningOnThe2nd = new(2026, 10, 2, 7, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime LateEveningUtcOnThe2nd = new(2026, 10, 2, 23, 30, 0, DateTimeKind.Utc);

        private static readonly DateTime MorningOnThe4th = new(2026, 10, 4, 7, 0, 0, DateTimeKind.Utc);

        private static readonly DateOnly The2nd = new(2026, 10, 2);

        private static readonly DateOnly The3rd = new(2026, 10, 3);

        private static readonly DateOnly The4th = new(2026, 10, 4);

        [TestCase("team")]
        [TestCase("portfolio")]
        public async Task The_author_changes_their_note_and_the_report_shows_the_new_text_marked_as_edited_with_the_day_in_the_same_place(string ownerKind)
        {
            var owner = AnOwner(ownerKind);
            var report = await AReportOf(owner);
            var older = await AddANote(owner, report, SignedInAs(Ann), Training, MorningOnThe2nd);
            await AddANote(owner, report, SignedInAs(Ann), TeamGrew, LateEveningUtcOnThe2nd);

            var changed = await ChangeANote(owner, report, IdOf(older), SignedInAs(Ann), $"  {TrainingMoved}\n ", MorningOnThe4th);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(changed.Status, Is.EqualTo(ReportOutcomeStatus.Done), $"the note was not changed: {changed.Code}");
                Assert.That(changed.Value?.Text, Is.EqualTo(TrainingMoved), "the changed note is answered with its text without the outer blanks");
                Assert.That(TheNotesListed(owner, report), Is.EqualTo(new[]
                {
                    new ShownNote(TeamGrew, The3rd, null, Ann),
                    new ShownNote(TrainingMoved, The2nd, The4th, Ann),
                }), "the change keeps the note where it was written and keeps its writer, and the other note is not marked");
            }
        }

        [Test]
        public async Task A_change_that_keeps_the_same_text_still_marks_the_note_as_edited_with_the_day()
        {
            var owner = ATeam("Lightspeed");
            var report = await AReportOf(owner);
            var note = await AddANote(owner, report, SignedInAs(Ann), Training, MorningOnThe2nd);

            var changed = await ChangeANote(owner, report, IdOf(note), SignedInAs(Ann), Training, MorningOnThe4th);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(changed.Status, Is.EqualTo(ReportOutcomeStatus.Done), $"the note was not changed: {changed.Code}");
                Assert.That(TheNotesListed(owner, report), Is.EqualTo(new[] { new ShownNote(Training, The2nd, The4th, Ann) }));
            }
        }

        [TestCase("team")]
        [TestCase("portfolio")]
        public async Task The_author_deletes_their_note_and_the_report_no_longer_lists_it_and_keeps_the_others(string ownerKind)
        {
            var owner = AnOwner(ownerKind);
            var report = await AReportOf(owner);
            var older = await AddANote(owner, report, SignedInAs(Ann), Training, MorningOnThe2nd);
            await AddANote(owner, report, SignedInAs(Ann), TeamGrew, LateEveningUtcOnThe2nd);

            var deleted = await DeleteANote(owner, report, IdOf(older), SignedInAs(Ann));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(deleted.Status, Is.EqualTo(ReportOutcomeStatus.Done), $"the note was not deleted: {deleted.Code}");
                Assert.That(deleted.Value?.Text, Is.EqualTo(Training), "the deleted note is answered, so a caller can say which note went");
                Assert.That(TheNotesListed(owner, report), Is.EqualTo(new[] { new ShownNote(TeamGrew, The3rd, null, Ann) }));
            }
        }

        [Test]
        public async Task A_writer_who_is_not_the_author_is_refused_a_change_and_a_delete_and_the_note_stays_as_written()
        {
            var owner = ATeam("Lightspeed");
            var report = await AReportOf(owner);
            var note = await AddANote(owner, report, SignedInAs(Ann), Training, MorningOnThe2nd);

            var changed = await ChangeANote(owner, report, IdOf(note), SignedInAs(Bob), TrainingMoved, MorningOnThe4th);
            var deleted = await DeleteANote(owner, report, IdOf(note), SignedInAs(Bob));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(changed.Status, Is.EqualTo(ReportOutcomeStatus.Refused));
                Assert.That(changed.Code, Is.EqualTo(ReportRefusalCodes.NoteNotYours), "a change by somebody else is refused because the note is not theirs");
                Assert.That(deleted.Status, Is.EqualTo(ReportOutcomeStatus.Refused));
                Assert.That(deleted.Code, Is.EqualTo(ReportRefusalCodes.NoteNotYours), "a delete by somebody else is refused because the note is not theirs");
                Assert.That(TheNotesListed(owner, report), Is.EqualTo(new[] { new ShownNote(Training, The2nd, null, Ann) }));
                Assert.That(TheNotesAsOfferedTo(owner, report, SignedInAs(Bob)), Is.EqualTo(new[] { (Training, false) }), "the other writer is shown no control on it");
                Assert.That(TheNotesAsOfferedTo(owner, report, SignedInAs(Ann)), Is.EqualTo(new[] { (Training, true) }), "the author is shown the controls");
            }
        }

        [TestCase(Bob)]
        [TestCase(null)]
        public async Task A_note_nobody_signed_is_changed_and_deleted_by_any_writer_whether_sign_in_is_on_or_off_now(string? writer)
        {
            var owner = ATeam("Lightspeed");
            var report = await AReportOf(owner);
            var toChange = await AddANote(owner, report, SignInIsOff, Training, MorningOnThe2nd);
            var toDelete = await AddANote(owner, report, SignInIsOff, TeamGrew, LateEveningUtcOnThe2nd);

            var offered = TheNotesAsOfferedTo(owner, report, Caller(writer));
            var changed = await ChangeANote(owner, report, IdOf(toChange), Caller(writer), TrainingMoved, MorningOnThe4th);
            var deleted = await DeleteANote(owner, report, IdOf(toDelete), Caller(writer));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(offered, Is.EqualTo(new[] { (TeamGrew, true), (Training, true) }), "every writer is shown the controls on a note nobody signed");
                Assert.That(changed.Status, Is.EqualTo(ReportOutcomeStatus.Done), $"the note was not changed: {changed.Code}");
                Assert.That(deleted.Status, Is.EqualTo(ReportOutcomeStatus.Done), $"the note was not deleted: {deleted.Code}");
                Assert.That(TheNotesListed(owner, report), Is.EqualTo(new[] { new ShownNote(TrainingMoved, The2nd, The4th, null) }));
            }
        }

        [Test]
        public async Task A_note_signed_while_sign_in_was_on_stays_its_authors_after_sign_in_is_switched_off()
        {
            var owner = ATeam("Lightspeed");
            var report = await AReportOf(owner);
            var note = await AddANote(owner, report, SignedInAs(Ann), Training, MorningOnThe2nd);

            var offered = TheNotesAsOfferedTo(owner, report, SignInIsOff);
            var changed = await ChangeANote(owner, report, IdOf(note), SignInIsOff, TrainingMoved, MorningOnThe4th);
            var deleted = await DeleteANote(owner, report, IdOf(note), SignInIsOff);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(offered, Is.EqualTo(new[] { (Training, false) }), "no control is shown on it with sign-in off");
                Assert.That(changed.Status, Is.EqualTo(ReportOutcomeStatus.Refused));
                Assert.That(changed.Code, Is.EqualTo(ReportRefusalCodes.NoteNotYours), "refused because the note is signed, which is not the same as being refused for a different signer: nobody is signed in");
                Assert.That(deleted.Status, Is.EqualTo(ReportOutcomeStatus.Refused));
                Assert.That(deleted.Code, Is.EqualTo(ReportRefusalCodes.NoteNotYours));
                Assert.That(TheNotesListed(owner, report), Is.EqualTo(new[] { new ShownNote(Training, The2nd, null, Ann) }));
            }
        }

        [TestCase(Ann, Ann, true)]
        [TestCase(Ann, Bob, false)]
        [TestCase(null, Ann, true)]
        [TestCase(null, null, true)]
        [TestCase(Ann, null, false)]
        public async Task A_note_is_offered_a_change_and_a_delete_exactly_when_a_direct_request_for_them_is_accepted(string? signer, string? caller, bool mayChange)
        {
            var owner = ATeam("Lightspeed");
            var report = await AReportOf(owner);
            var toChange = await AddANote(owner, report, Caller(signer), Training, MorningOnThe2nd);
            var toDelete = await AddANote(owner, report, Caller(signer), TeamGrew, LateEveningUtcOnThe2nd);

            var offered = TheNotesAsOfferedTo(owner, report, Caller(caller));
            var changed = await ChangeANote(owner, report, IdOf(toChange), Caller(caller), TrainingMoved, MorningOnThe4th);
            var deleted = await DeleteANote(owner, report, IdOf(toDelete), Caller(caller));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(offered, Is.EqualTo(new[] { (TeamGrew, mayChange), (Training, mayChange) }), $"signer {signer ?? "nobody"}, caller {caller ?? "nobody"}");
                Assert.That(changed.Status == ReportOutcomeStatus.Done, Is.EqualTo(mayChange), $"the change was {changed.Status} {changed.Code}, which must agree with what the list offered");
                Assert.That(deleted.Status == ReportOutcomeStatus.Done, Is.EqualTo(mayChange), $"the delete was {deleted.Status} {deleted.Code}, which must agree with what the list offered");
                Assert.That(mayChange || (changed.Code == ReportRefusalCodes.NoteNotYours && deleted.Code == ReportRefusalCodes.NoteNotYours), Is.True, "a refusal names the note as not theirs");
            }
        }

        [Test]
        public async Task Signed_in_with_nobody_nameable_a_change_and_a_delete_are_refused_for_that_reason_whoever_signed_the_note_and_the_notes_stay_as_they_were()
        {
            var owner = ATeam("Lightspeed");
            var report = await AReportOf(owner);
            var signed = await AddANote(owner, report, SignedInAs(Ann), Training, MorningOnThe2nd);
            var unsigned = await AddANote(owner, report, SignInIsOff, TeamGrew, LateEveningUtcOnThe2nd);

            var changed = await ChangeANote(owner, report, IdOf(signed), SignedInButNotKnown, TrainingMoved, MorningOnThe4th);
            var deleted = await DeleteANote(owner, report, IdOf(unsigned), SignedInButNotKnown);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(changed.Status, Is.EqualTo(ReportOutcomeStatus.Refused));
                Assert.That(changed.Code, Is.EqualTo(ReportRefusalCodes.NoteAuthorUnknown));
                Assert.That(deleted.Status, Is.EqualTo(ReportOutcomeStatus.Refused));
                Assert.That(deleted.Code, Is.EqualTo(ReportRefusalCodes.NoteAuthorUnknown));
                Assert.That(TheNotesListed(owner, report), Is.EqualTo(new[]
                {
                    new ShownNote(TeamGrew, The3rd, null, null),
                    new ShownNote(Training, The2nd, null, Ann),
                }));
            }
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\t \r\n ")]
        [TestCase(null)]
        public async Task A_change_to_no_text_is_not_saved_and_the_note_stays_as_it_was(string? text)
        {
            var owner = ATeam("Lightspeed");
            var report = await AReportOf(owner);
            var note = await AddANote(owner, report, SignedInAs(Ann), Training, MorningOnThe2nd);

            var refused = await ChangeANote(owner, report, IdOf(note), SignedInAs(Ann), text, MorningOnThe4th);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.Status, Is.EqualTo(ReportOutcomeStatus.Refused));
                Assert.That(refused.Code, Is.EqualTo(ReportRefusalCodes.NoteTextMissing));
                Assert.That(TheNotesListed(owner, report), Is.EqualTo(new[] { new ShownNote(Training, The2nd, null, Ann) }), "a refused change leaves no edited mark");
            }
        }

        [Test]
        public async Task A_change_to_more_than_2000_characters_is_refused_for_that_reason_while_one_of_exactly_2000_between_blanks_is_kept()
        {
            var owner = ATeam("Lightspeed");
            var report = await AReportOf(owner);
            var note = await AddANote(owner, report, SignedInAs(Ann), Training, MorningOnThe2nd);
            var longest = new string('x', ReportNote.LongestText);

            var refused = await ChangeANote(owner, report, IdOf(note), SignedInAs(Ann), longest + "x", MorningOnThe4th);
            var afterTheRefusal = TheNotesListed(owner, report);
            var kept = await ChangeANote(owner, report, IdOf(note), SignedInAs(Ann), $" {longest}\n", MorningOnThe4th);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.Status, Is.EqualTo(ReportOutcomeStatus.Refused));
                Assert.That(refused.Code, Is.EqualTo(ReportRefusalCodes.NoteTextTooLong));
                Assert.That(afterTheRefusal, Is.EqualTo(new[] { new ShownNote(Training, The2nd, null, Ann) }));
                Assert.That(kept.Status, Is.EqualTo(ReportOutcomeStatus.Done), $"2000 characters must be kept: {kept.Code}");
                Assert.That(TheNotesListed(owner, report), Is.EqualTo(new[] { new ShownNote(longest, The2nd, The4th, Ann) }));
            }
        }

        [Test]
        public async Task Who_may_change_a_note_is_judged_before_what_is_written()
        {
            var owner = ATeam("Lightspeed");
            var report = await AReportOf(owner);
            var note = await AddANote(owner, report, SignedInAs(Ann), Training, MorningOnThe2nd);

            var refused = await ChangeANote(owner, report, IdOf(note), SignedInAs(Bob), "   ", MorningOnThe4th);

            Assert.That(refused.Code, Is.EqualTo(ReportRefusalCodes.NoteNotYours),
                "somebody who may not change the note is refused for that, even before a missing text is looked at");
        }

        [Test]
        public async Task A_note_that_is_gone_is_found_missing_before_anything_else_is_judged_and_the_other_notes_stay()
        {
            var owner = ATeam("Lightspeed");
            var report = await AReportOf(owner);
            var gone = await AddANote(owner, report, SignedInAs(Ann), Training, MorningOnThe2nd);
            await AddANote(owner, report, SignedInAs(Ann), TeamGrew, LateEveningUtcOnThe2nd);
            await DeleteANote(owner, report, IdOf(gone), SignedInAs(Ann));

            var changed = await ChangeANote(owner, report, IdOf(gone), SignedInButNotKnown, "", MorningOnThe4th);
            var deletedAgain = await DeleteANote(owner, report, IdOf(gone), SignedInAs(Bob));
            var neverThere = await DeleteANote(owner, report, IdOf(gone) + 1000, SignedInAs(Bob));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(changed.Status, Is.EqualTo(ReportOutcomeStatus.NotFound));
                Assert.That(deletedAgain.Status, Is.EqualTo(ReportOutcomeStatus.NotFound));
                Assert.That(neverThere.Status, Is.EqualTo(ReportOutcomeStatus.NotFound));
                Assert.That(TheNotesListed(owner, report), Is.EqualTo(new[] { new ShownNote(TeamGrew, The3rd, null, Ann) }));
            }
        }

        [Test]
        public async Task A_note_is_found_missing_through_a_report_that_does_not_hold_it_or_an_owner_that_does_not_have_the_report()
        {
            var lightspeed = ATeam("Lightspeed");
            var zenith = ATeam("Zenith");
            var lightspeedReport = await AReportOf(lightspeed);
            var zenithReport = await AReportOf(zenith);
            var note = await AddANote(lightspeed, lightspeedReport, SignInIsOff, Training, MorningOnThe2nd);

            var onAnotherReport = await ChangeANote(zenith, zenithReport, IdOf(note), SignInIsOff, TrainingMoved, MorningOnThe4th);
            var ofAnotherOwner = await DeleteANote(zenith, lightspeedReport, IdOf(note), SignInIsOff);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(onAnotherReport.Status, Is.EqualTo(ReportOutcomeStatus.NotFound));
                Assert.That(ofAnotherOwner.Status, Is.EqualTo(ReportOutcomeStatus.NotFound));
                Assert.That(TheNotesListed(lightspeed, lightspeedReport), Is.EqualTo(new[] { new ShownNote(Training, The2nd, null, null) }));
            }
        }

        /// <summary>For every pairing of who signed a note and who asks, in both the change and the delete, a note nobody signed is changed by anyone and a signed note only by its signer.</summary>
        [Test]
        public async Task Every_pairing_of_signer_and_caller_is_accepted_only_for_an_unsigned_note_or_the_signer()
        {
            var owner = ATeam("Lightspeed");
            var report = await AReportOf(owner);
            string?[] people = [null, Ann, Bob, Cy];

            foreach (var signer in people)
            {
                foreach (var caller in people)
                {
                    var forChange = await AddANote(owner, report, Caller(signer), Training, MorningOnThe2nd);
                    var forDelete = await AddANote(owner, report, Caller(signer), TeamGrew, MorningOnThe2nd);

                    var changed = await ChangeANote(owner, report, IdOf(forChange), Caller(caller), TrainingMoved, MorningOnThe4th);
                    var deleted = await DeleteANote(owner, report, IdOf(forDelete), Caller(caller));

                    var expected = AModelOf(signer).MayBeChangedBy(caller);
                    using (Assert.EnterMultipleScope())
                    {
                        Assert.That(changed.Status == ReportOutcomeStatus.Done, Is.EqualTo(expected), $"change: signer {signer ?? "nobody"}, caller {caller ?? "nobody"}: {changed.Status} {changed.Code}");
                        Assert.That(deleted.Status == ReportOutcomeStatus.Done, Is.EqualTo(expected), $"delete: signer {signer ?? "nobody"}, caller {caller ?? "nobody"}: {deleted.Status} {deleted.Code}");
                    }
                }
            }
        }

        /// <summary>After any sequence of notes written by people signed in or not, and changes and deletes by any of them, each is accepted exactly as the signers say, and the report lists exactly the notes still held.</summary>
        [Test]
        public async Task Whatever_was_written_and_changed_before_a_change_is_accepted_only_for_an_unsigned_note_or_the_signer()
        {
            var owner = ATeam("Lightspeed");
            string?[] people = [null, Ann, Bob, Cy];
            var random = new Random(Seed);

            for (var run = 0; run < Runs; run++)
            {
                var report = await AReportOf(owner);
                var held = new List<(int Id, string? Signer)>();

                var steps = random.Next(0, MostSteps + 1);
                for (var step = 0; step < steps; step++)
                {
                    var caller = people[random.Next(people.Length)];
                    var what = held.Count == 0 ? 0 : random.Next(3);
                    if (what == 0)
                    {
                        var added = await AddANote(owner, report, Caller(caller), $"note {run}.{step}", MorningOnThe2nd.AddMinutes(step));
                        Assert.That(added.Status, Is.EqualTo(ReportOutcomeStatus.Done), $"seed {Seed}, run {run}, step {step}: {added.Code}");
                        held.Add((IdOf(added), caller));
                        continue;
                    }

                    var target = held[random.Next(held.Count)];
                    var expected = AModelOf(target.Signer).MayBeChangedBy(caller);
                    var outcome = what == 1
                        ? await ChangeANote(owner, report, target.Id, Caller(caller), $"changed {run}.{step}", MorningOnThe4th)
                        : await DeleteANote(owner, report, target.Id, Caller(caller));

                    Assert.That(outcome.Status == ReportOutcomeStatus.Done, Is.EqualTo(expected),
                        $"seed {Seed}, run {run}, step {step}: note signed by {target.Signer ?? "nobody"}, asked by {caller ?? "nobody"}: {outcome.Status} {outcome.Code}");
                    if (expected && what == 2)
                    {
                        held.Remove(target);
                    }
                }

                var listed = ReadTheNotes(owner, report);
                Assert.That(listed.Value!.Select(note => note.Id).Order(), Is.EqualTo(held.Select(note => note.Id).Order()),
                    $"seed {Seed}, run {run}: the report must list exactly the notes still held");
            }
        }

        private static int IdOf(ReportOutcome<ReportNoteRead> outcome) => outcome.Value!.Id;

        private const int Seed = 4711;

        private const int Runs = 15;

        private const int MostSteps = 10;

        private ReportOwner AnOwner(string ownerKind)
            => ownerKind == "team" ? ATeam("Team Lightspeed") : APortfolio("Project Apollo");

        // The rule written out from what a reader of the notes is promised, not taken from the code under test.
        private static SignedBy AModelOf(string? signer) => new(signer);

        private sealed record SignedBy(string? Signer)
        {
            public bool MayBeChangedBy(string? caller) => Signer is null || Signer == caller;
        }
    }
}
