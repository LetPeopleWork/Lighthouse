import { useQuery, useQueryClient } from "@tanstack/react-query";
import { createContext, useCallback, useContext, useMemo } from "react";
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

export function useTerminology(): ITerminologyContext {
	const context = useContext(TerminologyContext);
	if (context === null || context === undefined) {
		throw new Error("useTerminology must be used within a TerminologyProvider");
	}
	return context;
}
