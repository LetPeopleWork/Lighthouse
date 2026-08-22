import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { FeatureMetric } from "../../../../../models/Delivery/DeliveryMetricsHistory";
import ArchivedFeatureGrid from "./ArchivedFeatureGrid";

const { mockUseLicenseRestrictions } = vi.hoisted(() => ({
	mockUseLicenseRestrictions: vi.fn(),
}));

vi.mock("../../../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: mockUseLicenseRestrictions,
}));

const pinnedRows: FeatureMetric[] = [
	{
		referenceId: "FTR-1",
		name: "Checkout rewrite",
		completion: 60,
		likelihood: 72,
		totalItems: 20,
		isUsingDefaultSize: false,
		url: "https://tracker.example/FTR-1",
	},
	{
		referenceId: "FTR-2",
		name: "Search relevance",
		completion: 100,
		likelihood: null,
		totalItems: 8,
		isUsingDefaultSize: true,
		// A record written before the link was kept. The row still renders, unlinked.
		url: null,
	},
];

const renderGrid = (rows: FeatureMetric[] = pinnedRows) =>
	render(
		<ArchivedFeatureGrid
			rows={rows}
			deliveryId={9}
			featureTerm="Feature"
			featuresTerm="Features"
			exportFileName="Autumn Launch"
			exportTable={vi.fn()}
		/>,
	);

describe("ArchivedFeatureGrid", () => {
	beforeEach(() => {
		localStorage.clear();
		mockUseLicenseRestrictions.mockReturnValue({
			licenseStatus: { canUsePremiumFeatures: true },
			isLoading: false,
		});
	});

	it("lists exactly the Feature rows that were written down", () => {
		renderGrid();

		expect(screen.getByText("FTR-1: Checkout rewrite")).toBeInTheDocument();
		expect(screen.getByText("FTR-2: Search relevance")).toBeInTheDocument();
	});

	it("answers which Features were in, and nothing else", () => {
		renderGrid();

		expect(screen.getByText("Feature Name")).toBeInTheDocument();

		// The record holds a completion, a chance and a size for every row. None of them is what a
		// reader opens a closed Delivery to find out, and each is a number they would then have to
		// decide whether to trust.
		expect(screen.queryByText("Completion")).not.toBeInTheDocument();
		expect(screen.queryByText("Likelihood")).not.toBeInTheDocument();
		expect(screen.queryByText("Work Items")).not.toBeInTheDocument();
		expect(screen.queryByText("Size")).not.toBeInTheDocument();
		expect(screen.queryByText("Reference")).not.toBeInTheDocument();

		expect(screen.queryByText("State")).not.toBeInTheDocument();
		expect(screen.queryByText("Team")).not.toBeInTheDocument();
		expect(screen.queryByText("Depends On")).not.toBeInTheDocument();
	});

	it("names each Feature by its reference and its name together", () => {
		renderGrid();

		expect(screen.getByText("FTR-1: Checkout rewrite")).toBeInTheDocument();
		expect(screen.getByText("FTR-2: Search relevance")).toBeInTheDocument();
	});

	it("opens the Feature in the work tracking system where the record kept a link", () => {
		renderGrid();

		expect(
			screen.getByRole("link", { name: "FTR-1: Checkout rewrite" }),
		).toHaveAttribute("href", "https://tracker.example/FTR-1");
	});

	it("still lists a row written before links were kept, without one", () => {
		renderGrid();

		expect(screen.getByText("FTR-2: Search relevance")).toBeInTheDocument();
		expect(
			screen.queryByRole("link", { name: "FTR-2: Search relevance" }),
		).not.toBeInTheDocument();
	});

	it("says so when the Delivery closed holding no Features at all", () => {
		renderGrid([]);

		expect(screen.getByText("No Features in this record")).toBeInTheDocument();
	});

	it("lists a row that recorded nothing but a name", () => {
		renderGrid([
			{
				referenceId: "FTR-9",
				name: "Older row",
				completion: 40,
				likelihood: null,
				totalItems: null,
				isUsingDefaultSize: null,
				url: null,
			},
		]);

		expect(screen.getByText("FTR-9: Older row")).toBeInTheDocument();
	});
});
