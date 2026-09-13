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
        /// The address an instance would send to if nobody told it otherwise - and the reason nobody
        /// may. This is the one collector this product has, and the numbers in it are read as the
        /// count of people using Lighthouse. An instance that fell back here without being told to
        /// would add invented events to that count, so falling back is refused rather than defaulted.
        /// </summary>
        private const string TheOnlyCollectorThereIs = "https://eu.i.posthog.com";

        private const string WhereEventsArePosted = "i/v0/e/";

        private static readonly JsonSerializerOptions Wire = new();

        private int timesItRefusedToFallBack;

        public async Task PublishAsync(
            UsageDataEmitPermit permit, AcceptedUsageDataBatch batch, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(permit);
            ArgumentNullException.ThrowIfNull(batch);

            var address = WhereThisInstanceWasToldToSend();

            if (address is null)
            {
                return;
            }

            var message = JsonSerializer.Serialize(EverythingInTheBatch(permit, batch), Wire);

            using var client = clients.CreateClient(HttpClientName);
            using var content = new StringContent(message, Encoding.UTF8, "application/json");
            using var answer = await client.PostAsync(address, content, cancellationToken);

            // Thrown rather than examined. Whoever called this counts what could not be sent and
            // drops it; there is no second attempt, because a retry is another chance to send
            // something whose consent may have changed in the meantime.
            answer.EnsureSuccessStatusCode();
        }

        private Uri? WhereThisInstanceWasToldToSend()
        {
            var supplied = configuration.CurrentValue.CollectorBaseUrl;

            if (string.IsNullOrWhiteSpace(supplied))
            {
                SayThatNothingIsBeingSent();
                return null;
            }

            return new Uri($"{supplied.TrimEnd('/')}/{WhereEventsArePosted}");
        }

        /// <summary>
        /// Said once, not once per batch. A browser that agreed and left a tab open goes on handing
        /// things in forever, so a line each time would let an instance that cannot send fill its own
        /// disk - and refusing quietly is the one thing that must not happen here, because a usage
        /// event dropping silently is normal by design and looks exactly like working.
        /// </summary>
        private void SayThatNothingIsBeingSent()
        {
            if (Interlocked.Increment(ref timesItRefusedToFallBack) != 1)
            {
                return;
            }

            logger.LogWarning(
                "Usage data: nothing is being sent, because UsageData:CollectorBaseUrl was never set. There is no "
                + "built-in fallback on purpose - {Collector} is the only collector this product has, and an instance "
                + "that sent there without being told to would add invented events to the numbers everybody else is "
                + "counted in. This is reported once rather than once per batch.",
                TheOnlyCollectorThereIs);
        }

        private List<CollectorMessage> EverythingInTheBatch(
            UsageDataEmitPermit permit, AcceptedUsageDataBatch batch)
        {
            var apiKey = configuration.CurrentValue.ProjectApiKey;

            // Asked once for the whole batch rather than per message. These are facts about the
            // instance, and one of them reads the licence and the database behind it.
            var facts = instance.Describe();

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
