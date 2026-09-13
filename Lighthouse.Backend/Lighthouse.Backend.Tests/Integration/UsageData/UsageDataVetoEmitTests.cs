using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Implementation.UsageData;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Integration.UsageData
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic 5733 slice 03, ADO #5836) - what the administrator's veto
    /// actually stops. US-06 (AC-06.3, AC-06.4, AC-06.5, AC-06.7).
    ///
    /// The veto is enforced where the data leaves, not where the button is. A browser that agreed
    /// before the veto was engaged, and has had a tab open ever since, keeps handing batches in; its
    /// request is accepted exactly as before and the batch is dropped. That is the difference between
    /// an administrator's screenshot being true and it being approximately true, and it is only
    /// checkable at the boundary - so every scenario here reads what did or did not reach the
    /// collector rather than what some component was asked to do.
    ///
    /// How that boundary is watched, and why nothing here can reach the real collector, lives in
    /// <see cref="UsageDataCollectorObservationTest"/>.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    [Category("slice-03")]
    public class UsageDataVetoEmitTests : UsageDataCollectorObservationTest
    {
        /// <summary>
        /// The control, and it is not optional. Every scenario below whose point is that nothing was
        /// sent is worthless against a pipe that sends nothing anyway, and a pipe that sends nothing
        /// anyway looks exactly like a veto working perfectly. This is what tells the two apart, and it
        /// is why it sits in the same fixture rather than being left to the pipe's own tests.
        /// </summary>
        [Test]
        public async Task With_nothing_vetoed_a_consenting_browser_reaches_the_collector()
        {
            var token = await ABrowserThatAgreedAsync();

            await HandInAndForgetTheAnswerAsync(token, ABatchOf(TabOpened, TeamMetricsTab));
            var received = await EverythingTheCollectorReceived();

            Assert.That(received, Does.Contain(TabOpened),
                "the batch this scenario handed in did not reach the collector, so every 'the veto "
                + "stopped it' scenario below would hold against a pipe that never carries this "
                + "event at all. Not-empty is too weak here: anything else the instance happened to "
                + "send would satisfy it");
        }

        /// <summary>
        /// The fail-open rule slice 02 already depends on, asserted here because this is the slice that
        /// introduces the row it is about. An instance whose row has not been seeded yet - every
        /// instance, between the release landing and the seeder running - behaves as though nothing
        /// were vetoed. The opposite reading would silently stop the feature on upgrade and report
        /// nothing, and the Epic's uptake number would come back zero for a reason nothing names.
        /// </summary>
        [Test]
        public async Task With_no_row_at_all_the_instance_behaves_as_though_nothing_were_vetoed()
        {
            GivenNoVetoRowExists();
            GivenTheInstanceIsOldEnoughToAsk();
            var token = await ABrowserThatAgreedAsync();

            await HandInAndForgetTheAnswerAsync(token, ABatchOf(TabOpened, TeamMetricsTab));
            var received = await EverythingTheCollectorReceived();

            // A browser that has already answered is never asked again whatever the veto says, so the
            // asking half has to be read through one that has not - otherwise this passes for the
            // wrong reason and would go on passing if an absent row started meaning "vetoed".
            var undecided = await TheStateThisBrowserIsToldAsync(token: null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(received, Does.Contain(TabOpened),
                    "an absent row was read as a veto, so a release stops sending the moment it lands "
                    + "and starts again whenever the seeder happens to run");
                Assert.That(Flag(undecided, "mayAsk"), Is.True,
                    "an absent row was read as a veto, so nobody is asked on an instance where no "
                    + "administrator has decided anything");
            }
        }

        // @AC-06.3 - the guarantee the whole slice exists for. The browser is not asked to stop and is
        // not told it was stopped; its request is accepted exactly as before and the batch is dropped.
        [Test]
        public async Task An_engaged_veto_drops_the_batch_of_a_browser_that_had_already_consented()
        {
            var token = await ABrowserThatAgreedAsync();
            GivenTheAdministratorHasEngagedTheVeto();

            using var answer = await HandInAsync(token, ABatchOf(TabOpened, TeamMetricsTab));
            var received = await EverythingTheCollectorReceived();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
                    "refusing the request would tell a browser its token was recognised, and would "
                    + "make a stale tab behave differently from a fresh one");
                Assert.That(received, Is.Empty,
                    "the administrator stopped usage data and this instance sent anyway. A tab open "
                    + "since before the switch was thrown is exactly the case this has to cover");
            }
        }

        // @AC-06.5 - suspended, never revoked. The browser that consented before the veto is not asked
        // anything when it is lifted; it simply starts being counted again.
        [Test]
        public async Task Lifting_the_veto_lets_the_same_browser_send_again_without_asking_it_anything()
        {
            var token = await ABrowserThatAgreedAsync();
            GivenTheAdministratorHasEngagedTheVeto();
            await HandInAndForgetTheAnswerAsync(token, ABatchOf(TabOpened, TeamMetricsTab));
            await EverythingTheCollectorReceived();

            GivenTheAdministratorHasLiftedTheVeto();
            await HandInAndForgetTheAnswerAsync(token, ABatchOf(TabOpened, TeamMetricsTab));
            var received = await EverythingTheCollectorReceived();
            var state = await TheStateThisBrowserIsToldAsync(token);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(received, Is.Not.Empty,
                    "the consent was treated as revoked rather than suspended, so lifting the veto "
                    + "leaves an instance where everybody has to be asked all over again");
                Assert.That(Text(state, "decision"), Is.EqualTo("Granted"),
                    "the browser's own answer was altered by somebody else's decision");
                Assert.That(Flag(state, "mayAsk"), Is.False,
                    "a browser that already agreed is being lined up to be asked again");
            }
        }

        // @AC-06.4 @AC-05.8 - the asking stops too, and it stops for a browser that would otherwise be
        // due. Suppressing only the sending would leave the dialog appearing on an instance whose
        // administrator has stopped the whole thing.
        [Test]
        public async Task An_engaged_veto_stops_the_question_being_put_at_all()
        {
            GivenTheInstanceIsOldEnoughToAsk();
            GivenTheAdministratorHasEngagedTheVeto();

            var state = await TheStateThisBrowserIsToldAsync(token: null);

            Assert.That(Flag(state, "mayAsk"), Is.False,
                "the dialog is still offered on an instance whose administrator stopped usage data, "
                + "which is the one thing an administrator using this switch is trying to prevent");
        }

        // @AC-06.7 @DT-25 - the indicator may not say data is being sent from a browser nothing is sent
        // from. The answer used to be derived from this browser's own decision alone, so a granted
        // browser was told "sending" while the emit path dropped everything it handed in.
        [Test]
        public async Task An_engaged_veto_makes_a_consenting_browser_be_told_it_is_not_sending()
        {
            var token = await ABrowserThatAgreedAsync();
            GivenTheAdministratorHasEngagedTheVeto();

            var state = await TheStateThisBrowserIsToldAsync(token);

            Assert.That(Flag(state, "sending"), Is.False,
                "the footer says data is being sent from this browser while the emit path drops "
                + "every batch it hands in - the indicator is reporting a decision, not a fact");
        }

        // @AC-06.7 @DT-26 - and it says whose doing it was. "Not sending" alone reads as "you declined"
        // to somebody who did not decline, which is the reading this criterion exists to rule out.
        [Test]
        public async Task An_engaged_veto_says_the_administrator_stopped_it()
        {
            var token = await ABrowserThatAgreedAsync();
            GivenTheAdministratorHasEngagedTheVeto();

            var state = await TheStateThisBrowserIsToldAsync(token);

            Assert.That(Flag(state, "administratorDisabled"), Is.True,
                "nothing in the answer distinguishes the administrator's decision from the reader's "
                + "own, so the footer can only blame the reader for a choice somebody else made");
        }

        // @AC-06.7 - the other half, and the one that makes the field mean something. A browser that
        // declined is told it is not sending and is NOT told an administrator did it.
        [Test]
        public async Task A_browser_that_declined_is_not_told_the_administrator_did_it()
        {
            var token = await ABrowserThatRefusedAsync();

            var state = await TheStateThisBrowserIsToldAsync(token);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Flag(state, "sending"), Is.False,
                    "somebody declined and is being told their data is going out");
                Assert.That(Flag(state, "administratorDisabled"), Is.False,
                    "a reader who declined is told an administrator stopped it, so the field is on "
                    + "whenever nothing is being sent and says nothing anybody could act on");
            }
        }

        // --- Given ---

        private void GivenTheAdministratorHasEngagedTheVeto() => StoreTheVeto(engaged: true);

        private void GivenTheAdministratorHasLiftedTheVeto() => StoreTheVeto(engaged: false);

        private void GivenNoVetoRowExists()
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<OptionalFeature>>();

            var existing = repository.GetByPredicate(feature => feature.Key == UsageDataMasterSwitch.Key);

            if (existing is null)
            {
                return;
            }

            repository.Remove(existing);
            repository.Save().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Backdates the install timestamp past the threshold, so a scenario about the veto is not
        /// quietly answered by an instance that is too young to ask anybody anything.
        /// </summary>
        private void GivenTheInstanceIsOldEnoughToAsk()
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var longEnoughAgo = DateTime.UtcNow.AddYears(-1).ToString("O");

            var installed = context.AppSettings.FirstOrDefault(
                setting => setting.Key == AppSettingKeys.InstallTimestamp);

            if (installed is null)
            {
                // This host runs no seeders, so there is no timestamp to move. An instance whose age
                // cannot be established is never asked anything, which would answer every scenario
                // here before it got to the veto.
                context.AppSettings.Add(new AppSetting
                {
                    Key = AppSettingKeys.InstallTimestamp,
                    Value = longEnoughAgo,
                });
            }
            else
            {
                installed.Value = longEnoughAgo;
            }

            context.SaveChanges();
        }

        private void StoreTheVeto(bool engaged)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<OptionalFeature>>();

            var existing = repository.GetByPredicate(feature => feature.Key == UsageDataMasterSwitch.Key);

            if (existing is null)
            {
                repository.Add(new OptionalFeature
                {
                    Id = 0,
                    Key = UsageDataMasterSwitch.Key,
                    Name = "Never send usage data",
                    Description = "Seeded by a scenario; the product's own wording is asserted elsewhere.",
                    Enabled = engaged,
                    IsPremium = true,
                });
            }
            else
            {
                existing.Enabled = engaged;
            }

            repository.Save().GetAwaiter().GetResult();
        }

        // --- When ---

        /// <summary>
        /// A batch handed in for its effect rather than its answer. The response is still disposed;
        /// the scenarios that need to read it keep it instead.
        /// </summary>
        private async Task HandInAndForgetTheAnswerAsync(string? token, string body)
        {
            using var response = await HandInAsync(token, body);
        }

        // --- Reading the answer ---

        private static bool? Flag(JsonElement state, string name)
        {
            return state.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null
                ? value.GetBoolean()
                : null;
        }

        private static string? Text(JsonElement state, string name)
        {
            return state.TryGetProperty(name, out var value) ? value.GetString() : null;
        }
    }
}
