using System.Net;
using System.Text.Json.Nodes;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    internal static class ConcurrencyTokenTestHelpers
    {
        internal const string ConcurrencyConflictCode = "concurrency-conflict";

        internal static async Task<JsonObject> GetJsonObject(HttpClient client, string url)
        {
            var response = await client.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
            return JsonNode.Parse(body)!.AsObject();
        }

        internal static Guid GetToken(JsonObject payload)
        {
            var tokenNode = payload["concurrencyToken"];
            Assert.That(tokenNode, Is.Not.Null,
                "Settings payload must expose concurrencyToken so clients can echo it back on save.");
            return Guid.Parse(tokenNode!.GetValue<string>());
        }

        internal static string GetString(JsonObject payload, string propertyName)
        {
            return payload[propertyName]!.GetValue<string>();
        }
    }
}
