using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// Epic #5511 slice 03. The pair survives a round trip, and anything unreadable reads as nothing
    /// recorded rather than throwing.
    ///
    /// The forgiveness matters more than it looks. This value is written by whichever replica handled a
    /// transition, so during a rolling upgrade it can be read by a build that did not write it - and the
    /// alternative to answering "nothing recorded" is costing an operator the whole task list over one
    /// malformed field, at exactly the moment they are trying to find out what the instance is doing.
    /// </summary>
    [TestFixture]
    public class UpdateMomentsTest
    {
        private static readonly DateTimeOffset Admitted = new(2031, 4, 17, 9, 30, 0, TimeSpan.Zero);

        [Test]
        public void BothMoments_SurviveARoundTrip()
        {
            var written = new UpdateMoments(Admitted, Admitted.AddMinutes(5)).ToStorageValue();

            var read = UpdateMoments.Parse(written);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.QueuedAt, Is.EqualTo(Admitted));
                Assert.That(read.StartedAt, Is.EqualTo(Admitted.AddMinutes(5)));
            }
        }

        [Test]
        public void WorkThatIsOnlyWaiting_KeepsItsAdmissionAndHasNoStart()
        {
            var read = UpdateMoments.Parse(new UpdateMoments(Admitted, null).ToStorageValue());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.QueuedAt, Is.EqualTo(Admitted),
                    "Waiting work still has to say how long it has been waiting.");
                Assert.That(read.StartedAt, Is.Null,
                    "It has not started, and a stand-in start moment would make a queued row read as running.");
            }
        }

        [Test]
        public void WorkStartedByAReplicaThatNeverRecordedItsAdmission_KeepsTheStartItDoesHave()
        {
            var read = UpdateMoments.Parse(new UpdateMoments(null, Admitted).ToStorageValue());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.QueuedAt, Is.Null);
                Assert.That(read.StartedAt, Is.EqualTo(Admitted),
                    "Mid-upgrade an entry can be started by this build after an older one admitted it. Losing the "
                    + "half that was recorded would cost the row a duration it can honestly report.");
            }
        }

        [Test]
        public void AMomentAtTheUnixEpoch_IsAMomentRatherThanAnAbsence()
        {
            var read = UpdateMoments.Parse(new UpdateMoments(DateTimeOffset.UnixEpoch, null).ToStorageValue());

            Assert.That(read.QueuedAt, Is.EqualTo(DateTimeOffset.UnixEpoch),
                "Zero milliseconds is a real instant, and an encoding that cannot tell it from an empty field "
                + "silently drops one.");
        }

        [TestCase(null, TestName = "Parse_Null_ReadsAsNothingRecorded")]
        [TestCase("", TestName = "Parse_Empty_ReadsAsNothingRecorded")]
        [TestCase("not a pair at all", TestName = "Parse_WithNoSeparator_ReadsAsNothingRecorded")]
        [TestCase("|", TestName = "Parse_WithBothHalvesBlank_ReadsAsNothingRecorded")]
        [TestCase("tomorrow|yesterday", TestName = "Parse_WithUnreadableHalves_ReadsAsNothingRecorded")]
        [TestCase("1743500000000", TestName = "Parse_WithOnlyOneHalfAndNoSeparator_ReadsAsNothingRecorded")]

        // A number can parse as a long and still be nowhere near an instant this type can express, and the
        // likeliest source is the very thing this format is supposed to survive: a later build writing a
        // finer unit into the same field. Today in microseconds is 1.79e15 and in ticks 6.4e17 - both are
        // ordinary-looking numbers, and both are out of range.
        [TestCase("9223372036854775807|9223372036854775807", TestName = "Parse_AtLongMaxValue_ReadsAsNothingRecorded")]
        [TestCase("-9223372036854775808|-9223372036854775808", TestName = "Parse_AtLongMinValue_ReadsAsNothingRecorded")]
        [TestCase("1790000000000000|1790000000000000", TestName = "Parse_WhatMicrosecondsWouldLookLike_ReadsAsNothingRecorded")]
        [TestCase("638500000000000000|638500000000000000", TestName = "Parse_WhatTicksWouldLookLike_ReadsAsNothingRecorded")]
        [TestCase("253402300800000|253402300800000", TestName = "Parse_OneMillisecondPastTheLastExpressibleInstant_ReadsAsNothingRecorded")]
        public void SomethingUnreadable_ReadsAsNothingRecordedRatherThanThrowing(string? written)
        {
            var read = UpdateMoments.Parse(written);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.QueuedAt, Is.Null);
                Assert.That(read.StartedAt, Is.Null);
            }
        }

        [Test]
        public void OneUnreadableHalf_DoesNotCostTheOtherOne()
        {
            var read = UpdateMoments.Parse($"{Admitted.ToUnixTimeMilliseconds()}|rubbish");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.QueuedAt, Is.EqualTo(Admitted));
                Assert.That(read.StartedAt, Is.Null);
            }
        }

        /// <summary>
        /// The written form is read by replicas running a different build of Lighthouse, so it is a contract
        /// between versions rather than an internal detail. Pinned exactly, because a change to it is
        /// invisible on one node and only shows up as every row losing its duration mid-upgrade.
        /// </summary>
        [Test]
        public void TheWrittenForm_IsTheTwoMomentsInUnixMillisecondsSeparatedByABar()
        {
            var written = new UpdateMoments(Admitted, Admitted.AddMinutes(5)).ToStorageValue();

            Assert.That(written, Is.EqualTo($"{Admitted.ToUnixTimeMilliseconds()}|{Admitted.AddMinutes(5).ToUnixTimeMilliseconds()}"));
        }

        [Test]
        public void WorkThatIsOnlyWaiting_IsWrittenWithAnEmptySecondHalf()
        {
            var written = new UpdateMoments(Admitted, null).ToStorageValue();

            Assert.That(written, Is.EqualTo($"{Admitted.ToUnixTimeMilliseconds()}|"));
        }

        [Test]
        public void TheEarliestExpressibleInstant_IsStillAMoment()
        {
            var read = UpdateMoments.Parse($"{DateTimeOffset.MinValue.ToUnixTimeMilliseconds()}|");

            Assert.That(read.QueuedAt, Is.Not.Null,
                "The range guard has to refuse what cannot be expressed and nothing else. Both ends of it.");
        }

        [Test]
        public void TheLastExpressibleInstant_IsStillAMoment()
        {
            var read = UpdateMoments.Parse($"{DateTimeOffset.MaxValue.ToUnixTimeMilliseconds()}|");

            Assert.That(read.QueuedAt, Is.Not.Null,
                "The range guard has to refuse what cannot be expressed and nothing else; taking the boundary "
                + "with it would be a second way to lose a moment that was recorded correctly.");
        }

        [Test]
        public void NothingRecorded_IsBothHalvesAbsent()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(UpdateMoments.NothingRecorded.QueuedAt, Is.Null);
                Assert.That(UpdateMoments.NothingRecorded.StartedAt, Is.Null);
            }
        }
    }
}
