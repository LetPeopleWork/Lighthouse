import type {
	WorkItemsDialogContextId,
	WorkItemsDialogOwnerKind,
} from "../../../components/Common/WorkItemsDialog/workItemsDialogContexts";
import type { IFeature } from "../../../models/Feature";
import type { IEstimationVsCycleTimeResponse } from "../../../models/Metrics/EstimationVsCycleTimeData";
import type { INamedCycleTimeDefinition } from "../../../models/Metrics/NamedCycleTime";
import type { RunChartData } from "../../../models/Metrics/RunChartData";
import type { ISleRisk } from "../../../models/Metrics/SleRisk";
import type { IPercentileValue } from "../../../models/PercentileValue";
import type { IPerStatePercentileValues } from "../../../models/PerStatePercentileValues";
import type { IWorkItem } from "../../../models/WorkItem";
import {
	ageBandColumnDescription,
	ageBandColumnHeaderName,
	buildAgeBandColumnDescriptor,
} from "../../../utils/charts/paceBands";
import {
	buildSleRiskColumnDescriptor,
	sleRiskColumnDescription,
	sleRiskColumnHeaderName,
} from "../../../utils/charts/sleRisk";
import type { ViewDataPayload } from "./WidgetShell";
import { extractWorkItems } from "./workItemLookup";

type ViewDataInputs = {
	readonly title: string;
	/** The last day of the range, the day the lists of what was in progress over it measure age on. */
	readonly endDate: Date;
	readonly ownerKind: WorkItemsDialogOwnerKind;
	readonly inProgressItems: IWorkItem[];
	readonly blockedItems: IWorkItem[];
	readonly staleItems: IWorkItem[];
	readonly featuresInProgress: IWorkItem[] | undefined;
	readonly cycleTimeData: IWorkItem[];
	readonly throughputData: RunChartData | null;
	readonly wipOverTimeData: RunChartData | null;
	readonly allFeaturesForSizeChart: IFeature[];
	readonly serviceLevelExpectation: IPercentileValue | null;
	/**
	 * The team's target in days, taken from the team rather than from the percentile above. That one
	 * is withheld unless a probability is set too, and the risk column does not need a probability -
	 * the endpoint answers on the range alone, so reading the range off it would blank the column
	 * for a team that has one.
	 */
	readonly sleRangeInDays: number | undefined;
	readonly percentilesScopeDefinitionId: number | null;
	readonly namedCycleTimeDefinitions: INamedCycleTimeDefinition[];
	readonly estimationVsCycleTimeData: IEstimationVsCycleTimeResponse | null;
	readonly arrivalsData: RunChartData | null;
	readonly workItemLookup: Map<number, IWorkItem>;
	readonly perStatePercentileValues: IPerStatePercentileValues[];
	readonly sleRiskValues: ISleRisk[];
	readonly doingStates: string[];
	readonly terms: {
		workItem: string;
		workItems: string;
		features: string;
		cycleTime: string;
		workItemAge: string;
		blocked: string;
		sle: string;
	};
};

