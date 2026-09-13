using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Models.UsageData;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.UsageData;
using Microsoft.Extensions.Options;

namespace Lighthouse.Backend.Services.Implementation.UsageData
{
    /// <summary>
    /// The way out, and the only file that knows where out is. Everything about the collector lives
    /// here - the address, the client used to reach it and how both are registered - so that there is
    /// one answer to "where does this data go" and one place to change it.
    ///
    /// Two instructions ride every message rather than being set once in the collector's own console:
    /// discard the caller's address, and do not look up where that address is. A promise made in
    /// somebody else's settings page holds only while nobody changes it; a promise made in the bytes
    /// we send is one this project's own tests can check.
    /// </summary>
    public sealed class PostHogUsageDataPublisher(
        IHttpClientFactory clients,
        IOptionsMonitor<UsageDataConfiguration> configuration,
        IUsageDataInstanceProperties instance,
        ILighthouseClock clock,
        ILogger<PostHogUsageDataPublisher> logger) : IUsageDataPublisher
    {
        public const string HttpClientName = "UsageDataCollector";

        /// <summary>
        /// Where this goes when nobody says otherwise. It is the one collector this product has, and
        /// what is counted in it is read as how much Lighthouse is used, so what may reach it is
        /// narrowed below rather than left to whoever happens to start a build.
        /// </summary>
        private const string TheOnlyCollectorThereIs = "https://eu.i.posthog.com";

        private const string WhereEventsArePosted = "i/v0/e/";

        private static readonly JsonSerializerOptions Wire = new();

        private static readonly Uri WhereTheBuiltInOneSends =
            new($"{TheOnlyCollectorThereIs}/{WhereEventsArePosted}");

        private int timesABuildNobodyPublishedStayedQuiet;
        private int timesTheAddressSomebodyNamedWasNotOne;

        public async Task PublishAsync(
            UsageDataEmitPermit permit, AcceptedUsageDataBatch batch, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(permit);
            ArgumentNullException.ThrowIfNull(batch);

            // Asked once for the whole batch rather than per message, because one of these reads the
            // licence and the database behind it.
            var facts = instance.Describe();

            var address = WhereThisOneSends(facts);

            if (address is null)
            {
                return;
            }

            var message = JsonSerializer.Serialize(EverythingInTheBatch(permit, batch, facts), Wire);

            using var client = clients.CreateClient(HttpClientName);
            using var content = new StringContent(message, Encoding.UTF8, "application/json");
            using var answer = await client.PostAsync(address, content, cancellationToken);

            // Thrown rather than examined. Whoever called this counts what could not be sent and
            // drops it; there is no second attempt, because a retry is another chance to send
            // something whose consent may have changed in the meantime.
            answer.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Somewhere named on purpose takes whatever this build is - that is how a fork points at its
        /// own collector, and how anyone checks the whole path end to end before shipping.
        ///
        /// The built-in one is narrower, and takes published releases only. Every copy of Lighthouse
        /// carries the address and the key, so anyone running from source would otherwise land in the
        /// figures alongside the people actually using the product. A working tree is also where the
        /// version stops being something a release shares and starts being close to unique to
        /// whoever built it, which is the other reason to leave it out of a count.
        /// </summary>
        private Uri? WhereThisOneSends(UsageDataInstanceFacts facts)
        {
            var named = configuration.CurrentValue.CollectorBaseUrl;

            if (!string.IsNullOrWhiteSpace(named))
            {
                var address = EventsPostedTo(named);

                if (address is null)
                {
                    SayTheAddressSomebodyNamedIsNotOne(named);
                }

                return address;
            }

            if (!facts.IsPublishedRelease)
            {
                SayThatABuildNobodyPublishedSendsNothing();
                return null;
            }

            return WhereTheBuiltInOneSends;
        }

        /// <summary>
        /// Reads what somebody typed, or says it cannot be read. It has to be a whole web address -
        /// scheme and host both - because the setting most likely to be got wrong is the host on its
        /// own, with the https left off, and a half-address has nowhere to post to.
        /// </summary>
        private static Uri? EventsPostedTo(string named)
        {
            var posted = $"{named.Trim().TrimEnd('/')}/{WhereEventsArePosted}";

            return Uri.TryCreate(posted, UriKind.Absolute, out var address)
                && (address.Scheme == Uri.UriSchemeHttp || address.Scheme == Uri.UriSchemeHttps)
                ? address
                : null;
        }

        /// <summary>
        /// The instance keeps running, and says so once. Refusing to start would turn a typo in an
        /// optional setting into an outage for the whole product, which is a far worse outcome than
        /// usage data not being sent - but being told nothing is worse still, because a half-typed
        /// address would otherwise disable the feature for the life of the process while looking
        /// exactly like an instance where nobody agreed.
        ///
        /// Said once rather than once per batch, for the reason the line below carries.
        /// </summary>
        private void SayTheAddressSomebodyNamedIsNotOne(string named)
        {
            if (Interlocked.Increment(ref timesTheAddressSomebodyNamedWasNotOne) != 1)
            {
                return;
            }

            logger.LogWarning(
                // Stryker disable once String: the behaviour checked here is that a mistyped address
                // is reported exactly once, and that the line quotes back what was actually set. How
                // the correction is explained to whoever reads it is not something to freeze.
                "Usage data: somebody agreed on this instance, but nothing is being sent, because "
                + "UsageData:CollectorBaseUrl is set to {Named}, which is not a web address anything can post to. "
                + "It needs the scheme as well as the host, as in https://example.com. Nothing will be sent until "
                + "that is corrected or the setting is cleared. Reported once rather than once per batch.",
                named);
        }

        /// <summary>
        /// Said once, not once per batch. A browser that agreed and left a tab open goes on handing
        /// things in forever, so a line each time would let a developer's own machine fill its own
        /// disk - and staying quiet about it is the one thing that must not happen here, because a
        /// usage event dropping silently is normal by design and looks exactly like working.
        /// </summary>
        private void SayThatABuildNobodyPublishedSendsNothing()
        {
            if (Interlocked.Increment(ref timesABuildNobodyPublishedStayedQuiet) != 1)
            {
                return;
            }

            logger.LogWarning(
                // Stryker disable once String: the behaviour checked here is that a build nobody
                // published says so once rather than dropping events in silence. The sentence that
                // tells a reader how to send anyway is not something to freeze.
                "Usage data: somebody agreed on this instance, but nothing is being sent, because this is not a "
                + "published release and no collector was named. Builds nobody published are kept out of the shared "
                + "figures on purpose. Set UsageData:CollectorBaseUrl to send anyway - to {Collector} to join those "
                + "figures, or anywhere else to watch what would be sent. Reported once rather than once per batch.",
                TheOnlyCollectorThereIs);
        }

        private List<CollectorMessage> EverythingInTheBatch(
            UsageDataEmitPermit permit, AcceptedUsageDataBatch batch, UsageDataInstanceFacts facts)
        {
            var apiKey = configuration.CurrentValue.ProjectApiKey;

            return [.. batch.Events.Select(reported => new CollectorMessage(
                apiKey,
                reported.Name.ToString(),
                permit.AnalyticsId,
                clock.Now.AddMilliseconds(-reported.OffsetMs),
                new WhatEachMessageCarries(
                    UsageDataRoutePatterns.All[reported.Route],
                    facts.Version,
                    facts.DeploymentMode.ToString(),
                    facts.LicenceTier,
                    facts.AuthenticationEnabled,
                    Ip: null,
                    GeoIpDisable: true)))];
        }

        private sealed record CollectorMessage(
            [property: JsonPropertyName("api_key")] string? ApiKey,
            [property: JsonPropertyName("event")] string Event,
            [property: JsonPropertyName("distinct_id")] string DistinctId,
            [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
            [property: JsonPropertyName("properties")] WhatEachMessageCarries Properties);

        /// <summary>
        /// The address of the page that was opened, as this application publishes it rather than as
        /// the browser sent it; the four facts about the instance, none of which the browser is ever
        /// asked for; and the two instructions that keep the caller's own address out of what the
        /// collector stores.
        /// </summary>
        private sealed record WhatEachMessageCarries(
            [property: JsonPropertyName("route")] string Route,
            [property: JsonPropertyName("version")] string Version,
            [property: JsonPropertyName("deployment_mode")] string DeploymentMode,
            [property: JsonPropertyName("licence_tier")] string LicenceTier,
            [property: JsonPropertyName("auth_enabled")] bool AuthenticationEnabled,
            [property: JsonPropertyName("$ip")] string? Ip,
            [property: JsonPropertyName("$geoip_disable")] bool GeoIpDisable);
    }

    /// <summary>
    /// The client the way out sends through, and the way out itself. Registered here beside the
    /// address instead of in the composition root: one file naming the collector is what makes there
    /// be a single answer to where this data goes, and a mention anywhere else takes that away.
    /// </summary>
    public static class UsageDataPublishing
    {
        public static IServiceCollection AddUsageDataPublishing(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddHttpClient(PostHogUsageDataPublisher.HttpClientName, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            });

            services.AddSingleton<IUsageDataDeploymentModeResolver, UsageDataDeploymentModeResolver>();
            services.AddSingleton<IUsageDataInstanceProperties, UsageDataInstanceProperties>();
            services.AddSingleton<IUsageDataPublisher, PostHogUsageDataPublisher>();

            return services;
        }
    }
}
