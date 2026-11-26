namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// A string in a test file that names a moving write-back round type or its file by its old Services path, and the text it
    /// reads after the move. MayLeave marks a string a later step takes away altogether, such as an entry of a list that is
    /// deleted once the round has a namespace of its own.
    /// </summary>
    internal sealed record WriteBackRename(string File, string OldText, string NewText, bool MayLeave = false)
    {
        public RenamedString AsRenamedString() => new(File, OldText, NewText);
    }

    /// <summary>
    /// Every string in the test project that names a moving write-back round type by its old Services path, taken from a search
    /// of the test project before the move, as old and new text. Each file holds exactly one of the two forms, so the list holds
    /// before, during and after the move and is never edited by a later step.
    /// </summary>
    internal static class WriteBackStringsTheMoveRenames
    {
        private const string Architecture = "Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/";

        public static IReadOnlyList<WriteBackRename> Entries { get; } =
        [
            new(Architecture + "QuietWriteBackSeamArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.WriteBackTriggerService", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.WriteBackTriggerService", MayLeave: true),
            new(Architecture + "QuietWriteBackSeamArchUnitTest.cs", "Lighthouse.Backend.Services.Interfaces.IWriteBackService", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.IWriteBackService", MayLeave: true),
            new(Architecture + "FlowMetricsInboundApplicationArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.WriteBackTriggerService", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.WriteBackTriggerService", MayLeave: true),
            new(Architecture + "OrganisationSetupMembership.cs", "Services/Implementation/WriteBackRound.cs", "Modules/Forecasting/Application/WriteBack/WriteBackRound.cs"),
            new(Architecture + "OrganisationSetupMembership.cs", "Services/Implementation/WriteBackRoundContext.cs", "Modules/Forecasting/Application/WriteBack/WriteBackRoundContext.cs"),
            new(Architecture + "OrganisationSetupMembership.cs", "Services/Implementation/WriteBackCollector.cs", "Modules/Forecasting/Application/WriteBack/WriteBackCollector.cs"),
            new(Architecture + "PromisesUnitTestPlacementTest.cs", "Services/Implementation/WriteBackCollector.cs", "Modules/Forecasting/Application/WriteBack/WriteBackCollector.cs", MayLeave: true),
            new(Architecture + "PromisesUnitTestPlacementTest.cs", "Services/Implementation/WriteBackRound.cs", "Modules/Forecasting/Application/WriteBack/WriteBackRound.cs", MayLeave: true),
            new(Architecture + "PromisesUnitTestPlacementTest.cs", "Services/Implementation/WriteBackRoundContext.cs", "Modules/Forecasting/Application/WriteBack/WriteBackRoundContext.cs", MayLeave: true),
            new(Architecture + "PromisesUnitTestPlacementTest.cs", "Services/Implementation/WriteBackService.cs", "Modules/Forecasting/Application/WriteBack/WriteBackService.cs", MayLeave: true),
            new(Architecture + "PromisesUnitTestPlacementTest.cs", "Services/Implementation/WriteBackTriggerService.cs", "Modules/Forecasting/Application/WriteBack/WriteBackTriggerService.cs", MayLeave: true),
            new(Architecture + "PromisesUnitTestPlacementTest.cs", "Services/Interfaces/IWriteBackCollector.cs", "Modules/Forecasting/Application/WriteBack/IWriteBackCollector.cs", MayLeave: true),
            new(Architecture + "PromisesUnitTestPlacementTest.cs", "Services/Interfaces/IWriteBackService.cs", "Modules/Forecasting/Application/WriteBack/IWriteBackService.cs", MayLeave: true),
            new(Architecture + "PromisesUnitTestPlacementTest.cs", "Services/Interfaces/IWriteBackTriggerService.cs", "Modules/Forecasting/Application/WriteBack/IWriteBackTriggerService.cs", MayLeave: true),
            new(Architecture + "AccessModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.WriteBackCollector", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.WriteBackCollector", MayLeave: true),
            new(Architecture + "AccessModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.WriteBackRound", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.WriteBackRound", MayLeave: true),
            new(Architecture + "AccessModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.WriteBackRoundContext", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.WriteBackRoundContext", MayLeave: true),
            new(Architecture + "AccessModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.WriteBackService", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.WriteBackService", MayLeave: true),
            new(Architecture + "AccessModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.WriteBackTriggerService", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.WriteBackTriggerService", MayLeave: true),
            new(Architecture + "AccessModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Interfaces.IWriteBackCollector", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.IWriteBackCollector", MayLeave: true),
            new(Architecture + "AccessModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Interfaces.IWriteBackService", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.IWriteBackService", MayLeave: true),
            new(Architecture + "AccessModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Interfaces.IWriteBackTriggerService", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.IWriteBackTriggerService", MayLeave: true),
            new(Architecture + "LicensingModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.WriteBackTriggerService", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.WriteBackTriggerService", MayLeave: true),
            new(Architecture + "LicensingModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.WriteBackCollector", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.WriteBackCollector", MayLeave: true),
            new(Architecture + "LicensingModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.WriteBackRound", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.WriteBackRound", MayLeave: true),
            new(Architecture + "LicensingModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.WriteBackRoundContext", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.WriteBackRoundContext", MayLeave: true),
            new(Architecture + "LicensingModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.WriteBackService", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.WriteBackService", MayLeave: true),
            new(Architecture + "LicensingModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Interfaces.IWriteBackCollector", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.IWriteBackCollector", MayLeave: true),
            new(Architecture + "LicensingModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Interfaces.IWriteBackService", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.IWriteBackService", MayLeave: true),
            new(Architecture + "LicensingModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Interfaces.IWriteBackTriggerService", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.IWriteBackTriggerService", MayLeave: true),
            new(Architecture + "PlatformLaneUnitTestPlacementTest.cs", "Services/Implementation/WriteBackCollector.cs", "Modules/Forecasting/Application/WriteBack/WriteBackCollector.cs", MayLeave: true),
            new(Architecture + "PlatformLaneUnitTestPlacementTest.cs", "Services/Implementation/WriteBackRound.cs", "Modules/Forecasting/Application/WriteBack/WriteBackRound.cs", MayLeave: true),
            new(Architecture + "PlatformLaneUnitTestPlacementTest.cs", "Services/Implementation/WriteBackRoundContext.cs", "Modules/Forecasting/Application/WriteBack/WriteBackRoundContext.cs", MayLeave: true),
            new(Architecture + "PlatformLaneUnitTestPlacementTest.cs", "Services/Implementation/WriteBackService.cs", "Modules/Forecasting/Application/WriteBack/WriteBackService.cs", MayLeave: true),
            new(Architecture + "PlatformLaneUnitTestPlacementTest.cs", "Services/Implementation/WriteBackTriggerService.cs", "Modules/Forecasting/Application/WriteBack/WriteBackTriggerService.cs", MayLeave: true),
            new(Architecture + "PlatformLaneUnitTestPlacementTest.cs", "Services/Interfaces/IWriteBackCollector.cs", "Modules/Forecasting/Application/WriteBack/IWriteBackCollector.cs", MayLeave: true),
            new(Architecture + "PlatformLaneUnitTestPlacementTest.cs", "Services/Interfaces/IWriteBackService.cs", "Modules/Forecasting/Application/WriteBack/IWriteBackService.cs", MayLeave: true),
            new(Architecture + "PlatformLaneUnitTestPlacementTest.cs", "Services/Interfaces/IWriteBackTriggerService.cs", "Modules/Forecasting/Application/WriteBack/IWriteBackTriggerService.cs", MayLeave: true),
            new(Architecture + "PromisesModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.WriteBackCollector", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.WriteBackCollector", MayLeave: true),
            new(Architecture + "PromisesModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.WriteBackRound", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.WriteBackRound", MayLeave: true),
            new(Architecture + "PromisesModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.WriteBackRoundContext", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.WriteBackRoundContext", MayLeave: true),
            new(Architecture + "PromisesModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.WriteBackService", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.WriteBackService", MayLeave: true),
            new(Architecture + "PromisesModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.WriteBackTriggerService", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.WriteBackTriggerService", MayLeave: true),
            new(Architecture + "PromisesModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Interfaces.IWriteBackCollector", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.IWriteBackCollector", MayLeave: true),
            new(Architecture + "PromisesModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Interfaces.IWriteBackService", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.IWriteBackService", MayLeave: true),
            new(Architecture + "PromisesModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Interfaces.IWriteBackTriggerService", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.IWriteBackTriggerService", MayLeave: true),
            new(Architecture + "RepositoryLeakRatchetArchUnitTest.cs", "Services/Implementation/WriteBackTriggerService.cs", "Modules/Forecasting/Application/WriteBack/WriteBackTriggerService.cs"),
            new(Architecture + "PlatformLaneBoundariesArchUnitTest.cs", "Lighthouse.Backend.Platform.UpdateQueue.UpdateQueueService -> Lighthouse.Backend.Services.Implementation.WriteBackCollector", "Lighthouse.Backend.Platform.UpdateQueue.UpdateQueueService -> Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.WriteBackCollector", MayLeave: true),
            new(Architecture + "PlatformLaneBoundariesArchUnitTest.cs", "Lighthouse.Backend.Platform.UpdateQueue.UpdateQueueService -> Lighthouse.Backend.Services.Implementation.WriteBackRound", "Lighthouse.Backend.Platform.UpdateQueue.UpdateQueueService -> Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.WriteBackRound", MayLeave: true),
            new(Architecture + "PlatformLaneBoundariesArchUnitTest.cs", "Lighthouse.Backend.Platform.UpdateQueue.UpdateQueueService -> Lighthouse.Backend.Services.Implementation.WriteBackRoundContext", "Lighthouse.Backend.Platform.UpdateQueue.UpdateQueueService -> Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.WriteBackRoundContext", MayLeave: true),
            new(Architecture + "PlatformLaneBoundariesArchUnitTest.cs", "Lighthouse.Backend.Platform.UpdateQueue.UpdateQueueService -> Lighthouse.Backend.Services.Interfaces.IWriteBackService", "Lighthouse.Backend.Platform.UpdateQueue.UpdateQueueService -> Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.IWriteBackService", MayLeave: true),
            new(Architecture + "PlatformLaneBoundariesArchUnitTest.cs", "Lighthouse.Backend.Platform.UpdateQueue.UpdateQueueService+HeldUpdate -> Lighthouse.Backend.Services.Implementation.WriteBackRound", "Lighthouse.Backend.Platform.UpdateQueue.UpdateQueueService+HeldUpdate -> Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.WriteBackRound", MayLeave: true),
            new(Architecture + "TrackerIntegrationModuleBoundariesArchUnitTest.cs", "Lighthouse.Backend.Services.Implementation.WriteBackService", "Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.WriteBackService"),
        ];
    }
}
