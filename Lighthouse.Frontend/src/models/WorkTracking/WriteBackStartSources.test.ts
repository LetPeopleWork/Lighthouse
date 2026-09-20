import {
	FORECAST_SOURCES,
	PORTFOLIO_ONLY_SOURCES,
	TEAM_ONLY_SOURCES,
	VALUE_SOURCE_DISPLAY_NAMES,
	WriteBackValueSource,
} from "./WriteBackMappingDefinition";

const START_SOURCES = [
	WriteBackValueSource.ForecastedStartPercentile50,
	WriteBackValueSource.ForecastedStartPercentile70,
	WriteBackValueSource.ForecastedStartPercentile85,
	WriteBackValueSource.ForecastedStartPercentile95,
];

describe("the write-back sources for when work begins", () => {
	// The wire carries the member NAME, so these numbers are this app's own identity for a source
	// rather than something the backend reads. They are kept level with the backend's ordinals anyway,
	// because two enums that look like each other and disagree is a trap for whoever reads them next -
	// and because a number typed twice would quietly merge two sources into one.
	it.each([
		[WriteBackValueSource.ForecastedStartPercentile50, 7],
		[WriteBackValueSource.ForecastedStartPercentile70, 8],
		[WriteBackValueSource.ForecastedStartPercentile85, 9],
		[WriteBackValueSource.ForecastedStartPercentile95, 10],
	])("numbers %s as %i, level with the backend", (source, expected) => {
		expect(source).toBe(expected);
	});

	it("gives every source its own number", () => {
		const numbers = Object.values(WriteBackValueSource).filter(
			(value): value is number => typeof value === "number",
		);

		expect(new Set(numbers).size).toBe(numbers.length);
	});

	// Being a forecast source is what puts the value-type and date-format controls on screen. A start
	// date is written in exactly the same shapes a completion date is.
	it.each(START_SOURCES)("treats %s as a forecast source", (source) => {
		expect(FORECAST_SOURCES.has(source)).toBe(true);
	});

	// A Feature's start belongs to the Feature, so the question is asked of a portfolio. Asking it of a
	// team has no answer, the same way the completion percentiles do not.
	it.each(START_SOURCES)("offers %s for portfolios only", (source) => {
		expect(PORTFOLIO_ONLY_SOURCES.has(source)).toBe(true);
		expect(TEAM_ONLY_SOURCES.has(source)).toBe(false);
	});

	// What the administrator reads in the dropdown. Both ends are named for the end they mean: "Forecast"
	// alone said nothing about which one, which only became a question once both were on the list.
	it.each([
		[
			WriteBackValueSource.ForecastedStartPercentile50,
			"Forecasted Start (50th Percentile)",
		],
		[
			WriteBackValueSource.ForecastedStartPercentile70,
			"Forecasted Start (70th Percentile)",
		],
		[
			WriteBackValueSource.ForecastedStartPercentile85,
			"Forecasted Start (85th Percentile)",
		],
		[
			WriteBackValueSource.ForecastedStartPercentile95,
			"Forecasted Start (95th Percentile)",
		],
		[
			WriteBackValueSource.ForecastPercentile50,
			"Forecasted Completion (50th Percentile)",
		],
		[
			WriteBackValueSource.ForecastPercentile70,
			"Forecasted Completion (70th Percentile)",
		],
		[
			WriteBackValueSource.ForecastPercentile85,
			"Forecasted Completion (85th Percentile)",
		],
		[
			WriteBackValueSource.ForecastPercentile95,
			"Forecasted Completion (95th Percentile)",
		],
	])("names %s as %s", (source, expected) => {
		expect(VALUE_SOURCE_DISPLAY_NAMES[source]).toBe(expected);
	});
});
