import { Feature, FeatureSchema } from "./Feature";

// The path a Feature actually takes off the wire: zod parses the JSON, then fromParsed builds the
// model. Both halves are exercised together because splitting them would test neither seam - a
// schema that accepts a payload the mapper then mishandles is still a broken read.
const decode = (payload: Record<string, unknown>): Feature =>
	Feature.fromParsed(FeatureSchema.parse(payload));

// Everything the backend always sends, so each test can name only the part it is about.
const wireFeature = (overrides: Record<string, unknown> = {}) => ({
	name: "Some Feature",
	id: 42,
	referenceId: "FTR-42",
	state: "In Progress",
	type: "Feature",
	stateCategory: "Doing",
	lastUpdated: "2026-09-20T08:00:00Z",
	startedDate: null,
	closedDate: null,
	cycleTime: 0,
	workItemAge: 3,
	size: 10,
	owningTeam: "Team A",
	isUsingDefaultFeatureSize: false,
	parentWorkItemReference: "",
	remainingWork: { "1": 5 },
	totalWork: { "1": 10 },
	forecasts: [],
	...overrides,
});

describe("reading a Feature's start forecast off the wire", () => {
	it("turns each percentile into a forecast with a real date", () => {
		const feature = decode(
			wireFeature({
				startForecast: {
					source: "Forecast",
					percentiles: [
						{ probability: 50, expectedDate: "2026-10-01T00:00:00Z" },
						{ probability: 85, expectedDate: "2026-10-15T00:00:00Z" },
					],
				},
			}),
		);

		expect(feature.startForecast?.source).toBe("Forecast");
		expect(feature.startForecast?.percentiles).toHaveLength(2);
		expect(feature.startForecast?.percentiles[0].probability).toBe(50);
		expect(feature.startForecast?.percentiles[0].expectedDate).toEqual(
			new Date("2026-10-01T00:00:00Z"),
		);
		expect(feature.startForecast?.percentiles[1].probability).toBe(85);
		expect(feature.startForecast?.percentiles[1].expectedDate).toEqual(
			new Date("2026-10-15T00:00:00Z"),
		);
	});

	it("reads an observed start as a date, not as the string it arrived as", () => {
		const feature = decode(
			wireFeature({
				startForecast: {
					source: "Observed",
					observedDate: "2026-09-14T00:00:00Z",
					percentiles: [],
				},
			}),
		);

		expect(feature.startForecast?.source).toBe("Observed");
		expect(feature.startForecast?.observedDate).toEqual(
			new Date("2026-09-14T00:00:00Z"),
		);
	});

	// The cell asks `start.observedDate &&` before rendering. A null surviving the decode would be
	// falsy and so would look correct here, while the declared type says Date | undefined - so the
	// assertion is on the value itself, not on what the cell happens to do with it.
	it("normalises a missing observed date to undefined rather than null", () => {
		const feature = decode(
			wireFeature({
				startForecast: {
					source: "Forecast",
					observedDate: null,
					percentiles: [],
				},
			}),
		);

		expect(feature.startForecast?.observedDate).toBeUndefined();
	});

	it("leaves the percentiles an empty list when the backend sends none", () => {
		const feature = decode(
			wireFeature({ startForecast: { source: "Unknown" } }),
		);

		expect(feature.startForecast?.percentiles).toEqual([]);
	});

	// A frontend can be served by a backend that predates this field. If that made the parse fail,
	// one missing forecast would take the whole Feature list down with it.
	it("still reads a Feature sent by a backend that knows nothing about start forecasts", () => {
		const feature = decode(wireFeature());

		expect(feature.startForecast).toBeUndefined();
		expect(feature.name).toBe("Some Feature");
	});
});

describe("reading a Feature's per-team forecasts off the wire", () => {
	it("keeps both ends of each contributing team's share", () => {
		const feature = decode(
			wireFeature({
				teamForecasts: [
					{
						teamId: 7,
						startPercentiles: [
							{ probability: 50, expectedDate: "2026-10-01T00:00:00Z" },
						],
						completionPercentiles: [
							{ probability: 85, expectedDate: "2026-11-20T00:00:00Z" },
						],
					},
				],
			}),
		);

		expect(feature.teamForecasts).toHaveLength(1);
		expect(feature.teamForecasts[0].teamId).toBe(7);
		expect(feature.teamForecasts[0].startPercentiles[0].expectedDate).toEqual(
			new Date("2026-10-01T00:00:00Z"),
		);
		expect(
			feature.teamForecasts[0].completionPercentiles[0].expectedDate,
		).toEqual(new Date("2026-11-20T00:00:00Z"));
	});

	// Consumers iterate this list without guarding it, so an undefined here throws at the call site
	// rather than showing an empty breakdown.
	it("is an empty list, never undefined, when the backend sends no breakdown", () => {
		const feature = decode(wireFeature());

		expect(feature.teamForecasts).toEqual([]);
	});

	it("leaves either end an empty list when only one of them is sent", () => {
		const feature = decode(wireFeature({ teamForecasts: [{ teamId: 7 }] }));

		expect(feature.teamForecasts[0].startPercentiles).toEqual([]);
		expect(feature.teamForecasts[0].completionPercentiles).toEqual([]);
	});
});
