import { screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import { Delivery } from "../../../../../models/Delivery";
import { Feature } from "../../../../../models/Feature";
import { WhenForecast } from "../../../../../models/Forecasts/WhenForecast";
import type { IWorkItem } from "../../../../../models/WorkItem";
import { renderWithQueryClient } from "../../../../../tests/renderWithQueryClient";
import DeliverySection from "./DeliverySection";

vi.mock("../../../../../services/Api/ApiServiceContext", () => ({
	ApiServiceContext: {
		_currentValue: {
			featureService: {
				getFeatureWorkItems: vi.fn(),
			},
		},
	},
}));

vi.mock(
	"../../../../../components/Common/FeatureListDataGrid/FeatureProgressIndicator",
	() => ({
		default: ({ feature }: { feature: { id: number } }) => (
			<span data-testid={`progress-${feature.id}`} />
		),
	}),
);

vi.mock(
	"../../../../../components/Common/WorkItemsDialog/WorkItemsDialog",
	() => ({
		default: ({ open }: { items: IWorkItem[]; open: boolean }) =>
			open ? <div data-testid="work-items-dialog" /> : null,
	}),
);

const percentile = (probability: number, beyondHorizon: boolean) => {
	const forecast = WhenForecast.new(
		probability,
		beyondHorizon
			? new Date("2031-10-18T00:00:00Z")
			: new Date("2029-03-14T00:00:00Z"),
	);
	forecast.beyondHorizon = beyondHorizon;
	forecast.horizonYears = 5;
	return forecast;
};

const aDeliveryWhereSixInTenRunsFinish = (): Delivery =>
	Object.assign(new Delivery(), {
		id: 1,
		name: "Q4 Billing",
		date: new Date("2026-12-15").toISOString(),
		features: [1],
		likelihoodPercentage: 4,
		teamsWithoutForecast: [],
		progress: 4,
		remainingWork: 480,
		totalWork: 500,
		hasSufficientData: true,
		completionDates: [
			percentile(50, false),
			percentile(70, true),
			percentile(85, true),
			percentile(95, true),
		],
		featureLikelihoods: [
			{ featureId: 1, likelihoodPercentage: 4, hasSufficientData: true },
		],
	});

const billingMigration = (): Feature =>
	Object.assign(new Feature(), {
		id: 1,
		name: "Billing Migration",
		remainingWork: { "1": 480 },
		totalWork: { "1": 500 },
		forecasts: [],
	});

const renderSection = () =>
	renderWithQueryClient(
		<MemoryRouter>
			<DeliverySection
				delivery={aDeliveryWhereSixInTenRunsFinish()}
				features={[billingMigration()]}
				isExpanded={true}
				isLoadingFeatures={false}
				onToggleExpanded={vi.fn()}
				onDelete={vi.fn()}
				onEdit={vi.fn()}
				teams={[{ id: 1, name: "Payments" }]}
			/>
		</MemoryRouter>,
	);

describe("the Delivery's forecast chips when percentiles are beyond the horizon", () => {
	it(`read "85%: More than 5 years"`, () => {
		renderSection();

		expect(screen.getByText(/^85%: More than 5 years$/)).toBeInTheDocument();
		expect(screen.getByText(/^70%: More than 5 years$/)).toBeInTheDocument();
		expect(screen.getByText(/^95%: More than 5 years$/)).toBeInTheDocument();
	});

	it(`never show the stand-in date`, () => {
		renderSection();

		expect(screen.getAllByText(/^\d+%: /)).toHaveLength(4);
		expect(screen.queryAllByText(/^\d+%: .*2031/)).toHaveLength(0);
	});

	/** Already true; the percentile that has a day keeps its date. */
	it("keep the date of the percentile that has a day", () => {
		renderSection();

		expect(screen.getByText(/^50%: /)).toBeInTheDocument();
		expect(screen.queryByText(/^50%: More than/)).not.toBeInTheDocument();
	});
});
