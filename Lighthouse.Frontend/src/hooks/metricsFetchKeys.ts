/**
 * One key per gated fetch group in `useMetricsData`. Keys that name a group rather than a single
 * call — `featureSizeData` (size percentiles + all features), `pbcCore` (WIP + total-age PBC),
 * `pbcCharts` (cycle-time + arrivals PBC) — are the ones whose calls share both a consumer set and
 * a window, so gating them apart would buy nothing. The Throughput PBC has a key of its own because
 * its filter switch asks for it again on its own.
 */
export const metricsFetchKeys = [
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
	"sleRisk",
	"cumulativeStateTime",
	"cumulativeStateTimeSelection",
	"cumulativeStateTimeScope",
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
	"throughputPbc",
] as const;

export type MetricsFetchKey = (typeof metricsFetchKeys)[number];
