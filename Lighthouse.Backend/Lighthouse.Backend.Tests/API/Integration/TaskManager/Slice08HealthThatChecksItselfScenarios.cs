using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.TaskManager
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic #5511 — Task Manager), slice 08: connection health that
    /// persists and checks itself. US-08A, US-08B, US-08C — AC-08A.1 … AC-08C.5.
    ///
    /// Driving ports: the System-Administrator-guarded connection health read and its Test connection
    /// action, both over HTTP; the scheduled refresh that feeds them; and the health prober, entered the
    /// way <c>UsageDataForwardingService</c> is entered — through the registered hosted service's own
    /// single-pass method, because the test host runs no background work and waiting out a schedule
    /// nobody can see is not a test.
    ///
    /// Two acceptance criteria are answered elsewhere, neither silently:
    ///
    /// AC-08B.8 (the header badge reflects a probe-derived failure with no frontend change) is a
    /// frontend promise and already has its tests — <c>TaskManagerIcon.test.tsx</c> asserts the badge
    /// colour and count today. D21 recorded that the badge is verified rather than rebuilt, so the
    /// criterion here is that those tests keep passing untouched, which is a fact about the frontend
    /// suite and not a scenario this file can hold.
    ///
    /// AC-08C.3 (the regression test names the root cause of #6010 rather than the symptom) is the one
    /// criterion DISTILL could not author. See <c>Slice08AzureDevOpsRegression</c> below.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5511-task-manager")]
    [Category("slice-08")]
    public partial class Slice08HealthThatChecksItselfTest
    {
        // --- US-08A — a check that stays checked ---

        // @driving_port @real-io @AC-08A.1
        // The defect as the maintainer met it: press the button, get a green tick, come back after the
        // hourly refresh and the row reads "Not checked yet" with nothing on screen to say what undid it.
        [Test]
        public async Task A_check_survives_the_refresh_that_follows_it()
        {
            var connection = GivenAConnection();
            var team = GivenATeamOn(connection);
            GivenTheTrackerAnswersNormally();
            await WhenTheAdministratorTestsThatConnection(connection);
            await ThenThatConnectionReads(connection, "Healthy");

            await WhenTheScheduledRefreshOfThatTeamRuns(team);

            await ThenThatConnectionReads(connection, "Healthy");
            await ThenThatConnectionWasObservedNoEarlierThanTheCheckBeforeIt(connection);
        }

        // @driving_port @real-io @AC-08A.2
        // A refresh authenticated against the tracker and read real data from it. If that is not evidence
        // the credential works, nothing is — and the button, which reads one trivial record, is weaker.
        [Test]
        public async Task A_refresh_that_works_records_health_for_a_connection_nobody_checked()
        {
            var connection = GivenAConnection();
            var team = GivenATeamOn(connection);
            GivenTheTrackerAnswersNormally();

            await WhenTheScheduledRefreshOfThatTeamRuns(team);

            await ThenThatConnectionReads(connection, "Healthy");
        }

        // @driving_port @real-io @AC-08A.3
        // Recovery has to be as visible as failure. This scenario replaces slice 05's
        // A_refresh_that_works_clears_the_failure_before_it_without_claiming_health, whose expected
        // outcome D19 reverses — see the note in the Specifications file.
        [Test]
        public async Task A_refresh_that_works_replaces_the_failure_before_it()
        {
            var connection = GivenAConnection();
            var team = GivenATeamOn(connection);
            GivenTheTrackerRejectsTheCredential();
            await WhenTheScheduledRefreshOfThatTeamRuns(team);
            await ThenThatConnectionReads(connection, "AuthenticationFailed");

            GivenTheTrackerAnswersNormally();
            await WhenTheScheduledRefreshOfThatTeamRuns(team);

            await ThenThatConnectionReads(connection, "Healthy");
        }

        // @driving_port @real-io @AC-08A.4
        // The defect being fixed is a delete. A delete followed by an insert leaves exactly the state this
        // slice wants and is still the bug, so the end state cannot be what proves it gone — the row's own
        // identity is the only thing that can, and that is why this scenario reads the stored row.
        [Test]
        public async Task A_successful_refresh_updates_the_verdict_rather_than_deleting_it()
        {
            var connection = GivenAConnection();
            var team = GivenATeamOn(connection);
            GivenTheTrackerAnswersNormally();
            await WhenTheAdministratorTestsThatConnection(connection);
            var theRowThatWasRecorded = TheStoredVerdictIdFor(connection);

            await WhenTheScheduledRefreshOfThatTeamRuns(team);

            ThenTheStoredVerdictIsStillTheSameRow(connection, theRowThatWasRecorded);
        }

        // @driving_port @real-io @error @AC-08A.5
        // Unchanged from slice 05 and re-asserted here because D19 rewrites the method next to it. A
        // cancel is not a verdict: somebody chose to stop it, nothing was learned about the credential,
        // and recording health from it would claim a round trip that never finished.
        [Test]
        public async Task A_refresh_an_operator_stopped_still_says_nothing_about_the_credential()
        {
            var connection = GivenAConnection();
            var team = GivenATeamOn(connection);
            GivenTheTrackerNeverAnswers();

            await WhenTheRefreshOfThatTeamIsStoppedMidFlight(team);

            await ThenThatConnectionReads(connection, "Unknown");
        }

        // @driving_port @real-io @error @AC-08A.6
        // The one way D19 could paint a tick over something broken. Describe folds the OAuth credential
        // row in, and a refresh that succeeded says nothing about a grant that is separately disconnected.
        [Test]
        public async Task A_broken_oauth_grant_outlives_a_refresh_that_worked()
        {
            var connection = GivenAnOAuthConnectionWhoseGrantIsBroken();
            var team = GivenATeamOn(connection);
            GivenTheTrackerAnswersNormally();

            await WhenTheScheduledRefreshOfThatTeamRuns(team);

            await ThenThatConnectionReads(connection, "AuthenticationFailed");
        }

        // @driving_port @real-io @error @AC-08A.7
        // The refresh has already done its work and written its row by the time health is recorded.
        // Failing here would turn a completed refresh into a failed one over a verdict nobody asked for.
        [Test]
        public async Task A_health_verdict_that_cannot_be_recorded_does_not_fail_the_refresh()
        {
            var connection = GivenAConnection();
            var team = GivenATeamOn(connection);
            GivenTheTrackerAnswersNormally();
            GivenRecordingHealthThrows();

            await WhenTheScheduledRefreshOfThatTeamRuns(team);

            ThenTheRefreshItselfIsRecordedAsSucceeding(team);
            ThenTheBrowserWasToldTheRefreshCompleted();
        }

        // --- US-08B — Lighthouse checks, so I do not have to ---

        // @walking_skeleton @driving_adapter @real-io @AC-08B.1
        // The wiring, entered the way a running instance enters it. A prober that works when a test calls
        // the service directly and is registered nowhere is the defect this scenario exists to make
        // impossible — the slice's whole promise is that nobody has to press anything.
        [Test]
        public async Task An_instance_left_running_checks_a_connection_nobody_clicked()
        {
            var connection = GivenAConnection();
            GivenTheTrackerAnswersNormally();

            await WhenTheInstanceChecksWhatItHasNotHeardFrom();

            await ThenThatConnectionReads(connection, "Healthy");
        }

        // @driving_port @real-io @AC-08B.2
        [Test]
        public async Task A_connection_whose_answer_has_gone_stale_is_asked_again()
        {
            var connection = GivenAConnection();
            GivenTheTrackerAnswersNormally();
            await WhenTheInstanceChecksWhatItHasNotHeardFrom();

            GivenTheTrackerRejectsTheCredential();
            GivenTheThresholdHasPassed();
            await WhenTheInstanceChecksWhatItHasNotHeardFrom();

            await ThenThatConnectionReads(connection, "AuthenticationFailed");
        }

        // @driving_port @real-io @AC-08B.3
        // The criterion that separates this from the loop D9 refused, and the only one a naive "probe
        // everything on a timer" fails. It is asserted as an absence of a call rather than as a state,
        // because the state after a redundant probe is identical to the state after no probe at all.
        [Test]
        public async Task A_connection_answered_recently_is_left_alone()
        {
            GivenAConnection();
            GivenTheTrackerAnswersNormally();
            await WhenTheInstanceChecksWhatItHasNotHeardFrom();

            GivenTheTrackerIsWatchedFromNowOn();
            await WhenTheInstanceChecksWhatItHasNotHeardFrom();

            ThenTheTrackerWasNotAskedAgain();
        }

        // @driving_port @real-io @AC-08B.3
        // The economic claim the whole reopening of D9 rests on: because a successful refresh now records
        // health, a connection something refreshes never goes stale and therefore costs no probe at all.
        // Measured as OUT-5511-08-probe-cost. If this scenario fails, D20 is not the decision that was
        // taken — it is the recurring outbound loop D9 refused, wearing a staleness check.
        [Test]
        public async Task A_connection_a_refresh_keeps_fresh_is_never_probed()
        {
            var connection = GivenAConnection();
            var team = GivenATeamOn(connection);
            GivenTheTrackerAnswersNormally();
            await WhenTheScheduledRefreshOfThatTeamRuns(team);

            GivenTheTrackerIsWatchedFromNowOn();
            await WhenTheInstanceChecksWhatItHasNotHeardFrom();

            ThenTheTrackerWasNotAskedAgain();
            await ThenThatConnectionReads(connection, "Healthy");
        }

        // @driving_port @real-io @error @AC-08B.4
        [Test]
        public async Task A_probe_the_tracker_refuses_is_recorded_as_a_credential_problem()
        {
            var connection = GivenAConnection();
            GivenTheTrackerRejectsTheCredential();

            await WhenTheInstanceChecksWhatItHasNotHeardFrom();

            await ThenThatConnectionReads(connection, "AuthenticationFailed");
        }

        // @driving_port @real-io @error @AC-08B.4
        // A probe that ran is evidence, whatever it found. Leaving the connection Unknown because the
        // answer was bad would put the one state that means "nobody has asked" over a question that was
        // asked and answered — which is how Unknown stops being readable and starts hiding faults.
        [Test]
        public async Task A_probe_that_cannot_reach_the_tracker_says_unreachable_rather_than_nothing()
        {
            var connection = GivenAConnection();
            GivenTheTrackerFailsWithoutSayingWhy();

            await WhenTheInstanceChecksWhatItHasNotHeardFrom();

            await ThenThatConnectionReads(connection, "Unreachable");
        }

        // @driving_port @real-io @error @AC-08B.5
        // One connector raising instead of returning must not silence every other connection's health.
        // This is about the loop's resilience and is not the fix for #6010 (D28) — US-08C is that.
        [Test]
        public async Task A_connector_that_throws_does_not_silence_the_other_connections()
        {
            var raises = GivenAConnection();
            var answers = GivenAConnection();
            GivenTheTrackerThrowsFor(raises);
            GivenTheTrackerAnswersNormallyFor(answers);

            await WhenTheInstanceChecksWhatItHasNotHeardFrom();

            await ThenThatConnectionReads(answers, "Healthy");
        }

        // @driving_port @real-io @AC-08B.6
        // A verdict outlives the process that observed it, so a fresh instance does not start from
        // nothing and must not re-ask every tracker on the way up. Startup is the loop's first tick, not
        // a separate mechanism — which is only true if the first tick obeys the same staleness rule.
        [Test]
        public async Task A_verdict_that_outlived_the_process_is_not_asked_again_at_startup()
        {
            GivenAConnectionCheckedRecentlyByAnEarlierProcess();
            GivenTheTrackerIsWatchedFromNowOn();

            await WhenTheInstanceChecksWhatItHasNotHeardFrom();

            ThenTheTrackerWasNotAskedAgain();
        }

        // @driving_port @real-io @error @AC-08B.7
        // Two replicas both find the connection stale on the same tick. The verdict row is the claim, so
        // one of them wins it and the other finds nothing to do. Run through two service scopes against
        // the real database, because the unique index and the conditional update are database behaviour
        // and a doubled repository asserts nothing about either.
        [Test]
        public async Task Two_replicas_that_both_find_a_connection_stale_ask_it_once()
        {
            var connection = GivenAConnection();
            GivenTheTrackerAnswersNormally();

            await WhenTwoInstancesCheckAtTheSameMoment();

            ThenTheTrackerWasAskedExactlyOnce();
            await ThenThatConnectionReads(connection, "Healthy");
        }

        // --- US-08C — testing an Azure DevOps connection answers instead of failing ---

        // @driving_port @real-io @error @AC-08C.1 @AC-08C.2
        // Bug #6010 through the real Azure DevOps connector rather than the harness double, because a
        // double cannot reproduce a defect that lives in the connector or in the classification above it.
        // The host does not exist, so nothing reaches a real tracker and no credential is needed: the
        // promise is that an unreachable Azure DevOps instance is answered, not thrown.
        [Test]
        public async Task Testing_an_azure_devops_connection_answers_a_verdict_rather_than_failing()
        {
            var connection = GivenARealAzureDevOpsConnectionPointingNowhere();

            var answer = await WhenTheAdministratorTestsThatConnectionForReal(connection);

            ThenTheTestAnsweredRatherThanFailed(answer);
        }

        // @driving_port @real-io @error @AC-08C.2
        // The one line in the Azure DevOps connector that sits outside its own try, and therefore the
        // cheapest candidate for #6010's 500: the Url lookup throws ArgumentException when the option row
        // is absent, and nothing between there and the controller catches it. Needs no network at all.
        [Test]
        public async Task Testing_an_azure_devops_connection_with_no_url_answers_rather_than_throwing()
        {
            var connection = GivenARealAzureDevOpsConnectionWithNoUrlRecorded();

            var answer = await WhenTheAdministratorTestsThatConnectionForReal(connection);

            ThenTheTestAnsweredRatherThanFailed(answer);
        }

        // @driving_port @real-io @AC-08C.4
        // Whatever the fix turns out to be it lands in shared code, so every connection type is
        // downstream of it. The positive control on the guard: a fix that answers by swallowing would
        // also swallow this, and this one has a verdict to lose.
        [Test]
        public async Task Testing_a_connection_that_works_still_answers_that_it_is_healthy()
        {
            var connection = GivenAConnection();
            GivenTheTrackerAnswersNormally();

            await WhenTheAdministratorTestsThatConnection(connection);

            ThenTheTestItselfAnswered("Healthy");
        }

        // @driving_port @real-io @error @AC-08C.5
        // Regression guard on slice 05. A guard added to stop #6010's throw reaching the controller is
        // exactly the kind of change that turns a named, actionable message into "something went wrong",
        // and this message is the one that stops an administrator reissuing a credential that was intact.
        [Test]
        public async Task A_connection_with_an_unreadable_secret_still_names_the_field_to_enter_again()
        {
            var connection = GivenAConnectionWhoseStoredSecretCannotBeRead();

            await WhenTheAdministratorTestsThatConnection(connection);

            await ThenThatConnectionIsExplainedAsAKeyThisInstanceLost(connection);
        }
    }
}
