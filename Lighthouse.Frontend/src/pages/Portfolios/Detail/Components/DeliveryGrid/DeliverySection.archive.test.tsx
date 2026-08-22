import { render, screen } from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import { Delivery, type IDelivery } from "../../../../../models/Delivery";
import { DeliverySelectionMode } from "../../../../../models/WorkItemRules";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";
import { createMockApiServiceContext } from "../../../../../tests/MockApiServiceProvider";
import { PREMIUM_UPGRADE_TOOLTIP } from "../../../../../utils/premiumUpgradeTooltip";
import DeliverySection from "./DeliverySection";

const makeDelivery = (): Delivery =>
	Delivery.fromBackend({
		id: 42,
		name: "Q3 Platform",
		date: "2026-09-12T00:00:00",
		portfolioId: 7,
		features: [],
		likelihoodPercentage: 82,
		progress: 0.4,
		remainingWork: 48,
		totalWork: 120,
		featureLikelihoods: [],
		completionDates: [],
		selectionMode: DeliverySelectionMode.Manual,
		metricSnapshotCount: 0,
	} as IDelivery);

const renderSection = (props: {
	canEdit?: boolean;
	canArchive?: boolean;
	onArchive?: (delivery: Delivery) => void;
}) =>
	render(
		<MemoryRouter>
			<ApiServiceContext.Provider value={createMockApiServiceContext({})}>
				<DeliverySection
					delivery={makeDelivery()}
					features={[]}
					isExpanded={false}
					isLoadingFeatures={false}
					onToggleExpanded={vi.fn()}
					onDelete={vi.fn()}
					onEdit={vi.fn()}
					onArchive={props.onArchive ?? vi.fn()}
					teams={[]}
					canEdit={props.canEdit ?? true}
					canArchive={props.canArchive ?? true}
				/>
			</ApiServiceContext.Provider>
		</MemoryRouter>,
	);

describe("DeliverySection archive action", () => {
	it("offers Archive beside Edit and Delete to someone who may change the Portfolio", () => {
		renderSection({ canEdit: true });

		expect(screen.getByLabelText("archive")).toBeInTheDocument();
		expect(screen.getByLabelText("edit")).toBeInTheDocument();
		expect(screen.getByLabelText("delete")).toBeInTheDocument();
	});

	it("offers no Archive at all to a reader who may not change the Portfolio", () => {
		renderSection({ canEdit: false });

		expect(screen.queryByLabelText("archive")).not.toBeInTheDocument();
	});

	it("hands the Delivery over when Archive is chosen", async () => {
		const onArchive = vi.fn();
		renderSection({ onArchive });

		await userEvent.click(screen.getByLabelText("archive"));

		expect(onArchive).toHaveBeenCalledTimes(1);
		expect(onArchive.mock.calls[0][0].id).toBe(42);
	});

	it("shows Archive without a licence but will not run it, in the words the export actions use", async () => {
		const onArchive = vi.fn();
		renderSection({ canArchive: false, onArchive });

		const archiveButton = screen.getByLabelText("archive");

		expect(archiveButton).toBeDisabled();
		expect(archiveButton.parentElement).toHaveAttribute(
			"aria-label",
			PREMIUM_UPGRADE_TOOLTIP,
		);
		expect(PREMIUM_UPGRADE_TOOLTIP).toBe("Premium feature - Upgrade to use");
	});

	it("leaves Edit and Delete alone when there is no licence", () => {
		renderSection({ canArchive: false });

		expect(screen.getByLabelText("edit")).not.toBeDisabled();
		expect(screen.getByLabelText("delete")).not.toBeDisabled();
	});
});
