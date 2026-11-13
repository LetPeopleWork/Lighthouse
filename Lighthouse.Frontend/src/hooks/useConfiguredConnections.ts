import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useCallback, useContext } from "react";
import type { IWorkTrackingSystemConnection } from "../models/WorkTracking/WorkTrackingSystemConnection";
import { ApiError } from "../services/Api/ApiError";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";

const CONFIGURED_CONNECTIONS_KEY = ["configuredConnections"] as const;

/** The configured connections, as far as the list can truthfully be told. */
export type ConfiguredConnectionsState =
	| { status: "loading" }
	| { status: "refused" }
	| { status: "failed" }
	| { status: "ready"; connections: IWorkTrackingSystemConnection[] };

export const CONNECTIONS_REFUSED_MESSAGE =
	"You don't have access to the connections list. Ask a Lighthouse administrator to check your roles.";

const isRefusal = (error: unknown): boolean =>
	error instanceof ApiError && error.code === 403;

/**
 * Drops the remembered list after a connection was added, changed or removed, so the next reader asks the
 * server again instead of showing the list from before as if it were current. A list on screen asks again at once,
 * showing its spinner meanwhile.
 */
export const useForgetConfiguredConnections = (): (() => void) => {
	const queryClient = useQueryClient();
	return useCallback(() => {
		queryClient
			.resetQueries({ queryKey: CONFIGURED_CONNECTIONS_KEY })
			.catch(() => undefined);
	}, [queryClient]);
};

export const useConfiguredConnections = (): ConfiguredConnectionsState => {
	const { workTrackingSystemService } = useContext(ApiServiceContext);
	const list = useQuery({
		queryKey: CONFIGURED_CONNECTIONS_KEY,
		queryFn: () => workTrackingSystemService.getConfiguredWorkTrackingSystems(),
		refetchOnWindowFocus: false,
	});

	if (list.isError) {
		return { status: isRefusal(list.error) ? "refused" : "failed" };
	}
	if (list.data === undefined) {
		return { status: "loading" };
	}
	return { status: "ready", connections: list.data };
};
