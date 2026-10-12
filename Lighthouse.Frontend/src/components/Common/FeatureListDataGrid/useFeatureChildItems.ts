import { useQuery } from "@tanstack/react-query";
import { useCallback, useContext, useState } from "react";
import type { IFeature } from "../../../models/Feature";
import type { IWorkItem } from "../../../models/WorkItem";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import type { WorkItemsDialogStatus } from "../WorkItemsDialog/WorkItemsDialog";
import { dialogRowsOf } from "../WorkItemsDialog/workItemsDialogRows";

// Nothing is kept between opens: every open asks again, so a reopen after a failure is a retry.
const childItemsQueryOptions = {
	staleTime: 0,
	gcTime: 0,
	retry: false,
	refetchOnWindowFocus: false,
	refetchOnReconnect: false,
} as const;

export interface FeatureChildItems {
	readonly selectedFeature: IFeature | null;
	readonly items: IWorkItem[];
	readonly status: WorkItemsDialogStatus;
	readonly show: (feature: IFeature) => void;
	readonly close: () => void;
}

export function useFeatureChildItems(): FeatureChildItems {
	const { featureService } = useContext(ApiServiceContext);
	const [selectedFeature, setSelectedFeature] = useState<IFeature | null>(null);
	const featureId = selectedFeature?.id ?? null;

	const query = useQuery({
		...childItemsQueryOptions,
		queryKey: ["features", "childItems", featureId] as const,
		queryFn: () => featureService.getFeatureWorkItems(featureId ?? 0),
		enabled: featureId !== null,
	});

	const { status, items } = dialogRowsOf(query);
	const close = useCallback(() => setSelectedFeature(null), []);

	return {
		selectedFeature,
		items,
		status,
		show: setSelectedFeature,
		close,
	};
}
