import { render, screen } from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import { Delivery, type IDelivery } from "../../../../../models/Delivery";
import { DeliverySelectionMode } from "../../../../../models/WorkItemRules";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";
import { createMockApiServiceContext } from "../../../../../tests/MockApiServiceProvider";
import DeliverySection from "./DeliverySection";

const makeDelivery = (overrides: Partial<IDelivery> = {}): Delivery =>
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
		...overrides,
	} as IDelivery);

const renderSection = (delivery: Delivery) =>
	render(
		<MemoryRouter>
			<ApiServiceContext.Provider value={createMockApiServiceContext({})}>
				<DeliverySection
					delivery={delivery}
					features={[]}
					isExpanded={true}
					isLoadingFeatures={false}
					onToggleExpanded={vi.fn()}
					onDelete={vi.fn()}
					onEdit={vi.fn()}
					teams={[]}
					canEdit={true}
				/>
			</ApiServiceContext.Provider>
		</MemoryRouter>,
	);

describe("DeliverySection notes tab", () => {
	it("offers Notes beside Work Items and Metrics", () => {
		renderSection(makeDelivery());

		expect(screen.getByRole("tab", { name: "Notes" })).toBeInTheDocument();
	});

	it("keeps Notes reachable on a Delivery with no history at all", async () => {
		// Metrics needs accumulated days before it means anything. A note does not, so the two tabs
		// must not share a condition.
		renderSection(makeDelivery({ metricSnapshotCount: 0 }));

		const notesTab = screen.getByRole("tab", { name: "Notes" });
		expect(notesTab).not.toBeDisabled();

		await userEvent.click(notesTab);

		expect(
			await screen.findByTestId("delivery-notes-panel"),
		).toBeInTheDocument();
	});
});
