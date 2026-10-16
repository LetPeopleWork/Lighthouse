using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection
{
    /// <summary>
    /// A real host over a fresh database, signed in as a System Admin, holding the Payments department's
    /// connection "Jira DC – Payments". Its Teams, in the order they were created: Fraud Signals, Ops Tooling and
    /// Mobile Wallet name no Parent Override Field; Ledger names "Caused by" as its own. Its Portfolios: Card
    /// Issuing names nothing; Merchant Onboarding names its own field for every setting. A second connection,
    /// "Jira DC – Treasury", has fields of its own, so a value from the wrong connection can be told apart.
    ///
    /// The connection is saved through the route the connection page uses, with the body the page sends: the
    /// connection as the list read returned it, with the admin's edit applied. Team and Portfolio settings are
    /// read and saved through their settings routes, the way the settings pages and the command line do. What a
    /// save started is read in the Task Manager and the refresh history; what it said is read from the log, the
    /// way Settings → System Info → Logs shows it.
    ///
    /// Only the tracker is a stand-in. It holds one item per Team and per Portfolio, which never changes, and
    /// it answers honestly about the settings it was handed: each item hangs under the value of whichever
    /// Parent Override Field the owner gave it, and the tracker remembers which field it was asked to read for
    /// each setting. So "follows the default" is observed where it matters, in what the tracker was asked for.
    /// </summary>
    public abstract class ConnectionDefaultsAcceptanceTest
    {
        protected const string JiraDcPayments = "Jira DC – Payments";
        protected const string JiraDcTreasury = "Jira DC – Treasury";
        protected const string PaymentsAzureDevOps = "Payments Azure DevOps";
        protected const string PaymentsCsv = "Payments CSV";
        protected const string PaymentsLinear = "Payments Linear";
        protected const string PaymentsServiceNow = "Payments ServiceNow";

        protected const string FraudSignals = "Fraud Signals";
        protected const string OpsTooling = "Ops Tooling";
        protected const string MobileWallet = "Mobile Wallet";
        protected const string Ledger = "Ledger";
        protected const string CardIssuing = "Card Issuing";
        protected const string MerchantOnboarding = "Merchant Onboarding";

        protected const string CausedBy = "Caused by";
        protected const string ParentLink = "Parent Link";
        protected const string ReleaseTrain = "Release Train";
        protected const string BlockedByKeys = "Blocked By Keys";
        protected const string TShirtSize = "T-Shirt Size";
        protected const string ProductOwner = "Product Owner";
        protected const string StoryPoints = "Story Points";
        protected const string Points = "Points";
        protected const string TreasuryCausedBy = "Treasury Caused by";

        /// <summary>Where a Team's item hangs when no Parent Override Field is read: the tracker's own parent.</summary>
        protected const string TheTrackersOwnParent = "PAY-1";

        /// <summary>The parent the field "Caused by" holds on every item.</summary>
        protected const string WhatCausedByHolds = "PAY-7";

        /// <summary>The parent the field "Parent Link" holds on every item.</summary>
        protected const string WhatParentLinkHolds = "PAY-9";

        protected static readonly string[] TheThreeFollowers = [FraudSignals, OpsTooling, MobileWallet];

        private const string ConnectionsRoute = "/api/latest/worktrackingsystemconnections";
        private const string TaskListRoute = "/api/latest/update/tasks";
        private const string RefreshLogRoute = "/api/latest/systeminfo/refreshlog";
        private const string JsonMediaType = "application/json";
        private const string AdditionalFieldDefinitions = "additionalFieldDefinitions";
        private const string DisplayName = "displayName";
        private const string DefaultsMember = "defaults";

        private static readonly DateTime TheItemLastChangedAt = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
        private static readonly TimeSpan LongEnoughForAnyUpdateToFinish = TimeSpan.FromSeconds(30);

        private static readonly (string DisplayName, string Reference)[] ThePaymentsFields =
        [
            (CausedBy, "customfield_10500"),
            (ParentLink, "customfield_10014"),
            (ReleaseTrain, "customfield_10600"),
            (BlockedByKeys, "customfield_10700"),
            (TShirtSize, "customfield_10800"),
            (ProductOwner, "customfield_10900"),
            (StoryPoints, "customfield_10016"),
        ];

        private readonly Dictionary<string, int> connectionIds = [];
        private readonly Dictionary<string, int> teamIds = [];
        private readonly Dictionary<string, int> portfolioIds = [];
        private readonly Dictionary<(string Owner, OwnerSetting Setting), string?> whatTheTrackerWasAskedToRead = [];

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private CapturedLogMessages whatWasLogged = null!;
        private SerilogLoggerFactory theLog = null!;
        private Func<Task>? meanwhileBeforeTheSaveCommits;
        private int lastHistoryEntryBeforeTheSave;
        private int logLinesBeforeTheSave;

        protected WebApplicationFactory<Program> Factory { get; private set; } = null!;

        protected HttpClient Client { get; private set; } = null!;

        protected HttpStatusCode SaveAnswer { get; private set; }

        protected string SaveAnswerBody { get; private set; } = string.Empty;

        /// <summary>A default a connection holds for every Team, or for every Portfolio, that follows it.</summary>
        public enum ConnectionDefault
        {
            TeamsParentOverrideField,
            PortfoliosParentOverrideField,
            PortfoliosDependencyField,
            PortfoliosSizeEstimateField,
            PortfoliosFeatureOwnerField,
            TeamsEstimation,
            PortfoliosEstimation,
        }

        /// <summary>A setting an owner either follows from its connection or holds as its own.</summary>
        public enum OwnerSetting
        {
            ParentOverrideField,
            DependencyField,
            SizeEstimateField,
            FeatureOwnerField,
            Estimation,
        }

        /// <summary>How a Team's or Portfolio's setting reads on its settings page.</summary>
        public enum Source
        {
            ConnectionDefault,
            Own,
        }

        /// <summary>The shape a script saves an owner's settings back in.</summary>
        public enum RoundTrip
        {
            EchoingEveryProperty,
            DroppingWhatItDoesNotKnow,
        }

        [SetUp]
        public async Task StartLighthouseForThePaymentsDepartment()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.SetUp);

            connectionIds.Clear();
            teamIds.Clear();
            portfolioIds.Clear();
            whatTheTrackerWasAskedToRead.Clear();
            meanwhileBeforeTheSaveCommits = null;
            lastHistoryEntryBeforeTheSave = 0;
            logLinesBeforeTheSave = 0;
            SaveAnswer = default;
            SaveAnswerBody = string.Empty;

            whatWasLogged = new CapturedLogMessages();
            theLog = new SerilogLoggerFactory(
                new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(whatWasLogged).CreateLogger(),
                dispose: true);

            var licence = new Mock<ILicenseService>();
            licence.Setup(service => service.CanUsePremiumFeatures()).Returns(true);

            var tracker = ATrackerThatReadsWhatItIsHanded();
            var connectorFactory = new Mock<IWorkTrackingConnectorFactory>();
            connectorFactory
                .Setup(factory => factory.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>()))
                .Returns(tracker.Object);

            var log = theLog;
            rootFactory = new TestWebApplicationFactory<Program>();
            Factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory).WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ILicenseService>();
                    services.AddScoped(_ => licence.Object);

                    services.RemoveAll<IWorkTrackingConnectorFactory>();
                    services.AddScoped(_ => connectorFactory.Object);

                    services.RemoveAll<IForecastService>();
                    services.AddScoped(_ => Mock.Of<IForecastService>());

                    services.RemoveAll<ILoggerFactory>();
                    services.AddSingleton<ILoggerFactory>(log);

                    LetAnotherSaveLandBeforeTheConnectionSaveCommits(services);
                });
            });

            Client = Factory.CreateClient();
            Client.AsSystemAdmin();

            using var scope = Factory.Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            await database.Database.EnsureDeletedAsync();
            await database.Database.EnsureCreatedAsync();

            foreach (var seeder in scope.ServiceProvider.GetServices<ISeeder>())
            {
                await seeder.Seed();
            }

            await SeedThePaymentsDepartment();
        }

        [TearDown]
        public async Task StopLighthouse()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.TearDown);

            await TheQueueGoesIdle();

            using (var scope = Factory.Services.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeletedAsync();
            }

            Client.Dispose();
            Factory.Dispose();
            rootFactory.Dispose();
            theLog.Dispose();
        }

        // --- Given ---

        /// <summary>
        /// The other trackers a department may connect, each with one Team and one Portfolio, so what each one
        /// offers as defaults can be asked of it.
        /// </summary>
        protected async Task GivenAConnectionOfEveryOtherTracker()
        {
            await SeedConnection(PaymentsAzureDevOps, WorkTrackingSystems.AzureDevOps, ThePaymentsFields);
            await SeedConnection(PaymentsCsv, WorkTrackingSystems.Csv, ThePaymentsFields);
            await SeedConnection(PaymentsLinear, WorkTrackingSystems.Linear, ThePaymentsFields);
            await SeedConnection(PaymentsServiceNow, WorkTrackingSystems.ServiceNow, ThePaymentsFields);
        }


        protected async Task GivenTheDefaultIs(ConnectionDefault theDefault, string fieldName)
        {
            await WhenTheAdminSetsTheDefault(theDefault, fieldName);
            ThenTheSaveIsAccepted();
        }

        protected async Task GivenTheTeamsEstimationDefaultIs(string fieldName, string unit)
        {
            await WhenTheAdminSetsTheEstimationDefault(ConnectionDefault.TeamsEstimation, fieldName, unit);
            ThenTheSaveIsAccepted();
        }

        protected async Task GivenTheTeamChoseNone(string team)
        {
            await WhenTheOwnersAdminSaves(team, OwnerSetting.ParentOverrideField, fieldName: null, Source.Own);
            ThenTheOwnerSaveIsAccepted();
        }

        protected async Task GivenTheTeamOwns(string team, string fieldName)
        {
            await WhenTheOwnersAdminSaves(team, OwnerSetting.ParentOverrideField, fieldName, Source.Own);
            ThenTheOwnerSaveIsAccepted();
        }

        protected async Task GivenEveryTeamOwnsItsParentOverrideField()
        {
            foreach (var team in TheThreeFollowers)
            {
                await GivenTheTeamOwns(team, ParentLink);
            }
        }

        protected async Task GivenEveryTeamAndPortfolioHasBeenUpdatedOnce()
        {
            foreach (var team in teamIds.Values)
            {
                Factory.Services.GetRequiredService<ITeamUpdater>().TriggerUpdate(team);
                await TheQueueGoesIdle();
            }

            foreach (var portfolio in portfolioIds.Values)
            {
                Factory.Services.GetRequiredService<IPortfolioUpdater>().TriggerUpdate(portfolio);
                await TheQueueGoesIdle();
            }
        }

        /// <summary>The settings of an owner as a script read them, kept to be saved back later.</summary>
        protected Task<JsonObject> GivenAScriptReadTheSettingsOf(string owner) => TheSettingsOf(owner);

        /// <summary>The connection as an admin's page read it, kept to be saved later by that admin.</summary>
        protected Task<JsonObject> GivenAnAdminOpenedTheConnection() => TheConnectionAsThePageReadsIt(JiraDcPayments);

        /// <summary>
        /// While the admin's connection save is open and before it commits, someone else's save lands. Needs the
        /// connection save to go through <see cref="IConnectionSaveService"/>; without it, nothing lands and the
        /// scenario fails on what it expects.
        /// </summary>
        protected void GivenWhileTheConnectionSaveIsOpen(string team, string startsUsing)
            => meanwhileBeforeTheSaveCommits = async () =>
            {
                using var teamAdmin = Factory.CreateClient().AsSystemAdmin("ops-tooling-admin");
                var settings = await TheSettingsOf(team);
                SetTheOwnersValue(settings, OwnerSetting.ParentOverrideField, await TheFieldId(JiraDcPayments, startsUsing), Source.Own);
                using var content = AsJson(settings);
                using var response = await teamAdmin.PutAsync(new Uri(TheOwnersRoute(team), UriKind.Relative), content);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
            };

        // --- When: the System Admin saves the connection ---

        protected Task WhenTheAdminSetsTheDefault(ConnectionDefault theDefault, string fieldName, bool? startUpdates = null)
            => SaveTheConnection(read => DefaultsOf(read)[TheMember(theDefault)] = TheFieldIdIn(read, fieldName), startUpdates);

        protected Task WhenTheAdminSetsTheDefaultOn(string connection, ConnectionDefault theDefault, string fieldName)
            => SaveTheConnection(connection, read => DefaultsOf(read)[TheMember(theDefault)] = TheFieldIdIn(read, fieldName), startUpdates: null);

        protected Task WhenTheAdminSetsTheEstimationDefault(ConnectionDefault theDefault, string fieldName, string unit, bool? startUpdates = null)
            => SaveTheConnection(read => DefaultsOf(read)[TheMember(theDefault)] = AnEstimationBundle(TheFieldIdIn(read, fieldName), unit), startUpdates);

        /// <summary>The default is sent as empty: the admin cleared it.</summary>
        protected Task WhenTheAdminClearsTheDefault(ConnectionDefault theDefault)
            => SaveTheConnection(read => DefaultsOf(read)[TheMember(theDefault)] = null, startUpdates: null);

        /// <summary>A script that knows nothing of defaults saves the connection with its name changed.</summary>
        protected Task WhenAScriptSavesTheConnectionWithoutItsDefaults()
            => SaveTheConnection(read =>
            {
                read.Remove(DefaultsMember);
                read["name"] = $"{JiraDcPayments} (renamed)";
            }, startUpdates: null);

        protected Task WhenTheAdminRenamesTheConnection()
            => SaveTheConnection(read => read["name"] = $"{JiraDcPayments} (renamed)", startUpdates: null);

        /// <summary>
        /// The admin adds an Additional Field and, in the same save, names it as the default, the way the page
        /// sends a field that has no id yet.
        /// </summary>
        protected Task WhenTheAdminAddsAFieldAndMakesItTheDefault(ConnectionDefault theDefault, string displayName, string reference)
            => SaveTheConnection(read =>
            {
                FieldsOf(read).Add(new JsonObject { ["id"] = -1, [DisplayName] = displayName, ["reference"] = reference, ["isPredefined"] = false });
                DefaultsOf(read)[TheMember(theDefault)] = -1;
            }, startUpdates: null);

        protected Task WhenTheAdminRemovesTheField(string fieldName)
            => SaveTheConnection(read => FieldsOf(read).Remove(TheField(FieldsOf(read), fieldName)), startUpdates: null);

        /// <summary>The admin saves the copy of the connection they opened earlier, with this edit.</summary>
        protected async Task WhenThatAdminSavesTheDefault(JsonObject openedEarlier, ConnectionDefault theDefault, string fieldName)
        {
            await SetTheRestAside();
            DefaultsOf(openedEarlier)[TheMember(theDefault)] = TheFieldIdIn(openedEarlier, fieldName);
            await Put(JiraDcPayments, openedEarlier, startUpdates: null);
        }

        protected async Task WhenSomeoneWithoutConnectionRightsSetsTheDefault(string team)
        {
            var read = await TheConnectionAsThePageReadsIt(JiraDcPayments);
            DefaultsOf(read)[TheMember(ConnectionDefault.TeamsParentOverrideField)] = TheFieldIdIn(read, CausedBy);

            using var teamAdmin = Factory.CreateClient().AsTeamAdmin(teamIds[team]);
            using var content = AsJson(read);
            using var response = await teamAdmin.PutAsync(new Uri($"{ConnectionsRoute}/{connectionIds[JiraDcPayments]}", UriKind.Relative), content);

            SaveAnswer = response.StatusCode;
            SaveAnswerBody = await response.Content.ReadAsStringAsync();
        }

        // --- When: an owner's admin, or a script, saves a Team's or Portfolio's settings ---

        /// <summary>
        /// The owner's settings page saves the setting with the source it shows. A null field is None; a null
        /// source is a client that does not know about sources.
        /// </summary>
        protected async Task WhenTheOwnersAdminSaves(string owner, OwnerSetting setting, string? fieldName, Source? source)
        {
            await SetTheRestAside();
            var settings = await TheSettingsOf(owner);
            SetTheOwnersValue(settings, setting, fieldName is null ? null : await TheFieldId(JiraDcPayments, fieldName), source);
            await PutTheOwner(owner, settings);
        }

        /// <summary>An owner saved with a field of another connection as its own value.</summary>
        protected async Task WhenTheOwnersAdminSavesAFieldOf(string owner, string connection, string fieldName)
        {
            await SetTheRestAside();
            var settings = await TheSettingsOf(owner);
            SetTheOwnersValue(settings, OwnerSetting.ParentOverrideField, await TheFieldId(connection, fieldName), Source.Own);
            await PutTheOwner(owner, settings);
        }

        /// <summary>A script saves back, unchanged, the settings it read.</summary>
        protected async Task WhenAScriptSavesBackWhatItRead(string owner, RoundTrip shape)
        {
            await SetTheRestAside();
            var settings = await TheSettingsOf(owner);

            if (shape == RoundTrip.DroppingWhatItDoesNotKnow)
            {
                DropEverySourceAndDefaultName(settings);
            }

            await PutTheOwner(owner, settings);
        }

        /// <summary>A script saves back settings it read earlier, before the default moved, without any source.</summary>
        protected async Task WhenAScriptSavesBackItsStaleCopy(string owner, JsonObject readEarlier, bool keepingTheSourceItRead)
        {
            await SetTheRestAside();

            if (!keepingTheSourceItRead)
            {
                DropEverySourceAndDefaultName(readEarlier);
            }

            await PutTheOwner(owner, readEarlier);
        }

        /// <summary>A new Team on the connection, created with a body copied from a Team that names nothing.</summary>
        protected async Task WhenANewTeamIsCreated(string name)
        {
            var settings = await TheSettingsOf(FraudSignals);
            settings["id"] = 0;
            settings["name"] = name;
            settings["dataRetrievalValue"] = $"project = {name.Replace(' ', '_')}";
            settings.Remove("concurrencyToken");

            using var content = AsJson(settings);
            using var response = await Client.PostAsync(new Uri("/api/latest/teams", UriKind.Relative), content);
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
            teamIds[name] = JsonNode.Parse(body)!["id"]!.GetValue<int>();
        }

        protected async Task WhenTheNextScheduledUpdateOf(string owner)
        {
            await SetTheRestAside();

            if (teamIds.TryGetValue(owner, out var team))
            {
                Factory.Services.GetRequiredService<ITeamUpdater>().TriggerUpdate(team);
            }
            else
            {
                Factory.Services.GetRequiredService<IPortfolioUpdater>().TriggerUpdate(portfolioIds[owner]);
            }

            await TheQueueGoesIdle();
        }

        // --- Then ---

        protected void ThenTheSaveIsAccepted()
            => Assert.That(SaveAnswer, Is.EqualTo(HttpStatusCode.OK), $"The save was refused: {SaveAnswerBody}");

        protected void ThenTheOwnerSaveIsAccepted() => ThenTheSaveIsAccepted();

        protected void ThenTheSaveIsRefusedWith(HttpStatusCode refusal)
            => Assert.That(SaveAnswer, Is.EqualTo(refusal), $"The save answered {(int)SaveAnswer}: {SaveAnswerBody}");

        /// <summary>The refusal the page shows, read from the answer's code and message.</summary>
        protected (string? Code, string? Message) TheRefusal()
        {
            var answer = JsonNode.Parse(string.IsNullOrWhiteSpace(SaveAnswerBody) ? "{}" : SaveAnswerBody) as JsonObject;
            return (answer?["code"]?.ToString(), answer?["message"]?.ToString() ?? answer?["detail"]?.ToString());
        }

        /// <summary>What the owner's settings page shows for the setting: where it comes from, and what it reads.</summary>
        protected async Task<(string? Source, string? DefaultName, string? Reads)> WhatTheSettingsPageShows(string owner, OwnerSetting setting)
        {
            var settings = await TheSettingsOf(owner);
            var member = TheOwnerMember(setting);
            var effective = settings[$"{TheEffectiveIdMember(setting)}"]?.GetValue<int?>();

            return (settings[$"{member}Source"]?.ToString(), settings[$"{member}DefaultName"]?.ToString(), await TheFieldNamed(JiraDcPayments, effective));
        }

        /// <summary>The same page, as a Team admin without connection rights reads it.</summary>
        protected async Task<(string? Source, string? DefaultName, string? Tracker)> WhatTheTeamAdminSees(string team)
        {
            using var teamAdmin = Factory.CreateClient().AsTeamAdmin(teamIds[team]);
            using var response = await teamAdmin.GetAsync(new Uri($"{TheOwnersRoute(team)}/settings", UriKind.Relative));
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
            var settings = JsonNode.Parse(body)!.AsObject();

            return (settings["parentOverrideSource"]?.ToString(), settings["parentOverrideDefaultName"]?.ToString(), settings["workTrackingSystem"]?.ToString());
        }

        protected async Task<(string? FieldName, string? Unit, string? Source, string? DefaultName)> WhatTheEstimationReadsFor(string owner)
        {
            var settings = await TheSettingsOf(owner);

            return (
                await TheFieldNamed(JiraDcPayments, settings["estimationAdditionalFieldDefinitionId"]?.GetValue<int?>()),
                settings["estimationUnit"]?.ToString(),
                settings["estimationSource"]?.ToString(),
                settings["estimationDefaultName"]?.ToString());
        }

        /// <summary>The field the tracker was last asked to read for this setting of this owner; null when none.</summary>
        protected string? WhatTheTrackerWasAskedToReadFor(string owner, OwnerSetting setting)
            => whatTheTrackerWasAskedToRead.TryGetValue((owner, setting), out var field) ? field : "<never asked>";

        /// <summary>Where the owner's item hangs after its update, as its Work Items page lists it.</summary>
        protected async Task<string> WhereTheItemOfHangs(string owner)
        {
            var item = TheItemOf(owner);
            var route = teamIds.TryGetValue(owner, out var teamId)
                ? $"/api/latest/teams/{teamId}/metrics/wip?asOfDate={DateTime.UtcNow:yyyy-MM-dd}"
                : $"/api/latest/features/references?featureReferences={Uri.EscapeDataString(item)}";

            using var response = await Client.GetAsync(new Uri(route, UriKind.Relative));
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            var listed = JsonNode.Parse(body)!.AsArray().OfType<JsonObject>()
                .FirstOrDefault(row => row["referenceId"]?.GetValue<string>() == item);

            return listed?["parentWorkItemReference"]?.GetValue<string>() ?? $"<{item} was never listed>";
        }

        /// <summary>
        /// What the Portfolio's Feature shows after its update, through the route the Features list reads: how
        /// many Work Items it is sized at, and the Teams that work is split across.
        /// </summary>
        protected async Task<(int SizedAt, IReadOnlyList<string> SplitAcross)> WhatTheFeatureOfShows(string portfolio)
        {
            var item = TheItemOf(portfolio);
            using var response = await Client.GetAsync(new Uri($"/api/latest/features/references?featureReferences={Uri.EscapeDataString(item)}", UriKind.Relative));
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            var feature = JsonNode.Parse(body)!.AsArray().OfType<JsonObject>()
                .FirstOrDefault(row => row["referenceId"]?.GetValue<string>() == item);
            Assert.That(feature?["totalWork"], Is.InstanceOf<JsonObject>(), $"{item} is not listed with its work: {body}");

            var work = feature!["totalWork"]!.AsObject();
            var teamNames = teamIds.ToDictionary(team => $"{team.Value}", team => team.Key);

            return (
                work.Sum(share => share.Value!.GetValue<int>()),
                [.. work.Where(share => share.Value!.GetValue<int>() > 0).Select(share => teamNames.GetValueOrDefault(share.Key, $"<team {share.Key}>")).Order(StringComparer.Ordinal)]);
        }

        /// <summary>Every Team this scenario's Lighthouse holds, by name.</summary>
        protected IReadOnlyList<string> EveryTeam() => [.. teamIds.Keys.Order(StringComparer.Ordinal)];

        /// <summary>The connection's defaults and who follows them, as the connection page reads them.</summary>
        protected async Task<JsonObject> WhatTheConnectionPageReads(string connection) => await TheConnectionAsThePageReadsIt(connection);

        protected async Task<string?> TheDefaultNamedOnTheConnection(ConnectionDefault theDefault)
        {
            var read = await TheConnectionAsThePageReadsIt(JiraDcPayments);
            Assert.That(read[DefaultsMember], Is.InstanceOf<JsonObject>(), "The connection page reads the connection's defaults.");
            var named = read[DefaultsMember]?[TheMember(theDefault)];
            return named is null ? null : TheFieldNamedIn(read, JiraDcPayments, named.GetValue<int?>());
        }

        protected async Task<int?> HowManyFollow(ConnectionDefault theDefault)
        {
            var read = await TheConnectionAsThePageReadsIt(JiraDcPayments);
            return read["usedBy"]?["followers"]?[TheMember(theDefault)]?.GetValue<int?>();
        }

        /// <summary>
        /// The defaults the connection says it offers. Null when it says nothing about them at all, which a
        /// connection offering none must not do: it says so with an empty list.
        /// </summary>
        protected async Task<IReadOnlyList<string>?> WhatTheConnectionOffersAsDefaults(string connection)
        {
            var read = await TheConnectionAsThePageReadsIt(connection);
            return read["supportedDefaults"] is JsonArray offered ? [.. offered.Select(member => member?.ToString() ?? string.Empty)] : null;
        }

        protected async Task<IReadOnlyList<string>> TheFieldsOfTheConnection(string connection)
        {
            var read = await TheConnectionAsThePageReadsIt(connection);
            return [.. FieldsOf(read).Select(field => field?[DisplayName]?.ToString() ?? string.Empty)];
        }

        /// <summary>Every Information entry about a changed default the saves since the scenario's start wrote.</summary>
        protected IReadOnlyList<string> TheLogEntriesAboutChangedDefaults()
            => [.. whatWasLogged.At(LogEventLevel.Information).Skip(logLinesBeforeTheSave).Where(line => line.Contains(" default ", StringComparison.Ordinal) && line.StartsWith("Connection \"", StringComparison.Ordinal))];

        /// <summary>The Teams and Portfolios the Task Manager lists, once the save has had time to queue them.</summary>
        protected async Task<IReadOnlyList<string>> WhatTheTaskManagerListsSoonAfterTheSave(int expected)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            var listed = await TheTeamsAndPortfoliosInTheTaskManager();

            while (listed.Count < expected && DateTime.UtcNow < deadline)
            {
                await Task.Delay(50);
                listed = await TheTeamsAndPortfoliosInTheTaskManager();
            }

            return listed;
        }

        /// <summary>Each update run since the save, as "name Mode", once the queue has gone quiet.</summary>
        protected async Task<IReadOnlyList<string>> WhatRanSinceTheSave(params string[] waitingFor)
        {
            var deadline = DateTime.UtcNow + LongEnoughForAnyUpdateToFinish;
            await TheQueueGoesIdle();
            var history = await TheRefreshHistorySinceTheSave();

            while (!waitingFor.All(owner => history.Exists(entry => entry.EntityName == owner)) && DateTime.UtcNow < deadline)
            {
                await Task.Delay(50);
                await TheQueueGoesIdle();
                history = await TheRefreshHistorySinceTheSave();
            }

            return [.. history.Select(entry => $"{entry.EntityName} {entry.Mode}")];
        }

        // --- The tracker ---

        private static string TheItemOf(string owner) => $"{owner}-1";

        private Mock<IWorkTrackingConnector> ATrackerThatReadsWhatItIsHanded()
        {
            var connector = new Mock<IWorkTrackingConnector>();
            connector.Setup(c => c.SupportsTransitionHistory(It.IsAny<WorkTrackingSystemConnection>())).Returns(false);
            connector.Setup(c => c.SupportsIncrementalSync(It.IsAny<WorkTrackingSystemConnection>())).Returns(true);
            connector.Setup(c => c.GetPredefinedAdditionalFields(It.IsAny<WorkTrackingSystemConnection>())).Returns([]);

            connector
                .Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>(), It.IsAny<CancellationToken>()))
                .Returns<Team, CancellationToken>((team, _) => Task.FromResult<IEnumerable<WorkItem>>(TheWorkItemOf(team)));
            connector
                .Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
                .Returns<Team, IReadOnlyCollection<string>, CancellationToken>((team, wanted, _) => Task.FromResult<IEnumerable<WorkItem>>([.. TheWorkItemOf(team).Where(item => wanted.Contains(item.ReferenceId))]));
            connector
                .Setup(c => c.SweepWorkItemsForTeam(It.IsAny<Team>(), It.IsAny<CancellationToken>()))
                .Returns<Team, CancellationToken>((team, _) => Task.FromResult<IReadOnlyList<RemoteRecordStamp>>([new RemoteRecordStamp(TheItemOf(team.Name), TheItemLastChangedAt)]));

            connector
                .Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>(), It.IsAny<CancellationToken>()))
                .Returns<Portfolio, CancellationToken>((portfolio, _) => Task.FromResult(TheFeatureOf(portfolio)));
            connector
                .Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
                .Returns<Portfolio, IReadOnlyCollection<string>, CancellationToken>((portfolio, wanted, _) => Task.FromResult<List<Feature>>([.. TheFeatureOf(portfolio).Where(feature => wanted.Contains(feature.ReferenceId))]));
            connector
                .Setup(c => c.SweepFeaturesForPortfolio(It.IsAny<Portfolio>(), It.IsAny<CancellationToken>()))
                .Returns<Portfolio, CancellationToken>((portfolio, _) => Task.FromResult<IReadOnlyList<RemoteRecordStamp>>([new RemoteRecordStamp(TheItemOf(portfolio.Name), TheItemLastChangedAt)]));
            connector
                .Setup(c => c.GetParentFeaturesDetails(It.IsAny<Portfolio>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Feature>());
            connector
                .Setup(c => c.SweepParentFeatures(It.IsAny<Portfolio>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<RemoteRecordStamp>().AsReadOnly());

            return connector;
        }

        private List<WorkItem> TheWorkItemOf(Team team)
        {
            var parentField = Remember(team.Name, OwnerSetting.ParentOverrideField, team, team.ParentOverrideAdditionalFieldDefinitionId);
            Remember(team.Name, OwnerSetting.Estimation, team, team.EstimationAdditionalFieldDefinitionId);

            return
            [
                new WorkItem(new WorkItemBase
                {
                    ReferenceId = TheItemOf(team.Name),
                    Name = TheItemOf(team.Name),
                    Type = "Story",
                    State = "In Progress",
                    StateCategory = StateCategories.Doing,
                    Order = "1",
                    ParentReferenceId = WhatTheFieldHolds(parentField),
                }, team)
                {
                    LastChangedRemote = TheItemLastChangedAt,
                },
            ];
        }

        private List<Feature> TheFeatureOf(Portfolio portfolio)
        {
            var parentField = Remember(portfolio.Name, OwnerSetting.ParentOverrideField, portfolio, portfolio.ParentOverrideAdditionalFieldDefinitionId);
            Remember(portfolio.Name, OwnerSetting.DependencyField, portfolio, portfolio.DependencyOverrideAdditionalFieldDefinitionId);
            Remember(portfolio.Name, OwnerSetting.SizeEstimateField, portfolio, portfolio.SizeEstimateAdditionalFieldDefinitionId);
            Remember(portfolio.Name, OwnerSetting.FeatureOwnerField, portfolio, portfolio.FeatureOwnerAdditionalFieldDefinitionId);
            Remember(portfolio.Name, OwnerSetting.Estimation, portfolio, portfolio.EstimationAdditionalFieldDefinitionId);

            return
            [
                new Feature(new WorkItemBase
                {
                    ReferenceId = TheItemOf(portfolio.Name),
                    Name = TheItemOf(portfolio.Name),
                    Type = "Epic",
                    State = "In Progress",
                    StateCategory = StateCategories.Doing,
                    Order = "1",
                    ParentReferenceId = WhatTheFieldHolds(parentField),
                })
                {
                    LastChangedRemote = TheItemLastChangedAt,
                },
            ];
        }

        private string? Remember(string owner, OwnerSetting setting, WorkTrackingSystemOptionsOwner handed, int? fieldId)
        {
            var field = fieldId is null
                ? null
                : handed.WorkTrackingSystemConnection.AdditionalFieldDefinitions.FirstOrDefault(definition => definition.Id == fieldId)?.DisplayName ?? $"<field {fieldId} is not on the connection>";

            whatTheTrackerWasAskedToRead[(owner, setting)] = field;
            return field;
        }

        private static string WhatTheFieldHolds(string? field) => field switch
        {
            CausedBy => WhatCausedByHolds,
            ParentLink => WhatParentLinkHolds,
            _ => TheTrackersOwnParent,
        };

        // --- Seeding (preconditions only, never the expected outcome) ---

        private async Task SeedThePaymentsDepartment()
        {
            await SeedConnection(JiraDcPayments, WorkTrackingSystems.Jira, ThePaymentsFields);
            await SeedConnection(JiraDcTreasury, WorkTrackingSystems.Jira, (TreasuryCausedBy, "customfield_10500"));

            foreach (var team in TheThreeFollowers)
            {
                await SeedTeam(team, ownParentOverride: null);
            }

            await SeedTeam(Ledger, ownParentOverride: CausedBy);

            await SeedPortfolio(CardIssuing, ownsEverything: false);
            await SeedPortfolio(MerchantOnboarding, ownsEverything: true);
        }

        private async Task SeedConnection(string name, WorkTrackingSystems system, params (string DisplayName, string Reference)[] fields)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();

            var connection = new WorkTrackingSystemConnection
            {
                Name = name,
                WorkTrackingSystem = system,
                AuthenticationMethodKey = system switch
                {
                    WorkTrackingSystems.Jira => AuthenticationMethodKeys.JiraDataCenter,
                    WorkTrackingSystems.AzureDevOps => AuthenticationMethodKeys.AzureDevOpsPat,
                    WorkTrackingSystems.Linear => AuthenticationMethodKeys.LinearApiKey,
                    WorkTrackingSystems.ServiceNow => AuthenticationMethodKeys.ServiceNowBasic,
                    _ => AuthenticationMethodKeys.None,
                },
            };

            connection.Options.Add(new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Url, Value = "https://jira.payments.example", IsSecret = false });
            connection.Options.Add(new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Username, Value = "priya@payments.example", IsSecret = false });
            connection.AdditionalFieldDefinitions.AddRange(fields.Select(field => new AdditionalFieldDefinition { DisplayName = field.DisplayName, Reference = field.Reference }));

            repository.Add(connection);
            await repository.Save();
            connectionIds[name] = connection.Id;
        }

        private async Task SeedTeam(string name, string? ownParentOverride)
        {
            using var scope = Factory.Services.CreateScope();
            var services = scope.ServiceProvider;
            var connection = services.GetRequiredService<IRepository<WorkTrackingSystemConnection>>().GetById(connectionIds[JiraDcPayments])!;

            var team = new Team
            {
                Name = name,
                WorkTrackingSystemConnection = connection,
                DoneItemsCutoffDays = 365,
                DataRetrievalValue = $"project = {name.Replace(' ', '_')}",
                WorkItemTypes = ["Story"],
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
                ThroughputHistory = 30,
                ParentOverrideAdditionalFieldDefinitionId = ownParentOverride is null ? null : TheFieldOn(connection, ownParentOverride),
            };

            var repository = services.GetRequiredService<IRepository<Team>>();
            repository.Add(team);
            await repository.Save();
            teamIds[name] = team.Id;
        }

        private async Task SeedPortfolio(string name, bool ownsEverything)
        {
            using var scope = Factory.Services.CreateScope();
            var services = scope.ServiceProvider;
            var connection = services.GetRequiredService<IRepository<WorkTrackingSystemConnection>>().GetById(connectionIds[JiraDcPayments])!;

            var portfolio = new Portfolio
            {
                Name = name,
                WorkTrackingSystemConnection = connection,
                DoneItemsCutoffDays = 365,
                DataRetrievalValue = $"project = {name.Replace(' ', '_')}",
                WorkItemTypes = ["Epic"],
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
            };

            if (ownsEverything)
            {
                portfolio.ParentOverrideAdditionalFieldDefinitionId = TheFieldOn(connection, ParentLink);
                portfolio.DependencyOverrideAdditionalFieldDefinitionId = TheFieldOn(connection, BlockedByKeys);
                portfolio.SizeEstimateAdditionalFieldDefinitionId = TheFieldOn(connection, TShirtSize);
                portfolio.FeatureOwnerAdditionalFieldDefinitionId = TheFieldOn(connection, ProductOwner);
                portfolio.EstimationAdditionalFieldDefinitionId = TheFieldOn(connection, StoryPoints);
                portfolio.EstimationUnit = Points;
            }

            var repository = services.GetRequiredService<IRepository<Portfolio>>();
            repository.Add(portfolio);
            await repository.Save();
            portfolioIds[name] = portfolio.Id;
        }

        private static int TheFieldOn(WorkTrackingSystemConnection connection, string displayName)
            => connection.AdditionalFieldDefinitions.First(field => field.DisplayName == displayName).Id;

        // --- The saves, as the pages send them ---

        private async Task SetTheRestAside()
        {
            lastHistoryEntryBeforeTheSave = (await TheRefreshHistory()).Select(entry => entry.Id).DefaultIfEmpty(0).Max();
            logLinesBeforeTheSave = whatWasLogged.At(LogEventLevel.Information).Count;
        }

        private Task SaveTheConnection(Action<JsonObject> edit, bool? startUpdates) => SaveTheConnection(JiraDcPayments, edit, startUpdates);

        private async Task SaveTheConnection(string connection, Action<JsonObject> edit, bool? startUpdates)
        {
            await SetTheRestAside();
            var read = await TheConnectionAsThePageReadsIt(connection);
            edit(read);
            await Put(connection, read, startUpdates);
        }

        private async Task Put(string connection, JsonObject body, bool? startUpdates)
        {
            var route = startUpdates is null
                ? $"{ConnectionsRoute}/{connectionIds[connection]}"
                : $"{ConnectionsRoute}/{connectionIds[connection]}?startUpdates={(startUpdates.Value ? "true" : "false")}";

            using var content = AsJson(body);
            using var response = await Client.PutAsync(new Uri(route, UriKind.Relative), content);

            SaveAnswer = response.StatusCode;
            SaveAnswerBody = await response.Content.ReadAsStringAsync();
        }

        private async Task PutTheOwner(string owner, JsonObject settings)
        {
            using var content = AsJson(settings);
            using var response = await Client.PutAsync(new Uri(TheOwnersRoute(owner), UriKind.Relative), content);

            SaveAnswer = response.StatusCode;
            SaveAnswerBody = await response.Content.ReadAsStringAsync();
        }

        private async Task<JsonObject> TheConnectionAsThePageReadsIt(string connection)
        {
            using var response = await Client.GetAsync(new Uri(ConnectionsRoute, UriKind.Relative));
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            var read = JsonNode.Parse(body)!.AsArray()
                .OfType<JsonObject>()
                .FirstOrDefault(candidate => candidate["id"]?.GetValue<int>() == connectionIds[connection]);

            Assert.That(read, Is.Not.Null, $"{connection} is not among the connections: {body}");
            return read!;
        }

        private async Task<JsonObject> TheSettingsOf(string owner)
        {
            using var response = await Client.GetAsync(new Uri($"{TheOwnersRoute(owner)}/settings", UriKind.Relative));
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
            return JsonNode.Parse(body)!.AsObject();
        }

        private string TheOwnersRoute(string owner)
            => teamIds.TryGetValue(owner, out var team) ? $"/api/latest/teams/{team}" : $"/api/latest/portfolios/{portfolioIds[owner]}";

        /// <summary>The id of one of the connection's Additional Fields, as the connection page reads it.</summary>
        private async Task<int> TheFieldId(string connection, string fieldName)
            => TheFieldIdIn(await TheConnectionAsThePageReadsIt(connection), fieldName);

        private static int TheFieldIdIn(JsonObject connectionRead, string fieldName)
            => TheField(FieldsOf(connectionRead), fieldName)["id"]!.GetValue<int>();

        /// <summary>The name the connection page shows for one of the connection's Additional Fields.</summary>
        private async Task<string?> TheFieldNamed(string connection, int? fieldId)
            => fieldId is null ? null : TheFieldNamedIn(await TheConnectionAsThePageReadsIt(connection), connection, fieldId);

        private static string? TheFieldNamedIn(JsonObject connectionRead, string connection, int? fieldId)
            => fieldId is null
                ? null
                : FieldsOf(connectionRead).OfType<JsonObject>().FirstOrDefault(field => field["id"]?.GetValue<int>() == fieldId)?[DisplayName]?.GetValue<string>()
                    ?? $"<field {fieldId} is not on {connection}>";

        private static void SetTheOwnersValue(JsonObject settings, OwnerSetting setting, int? fieldId, Source? source)
        {
            settings[TheEffectiveIdMember(setting)] = fieldId;

            if (source is null)
            {
                settings.Remove($"{TheOwnerMember(setting)}Source");
            }
            else
            {
                settings[$"{TheOwnerMember(setting)}Source"] = source.Value.ToString();
            }
        }

        private static void DropEverySourceAndDefaultName(JsonObject settings)
        {
            foreach (var member in settings.Select(pair => pair.Key).Where(key => key.EndsWith("Source", StringComparison.Ordinal) || key.EndsWith("DefaultName", StringComparison.Ordinal)).ToList())
            {
                settings.Remove(member);
            }
        }

        private static JsonObject AnEstimationBundle(int fieldId, string unit) => new()
        {
            ["additionalFieldDefinitionId"] = fieldId,
            ["unit"] = unit,
            ["useNonNumericEstimation"] = false,
            ["estimationCategoryValues"] = new JsonArray(),
        };

        private static JsonObject DefaultsOf(JsonObject read)
        {
            if (read[DefaultsMember] is not JsonObject defaults)
            {
                defaults = [];
                read[DefaultsMember] = defaults;
            }

            return defaults;
        }

        private static string TheMember(ConnectionDefault theDefault) => theDefault switch
        {
            ConnectionDefault.TeamsParentOverrideField => "parentOverrideForTeams",
            ConnectionDefault.PortfoliosParentOverrideField => "parentOverrideForPortfolios",
            ConnectionDefault.PortfoliosDependencyField => "dependencyFieldForPortfolios",
            ConnectionDefault.PortfoliosSizeEstimateField => "sizeEstimateFieldForPortfolios",
            ConnectionDefault.PortfoliosFeatureOwnerField => "featureOwnerFieldForPortfolios",
            ConnectionDefault.TeamsEstimation => "estimationForTeams",
            ConnectionDefault.PortfoliosEstimation => "estimationForPortfolios",
            _ => throw new ArgumentOutOfRangeException(nameof(theDefault), theDefault, null),
        };

        private static string TheOwnerMember(OwnerSetting setting) => setting switch
        {
            OwnerSetting.ParentOverrideField => "parentOverride",
            OwnerSetting.DependencyField => "dependencyOverride",
            OwnerSetting.SizeEstimateField => "sizeEstimate",
            OwnerSetting.FeatureOwnerField => "featureOwner",
            OwnerSetting.Estimation => "estimation",
            _ => throw new ArgumentOutOfRangeException(nameof(setting), setting, null),
        };

        private static string TheEffectiveIdMember(OwnerSetting setting) => $"{TheOwnerMember(setting)}AdditionalFieldDefinitionId";

        private static JsonArray FieldsOf(JsonObject read) => read[AdditionalFieldDefinitions]!.AsArray();

        private static JsonObject TheField(JsonArray fields, string displayName)
            => fields.OfType<JsonObject>().First(field => field[DisplayName]?.GetValue<string>() == displayName);

        private static StringContent AsJson(JsonObject body) => new(body.ToJsonString(), Encoding.UTF8, JsonMediaType);

        // --- Reading the Task Manager and the refresh history ---

        private async Task<List<string>> TheTeamsAndPortfoliosInTheTaskManager()
        {
            using var response = await Client.GetAsync(new Uri(TaskListRoute, UriKind.Relative));
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            using var rows = JsonDocument.Parse(body);
            return [.. rows.RootElement.EnumerateArray()
                .Where(row => row.GetProperty("updateType").GetString() is nameof(UpdateType.Team) or nameof(UpdateType.Features))
                .Select(row => row.GetProperty("name").GetString() ?? "(a row with no name)")];
        }

        /// <summary>One row of Refresh History, as the System Info page reads it.</summary>
        private sealed record ARefreshHistoryRow(int Id, string EntityName, string Mode);

        private async Task<List<ARefreshHistoryRow>> TheRefreshHistory()
        {
            using var response = await Client.GetAsync(new Uri(RefreshLogRoute, UriKind.Relative));
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            using var rows = JsonDocument.Parse(body);
            return [.. rows.RootElement.EnumerateArray()
                .Where(row => IsATeamOrPortfolioRow(row.GetProperty("type")))
                .Select(row => new ARefreshHistoryRow(
                    row.GetProperty("id").GetInt32(),
                    row.GetProperty("entityName").GetString() ?? string.Empty,
                    row.GetProperty("mode").ToString()))
                .OrderBy(row => row.Id)];
        }

        private static bool IsATeamOrPortfolioRow(JsonElement type)
            => type.ValueKind == JsonValueKind.String
                ? type.GetString() is nameof(RefreshType.Team) or nameof(RefreshType.Portfolio)
                : type.GetInt32() is (int)RefreshType.Team or (int)RefreshType.Portfolio;

        private async Task<List<ARefreshHistoryRow>> TheRefreshHistorySinceTheSave()
            => [.. (await TheRefreshHistory()).Where(entry => entry.Id > lastHistoryEntryBeforeTheSave)];

        protected async Task TheQueueGoesIdle()
        {
            var store = Factory.Services.GetRequiredService<IUpdateStatusStore>();
            var deadline = DateTime.UtcNow + LongEnoughForAnyUpdateToFinish;

            while (store.HasActiveWork() && DateTime.UtcNow < deadline)
            {
                await Task.Delay(20);
            }
        }

        // --- The gap between opening a connection save and committing it ---

        private void LetAnotherSaveLandBeforeTheConnectionSaveCommits(IServiceCollection services)
        {
            var registered = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(IConnectionSaveService));
            if (registered?.ImplementationType is null)
            {
                return;
            }

            var implementation = registered.ImplementationType;
            services.Remove(registered);
            services.Add(new ServiceDescriptor(implementation, implementation, registered.Lifetime));
            services.Add(new ServiceDescriptor(
                typeof(IConnectionSaveService),
                provider => new ASaveLandsInTheGap((IConnectionSaveService)provider.GetRequiredService(implementation), () => meanwhileBeforeTheSaveCommits),
                registered.Lifetime));
        }

        private sealed class ASaveLandsInTheGap(IConnectionSaveService inner, Func<Func<Task>?> meanwhile) : IConnectionSaveService
        {
            public Task<ConnectionSaveHandle?> Open(int connectionId, CancellationToken cancellationToken) => inner.Open(connectionId, cancellationToken);

            public async Task<ConnectionSaveOutcome> Commit(ConnectionSaveHandle handle, Guid? concurrencyToken, bool startUpdates, CancellationToken cancellationToken)
            {
                var landsFirst = meanwhile();
                if (landsFirst is not null)
                {
                    await landsFirst();
                }

                return await inner.Commit(handle, concurrencyToken, startUpdates, cancellationToken);
            }
        }
    }
}
