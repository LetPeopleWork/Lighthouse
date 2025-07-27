import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render } from "@testing-library/react";
import React from "react";
import { vi } from "vitest";
import { TERMINOLOGY_KEYS } from "../models/TerminologyKeys";
import {
	ApiServiceContext,
	type IApiServiceContext,
} from "../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../services/TerminologyContext";
import { createMockApiServiceContext } from "./MockApiServiceProvider";

// Simple key/value dictionary for terminology mappings - easily extensible
const mockTerminologyMap: Record<string, string> = {
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
	[TERMINOLOGY_KEYS.WORK_ITEM_QUERY]: "Work Item Query",
	[TERMINOLOGY_KEYS.BLOCKED]: "Blocked",
	[TERMINOLOGY_KEYS.SERVICE_LEVEL_EXPECTATION]: "Service Level Expectation",
	[TERMINOLOGY_KEYS.SLE]: "SLE",
	[TERMINOLOGY_KEYS.TEAM]: "Team",
	[TERMINOLOGY_KEYS.TEAMS]: "Teams",
	// Add more terminology mappings here as needed
};

vi.mock("../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => {
			return mockTerminologyMap[key] || key;
		},
		isLoading: false,
		error: null,
		refetchTerminology: vi.fn(),
	}),
	TerminologyProvider: ({ children }: { children: React.ReactNode }) =>
		children,
}));

// Test Providers wrapper component
export const TestProviders = ({
	children,
	apiServiceOverrides,
}: {
	children: React.ReactNode;
	apiServiceOverrides?: Partial<IApiServiceContext>;
}) => {
	const queryClient = new QueryClient({
		defaultOptions: {
			queries: {
				retry: false, // Disable retries for tests
				gcTime: 0, // Disable caching for tests
				staleTime: 0,
			},
		},
	});

	const mockApiContext: IApiServiceContext = createMockApiServiceContext({
		...apiServiceOverrides,
	});

	return (
		<QueryClientProvider client={queryClient}>
			<ApiServiceContext.Provider value={mockApiContext}>
				<TerminologyProvider>{children}</TerminologyProvider>
			</ApiServiceContext.Provider>
		</QueryClientProvider>
	);
};

// Utility function to render components with all necessary providers
export const renderWithProviders = (
	component: React.ReactNode,
	apiServiceOverrides?: Partial<IApiServiceContext>,
) => {
	return render(
		<TestProviders apiServiceOverrides={apiServiceOverrides}>
			{component}
		</TestProviders>,
	);
};

// Error Boundary component for testing error scenarios
export class ErrorBoundary extends React.Component<
	{ children: React.ReactNode },
	{ hasError: boolean; error: Error | null }
> {
	constructor(props: { children: React.ReactNode }) {
		super(props);
		this.state = { hasError: false, error: null };
	}

	static getDerivedStateFromError(error: Error) {
		return { hasError: true, error };
	}

	componentDidCatch(_error: Error, _errorInfo: React.ErrorInfo) {
		// Error caught by boundary during testing
	}

	render() {
		if (this.state.hasError) {
			return (
				<div data-testid="error">
					{this.state.error?.message || "An error occurred"}
				</div>
			);
		}

		return this.props.children;
	}
}
