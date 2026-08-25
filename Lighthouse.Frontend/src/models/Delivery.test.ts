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
	isOverdue: false,
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
	// Decided on the instance time zone, which the browser has no way to know, so the flag crosses the
	// wire rather than being recomputed here. A viewer on the other side of midnight from the instance
	// must not see a different verdict from the one the instance would give.
	it.each([true, false])("carries the backend's verdict of %s", (verdict) => {
		const delivery = Delivery.fromBackend(
			buildBackendDelivery({ isOverdue: verdict }),
		);

		expect(delivery.isOverdue).toBe(verdict);
	});

	it("says nothing is overdue when the backend said nothing at all", () => {
		const withoutTheField = buildBackendDelivery({});
		delete (withoutTheField as Partial<IDelivery>).isOverdue;

		expect(Delivery.fromBackend(withoutTheField).isOverdue).toBe(false);
	});
});
