import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import type React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ITerminology } from "../models/Terminology";
import { createMockApiServiceContext } from "../tests/MockApiServiceProvider";
import { ErrorBoundary } from "../tests/TestProviders";
import {
	ApiServiceContext,
	type IApiServiceContext,
} from "./Api/ApiServiceContext";
import type { ITerminologyService } from "./Api/TerminologyService";
import { TerminologyProvider, useTerminology } from "./TerminologyContext";

// Test component to access the terminology context in tests
const TestComponent = () => {
	const { terminology, isLoading, error } = useTerminology();
	return (
		<div>
			<span data-testid="work-item">
				{terminology?.workItem || "loading..."}
			</span>
			<span data-testid="work-items">
				{terminology?.workItems || "loading..."}
			</span>
			<span data-testid="is-loading">{isLoading.toString()}</span>
			<span data-testid="error">{error || "no-error"}</span>
		</div>
	);
};

const TestComponentOutsideProvider = () => {
	const result = useTerminology();
	return (
		<div data-testid="success">
			Should not reach here: {JSON.stringify(result)}
		</div>
	);
};

// Helper function to render with providers
const renderWithProviders = (
	component: React.ReactNode,
	terminologyService: ITerminologyService,
) => {
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
		terminologyService: terminologyService,
	});

	return render(
		<QueryClientProvider client={queryClient}>
			<ApiServiceContext.Provider value={mockApiContext}>
				<TerminologyProvider>{component}</TerminologyProvider>
			</ApiServiceContext.Provider>
		</QueryClientProvider>,
	);
};

describe("TerminologyContext", () => {
	let mockTerminologyService: ITerminologyService;

	beforeEach(() => {
		mockTerminologyService = {
			getTerminology: vi.fn(),
		};
	});

	it("provides default terminology initially", () => {
		const mockTerminology: ITerminology = {
			workItem: "Task",
			workItems: "Tasks",
		};

		mockTerminologyService.getTerminology = vi
			.fn()
			.mockResolvedValue(mockTerminology);

		renderWithProviders(<TestComponent />, mockTerminologyService);

		// Initially should show loading state
		expect(screen.getByTestId("is-loading")).toHaveTextContent("true");
	});

	it("loads terminology from service successfully", async () => {
		const mockTerminology: ITerminology = {
			workItem: "Issue",
			workItems: "Issues",
		};

		mockTerminologyService.getTerminology = vi
			.fn()
			.mockResolvedValue(mockTerminology);

		renderWithProviders(<TestComponent />, mockTerminologyService);

		await waitFor(() => {
			expect(screen.getByTestId("is-loading")).toHaveTextContent("false");
		});

		expect(screen.getByTestId("work-item")).toHaveTextContent("Issue");
		expect(screen.getByTestId("work-items")).toHaveTextContent("Issues");
		expect(screen.getByTestId("error")).toHaveTextContent("no-error");
	});

	it("calls terminology service once", async () => {
		const mockTerminology: ITerminology = {
			workItem: "Story",
			workItems: "Stories",
		};

		mockTerminologyService.getTerminology = vi
			.fn()
			.mockResolvedValue(mockTerminology);

		renderWithProviders(<TestComponent />, mockTerminologyService);

		await waitFor(() => {
			expect(screen.getByTestId("is-loading")).toHaveTextContent("false");
		});

		expect(mockTerminologyService.getTerminology).toHaveBeenCalledTimes(1);
	});

	it("throws error when useTerminology is used outside TerminologyProvider", () => {
		// Mock console.error to suppress React error boundary logs
		const consoleSpy = vi.spyOn(console, "error").mockImplementation(() => {});

		render(
			<ErrorBoundary>
				<TestComponentOutsideProvider />
			</ErrorBoundary>,
		);

		expect(screen.getByTestId("error")).toHaveTextContent(
			"useTerminology must be used within a TerminologyProvider",
		);

		consoleSpy.mockRestore();
	});

	it("maintains cached data across re-renders", async () => {
		const mockTerminology: ITerminology = {
			workItem: "Feature",
			workItems: "Features",
		};

		mockTerminologyService.getTerminology = vi
			.fn()
			.mockResolvedValue(mockTerminology);

		const { rerender } = renderWithProviders(
			<TestComponent />,
			mockTerminologyService,
		);

		await waitFor(() => {
			expect(screen.getByTestId("is-loading")).toHaveTextContent("false");
		});

		expect(screen.getByTestId("work-item")).toHaveTextContent("Feature");

		const mockApiContext: IApiServiceContext = createMockApiServiceContext({
			terminologyService: mockTerminologyService,
		});

		// Re-render the component
		rerender(
			<QueryClientProvider
				client={
					new QueryClient({
						defaultOptions: {
							queries: {
								retry: false,
								gcTime: 0,
								staleTime: 0,
							},
						},
					})
				}
			>
				<ApiServiceContext.Provider value={mockApiContext}>
					<TerminologyProvider>
						<TestComponent />
					</TerminologyProvider>
				</ApiServiceContext.Provider>
			</QueryClientProvider>,
		);

		// Should still show the same data without additional API calls
		expect(screen.getByTestId("work-item")).toHaveTextContent("Feature");
	});

	it("provides correct context interface", async () => {
		const mockTerminology: ITerminology = {
			workItem: "Epic",
			workItems: "Epics",
		};

		mockTerminologyService.getTerminology = vi
			.fn()
			.mockResolvedValue(mockTerminology);

		renderWithProviders(<TestComponent />, mockTerminologyService);

		await waitFor(() => {
			expect(screen.getByTestId("is-loading")).toHaveTextContent("false");
		});

		// Verify all expected properties are present and correct
		expect(screen.getByTestId("work-item")).toBeInTheDocument();
		expect(screen.getByTestId("work-items")).toBeInTheDocument();
		expect(screen.getByTestId("is-loading")).toBeInTheDocument();
		expect(screen.getByTestId("error")).toBeInTheDocument();
	});
});
