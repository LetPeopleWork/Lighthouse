using System.Net;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Services.Implementation.DeliverySources;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;
using Moq;
using Moq.Protected;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// Writing a Lighthouse forecast onto a Jira Release. The whole point of reading the Release before
    /// writing it is that the description is a field people also write in, so what is asserted here is
    /// what survives: their words, and exactly one forecast however many times it has been published.
    ///
    /// Kept out of JiraWorkTrackingConnectorTest deliberately - that class is a live-Jira suite and the
    /// filter everybody runs excludes it, so a specification written there would silently never run.
    /// The transport is a stub, so a real Jira could not be reached even by accident.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5565-delivery-date-sync")]
    [Category("slice-04")]
    public class JiraDeliveryForecastPublisherTest
    {
        private const string JiraReleaseSourceKey = "jira-release";
        private const string TheRelease = "10412";
        // Written out rather than rendered, so this fixture pins what the adapter does with a block
        // rather than agreeing with the renderer about what one looks like. It is still the real shape:
        // the merge finds its own previous write by that shape, and a stand-in that only resembled one
        // would have every scenario here quietly appending.
        private const string TheBlock =
            "\U0001F52E Lighthouse forecast - updated 2026-08-25\n70%: 2026-09-15\nTarget 2026-10-01: 88% likely\n\U0001F52E";

        private const string ASecondBlock =
            "\U0001F52E Lighthouse forecast - updated 2026-08-26\n70%: 2026-09-16\nTarget 2026-10-01: 91% likely\n\U0001F52E";
        private const string WhatTheTeamWrote = "Ships with the autumn campaign. Ask Dana before moving this.";

        [Test]
        public void Every_Jira_connection_says_it_can_be_asked_to_publish()
        {
            var jira = AJiraHoldingAReleaseWithNoDescription();

            Assert.That(jira.Connector.SupportsDeliveryForecastPublishing(jira.Portfolio.WorkTrackingSystemConnection), Is.True,
                "the permission a Version write needs is held per project, so no answer given for a whole connection could be true of everything it touches.");
        }

        [Test]
        public async Task A_Release_with_no_description_gets_one_that_is_only_the_forecast()
        {
            var jira = AJiraHoldingAReleaseWithNoDescription();

            var result = await Publish(jira, TheBlock);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.TypeOf<DeliveryForecastPublishResult.Published>());
                Assert.That(jira.Description, Is.EqualTo(TheBlock));
            }
        }

        [Test]
        public async Task A_description_the_team_wrote_keeps_every_word_of_it()
        {
            var jira = AJiraHoldingAReleaseDescribedAs(WhatTheTeamWrote);

            await Publish(jira, TheBlock);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(jira.Description, Does.StartWith(WhatTheTeamWrote));
                Assert.That(jira.Description, Does.Contain(TheBlock));
            }
        }

        /// <summary>
        /// The criterion the slice rests on, proved end to end rather than at the merge alone: a Release
        /// published to on every refresh must not accumulate a forecast per refresh.
        /// </summary>
        [Test]
        public async Task Publishing_twice_leaves_one_forecast_not_two()
        {
            var jira = AJiraHoldingAReleaseDescribedAs(WhatTheTeamWrote);

            await Publish(jira, TheBlock);
            await Publish(jira, ASecondBlock);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(jira.Description, Does.StartWith(WhatTheTeamWrote));
                Assert.That(jira.Description, Does.Contain("updated 2026-08-26"));
                Assert.That(jira.Description, Does.Not.Contain("updated 2026-08-25"));
            }
        }

        [Test]
        public async Task The_forecast_is_written_to_the_Release_named_and_to_nothing_else()
        {
            var jira = AJiraHoldingAReleaseWithNoDescription();

            await Publish(jira, TheBlock);

            Assert.That(jira.Writes, Is.EqualTo(new[] { $"rest/api/3/version/{TheRelease}" }));
        }

        /// <summary>
        /// The Release was deleted between the read that resolved it and this write. Reported as a missing
        /// target rather than as a refusal, because the two send an administrator to fix different things.
        /// </summary>
        [Test]
        public async Task A_Release_Jira_no_longer_has_is_a_missing_target_and_nothing_is_written()
        {
            var jira = AJiraThatAnswers(HttpStatusCode.NotFound, "{}");

            var result = await Publish(jira, TheBlock);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.TypeOf<DeliveryForecastPublishResult.TargetMissing>());
                Assert.That(jira.Writes, Is.Empty);
            }
        }

        [Test]
        public async Task A_Release_that_disappears_between_the_read_and_the_write_is_a_missing_target()
        {
            var jira = AJiraHoldingAReleaseWithNoDescription();
            jira.RefuseWritesWith(HttpStatusCode.NotFound, "{}");

            var result = await Publish(jira, TheBlock);

            Assert.That(result, Is.TypeOf<DeliveryForecastPublishResult.TargetMissing>());
        }

        /// <summary>
        /// Jira's own sentence, verbatim. It already names what to fix in the words an administrator will
        /// search for, and a paraphrase would lose exactly that.
        /// </summary>
        [TestCase(HttpStatusCode.BadRequest, TestName = "The refusal Jira was measured to send - a 400, not the 403 its own documentation implies")]
        [TestCase(HttpStatusCode.Forbidden, TestName = "The refusal Jira documents")]
        [TestCase(HttpStatusCode.Unauthorized, TestName = "A credential Jira will not accept at all")]
        public async Task A_write_the_credential_may_not_make_is_refused_in_Jiras_own_words(HttpStatusCode status)
        {
            var jira = AJiraHoldingAReleaseWithNoDescription();
            jira.RefuseWritesWith(status, "{\"errorMessages\":[\"You do not have permission to edit this version.\"],\"errors\":{}}");

            var result = await Publish(jira, TheBlock);

            Assert.That(result, Is.EqualTo(new DeliveryForecastPublishResult.Refused("You do not have permission to edit this version.")));
        }

        /// <summary>
        /// The refusal this write can actually provoke. A description near Jira's size ceiling is refused
        /// with the sentence in the per-field half of the error body rather than the whole-request half,
        /// and reading only the first half would report the one thing an administrator can act on as a
        /// bare status line.
        /// </summary>
        [Test]
        public async Task A_refusal_about_one_field_is_read_out_of_the_half_of_the_body_it_arrives_in()
        {
            var jira = AJiraHoldingAReleaseWithNoDescription();
            jira.RefuseWritesWith(
                HttpStatusCode.BadRequest,
                "{\"errorMessages\":[],\"errors\":{\"description\":\"The description is over 16384 characters.\"}}");

            var result = await Publish(jira, TheBlock);

            Assert.That(result, Is.EqualTo(new DeliveryForecastPublishResult.Refused("The description is over 16384 characters.")));
        }

        /// <summary>
        /// A throttled Jira has refused nothing - it has asked to be left alone for a moment. Recorded as
        /// a refusal it would be written down as a standing permission problem, and a Portfolio big
        /// enough to be throttled partway through publishing would report one for every Delivery after
        /// the first, sending an administrator to audit a permission that was never wrong.
        /// </summary>
        [TestCase(HttpStatusCode.TooManyRequests, TestName = "Jira asking to be asked again later")]
        [TestCase(HttpStatusCode.RequestTimeout, TestName = "A request Jira ran out of time on")]
        public void A_Jira_asking_for_a_moment_is_not_an_answer_about_the_credential(HttpStatusCode status)
        {
            var jira = AJiraHoldingAReleaseWithNoDescription();
            jira.RefuseWritesWith(status, "{}");

            Assert.ThrowsAsync<HttpRequestException>(() => Publish(jira, TheBlock));
        }

        /// <summary>
        /// The block that is already on the Release is the block that would be written. Writing it again
        /// spends a request per Delivery per round to change nothing, on somebody else's Jira, whose rate
        /// limit is the thing this feature can least afford to spend.
        /// </summary>
        [Test]
        public async Task Publishing_a_forecast_that_has_not_moved_writes_nothing_at_all()
        {
            var jira = AJiraHoldingAReleaseWithNoDescription();
            await Publish(jira, TheBlock);
            jira.ForgetWhatWasWritten();

            var result = await Publish(jira, TheBlock);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.TypeOf<DeliveryForecastPublishResult.Published>());
                Assert.That(jira.Writes, Is.Empty);
            }
        }

        /// <summary>
        /// Jira refuses a description over its ceiling, and it refuses it the same way it refuses a
        /// credential that may not write. Told apart before the request rather than after, so an
        /// administrator reads a sentence about length instead of going to look at a permission that was
        /// never the problem.
        /// </summary>
        [Test]
        public async Task A_description_that_would_go_over_Jiras_limit_is_refused_here_and_says_why()
        {
            var jira = AJiraHoldingAReleaseDescribedAs(new string('x', 16_380));

            var result = await Publish(jira, TheBlock);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.TypeOf<DeliveryForecastPublishResult.Refused>()
                    .And.Property(nameof(DeliveryForecastPublishResult.Refused.Reason)).Contains("16,384"));
                Assert.That(jira.Writes, Is.Empty, "a request that is certain to be refused is not worth making.");
            }
        }

        /// <summary>
        /// What the refusal report puts on screen is the remote's own sentence. A rejection carrying no
        /// sentence has nothing an administrator could act on, and it is far likelier to be a request
        /// Lighthouse built wrong than a permission somebody can grant - so it is not written down as a
        /// permission problem at all.
        /// </summary>
        [TestCase("not json at all", TestName = "A body that is not an answer Jira wrote")]
        [TestCase("{\"errorMessages\":[],\"errors\":{}}", TestName = "Jira's error shape with nothing in either half")]
        public void A_rejection_Jira_gave_no_reason_for_is_not_a_permission_report(string body)
        {
            var jira = AJiraHoldingAReleaseWithNoDescription();
            jira.RefuseWritesWith(HttpStatusCode.BadRequest, body);

            Assert.ThrowsAsync<HttpRequestException>(() => Publish(jira, TheBlock));
        }

        /// <summary>
        /// A Jira that fell over has not refused anything - it has failed to answer. Reported as a refusal
        /// it would be written down as a standing permission problem that nobody can fix, so it is thrown
        /// and the caller treats it the way it treats a source it could not reach.
        /// </summary>
        [Test]
        public void A_Jira_that_fell_over_is_not_an_answer_about_the_credential()
        {
            var jira = AJiraHoldingAReleaseWithNoDescription();
            jira.RefuseWritesWith(HttpStatusCode.InternalServerError, "{}");

            Assert.ThrowsAsync<HttpRequestException>(() => Publish(jira, TheBlock));
        }

        [Test]
        public void A_source_this_connection_does_not_offer_is_refused_without_asking_Jira_anything()
        {
            var jira = AJiraHoldingAReleaseWithNoDescription();

            Assert.ThrowsAsync<ArgumentException>(() => jira.Connector.PublishAsync(
                jira.Portfolio.WorkTrackingSystemConnection,
                new DeliveryForecastPublication("jira-relase", TheRelease, TheBlock)));

            Assert.That(jira.Writes, Is.Empty);
        }

        private static Task<DeliveryForecastPublishResult> Publish(FakeJira jira, string block)
        {
            return jira.Connector.PublishAsync(
                jira.Portfolio.WorkTrackingSystemConnection,
                new DeliveryForecastPublication(JiraReleaseSourceKey, TheRelease, block));
        }

        private static FakeJira AJiraHoldingAReleaseWithNoDescription() => new(null);

        private static FakeJira AJiraHoldingAReleaseDescribedAs(string description) => new(description);

        private static FakeJira AJiraThatAnswers(HttpStatusCode status, string body)
        {
            var jira = new FakeJira(null);
            jira.RefuseReadsWith(status, body);

            return jira;
        }

        /// <summary>
        /// A Jira holding one Release, which remembers what was written to it. Remembering rather than
        /// merely recording the call is what lets the round trip be asserted: publishing twice has to
        /// find the first block on the second pass, and nothing short of a description that survives
        /// between the two would show that.
        /// </summary>
        private sealed class FakeJira
        {
            private HttpStatusCode readStatus = HttpStatusCode.OK;
            private string readBody = string.Empty;
            private HttpStatusCode writeStatus = HttpStatusCode.OK;
            private string writeBody = "{}";

            public FakeJira(string? description)
            {
                Description = description;

                var handlerMock = new Mock<HttpMessageHandler>();
                handlerMock
                    .Protected()
                    .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync((HttpRequestMessage request, CancellationToken _) => Answer(request));

                Portfolio = JiraConnectorTestSetup.APortfolioOnJiraCloud();
                Connector = JiraConnectorTestSetup.AConnectorOver(handlerMock.Object);
            }

            public string? Description { get; private set; }

            public List<string> Writes { get; } = [];

            public Portfolio Portfolio { get; }

            public JiraWorkTrackingConnector Connector { get; }

            public void RefuseReadsWith(HttpStatusCode status, string body)
            {
                readStatus = status;
                readBody = body;
            }

            public void ForgetWhatWasWritten() => Writes.Clear();

            public void RefuseWritesWith(HttpStatusCode status, string body)
            {
                writeStatus = status;
                writeBody = body;
            }

            private HttpResponseMessage Answer(HttpRequestMessage request)
            {
                var path = request.RequestUri!.AbsolutePath.TrimStart('/');

                if (request.Method == HttpMethod.Put)
                {
                    return AnswerWrite(request, path);
                }

                return readStatus == HttpStatusCode.OK
                    ? Respond(HttpStatusCode.OK, VersionJson())
                    : Respond(readStatus, readBody);
            }

            private HttpResponseMessage AnswerWrite(HttpRequestMessage request, string path)
            {
                if (writeStatus != HttpStatusCode.OK)
                {
                    return Respond(writeStatus, writeBody);
                }

                Writes.Add(path);
                Description = JsonDocument
                    .Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult())
                    .RootElement.GetProperty("description").GetString();

                return Respond(HttpStatusCode.OK, VersionJson());
            }

            /// <summary>
            /// A Release nobody has described leaves the key out of the answer altogether rather than
            /// sending null - which is the shape every Release on the demo instance had.
            /// </summary>
            private string VersionJson()
            {
                return Description is null
                    ? $"{{\"id\":\"{TheRelease}\",\"name\":\"2026 Q4\"}}"
                    : $"{{\"id\":\"{TheRelease}\",\"name\":\"2026 Q4\",\"description\":{JsonSerializer.Serialize(Description)}}}";
            }

            private static HttpResponseMessage Respond(HttpStatusCode status, string body)
                => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
}
