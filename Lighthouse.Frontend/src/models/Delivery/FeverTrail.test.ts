import {
	type DeliveryMetricsHistory,
	parseDeliveryMetricsHistory,
} from "./DeliveryMetricsHistory";
import {
	AMBER_RED_INTERCEPT,
	BAND_SLOPE,
	deriveFeatureFeverChart,
	type FeverZone,
	feverZonePolygons,
} from "./FeverTrail";

type RawFeature = {
	referenceId: string;
	name: string;
	completion: number;
	likelihood: number;
};

type RawPoint = {
	date: string;
	featureBreakdown?: RawFeature[];
};

const getMockPoint = (overrides: RawPoint) => ({
	date: overrides.date,
	totalWork: 20,
	doneWork: 0,
	remainingWork: 20,
	estimatedItemCount: null,
	forecastHowMany: null,
	likelihoodPercentage: null,
	whenDistribution: null,
	featureBreakdown: overrides.featureBreakdown ?? [],
});

const getMockHistory = (overrides: {
	points: RawPoint[];
}): DeliveryMetricsHistory =>
	parseDeliveryMetricsHistory({
		deliveryDate: "2026-06-21T00:00:00Z",
		firstSnapshotDate: "2026-06-01T00:00:00Z",
		points: overrides.points.map(getMockPoint),
	});

const twoFeatureHistory = getMockHistory({
	points: [
		{
			date: "2026-06-01T00:00:00Z",
			featureBreakdown: [
				{
					referenceId: "F-1",
					name: "Checkout",
					completion: 20,
					likelihood: 90,
				},
				{ referenceId: "F-2", name: "Search", completion: 10, likelihood: 50 },
			],
		},
		{
			date: "2026-06-08T00:00:00Z",
			featureBreakdown: [
				{
					referenceId: "F-1",
					name: "Checkout",
					completion: 60,
					likelihood: 95,
				},
				{ referenceId: "F-2", name: "Search", completion: 40, likelihood: 30 },
			],
		},
	],
});

describe("deriveFeatureFeverChart", () => {
	it("groups snapshots into one date-ordered series per feature", () => {
		const chart = deriveFeatureFeverChart(twoFeatureHistory);

		expect(chart.empty).toBe(false);
		expect(chart.features.map((feature) => feature.referenceId)).toEqual([
			"F-1",
			"F-2",
		]);
		expect(chart.features[0].points).toHaveLength(2);
	});

	it("maps completion onto x and the inverted likelihood onto chance of being late", () => {
		const checkout = deriveFeatureFeverChart(twoFeatureHistory).features[0];

		expect(checkout.points[0]).toMatchObject({
			completion: 20,
			chanceOfLate: 10,
		});
		expect(checkout.points[1]).toMatchObject({
			completion: 60,
			chanceOfLate: 5,
		});
	});

	it("exposes each feature's most recent snapshot as its latest position", () => {
		const search = deriveFeatureFeverChart(twoFeatureHistory).features[1];

		expect(search.name).toBe("Search");
		expect(search.latest).toMatchObject({ completion: 40, chanceOfLate: 70 });
	});

	it("includes only the snapshots in which a feature appears", () => {
		const chart = deriveFeatureFeverChart(
			getMockHistory({
				points: [
					{
						date: "2026-06-01T00:00:00Z",
						featureBreakdown: [
							{
								referenceId: "F-1",
								name: "Checkout",
								completion: 20,
								likelihood: 90,
							},
						],
					},
					{
						date: "2026-06-08T00:00:00Z",
						featureBreakdown: [
							{
								referenceId: "F-1",
								name: "Checkout",
								completion: 60,
								likelihood: 95,
							},
							{
								referenceId: "F-2",
								name: "Search",
								completion: 40,
								likelihood: 30,
							},
						],
					},
				],
			}),
		);

		const search = chart.features.find((f) => f.referenceId === "F-2");
		expect(search?.points).toHaveLength(1);
	});

	it("reports empty when no snapshot carries a feature breakdown", () => {
		const chart = deriveFeatureFeverChart(
			getMockHistory({
				points: [{ date: "2026-06-01T00:00:00Z", featureBreakdown: [] }],
			}),
		);

		expect(chart.empty).toBe(true);
		expect(chart.features).toEqual([]);
	});
});

describe("feverZonePolygons", () => {
	const byZone = (zone: FeverZone) =>
		feverZonePolygons().find((polygon) => polygon.zone === zone);

	it("anchors the amber/red band to the top-right corner and the green band to the bottom-right", () => {
		expect(byZone("green")?.points).toEqual([
			[0, 0],
			[100, 0],
			[100, BAND_SLOPE * 100],
		]);
		expect(byZone("red")?.points).toEqual([
			[0, AMBER_RED_INTERCEPT],
			[100, 100],
			[0, 100],
		]);
	});

	it("fills the plane with green, amber and red regions", () => {
		expect(feverZonePolygons().map((polygon) => polygon.zone)).toEqual([
			"green",
			"amber",
			"red",
		]);
	});
});
