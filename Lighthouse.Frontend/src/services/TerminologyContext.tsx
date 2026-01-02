import { useQuery, useQueryClient } from "@tanstack/react-query";
import { createContext, useCallback, useContext, useMemo } from "react";
import { TERMINOLOGY_KEYS } from "../models/TerminologyKeys";
import { ApiServiceContext } from "./Api/ApiServiceContext";

interface ITerminologyContext {
	getTerm: (key: string) => string;
	isLoading: boolean;
	error: string | null;
	refetchTerminology: () => void;
}

const TerminologyContext = createContext<ITerminologyContext | null>(null);

export function TerminologyProvider({
	children,
}: {
	readonly children: React.ReactNode;
}) {
	const { terminologyService } = useContext(ApiServiceContext);
	const queryClient = useQueryClient();

	const {
		data: terminologyData = [],
		isLoading,
		error,
		refetch,
	} = useQuery({
		queryKey: ["terminology-database"],
		queryFn: () => terminologyService.getAllTerminology(),
		staleTime: 1000 * 60 * 5, // 5 minutes - refresh more frequently for configurable data
		gcTime: 1000 * 60 * 60 * 24, // 24 hours cache time
		retry: false, // Disable retries to prevent hanging in loading state on errors
		refetchOnWindowFocus: false,
		refetchOnMount: false,
		refetchOnReconnect: true,
	});

	const refetchTerminology = useCallback(() => {
		queryClient.invalidateQueries({ queryKey: ["terminology-database"] });
		refetch();
	}, [queryClient, refetch]);

	const getTerm = useCallback(
		(key: string): string => {
			const term = terminologyData.find((t) => t.key === key);

			if (!term) {
				return key;
			}

			return term.value || term.defaultValue || key;
		},
		[terminologyData],
	);

	const contextValue = useMemo(
		() => ({
			isLoading,
			error: error ? "Failed to load terminology" : null,
			refetchTerminology,
			getTerm,
		}),
		[isLoading, error, refetchTerminology, getTerm],
	);

	return (
		<TerminologyContext.Provider value={contextValue}>
			{children}
		</TerminologyContext.Provider>
	);
}

const defaultTerminologyMap: Record<string, string> = {
	[TERMINOLOGY_KEYS.WORK_ITEM]: "Work Item",
	[TERMINOLOGY_KEYS.WORK_ITEMS]: "Work Items",
	[TERMINOLOGY_KEYS.FEATURE]: "Feature",
	[TERMINOLOGY_KEYS.FEATURES]: "Features",
	[TERMINOLOGY_KEYS.CYCLE_TIME]: "Cycle Time",
	[TERMINOLOGY_KEYS.THROUGHPUT]: "Throughput",
	[TERMINOLOGY_KEYS.WORK_IN_PROGRESS]: "Work In Progress",
	[TERMINOLOGY_KEYS.WIP]: "WIP",
	[TERMINOLOGY_KEYS.WORK_ITEM_AGE]: "Work Item Age",
	[TERMINOLOGY_KEYS.TAG]: "Tag",
	[TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEM]: "Work Tracking System",
	[TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEMS]: "Work Tracking Systems",
	[TERMINOLOGY_KEYS.BLOCKED]: "Blocked",
	[TERMINOLOGY_KEYS.SERVICE_LEVEL_EXPECTATION]: "Service Level Expectation",
	[TERMINOLOGY_KEYS.SLE]: "SLE",
	[TERMINOLOGY_KEYS.TEAM]: "Team",
	[TERMINOLOGY_KEYS.TEAMS]: "Teams",
	[TERMINOLOGY_KEYS.PORTFOLIO]: "Portfolio",
	[TERMINOLOGY_KEYS.PORTFOLIOS]: "Portfolios",
	[TERMINOLOGY_KEYS.DELIVERY]: "Delivery",
	[TERMINOLOGY_KEYS.DELIVERIES]: "Deliveries",
	// Add more terminology mappings here as needed
};

export function useTerminology(): ITerminologyContext {
	const context = useContext(TerminologyContext);
	if (context === null || context === undefined) {
		// Return dummy context for testing
		return {
			getTerm: (key: string) => {
				return defaultTerminologyMap[key] || key;
			},
			isLoading: false,
			error: null,
			refetchTerminology: () => {},
		};
	}
	return context;
}
