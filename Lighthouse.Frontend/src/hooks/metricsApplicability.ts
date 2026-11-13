import type {
	IFeatureSizeMetricsService,
	IProjectMetricsService,
	ITeamMetricsService,
} from "../services/Api/MetricsService";
import type { MetricsFetchKey } from "./metricsFetchKeys";

// Feature sizes alone do not make a Portfolio: a Team has them too. The Feature Size process
// behaviour chart and the size estimate are configured on a Portfolio only, so a Team is never
// treated as one, even when its service happens to carry those methods.
function isProjectMetricsService(
	service: object,
): service is IProjectMetricsService {
	return (
		providesFeatureSize(service) &&
		"getFeatureSizePbc" in service &&
		"getFeatureSizeEstimation" in service &&
		!isTeamOwnedMetricsService(service)
	);
}

// Teams and Portfolios both have Feature sizes; a Team counts only its own Work Items in each.
function providesFeatureSize(
	service: object,
): service is IFeatureSizeMetricsService {
	return (
		"getAllFeaturesForSizeChart" in service &&
		"getSizePercentiles" in service &&
		"getFeatureSizePercentilesInfo" in service
	);
}

function isTeamMetricsService(service: object): service is ITeamMetricsService {
	return "getFeaturesWorkedOnInfo" in service;
}

// The risk is a team question — a feature can sit in several portfolios, each with its own target —
// so only the team service answers it, and its presence is what says which kind of service this is.
function providesSleRisk(service: object): service is ITeamMetricsService {
	return "getSleRisk" in service;
}

// Owner type is discriminated exactly as BaseMetricsView does it, on `getFeaturesInProgress`.
// Deliberately NOT isTeamMetricsService: that predicate keys off getFeaturesWorkedOnInfo, which
// portfolio-shaped services also expose, so it answers "does this service report features worked
// on", not "is this a team".
export function isTeamOwnedMetricsService(service: object): boolean {
	return "getFeaturesInProgress" in service;
}

// Whether this owner has the question at all. A key that does not apply is never asked, and is
// never waited for either. Every key is listed, so a new one does not build until someone says
// which owners can ask it.
export function applicabilityFor(
	service: object,
	choices: CumulativeStateTimeChoicesMade,
): Record<MetricsFetchKey, boolean> {
	const isPortfolioShaped = isProjectMetricsService(service);
	const hasFeatureSizes = providesFeatureSize(service);
	return {
		blackoutPeriods: true,
		predictability: true,
		totalWorkItemAge: true,
		throughput: true,
		inProgressItems: true,
		blockedItems: true,
		wipOverTime: true,
		cycleTimeData: true,
		cycleTimePercentiles: true,
		workItemAgePercentiles: true,
		ageInStatePercentiles: true,
		cumulativeStateTime: true,
		flowEfficiency: true,
		estimationVsCycleTime: true,
		arrivals: true,
		throughputInfo: true,
		arrivalsInfo: true,
		wipOverviewInfo: true,
		totalWorkItemAgeInfo: true,
		predictabilityScoreInfo: true,
		cycleTimePercentilesInfo: true,
		blockedCountHistory: true,
		pbcCore: true,
		pbcCharts: true,
		throughputPbc: true,
		sleRisk: providesSleRisk(service),
		featureSizeData: hasFeatureSizes,
		featureSizePbc: isPortfolioShaped,
		featureSizeEstimation: isPortfolioShaped,
		featureSizePercentilesInfo: hasFeatureSizes,
		featuresWorkedOnInfo: isTeamMetricsService(service),
		cumulativeStateTimeSelection: choices.selection,
		cumulativeStateTimeScope: choices.scope,
	};
}

// Narrowing Cumulative Time per State counts again a chart its totals have already drawn, so only
// the totals say whether there is a chart on screen. Its controls then stay put while the narrowed
// count is on its way.
export const narrowsAChartAlreadyDrawn: ReadonlySet<string> =
	new Set<MetricsFetchKey>([
		"cumulativeStateTimeSelection",
		"cumulativeStateTimeScope",
	]);

export type CumulativeStateTimeChoicesMade = {
	readonly selection: boolean;
	readonly scope: boolean;
};
