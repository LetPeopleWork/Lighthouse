using System.Text.Json;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// DELIVER step 05-09 — ADR-071 architectural-enforcement suite. These are the five DELIVER-gate
    /// enforcement checks that could only be authored once <see cref="AdditionalFieldDefinition.IsPredefined"/>
    /// and the <c>GetPredefinedAdditionalFields</c> connector port existed (upstream-issues.md UC-5). They
    /// SHARPEN the black-box acceptance scenarios at the seam level and must not byte-duplicate them.
    ///
    /// The KEY discriminator is <see cref="A_settings_save_that_omits_the_predefined_field_preserves_its_persisted_id"/>:
    /// the black-box merge-back AT asserts only "predefined field still surfaced", which a GET-side
    /// auto-registration masks — it cannot tell "reconcile preserved the row" from "reconcile deleted it,
    /// GET re-created it under a NEW id". A churned id orphans <c>WriteBackMapping.AdditionalFieldDefinitionId</c>
    /// references and rule <c>additionalField.{id}</c> keys. This suite asserts the PERSISTED row identity
    /// directly (read back through the repository, before any re-registering GET), which the black-box cannot.
    ///
    /// These are the HTTP/seam checks (#1 id-stability, #3 inbound-only persisted, #4 idempotency persisted)
    /// that reuse the connection GET/PUT driving-port helpers on <see cref="Slice05PredefinedFieldTest"/>.
    /// The pure-unit checks (#2 slot-count, #5 DTO camelCase) live in the sibling fixtures below.
    /// </summary>
    public partial class Slice05PredefinedFieldTest
    {
        // #1 (KEY, back-prop from 05-03) — Reconcile merge-back must preserve the predefined row WITH THE SAME
        // persisted Id. Reading the persisted model right after the omitting PUT (before any GET re-registration)
        // makes this a true discriminator: a delete-and-recreate reconcile leaves zero predefined rows here, and a
        // delete-then-reregister-with-new-id reconcile leaves a DIFFERENT id — either fails this assertion.
        [Test]
        public async Task A_settings_save_that_omits_the_predefined_field_preserves_its_persisted_id()
        {
            var connectionId = GivenAJiraConnection(("Team", "customfield_10050"));

            var body = await WhenTheConnectionConfigurationIsRead(connectionId);
            var originalId = PredefinedFields(body).Single()["id"]!.GetValue<int>();

            var save = await WhenTheAdminSavesTheConnection(connectionId, WithoutPredefinedFields(AsConnectionPayload(body)));
            ThenTheSaveSucceeds(save);

            var persisted = ReadPersistedConnection(connectionId);
            var predefined = persisted.AdditionalFieldDefinitions.Where(f => f.IsPredefined).ToList();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(predefined, Has.Count.EqualTo(1),
                    "exactly one predefined row must survive a settings save that omits it (no delete-recreate).");
                Assert.That(predefined[0].Id, Is.EqualTo(originalId),
                    "the predefined field's persisted Id must be STABLE across an omitting settings save — a churned Id " +
                    "orphans write-back and rule additionalField.{id} references. Delete-recreate reconcile is forbidden.");
            }
        }

        // #3 — Inbound-only asserted on the PERSISTED model (not just served JSON): a settings save cannot change
        // the predefined field's Reference and cannot persist it as a write-back mapping target. Distinct from the
        // black-box A_predefined_field_is_inbound_only, which reads the served DTO — this pins the DB row itself.
        [Test]
        public async Task A_predefined_field_stays_inbound_only_in_the_persisted_model()
        {
            var connectionId = GivenAJiraConnection();

            var body = await WhenTheConnectionConfigurationIsRead(connectionId);
            var originalReference = PredefinedFields(body).Single()["reference"]!.GetValue<string>();

            var mutated = WithAWriteBackMappingTargetingTheFirstPredefinedField(
                WithAChangedReferenceOnEveryPredefinedField(AsConnectionPayload(body), "customfield_tampered"));
            var save = await WhenTheAdminSavesTheConnection(connectionId, mutated);
            ThenTheSaveSucceeds(save);

            var persisted = ReadPersistedConnection(connectionId);
            var predefinedField = persisted.AdditionalFieldDefinitions.Single(f => f.IsPredefined);
            var predefinedIds = persisted.AdditionalFieldDefinitions.Where(f => f.IsPredefined).Select(f => f.Id).ToHashSet();
            var targetsPredefined = persisted.WriteBackMappingDefinitions.Any(
                m => m.AdditionalFieldDefinitionId.HasValue && predefinedIds.Contains(m.AdditionalFieldDefinitionId.Value));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(predefinedField.Reference, Is.EqualTo(originalReference),
                    "a predefined field's Reference is owned by auto-registration and is immutable to a settings save.");
                Assert.That(targetsPredefined, Is.False,
                    "a predefined field is inbound-only — it must never be persisted as a write-back mapping target.");
            }
        }

        // #4 — Auto-registration is get-or-create at the PERSISTENCE level: repeated reads must never leave a
        // duplicate predefined row in the database. The black-box counterpart reads the served JSON twice; this
        // pins the persisted row count, catching a duplicate the serializer could otherwise dedupe or mask.
        [Test]
        public async Task Repeated_registration_persists_exactly_one_predefined_row()
        {
            var connectionId = GivenAJiraConnection();

            await WhenTheConnectionConfigurationIsRead(connectionId);
            await WhenTheConnectionConfigurationIsRead(connectionId);
            await WhenTheConnectionConfigurationIsRead(connectionId);

            var persisted = ReadPersistedConnection(connectionId);
            Assert.That(persisted.AdditionalFieldDefinitions.Count(f => f.IsPredefined), Is.EqualTo(1),
                "auto-registration is get-or-create: repeated reads must never persist a duplicate predefined row.");
        }

        // #6 (drift reconcile — mutation-hardening of EnsurePredefinedAdditionalFieldsRegistered) — a predefined
        // row persisted with a STALE Reference (the stable default resolved before the flagged field key was known)
        // must be RECONCILED IN PLACE to the connector-resolved Reference on the next GET: never duplicated, never
        // left stale. This pins the get-or-create UPDATE branch. An add-only dedup (match on Reference) would append
        // a SECOND predefined row here, orphaning write-back and rule additionalField.{id} references.
        [Test]
        public async Task A_predefined_field_with_a_stale_reference_is_reconciled_in_place_not_duplicated()
        {
            var connectionId = SeedJiraConnectionWithPredefinedField("Flagged", "customfield_stale");

            await WhenTheConnectionConfigurationIsRead(connectionId);

            var persisted = ReadPersistedConnection(connectionId);
            var predefined = persisted.AdditionalFieldDefinitions.Where(f => f.IsPredefined).ToList();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(predefined, Has.Count.EqualTo(1),
                    "a stale predefined row must be reconciled IN PLACE — never duplicated by an add-only registration.");
                Assert.That(predefined[0].Reference, Is.EqualTo("customfield_10001"),
                    "the predefined field's Reference must be updated to the connector-resolved value on registration.");
            }
        }

        private int SeedJiraConnectionWithPredefinedField(string displayName, string reference)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();

            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                AuthenticationMethodKey = AuthenticationMethodKeys.GetDefaultForSystem(WorkTrackingSystems.Jira),
            };
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
            {
                DisplayName = displayName,
                Reference = reference,
                IsPredefined = true,
            });

            repository.Add(connection);
            repository.Save().GetAwaiter().GetResult();
            return connection.Id;
        }

        private WorkTrackingSystemConnection ReadPersistedConnection(int connectionId)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();
            return repository.GetById(connectionId)
                ?? throw new InvalidOperationException($"Connection {connectionId} not found");
        }
    }

    /// <summary>
    /// #2 — Slot-count split (ADR-071): <see cref="AdditionalFieldsHelper.SupportsAdditionalFields"/> counts only
    /// user-owned (<c>!IsPredefined</c>) fields against the non-premium 2-field limit. A pure unit test on the
    /// helper seam — no host — so predefined fields provably do not consume a user field slot.
    /// </summary>
    [TestFixture]
    [Category("epic-5074-blocked-items")]
    [Category("slice-05")]
    public class Slice05AdditionalFieldSlotCountTest
    {
        [Test]
        public void Predefined_fields_do_not_consume_user_field_slots_on_a_non_premium_licence()
        {
            var licenseService = new Mock<ILicenseService>();
            licenseService.Setup(s => s.CanUsePremiumFeatures()).Returns(false);

            var withinUserSlots = new List<AdditionalFieldDefinition>
            {
                Predefined("customfield_flagged_a"),
                Predefined("customfield_flagged_b"),
                UserField("customfield_10050"),
            };
            var atUserSlotLimit = new List<AdditionalFieldDefinition>
            {
                Predefined("customfield_flagged_a"),
                UserField("customfield_10050"),
                UserField("customfield_10051"),
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(withinUserSlots.SupportsAdditionalFields(licenseService.Object), Is.True,
                    "N predefined + 1 user field stays within the non-premium 2-user-field limit — predefined must not count.");
                Assert.That(atUserSlotLimit.SupportsAdditionalFields(licenseService.Object), Is.False,
                    "N predefined + 2 user fields hits the non-premium limit regardless of how many predefined fields exist.");
            }
        }

        private static AdditionalFieldDefinition Predefined(string reference)
            => new() { Reference = reference, DisplayName = "Flagged", IsPredefined = true };

        private static AdditionalFieldDefinition UserField(string reference)
            => new() { Reference = reference, DisplayName = "User Field" };
    }

    /// <summary>
    /// #5 — FE isPredefined DTO split linkage (BE half). The FE editor split
    /// (AdditionalFieldsEditor.predefined.test.tsx, 05-08) relies on the BE serializing
    /// <see cref="AdditionalFieldDefinitionDto.IsPredefined"/> as the camelCase <c>isPredefined</c> property.
    /// One small serialization pin — do NOT duplicate the FE coverage.
    /// </summary>
    [TestFixture]
    [Category("epic-5074-blocked-items")]
    [Category("slice-05")]
    public class Slice05AdditionalFieldDtoSerializationTest
    {
        [Test]
        public void The_additional_field_dto_serializes_is_predefined_as_camel_case()
        {
            var dto = new AdditionalFieldDefinitionDto(new AdditionalFieldDefinition
            {
                Id = 7,
                DisplayName = "Flagged",
                Reference = "customfield_10001",
                IsPredefined = true,
            });

            var serialized = JsonSerializer.SerializeToNode(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();

            Assert.That(serialized["isPredefined"]?.GetValue<bool>(), Is.True,
                "the FE isPredefined DTO split relies on the BE serializing IsPredefined as camelCase 'isPredefined'.");
        }

        [Test]
        public void ToModel_round_trips_the_is_predefined_flag()
        {
            var dto = new AdditionalFieldDefinitionDto
            {
                Id = 7,
                DisplayName = "Flagged",
                Reference = "customfield_10001",
                IsPredefined = true,
            };

            var model = dto.ToModel();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(model.Id, Is.EqualTo(7));
                Assert.That(model.DisplayName, Is.EqualTo("Flagged"));
                Assert.That(model.Reference, Is.EqualTo("customfield_10001"));
                Assert.That(model.IsPredefined, Is.True,
                    "ToModel must carry IsPredefined through so a predefined field survives a DTO round-trip.");
            }
        }
    }
}
