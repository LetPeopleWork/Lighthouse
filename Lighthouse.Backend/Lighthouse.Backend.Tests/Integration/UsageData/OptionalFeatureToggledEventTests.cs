using System.Net;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.Integration.UsageData
{
    /// <summary>
    /// Acceptance scenarios for the event that says a behaviour setting under Settings -> System was
    /// switched: which setting, and which way it went.
    ///
    /// Written as what a browser actually posts and what actually reached the collector, for the
    /// reason the fixture beside this one gives. The event's name and its two parts do not exist in
    /// the code yet, and must not appear there before the usage data page describes them - so these
    /// name them as text, the whole assembly keeps building, and each scenario fails on its own
    /// assertion the moment it is un-ignored.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    [Category("optional-feature-toggled")]
    public class OptionalFeatureToggledEventTests : UsageDataCollectorObservationTest
    {
        private const string OptionalFeatureToggled = "OptionalFeatureToggled";

        private const string FeatureOrder = "FeatureOrder";

        private const string NeverSendUsageData = "NeverSendUsageData";

        private const string WhichSettingOnTheWire = "optional_feature";

        private const string WhichWayOnTheWire = "enabled";

        private const string OnlyWhichSetting = ",\"optionalFeature\":\"FeatureOrder\"";

        private const string OnlyWhichWay = ",\"enabled\":true";

        private const string BothWhichSettingAndWhichWay = OnlyWhichSetting + OnlyWhichWay;

        /// <summary>
        /// The key the product stores the ordering setting under. It is a different word from the
        /// one usage data publishes on purpose, which is what lets a scenario tell the two apart.
        /// </summary>
        private const string TheOrderingSettingsOwnKey = "FeatureOrdering";

        private const string TheOrderingSettingsOwnName = "Let Lighthouse own the order";

        /// <summary>
        /// Every event that is not about a setting being switched, each in the shape it is otherwise
        /// accepted in. The ones that carry a page or a kind of system are the ones worth having here,
        /// because a rule written only for the events that carry nothing would pass for all ten.
        /// </summary>
        private static readonly string[] EveryOtherEvent =
        [
            "TeamTabOpened",
            "PortfolioTabOpened",
            "TeamCreated",
            "TeamDeleted",
            "PortfolioCreated",
            "PortfolioDeleted",
            "TeamManualForecastRun",
            "WorkTrackingSystemConnected",
            "TeamRefreshTriggered",
            "PortfolioRefreshTriggered",
        ];

        /// <summary>
        /// What the usage data page says travels with every event, plus the two parts this one adds.
        /// Nothing else about the setting - not the key it is stored under, not its name, not its
        /// help text - is on that page, so nothing else may arrive.
        /// </summary>
        private static readonly string[] EverythingThePageSaysTravelsWithASettingSwitch =
        [
            "version",
            "deployment_mode",
            "licence_tier",
            "auth_enabled",
            "$ip",
            "$geoip_disable",
            WhichSettingOnTheWire,
            WhichWayOnTheWire,
        ];

        // @walking_skeleton @driving_port @real-io @AC-1.1
        [Test]
        public async Task Switching_the_feature_order_setting_on_arrives_saying_which_setting_and_that_it_is_now_on()
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(token, ASettingSwitched(FeatureOrder, enabled: true));

            var messages = EveryMessageIn(await EverythingTheCollectorReceived());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(messages, Has.Count.EqualTo(1),
                    "one switch is one event - no more, and not none");
            }

            var carried = messages[0].GetProperty("properties");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(messages[0].GetProperty("event").GetString(), Is.EqualTo(OptionalFeatureToggled));
                Assert.That(TextOf(carried, WhichSettingOnTheWire), Is.EqualTo(FeatureOrder));
                Assert.That(KindOf(carried, WhichWayOnTheWire), Is.EqualTo(JsonValueKind.True),
                    "the direction is the half of this event that answers whether people want the "
                    + "setting or want rid of it");
            }
        }

        // @driving_port @real-io @AC-1.1
        [Test]
        public async Task Switching_it_back_off_arrives_as_one_more_event_saying_it_is_now_off()
        {
            var token = await ABrowserThatAgreedAsync();

            using var switchedOn = await HandInAsync(token, ASettingSwitched(FeatureOrder, enabled: true));
            var sentWhenSwitchedOn = EveryMessageIn(await EverythingTheCollectorReceived());
            Outbound.Clear();

            using var switchedOff = await HandInAsync(token, ASettingSwitched(FeatureOrder, enabled: false));
            var sentWhenSwitchedOff = EveryMessageIn(await EverythingTheCollectorReceived());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sentWhenSwitchedOn, Has.Count.EqualTo(1),
                    "switching it on sent nothing, so the claim about switching it off below would "
                    + "hold for a pipe that never carried this event at all");
                Assert.That(switchedOff.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(sentWhenSwitchedOff, Has.Count.EqualTo(1));
            }

            var carried = sentWhenSwitchedOff[0].GetProperty("properties");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TextOf(carried, WhichSettingOnTheWire), Is.EqualTo(FeatureOrder));
                Assert.That(KindOf(carried, WhichWayOnTheWire), Is.EqualTo(JsonValueKind.False),
                    "a switch off counted as a switch on is the one wrong number this event exists "
                    + "to prevent");
            }
        }

        // @driving_port @real-io @error @AC-1.4
        [Test]
        [TestCase("refused")]
        [TestCase("never answered")]
        [TestCase("withdrew")]
        public async Task Nothing_leaves_a_browser_that_did_not_agree_when_it_switches_a_setting(string answer)
        {
            var token = await ABrowserThatAsync(answer);

            using var handedIn = await HandInAsync(token, ASettingSwitched(FeatureOrder, enabled: true));

            var sent = await EverythingTheCollectorReceived();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
                    "turning the message away would say it was read, and so that the gate was what "
                    + "stopped it - the answer has to be the same either way");
                Assert.That(sent, Is.Empty,
                    "somebody who did not agree was counted anyway");
            }
        }

        /// <summary>
        /// Which setting and which way are the whole of this event. One arriving without either
        /// would be counted as a switch of nothing in particular, or in no particular direction,
        /// which is worse than not counting it.
        ///
        /// Each case hands the complete message in afterwards. Today every message under this name
        /// is refused for the name alone, so without that second half the refusal would be proved
        /// by a product that cannot read this event at all.
        /// </summary>
        // @driving_port @real-io @error @AC-1.5
        [Test]
        [TestCase(OnlyWhichWay, TestName = "A setting switch that does not say which setting is refused")]
        [TestCase(OnlyWhichSetting, TestName = "A setting switch that does not say which way is refused")]
        [TestCase("", TestName = "A setting switch that says neither which setting nor which way is refused")]
        public async Task A_setting_switch_missing_part_of_what_it_says_is_refused(string whatItCarries)
        {
            await AssertItIsRefusedWhileTheCompleteMessageIsAccepted(AMessageAbout(OptionalFeatureToggled, whatItCarries));
        }

        /// <summary>
        /// The list of settings is usage data's own, and it names one setting. The product's stored
        /// key for a setting, a setting that no longer exists, and the administrator's veto are not
        /// on it, so none of them can be sent under it.
        ///
        /// The veto is left off on purpose. Switching it on can never be reported, because from that
        /// moment nothing leaves, and counting only the times it was lifted would read as people
        /// forever lifting it. The screen does not report it, and this is what stops a message built
        /// by hand from reporting it anyway.
        /// </summary>
        // @driving_port @real-io @error @AC-1.2 @AC-1.5
        [Test]
        [TestCase(NeverSendUsageData)]
        [TestCase("DeltaSync")]
        [TestCase(TheOrderingSettingsOwnKey)]
        [TestCase("UsageData")]
        public async Task A_setting_this_list_does_not_have_is_refused(string setting)
        {
            await AssertItIsRefusedWhileTheCompleteMessageIsAccepted(ASettingSwitchedTo(setting, "true"));
        }

        // @driving_port @real-io @error @AC-1.5
        [Test]
        [TestCase("\"true\"")]
        [TestCase("1")]
        [TestCase("null")]
        public async Task A_setting_switch_that_says_something_other_than_on_or_off_is_refused(string whichWay)
        {
            await AssertItIsRefusedWhileTheCompleteMessageIsAccepted(ASettingSwitchedTo(FeatureOrder, whichWay));
        }

        /// <summary>
        /// Each event may carry only what its own entry says it carries. A message about a Team
        /// being created that also says a setting was switched would put a switch in the numbers
        /// that nobody made.
        /// </summary>
        // @driving_port @real-io @error @AC-1.5
        [Test]
        [TestCaseSource(nameof(EveryOtherEvent))]
        public async Task Any_other_event_that_says_a_setting_was_switched_is_refused(string name)
        {
            await AssertItIsRefusedOutright(AMessageAbout(name, WhatItNeedsToBeRead(name) + BothWhichSettingAndWhichWay));
        }

        // @driving_port @real-io @error @AC-1.5
        [Test]
        [TestCase(OnlyWhichSetting)]
        [TestCase(OnlyWhichWay)]
        public async Task Any_other_event_carrying_either_half_of_a_setting_switch_is_refused(string whatItCarries)
        {
            await AssertItIsRefusedOutright(AMessageAbout("TeamCreated", whatItCarries));
        }

        /// <summary>
        /// The page the consent dialog links to is the only list of what is sent. This reads the
        /// message that actually left and checks every part of it against that list, so the
        /// setting's own key, name or help text travelling alongside would fail here rather than in
        /// somebody else's database.
        /// </summary>
        // @driving_port @real-io @AC-1.6
        [Test]
        public async Task Nothing_travels_with_a_setting_switch_beyond_which_setting_and_which_way()
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(token, ASettingSwitched(FeatureOrder, enabled: true));

            var sent = await EverythingTheCollectorReceived();
            var messages = EveryMessageIn(sent);

            Assert.That(messages, Has.Count.EqualTo(1),
                "nothing arrived, so there is nothing whose parts could be checked against the page");

            var carried = messages[0].GetProperty("properties")
                .EnumerateObject().Select(property => property.Name).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(carried, Is.EquivalentTo(EverythingThePageSaysTravelsWithASettingSwitch),
                    "something is travelling with a setting switch that the usage data page does not describe");
                Assert.That(sent, Does.Not.Contain(TheOrderingSettingsOwnKey));
                Assert.That(sent, Does.Not.Contain(TheOrderingSettingsOwnName));
            }
        }

        private async Task AssertItIsRefusedWhileTheCompleteMessageIsAccepted(string batch)
        {
            var token = await ABrowserThatAgreedAsync();

            using var refused = await HandInAsync(token, batch);
            var sentForTheRefusedOne = await EverythingTheCollectorReceived();

            using var complete = await HandInAsync(token, ASettingSwitched(FeatureOrder, enabled: true));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(sentForTheRefusedOne, Is.Empty,
                    "a message the product refused to read still reached the collector");
                Assert.That(complete.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
                    "the complete message is refused too, so the refusal above says nothing about "
                    + "what was missing or wrong - only that this event cannot be read at all");
            }
        }

        private async Task AssertItIsRefusedOutright(string batch)
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(token, batch);

            var sent = await EverythingTheCollectorReceived();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(sent, Is.Empty,
                    "a switch nobody made reached the collector attached to another event");
            }
        }

        private async Task<string?> ABrowserThatAsync(string answer) => answer switch
        {
            "refused" => await ABrowserThatRefusedAsync(),
            "withdrew" => await ABrowserThatWithdrewAsync(),
            _ => null,
        };

        private async Task<string> ABrowserThatWithdrewAsync()
        {
            var token = await ABrowserThatAgreedAsync();

            using var request = new HttpRequestMessage(HttpMethod.Delete, ConsentRoute);
            request.Headers.Add(ConsentTokenHeader, token);

            using var response = await Client.SendAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
                "the withdrawal did not go through, so whatever happens next is not about having withdrawn");

            return token;
        }

        /// <summary>
        /// What another event needs in order to be read at all. Without it the message is refused for
        /// the part it lacks, and a scenario about the part it should not have would pass without the
        /// rule it is about having done anything.
        /// </summary>
        private static string WhatItNeedsToBeRead(string name) => name switch
        {
            "TeamTabOpened" => ",\"route\":\"TeamDetail_Metrics\"",
            "PortfolioTabOpened" => ",\"route\":\"PortfolioDetail_Metrics\"",
            "WorkTrackingSystemConnected" => ",\"workTrackingSystem\":\"Jira\"",
            _ => string.Empty,
        };

        private static string ASettingSwitched(string setting, bool enabled)
            => ASettingSwitchedTo(setting, JsonSerializer.Serialize(enabled));

        private static string ASettingSwitchedTo(string setting, string whichWay)
            => AMessageAbout(OptionalFeatureToggled, $",\"optionalFeature\":\"{setting}\",\"enabled\":{whichWay}");

        private static string AMessageAbout(string name, string whatItCarries)
            => $"{{\"events\":[{{\"name\":\"{name}\"{whatItCarries},\"offsetMs\":0,\"sequence\":0}}]}}";

        private static List<JsonElement> EveryMessageIn(string sent)
        {
            if (string.IsNullOrEmpty(sent))
            {
                return [];
            }

            using var document = JsonDocument.Parse(sent);

            return [.. document.RootElement.EnumerateArray().Select(message => message.Clone())];
        }

        private static string? TextOf(JsonElement carried, string property)
            => carried.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

        private static JsonValueKind KindOf(JsonElement carried, string property)
            => carried.TryGetProperty(property, out var value) ? value.ValueKind : JsonValueKind.Undefined;
    }
}
