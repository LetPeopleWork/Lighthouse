import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ForecastService } from "../../services/Api/ForecastService";
import { Feature, FeatureSchema } from "../Feature";
import { type IWhenForecast, WhenForecast } from "./WhenForecast";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

const pendingEpic6311 =
	"Pending: every percentile carries whether it is beyond the horizon (epic 6311, slice 03)";

/**
 * Every screen that words a percentile reads it from a model the client rebuilt from the wire. Each
 * rebuild has to carry the flag and the horizon's years across, or a percentile beyond the horizon turns
 * back into its stand-in date somewhere between the payload and the page.
 */
describe("a percentile beyond the horizon, as the client rebuilds it", () => {
	const onTheWire = [
		{
			probability: 50,
			expectedDate: "2029-03-14T00:00:00Z",
			beyondHorizon: false,
			horizonYears: 5,
		},
		{
			probability: 85,
			expectedDate: "2031-10-18T00:00:00Z",
			beyondHorizon: true,
			horizonYears: 5,
		},
	];

	const wireFeature = (overrides: Record<string, unknown>) => ({
		name: "Billing Migration",
		id: 42,
		referenceId: "PAY-1",
		state: "In Progress",
		type: "Feature",
		stateCategory: "Doing",
		lastUpdated: "2026-10-18T08:00:00Z",
		startedDate: null,
		closedDate: null,
		cycleTime: 0,
		workItemAge: 3,
		size: 480,
		owningTeam: "Payments",
		isUsingDefaultFeatureSize: false,
		parentWorkItemReference: "",
		remainingWork: { "1": 480 },
		totalWork: { "1": 500 },
		forecasts: [],
		...overrides,
	});

	const decode = (overrides: Record<string, unknown>): Feature =>
		Feature.fromParsed(FeatureSchema.parse(wireFeature(overrides)));

	const readAs = (percentiles: IWhenForecast[]) =>
		percentiles.map(({ probability, beyondHorizon, horizonYears }) => ({
			probability,
			beyondHorizon,
			horizonYears,
		}));

	const asSent = [
		{ probability: 50, beyondHorizon: false, horizonYears: 5 },
		{ probability: 85, beyondHorizon: true, horizonYears: 5 },
	];

	const theFeatureRebuilds: [string, () => IWhenForecast[]][] = [
		[
			"the Feature's forecast",
			() => decode({ forecasts: onTheWire }).forecasts,
		],
		[
			"the Feature's start forecast",
			() =>
				decode({
					startForecast: { source: "Forecast", percentiles: onTheWire },
				}).startForecast?.percentiles ?? [],
		],
		[
			"a Team's share of the Feature",
			() =>
				decode({
					teamForecasts: [
						{
							teamId: 1,
							startPercentiles: [],
							completionPercentiles: onTheWire,
						},
					],
				}).teamForecasts[0].completionPercentiles,
		],
		[
			"a percentile read straight from the backend",
			() =>
				onTheWire.map((percentile) =>
					WhenForecast.fromBackend(percentile as unknown as IWhenForecast),
				),
		],
	];

	it.skip.each(theFeatureRebuilds)(
		`%s keeps the flag and the years (${pendingEpic6311})`,
		(_, rebuilt) => {
			expect(readAs(rebuilt())).toEqual(asSent);
		},
	);

	describe("a forecast asked for by hand", () => {
		beforeEach(() => {
			mockedAxios.create.mockReturnThis();
		});

		afterEach(() => {
			vi.resetAllMocks();
		});

		it.skip(`keeps the flag and the years (${pendingEpic6311})`, async () => {
			mockedAxios.post.mockResolvedValueOnce({
				data: {
					remainingItems: 480,
					targetDate: "2027-01-01T00:00:00Z",
					whenForecasts: onTheWire,
					howManyForecasts: [],
					likelihood: 0,
				},
			});

			const result = await new ForecastService().runManualForecast(
				1,
				480,
				new Date("2027-01-01T00:00:00Z"),
			);

			expect(readAs(result.whenForecasts)).toEqual(asSent);
		});
	});

	/** Already true; the guard that an older backend, which sends no flag, still reads as dates. */
	it("reads a percentile from a backend that sends no flag as one with a date", () => {
		const feature = decode({
			forecasts: [{ probability: 85, expectedDate: "2027-02-01T00:00:00Z" }],
		});

		expect(feature.forecasts[0].beyondHorizon ?? false).toBe(false);
		expect(feature.forecasts[0].expectedDate).toEqual(
			new Date("2027-02-01T00:00:00Z"),
		);
	});
});
