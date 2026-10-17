using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    [TestFixture]
    [Category("epic-6303-configure-once-per-connection")]
    public class IncomingSettingTest
    {
        private const int TheOwnersField = 5;
        private const int TheConnectionsDefault = 9;
        private const int AnotherField = 7;

        public enum Sent
        {
            Nothing,
            WhatTheOwnerReads,
            AnotherField,
        }

        [TestCase(SettingSource.ConnectionDefault, null, Sent.Nothing, SettingSource.ConnectionDefault, null)]
        [TestCase(SettingSource.ConnectionDefault, null, Sent.WhatTheOwnerReads, SettingSource.ConnectionDefault, null)]
        [TestCase(SettingSource.ConnectionDefault, null, Sent.AnotherField, SettingSource.Own, AnotherField)]
        [TestCase(SettingSource.Own, null, Sent.Nothing, SettingSource.Own, TheOwnersField)]
        [TestCase(SettingSource.Own, null, Sent.WhatTheOwnerReads, SettingSource.Own, TheOwnersField)]
        [TestCase(SettingSource.Own, null, Sent.AnotherField, SettingSource.Own, AnotherField)]
        [TestCase(SettingSource.ConnectionDefault, SettingSource.Own, Sent.Nothing, SettingSource.Own, null)]
        [TestCase(SettingSource.ConnectionDefault, SettingSource.Own, Sent.WhatTheOwnerReads, SettingSource.Own, TheConnectionsDefault)]
        [TestCase(SettingSource.ConnectionDefault, SettingSource.Own, Sent.AnotherField, SettingSource.Own, AnotherField)]
        [TestCase(SettingSource.Own, SettingSource.Own, Sent.Nothing, SettingSource.Own, null)]
        [TestCase(SettingSource.Own, SettingSource.Own, Sent.WhatTheOwnerReads, SettingSource.Own, TheOwnersField)]
        [TestCase(SettingSource.Own, SettingSource.Own, Sent.AnotherField, SettingSource.Own, AnotherField)]
        [TestCase(SettingSource.ConnectionDefault, SettingSource.ConnectionDefault, Sent.Nothing, SettingSource.ConnectionDefault, null)]
        [TestCase(SettingSource.ConnectionDefault, SettingSource.ConnectionDefault, Sent.WhatTheOwnerReads, SettingSource.ConnectionDefault, null)]
        [TestCase(SettingSource.ConnectionDefault, SettingSource.ConnectionDefault, Sent.AnotherField, SettingSource.ConnectionDefault, null)]
        [TestCase(SettingSource.Own, SettingSource.ConnectionDefault, Sent.Nothing, SettingSource.ConnectionDefault, null)]
        [TestCase(SettingSource.Own, SettingSource.ConnectionDefault, Sent.WhatTheOwnerReads, SettingSource.ConnectionDefault, null)]
        [TestCase(SettingSource.Own, SettingSource.ConnectionDefault, Sent.AnotherField, SettingSource.ConnectionDefault, null)]
        public void A_sent_setting_is_applied_as_sent_when_it_carries_a_source_and_only_as_a_new_value_when_it_does_not(
            SettingSource storedSource, SettingSource? sentSource, Sent sent, SettingSource expectedSource, int? expectedOwnValue)
        {
            var stored = storedSource == SettingSource.Own
                ? new StoredSetting(SettingSource.Own, TheOwnersField)
                : new StoredSetting(SettingSource.ConnectionDefault, null);
            var reads = storedSource == SettingSource.Own ? TheOwnersField : TheConnectionsDefault;
            int? sentValue = sent switch
            {
                Sent.Nothing => null,
                Sent.WhatTheOwnerReads => reads,
                _ => AnotherField,
            };

            var applied = IncomingSetting.AppliedTo(stored, reads, sentSource, sentValue);

            Assert.That(applied, Is.EqualTo(new StoredSetting(expectedSource, expectedOwnValue)));
        }
    }
}
