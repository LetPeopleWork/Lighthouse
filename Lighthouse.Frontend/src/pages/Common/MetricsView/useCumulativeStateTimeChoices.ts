import { useQuery } from "@tanstack/react-query";
import { useCallback, useState } from "react";
import {
	type MetricsOwnerKey,
	metricsQueryOptions,
} from "../../../hooks/useMetricsData";
import type { IFeature } from "../../../models/Feature";
import type { ICumulativeStateTimeCandidateRow } from "../../../models/Metrics/CumulativeStateTimeCandidates";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { formatLocalDate } from "../../../utils/date/localDate";

type ScopeChoice = {
	readonly definitionId: number;
	readonly question: string;
};

/**
 * The stretch the reader narrowed Cumulative Time per State to, if any.
 *
 * A stretch is chosen for one window only and is dropped once the window changes, so it is never
 * asked for the next window, and coming back to the old window does not bring it back.
 */
export function useCumulativeScopeChoice(selectedQuestion: string): {
	readonly scopeDefinitionId: number | null;
	readonly chooseScope: (definitionId: number | null) => void;
} {
	const [scopeChoice, setScopeChoice] = useState<ScopeChoice | null>(null);
	if (scopeChoice !== null && scopeChoice.question !== selectedQuestion) {
		setScopeChoice(null);
	}

	const chooseScope = useCallback(
		(definitionId: number | null) =>
			setScopeChoice(
				definitionId === null
					? null
					: { definitionId, question: selectedQuestion },
			),
		[selectedQuestion],
	);

	return {
		scopeDefinitionId:
			scopeChoice?.question === selectedQuestion
				? scopeChoice.definitionId
				: null,
		chooseScope,
	};
}

type CandidatesRequest<T extends IWorkItem | IFeature> = {
	readonly metricsService: IMetricsService<T>;
	readonly ownerType: "team" | "portfolio";
	readonly owner: MetricsOwnerKey;
	readonly startDate: Date;
	readonly endDate: Date;
	readonly selectedQuestion: string;
};

/**
 * The Work Items the Cumulative Time per State picker offers.
 *
 * They are asked for only once the reader opens the picker, and again for each window it is opened
 * in. A failure leaves the list empty and the chart as it was, because the chart's numbers do not
 * depend on that list.
 */
export function useCumulativeStateTimeCandidates<
	T extends IWorkItem | IFeature,
>({
	metricsService,
	ownerType,
	owner,
	startDate,
	endDate,
	selectedQuestion,
}: CandidatesRequest<T>): {
	readonly candidates: ICumulativeStateTimeCandidateRow[];
	readonly candidatesLoaded: boolean;
	readonly onPickerOpen: () => void;
} {
	const [pickerOpenedFor, setPickerOpenedFor] = useState<string | null>(null);
	const candidatesQuery = useQuery({
		...metricsQueryOptions,
		queryKey: [
			"metrics",
			"cumulativeStateTimeCandidates",
			{
				ownerType,
				...owner,
				from: formatLocalDate(startDate),
				to: formatLocalDate(endDate),
			},
		] as const,
		queryFn: async (): Promise<ICumulativeStateTimeCandidateRow[]> => {
			const response =
				ownerType === "team"
					? await metricsService.getCumulativeStateTimeCandidatesForTeam(
							owner.ownerId,
							startDate,
							endDate,
						)
					: await metricsService.getCumulativeStateTimeCandidatesForPortfolio(
							owner.ownerId,
							startDate,
							endDate,
						);
			return response.items;
		},
		enabled: pickerOpenedFor === selectedQuestion,
	});

	const onPickerOpen = useCallback(
		() => setPickerOpenedFor(selectedQuestion),
		[selectedQuestion],
	);

	return {
		candidates: candidatesQuery.data ?? [],
		candidatesLoaded: candidatesQuery.isSuccess,
		onPickerOpen,
	};
}
