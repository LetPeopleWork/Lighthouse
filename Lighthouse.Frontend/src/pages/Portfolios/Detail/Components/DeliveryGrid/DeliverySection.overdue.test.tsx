import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { Delivery } from "../../../../../models/Delivery";
import type { IEntityReference } from "../../../../../models/EntityReference";
import { Feature } from "../../../../../models/Feature";
import type { IWorkItem } from "../../../../../models/WorkItem";
import { DeliverySelectionMode } from "../../../../../models/WorkItemRules";
import DeliverySection from "./DeliverySection";

/**
 * A source-bound Delivery takes whatever date its Release now holds, and a Release that slipped past
 * its own date is an ordinary state - so a target in the past arrives on this screen without anyone
 * having typed it. The word, not only the colour, is what says so: a reader who cannot tell the two
 * greys apart would otherwise see a date and no indication it has been and gone.
 */

vi.mock("../../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => key,
	}),
}));

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

const TODAY = new Date("2026-08-25T09:00:00.000Z");

const teams: IEntityReference[] = [{ id: 1, name: "Team Alpha" }];

function deliveryDated(date: string, mode = DeliverySelectionMode.Manual) {
	const delivery = new Delivery();
	delivery.id = 1;
	delivery.name = "Release 3.0";
	delivery.date = date;
	delivery.features = [1];
	delivery.likelihoodPercentage = 72;
	delivery.teamsWithoutForecast = [];
	delivery.progress = 40;
	delivery.remainingWork = 6;
	delivery.totalWork = 10;
	delivery.hasSufficientData = true;
	delivery.completionDates = [];
	delivery.featureLikelihoods = [
		{ featureId: 1, likelihoodPercentage: 72, hasSufficientData: true },
	];
	delivery.selectionMode = mode;

	return delivery;
}

function featureNamed(id: number, name: string): Feature {
	const feature = new Feature();
	feature.id = id;
	feature.name = name;
	feature.remainingWork = { "1": 3 };
	feature.totalWork = { "1": 5 };
	feature.forecasts = [];

	return feature;
}

function renderSection(delivery: Delivery) {
	return render(
		<MemoryRouter>
			<DeliverySection
				delivery={delivery}
				features={[featureNamed(1, "Checkout")]}
				isExpanded={false}
				isLoadingFeatures={false}
				onToggleExpanded={vi.fn()}
				onDelete={vi.fn()}
				onEdit={vi.fn()}
				teams={teams}
			/>
		</MemoryRouter>,
	);
}

beforeEach(() => {
	vi.useFakeTimers();
	vi.setSystemTime(TODAY);
});

afterEach(() => {
	vi.useRealTimers();
});

describe("DeliverySection overdue rendering (AC-03.2)", () => {
	it("says a target that has been and gone is overdue", () => {
		renderSection(deliveryDated("2026-08-24T00:00:00.000Z"));

		expect(screen.getByText("Overdue")).toBeInTheDocument();
	});

	it("says the same about a Delivery that took the past date from its source", () => {
		renderSection(
			deliveryDated(
				"2026-08-24T00:00:00.000Z",
				DeliverySelectionMode.SourceBound,
			),
		);

		expect(screen.getByText("Overdue")).toBeInTheDocument();
	});

	// The day is not over. Saying otherwise on the morning of the target date tells a forecaster they
	// have missed something they have not.
	it("says nothing about a target due today", () => {
		renderSection(deliveryDated("2026-08-25T00:00:00.000Z"));

		expect(screen.queryByText("Overdue")).not.toBeInTheDocument();
	});

	it("says nothing about a target still ahead", () => {
		renderSection(deliveryDated("2026-12-19T00:00:00.000Z"));

		expect(screen.queryByText("Overdue")).not.toBeInTheDocument();
	});

	// The word replaces nothing. A reader who is told a target has passed and not which one has to go
	// and look it up.
	it("still shows the date it is overdue against", () => {
		const theTargetThatPassed = "2026-08-24T00:00:00.000Z";
		renderSection(deliveryDated(theTargetThatPassed));

		const asItIsPrinted = new Date(theTargetThatPassed).toLocaleDateString(
			undefined,
			{ timeZone: "UTC" },
		);

		expect(
			screen.getByText(`delivery Date: ${asItIsPrinted}`),
		).toBeInTheDocument();
	});
});
