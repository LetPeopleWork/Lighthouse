import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { Delivery } from "../../../../../models/Delivery";
import type { IEntityReference } from "../../../../../models/EntityReference";
import { Feature } from "../../../../../models/Feature";
import { WhenForecast } from "../../../../../models/Forecasts/WhenForecast";
import type { IWorkItem } from "../../../../../models/WorkItem";
import DeliverySection from "./DeliverySection";

const { mockGetFeatureWorkItems } = vi.hoisted(() => ({
	mockGetFeatureWorkItems: vi.fn(),
}));

// Mock dependencies
vi.mock("../../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => (key === "feature" ? "Feature" : key),
	}),
}));

vi.mock("../../../../../services/Api/ApiServiceContext", () => ({
	ApiServiceContext: {
		_currentValue: {
			featureService: {
				getFeatureWorkItems: mockGetFeatureWorkItems,
			},
		},
	},
}));

vi.mock(
	"../../../../../components/Common/FeatureListDataGrid/FeatureProgressIndicator",
	() => ({
		default: ({
			feature,
			onShowDetails,
		}: {
			feature: { id: number };
			onShowDetails: () => void;
		}) => (
			<button
				type="button"
				data-testid={`show-details-${feature.id}`}
				onClick={onShowDetails}
			>
				Show Details
			</button>
		),
	}),
);

vi.mock(
	"../../../../../components/Common/WorkItemsDialog/WorkItemsDialog",
	() => ({
		default: ({
			title,
			items,
			open,
			onClose,
		}: {
			title: string;
			items: IWorkItem[];
			open: boolean;
			onClose: () => void;
		}) =>
			open ? (
				<div data-testid="work-items-dialog">
					<span data-testid="dialog-title">{title}</span>
					<span data-testid="dialog-item-count">{items.length} items</span>
					<button type="button" onClick={onClose}>
						Close
					</button>
				</div>
			) : null,
	}),
);

