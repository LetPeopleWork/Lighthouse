namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>A type next to the write-back round that stays where it is, with the context that owns it and why it does not move.</summary>
    internal sealed record WriteBackNeighbourThatStays(string FullName, string Owner, string Reason);

    /// <summary>
    /// The write-back round decides what is written back to a tracker and when, staged per refresh round. Its five classes and
    /// three ports move out of the loose Services namespaces into Forecasting's Application, under WriteBack. This map names
    /// each moving type with its old and its new namespace, and each neighbour that stays with its owner and reason. Names are
    /// strings, so the map holds before, during and after the move; the compiled code decides whether it is right.
    /// </summary>
    internal static class WriteBackMembership
    {
        public static string TheArea => "Forecasting";

        public static string TheNewNamespace => string.Join('.', "Lighthouse.Backend.Modules.Forecasting.Application", "WriteBack");

        public static IReadOnlyList<MovingType> MovingTypes { get; } =
        [
            Moving("WriteBackCollector", TheLooseImplementation),
            Moving("WriteBackRound", TheLooseImplementation),
            Moving("WriteBackRoundContext", TheLooseImplementation),
            Moving("WriteBackService", TheLooseImplementation),
            Moving("WriteBackTriggerService", TheLooseImplementation),
            Moving("IWriteBackCollector", TheLooseInterfaces),
            Moving("IWriteBackService", TheLooseInterfaces),
            Moving("IWriteBackTriggerService", TheLooseInterfaces),
        ];

        public static IReadOnlyList<WriteBackNeighbourThatStays> NeighboursThatStay { get; } =
        [
            Kernel("WriteBackMappingDefinition", "an entity the database stores"),
            Kernel("WriteBackAppliesTo", "part of the stored mapping definition"),
            Kernel("WriteBackTargetValueType", "part of the stored mapping definition"),
            Kernel("WriteBackValueSource", "stored by its number in the mapping definition"),
            Kernel("WriteBackValueSources", "names the stored value sources"),
            Kernel("WriteBackFieldUpdate", "on the signature of Tracker integration's write-back port"),
            Kernel("WriteBackResult", "on the signature of Tracker integration's write-back port"),
            Kernel("WriteBackItemResult", "on the signature of Tracker integration's write-back port"),
            Kernel("NotificationSuppression", "read by the tracker connectors when they write"),
            new("Lighthouse.Backend.Modules.TrackerIntegration.Application.IWriteBackTarget", "Tracker integration", "translating a write into a tracker's fields is the tracker connectors' job, not the round's"),
            new("Lighthouse.Backend.API.DTO.WriteBackMappingDefinitionDto", "connection settings", "the shape the API shows a mapping in; a driving adapter"),
            new("Lighthouse.Backend.API.Helpers.WriteBackMappingValidator", "connection settings", "checks a mapping a user saves; a driving adapter"),
            new("Lighthouse.Backend.API.Helpers.WriteBackMappingsHelper", "connection settings", "maps saved mappings for the API; a driving adapter"),
            new("Lighthouse.Backend.Platform.UpdateQueue.UpdateQueueService", "the platform lane", "creates, hands over and flushes the round; it calls the round and is not part of it"),
            new("Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.TeamUpdater", "the update code", "stages its writes into the round; it calls the round and is not part of it"),
            new("Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.PortfolioUpdater", "the update code", "stages its writes into the round; it calls the round and is not part of it"),
            new("Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.UpdateServiceBase`1", "the update code", "resolves the round for the updaters; it calls the round and is not part of it"),
            new("Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastUpdater", "Forecasting", "triggers the round after a forecast; an adapter of Forecasting, not its Application"),
        ];

        /// <summary>
        /// The Work Items outbound rules that refuse a Work Items Application type reaching a type in the round's new namespace,
        /// measured from those rules before the move. Today none does: Work Items refuses the round only by its old full names,
        /// so a later step that takes those names away must give Work Items another way to refuse the round.
        /// </summary>
        public static IReadOnlyList<string> WorkItemsRulesRefusingTheRoundInItsNewPlace { get; } = [];

        private static string TheLooseImplementation => string.Join('.', "Lighthouse.Backend.Services", "Implementation");

        private static string TheLooseInterfaces => string.Join('.', "Lighthouse.Backend.Services", "Interfaces");

        private static MovingType Moving(string name, string oldNamespace) => new(name, TheArea, oldNamespace, TheNewNamespace);

        private static WriteBackNeighbourThatStays Kernel(string name, string reason)
            => new($"Lighthouse.Backend.Models.WriteBack.{name}", "the shared kernel", reason);
    }
}
