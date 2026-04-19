export type CategoryKey =
	| "flow-overview"
	| "flow-metrics"
	| "predictability"
	| "portfolio";

export type CategoryDefinition = {
	readonly key: CategoryKey;
	readonly displayName: string;
	readonly icon: string;
	readonly hoverText: string;
};

export type WidgetPlacement = {
	readonly widgetKey: string;
	readonly size: "small" | "medium" | "large" | "xlarge";
	readonly ownerFilter?: "portfolio-only" | "team-only";
};

export type TrendPolicy = "snapshot-compare" | "previous-period" | "none";

const categories: readonly CategoryDefinition[] = [
	{
		key: "flow-overview",
		displayName: "Flow Overview",
		icon: "Dashboard",
		hoverText: "How is my flow performing right now?",
	},
	{
		key: "flow-metrics",
		displayName: "Flow Metrics",
		icon: "ShowChart",
		hoverText: "Detailed flow metrics and trends over time",
	},
	{
		key: "predictability",
		displayName: "Predictability",
		icon: "Insights",
		hoverText: "How predictable is our delivery process?",
	},
	{
		key: "portfolio",
		displayName: "Portfolio & Features",
		icon: "AccountTree",
		hoverText: "How are features and portfolio items tracking?",
	},
];

const categoryWidgets: Record<CategoryKey, readonly WidgetPlacement[]> = {
	"flow-overview": [
		{ widgetKey: "wipOverview", size: "small" },
		{ widgetKey: "blockedOverview", size: "small" },
		{
			widgetKey: "featuresWorkedOnOverview",
			size: "small",
			ownerFilter: "team-only",
		},
		{ widgetKey: "totalWorkItemAge", size: "small" },
		{ widgetKey: "predictabilityScore", size: "small" },
		{ widgetKey: "percentiles", size: "small" },
		{ widgetKey: "totalThroughput", size: "small" },
		{ widgetKey: "totalArrivals", size: "small" },
		{
			widgetKey: "featureSizePercentiles",
			size: "small",
			ownerFilter: "portfolio-only",
		},
	],
	"flow-metrics": [
		{ widgetKey: "cycleScatter", size: "large" },
		{ widgetKey: "aging", size: "large" },
		{ widgetKey: "throughput", size: "large" },
		{ widgetKey: "stacked", size: "large" },
		{ widgetKey: "wipOverTime", size: "large" },
		{ widgetKey: "totalWorkItemAgeOverTime", size: "large" },
	],
	predictability: [
		{ widgetKey: "predictabilityScoreDetails", size: "large" },
		{ widgetKey: "arrivals", size: "large" },
		{ widgetKey: "throughputPbc", size: "large" },
		{ widgetKey: "arrivalsPbc", size: "large" },
		{ widgetKey: "wipPbc", size: "large" },
		{ widgetKey: "totalWorkItemAgePbc", size: "large" },
		{ widgetKey: "cycleTimePbc", size: "large" },
		{
			widgetKey: "featureSizePbc",
			size: "large",
			ownerFilter: "portfolio-only",
		},
	],
	portfolio: [
		{ widgetKey: "workDistribution", size: "large" },
		{ widgetKey: "featureSize", size: "large", ownerFilter: "portfolio-only" },
		{ widgetKey: "estimationVsCycleTime", size: "large" },
	],
};

const trendPolicies: Record<string, TrendPolicy> = {
	wipOverview: "snapshot-compare",
	featuresWorkedOnOverview: "snapshot-compare",
	totalWorkItemAge: "snapshot-compare",
	predictabilityScore: "previous-period",
	predictabilityScoreDetails: "none",
	totalThroughput: "previous-period",
	totalArrivals: "previous-period",
	percentiles: "previous-period",
	featureSizePercentiles: "previous-period",
	cycleScatter: "none",
	throughput: "none",
	arrivals: "none",
	wipOverTime: "none",
	totalWorkItemAgeOverTime: "none",
	workDistribution: "none",
	featureSize: "none",
	aging: "none",
	blockedOverview: "none",
	throughputPbc: "none",
	arrivalsPbc: "none",
	wipPbc: "none",
	totalWorkItemAgePbc: "none",
	cycleTimePbc: "none",
	featureSizePbc: "none",
	stacked: "none",
	estimationVsCycleTime: "none",
};

const DEFAULT_CATEGORY: CategoryKey = "flow-overview";

export function getCategories(): readonly CategoryDefinition[] {
	return categories;
}

export function getDefaultCategoryKey(): CategoryKey {
	return DEFAULT_CATEGORY;
}

export function getWidgetsForCategory(
	categoryKey: CategoryKey,
	ownerType: "team" | "portfolio",
): readonly WidgetPlacement[] {
	const widgets = categoryWidgets[categoryKey];
	return widgets.filter((w) => {
		if (w.ownerFilter === "portfolio-only" && ownerType !== "portfolio") {
			return false;
		}
		if (w.ownerFilter === "team-only" && ownerType !== "team") {
			return false;
		}
		return true;
	});
}

export function getTrendPolicy(widgetKey: string): TrendPolicy {
	return trendPolicies[widgetKey] ?? "none";
}
