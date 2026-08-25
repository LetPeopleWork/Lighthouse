import { describe, expect, it } from "vitest";
import { Delivery, type IDelivery } from "./Delivery";

/**
 * The wire mapping for what a source refused. Worth its own test because nothing else exercises it:
 * the component suites build a Delivery by hand, and the parse boundary is an identity pass-through
 * that validates nothing. Dropped or renamed, the notice silently never appears - which is exactly the
 * "switch that appears to do nothing" the feature exists to remove - with the suite still green.
 */

const WHAT_JIRA_SAID =
	"You must have global or project administrator rights in order to modify versions.";

function wirePayload(overrides: Partial<IDelivery> = {}): IDelivery {
	return {
		id: 1,
		name: "Release 3.0",
		date: "2026-12-19T00:00:00Z",
		portfolioId: 1,
		features: [],
		likelihoodPercentage: 72,
		progress: 0,
		remainingWork: 0,
		totalWork: 0,
		featureLikelihoods: [],
		completionDates: [],
		selectionMode: 2,
		metricSnapshotCount: 0,
		...overrides,
	} as unknown as IDelivery;
}

describe("Delivery.fromBackend and what a source refused", () => {
	it("keeps the sentence and the day the source gave", () => {
		const delivery = Delivery.fromBackend(
			wirePayload({
				lastPublishRefusalReason: WHAT_JIRA_SAID,
				lastPublishRefusedOn: "2026-08-25T00:00:00Z",
			}),
		);

		expect(delivery.lastPublishRefusalReason).toBe(WHAT_JIRA_SAID);
		expect(delivery.lastPublishRefusedOn).toBe("2026-08-25T00:00:00Z");
	});

	it("reads a delivery nothing refused as one nothing refused", () => {
		const delivery = Delivery.fromBackend(wirePayload());

		expect(delivery.lastPublishRefusalReason).toBeNull();
		expect(delivery.lastPublishRefusedOn).toBeNull();
	});

	it("carries whether the forecast is broadcast at all", () => {
		expect(
			Delivery.fromBackend(wirePayload({ publishForecastToSource: true }))
				.publishForecastToSource,
		).toBe(true);
		expect(Delivery.fromBackend(wirePayload()).publishForecastToSource).toBe(
			false,
		);
	});
});
