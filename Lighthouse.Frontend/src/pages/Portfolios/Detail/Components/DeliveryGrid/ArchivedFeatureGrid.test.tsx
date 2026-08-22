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
	},
	{
		referenceId: "FTR-2",
		name: "Search relevance",
		completion: 100,
		likelihood: null,
		totalItems: 8,
		isUsingDefaultSize: true,
	},
];

const renderGrid = (rows: FeatureMetric[] = pinnedRows) =>
	render(
		<ArchivedFeatureGrid
			rows={rows}
			deliveryId={9}
			featureTerm="Feature"
			featuresTerm="Features"
			workItemsTerm="Work Items"
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

		expect(screen.getByText("Checkout rewrite")).toBeInTheDocument();
		expect(screen.getByText("Search relevance")).toBeInTheDocument();
		expect(screen.getByText("FTR-1")).toBeInTheDocument();
		expect(screen.getByText("FTR-2")).toBeInTheDocument();
	});

	it("shows each row's totals as they were noted", () => {
		renderGrid();

		expect(screen.getByText("60%")).toBeInTheDocument();
		expect(screen.getByText("72%")).toBeInTheDocument();
		expect(screen.getByText("20")).toBeInTheDocument();
		expect(screen.getByText("8")).toBeInTheDocument();
	});

	it("carries only the columns the record held", () => {
		renderGrid();

		expect(screen.getByText("Reference")).toBeInTheDocument();
		expect(screen.getByText("Feature Name")).toBeInTheDocument();
		expect(screen.getByText("Completion")).toBeInTheDocument();
		expect(screen.getByText("Likelihood")).toBeInTheDocument();
		expect(screen.getByText("Work Items")).toBeInTheDocument();

		expect(screen.queryByText("State")).not.toBeInTheDocument();
		expect(screen.queryByText("Team")).not.toBeInTheDocument();
		expect(screen.queryByText("Forecast")).not.toBeInTheDocument();
		expect(screen.queryByText("Depends On")).not.toBeInTheDocument();
	});

	it("offers no way through to the Feature as it stands today", () => {
		renderGrid();

		expect(screen.queryByRole("link")).not.toBeInTheDocument();
	});

	it("says a Feature could not be forecast rather than showing a blank chance", () => {
		renderGrid();

		expect(screen.getByText("Cannot forecast")).toBeInTheDocument();
	});

	it("marks the Feature whose size was a default rather than a count", () => {
		renderGrid();

		expect(screen.getByText("Estimated")).toBeInTheDocument();
	});

	it("says so when the Delivery closed holding no Features at all", () => {
		renderGrid([]);

		expect(screen.getByText("No Features in this record")).toBeInTheDocument();
	});

	it("shows a dash where a row recorded before sizes were kept has no count", () => {
		renderGrid([
			{
				referenceId: "FTR-9",
				name: "Older row",
				completion: 40,
				likelihood: null,
				totalItems: null,
				isUsingDefaultSize: null,
			},
		]);

		expect(screen.getByText("Older row")).toBeInTheDocument();
		expect(screen.getByText("—")).toBeInTheDocument();
	});
});
