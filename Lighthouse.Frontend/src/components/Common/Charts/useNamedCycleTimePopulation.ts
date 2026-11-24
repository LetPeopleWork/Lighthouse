import { QueryClientContext, skipToken, useQuery } from "@tanstack/react-query";
import { useContext, useMemo } from "react";
import type { WidgetStatus } from "../../../hooks/useReportWidgetStatus";
import type { INamedCycleTimeDefinition } from "../../../models/Metrics/NamedCycleTime";
import type { IWorkItem } from "../../../models/WorkItem";
import type { WorkItemsDialogOwnerKind } from "../WorkItemsDialog/workItemsDialogContexts";
import { fetcherIdOf, withoutAProvider } from "./chartQueryClient";
import {
	type CycleTimeMeasure,
	DefaultCycleTimeMeasure,
	NamedCycleTimeMeasure,
} from "./cycleTimeMeasure";

/** Asks the backend for the Work Items that reached the definition's end state in the date range. */
export type NamedDataFetcher = (definitionId: number) => Promise<IWorkItem[]>;

// A named measure plots its own Work Items, so offering one without a way to fetch them cannot compile.
export type NamedMeasureProps =
	| {
			namedCycleTimeDefinitions?: never;
			onFetchNamedCycleTimeData?: never;
	  }
	| {
			namedCycleTimeDefinitions: INamedCycleTimeDefinition[];
			onFetchNamedCycleTimeData: NamedDataFetcher;
	  };

/** The Work Items one named Cycle Time measured, tagged with that definition. */
interface NamedPopulation {
	readonly definitionId: number;
	readonly items: IWorkItem[];
}

export interface NamedPopulationState {
	/** The answer to show: the selected definition's, or while it loads the one shown before it. */
	readonly answer: NamedPopulation | null;
	readonly status: WidgetStatus;
}

/** The named measure the reader selected, with the fetcher that answers for its Work Items. */
export interface NamedSelection {
	readonly definitionId: number;
	readonly fetcher: NamedDataFetcher;
}

interface NamedPopulationQuestion {
	readonly ownerKind: WorkItemsDialogOwnerKind;
	readonly ownerId?: number;
	readonly startDate?: Date;
	readonly endDate?: Date;
	readonly selection: NamedSelection | null;
}

// Keyed by owner, range and definition, so an answer for a measure the reader has left lands in its
// own entry and never on the chart.
export function useNamedPopulation({
	ownerKind,
	ownerId,
	startDate,
	endDate,
	selection,
}: NamedPopulationQuestion): NamedPopulationState {
	const client = useContext(QueryClientContext) ?? withoutAProvider;
	const query = useQuery(
		{
			queryKey: [
				"namedCycleTimeData",
				ownerKind,
				ownerId ?? null,
				startDate?.toISOString() ?? null,
				endDate?.toISOString() ?? null,
				fetcherIdOf(selection?.fetcher),
				selection?.definitionId ?? null,
			],
			queryFn: selection
				? async (): Promise<NamedPopulation> => ({
						definitionId: selection.definitionId,
						items: await selection.fetcher(selection.definitionId),
					})
				: skipToken,
			placeholderData: (previous: NamedPopulation | undefined) => previous,
		},
		client,
	);

	const { data, isError, isPlaceholderData } = query;
	const isSelected = selection !== null;
	return useMemo((): NamedPopulationState => {
		if (!isSelected) return { answer: null, status: "ready" };
		if (isError) return { answer: null, status: "error" };
		if (data === undefined || isPlaceholderData) {
			return { answer: data ?? null, status: "loading" };
		}
		return { answer: data, status: "ready" };
	}, [isSelected, data, isError, isPlaceholderData]);
}

const DEFAULT_MEASURE = new DefaultCycleTimeMeasure();

interface ShownPopulation {
	readonly measure: CycleTimeMeasure;
	readonly items: readonly IWorkItem[];
}

/**
 * What the chart plots: the default Cycle Time's closed Work Items, or a named measure's own
 * population. While a named answer is on its way the previous dots stay, for the frame to dim; after
 * a failure none are shown as if they were the answer.
 */
export function shownPopulation(
	defaultItems: readonly IWorkItem[],
	definitions: readonly INamedCycleTimeDefinition[],
	named: NamedPopulationState,
): ShownPopulation {
	if (named.status === "error") return { measure: DEFAULT_MEASURE, items: [] };
	const answer = named.answer;
	const definition =
		answer &&
		definitions.find((candidate) => candidate.id === answer.definitionId);
	if (!answer || !definition) {
		return { measure: DEFAULT_MEASURE, items: defaultItems };
	}
	return {
		measure: new NamedCycleTimeMeasure(definition),
		items: answer.items,
	};
}
