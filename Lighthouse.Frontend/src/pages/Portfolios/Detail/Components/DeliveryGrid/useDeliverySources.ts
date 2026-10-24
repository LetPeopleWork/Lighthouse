import { useQuery } from "@tanstack/react-query";
import { useContext } from "react";
import type { IDeliverySource } from "../../../../../models/Delivery/DeliverySource";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";

// Shared only by the readers on screen at the same time: nothing is kept once the last one goes, so
// opening the page again asks again, and a failure is not retried behind the reader's back.
const deliverySourcesQueryOptions = {
	staleTime: 0,
	gcTime: 0,
	retry: false,
	refetchOnWindowFocus: false,
	refetchOnReconnect: false,
} as const;

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
export function useDeliverySources(portfolioId: number): DeliverySourcesQuery {
	const { deliveryService } = useContext(ApiServiceContext);

	const { status, data, refetch } = useQuery({
		...deliverySourcesQueryOptions,
		queryKey: ["deliverySources", portfolioId] as const,
		queryFn: () => deliveryService.getDeliverySources(portfolioId),
	});

	return { status, data, refetch };
}
