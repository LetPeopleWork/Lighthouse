import { render, screen } from "@testing-library/react";
import { userEvent } from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import {
	ArchivedDelivery,
	ArchivedDeliverySchema,
} from "../../../../../models/Delivery/ArchivedDelivery";
import ArchivedDeliveriesSection from "./ArchivedDeliveriesSection";

vi.mock("../../../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) =>
			({
				delivery: "Delivery",
				deliveries: "Deliveries",
				features: "Features",
				workItems: "Work Items",
			})[key] ?? key,
	}),
}));

const makeArchived = (overrides: Record<string, unknown> = {}) =>
	ArchivedDelivery.fromParsed(
		ArchivedDeliverySchema.parse({
			id: 9,
			name: "Autumn Launch",
			date: "2026-05-01T00:00:00Z",
			portfolioId: 1,
			archivedOn: "2026-05-04T00:00:00Z",
			progress: 80,
			totalWork: 50,
			doneWork: 40,
			remainingWork: 10,
			likelihoodPercentage: 64,
			hasSufficientData: true,
			teamsWithoutForecast: [],
			selectionMode: "Manual",
			concurrencyToken: "33333333-3333-3333-3333-333333333333",
			...overrides,
		}),
	);

const renderSection = (props?: {
	archived?: ArchivedDelivery[];
	canEdit?: boolean;
	onDelete?: (delivery: ArchivedDelivery) => void;
}) =>
	render(
		<ArchivedDeliveriesSection
			archivedDeliveries={props?.archived ?? [makeArchived()]}
			canEdit={props?.canEdit ?? true}
			onDelete={props?.onDelete ?? vi.fn()}
		/>,
	);

const expand = async () =>
	userEvent.click(screen.getByRole("button", { name: /Archived/ }));

describe("ArchivedDeliveriesSection", () => {
	it("says nothing at all when a Portfolio has retired nothing", () => {
		renderSection({ archived: [] });

		expect(screen.queryByText(/Archived/)).not.toBeInTheDocument();
	});

	it("is folded away until someone opens it", () => {
		renderSection();

		expect(screen.getByRole("button", { name: /Archived/ })).toHaveAttribute(
			"aria-expanded",
			"false",
		);
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

	it("records the day it stopped moving", async () => {
		renderSection();

		await expand();

		expect(
			screen.getByText(
				new RegExp(
					`Archived: ${makeArchived().getFormattedArchivedOn().replace(/\//g, "\\/")}`,
				),
			),
		).toBeInTheDocument();
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
});
