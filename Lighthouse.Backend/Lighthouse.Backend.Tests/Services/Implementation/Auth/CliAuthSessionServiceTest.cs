using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Implementation.Auth;

namespace Lighthouse.Backend.Tests.Services.Implementation.Auth
{
    public class CliAuthSessionServiceTest
    {
        private CliAuthSessionService subject;

        [SetUp]
        public void Setup()
        {
            subject = new CliAuthSessionService();
        }

        [Test]
        public void StartSession_ReturnsUniqueSessionId()
        {
            var (sessionId1, _) = subject.StartSession();
            var (sessionId2, _) = subject.StartSession();

            Assert.That(sessionId1, Is.Not.EqualTo(sessionId2));
        }

        [Test]
        public void StartSession_ReturnsExpiryInFuture()
        {
            var (_, expiresAt) = subject.StartSession();

            Assert.That(expiresAt, Is.GreaterThan(DateTime.UtcNow));
        }

        [Test]
        public void StartSession_ExpiryIsApproximatelyTenMinutesFromNow()
        {
            var before = DateTime.UtcNow;
            var (_, expiresAt) = subject.StartSession();
            var after = DateTime.UtcNow;

            Assert.That(expiresAt, Is.InRange(before.AddMinutes(9.9), after.AddMinutes(10.1)));
        }

        [Test]
        public void PollSession_UnknownSessionId_ReturnsExpired()
        {
            var result = subject.PollSession("unknown-session-id");

            Assert.That(result.Status, Is.EqualTo("expired"));
        }

        [Test]
        public void PollSession_ValidNewSession_ReturnsPending()
        {
            var (sessionId, _) = subject.StartSession();

            var result = subject.PollSession(sessionId);

            Assert.That(result.Status, Is.EqualTo("pending"));
        }

        [Test]
        public void PollSession_ValidNewSession_TokenIsNull()
        {
            var (sessionId, _) = subject.StartSession();

            var result = subject.PollSession(sessionId);

            Assert.That(result.Token, Is.Null);
        }

        [Test]
        public void TryApproveSession_UnknownSessionId_ReturnsFalse()
        {
            var approved = subject.TryApproveSession("unknown-session-id", "user");

            Assert.That(approved, Is.False);
        }

        [Test]
        public void TryApproveSession_ValidSession_ReturnsTrue()
        {
            var (sessionId, _) = subject.StartSession();

            var approved = subject.TryApproveSession(sessionId, "testuser");

            Assert.That(approved, Is.True);
        }

        [Test]
        public void TryApproveSession_AlreadyApproved_ReturnsTrue()
        {
            var (sessionId, _) = subject.StartSession();
            subject.TryApproveSession(sessionId, "testuser");

            var approvedAgain = subject.TryApproveSession(sessionId, "testuser");

            Assert.That(approvedAgain, Is.True);
        }

        [Test]
        public void PollSession_AfterApproval_ReturnsApproved()
        {
            var (sessionId, _) = subject.StartSession();
            subject.TryApproveSession(sessionId, "testuser");

            var result = subject.PollSession(sessionId);

            Assert.That(result.Status, Is.EqualTo("approved"));
        }

        [Test]
        public void PollSession_AfterApproval_ReturnsToken()
        {
            var (sessionId, _) = subject.StartSession();
            subject.TryApproveSession(sessionId, "testuser");

            var result = subject.PollSession(sessionId);

            Assert.That(result.Token, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void PollSession_AfterApproval_ReturnsUserName()
        {
            var (sessionId, _) = subject.StartSession();
            subject.TryApproveSession(sessionId, "testuser");

            var result = subject.PollSession(sessionId);

            Assert.That(result.UserName, Is.EqualTo("testuser"));
        }

        [Test]
        public void PollSession_ApprovedTwice_ReturnsSameToken()
        {
            var (sessionId, _) = subject.StartSession();
            subject.TryApproveSession(sessionId, "testuser");
            var firstPoll = subject.PollSession(sessionId);

            subject.TryApproveSession(sessionId, "testuser");
            var secondPoll = subject.PollSession(sessionId);

            Assert.That(firstPoll.Token, Is.EqualTo(secondPoll.Token));
        }

        [Test]
        public void ValidateToken_UnknownToken_ReturnsFalse()
        {
            var isValid = subject.ValidateToken("not-a-real-token", out var userName);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(isValid, Is.False);
                Assert.That(userName, Is.Null);
            }
        }

        [Test]
        public void ValidateToken_ValidToken_ReturnsTrue()
        {
            var token = ApproveAndGetToken("validuser");

            var isValid = subject.ValidateToken(token, out var userName);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(isValid, Is.True);
                Assert.That(userName, Is.EqualTo("validuser"));
            }
        }

        [Test]
        public void ValidateToken_RevokedToken_ReturnsFalse()
        {
            var token = ApproveAndGetToken("validuser");
            subject.RevokeToken(token);

            var isValid = subject.ValidateToken(token, out var userName);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(isValid, Is.False);
                Assert.That(userName, Is.Null);
            }
        }

        [Test]
        public void RevokeToken_UnknownToken_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => subject.RevokeToken("not-a-real-token"));
        }

        [Test]
        public void RevokeToken_ValidToken_SubsequentValidationFails()
        {
            var token = ApproveAndGetToken("validuser");
            subject.RevokeToken(token);

            var isValid = subject.ValidateToken(token, out _);

            Assert.That(isValid, Is.False);
        }

        [Test]
        public void StartSession_MultipleApprovals_IssuesDifferentTokens()
        {
            var token1 = ApproveAndGetToken("user1");
            var token2 = ApproveAndGetToken("user2");

            Assert.That(token1, Is.Not.EqualTo(token2));
        }

        private string ApproveAndGetToken(string userName)
        {
            var (sessionId, _) = subject.StartSession();
            subject.TryApproveSession(sessionId, userName);
            return subject.PollSession(sessionId).Token!;
        }
    }
}