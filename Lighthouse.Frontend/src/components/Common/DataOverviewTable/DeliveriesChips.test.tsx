import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Delivery } from "../../../models/Delivery";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { TerminologyProvider } from "../../../services/TerminologyContext";
import {
	createMockApiServiceContext,
	createMockDeliveryService,
} from "../../../tests/MockApiServiceProvider";
import { DeliveriesChips } from "./DeliveriesChips";

const mockDeliveryService = {
	...createMockDeliveryService(),
	getByPortfolio: vi.fn(),
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
		mockDeliveryService.getByPortfolio.mockResolvedValue({
			active: [],
			archived: [],
		});

		renderWithProviders(<DeliveriesChips portfolioId={1} />);

		await waitFor(() => {
			expect(screen.getByText("No Deliveries")).toBeInTheDocument();
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

		mockDeliveryService.getByPortfolio.mockResolvedValue({
			active: mockDeliveries,
			archived: [],
		});

		renderWithProviders(<DeliveriesChips portfolioId={100} />);

		await waitFor(() => {
			expect(
				screen.getByText(/Q1 Release.*3 Features.*All Features: 75%/),
			).toBeInTheDocument();
			expect(
				screen.getByText(/Q2 Release.*3 Features.*All Features: 90%/),
			).toBeInTheDocument();
		});

		expect(mockDeliveryService.getByPortfolio).toHaveBeenCalledWith(100);
	});

	// @us-12 @slice-12 @ac-12.6 @error @contract-shape:pure-function
	it.skip("says the Deliveries couldn't be loaded, never that there are none", async () => {
		mockDeliveryService.getByPortfolio.mockRejectedValue(
			new Error("API Error"),
		);

		renderWithProviders(<DeliveriesChips portfolioId={1} />);

		expect(
			await screen.findByText("Couldn't load Deliveries"),
		).toBeInTheDocument();
		expect(screen.queryByText("No Deliveries")).toBeNull();
	});

	// @us-12 @slice-12 @ac-12.5 @boundary @contract-shape:pure-function
	it.skip("shows a spinner, not No Deliveries, while the Deliveries are on their way", () => {
		mockDeliveryService.getByPortfolio.mockReturnValue(
			new Promise(() => undefined),
		);

		renderWithProviders(<DeliveriesChips portfolioId={1} />);

		expect(screen.getByRole("progressbar")).toBeInTheDocument();
		expect(screen.queryByText("No Deliveries")).toBeNull();
	});

	// @us-12 @slice-12 @ac-12.4 @boundary @contract-shape:pure-function
	it.skip("shows the first two Deliveries in the order the server answers, then +N more to the Portfolio's Deliveries", async () => {
		mockDeliveryService.getByPortfolio.mockResolvedValue({
			active: [
				getMockDelivery({ id: 1, name: "Winter Release", portfolioId: 100 }),
				getMockDelivery({ id: 2, name: "Autumn Release", portfolioId: 100 }),
				getMockDelivery({ id: 3, name: "Summer Release", portfolioId: 100 }),
			],
			archived: [],
		});

		renderWithProviders(<DeliveriesChips portfolioId={100} />);

		const more = await screen.findByRole("link", { name: "+1 more" });
		const links = screen.getAllByRole("link");
		expect(links).toHaveLength(3);
		expect(links[0]).toHaveTextContent(/^Winter Release/);
		expect(links[1]).toHaveTextContent(/^Autumn Release/);
		expect(more).toHaveAttribute("href", "/portfolios/100/deliveries");
		expect(screen.queryByText(/Summer Release/)).toBeNull();
	});

	it("should navigate to portfolio deliveries page when chip is clicked", async () => {
		const mockDeliveries = [
			getMockDelivery({
				id: 1,
				name: "Q1 Release",
				portfolioId: 100,
			}),
		];

		mockDeliveryService.getByPortfolio.mockResolvedValue({
			active: mockDeliveries,
			archived: [],
		});

		renderWithProviders(<DeliveriesChips portfolioId={100} />);

		await waitFor(() => {
			expect(
				screen.getByText(/Q1 Release.*3 Features.*All Features: 75%/),
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
				id: 4,
				name: "Certain Delivery",
				likelihoodPercentage: 95,
				portfolioId: 100,
			}),
		];

		mockDeliveryService.getByPortfolio.mockResolvedValue({
			active: mockDeliveries,
			archived: [],
		});

		renderWithProviders(<DeliveriesChips portfolioId={100} />);

		await waitFor(() => {
			expect(screen.getByText(/Risky Delivery/)).toBeInTheDocument();
			expect(screen.getByText(/Certain Delivery/)).toBeInTheDocument();
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

		mockDeliveryService.getByPortfolio.mockResolvedValue({
			active: mockDeliveries,
			archived: [],
		});

		renderWithProviders(<DeliveriesChips portfolioId={1} />);

		await waitFor(() => {
			// Should use the "features" terminology from the terminology service
			expect(
				screen.getByText(/Feature Release.*5 Features/),
			).toBeInTheDocument();
		});
	});

	describe("never presents a portfolio delivery forecast as a certainty", () => {
		it("reads >95% on the portfolio chip for a delivery above 95% with work left", async () => {
			const mockDeliveries = [
				getMockDelivery({
					id: 1,
					name: "Q1 Release",
					likelihoodPercentage: 100,
					remainingWork: 25,
					portfolioId: 100,
				}),
			];

			mockDeliveryService.getByPortfolio.mockResolvedValue({
				active: mockDeliveries,
				archived: [],
			});

			renderWithProviders(<DeliveriesChips portfolioId={100} />);

			await waitFor(() => {
				expect(
					screen.getByText(/Q1 Release.*All Features: >95%/),
				).toBeInTheDocument();
			});
			expect(screen.queryByText(/All Features: 100%/)).not.toBeInTheDocument();
		});

		it("keeps the precise 95% for a delivery at the boundary", async () => {
			const mockDeliveries = [
				getMockDelivery({
					id: 1,
					name: "Q1 Release",
					likelihoodPercentage: 95,
					remainingWork: 25,
					portfolioId: 100,
				}),
			];

			mockDeliveryService.getByPortfolio.mockResolvedValue({
				active: mockDeliveries,
				archived: [],
			});

			renderWithProviders(<DeliveriesChips portfolioId={100} />);

			await waitFor(() => {
				expect(
					screen.getByText(/Q1 Release.*All Features: 95%/),
				).toBeInTheDocument();
			});
			expect(screen.queryByText(/All Features: >95%/)).not.toBeInTheDocument();
		});

		it("still reads 100% for a completed delivery with no work left", async () => {
			const mockDeliveries = [
				getMockDelivery({
					id: 1,
					name: "Q1 Release",
					likelihoodPercentage: 100,
					remainingWork: 0,
					portfolioId: 100,
				}),
			];

			mockDeliveryService.getByPortfolio.mockResolvedValue({
				active: mockDeliveries,
				archived: [],
			});

			renderWithProviders(<DeliveriesChips portfolioId={100} />);

			await waitFor(() => {
				expect(
					screen.getByText(/Q1 Release.*All Features: 100%/),
				).toBeInTheDocument();
			});
			expect(screen.queryByText(/All Features: >95%/)).not.toBeInTheDocument();
		});
	});

	describe("suppresses a portfolio delivery forecast built on too little throughput data", () => {
		it("shows a not-enough-data indicator instead of a percentage when the owning team lacks throughput history", async () => {
			const mockDeliveries = [
				getMockDelivery({
					id: 1,
					name: "New Team Release",
					likelihoodPercentage: 100,
					remainingWork: 25,
					hasSufficientData: false,
					portfolioId: 100,
				}),
			];

			mockDeliveryService.getByPortfolio.mockResolvedValue({
				active: mockDeliveries,
				archived: [],
			});

			renderWithProviders(<DeliveriesChips portfolioId={100} />);

			await waitFor(() => {
				expect(screen.getByText(/not enough.*data/i)).toBeInTheDocument();
			});
			expect(screen.queryByText(/All Features: 100%/)).not.toBeInTheDocument();
			expect(screen.queryByText(/All Features: >95%/)).not.toBeInTheDocument();
		});

		it("still shows the precise likelihood when the owning team has enough throughput data", async () => {
			const mockDeliveries = [
				getMockDelivery({
					id: 1,
					name: "Mature Team Release",
					likelihoodPercentage: 80,
					remainingWork: 25,
					hasSufficientData: true,
					portfolioId: 100,
				}),
			];

			mockDeliveryService.getByPortfolio.mockResolvedValue({
				active: mockDeliveries,
				archived: [],
			});

			renderWithProviders(<DeliveriesChips portfolioId={100} />);

			await waitFor(() => {
				expect(
					screen.getByText(/Mature Team Release.*All Features: 80%/),
				).toBeInTheDocument();
			});
			expect(screen.queryByText(/not enough.*data/i)).not.toBeInTheDocument();
		});

		it("does not suppress a completed delivery with no remaining work even when flagged insufficient", async () => {
			const mockDeliveries = [
				getMockDelivery({
					id: 1,
					name: "Done Release",
					likelihoodPercentage: 100,
					remainingWork: 0,
					hasSufficientData: false,
					portfolioId: 100,
				}),
			];

			mockDeliveryService.getByPortfolio.mockResolvedValue({
				active: mockDeliveries,
				archived: [],
			});

			renderWithProviders(<DeliveriesChips portfolioId={100} />);

			await waitFor(() => {
				expect(
					screen.getByText(/Done Release.*All Features: 100%/),
				).toBeInTheDocument();
			});
			expect(screen.queryByText(/not enough.*data/i)).not.toBeInTheDocument();
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
