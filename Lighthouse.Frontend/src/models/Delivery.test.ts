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
	...overrides,
});

describe("Delivery.getFormattedDate", () => {
	it.each([
		"2026-08-15T00:00:00.000Z",
		"2026-12-31T23:30:00.000Z",
		"2026-01-01T00:30:00.000Z",
	])("renders %s as the UTC calendar date regardless of viewer timezone (bug 4975)", (date) => {
		const delivery = Delivery.fromBackend(buildBackendDelivery({ date }));

		const expectedUtc = new Date(date).toLocaleDateString(undefined, {
			timeZone: "UTC",
		});

		expect(delivery.getFormattedDate()).toBe(expectedUtc);
	});
});
