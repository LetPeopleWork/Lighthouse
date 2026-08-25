import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import { Delivery } from "../../../../../models/Delivery";
import type { IEntityReference } from "../../../../../models/EntityReference";
import { Feature } from "../../../../../models/Feature";
import type { IWorkItem } from "../../../../../models/WorkItem";
import { DeliverySelectionMode } from "../../../../../models/WorkItemRules";
import DeliverySection from "./DeliverySection";

/**
 * A source-bound Delivery takes whatever date its Release now holds, and a Release that slipped past
 * its own date is an ordinary state - so a target already behind us arrives on this screen without
 * anyone having typed it.
 *
 * Whether it has passed is the backend's answer, not this component's: "today" is the instance's day
 * and the browser may be on the other side of midnight from it. So these render the flag as it comes
 * off the wire and say nothing about how it was decided.
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

const THE_DATE_ON_SCREEN = "2026-08-24T00:00:00.000Z";

const teams: IEntityReference[] = [{ id: 1, name: "Team Alpha" }];

function deliveryThatIs(
	overdue: boolean,
	mode = DeliverySelectionMode.Manual,
): Delivery {
	const delivery = new Delivery();
	delivery.id = 1;
	delivery.name = "Release 3.0";
	delivery.date = THE_DATE_ON_SCREEN;
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
	delivery.isOverdue = overdue;

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
				deliverySources={[{ key: "jira-release", displayName: "Jira Release" }]}
			/>
		</MemoryRouter>,
	);
}

function theDateLine() {
	return screen.getByText(
		`delivery Date: ${new Date(THE_DATE_ON_SCREEN).toLocaleDateString(undefined, { timeZone: "UTC" })}`,
	);
}

describe("DeliverySection overdue rendering (AC-03.2)", () => {
	it("says a target that has been and gone is overdue", () => {
		renderSection(deliveryThatIs(true));

		expect(screen.getByText("Overdue")).toBeInTheDocument();
	});

	it("says nothing about a target that has not", () => {
		renderSection(deliveryThatIs(false));

		expect(screen.queryByText("Overdue")).not.toBeInTheDocument();
	});

	// Red as well as worded, so it is not the word alone doing the work for a reader scanning a long
	// list. Asserted on the chip because the chip's colour lands as a class this environment can see -
	// the same colour set on the text beside it does not, which is why the text is left alone.
	it("draws the word in the colour trouble is drawn in", () => {
		renderSection(deliveryThatIs(true));

		expect(screen.getByText("Overdue").closest(".MuiChip-root")).toHaveClass(
			"MuiChip-colorError",
		);
	});

	// The word replaces nothing. A reader told a target has passed and not which one has to go and
	// look it up.
	it("still shows the date it is overdue against", () => {
		renderSection(deliveryThatIs(true));

		expect(theDateLine()).toBeInTheDocument();
	});

	it("explains the word to a pointer", () => {
		renderSection(deliveryThatIs(true));

		expect(
			screen.getByText("Overdue").closest(".MuiChip-root"),
		).toHaveAttribute("title", "The target date has passed.");
	});

	// The Delivery that reaches this state without anyone typing the date is the bound one, and it
	// already carries a marker saying it follows a Release. Both have to be readable at once.
	it("says it alongside the marker on a Delivery that follows a Release", () => {
		renderSection(deliveryThatIs(true, DeliverySelectionMode.SourceBound));

		expect(screen.getByText("Overdue")).toBeInTheDocument();
		expect(screen.getByLabelText(/Bound to|Jira Release/i)).toBeInTheDocument();
	});
});
