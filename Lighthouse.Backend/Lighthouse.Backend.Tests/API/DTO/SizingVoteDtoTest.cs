using System.Text.Json;
using System.Text.Json.Serialization;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.Refinement;

namespace Lighthouse.Backend.Tests.API.DTO
{
    [TestFixture]
    public class SizingVoteDtoTest
    {
        // The host reads every enum leniently, numbers included, so the vote has to hold its own line against that.
        private static readonly JsonSerializerOptions HostOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() },
        };

        [TestCase("""{"answer":"0","channel":"Web"}""")]
        [TestCase("""{"answer":0,"channel":"Web"}""")]
        [TestCase("""{"answer":"2","channel":"Web"}""")]
        [TestCase("""{"answer":"Maybe","channel":"Web"}""")]
        [TestCase("""{"answer":"Yes, No","channel":"Web"}""")]
        [TestCase("""{"answer":"Yes","channel":"1"}""")]
        [TestCase("""{"answer":"Yes","channel":3}""")]
        [TestCase("""{"answer":"Yes","channel":"Email"}""")]
        public void An_answer_or_channel_that_is_not_one_of_the_names_is_refused(string body)
        {
            Assert.That(() => JsonSerializer.Deserialize<SizingVoteDto>(body, HostOptions), Throws.InstanceOf<JsonException>());
        }

        [Test]
        public void A_refused_answer_names_the_answers_there_are()
        {
            var refusal = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SizingVoteDto>("""{"answer":"Maybe","channel":"Web"}""", HostOptions));

            Assert.That(refusal!.Message, Does.Contain("Expected one of Yes, YesBut, No."));
        }

        [TestCase("""{"answer":0,"channel":"Web"}""")]
        [TestCase("""{"answer":true,"channel":"Web"}""")]
        [TestCase("""{"answer":{},"channel":"Web"}""")]
        [TestCase("""{"answer":["Yes"],"channel":"Web"}""")]
        public void An_answer_that_is_not_a_string_is_refused_naming_the_answers_there_are(string body)
        {
            var refusal = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SizingVoteDto>(body, HostOptions));

            Assert.That(refusal!.Message, Does.Contain("Expected one of Yes, YesBut, No."));
        }

        [TestCase("Yes", "Web", SizingAnswer.Yes, SizingChannel.Web)]
        [TestCase("YesBut", "LiveSession", SizingAnswer.YesBut, SizingChannel.LiveSession)]
        [TestCase("No", "Cli", SizingAnswer.No, SizingChannel.Cli)]
        [TestCase("No", "Assistant", SizingAnswer.No, SizingChannel.Assistant)]
        public void An_answer_and_channel_named_as_declared_are_read_as_declared(string answer, string channel, SizingAnswer expectedAnswer, SizingChannel expectedChannel)
        {
            var vote = JsonSerializer.Deserialize<SizingVoteDto>($$"""{"answer":"{{answer}}","channel":"{{channel}}"}""", HostOptions);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(vote?.Answer, Is.EqualTo(expectedAnswer));
                Assert.That(vote?.Channel, Is.EqualTo(expectedChannel));
            }
        }
    }
}
