import { useQuery } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { dialogRowsOf } from "../../../components/Common/WorkItemsDialog/workItemsDialogRows";
import {
	metricsOwnerKeyOf,
	metricsQueryOptions,
} from "../../../hooks/useMetricsData";
import type { IFeature } from "../../../models/Feature";
import type { IFeatureOwner } from "../../../models/IFeatureOwner";
import type { ICumulativeStateTimeItemRow } from "../../../models/Metrics/CumulativeStateTimeItems";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { formatLocalDate } from "../../../utils/date/localDate";

interface StateDrillDownInput<
	T extends IWorkItem | IFeature,
	E extends IFeatureOwner,
> {
	metricsService: IMetricsService<T>;
	ownerType: "team" | "portfolio";
	entity: E;
	startDate: Date;
	endDate: Date;
	selectedItemIds: number[];
}

export function useStateDrillDown<
	T extends IWorkItem | IFeature,
	E extends IFeatureOwner,
>({
	metricsService,
	ownerType,
	entity,
	startDate,
	endDate,
	selectedItemIds,
}: StateDrillDownInput<T, E>) {
	const [drillDownState, setDrillDownState] = useState<string | null>(null);
	const [drillDownOpen, setDrillDownOpen] = useState(false);

	const onBarClick = (stateName: string) => {
		setDrillDownState(stateName);
		setDrillDownOpen(true);
	};

	const onClose = () => setDrillDownOpen(false);

	const drillDownQuery = useQuery({
		...metricsQueryOptions,
		queryKey: [
			"metrics",
			"cumulativeStateTimeItems",
			{
				ownerType,
				...metricsOwnerKeyOf(entity),
				state: drillDownState,
				from: formatLocalDate(startDate),
				to: formatLocalDate(endDate),
				itemIds: selectedItemIds,
			},
		] as const,
		queryFn: async (): Promise<ICumulativeStateTimeItemRow[]> => {
			const stateName = drillDownState ?? "";
			const activeItemIds =
				selectedItemIds.length > 0 ? selectedItemIds : undefined;
			const response =
				ownerType === "team"
					? await metricsService.getCumulativeStateTimeItemsForTeam(
							entity.id,
							stateName,
							startDate,
							endDate,
							activeItemIds,
						)
					: await metricsService.getCumulativeStateTimeItemsForPortfolio(
							entity.id,
							stateName,
							startDate,
							endDate,
							activeItemIds,
						);
			return response.items;
		},
		enabled: drillDownOpen && drillDownState !== null,
	});

	const { status: drillDownStatus, items: drillDownItems } =
		dialogRowsOf(drillDownQuery);

	const drillDownWorkItems = useMemo<IWorkItem[]>(
		() =>
			drillDownItems.map((item) => ({
				id: item.workItemId,
				referenceId: item.referenceId,
				name: item.title,
				type: item.type,
				state: item.state,
				stateCategory: item.stateCategory,
				url: item.url,
				startedDate: new Date(0),
				closedDate: new Date(0),
				cycleTime: 0,
				workItemAge: 0,
				parentWorkItemReference: "",
				isBlocked: false,
			})),
		[drillDownItems],
	);

	const drillDownDaysColumn = useMemo(() => {
		const daysById = new Map(
			drillDownItems.map((item) => [
				item.workItemId,
				Math.round(item.daysContributed * 10) / 10,
			]),
		);
		return { daysFor: (workItem: IWorkItem) => daysById.get(workItem.id) ?? 0 };
	}, [drillDownItems]);

	return {
		drillDownState,
		drillDownOpen,
		onBarClick,
		onClose,
		drillDownStatus,
		drillDownWorkItems,
		drillDownDaysColumn,
	};
}
