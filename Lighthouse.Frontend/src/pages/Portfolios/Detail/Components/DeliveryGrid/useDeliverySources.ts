import type { IDeliverySource } from "../../../../../models/Delivery/DeliverySource";

export const __SCAFFOLD__ = true;

/** Where the Portfolio's answer about the delivery sources its connection offers stands. */
export interface DeliverySourcesQuery {
	status: "pending" | "error" | "success";
	data: IDeliverySource[] | undefined;
	refetch: () => Promise<unknown>;
}

/**
 * The one answer the Deliveries page and the Delivery dialog both read, so opening the dialog does not
 * ask the server again for what the page already holds.
 */
export function useDeliverySources(_portfolioId: number): DeliverySourcesQuery {
	throw new Error("useDeliverySources is not built yet");
}
