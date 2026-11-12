import type { WorkItemsDialogOwnerKind } from "../../../components/Common/WorkItemsDialog/workItemsDialogContexts";
import type { IFeature } from "../../../models/Feature";
import type { RunChartData } from "../../../models/Metrics/RunChartData";
import type { IWorkItem } from "../../../models/WorkItem";

export function extractWorkItems(
	workItemsPerUnitOfTime?: Record<string, IWorkItem[]>,
): IWorkItem[] {
	if (!workItemsPerUnitOfTime) return [];
	return Object.values(workItemsPerUnitOfTime).flat();
}

function addItemsWithoutDuplicates(
	lookup: Map<number, IWorkItem>,
	items: IWorkItem[],
): void {
	for (const item of items) {
		if (!lookup.has(item.id)) {
			lookup.set(item.id, item);
		}
	}
}

export function buildWorkItemLookup(
	throughputData: RunChartData | null,
	wipOverTimeData: RunChartData | null,
	cycleTimeData: IWorkItem[],
	inProgressItems: IWorkItem[],
	allFeaturesForSizeChart: IFeature[],
	ownerKind: WorkItemsDialogOwnerKind,
): Map<number, IWorkItem> {
	const lookup = new Map<number, IWorkItem>();

	// Several sources can describe the same work item, and they do not all carry the same fields.
	// The cycle time and in-progress endpoints return the richest record, so whoever comes first
	// here wins and the run charts only fill in items no other source mentions.
	const sourcesRichestFirst = [
		cycleTimeData,
		inProgressItems,
		extractWorkItems(throughputData?.workItemsPerUnitOfTime),
		extractWorkItems(wipOverTimeData?.workItemsPerUnitOfTime),
	];

	for (const source of sourcesRichestFirst) {
		addItemsWithoutDuplicates(lookup, source);
	}

	// A Team's Work Items and its Features are numbered in separate tables, so on a Team a Feature
	// would overwrite the Work Item that happens to share its id. Only a Portfolio, whose every
	// source is a Feature, lets its Features in.
	if (ownerKind === "team") {
		return lookup;
	}

	for (const item of allFeaturesForSizeChart) {
		lookup.set(item.id, item as unknown as IWorkItem);
	}

	return lookup;
}
