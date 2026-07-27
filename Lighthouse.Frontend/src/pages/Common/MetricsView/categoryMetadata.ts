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
		{ widgetKey: "staleOverview", size: "small" },
		{
			widgetKey: "featuresWorkedOnOverview",
			size: "small",
			ownerFilter: "team-only",
		},
		{ widgetKey: "totalWorkItemAge", size: "small" },
		{ widgetKey: "flowEfficiency", size: "small" },
		{ widgetKey: "predictabilityScore", size: "small" },
		{ widgetKey: "percentiles", size: "small" },
		{ widgetKey: "workItemAgePercentiles", size: "small" },
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
		{ widgetKey: "wipOverTime", size: "large" },
		{ widgetKey: "totalWorkItemAgeOverTime", size: "large" },
		{ widgetKey: "arrivals", size: "large" },
		{ widgetKey: "stacked", size: "large" },
		{ widgetKey: "loadBalanceMatrix", size: "large" },
		{ widgetKey: "stateTimeCumulative", size: "large" },
		{ widgetKey: "blockedCountHistory", size: "large" },
	],
	predictability: [
		{ widgetKey: "predictabilityScoreDetails", size: "large" },
		{ widgetKey: "percentilesOverTime", size: "large" },
		{ widgetKey: "pbcOverTime", size: "large" },
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
	percentilesOverTime: "none",
	// Already a trend — a previous-period arrow on top of it would be nonsense.
	pbcOverTime: "none",
	totalThroughput: "previous-period",
	totalArrivals: "previous-period",
	percentiles: "previous-period",
	workItemAgePercentiles: "previous-period",
	featureSizePercentiles: "previous-period",
	cycleScatter: "none",
	throughput: "none",
	arrivals: "none",
	wipOverTime: "none",
	totalWorkItemAgeOverTime: "none",
	loadBalanceMatrix: "none",
	workDistribution: "none",
	featureSize: "none",
	aging: "none",
	blockedOverview: "previous-period",
	staleOverview: "none",
	flowEfficiency: "none",
	throughputPbc: "none",
	arrivalsPbc: "none",
	wipPbc: "none",
	totalWorkItemAgePbc: "none",
	cycleTimePbc: "none",
	featureSizePbc: "none",
	stacked: "none",
	estimationVsCycleTime: "none",
	stateTimeCumulative: "none",
	blockedCountHistory: "none",
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

/**
 * One key per gated fetch group in `useMetricsData`. Keys that name a group rather than a single
 * call — `featureSizeData` (size percentiles + all features), `pbcCore` (WIP + total-age PBC),
 * `pbcCharts` (throughput + cycle-time + arrivals PBC) — are the ones whose calls share both a
 * consumer set and a batch, so gating them apart would buy nothing.
 */
const metricsFetchKeys = [
	"blackoutPeriods",
	"predictability",
	"totalWorkItemAge",
	"throughput",
	"inProgressItems",
	"blockedItems",
	"wipOverTime",
	"cycleTimeData",
	"cycleTimePercentiles",
	"workItemAgePercentiles",
	"ageInStatePercentiles",
	"cumulativeStateTime",
	"flowEfficiency",
	"featureSizeData",
	"featureSizePbc",
	"featureSizeEstimation",
	"featureSizePercentilesInfo",
	"estimationVsCycleTime",
	"arrivals",
	"throughputInfo",
	"arrivalsInfo",
	"wipOverviewInfo",
	"totalWorkItemAgeInfo",
	"predictabilityScoreInfo",
	"cycleTimePercentilesInfo",
	"blockedCountHistory",
	"featuresWorkedOnInfo",
	"pbcCore",
	"pbcCharts",
] as const;

export type MetricsFetchKey = (typeof metricsFetchKeys)[number];

/**
 * Every process-behaviour-chart node is handed `workItemLookup` (BaseMetricsView.tsx:854), which
 * is built from throughput + WIP-over-time + cycle-time + in-progress items (:1545-1553), so a
 * PBC widget cannot name its drill-through points without them. Bug #5571 risk R3 was decided by
 * the maintainer on 2026-07-27 in favour of keeping the names: Predictability therefore saves
 * little, and the whole prize sits on the default Flow Overview view. Do not trim these.
 *
 * The lookup's fifth source, `featureSizeData`, rides on the portfolio-only `featureSizePbc`
 * entry — a team service never populates `allFeaturesForSizeChart` in the first place.
 */
const workItemLookupSources: readonly MetricsFetchKey[] = [
	"throughput",
	"wipOverTime",
	"cycleTimeData",
	"inProgressItems",
];

/**
 * What a widget needs to render *completely* — body AND RAG footer AND trend AND view-data.
 *
 * The footer/trend/view-data entries are the load-bearing ones: several Flow Overview chips are
 * computed in `BaseMetricsView` from data whose primary consumer sits in another category
 * (Bug #5571 §Q5). Omitting one blanks a chip or empties a drill-in table on the default view,
 * which is why `categoryMetadata.test.ts` asserts every widget in every category has an entry.
 *
 * An empty entry is legitimate only for a widget that owns its own fetch lifecycle
 * (`percentilesOverTime`, `pbcOverTime`); the test pins that exception list.
 */
const widgetFetchRequirements: Record<string, readonly MetricsFetchKey[]> = {
	// --- flow-overview ---------------------------------------------------------------------
	wipOverview: ["inProgressItems", "wipOverviewInfo"],
	// trend: computeBlockedTrend(blockedCountHistory) — BaseMetricsView.tsx:1828
	blockedOverview: ["blockedItems", "blockedCountHistory"],
	// staleItems is derived from inProgressItems — BaseMetricsView.tsx:1581
	staleOverview: ["inProgressItems"],
	// body + view-data come from the featuresInProgress prop, not from useMetricsData
	featuresWorkedOnOverview: ["featuresWorkedOnInfo"],
	// RAG reads totalWorkItemAge and currentWip — BaseMetricsView.tsx:365-374
	totalWorkItemAge: [
		"totalWorkItemAge",
		"inProgressItems",
		"totalWorkItemAgeInfo",
	],
	flowEfficiency: ["flowEfficiency"],
	predictabilityScore: ["predictability", "predictabilityScoreInfo"],
	// RAG needs the raw cycle times (ragRules.ts:174); ICycleTimePercentilesInfo carries none
	percentiles: [
		"cycleTimePercentiles",
		"cycleTimeData",
		"cycleTimePercentilesInfo",
	],
	// RAG reads agingItems, derived from inProgressItems — BaseMetricsView.tsx:1732
	workItemAgePercentiles: ["workItemAgePercentiles", "inProgressItems"],
	// RAG reads startedTotal/closedTotal — BaseMetricsView.tsx:446, sourced at :1708-1709
	totalThroughput: ["throughputInfo", "throughput", "arrivals"],
	totalArrivals: ["arrivalsInfo", "arrivals", "throughput"],
	// RAG needs sizePercentileValues + active feature sizes (ragRules.ts:619)
	featureSizePercentiles: ["featureSizePercentilesInfo", "featureSizeData"],

	// --- flow-metrics ----------------------------------------------------------------------
	cycleScatter: ["cycleTimeData", "cycleTimePercentiles", "blackoutPeriods"],
	aging: [
		"inProgressItems",
		"cycleTimePercentiles",
		"ageInStatePercentiles",
		"workItemAgePercentiles",
	],
	throughput: ["throughput"],
	wipOverTime: ["wipOverTime"],
	// RAG reads the total-age PBC's first and last points — BaseMetricsView.tsx:1746-1750
	totalWorkItemAgeOverTime: ["wipOverTime", "pbcCore"],
	arrivals: ["arrivals", "throughput"],
	stacked: ["throughput", "arrivals", "wipOverTime"],
	// deriveLoadBalanceMatrixData reads WIP + total age + both core PBCs — :1594-1610
	loadBalanceMatrix: ["inProgressItems", "totalWorkItemAge", "pbcCore"],
	stateTimeCumulative: ["cumulativeStateTime"],
	// footer is the max blocked age, read off the blocked items — BaseMetricsView.tsx:329
	blockedCountHistory: ["blockedCountHistory", "blockedItems"],

	// --- predictability --------------------------------------------------------------------
	predictabilityScoreDetails: ["predictability"],
	// Both fetch lazily inside themselves and read nothing from useMetricsData.
	percentilesOverTime: [],
	pbcOverTime: [],
	throughputPbc: ["pbcCharts", ...workItemLookupSources],
	arrivalsPbc: ["pbcCharts", "arrivals", ...workItemLookupSources],
	cycleTimePbc: ["pbcCharts", ...workItemLookupSources],
	wipPbc: ["pbcCore", ...workItemLookupSources],
	totalWorkItemAgePbc: ["pbcCore", ...workItemLookupSources],
	featureSizePbc: [
		"featureSizePbc",
		"featureSizeData",
		...workItemLookupSources,
	],

	// --- portfolio -------------------------------------------------------------------------
	workDistribution: ["cycleTimeData", "inProgressItems"],
	featureSize: ["featureSizeData", "featureSizeEstimation"],
	// view-data resolves dataPoints through workItemLookup — BaseMetricsView.tsx:574-579
	estimationVsCycleTime: ["estimationVsCycleTime", ...workItemLookupSources],
};

export function getMetricsFetchKeys(): readonly MetricsFetchKey[] {
	return metricsFetchKeys;
}

export function getFetchRequirementsForWidget(
	widgetKey: string,
): readonly MetricsFetchKey[] | undefined {
	return widgetFetchRequirements[widgetKey];
}

export function getFetchKeysForCategories(
	categoryKeys: readonly CategoryKey[],
	ownerType: "team" | "portfolio",
): ReadonlySet<MetricsFetchKey> {
	const fetchKeys = new Set<MetricsFetchKey>();
	for (const categoryKey of categoryKeys) {
		for (const widget of getWidgetsForCategory(categoryKey, ownerType)) {
			for (const fetchKey of widgetFetchRequirements[widget.widgetKey] ?? []) {
				fetchKeys.add(fetchKey);
			}
		}
	}
	return fetchKeys;
}
