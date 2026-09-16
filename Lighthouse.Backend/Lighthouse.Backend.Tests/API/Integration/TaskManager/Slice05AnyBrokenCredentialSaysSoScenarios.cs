using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.TaskManager
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic #5511 — Task Manager), slice 05 / ADO #5019: any broken
    /// credential says so, not just OAuth. Driving ports: the System-Administrator-guarded connection
    /// health read and its Test connection action, both over HTTP, and the scheduled refresh that
    /// feeds them. US-05, AC-05.1 … AC-05.8.
    ///
    /// The observable is what an administrator is told about one connection. Everything on the path
    /// stays production — the update queue, the refresh, the verdict row and the read — and only the
    /// work tracking system itself is faked, because a scenario about a 401 cannot ask a real tracker
    /// to issue one.
    ///
    /// Two acceptance criteria are answered elsewhere, neither silently:
    ///
    /// AC-05.6 (<c>OAuthHealthIcon</c> is deleted, its badge folded into the activity icon) and
    /// AC-05.7 (the icon's colour and tooltip) are frontend promises and live in the icon's own specs.
    /// The backend half of AC-05.7 — that the read carries enough to colour the badge — is the state
    /// on every row, which every scenario below asserts.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5511-task-manager")]
    [Category("slice-05")]
    public partial class Slice05AnyBrokenCredentialSaysSoTest
    {
        // @walking_skeleton @driving_port @real-io @error @AC-05.1 @AC-05.2
        // The whole slice in one line: a connection that authenticates with a PAT rather than OAuth has
        // no OAuthCredential row at all, so today it is invisible to the only health signal that exists.
        [Test]
        public async Task A_refresh_that_fails_on_a_rejected_credential_says_the_credential_was_rejected()
        {
            var connection = GivenAConnectionAuthenticatingWithAToken();
            var team = GivenATeamOn(connection);
            GivenTheTrackerRejectsTheCredential();

            await WhenTheScheduledRefreshOfThatTeamRuns(team);

            await ThenThatConnectionReads(connection, "AuthenticationFailed");
        }

        // @driving_port @real-io @error @AC-05.2
        // The guard on the whole slice. Linear's ValidateConnection answers `validation_failed` for
        // everything, so nothing there can be told from anything else — and reporting that as a rejected
        // credential sends an administrator to reissue a key that was never the problem.
        [Test]
        public async Task A_refresh_that_fails_where_the_tracker_cannot_say_why_reads_unreachable_not_rejected()
        {
            var connection = GivenAConnectionAuthenticatingWithAToken();
            var team = GivenATeamOn(connection);
            GivenTheTrackerFailsWithoutSayingWhy();

            await WhenTheScheduledRefreshOfThatTeamRuns(team);

            await ThenThatConnectionReads(connection, "Unreachable");
        }

        // @driving_port @real-io @AC-05.1 @AC-05.3
        // Claiming health from an absence of evidence is how the icon this slice deletes came to mislead.
        [Test]
        public async Task A_connection_nothing_has_been_observed_about_is_listed_without_claiming_it_is_healthy()
        {
            var connection = GivenAConnectionAuthenticatingWithAToken();

            await ThenThatConnectionReads(connection, "Unknown");
        }

        // @driving_port @real-io @AC-05.3
        // A refresh that reached the tracker and came back is evidence the credential was accepted, so a
        // failure recorded before it no longer describes this connection.
        //
        // This scenario used to end on "Unknown", on the reasoning that a refresh clears a failure but
        // only Test connection may claim health. Slice 08's D19 reversed that: a refresh authenticated
        // and read real data, which is stronger evidence than the button, and erasing the verdict is what
        // sent an administrator who had just checked a connection back to "not checked yet" after the
        // next hourly refresh. The promise this scenario guards is unchanged — a recorded failure must
        // not outlive the good refresh that follows it — only the state it lands on.
        [Test]
        public async Task A_refresh_that_works_replaces_the_failure_before_it()
        {
            var connection = GivenAConnectionAuthenticatingWithAToken();
            var team = GivenATeamOn(connection);
            GivenTheTrackerRejectsTheCredential();
            await WhenTheScheduledRefreshOfThatTeamRuns(team);

            GivenTheTrackerAnswersNormally();
            await WhenTheScheduledRefreshOfThatTeamRuns(team);

            await ThenThatConnectionReads(connection, "Healthy");
        }

        // @driving_port @real-io @error @AC-05.2
        // A cancel is not a verdict. The operator chose it, nothing was learned about the credential, and
        // a row reading "authentication failed" against a connection somebody had just protected from a
        // rate limit is worse than saying nothing.
        [Test]
        public async Task A_refresh_an_operator_stopped_says_nothing_about_the_credential()
        {
            var connection = GivenAConnectionAuthenticatingWithAToken();
            var team = GivenATeamOn(connection);
            GivenTheTrackerNeverAnswers();

            await WhenTheRefreshOfThatTeamIsStoppedMidFlight(team);

            await ThenThatConnectionReads(connection, "Unknown");
        }

        // @driving_port @real-io @error @AC-05.2
        // The exact harm BuildUnreadableSecretReason exists to prevent, arriving through a new door. A
        // secret this instance can no longer decrypt must never be handed to the tracker to be refused:
        // the tracker would answer 401 and Lighthouse would blame a credential that is intact.
        [Test]
        public async Task A_credential_this_instance_can_no_longer_read_is_never_offered_to_the_tracker()
        {
            var connection = GivenAConnectionWhoseStoredSecretCannotBeRead();

            await WhenTheAdministratorTestsThatConnection(connection);

            await ThenThatConnectionReads(connection, "AuthenticationFailed");
            ThenTheTrackerWasNeverAsked();
        }

        // @driving_port @real-io @error @AC-05.2
        // Saying the credential needs attention is only half the answer, and the wrong half on its own: the
        // administrator has to be told it is this instance that cannot read the secret, and which field to
        // enter again. Told only "authentication failed" they go to the tracker and reissue a token that
        // was never refused.
        [Test]
        public async Task A_credential_this_instance_can_no_longer_read_says_which_field_to_enter_again()
        {
            var connection = GivenAConnectionWhoseStoredSecretCannotBeRead();

            await WhenTheAdministratorTestsThatConnection(connection);

            await ThenThatConnectionIsExplainedAsAKeyThisInstanceLost(connection);
        }

        // @driving_port @real-io @error @AC-05.2
        // The row is the most recent answer, not the first one. A connection whose cause changes between
        // two failures and goes on showing the old one sends an administrator after the wrong problem —
        // and it is the failure mode a verdict that is only ever inserted, never replaced, produces.
        [Test]
        public async Task A_connection_that_fails_twice_says_why_it_failed_the_second_time()
        {
            var connection = GivenAConnectionAuthenticatingWithAToken();
            var team = GivenATeamOn(connection);

            GivenTheTrackerFailsWithoutSayingWhy();
            await WhenTheScheduledRefreshOfThatTeamRuns(team);
            await ThenThatConnectionReads(connection, "Unreachable");

            GivenTheTrackerRejectsTheCredential();
            await WhenTheScheduledRefreshOfThatTeamRuns(team);

            await ThenThatConnectionReads(connection, "AuthenticationFailed");
        }

        // @driving_port @real-io @error @AC-05.4 @AC-05.5
        // Test connection answers with the same folding the list uses. A tracker that accepts the request
        // says nothing about an OAuth grant Lighthouse already knows is broken, so a button that reported
        // "Healthy" here would contradict the row it was pressed from.
        [Test]
        public async Task Testing_an_oauth_connection_whose_grant_is_broken_still_says_it_needs_reconnecting()
        {
            var connection = GivenAnOAuthConnectionWhoseGrantIsBroken();
            GivenTheTrackerAnswersNormally();

            await WhenTheAdministratorTestsThatConnection(connection);

            ThenTheTestItselfAnswered("AuthenticationFailed");
            await ThenThatConnectionReads(connection, "AuthenticationFailed");
            await ThenThatConnectionIsExplainedWithAReconnect(connection);
        }

        // @driving_port @real-io @AC-05.4
        [Test]
        public async Task Testing_a_connection_asks_that_tracker_once_and_records_what_it_answered()
        {
            var connection = GivenAConnectionAuthenticatingWithAToken();
            GivenTheTrackerAnswersNormally();

            await WhenTheAdministratorTestsThatConnection(connection);

            ThenTheTestItselfAnswered("Healthy");
            await ThenThatConnectionReads(connection, "Healthy");
            ThenTheTrackerWasAskedExactlyOnce();
        }

        // @driving_port @real-io @error @AC-05.4
        // Testing one connection must not touch the others, which is the whole difference between this
        // and the probe loop D9 refused.
        [Test]
        public async Task Testing_one_connection_leaves_every_other_connection_as_it_found_it()
        {
            var tested = GivenAConnectionAuthenticatingWithAToken();
            var untouched = GivenAConnectionAuthenticatingWithAToken();
            GivenTheTrackerAnswersNormally();

            await WhenTheAdministratorTestsThatConnection(tested);

            await ThenThatConnectionReads(tested, "Healthy");
            await ThenThatConnectionReads(untouched, "Unknown");
        }

        // @driving_port @real-io @error @AC-05.4
        [Test]
        public async Task Testing_a_connection_that_is_no_longer_there_is_refused_rather_than_answered()
        {
            await ThenTestingAConnectionThatDoesNotExistIsRefused();
        }

        // @driving_port @real-io @error @AC-05.5
        // The behaviour OAuthHealthIcon shipped, arriving at the new surface unchanged. Deleting the icon
        // in this slice is only allowed because this holds.
        [Test]
        public async Task An_oauth_connection_that_lost_its_grant_still_says_it_needs_reconnecting()
        {
            var connection = GivenAnOAuthConnectionWhoseGrantIsBroken();

            await ThenThatConnectionReads(connection, "AuthenticationFailed");
            await ThenThatConnectionIsExplainedWithAReconnect(connection);
        }

        // @driving_port @real-io @AC-05.5
        // The positive control. Without it, "every OAuth connection needs reconnecting" satisfies the
        // scenario above and nothing catches it.
        [Test]
        public async Task An_oauth_connection_whose_grant_is_intact_is_not_reported_as_broken()
        {
            var connection = GivenAnOAuthConnectionWhoseGrantIsIntact();

            await ThenThatConnectionReads(connection, "Unknown");
        }

        // @driving_port @real-io @error @AC-05.5
        // Which of the two sources wins when they disagree. A live 401 against an OAuth connection whose
        // stored grant still reads Valid is the tracker telling the truth later than the credential row.
        [Test]
        public async Task A_rejected_refresh_outranks_an_oauth_grant_that_still_believes_it_is_valid()
        {
            var connection = GivenAnOAuthConnectionWhoseGrantIsIntact();
            var team = GivenATeamOn(connection);
            GivenTheTrackerRejectsTheCredential();

            await WhenTheScheduledRefreshOfThatTeamRuns(team);

            await ThenThatConnectionReads(connection, "AuthenticationFailed");
        }

        // @driving_port @real-io @AC-05.1
        // AC-05.1 is a claim about the list, not about a row: a connection that has no OAuthCredential row
        // is exactly the one the old aggregator could not see, and it has to appear beside one that does.
        [Test]
        public async Task Every_connection_is_listed_whatever_it_authenticates_with()
        {
            var token = GivenAConnectionAuthenticatingWithAToken();
            var oauth = GivenAnOAuthConnectionWhoseGrantIsIntact();

            await ThenTheHealthListNamesExactly(token, oauth);
        }

        // @driving_port @real-io @error @AC-05.8
        [Test]
        public async Task Connection_names_are_not_handed_to_somebody_who_is_not_an_administrator()
        {
            await ThenBothConnectionHealthRoutesRefuseANonAdministratorTheWayTheRefreshLogDoes();
        }
    }
}
