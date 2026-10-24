import type {
	IDeliverySource,
	IDeliverySourceOption,
} from "../../../../../models/Delivery/DeliverySource";
import type { WidgetStatus } from "../../../../Common/MetricsView/widgetStatus";
import type { PremiumVerdict } from "./premiumVerdict";

export const __SCAFFOLD__ = true;

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

/** The entries every offered source holds, asked for once per opening of the dialog. */
export function useDeliverySourceOptions(
	_request: DeliverySourceOptionsRequest,
): (sourceKey: string) => DeliverySourceOptionsState {
	throw new Error("useDeliverySourceOptions is not built yet");
}
