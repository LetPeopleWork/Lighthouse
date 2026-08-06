using Lighthouse.Backend.Models.Auth;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.Models.Auth
{
    // Epic 5146 slice 02a (#5641) — ADR-129. The Forge resolver reads these property names off the
    // wire and branches on the refusal code, so a rename breaks it with no compiler and no server
    // error. The default of every string is asserted too: a DTO that ships a placeholder instead of
    // an empty value is a token the resolver would dutifully send back to the entry point.
    [TestFixture]
    public class EmbedSessionContractShapeTest
    {
        private static readonly JsonSerializerOptions WireOptions = new(JsonSerializerDefaults.Web);

        [Test]
        public void EmbedSessionTokenResponse_WireShape_IsTokenExpiresAtAndEmbedUrl()
        {
            using var document = JsonDocument.Parse(JsonSerializer.Serialize(new EmbedSessionTokenResponse(), WireOptions));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(document.RootElement.GetProperty("token").GetString(), Is.Empty);
                Assert.That(document.RootElement.GetProperty("embedUrl").GetString(), Is.Empty);
                Assert.That(document.RootElement.TryGetProperty("expiresAt", out _), Is.True,
                    "the resolver re-mints on expiry, so it needs the expiry it was given");
            }
        }

        [Test]
        public void EmbedSessionRefusal_WireShape_IsReasonAndMessage()
        {
            using var document = JsonDocument.Parse(JsonSerializer.Serialize(new EmbedSessionRefusal(), WireOptions));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(document.RootElement.GetProperty("reason").GetString(), Is.Empty);
                Assert.That(document.RootElement.GetProperty("message").GetString(), Is.Empty);
            }
        }

        [Test]
        public void EmbedSessionTokenMintResult_UnsetToken_IsEmpty()
        {
            Assert.That(new EmbedSessionTokenMintResult().Token, Is.Empty);
        }

        [Test]
        public void EmbedSessionToken_UnsetIdentifiers_AreNull()
        {
            var token = new EmbedSessionToken();

            using (Assert.EnterMultipleScope())
            {
                // ADR-132 D65: null, not empty. IX_EmbedSessionTokens_TokenId is unique and exempts
                // NULLs; an empty-string default would collide across refusal rows and fail the
                // IS NULL arm of CK_EmbedSessionTokens_GrantOrRefusal. The original rationale —
                // that no unset row should carry a real, lookupable identifier — is served by null.
                Assert.That(token.TokenId, Is.Null,
                    "a default sentinel would be a real, lookupable TokenId shared by every unset row");
                Assert.That(token.SecretHash, Is.Null);
                Assert.That(token.RedeemedAt, Is.Null);
                Assert.That(token.RevokedAt, Is.Null);
            }
        }

        [Test]
        public void EmbedSessionTokenRedemption_Refused_CarriesNoSuccessAndNoApiKey()
        {
            var refused = EmbedSessionTokenRedemption.Refused;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.Succeeded, Is.False,
                    "every refusal path returns this one value — a truthy Refused signs everyone in");
                Assert.That(refused.ApiKeyId, Is.Zero);
            }
        }
    }
}
