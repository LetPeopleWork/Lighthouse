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
};

// What the product calls something before this instance has said otherwise - the same words a fresh
// install is seeded with. The list is fetched once and never retried, so between first paint and the
// answer arriving, and forever after if the answer never does, this is what a reader sees. Handing
// back the lookup key instead prints "the order of your features" - a lowercase key read as prose.
// A key the product has no word for is still returned as it is: it is a caller's mistake, and
// printing it verbatim is what makes it noticeable.
const theProductsOwnWordFor = (key: string): string =>
	defaultTerminologyMap[key] ?? key;

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

	// Bug #5732: the `= []` default above only fires for undefined. A resolved-but-wrong
	// payload (an HTML string, once) reached `.find` during render and blanked the app.
	const terms = useMemo(
		() => (Array.isArray(terminologyData) ? terminologyData : []),
		[terminologyData],
	);

	const getTerm = useCallback(
		(key: string): string => {
			const term = terms.find((t) => t.key === key);

			return term?.value || term?.defaultValue || theProductsOwnWordFor(key);
		},
		[terms],
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

export function useTerminology(): ITerminologyContext {
	const context = useContext(TerminologyContext);
	if (context === null || context === undefined) {
		// Return dummy context for testing
		return {
			getTerm: theProductsOwnWordFor,
			isLoading: false,
			error: null,
			refetchTerminology: () => {},
		};
	}
	return context;
}
