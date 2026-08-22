import {
	ArchivedDelivery,
	ArchivedDeliverySchema,
} from "../models/Delivery/ArchivedDelivery";

/**
 * One retired Delivery as it comes off the wire. Shared by the tests that read it back, so a change
 * to the pinned shape has one place to land rather than five near-identical literals to chase.
 */
export const makeArchivedDelivery = (
	overrides: Record<string, unknown> = {},
): ArchivedDelivery =>
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
			concurrencyToken: "22222222-2222-2222-2222-222222222222",
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