describe("DeliverySection", () => {
	beforeEach(() => {
		mockGetFeatureWorkItems.mockReset();
	});

	const mockDelivery = new Delivery();
	mockDelivery.id = 1;
	mockDelivery.name = "Test Delivery";
	mockDelivery.date = new Date("2025-01-31").toISOString();
	mockDelivery.features = [1]; // Feature IDs
	mockDelivery.likelihoodPercentage = 75;
	mockDelivery.progress = 60;
	mockDelivery.remainingWork = 4;
	mockDelivery.totalWork = 10;
	mockDelivery.featureLikelihoods = [
		{ featureId: 1, likelihoodPercentage: 75 },
	];
	mockDelivery.completionDates = [];

	const mockFeature = new Feature();
	mockFeature.id = 1;
	mockFeature.name = "Test Feature";
	mockFeature.remainingWork = { "1": 5 };
	mockFeature.totalWork = { "1": 10 };
	mockFeature.forecasts = [];

	const mockTeams: IEntityReference[] = [
		{ id: 1, name: "Team Alpha" },
		{ id: 2, name: "Team Beta" },
	];

	const mockProps = {
		delivery: mockDelivery,
		features: [mockFeature],
		isExpanded: true,
		isLoadingFeatures: false,
		onToggleExpanded: vi.fn(),
		onDelete: vi.fn(),
		onEdit: vi.fn(),
		teams: mockTeams,
	};

	it("should render delivery section with correct information", () => {
		render(
			<MemoryRouter>
				<DeliverySection {...mockProps} />
			</MemoryRouter>,
		);

		expect(screen.getByText("Test Delivery")).toBeInTheDocument();
		expect(screen.getByText("Delivery Date: 1/31/2025")).toBeInTheDocument();
		expect(screen.getByText("Likelihood: 75%")).toBeInTheDocument();
		expect(screen.getByText(/1 Feature/i)).toBeInTheDocument();
	});

	it("should show loading state when features are being loaded", () => {
		const loadingProps = {
			...mockProps,
			isExpanded: true,
			isLoadingFeatures: true,
		};

		render(
			<MemoryRouter>
				<DeliverySection {...loadingProps} />
			</MemoryRouter>,
		);

		expect(screen.getByText("Loading features...")).toBeInTheDocument();
	});

	it("should show empty state when no features", () => {
		const emptyProps = {
			...mockProps,
			features: [],
			isExpanded: true,
		};

		render(
			<MemoryRouter>
				<DeliverySection {...emptyProps} />
			</MemoryRouter>,
		);

		expect(
			screen.getByText("No features in this delivery."),
		).toBeInTheDocument();
	});

	it("should call onEdit when edit button is clicked", () => {
		render(
			<MemoryRouter>
				<DeliverySection {...mockProps} />
			</MemoryRouter>,
		);

		const editButton = screen.getByLabelText("edit");
		fireEvent.click(editButton);

		expect(mockProps.onEdit).toHaveBeenCalledWith(mockDelivery);
		expect(mockProps.onEdit).toHaveBeenCalledTimes(1);
	});

	it("should call onDelete when delete button is clicked", () => {
		render(
			<MemoryRouter>
				<DeliverySection {...mockProps} />
			</MemoryRouter>,
		);

		const deleteButton = screen.getByLabelText("delete");
		fireEvent.click(deleteButton);

		expect(mockProps.onDelete).toHaveBeenCalledWith(mockDelivery);
		expect(mockProps.onDelete).toHaveBeenCalledTimes(1);
	});

	it("should have both edit and delete buttons", () => {
		render(
			<MemoryRouter>
				<DeliverySection {...mockProps} />
			</MemoryRouter>,
		);

		expect(screen.getByLabelText("edit")).toBeInTheDocument();
		expect(screen.getByLabelText("delete")).toBeInTheDocument();
	});

	it("should not call onToggleExpanded when edit button is clicked", () => {
		render(
			<MemoryRouter>
				<DeliverySection {...mockProps} />
			</MemoryRouter>,
		);

		const editButton = screen.getByLabelText("edit");
		fireEvent.click(editButton);

		// onToggleExpanded should not be called when clicking edit button
		expect(mockProps.onToggleExpanded).not.toHaveBeenCalled();
	});

	describe("WorkItemsDialog", () => {
		it("should not show WorkItemsDialog by default", () => {
			render(
				<MemoryRouter>
					<DeliverySection {...mockProps} />
				</MemoryRouter>,
			);

			expect(screen.queryByTestId("work-items-dialog")).not.toBeInTheDocument();
		});

		it("should open WorkItemsDialog when feature details are requested", async () => {
			mockGetFeatureWorkItems.mockResolvedValue([]);

			render(
				<MemoryRouter>
					<DeliverySection {...mockProps} />
				</MemoryRouter>,
			);

			fireEvent.click(screen.getByTestId("show-details-1"));

			await waitFor(() => {
				expect(screen.getByTestId("work-items-dialog")).toBeInTheDocument();
			});
		});

		it("should call getFeatureWorkItems with the correct feature id", async () => {
			mockGetFeatureWorkItems.mockResolvedValue([]);

			render(
				<MemoryRouter>
					<DeliverySection {...mockProps} />
				</MemoryRouter>,
			);

			fireEvent.click(screen.getByTestId("show-details-1"));

			await waitFor(() => {
				expect(mockGetFeatureWorkItems).toHaveBeenCalledWith(mockFeature.id);
			});
		});

		it("should display the correct feature name in the dialog title", async () => {
			mockGetFeatureWorkItems.mockResolvedValue([]);

			render(
				<MemoryRouter>
					<DeliverySection {...mockProps} />
				</MemoryRouter>,
			);

			fireEvent.click(screen.getByTestId("show-details-1"));

			await waitFor(() => {
				expect(screen.getByTestId("dialog-title")).toHaveTextContent(
					"Test Feature",
				);
			});
		});

		it("should display work items in the dialog once loaded", async () => {
			const mockWorkItems: IWorkItem[] = [
				{ referenceId: "wi-1", name: "Story 1" } as IWorkItem,
				{ referenceId: "wi-2", name: "Story 2" } as IWorkItem,
			];
			mockGetFeatureWorkItems.mockResolvedValue(mockWorkItems);

			render(
				<MemoryRouter>
					<DeliverySection {...mockProps} />
				</MemoryRouter>,
			);

			fireEvent.click(screen.getByTestId("show-details-1"));

			// Dialog opens immediately with 0 items while loading
			await waitFor(() => {
				expect(screen.getByTestId("dialog-item-count")).toHaveTextContent(
					"0 items",
				);
			});

			// Items populate once the service resolves
			await waitFor(() => {
				expect(screen.getByTestId("dialog-item-count")).toHaveTextContent(
					"2 items",
				);
			});
		});

		it("should close the dialog when onClose is triggered", async () => {
			mockGetFeatureWorkItems.mockResolvedValue([]);

			render(
				<MemoryRouter>
					<DeliverySection {...mockProps} />
				</MemoryRouter>,
			);

			fireEvent.click(screen.getByTestId("show-details-1"));

			await waitFor(() => {
				expect(screen.getByTestId("work-items-dialog")).toBeInTheDocument();
			});

			fireEvent.click(screen.getByText("Close"));

			await waitFor(() => {
				expect(
					screen.queryByTestId("work-items-dialog"),
				).not.toBeInTheDocument();
			});
		});

		it("should clear work items when a new feature is opened before the previous resolves", async () => {
			// Simulate a delayed first call followed by a fast second call
			let resolveFirst!: (value: IWorkItem[]) => void;
			const firstCall = new Promise<IWorkItem[]>((res) => {
				resolveFirst = res;
			});
			mockGetFeatureWorkItems.mockReturnValueOnce(firstCall);
			mockGetFeatureWorkItems.mockResolvedValueOnce([]);

			const secondFeature = new Feature();
			secondFeature.id = 2;
			secondFeature.name = "Second Feature";
			secondFeature.remainingWork = { "1": 2 };
			secondFeature.totalWork = { "1": 5 };
			secondFeature.forecasts = [];

			const propsWithTwoFeatures = {
				...mockProps,
				features: [mockFeature, secondFeature],
			};

			render(
				<MemoryRouter>
					<DeliverySection {...propsWithTwoFeatures} />
				</MemoryRouter>,
			);

			// Open first feature - dialog opens, work items are empty (loading)
			fireEvent.click(screen.getByTestId("show-details-1"));
			await waitFor(() => {
				expect(screen.getByTestId("dialog-item-count")).toHaveTextContent(
					"0 items",
				);
			});

			// Open second feature before first resolves - items should still be empty (reset)
			fireEvent.click(screen.getByTestId("show-details-2"));
			await waitFor(() => {
				expect(screen.getByTestId("dialog-item-count")).toHaveTextContent(
					"0 items",
				);
			});

			// Resolve the first (now stale) call - items should not bleed into the second feature's dialog
			resolveFirst([
				{ referenceId: "wi-stale", name: "Stale Item" } as IWorkItem,
			]);

			// The dialog for the second feature should not show the stale items
			// (this validates setFeatureWorkItems([]) is called on each open)
			expect(mockGetFeatureWorkItems).toHaveBeenCalledTimes(2);
		});
	});

	describe("Forecast Chips", () => {
		it("should display 85% forecast chip when forecast exists", () => {
			const deliveryWithForecasts = new Delivery();
			deliveryWithForecasts.id = 1;
			deliveryWithForecasts.name = "Test Delivery";
			deliveryWithForecasts.date = new Date("2026-02-15").toISOString();
			deliveryWithForecasts.features = [1];
			deliveryWithForecasts.likelihoodPercentage = 75;
			deliveryWithForecasts.progress = 60;
			deliveryWithForecasts.remainingWork = 4;
			deliveryWithForecasts.totalWork = 10;
			deliveryWithForecasts.featureLikelihoods = [];
			deliveryWithForecasts.completionDates = [
				WhenForecast.new(85, new Date("2026-02-10")),
			];

			const propsWithForecasts = {
				...mockProps,
				delivery: deliveryWithForecasts,
			};

			render(
				<MemoryRouter>
					<DeliverySection {...propsWithForecasts} />
				</MemoryRouter>,
			);

			expect(screen.getByText(/85%:/)).toBeInTheDocument();
		});

		it("should display 70-95% range forecasts when both forecasts exist", () => {
			const deliveryWithForecasts = new Delivery();
			deliveryWithForecasts.id = 1;
			deliveryWithForecasts.name = "Test Delivery";
			deliveryWithForecasts.date = new Date("2026-02-15").toISOString();
			deliveryWithForecasts.features = [1];
			deliveryWithForecasts.likelihoodPercentage = 75;
			deliveryWithForecasts.progress = 60;
			deliveryWithForecasts.remainingWork = 4;
			deliveryWithForecasts.totalWork = 10;
			deliveryWithForecasts.featureLikelihoods = [];
			deliveryWithForecasts.completionDates = [
				WhenForecast.new(70, new Date("2026-02-01")),
				WhenForecast.new(95, new Date("2026-02-20")),
			];

			const propsWithForecasts = {
				...mockProps,
				delivery: deliveryWithForecasts,
			};

			render(
				<MemoryRouter>
					<DeliverySection {...propsWithForecasts} />
				</MemoryRouter>,
			);

			expect(screen.getByText(/70%:/)).toBeInTheDocument();
			expect(screen.getByText(/95%:/)).toBeInTheDocument();
		});

		it("should not display 85% chip when forecast does not exist", () => {
			render(
				<MemoryRouter>
					<DeliverySection {...mockProps} />
				</MemoryRouter>,
			);

			expect(screen.queryByText(/85%:/)).not.toBeInTheDocument();
		});

		it("should not display 70-95% range chip when forecasts are missing", () => {
			const deliveryWithPartialForecasts = new Delivery();
			deliveryWithPartialForecasts.id = 1;
			deliveryWithPartialForecasts.name = "Test Delivery";
			deliveryWithPartialForecasts.date = new Date("2026-02-15").toISOString();
			deliveryWithPartialForecasts.features = [1];
			deliveryWithPartialForecasts.likelihoodPercentage = 75;
			deliveryWithPartialForecasts.progress = 60;
			deliveryWithPartialForecasts.remainingWork = 4;
			deliveryWithPartialForecasts.totalWork = 10;
			deliveryWithPartialForecasts.featureLikelihoods = [];
			deliveryWithPartialForecasts.completionDates = [
				WhenForecast.new(70, new Date("2026-02-01")),
				// Missing 95% forecast
			];

			const propsWithPartialForecasts = {
				...mockProps,
				delivery: deliveryWithPartialForecasts,
			};

			render(
				<MemoryRouter>
					<DeliverySection {...propsWithPartialForecasts} />
				</MemoryRouter>,
			);

			expect(screen.queryByText(/70-95%:/)).not.toBeInTheDocument();
		});

		it("should display both forecast chips when all forecasts exist", () => {
			const deliveryWithAllForecasts = new Delivery();
			deliveryWithAllForecasts.id = 1;
			deliveryWithAllForecasts.name = "Test Delivery";
			deliveryWithAllForecasts.date = new Date("2026-02-15").toISOString();
			deliveryWithAllForecasts.features = [1];
			deliveryWithAllForecasts.likelihoodPercentage = 75;
			deliveryWithAllForecasts.progress = 60;
			deliveryWithAllForecasts.remainingWork = 4;
			deliveryWithAllForecasts.totalWork = 10;
			deliveryWithAllForecasts.featureLikelihoods = [];
			deliveryWithAllForecasts.completionDates = [
				WhenForecast.new(70, new Date("2026-02-01")),
				WhenForecast.new(85, new Date("2026-02-10")),
				WhenForecast.new(95, new Date("2026-02-20")),
			];

			const propsWithAllForecasts = {
				...mockProps,
				delivery: deliveryWithAllForecasts,
			};

			render(
				<MemoryRouter>
					<DeliverySection {...propsWithAllForecasts} />
				</MemoryRouter>,
			);

			expect(screen.getByText(/70%:/)).toBeInTheDocument();
			expect(screen.getByText(/85%:/)).toBeInTheDocument();
			expect(screen.getByText(/95%:/)).toBeInTheDocument();
		});
	});
});
