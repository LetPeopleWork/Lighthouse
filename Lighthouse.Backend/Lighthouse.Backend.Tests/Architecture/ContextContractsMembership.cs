namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>Why a type belongs to what its context publishes.</summary>
    internal enum ContractAdmission
    {
        /// <summary>Another context reaches it, and it is a shape rather than an algorithm: an interface, an enum, a record or struct holding no service, a class of constants, or an event the context publishes.</summary>
        Seed,

        /// <summary>A published type names it in its public signature, so it is published with it.</summary>
        Closure,
    }

    /// <summary>
    /// What each packaged context publishes for the other contexts, measured from the compiled code before anything moves:
    /// one entry per type, with its context, the namespace it lives in today, the Contracts namespace of its context it moves
    /// into, and why it is published. Names are strings, so the map holds before, during and after the moves; the compiled code
    /// decides whether it is right. Types another context reaches that are algorithms, not shapes, stay where they are and are
    /// listed apart with the reason.
    /// </summary>
    internal static class ContextContractsMembership
    {
        // A property, not a constant: the move laws of earlier moves read every constant string of the test assembly as a rule
        // pattern, and this one names the modules root without being a rule.
        public static string TheModulesRoot => string.Join('.', "Lighthouse.Backend", "Modules");

        public static IReadOnlyList<string> TheContexts { get; } =
        [
            "Access",
            "FlowMetrics",
            "Forecasting",
            "Licensing",
            "OrganisationSetup",
            "Promises",
            "Refinement",
            "Reports",
            "TrackerIntegration",
            "WorkItems",
        ];

        public static IReadOnlyList<(MovingType Type, ContractAdmission Admission)> Entries { get; } =
        [
            Seed("IAuthModeResolver", "Access", "Application"),
            Seed("IRbacAdministrationService", "Access", "Application"),
            Seed("ForecastThroughputStatus", "FlowMetrics", "Application"),
            Seed("IForecastDataSufficiency", "FlowMetrics", "Application"),
            Seed("IHowManyForecaster", "FlowMetrics", "Application"),
            Seed("IPortfolioMetricsService", "FlowMetrics", "Application"),
            Seed("ITeamMetricsService", "FlowMetrics", "Application"),
            Seed("IThroughputFilter", "FlowMetrics", "Application"),
            Closure("CycleTimeFeature", "FlowMetrics", "Application"),
            Closure("CycleTimeWorkItem", "FlowMetrics", "Application"),
            Closure("OutlierVerdict", "FlowMetrics", "Application"),
            Closure("UpperNaturalProcessLimit", "FlowMetrics", "Domain"),
            Seed("IForecastFilterRuleService", "Forecasting", "Application"),
            Seed("IForecastService", "Forecasting", "Application"),
            Closure("ForecastRunOutcome", "Forecasting", "Domain"),
            Seed("ILicenseService", "Licensing", "Application"),
            Seed("IAppSettingService", "OrganisationSetup", "Application"),
            Seed("IBlackoutPeriodService", "OrganisationSetup", "Application"),
            Seed("IFeatureOrdering", "OrganisationSetup", "Application"),
            Seed("IDeliveryForecastPublisher", "TrackerIntegration", "Application"),
            Seed("IDeliverySourceProvider", "TrackerIntegration", "Application"),
            Seed("IDescriptionBlockMerger", "TrackerIntegration", "Application"),
            Seed("IFeatureSource", "TrackerIntegration", "Application"),
            Seed("IIncrementalSource", "TrackerIntegration", "Application"),
            Seed("IWorkTrackingConnector", "TrackerIntegration", "Application"),
            Seed("IWorkTrackingConnectorFactory", "TrackerIntegration", "Application"),
            Seed("IWriteBackTarget", "TrackerIntegration", "Application"),
            Seed("IBlockedItemService", "WorkItems", "Application"),
            Seed("IRefreshLogService", "WorkItems", "Application"),
            Seed("IRuleEvaluator`1", "WorkItems", "Application"),
            Seed("IRuleFieldProvider`1", "WorkItems", "Application"),
            Seed("IWhatTheForecastWaitsFor", "WorkItems", "Application.Dependencies"),
            Event("FeatureBlocked", "WorkItems"),
            Event("FeatureUnblocked", "WorkItems"),
            Event("PortfolioFeaturesRefreshed", "WorkItems"),
            Event("TeamDataRefreshed", "WorkItems"),
            Event("WorkItemBecameStale", "WorkItems"),
            Event("WorkItemBlocked", "WorkItems"),
            Event("WorkItemTransitioned", "WorkItems"),
            Event("WorkItemUnblocked", "WorkItems"),
            Event("PortfolioForecastsUpdated", "Forecasting"),
            Event("BlackoutConfigurationChanged", "OrganisationSetup"),
            Event("ConnectionFetchShapeChanged", "OrganisationSetup"),
            Event("FeatureOrderingPolicyChanged", "OrganisationSetup"),
            Event("FeatureRankChanged", "OrganisationSetup"),
            Event("PortfolioDeleted", "OrganisationSetup"),
            Event("TeamDeleted", "OrganisationSetup"),
        ];

        /// <summary>Types another context reaches that hold behaviour, so they are not published; each such edge is listed by the contracts rule with its owner.</summary>
        public static IReadOnlyList<string> ReachedButNotPublished { get; } =
        [
            string.Join('.', TheModulesRoot, "Access.Application.CurrentUserProfileService"),
            string.Join('.', TheModulesRoot, "FlowMetrics.Domain.PercentileCalculator"),
            string.Join('.', TheModulesRoot, "OrganisationSetup.Domain.WeeklyRecurrence"),
            string.Join('.', TheModulesRoot, "TrackerIntegration.Domain.ParentSourceSelector"),
            string.Join('.', TheModulesRoot, "WorkItems.Application.FeatureFieldProvider"),
            string.Join('.', TheModulesRoot, "WorkItems.Application.RuleEvaluator`1"),
            string.Join('.', TheModulesRoot, "WorkItems.Domain.FetchFingerprint"),
        ];

        public static string ContractsOf(string context) => string.Join('.', TheModulesRoot, context, "Contracts");

        private static (MovingType Type, ContractAdmission Admission) Seed(string name, string context, string layer)
            => (new MovingType(name, context, string.Join('.', TheModulesRoot, context, layer), ContractsOf(context)), ContractAdmission.Seed);

        private static (MovingType Type, ContractAdmission Admission) Closure(string name, string context, string layer)
            => (new MovingType(name, context, string.Join('.', TheModulesRoot, context, layer), ContractsOf(context)), ContractAdmission.Closure);

        // Each event moves into the Contracts of the context that publishes it, even where today's publisher is code that
        // belongs to no context yet.
        private static (MovingType Type, ContractAdmission Admission) Event(string name, string publisher)
            => (new MovingType(name, publisher, string.Join('.', "Lighthouse.Backend.Models", "Events"), ContractsOf(publisher)), ContractAdmission.Seed);
    }
}
