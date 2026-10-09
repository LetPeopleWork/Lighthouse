import { useQuery } from "@tanstack/react-query";
import {
	type MetricsOwnerRequest,
	metricsQueryOptions,
} from "../../../hooks/useMetricsData";
import { formatLocalDate } from "../../../utils/date/localDate";
import { fetchKeyStateOf, type WidgetStatus } from "./widgetStatus";

type FilteredViewRequest<V> = {
	readonly fetchName: string;
	readonly owner: MetricsOwnerRequest | undefined;
	readonly startDate: Date | undefined;
	readonly endDate: Date | undefined;
	/** Whether the reader has the chart's filter switched on. */
	readonly filtered: boolean;
	readonly canAsk?: boolean;
	readonly ask: () => Promise<V>;
	readonly failureMessage: string;
};

/** The filtered answer behind a chart's filter switch, and where its request stands. */
export function useFilteredView<V>({
	fetchName,
	owner,
	startDate,
	endDate,
	filtered,
	canAsk = true,
	ask,
	failureMessage,
}: FilteredViewRequest<V>): {
	readonly answer: V | undefined;
	readonly status: WidgetStatus;
} {
	const query = useQuery({
		...metricsQueryOptions,
		queryKey: [
			"metrics",
			fetchName,
			{
				view: "filtered",
				...owner,
				from: startDate ? formatLocalDate(startDate) : null,
				to: endDate ? formatLocalDate(endDate) : null,
			},
		] as const,
		queryFn: async () => {
			try {
				return await ask();
			} catch (error) {
				console.error(failureMessage, error);
				throw error;
			}
		},
		// Turning the filter off and on again shows the answer this window already has, rather than
		// asking again; a window the filter has never been on for is never asked.
		enabled: (current) =>
			canAsk && (filtered || current.state.data !== undefined),
	});

	// With the filter off the chart shows the page's own answer, so a refetch of the filtered answer
	// it keeps for later holds nothing back.
	return {
		answer: query.data,
		status: fetchKeyStateOf([query], filtered).status,
	};
}
