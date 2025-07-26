import { useQuery } from "@tanstack/react-query";
import { createContext, useContext, useMemo } from "react";
import type { ITerminology } from "../models/Terminology";
import { ApiServiceContext } from "./Api/ApiServiceContext";

interface ITerminologyContext {
	terminology: ITerminology;
	isLoading: boolean;
	error: string | null;
}

const defaultTerminology: ITerminology = {
	workItem: "Work Item",
	workItems: "Work Items",
};

const TerminologyContext = createContext<ITerminologyContext | null>(null);

export function TerminologyProvider({
	children,
}: {
	readonly children: React.ReactNode;
}) {
	const { terminologyService } = useContext(ApiServiceContext);

	const {
		data: terminology = defaultTerminology,
		isLoading,
		error,
	} = useQuery({
		queryKey: ["terminology"],
		queryFn: () => terminologyService.getTerminology(),
		staleTime: 1000 * 60 * 60 * 24, // 24 hours - terminology rarely changes
		gcTime: 1000 * 60 * 60 * 24 * 7, // 7 days cache time
		retry: 3,
		retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
		refetchOnWindowFocus: false,
		refetchOnMount: false,
		refetchOnReconnect: true,
	});

	const contextValue = useMemo(
		() => ({
			terminology,
			isLoading,
			error: error ? "Failed to load terminology" : null,
		}),
		[terminology, isLoading, error],
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
