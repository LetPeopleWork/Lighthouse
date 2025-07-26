import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render } from "@testing-library/react";
import React from "react";
import { vi } from "vitest";
import {
	ApiServiceContext,
	type IApiServiceContext,
} from "../services/Api/ApiServiceContext";
import type { ITerminologyService } from "../services/Api/TerminologyService";
import { TerminologyProvider } from "../services/TerminologyContext";
import { createMockApiServiceContext } from "./MockApiServiceProvider";

// Default mock terminology service
const createMockTerminologyService = (): ITerminologyService => {
	return {
		getTerminology: vi.fn().mockResolvedValue({
			workItem: "Work Item",
			workItems: "Work Items",
		}),
	};
};

// Test Providers wrapper component
export const TestProviders = ({
	children,
	terminologyService,
	apiServiceOverrides,
}: {
	children: React.ReactNode;
	terminologyService?: ITerminologyService;
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

	const mockTerminologyService =
		terminologyService || createMockTerminologyService();

	const mockApiContext: IApiServiceContext = createMockApiServiceContext({
		terminologyService: mockTerminologyService,
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
	options?: {
		terminologyService?: ITerminologyService;
		apiServiceOverrides?: Partial<IApiServiceContext>;
	},
) => {
	return render(
		<TestProviders
			terminologyService={options?.terminologyService}
			apiServiceOverrides={options?.apiServiceOverrides}
		>
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
