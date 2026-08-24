using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// A Delivery that follows a Release elsewhere has to still be following it after Lighthouse is
    /// restarted, so where it follows and which Release it follows there belong in the database
    /// rather than only in the object. Every way of choosing Features is saved and read back here,
    /// with every stored field checked on the way back, so a change that drops one of them on one
    /// kind of Delivery cannot pass on the strength of another kind.
    /// </summary>
    public class DeliverySourceBindingPersistenceTest
    {
        private const string ReleaseSourceKey = "jira-release";
        private const string ReleaseId = "10412";
        private const string DeliveryName = "Autumn Release";
        private const string RuleThatChoosesFeatures = "{\"conditions\":[]}";

        private DbContextOptions<LighthouseAppContext> options = null!;
        private Mock<ICryptoService> cryptoServiceMock = null!;
        private Mock<ILogger<LighthouseAppContext>> appContextLoggerMock = null!;

        [SetUp]
        public void SetUp()
        {
            options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            cryptoServiceMock = new Mock<ICryptoService>();
            appContextLoggerMock = new Mock<ILogger<LighthouseAppContext>>();
        }

        [TestCaseSource(nameof(EveryWayADeliveryChoosesItsFeatures))]
        public async Task A_Delivery_bound_to_a_Release_keeps_its_handler_key_and_source_reference_across_a_save_and_reload(
            DeliverySelectionMode wayOfChoosing,
            string? rule,
            int? ruleVersion,
            string? whereItFollows,
            string? whichRelease)
        {
            var deliveryDate = TestToday.AFutureDate;

            var savedId = await SaveAndForget(() => new Delivery(DeliveryName, deliveryDate, 1)
            {
                SelectionMode = wayOfChoosing,
                RuleDefinitionJson = rule,
                RuleSchemaVersion = ruleVersion,
                SourceKey = whereItFollows,
                SourceReference = whichRelease,
            });

            using var reading = CreateContext();
            var reloaded = await reading.Deliveries.SingleAsync(delivery => delivery.Id == savedId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reloaded.Name, Is.EqualTo(DeliveryName));
                Assert.That(reloaded.Date, Is.EqualTo(deliveryDate));
                Assert.That(reloaded.SelectionMode, Is.EqualTo(wayOfChoosing));
                Assert.That(reloaded.RuleDefinitionJson, Is.EqualTo(rule));
                Assert.That(reloaded.RuleSchemaVersion, Is.EqualTo(ruleVersion));
                Assert.That(reloaded.SourceKey, Is.EqualTo(whereItFollows));
                Assert.That(reloaded.SourceReference, Is.EqualTo(whichRelease));

                // Nothing has gone looking at the Release yet, and until something does, a Delivery
                // that claims to have heard from it would be claiming something nobody checked.
                Assert.That(reloaded.SourceLastSyncedOn, Is.Null);
                Assert.That(reloaded.SourceUnavailableReason, Is.Null);
            }
        }

        /// <summary>
        /// Every Delivery already saved has all four of these columns empty, and reading one back has
        /// to leave it exactly as it was: an instance that never points a Delivery at a Release is
        /// not touched by any of this.
        /// </summary>
        [Test]
        public async Task A_Delivery_that_follows_nothing_reads_back_following_nothing()
        {
            var deliveryDate = TestToday.AFutureDate;

            var savedId = await SaveAndForget(() => new Delivery(DeliveryName, deliveryDate, 1));

            using var reading = CreateContext();
            var reloaded = await reading.Deliveries.SingleAsync(delivery => delivery.Id == savedId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reloaded.SelectionMode, Is.EqualTo(DeliverySelectionMode.Manual));
                Assert.That(reloaded.SourceKey, Is.Null);
                Assert.That(reloaded.SourceReference, Is.Null);
                Assert.That(reloaded.SourceLastSyncedOn, Is.Null);
                Assert.That(reloaded.SourceUnavailableReason, Is.Null);
            }
        }

        /// <summary>
        /// The reason a source stopped resolving is stored as the bare number behind the name, so it
        /// lands in the column an int would have used and reading it back gives the name again
        /// rather than a number nobody translated.
        /// </summary>
        [TestCase(DeliverySourceUnavailableReason.SourceNotFound)]
        [TestCase(DeliverySourceUnavailableReason.SourceHasNoDate)]
        [TestCase(DeliverySourceUnavailableReason.CapabilityWithdrawn)]
        public async Task Why_a_Release_stopped_resolving_survives_a_save_and_reload_as_the_reason_it_was_written_as(DeliverySourceUnavailableReason reason)
        {
            var deliveryDate = TestToday.AFutureDate;
            var heardFromAt = deliveryDate.AddDays(-10);

            var savedId = await SaveAndForget(() => new Delivery(DeliveryName, deliveryDate, 1)
            {
                SelectionMode = DeliverySelectionMode.SourceBound,
                SourceKey = ReleaseSourceKey,
                SourceReference = ReleaseId,
                SourceLastSyncedOn = heardFromAt,
                SourceUnavailableReason = reason,
            });

            using var reading = CreateContext();
            var reloaded = await reading.Deliveries.SingleAsync(delivery => delivery.Id == savedId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reloaded.SourceLastSyncedOn, Is.EqualTo(heardFromAt));
                Assert.That(reloaded.SourceUnavailableReason, Is.EqualTo(reason));
            }
        }

        private static IEnumerable<TestCaseData> EveryWayADeliveryChoosesItsFeatures()
        {
            yield return new TestCaseData(DeliverySelectionMode.Manual, null, null, null, null)
                .SetName("Chosen by hand");
            yield return new TestCaseData(DeliverySelectionMode.RuleBased, RuleThatChoosesFeatures, 1, null, null)
                .SetName("Chosen by a rule");
            yield return new TestCaseData(DeliverySelectionMode.SourceBound, null, null, ReleaseSourceKey, ReleaseId)
                .SetName("Following a Release");
        }

        /// <summary>
        /// The write happens in a context of its own and that context is then gone, so what the read
        /// gets back came out of the database rather than off the object that was just saved.
        /// </summary>
        private async Task<int> SaveAndForget(Func<Delivery> build)
        {
            using var writing = CreateContext();
            var delivery = build();

            writing.Deliveries.Add(delivery);
            await writing.SaveChangesAsync();

            return delivery.Id;
        }

        private LighthouseAppContext CreateContext()
        {
            return new LighthouseAppContext(options, cryptoServiceMock.Object, appContextLoggerMock.Object);
        }
    }
}
