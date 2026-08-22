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
});
