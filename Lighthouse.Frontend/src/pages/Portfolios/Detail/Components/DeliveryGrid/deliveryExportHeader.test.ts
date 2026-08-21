import { describe, expect, it } from "vitest";
import { Delivery, type IDelivery } from "../../../../../models/Delivery";
import { WhenForecast } from "../../../../../models/Forecasts/WhenForecast";
import { DeliverySelectionMode } from "../../../../../models/WorkItemRules";
import { buildDeliveryExportHeaderRows } from "./deliveryExportHeader";

const TERMS = { deliveryTerm: "Delivery", workItemsTerm: "Work Items" };

const makeDelivery = (overrides: Partial<IDelivery> = {}): Delivery => {
	const delivery = Delivery.fromBackend({
		id: 1,
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
		metricSnapshotCount: 3,
		...overrides,
	} as IDelivery);

	if (overrides.completionDates) {
		delivery.completionDates = overrides.completionDates as WhenForecast[];
	}

	return delivery;
};

const valueFor = (
	rows: ReturnType<typeof buildDeliveryExportHeaderRows>,
	label: string,
) => rows.find((row) => row.label === label)?.value;

describe("buildDeliveryExportHeaderRows", () => {
	it("emits the nine agreed fields in the agreed order", () => {
		const rows = buildDeliveryExportHeaderRows(makeDelivery(), TERMS);

		expect(rows.map((row) => row.label)).toEqual([
			"Delivery",
			"Date",
			"Forecast 70%",
			"Forecast 85%",
			"Forecast 95%",
			"Likelihood",
			"Total Work Items",
			"Completed Work Items",
			"Remaining Work Items",
		]);
	});

	it("derives completed work from the total and the remainder", () => {
		const rows = buildDeliveryExportHeaderRows(
			makeDelivery({ totalWork: 120, remainingWork: 48 }),
			TERMS,
		);

		expect(valueFor(rows, "Total Work Items")).toBe("120");
		expect(valueFor(rows, "Completed Work Items")).toBe("72");
		expect(valueFor(rows, "Remaining Work Items")).toBe("48");
	});

	it("renders each forecast the Delivery actually has", () => {
		const rows = buildDeliveryExportHeaderRows(
			makeDelivery({
				completionDates: [
					WhenForecast.new(70, new Date(2026, 8, 5)),
					WhenForecast.new(85, new Date(2026, 8, 12)),
					WhenForecast.new(95, new Date(2026, 8, 26)),
				],
			}),
			TERMS,
		);

		expect(valueFor(rows, "Forecast 70%")).toBe("2026-09-05");
		expect(valueFor(rows, "Forecast 85%")).toBe("2026-09-12");
		expect(valueFor(rows, "Forecast 95%")).toBe("2026-09-26");
	});

	it("leaves a forecast nobody computed empty rather than inventing one", () => {
		const rows = buildDeliveryExportHeaderRows(
			makeDelivery({ likelihoodPercentage: null, completionDates: [] }),
			TERMS,
		);

		for (const label of [
			"Likelihood",
			"Forecast 70%",
			"Forecast 85%",
			"Forecast 95%",
		]) {
			expect(valueFor(rows, label)).toBe("");
		}
	});

	it("never emits null, undefined, NaN or a fabricated zero for a missing forecast", () => {
		const rows = buildDeliveryExportHeaderRows(
			makeDelivery({ likelihoodPercentage: null, completionDates: [] }),
			TERMS,
		);

		for (const row of rows) {
			expect(row.value).not.toMatch(/null|undefined|NaN/i);
		}
		expect(valueFor(rows, "Likelihood")).not.toBe("0%");
	});

	it("writes the labels in the words this instance uses", () => {
		const rows = buildDeliveryExportHeaderRows(makeDelivery(), {
			deliveryTerm: "Milestone",
			workItemsTerm: "Tickets",
		});

		expect(rows.map((row) => row.label)).toContain("Milestone");
		expect(rows.map((row) => row.label)).toContain("Total Tickets");
		expect(rows.map((row) => row.label)).not.toContain("Delivery");
	});

	it("keeps a name containing a comma, a quote or a line break intact", () => {
		const awkward = 'Q3 "Platform", phase\none';
		const rows = buildDeliveryExportHeaderRows(
			makeDelivery({ name: awkward }),
			TERMS,
		);

		expect(valueFor(rows, "Delivery")).toBe(awkward);
	});

	it("renders the Delivery's own date as a calendar day", () => {
		const rows = buildDeliveryExportHeaderRows(
			makeDelivery({ date: "2026-09-12T00:00:00" }),
			TERMS,
		);

		expect(valueFor(rows, "Date")).toBe("2026-09-12");
	});
});
