import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { Delivery } from "../../../../../models/Delivery";
import type { DeliveryMetricsHistory } from "../../../../../models/Delivery/DeliveryMetricsHistory";
import type { IEntityReference } from "../../../../../models/EntityReference";
import { Feature } from "../../../../../models/Feature";
import DeliverySection from "./DeliverySection";

const {
	mockGetMetricsHistory,
	mockUseLicenseRestrictions,
	mockBurnupChart,
	mockPredictabilityChart,
	mockFeverChart,
} = vi.hoisted(() => ({
	mockGetMetricsHistory: vi.fn(),
	mockUseLicenseRestrictions: vi.fn(),
	mockBurnupChart: vi.fn((_props: { history: unknown }) => (
		<div data-testid="burnup-chart" />
	)),
	mockPredictabilityChart: vi.fn((_props: { history: unknown }) => (
		<div data-testid="predictability-chart" />
	)),
	mockFeverChart: vi.fn((_props: { history: unknown }) => (
		<div data-testid="fever-chart" />
	)),
}));

vi.mock("../../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => (key === "workItems" ? "Work Items" : key),
	}),
}));

vi.mock("../../../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: mockUseLicenseRestrictions,
}));

vi.mock("../../../../../services/Api/ApiServiceContext", () => ({
	ApiServiceContext: {
		_currentValue: {
			featureService: { getFeatureWorkItems: vi.fn().mockResolvedValue([]) },
			deliveryService: { getMetricsHistory: mockGetMetricsHistory },
		},
	},
}));

vi.mock("../../../../../components/Common/Charts/DeliveryBurnupChart", () => ({
	default: (props: { history: DeliveryMetricsHistory }) =>
		mockBurnupChart(props),
}));

vi.mock(
	"../../../../../components/Common/Charts/DeliveryPredictabilityChart",
	() => ({
		default: (props: { history: DeliveryMetricsHistory }) =>
			mockPredictabilityChart(props),
	}),
);

vi.mock("../../../../../components/Common/Charts/DeliveryFeverChart", () => ({
	default: (props: { history: DeliveryMetricsHistory }) =>
		mockFeverChart(props),
}));

const getHistory = (): DeliveryMetricsHistory => ({
	deliveryDate: new Date("2026-06-10T00:00:00Z"),
	firstSnapshotDate: new Date("2026-06-01T00:00:00Z"),
	points: [
		{
			date: new Date("2026-06-01T00:00:00Z"),
			totalWork: 20,
			doneWork: 4,
			remainingWork: 16,
			estimatedItemCount: null,
			forecastHowMany: null,
			likelihoodPercentage: null,
			whenDistribution: null,
			featureBreakdown: [],
		},
	],
});

const getMockDelivery = (): Delivery => {
	const delivery = new Delivery();
	delivery.id = 7;
	delivery.name = "Test Delivery";
	delivery.date = new Date("2026-06-10").toISOString();
	delivery.features = [1];
	delivery.likelihoodPercentage = 75;
	delivery.progress = 60;
	delivery.remainingWork = 4;
	delivery.totalWork = 10;
	delivery.featureLikelihoods = [];
	delivery.completionDates = [];
	return delivery;
};

const getMockFeature = (): Feature => {
	const feature = new Feature();
	feature.id = 1;
	feature.name = "Test Feature";
	feature.remainingWork = { "1": 5 };
	feature.totalWork = { "1": 10 };
	feature.forecasts = [];
	return feature;
};

const mockTeams: IEntityReference[] = [{ id: 1, name: "Team Alpha" }];

const renderSection = () =>
	render(
		<MemoryRouter>
			<DeliverySection
				delivery={getMockDelivery()}
				features={[getMockFeature()]}
				isExpanded={true}
				isLoadingFeatures={false}
				onToggleExpanded={vi.fn()}
				onDelete={vi.fn()}
				onEdit={vi.fn()}
				teams={mockTeams}
			/>
		</MemoryRouter>,
	);

const setPremium = (canUsePremiumFeatures: boolean) => {
	mockUseLicenseRestrictions.mockReturnValue({
		licenseStatus: { canUsePremiumFeatures },
	});
};

