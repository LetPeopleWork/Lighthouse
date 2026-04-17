export type CategoryKey =
	| "flow-overview"
	| "cycle-time"
	| "throughput"
	| "wip-aging"
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

const categories: readonly CategoryDefinition[] = [
	{
		key: "flow-overview",
		displayName: "Flow Overview",
		icon: "Dashboard",
		hoverText: "How is my flow performing right now?",
	},
	{
		key: "cycle-time",
		displayName: "Cycle Time",
		icon: "Timer",
		hoverText: "How long do items take to complete?",
	},
	{
		key: "throughput",
		displayName: "Throughput",
		icon: "ShowChart",
		hoverText: "How many items are we completing over time?",
	},
	{
		key: "wip-aging",
		displayName: "WIP & Aging",
		icon: "HourglassEmpty",
		hoverText: "How much work is in progress and how old is it?",
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
		{ widgetKey: "startedVsFinished", size: "small" },
		{ widgetKey: "percentiles", size: "small" },
	],
	"cycle-time": [
		{ widgetKey: "percentiles", size: "small" },
		{ widgetKey: "totalWorkItemAge", size: "small" },
		{ widgetKey: "cycleScatter", size: "large" },
		{ widgetKey: "aging", size: "large" },
		{ widgetKey: "cycleTimePbc", size: "large" },
	],
	throughput: [
		{ widgetKey: "startedVsFinished", size: "small" },
		{ widgetKey: "predictabilityScore", size: "small" },
		{ widgetKey: "throughput", size: "large" },
		{ widgetKey: "stacked", size: "large" },
		{ widgetKey: "throughputPbc", size: "large" },
	],
	"wip-aging": [
		{ widgetKey: "wipOverview", size: "small" },
		{ widgetKey: "totalWorkItemAge", size: "small" },
		{ widgetKey: "aging", size: "large" },
		{ widgetKey: "wipOverTime", size: "large" },
		{ widgetKey: "totalWorkItemAgeOverTime", size: "large" },
		{ widgetKey: "wipPbc", size: "large" },
		{ widgetKey: "totalWorkItemAgePbc", size: "large" },
	],
	predictability: [
		{ widgetKey: "cycleScatter", size: "large" },
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
		{
			widgetKey: "featuresWorkedOnOverview",
			size: "small",
			ownerFilter: "team-only",
		},
		{ widgetKey: "predictabilityScore", size: "small" },
		{ widgetKey: "workDistribution", size: "large" },
		{ widgetKey: "featureSize", size: "large", ownerFilter: "portfolio-only" },
		{ widgetKey: "estimationVsCycleTime", size: "large" },
	],
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
