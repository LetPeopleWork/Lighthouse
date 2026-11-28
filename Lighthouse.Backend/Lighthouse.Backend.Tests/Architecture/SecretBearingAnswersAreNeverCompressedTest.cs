using Lighthouse.Backend.API;
using Lighthouse.Backend.Platform.Host;
using Lighthouse.Backend.Platform.UpdateQueue;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The logs, the embed handshake and the update hub answer with bodies that can hold a secret or log text an
    /// attacker can reach, so a compressed answer of theirs would let an attacker guess the secret from its size.
    /// They stay uncompressed because each class carries the never-compress marker, whatever its routes are called.
    /// The compression rule only ever sees the metadata it is given, so this is what keeps a removed marker from
    /// going unnoticed.
    /// </summary>
    [TestFixture]
    public class SecretBearingAnswersAreNeverCompressedTest
    {
        // The marker does not exist until the rule reads it, and a typed reference to a missing type would stop the
        // whole test project from compiling. It is looked up by name until it exists; then it becomes typeof.
        private const string TheMarkerName = "Lighthouse.Backend.Platform.Host.NeverCompressResponseAttribute";

        private static readonly Type[] ClassesWhoseAnswersCanCarryASecret =
        [
            typeof(LogsController),
            typeof(EmbedHandshakeController),
            typeof(UpdateNotificationHub),
        ];

        [TestCaseSource(nameof(ClassesWhoseAnswersCanCarryASecret))]
        public void Every_answer_that_can_carry_a_secret_or_log_text_is_marked_never_compress(Type answeringClass)
        {
            var markerType = typeof(BreachSafeResponseCompressionProvider).Assembly.GetType(TheMarkerName);

            var carriesTheMarker = markerType is not null
                && answeringClass.GetCustomAttributes(inherit: false).Any(attribute => attribute.GetType() == markerType);

            Assert.That(carriesTheMarker, Is.True, $"WHAT: {answeringClass.Name} does not carry the never-compress marker {TheMarkerName} on its class (marker type found: {markerType is not null}). WHY: its answers can hold a secret or log text next to attacker text, and a compressed answer lets an attacker guess the secret from its size; without the marker the rule compresses it. HOW: put the marker on the class itself, so every action and route of it is covered whatever the route is called.");
        }
    }
}
