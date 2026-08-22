import { describe, expect, it } from "vitest";
import {
	ArchivedDelivery,
	ArchivedDeliverySchema,
} from "../../../../../models/Delivery/ArchivedDelivery";
import { DeliverySelectionMode } from "../../../../../models/WorkItemRules";
import { formatLocalDate } from "../../../../../utils/date/localDate";
import {
	buildArchivedDeliveryExportTable,
	buildDeliveryExportTable,
	type DeliveryExportTerms,
} from "./deliveryExportTable";

const terms: DeliveryExportTerms = {
	deliveryTerm: "Delivery",
	workItemsTerm: "Work Items",
	featureTerm: "Feature",
	portfolioTerm: "Portfolio",
};

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
			concurrencyToken: "44444444-4444-4444-4444-444444444444",
			featureBreakdown: [
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
			],
			whenDistribution: [
				{ probability: 50, expectedDate: "2026-04-20T00:00:00Z" },
				{ probability: 70, expectedDate: "2026-04-24T00:00:00Z" },
				{ probability: 85, expectedDate: "2026-04-29T00:00:00Z" },
				{ probability: 95, expectedDate: "2026-05-06T00:00:00Z" },
			],
			rules: [],
			mode: "and",
			metricSnapshotCount: 11,
			...overrides,
		}),
	);

describe("buildArchivedDeliveryExportTable", () => {
	it("writes the same columns a live Delivery exports, so the two files can be read side by side", () => {
		const archived = makeArchived();

		const table = buildArchivedDeliveryExportTable(
			archived,
			archived.featureBreakdown,
			terms,
		);

		expect(table.headers).toEqual([
			"Name",
			"Team",
			"Progress",
			"Forecast 50%",
			"Forecast 70%",
			"Forecast 85%",
			"Forecast 95%",
			"Likelihood",
			"State",
			"Dependencies",
			"Warnings",
		]);
	});

	it("puts the Delivery itself in the first data row, as a live export does", () => {
		const archived = makeArchived();

		const table = buildArchivedDeliveryExportTable(
			archived,
			archived.featureBreakdown,
			terms,
		);

		expect(table.rows[0][0]).toBe("Autumn Launch (Delivery)");
		expect(table.rows[0][2]).toBe("40/50");
		expect(table.rows[0][7]).toBe("64%");
	});

	it("carries the forecast dates that were worked out on the closing day", () => {
		const archived = makeArchived();

		const table = buildArchivedDeliveryExportTable(
			archived,
			archived.featureBreakdown,
			terms,
		);

		expect(table.rows[0].slice(3, 7)).toEqual([
			formatLocalDate(new Date("2026-04-20T00:00:00Z")),
			formatLocalDate(new Date("2026-04-24T00:00:00Z")),
			formatLocalDate(new Date("2026-04-29T00:00:00Z")),
			formatLocalDate(new Date("2026-05-06T00:00:00Z")),
		]);
	});

	it("writes the Feature rows that were noted, not ones worked out now", () => {
		const archived = makeArchived();

		const table = buildArchivedDeliveryExportTable(
			archived,
			archived.featureBreakdown,
			terms,
		);

		expect(table.rows).toHaveLength(3);
		expect(table.rows[1][0]).toBe("FTR-1: Checkout rewrite");
		expect(table.rows[1][2]).toBe("12/20");
		expect(table.rows[1][7]).toBe("72%");
		expect(table.rows[2][0]).toBe("FTR-2: Search relevance");
		expect(table.rows[2][2]).toBe("8/8");
	});

	it("emits the rows in the order the reader is looking at them", () => {
		const archived = makeArchived();

		const table = buildArchivedDeliveryExportTable(
			archived,
			[archived.featureBreakdown[1], archived.featureBreakdown[0]],
			terms,
		);

		expect(table.rows[1][0]).toBe("FTR-2: Search relevance");
		expect(table.rows[2][0]).toBe("FTR-1: Checkout rewrite");
	});

	it("leaves blank every cell the record never held, rather than inventing one", () => {
		const archived = makeArchived();

		const table = buildArchivedDeliveryExportTable(
			archived,
			archived.featureBreakdown,
			terms,
		);

		const featureRow = table.rows[1];
		expect(featureRow[1]).toBe("");
		expect(featureRow.slice(3, 7)).toEqual(["", "", "", ""]);
		expect(featureRow[8]).toBe("");
		expect(featureRow[9]).toBe("");
	});

	it("answers the warning question when the record answered it", () => {
		const archived = makeArchived();

		const table = buildArchivedDeliveryExportTable(
			archived,
			archived.featureBreakdown,
			terms,
		);

		// The record says this Feature's size was counted, which is an answer. Blanking it would
		// read as nobody having asked, and the reader could not line it up against a live export.
		expect(table.rows[1][10]).toBe("No");
	});

	it("flags the Feature whose size was a default rather than a count", () => {
		const archived = makeArchived();

		const table = buildArchivedDeliveryExportTable(
			archived,
			archived.featureBreakdown,
			terms,
		);

		expect(table.rows[2][10]).toBe("Yes");
	});

	it("says a Feature could not be forecast rather than reporting a zero chance", () => {
		const archived = makeArchived({
			featureBreakdown: [
				{
					referenceId: "FTR-3",
					name: "Payments",
					completion: 25,
					likelihood: null,
					totalItems: 4,
					isUsingDefaultSize: false,
				},
			],
		});

		const table = buildArchivedDeliveryExportTable(
			archived,
			archived.featureBreakdown,
			terms,
		);

		expect(table.rows[1][7]).toBe("Cannot forecast");
	});

	it("leaves the Delivery's likelihood blank when it closed without one", () => {
		const archived = makeArchived({ likelihoodPercentage: null });

		const table = buildArchivedDeliveryExportTable(
			archived,
			archived.featureBreakdown,
			terms,
		);

		expect(table.rows[0][7]).toBe("");
	});

	it("leaves progress blank on a row recorded before Work Item counts were kept", () => {
		const archived = makeArchived({
			featureBreakdown: [
				{
					referenceId: "FTR-9",
					name: "Older row",
					completion: 40,
					likelihood: null,
				},
			],
		});

		const table = buildArchivedDeliveryExportTable(
			archived,
			archived.featureBreakdown,
			terms,
		);

		expect(table.rows[1][2]).toBe("");
	});

	it("keeps the same header row as the live builder, from the one definition", () => {
		const archived = makeArchived();

		expect(
			buildArchivedDeliveryExportTable(
				archived,
				archived.featureBreakdown,
				terms,
			).headers,
		).toEqual(
			buildDeliveryExportTable(
				{
					id: 1,
					name: "Live",
					date: "2026-05-01",
					portfolioId: 1,
					features: [],
					likelihoodPercentage: null,
					progress: 0,
					remainingWork: 0,
					totalWork: 0,
					featureLikelihoods: [],
					completionDates: [],
					selectionMode: DeliverySelectionMode.Manual,
					metricSnapshotCount: 0,
				},
				[],
				[],
				terms,
			).headers,
		);
	});
});
