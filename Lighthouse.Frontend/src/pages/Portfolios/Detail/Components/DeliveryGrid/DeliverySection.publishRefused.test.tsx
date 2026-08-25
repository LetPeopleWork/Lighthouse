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
 * Where the refused-publish notice appears, and how it sits beside the broken-source one. The two are
 * separate on purpose and can both be true at once: one says nothing is maintaining the values on the
 * row, the other says the row is current and a write elsewhere did not happen. Collapsing them would
 * have a healthy Delivery read as a broken one.
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
const WHAT_JIRA_SAID =
	"You must have global or project administrator rights in order to modify versions.";

function aBroadcastingDelivery(overrides: Partial<Delivery> = {}): Delivery {
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
	delivery.selectionMode = DeliverySelectionMode.SourceBound;
	delivery.sourceKey = "jira-release";
	delivery.sourceReference = "10007";
	delivery.sourceLastSyncedOn = "2026-08-20T00:00:00.000Z";
	delivery.sourceUnavailableReason = null;
	delivery.publishForecastToSource = true;
	delivery.lastPublishRefusedOn = null;
	delivery.lastPublishRefusalReason = null;
	delivery.isOverdue = false;

	return Object.assign(delivery, overrides);
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

describe("DeliverySection refused-publish notice (US-06)", () => {
	it("shows the notice on a Delivery whose forecast was refused", () => {
		renderSection(
			aBroadcastingDelivery({
				lastPublishRefusalReason: WHAT_JIRA_SAID,
				lastPublishRefusedOn: "2026-08-25T00:00:00.000Z",
			}),
		);

		expect(screen.getByText("Forecast not published")).toBeInTheDocument();
	});

	it("shows nothing on a Delivery nothing has refused", () => {
		renderSection(aBroadcastingDelivery());

		expect(
			screen.queryByText("Forecast not published"),
		).not.toBeInTheDocument();
	});

	/**
	 * AC-06.4, on screen. Reading Releases and writing to them are separate capabilities, so a refused
	 * write must not make a Delivery that is syncing perfectly well look like one whose source is gone.
	 */
	it("does not make a healthy source look broken", () => {
		renderSection(
			aBroadcastingDelivery({ lastPublishRefusalReason: WHAT_JIRA_SAID }),
		);

		expect(screen.queryByText("Source unavailable")).not.toBeInTheDocument();
	});

	/**
	 * Both at once is a real state: a Release that was deleted after refusing a write. Each notice
	 * sends the reader somewhere different, so neither may swallow the other.
	 */
	it("stands beside the broken-source notice rather than replacing it", () => {
		renderSection(
			aBroadcastingDelivery({
				sourceUnavailableReason: "SourceNotFound",
				lastPublishRefusalReason: WHAT_JIRA_SAID,
			}),
		);

		expect(screen.getByText("Source unavailable")).toBeInTheDocument();
		expect(screen.getByText("Forecast not published")).toBeInTheDocument();
	});

	it("keeps the Delivery's own values on screen beside the notice", () => {
		renderSection(
			aBroadcastingDelivery({ lastPublishRefusalReason: WHAT_JIRA_SAID }),
		);

		expect(screen.getByText("Release 3.0")).toBeInTheDocument();
	});
});