describe("DeliverySection Metrics tab", () => {
	beforeEach(() => {
		mockGetMetricsHistory.mockReset();
		mockBurnupChart.mockClear();
		mockPredictabilityChart.mockClear();
		mockFeverChart.mockClear();
		mockGetMetricsHistory.mockResolvedValue(getHistory());
	});

	it("offers the Metrics tab to community users without a premium license", () => {
		setPremium(false);
		renderSection();
		expect(screen.getByRole("tab", { name: "Metrics" })).toBeInTheDocument();
	});

	it("renders the burnup chart for community users without a premium license", async () => {
		setPremium(false);
		renderSection();

		fireEvent.click(screen.getByRole("tab", { name: "Metrics" }));

		await waitFor(() => {
			expect(screen.getByTestId("burnup-chart")).toBeInTheDocument();
		});
	});

	it("does not fetch metrics history while the Work Items tab is active", () => {
		setPremium(true);
		renderSection();
		expect(mockGetMetricsHistory).not.toHaveBeenCalled();
	});

	it("does not render the burnup chart while the Work Items tab is the active one", () => {
		setPremium(true);
		renderSection();

		expect(screen.getByRole("tab", { name: "Work Items" })).toHaveAttribute(
			"aria-selected",
			"true",
		);
		expect(screen.queryByTestId("burnup-chart")).not.toBeInTheDocument();
	});

	it("removes the burnup chart again when returning to the Work Items tab", async () => {
		setPremium(true);
		renderSection();

		fireEvent.click(screen.getByRole("tab", { name: "Metrics" }));
		await waitFor(() => {
			expect(screen.getByTestId("burnup-chart")).toBeInTheDocument();
		});

		fireEvent.click(screen.getByRole("tab", { name: "Work Items" }));

		expect(screen.queryByTestId("burnup-chart")).not.toBeInTheDocument();
	});

	it("does not fetch metrics when the active tab is switched to a non-Metrics tab", () => {
		setPremium(true);
		renderSection();

		fireEvent.click(screen.getByRole("tab", { name: "Work Items" }));

		expect(screen.getByRole("tab", { name: "Work Items" })).toHaveAttribute(
			"aria-selected",
			"true",
		);
		expect(mockGetMetricsHistory).not.toHaveBeenCalled();
	});

	it("lazily fetches metrics history for the delivery on the first Metrics-tab open", async () => {
		setPremium(true);
		renderSection();

		fireEvent.click(screen.getByRole("tab", { name: "Metrics" }));

		await waitFor(() => {
			expect(mockGetMetricsHistory).toHaveBeenCalledWith(7);
		});
		expect(mockGetMetricsHistory).toHaveBeenCalledTimes(1);
	});

	it("renders the burnup chart with the fetched history once Metrics is opened", async () => {
		setPremium(true);
		renderSection();

		fireEvent.click(screen.getByRole("tab", { name: "Metrics" }));

		await waitFor(() => {
			expect(screen.getByTestId("burnup-chart")).toBeInTheDocument();
		});
		expect(mockBurnupChart).toHaveBeenCalledWith(
			expect.objectContaining({ history: getHistory() }),
		);
	});

	it("renders the burnup, predictability and fever charts together from the same fetched history", async () => {
		setPremium(false);
		renderSection();

		fireEvent.click(screen.getByRole("tab", { name: "Metrics" }));

		await waitFor(() => {
			expect(screen.getByTestId("burnup-chart")).toBeInTheDocument();
		});
		expect(screen.getByTestId("predictability-chart")).toBeInTheDocument();
		expect(screen.getByTestId("fever-chart")).toBeInTheDocument();
		expect(mockGetMetricsHistory).toHaveBeenCalledTimes(1);
		expect(mockPredictabilityChart).toHaveBeenCalledWith(
			expect.objectContaining({ history: getHistory() }),
		);
		expect(mockFeverChart).toHaveBeenCalledWith(
			expect.objectContaining({ history: getHistory() }),
		);
	});

	it("shows a loading placeholder while the metrics history is still in flight", async () => {
		setPremium(true);
		let resolveFetch!: (value: DeliveryMetricsHistory) => void;
		mockGetMetricsHistory.mockReturnValue(
			new Promise<DeliveryMetricsHistory>((resolve) => {
				resolveFetch = resolve;
			}),
		);
		renderSection();

		fireEvent.click(screen.getByRole("tab", { name: "Metrics" }));

		expect(screen.getByText("Loading metrics...")).toBeInTheDocument();
		expect(screen.queryByTestId("burnup-chart")).not.toBeInTheDocument();

		resolveFetch(getHistory());
		await waitFor(() => {
			expect(screen.getByTestId("burnup-chart")).toBeInTheDocument();
		});
	});

	it("fetches metrics history only once across repeated tab switches", async () => {
		setPremium(true);
		renderSection();

		fireEvent.click(screen.getByRole("tab", { name: "Metrics" }));
		await waitFor(() => {
			expect(screen.getByTestId("burnup-chart")).toBeInTheDocument();
		});

		fireEvent.click(screen.getByRole("tab", { name: "Work Items" }));
		fireEvent.click(screen.getByRole("tab", { name: "Metrics" }));

		await waitFor(() => {
			expect(screen.getByTestId("burnup-chart")).toBeInTheDocument();
		});
		expect(mockGetMetricsHistory).toHaveBeenCalledTimes(1);
	});
});
