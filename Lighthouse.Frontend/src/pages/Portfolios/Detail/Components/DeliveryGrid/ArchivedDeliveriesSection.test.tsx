import { render, screen } from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ArchivedDelivery } from "../../../../../models/Delivery/ArchivedDelivery";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";
import { makeArchivedDelivery } from "../../../../../tests/ArchivedDeliveryFixture";
import {
	createMockApiServiceContext,
	createMockDeliveryService,
} from "../../../../../tests/MockApiServiceProvider";
import ArchivedDeliveriesSection from "./ArchivedDeliveriesSection";

const { mockUseLicenseRestrictions } = vi.hoisted(() => ({
	mockUseLicenseRestrictions: vi.fn(),
}));

vi.mock("../../../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: mockUseLicenseRestrictions,
}));

vi.mock("../../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) =>
			({
				delivery: "Delivery",
				deliveries: "Deliveries",
				feature: "Feature",
				features: "Features",
				workItems: "Work Items",
			})[key] ?? key,
	}),
}));

const makeArchived = makeArchivedDelivery;

const renderSection = (props?: {
	archived?: ArchivedDelivery[];
	canEdit?: boolean;
	onDelete?: (delivery: ArchivedDelivery) => void;
	onUnarchive?: (delivery: ArchivedDelivery) => void;
	canUsePremiumFeatures?: boolean;
}) => {
	mockUseLicenseRestrictions.mockReturnValue({
		licenseStatus: {
			canUsePremiumFeatures: props?.canUsePremiumFeatures ?? true,
		},
		isLoading: false,
	});

	const deliveryService = createMockDeliveryService();
	const context = createMockApiServiceContext({ deliveryService });

	render(
		<ApiServiceContext.Provider value={context}>
			<ArchivedDeliveriesSection
				archivedDeliveries={props?.archived ?? [makeArchived()]}
				canEdit={props?.canEdit ?? true}
				onDelete={props?.onDelete ?? vi.fn()}
				onUnarchive={props?.onUnarchive ?? vi.fn()}
			/>
		</ApiServiceContext.Provider>,
	);

	return { deliveryService };
};

const expand = async () =>
	userEvent.click(screen.getByRole("button", { name: /Archived Deliveries/ }));

describe("ArchivedDeliveriesSection", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		localStorage.clear();
	});

	it("says nothing at all when a Portfolio has retired nothing", () => {
		renderSection({ archived: [] });

		expect(screen.queryByText(/Archived/)).not.toBeInTheDocument();
	});

	it("is folded away until someone opens it", () => {
		renderSection();

		expect(
			screen.getByRole("button", { name: /Archived Deliveries/ }),
		).toHaveAttribute("aria-expanded", "false");
	});

	it("shows the name, the date and the numbers that were written down once opened", async () => {
		renderSection();

		await expand();

		expect(await screen.findByText("Autumn Launch")).toBeInTheDocument();
		expect(
			screen.getByText(
				new RegExp(
					`Delivery Date: ${makeArchived().getFormattedDate().replace(/\//g, "\\/")}`,
				),
			),
		).toBeInTheDocument();
		expect(screen.getByText(/80% \(40\/50\)/)).toBeInTheDocument();
		expect(screen.getByText(/All Features by .*: 64%/)).toBeInTheDocument();
	});

	it("marks the retired Delivery as archived, and says on which day", async () => {
		renderSection();

		await expand();

		expect(await screen.findByTestId("archived-marker")).toHaveTextContent(
			`Archived: ${makeArchived().getFormattedArchivedOn()}`,
		);
	});

	it("still says why a Delivery that closed without a forecast has no number", async () => {
		renderSection({
			archived: [
				makeArchived({
					likelihoodPercentage: null,
					teamsWithoutForecast: ["Team Alpha"],
				}),
			],
		});

		await expand();

		expect(screen.getByText("Cannot forecast")).toBeInTheDocument();
	});

	it("keeps Delete available on a retired Delivery", async () => {
		const onDelete = vi.fn();
		renderSection({ onDelete });

		await expand();
		await userEvent.click(screen.getByLabelText("delete"));

		expect(onDelete).toHaveBeenCalledTimes(1);
		expect(onDelete.mock.calls[0][0].id).toBe(9);
	});

	it("offers no Delete to a reader who may not change the Portfolio", async () => {
		renderSection({ canEdit: false });

		await expand();

		expect(screen.queryByLabelText("delete")).not.toBeInTheDocument();
	});

	it("offers a way to bring a retired Delivery back", async () => {
		const onUnarchive = vi.fn();
		renderSection({ onUnarchive });

		await expand();
		await userEvent.click(screen.getByLabelText("unarchive"));

		expect(onUnarchive).toHaveBeenCalledTimes(1);
		expect(onUnarchive.mock.calls[0][0].id).toBe(9);
	});

	it("lets a lapsed instance bring a Delivery back, so nobody is left stranded in the archive", async () => {
		const onUnarchive = vi.fn();
		renderSection({ onUnarchive, canUsePremiumFeatures: false });

		await expand();
		const unarchive = screen.getByLabelText("unarchive");

		expect(unarchive).not.toBeDisabled();

		await userEvent.click(unarchive);
		expect(onUnarchive).toHaveBeenCalledTimes(1);
	});

	it("offers no way back to a reader who may not change the Portfolio", async () => {
		renderSection({ canEdit: false });

		await expand();

		expect(screen.queryByLabelText("unarchive")).not.toBeInTheDocument();
	});
});
