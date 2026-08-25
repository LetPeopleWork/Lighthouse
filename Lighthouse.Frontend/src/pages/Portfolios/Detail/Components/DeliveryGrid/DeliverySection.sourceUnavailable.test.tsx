import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import { Delivery } from "../../../../../models/Delivery";
import type { DeliverySourceUnavailableReason } from "../../../../../models/Delivery/DeliverySource";
import type { IEntityReference } from "../../../../../models/EntityReference";
import { Feature } from "../../../../../models/Feature";
import type { IWorkItem } from "../../../../../models/WorkItem";
import { DeliverySelectionMode } from "../../../../../models/WorkItemRules";
import DeliverySection from "./DeliverySection";

/**
 * Where the broken-source notice appears, and where it must not. It sits outside the Accordion on
 * purpose: reading it must never collapse the Delivery it is about.
 */

vi.mock("../../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({ getTerm: (key: string) => key }),
}));

vi.mock("../../../../../services/Api/ApiServiceContext", () => ({
	ApiServiceContext: {
		_currentValue: { featureService: { getFeatureWorkItems: vi.fn() } },
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

const teams: IEntityReference[] = [{ id: 1, name: "Team Alpha" }];

function deliveryWith(
	mode: DeliverySelectionMode,
	reason: DeliverySourceUnavailableReason | null,
): Delivery {
	const delivery = new Delivery();
	delivery.id = 1;
	delivery.name = "Release 3.0";
	delivery.date = "2026-12-19T00:00:00.000Z";
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
	delivery.sourceKey = "jira-release";
	delivery.sourceReference = "10007";
	delivery.sourceLastSyncedOn = "2026-08-20T00:00:00.000Z";
	delivery.sourceUnavailableReason = reason;
	delivery.isOverdue = false;

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

function renderSection(delivery: Delivery, onUnbind?: () => void) {
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
				onUnbind={onUnbind}
			/>
		</MemoryRouter>,
	);
}

describe("DeliverySection broken-source notice (US-04)", () => {
	it("shows the notice on a Delivery whose source is finished", () => {
		renderSection(
			deliveryWith(DeliverySelectionMode.SourceBound, "SourceNotFound"),
		);

		expect(screen.getByText("Source unavailable")).toBeInTheDocument();
	});

	it("shows nothing on a Delivery whose source is answering", () => {
		renderSection(deliveryWith(DeliverySelectionMode.SourceBound, null));

		expect(screen.queryByText("Source unavailable")).not.toBeInTheDocument();
	});

	// A Delivery somebody released keeps its values and stops carrying anything about the source. If a
	// stale reason ever reached the row, saying a hand-maintained Delivery has a broken source would be
	// worse than saying nothing.
	it("shows nothing on a Delivery that follows nothing, whatever the row still carries", () => {
		renderSection(deliveryWith(DeliverySelectionMode.Manual, "SourceNotFound"));

		expect(screen.queryByText("Source unavailable")).not.toBeInTheDocument();
	});

	it("keeps the Delivery's own values on screen beside the notice", () => {
		renderSection(
			deliveryWith(DeliverySelectionMode.SourceBound, "SourceNotFound"),
		);

		expect(screen.getByText("Release 3.0")).toBeInTheDocument();
	});

	it("asks before it stops following, rather than doing it on one click", async () => {
		renderSection(
			deliveryWith(DeliverySelectionMode.SourceBound, "SourceNotFound"),
			vi.fn(),
		);

		await userEvent.click(
			screen.getByRole("button", { name: "Stop following" }),
		);

		expect(screen.getByRole("dialog")).toBeInTheDocument();
	});

	// Two different reasons the way out is not on offer, and they are not the same test. A missing
	// handler is the screen not wiring one up; a reader who may not edit is the product refusing them.
	// Only the second says anything about permissions, and it is the one that was missing.
	it("offers nothing to press when the screen wired up no way out", () => {
		renderSection(
			deliveryWith(DeliverySelectionMode.SourceBound, "SourceNotFound"),
			undefined,
		);

		expect(
			screen.queryByRole("button", { name: "Stop following" }),
		).not.toBeInTheDocument();
	});

	it("still says what is wrong to a reader who may not edit, and offers them nothing to press", () => {
		render(
			<MemoryRouter>
				<DeliverySection
					delivery={deliveryWith(
						DeliverySelectionMode.SourceBound,
						"SourceNotFound",
					)}
					features={[featureNamed(1, "Checkout")]}
					isExpanded={false}
					isLoadingFeatures={false}
					onToggleExpanded={vi.fn()}
					onDelete={vi.fn()}
					onEdit={vi.fn()}
					teams={teams}
					deliverySources={[
						{ key: "jira-release", displayName: "Jira Release" },
					]}
					onUnbind={vi.fn()}
					canEdit={false}
				/>
			</MemoryRouter>,
		);

		expect(screen.getByText("Source unavailable")).toBeInTheDocument();
		expect(
			screen.queryByRole("button", { name: "Stop following" }),
		).not.toBeInTheDocument();
	});
});
