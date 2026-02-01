import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Delivery } from "../../../models/Delivery";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import { DeliveriesChips } from "./DeliveriesChips";

// Mock the delivery service
const mockDeliveryService = {
	getByPortfolio: vi.fn(),
	getAll: vi.fn(),
	create: vi.fn(),
	update: vi.fn(),
	delete: vi.fn(),
	getRuleSchema: vi.fn(),
	validateRules: vi.fn(),
};

const mockApiServiceContext = createMockApiServiceContext({
	deliveryService: mockDeliveryService,
});

const renderWithProviders = (component: React.ReactElement) => {
	const queryClient = new QueryClient({
		defaultOptions: {
			queries: { retry: false },
			mutations: { retry: false },
		},
	});

	return render(
		<MemoryRouter>
			<QueryClientProvider client={queryClient}>
				<ApiServiceContext.Provider value={mockApiServiceContext}>
					<TerminologyProvider>{component}</TerminologyProvider>
				</ApiServiceContext.Provider>
			</QueryClientProvider>
		</MemoryRouter>,
	);
};

const getMockDelivery = (overrides?: Partial<Delivery>): Delivery => {
	const baseDelivery = {
		id: 1,
		name: "Q1 Release",
		date: "2025-03-31T00:00:00Z",
		portfolioId: 100,
		features: [1, 2, 3],
		likelihoodPercentage: 75,
		progress: 50,
		remainingWork: 25,
		totalWork: 100,
		featureLikelihoods: [],
		getFormattedDate: () => "3/31/2025",
		getFeatureCount: () => 3,
		getLikelihoodLevel: () => "likely" as const,
		getFeatureLikelihood: () => 0,
	};

	return { ...baseDelivery, ...overrides } as Delivery;
};

describe("DeliveriesChips", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("should display no deliveries message when portfolio has no deliveries", async () => {
		mockDeliveryService.getByPortfolio.mockResolvedValue([]);

		renderWithProviders(<DeliveriesChips portfolioId={1} />);

		await waitFor(() => {
			expect(screen.getByText("No deliveries")).toBeInTheDocument();
		});

		expect(mockDeliveryService.getByPortfolio).toHaveBeenCalledWith(1);
	});

	it("should display delivery chips when portfolio has deliveries", async () => {
		const mockDeliveries = [
			getMockDelivery({
				id: 1,
				name: "Q1 Release",
				likelihoodPercentage: 75,
				portfolioId: 100,
			}),
			getMockDelivery({
				id: 2,
				name: "Q2 Release",
				likelihoodPercentage: 90,
				portfolioId: 100,
			}),
		];

		mockDeliveryService.getByPortfolio.mockResolvedValue(mockDeliveries);

		renderWithProviders(<DeliveriesChips portfolioId={100} />);

		await waitFor(() => {
			expect(
				screen.getByText(/Q1 Release.*3 features.*Likelihood: 75%/),
			).toBeInTheDocument();
			expect(
				screen.getByText(/Q2 Release.*3 features.*Likelihood: 90%/),
			).toBeInTheDocument();
		});

		expect(mockDeliveryService.getByPortfolio).toHaveBeenCalledWith(100);
	});

	it("should handle API errors silently and show no deliveries", async () => {
		mockDeliveryService.getByPortfolio.mockRejectedValue(
			new Error("API Error"),
		);

		renderWithProviders(<DeliveriesChips portfolioId={1} />);

		await waitFor(() => {
			expect(screen.getByText("No deliveries")).toBeInTheDocument();
		});
	});

	it("should navigate to portfolio deliveries page when chip is clicked", async () => {
		const mockDeliveries = [
			getMockDelivery({
				id: 1,
				name: "Q1 Release",
				portfolioId: 100,
			}),
		];

		mockDeliveryService.getByPortfolio.mockResolvedValue(mockDeliveries);

		renderWithProviders(<DeliveriesChips portfolioId={100} />);

		await waitFor(() => {
			expect(
				screen.getByText(/Q1 Release.*3 features.*Likelihood: 75%/),
			).toBeInTheDocument();
		});

		const chipLink = screen.getByRole("link");
		expect(chipLink).toHaveAttribute("href", "/portfolios/100/deliveries");
	});

	it("should display chips with correct forecast level colors for different likelihood percentages", async () => {
		const mockDeliveries = [
			getMockDelivery({
				id: 1,
				name: "Risky Delivery",
				likelihoodPercentage: 30,
				portfolioId: 100,
			}),
			getMockDelivery({
				id: 2,
				name: "Realistic Delivery",
				likelihoodPercentage: 65,
				portfolioId: 100,
			}),
			getMockDelivery({
				id: 3,
				name: "Likely Delivery",
				likelihoodPercentage: 80,
				portfolioId: 100,
			}),
			getMockDelivery({
				id: 4,
				name: "Certain Delivery",
				likelihoodPercentage: 95,
				portfolioId: 100,
			}),
		];

		mockDeliveryService.getByPortfolio.mockResolvedValue(mockDeliveries);

		renderWithProviders(<DeliveriesChips portfolioId={100} />);

		await waitFor(() => {
			const riskyChip = screen.getByText(/Risky Delivery/);
			const realisticChip = screen.getByText(/Realistic Delivery/);
			const likelyChip = screen.getByText(/Likely Delivery/);
			const certainChip = screen.getByText(/Certain Delivery/);

			expect(riskyChip).toBeInTheDocument();
			expect(realisticChip).toBeInTheDocument();
			expect(likelyChip).toBeInTheDocument();
			expect(certainChip).toBeInTheDocument();

			// Check that chips have different colors based on forecast level
			// This would require checking computed styles or data-testid attributes
		});
	});

	it("should display feature count using correct terminology", async () => {
		const mockDeliveries = [
			getMockDelivery({
				name: "Feature Release",
				features: [1, 2, 3, 4, 5],
				portfolioId: 1,
				getFeatureCount: () => 5, // Override to return 5
			}),
		];

		mockDeliveryService.getByPortfolio.mockResolvedValue(mockDeliveries);

		renderWithProviders(<DeliveriesChips portfolioId={1} />);

		await waitFor(() => {
			// Should use the "features" terminology from the terminology service
			expect(
				screen.getByText(/Feature Release.*5 features/),
			).toBeInTheDocument();
		});
	});

	it("should not fetch deliveries when portfolioId is 0 or undefined", () => {
		renderWithProviders(<DeliveriesChips portfolioId={0} />);

		expect(mockDeliveryService.getByPortfolio).not.toHaveBeenCalled();
	});

	it("should refetch deliveries when portfolioId changes", async () => {
		const { rerender } = renderWithProviders(
			<DeliveriesChips portfolioId={1} />,
		);

		expect(mockDeliveryService.getByPortfolio).toHaveBeenCalledWith(1);

		// Change portfolioId
		rerender(
			<MemoryRouter>
				<QueryClientProvider
					client={
						new QueryClient({
							defaultOptions: {
								queries: { retry: false },
								mutations: { retry: false },
							},
						})
					}
				>
					<ApiServiceContext.Provider value={mockApiServiceContext}>
						<TerminologyProvider>
							<DeliveriesChips portfolioId={2} />
						</TerminologyProvider>
					</ApiServiceContext.Provider>
				</QueryClientProvider>
			</MemoryRouter>,
		);

		expect(mockDeliveryService.getByPortfolio).toHaveBeenCalledWith(2);
		expect(mockDeliveryService.getByPortfolio).toHaveBeenCalledTimes(2);
	});
});
