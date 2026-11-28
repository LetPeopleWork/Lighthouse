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
        private static readonly Type[] ClassesWhoseAnswersCanCarryASecret =
        [
            typeof(LogsController),
            typeof(EmbedHandshakeController),
            typeof(UpdateNotificationHub),
        ];

        [TestCaseSource(nameof(ClassesWhoseAnswersCanCarryASecret))]
        public void Every_answer_that_can_carry_a_secret_or_log_text_is_marked_never_compress(Type answeringClass)
        {
            var carriesTheMarker = Attribute.IsDefined(answeringClass, typeof(NeverCompressResponseAttribute), inherit: false);

            Assert.That(carriesTheMarker, Is.True, $"WHAT: {answeringClass.Name} does not carry the never-compress marker {nameof(NeverCompressResponseAttribute)} on its class. WHY: its answers can hold a secret or log text next to attacker text, and a compressed answer lets an attacker guess the secret from its size; without the marker the rule compresses it. HOW: put the marker on the class itself, so every action and route of it is covered whatever the route is called.");
        }
    }
}
