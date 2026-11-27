import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import type { MetricsOwnerKey } from "../../../hooks/metricsOwnerKey";
import type { IFeature } from "../../../models/Feature";
import type {
	IWithinSleOverTime,
	WithinSleHorizon,
} from "../../../models/Metrics/WithinSleOverTime";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { formatLocalDate } from "../../../utils/date/localDate";
import { seriesStatusOf, type WidgetStatus } from "./widgetStatus";

export interface WithinSleOverTimeState {
	horizon: WithinSleHorizon;
	setHorizon: (horizon: WithinSleHorizon) => void;
	/** undefined while the selected horizon is still on its way: the previous horizon's answer is never shown as current. */
	answer: IWithinSleOverTime | undefined;
	status: WidgetStatus;
}

export function useWithinSleOverTime(
	owner: MetricsOwnerKey,
	metricsService: IMetricsService<IWorkItem | IFeature>,
	startDate: Date,
	endDate: Date,
): WithinSleOverTimeState {
	const [horizon, setHorizon] = useState<WithinSleHorizon>(30);
	const query = useQuery({
		queryKey: [
			"withinSleOverTime",
			owner.ownerId,
			owner.ownerUpdatedAt,
			horizon,
			formatLocalDate(startDate),
			formatLocalDate(endDate),
		],
		queryFn: () =>
			metricsService.getWithinSleOverTime(
				owner.ownerId,
				horizon,
				startDate,
				endDate,
			),
	});

	return {
		horizon,
		setHorizon,
		answer: query.data,
		status: seriesStatusOf(query.data?.days ?? null, query.isError),
	};
}