export function buildViewData(
	inputs: ViewDataInputs,
): Record<string, ViewDataPayload | undefined> {
	const { terms } = inputs;

	const ageBandColumn = buildAgeBandColumnDescriptor({
		perStatePercentileValues: inputs.perStatePercentileValues,
		doingStates: inputs.doingStates,
		headerName: ageBandColumnHeaderName(terms.workItemAge),
		description: ageBandColumnDescription(terms.workItems),
	});
	const sleRiskColumn = buildSleRiskColumnDescriptor({
		answers: inputs.sleRiskValues,
		headerName: sleRiskColumnHeaderName(terms.sle),
		description: sleRiskColumnDescription(terms.workItem, terms.sle),
		workItemTerm: terms.workItem,
		workItemsTerm: terms.workItems,
		sleTerm: terms.sle,
		sleRangeInDays: inputs.sleRangeInDays,
	});
	const throughputItems = extractWorkItems(
		inputs.throughputData?.workItemsPerUnitOfTime,
	);
	const wipOverTimeItems = extractWorkItems(
		inputs.wipOverTimeData?.workItemsPerUnitOfTime,
	);
	const arrivalsItems = extractWorkItems(
		inputs.arrivalsData?.workItemsPerUnitOfTime,
	);

	const startedVsFinishedItems = (() => {
		const items: IWorkItem[] = [];
		if (inputs.arrivalsData) {
			const startedWorkItems = extractWorkItems(
				inputs.arrivalsData.workItemsPerUnitOfTime,
			);
			const notClosedStartedItems = startedWorkItems.filter(
				(item) => item.closedDate === null,
			);
			items.push(...notClosedStartedItems);
		}
		if (inputs.throughputData) {
			items.push(...throughputItems);
		}
		return items;
	})();

	const workDistributionItems = [
		...inputs.cycleTimeData,
		...inputs.inProgressItems,
	];

	const estimationItems =
		inputs.estimationVsCycleTimeData?.dataPoints
			?.flatMap((dp) =>
				dp.workItemIds.map((id) => inputs.workItemLookup.get(id)),
			)
			.filter((item): item is IWorkItem => item !== undefined) ?? [];

	const namedPercentilesScope = (() => {
		const id = inputs.percentilesScopeDefinitionId;
		if (id === null) {
			return null;
		}
		const definition = inputs.namedCycleTimeDefinitions.find(
			(candidate) => candidate.id === id,
		);
		if (definition === undefined) {
			return null;
		}
		return {
			id,
			valueFor: (item: IWorkItem) =>
				item.namedCycleTimes?.find((value) => value.definitionId === id)?.days,
		};
	})();

	const opens = (context: WorkItemsDialogContextId) => ({
		context,
		ownerKind: inputs.ownerKind,
	});
	const closedItems = opens("closedItems");
	const inProgress = opens("inProgress");
	const arrivals = opens("arrivals");
	// These lists hold everything in progress on any day of the range, so their ages are read on its
	// last day, the same day the rest of the view describes; an item finished by then shows none.
	const inProgressOverTheRange = { ...inProgress, ageOn: inputs.endDate };

	// Every list of what the team has in flight today, and nothing else, carries the risk column —
	// the number answers "will this item breach", which only means something for an item still
	// running. Sharing one base is what stops the next such list from being added without it, which
	// is how two of these four went a release without one.
	const inFlight = {
		title: `${inputs.title} in Progress`,
		items: inputs.inProgressItems,
		...inProgress,
		sleRiskColumn,
	};

	const timeInStateColumn = { now: inputs.endDate };

	return {
		wipOverview: {
			...inFlight,
			timeInStateColumn,
		},
		blockedOverview: {
			title: `${terms.blocked} ${terms.workItems}`,
			items: inputs.blockedItems,
			...opens("blocked"),
		},
		staleOverview: {
			title: `Stale ${terms.workItems}`,
			items: inputs.staleItems,
			...opens("stale"),
			timeInStateColumn,
		},
		featuresWorkedOnOverview: inputs.featuresInProgress
			? {
					title: `${terms.features} being Worked On`,
					items: inputs.featuresInProgress,
					...inProgress,
				}
			: undefined,
		percentiles: namedPercentilesScope
			? {
					title: `Closed ${terms.workItems}`,
					items: inputs.cycleTimeData.filter(
						(item) => namedPercentilesScope.valueFor(item) !== undefined,
					),
					...closedItems,
					cycleTimeScope: namedPercentilesScope.id,
					namedCycleTimeDefinitions: inputs.namedCycleTimeDefinitions,
				}
			: {
					title: `Closed ${terms.workItems}`,
					items: inputs.cycleTimeData,
					...closedItems,
					sle: inputs.serviceLevelExpectation?.value,
				},
		totalWorkItemAge: inFlight,
		workItemAgePercentiles: inFlight,
		// Every item in flight with its risk, not only the ones at risk — a coach opening this is
		// asking what is behind the number, and the items just under the line are the ones worth
		// seeing before they cross it.
		sleRisk: inFlight,
		throughput: {
			title: `${inputs.title} Completed`,
			items: throughputItems,
			...closedItems,
		},
		cycleScatter: {
			title: `Closed ${terms.workItems}`,
			items: inputs.cycleTimeData,
			...closedItems,
			sle: inputs.serviceLevelExpectation?.value,
		},
		workDistribution: {
			title: `${terms.workItems} Distribution`,
			items: workDistributionItems,
			...opens("workDistribution"),
		},
		aging: {
			...inFlight,
			...opens("aging"),
			title: `${terms.workItems} in Progress`,
			ageBandColumn,
		},
		wipOverTime: {
			title: `${inputs.title} In Progress`,
			items: wipOverTimeItems,
			...inProgressOverTheRange,
		},
		totalWorkItemAgeOverTime: {
			title: `${inputs.title} Contributing to Total Age`,
			items: wipOverTimeItems,
			...inProgressOverTheRange,
		},
		stacked: {
			title: `Started and Closed ${terms.workItems}`,
			items: startedVsFinishedItems,
			...opens("startedAndClosed"),
		},
		estimationVsCycleTime: {
			title: `${terms.workItems} with Estimates`,
			items: estimationItems,
			...opens("estimation"),
		},
		featureSize: {
			title: `${terms.features}`,
			items: inputs.allFeaturesForSizeChart as unknown as IWorkItem[],
			...opens("featureSize"),
		},
		featureSizePercentiles: {
			title: `Closed ${terms.features}`,
			// A Feature sized by the default is left out of the percentiles, so the table leaves it out too.
			items: inputs.allFeaturesForSizeChart.filter(
				(feature) => feature.stateCategory === "Done" && feature.size > 0,
			) as unknown as IWorkItem[],
			...opens("featureSize"),
		},
		throughputPbc: {
			title: `${inputs.title} Completed`,
			items: throughputItems,
			...closedItems,
		},
		wipPbc: {
			title: `${inputs.title} In Progress`,
			items: wipOverTimeItems,
			...inProgressOverTheRange,
		},
		totalWorkItemAgePbc: {
			title: `${inputs.title} Contributing to Total Age`,
			items: wipOverTimeItems,
			...inProgressOverTheRange,
		},
		cycleTimePbc: {
			title: `Closed ${terms.workItems}`,
			items: inputs.cycleTimeData,
			...closedItems,
			sle: inputs.serviceLevelExpectation?.value,
		},
		featureSizePbc: {
			title: `${terms.features}`,
			items: inputs.allFeaturesForSizeChart as unknown as IWorkItem[],
			...opens("featureSize"),
		},
		arrivals: {
			title: `${inputs.title} Started`,
			items: arrivalsItems,
			...arrivals,
		},
		arrivalsPbc: {
			title: `${inputs.title} Started`,
			items: arrivalsItems,
			...arrivals,
		},
		totalThroughput: {
			title: `${inputs.title} Completed`,
			items: throughputItems,
			...closedItems,
		},
		totalArrivals: {
			title: `${inputs.title} Started`,
			items: arrivalsItems,
			...arrivals,
		},
	};
}
