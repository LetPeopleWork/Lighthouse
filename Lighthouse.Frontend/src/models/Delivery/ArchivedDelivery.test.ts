import { describe, expect, it } from "vitest";
import { ArchivedDelivery, ArchivedDeliverySchema } from "./ArchivedDelivery";

const wireRow = {
	id: 7,
	name: "Phoenix Release",
	date: "2026-06-01T00:00:00Z",
	portfolioId: 3,
	archivedOn: "2026-08-20T00:00:00Z",
	progress: 80,
	totalWork: 50,
	doneWork: 40,
	remainingWork: 10,
	likelihoodPercentage: 64.4,
	hasSufficientData: true,
	teamsWithoutForecast: [],
	selectionMode: "Manual",
	concurrencyToken: "33333333-3333-3333-3333-333333333333",
	featureBreakdown: [
		{
			referenceId: "FTR-1",
			name: "Checkout rewrite",
			completion: 60,
			likelihood: 72,
			totalItems: 20,
			isUsingDefaultSize: false,
		},
	],
	whenDistribution: [{ probability: 85, expectedDate: "2026-06-14T00:00:00Z" }],
	rules: [],
	mode: "and",
	metricSnapshotCount: 11,
};

describe("ArchivedDelivery", () => {
	it("reads every number the server wrote down at closure", () => {
		const archived = ArchivedDelivery.fromParsed(
			ArchivedDeliverySchema.parse(wireRow),
		);

		expect(archived.id).toBe(7);
		expect(archived.name).toBe("Phoenix Release");
		expect(archived.portfolioId).toBe(3);
		expect(archived.progress).toBe(80);
		expect(archived.totalWork).toBe(50);
		expect(archived.doneWork).toBe(40);
		expect(archived.remainingWork).toBe(10);
		expect(archived.likelihoodPercentage).toBe(64.4);
		expect(archived.hasSufficientData).toBe(true);
		expect(archived.teamsWithoutForecast).toEqual([]);
		expect(archived.selectionMode).toBe("Manual");
		expect(archived.concurrencyToken).toBe(
			"33333333-3333-3333-3333-333333333333",
		);
	});

	it("carries the Feature rows themselves, so there is nothing to look up", () => {
		const archived = ArchivedDelivery.fromParsed(
			ArchivedDeliverySchema.parse(wireRow),
		);

		expect(archived.featureBreakdown).toEqual([
			{
				referenceId: "FTR-1",
				name: "Checkout rewrite",
				completion: 60,
				likelihood: 72,
				totalItems: 20,
				isUsingDefaultSize: false,
				url: null,
			},
		]);
	});

	it("accepts a Feature row recorded before sizes were kept", () => {
		const archived = ArchivedDelivery.fromParsed(
			ArchivedDeliverySchema.parse({
				...wireRow,
				featureBreakdown: [
					{
						referenceId: "FTR-9",
						name: "Older row",
						completion: 100,
						likelihood: null,
					},
				],
			}),
		);

		expect(archived.featureBreakdown[0].totalItems).toBeNull();
		expect(archived.featureBreakdown[0].isUsingDefaultSize).toBeNull();
		expect(archived.featureBreakdown[0].likelihood).toBeNull();
	});

	it("keeps the forecast dates that were worked out on the closing day", () => {
		const archived = ArchivedDelivery.fromParsed(
			ArchivedDeliverySchema.parse(wireRow),
		);

		expect(archived.whenDistribution).toHaveLength(1);
		expect(archived.whenDistribution[0].probability).toBe(85);
		expect(archived.whenDistribution[0].expectedDate).toEqual(
			new Date("2026-06-14T00:00:00Z"),
		);
	});

	it("keeps the rule the Delivery was picking its Features by", () => {
		const archived = ArchivedDelivery.fromParsed(
			ArchivedDeliverySchema.parse({
				...wireRow,
				selectionMode: "RuleBased",
				rules: [{ fieldKey: "tag", operator: "equals", value: "phoenix" }],
				mode: "or",
			}),
		);

		expect(archived.isRuleBased).toBe(true);
		expect(archived.rules).toEqual([
			{ fieldKey: "tag", operator: "equals", value: "phoenix" },
		]);
		expect(archived.mode).toBe("or");
	});

	it("counts the days of history standing behind the record", () => {
		const archived = ArchivedDelivery.fromParsed(
			ArchivedDeliverySchema.parse(wireRow),
		);

		expect(archived.metricSnapshotCount).toBe(11);
	});

	it("carries none of the live fields a Delivery is worked out from", () => {
		const archived = ArchivedDelivery.fromParsed(
			ArchivedDeliverySchema.parse({
				...wireRow,
				features: [1, 2],
				completionDates: [{ probability: 85, expectedDate: "2026-06-01" }],
				featureLikelihoods: [{ featureId: 1, likelihoodPercentage: 90 }],
			}),
		);

		expect("features" in archived).toBe(false);
		expect("completionDates" in archived).toBe(false);
		expect("featureLikelihoods" in archived).toBe(false);
	});

	it("gives a pinned Feature row no id to reach a live Feature by, but keeps its link", () => {
		const archived = ArchivedDelivery.fromParsed(
			ArchivedDeliverySchema.parse({
				...wireRow,
				featureBreakdown: [
					{
						referenceId: "FTR-1",
						name: "Checkout rewrite",
						completion: 60,
						likelihood: 72,
						id: 4242,
						url: "https://tracker.example/FTR-1",
					},
				],
			}),
		);

		// An id is a way to fetch the Feature as it stands today, which is the one thing this record
		// must not offer. A link is not: it opens the work tracking system, and nothing it shows can
		// travel back into this view.
		expect("id" in archived.featureBreakdown[0]).toBe(false);
		expect(archived.featureBreakdown[0].url).toBe(
			"https://tracker.example/FTR-1",
		);
	});

	it("keeps a Delivery that closed without a forecast, and the Teams that explain why", () => {
		const archived = ArchivedDelivery.fromParsed(
			ArchivedDeliverySchema.parse({
				...wireRow,
				likelihoodPercentage: null,
				hasSufficientData: false,
				teamsWithoutForecast: ["Team Alpha", "Team Beta"],
			}),
		);

		expect(archived.likelihoodPercentage).toBeNull();
		expect(archived.hasSufficientData).toBe(false);
		expect(archived.teamsWithoutForecast).toEqual(["Team Alpha", "Team Beta"]);
	});

	it("formats both dates in UTC, so the closing day reads the same everywhere", () => {
		const archived = ArchivedDelivery.fromParsed(
			ArchivedDeliverySchema.parse(wireRow),
		);

		expect(archived.getFormattedDate()).toBe(
			new Date("2026-06-01T00:00:00Z").toLocaleDateString(undefined, {
				timeZone: "UTC",
			}),
		);
		expect(archived.getFormattedArchivedOn()).toBe(
			new Date("2026-08-20T00:00:00Z").toLocaleDateString(undefined, {
				timeZone: "UTC",
			}),
		);
	});

	it("refuses a row that is missing a number it is supposed to have pinned", () => {
		const { totalWork, ...withoutTotalWork } = wireRow;
		void totalWork;

		expect(() => ArchivedDeliverySchema.parse(withoutTotalWork)).toThrow();
	});

	it("refuses a row that arrives without the Feature rows it was closed with", () => {
		const { featureBreakdown, ...withoutRows } = wireRow;
		void featureBreakdown;

		expect(() => ArchivedDeliverySchema.parse(withoutRows)).toThrow();
	});
});
