import { useQueries } from "@tanstack/react-query";
import { useContext } from "react";
import type {
	IDeliverySource,
	IDeliverySourceOption,
} from "../../../../../models/Delivery/DeliverySource";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";
import type { WidgetStatus } from "../../../../Common/MetricsView/widgetStatus";
import { askedAfreshQueryOptions } from "./askedAfreshQueryOptions";
import type { PremiumVerdict } from "./premiumVerdict";

export interface DeliverySourceOptionsRequest {
	portfolioId: number;
	isOpen: boolean;
	/** Counts the openings of the dialog, so each one asks afresh and a late answer meets nobody. */
	opening: number;
	offeredSources: IDeliverySource[] | undefined;
	verdict: PremiumVerdict;
}

export interface DeliverySourceOptionsState {
	status: WidgetStatus;
	options: IDeliverySourceOption[] | undefined;
}

const STILL_LOADING: DeliverySourceOptionsState = {
	status: "loading",
	options: undefined,
};

const COULD_NOT_LOAD: DeliverySourceOptionsState = {
	status: "error",
	options: undefined,
};

/** The entries every offered source holds, asked for once per opening of the dialog. */
export function useDeliverySourceOptions({
	portfolioId,
	isOpen,
	opening,
	offeredSources,
	verdict,
}: DeliverySourceOptionsRequest): (
	sourceKey: string,
) => DeliverySourceOptionsState {
	const { deliveryService } = useContext(ApiServiceContext);
	const sourceKeys = (offeredSources ?? []).map((source) => source.key);
	// Asked for while the reader is still on another tab, so the list is there by the time they open
	// its tab. Only a licence known to be premium may ask: the server refuses everyone else.
	const enabled = isOpen && verdict === "premium";

	const results = useQueries({
		queries: sourceKeys.map((sourceKey) => ({
			...askedAfreshQueryOptions,
			queryKey: ["deliverySourceOptions", portfolioId, sourceKey, opening],
			queryFn: () =>
				deliveryService.getDeliverySourceOptions(portfolioId, sourceKey),
			enabled,
		})),
	});

	return (sourceKey) => {
		const result = results[sourceKeys.indexOf(sourceKey)];
		if (!enabled || result === undefined) {
			return STILL_LOADING;
		}
		if (result.status === "error") {
			return COULD_NOT_LOAD;
		}
		if (result.status === "pending") {
			return STILL_LOADING;
		}
		return { status: "ready", options: result.data };
	};
}
