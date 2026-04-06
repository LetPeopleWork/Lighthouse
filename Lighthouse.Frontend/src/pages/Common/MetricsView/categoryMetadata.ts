export type CategoryKey =
	| "flow-health"
	| "aging-stability"
	| "predictability"
	| "portfolio"
	| "overview";

export type CategoryDefinition = {
	readonly key: CategoryKey;
	readonly displayName: string;
};

export type WidgetPlacement = {
	readonly widgetKey: string;
	readonly size: "small" | "medium" | "large" | "xlarge";
	readonly ownerFilter?: "portfolio-only" | "team-only";
};

const categories: readonly CategoryDefinition[] = [
	{ key: "flow-health", displayName: "Flow Health" },
	{ key: "aging-stability", displayName: "Aging & Stability" },
	{ key: "predictability", displayName: "Predictability" },
	{ key: "portfolio", displayName: "Portfolio" },
	{ key: "overview", displayName: "Overview" },
];

const categoryWidgets: Record<CategoryKey, readonly WidgetPlacement[]> = {
	"flow-health": [
		{ widgetKey: "wipOverview", size: "small" },
		{ widgetKey: "startedVsFinished", size: "small" },
		{ widgetKey: "percentiles", size: "small" },
		{ widgetKey: "totalWorkItemAge", size: "small" },
		{ widgetKey: "throughput", size: "large" },
		{ widgetKey: "cycleScatter", size: "large" },
		{ widgetKey: "workDistribution", size: "large" },
		{ widgetKey: "wipOverTime", size: "large" },
	],
	"aging-stability": [
		{ widgetKey: "wipOverview", size: "small" },
		{ widgetKey: "totalWorkItemAge", size: "small" },
		{ widgetKey: "aging", size: "large" },
		{ widgetKey: "wipOverTime", size: "large" },
		{ widgetKey: "totalWorkItemAgeOverTime", size: "large" },
		{ widgetKey: "stacked", size: "large" },
	],
	predictability: [
		{ widgetKey: "predictabilityScore", size: "small" },
		{ widgetKey: "percentiles", size: "small" },
		{ widgetKey: "predictabilityScoreDetails", size: "large" },
		{ widgetKey: "throughputPbc", size: "large" },
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
	overview: [
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
};

const DEFAULT_CATEGORY: CategoryKey = "flow-health";

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
