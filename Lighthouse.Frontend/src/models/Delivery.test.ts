import type { IDelivery } from "./Delivery";
import { Delivery } from "./Delivery";
import { DeliverySelectionMode } from "./WorkItemRules";

const buildBackendDelivery = (
	overrides: Partial<IDelivery> = {},
): IDelivery => ({
	id: 1,
	name: "Release 1",
	date: "2026-08-15T00:00:00.000Z",
	portfolioId: 1,
	features: [],
	likelihoodPercentage: 0,
	progress: 0,
	remainingWork: 0,
	totalWork: 0,
	featureLikelihoods: [],
	completionDates: [],
	selectionMode: DeliverySelectionMode.Manual,
	metricSnapshotCount: 0,
	...overrides,
});

describe("Delivery.getFormattedDate", () => {
	it.each([
		"2026-08-15T00:00:00.000Z",
		"2026-12-31T23:30:00.000Z",
		"2026-01-01T00:30:00.000Z",
	])(
		"renders %s as the UTC calendar date regardless of viewer timezone (bug 4975)",
		(date) => {
			const delivery = Delivery.fromBackend(buildBackendDelivery({ date }));

			const expectedUtc = new Date(date).toLocaleDateString(undefined, {
				timeZone: "UTC",
			});

			expect(delivery.getFormattedDate()).toBe(expectedUtc);
		},
	);
});

describe("Delivery.isOverdue", () => {
	const theDayTheScreenIsBeingRead = new Date("2026-08-25T09:00:00.000Z");

	it.each([
		["2026-08-24T00:00:00.000Z", true, "the day before"],
		["2026-08-25T00:00:00.000Z", false, "today"],
		["2026-08-26T00:00:00.000Z", false, "the day after"],
	])("treats %s as overdue=%s (%s)", (date, expected) => {
		const delivery = Delivery.fromBackend(buildBackendDelivery({ date }));

		expect(delivery.isOverdue(theDayTheScreenIsBeingRead)).toBe(expected);
	});

	// A target arriving from Jira can land anywhere in the day, and the screen shows the UTC day it
	// falls on. Reading the day off the local clock instead would call a Delivery overdue while the
	// date printed beside the word still says today.
	it("compares the day the screen shows, not the instant", () => {
		const delivery = Delivery.fromBackend(
			buildBackendDelivery({ date: "2026-08-25T23:30:00.000Z" }),
		);

		expect(delivery.isOverdue(new Date("2026-08-25T23:59:00.000Z"))).toBe(
			false,
		);
	});

	it("says nothing is overdue when the target is still ahead of an unspecified now", () => {
		const delivery = Delivery.fromBackend(
			buildBackendDelivery({ date: "2999-01-01T00:00:00.000Z" }),
		);

		expect(delivery.isOverdue()).toBe(false);
	});
});
