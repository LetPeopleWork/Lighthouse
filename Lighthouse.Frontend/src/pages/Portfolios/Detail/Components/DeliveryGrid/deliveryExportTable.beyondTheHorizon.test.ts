import { describe, expect, it } from "vitest";
import { Delivery, type IDelivery } from "../../../../../models/Delivery";
import { Feature } from "../../../../../models/Feature";
import { WhenForecast } from "../../../../../models/Forecasts/WhenForecast";
import { DeliverySelectionMode } from "../../../../../models/WorkItemRules";
import { buildDeliveryExportTable } from "./deliveryExportTable";

const TERMS = {
	deliveryTerm: "Delivery",
	workItemsTerm: "Work Items",
	featureTerm: "Feature",
	portfolioTerm: "Portfolio",
	teamTerm: "Team",
};

const FORECAST_COLUMNS = [3, 4, 5, 6];

const percentile = (
	probability: number,
	beyondHorizon: boolean,
): WhenForecast => {
	const forecast = WhenForecast.new(
		probability,
		beyondHorizon
			? new Date("2031-10-18T00:00:00Z")
			: new Date("2029-03-14T00:00:00Z"),
	);
	forecast.beyondHorizon = beyondHorizon;
	forecast.horizonYears = 5;
	return forecast;
};

/** Six in ten runs finished: the 50% has a day, the three above it are beyond the horizon. */
const sixInTenFinish = (): WhenForecast[] => [
	percentile(50, false),
	percentile(70, true),
	percentile(85, true),
	percentile(95, true),
];

const aDelivery = (completionDates: WhenForecast[]): Delivery => {
	const delivery = Delivery.fromBackend({
		id: 1,
		name: "Q4 Billing",
		date: "2026-12-15T00:00:00",
		portfolioId: 7,
		features: [],
		likelihoodPercentage: 12,
		progress: 0.1,
		remainingWork: 480,
		totalWork: 500,
		featureLikelihoods: [],
		completionDates: [],
		selectionMode: DeliverySelectionMode.Manual,
		metricSnapshotCount: 3,
	} as unknown as IDelivery);
	delivery.completionDates = completionDates;
	return delivery;
};

const aFeature = (forecasts: WhenForecast[]): Feature =>
	Object.assign(new Feature(), {
		id: 11,
		name: "Billing Migration",
		referenceId: "PAY-1",
		state: "Active",
		stateCategory: "Doing",
		lastUpdated: new Date(),
		isUsingDefaultFeatureSize: false,
		projects: [],
		remainingWork: { 1: 480 },
		totalWork: { 1: 500 },
		forecasts,
		teamsWithoutForecast: [],
		dependsOn: [],
		url: "",
	});

const forecastCellsOf = (row: string[]) =>
	FORECAST_COLUMNS.map((column) => row[column]);

describe("exporting a Delivery whose percentiles are beyond the horizon", () => {
	it(`writes "More than 5 years" in the Feature's beyond cells and the date in the one that has a day`, () => {
		const table = buildDeliveryExportTable(
			aDelivery([]),
			[aFeature(sixInTenFinish())],
			[{ id: 1, name: "Payments" }],
			TERMS,
		);

		const cells = forecastCellsOf(table.rows[1]);
		expect(cells.slice(1)).toEqual([
			"More than 5 years",
			"More than 5 years",
			"More than 5 years",
		]);
		expect(cells[0]).not.toBe("More than 5 years");
		expect(cells[0]).not.toBe("");
	});

	it(`writes "More than 5 years" in the Delivery's own beyond cells`, () => {
		const table = buildDeliveryExportTable(
			aDelivery(sixInTenFinish()),
			[],
			[],
			TERMS,
		);

		expect(forecastCellsOf(table.rows[0]).slice(1)).toEqual([
			"More than 5 years",
			"More than 5 years",
			"More than 5 years",
		]);
	});

	it(`never writes the stand-in date, an empty cell or "Cannot forecast" for a beyond percentile`, () => {
		const table = buildDeliveryExportTable(
			aDelivery(sixInTenFinish()),
			[aFeature(sixInTenFinish())],
			[{ id: 1, name: "Payments" }],
			TERMS,
		);

		expect(table.rows).toHaveLength(2);

		for (const row of table.rows) {
			expect(forecastCellsOf(row)).toHaveLength(4);

			for (const cell of forecastCellsOf(row).slice(1)) {
				expect(cell).not.toMatch(/2031|^$|cannot forecast/i);
			}
		}
	});
});
