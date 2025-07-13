import { describe, expect, it } from "vitest";
import type { IPercentileValue } from "../PercentileValue";
import {
	ForecastPredictabilityScore,
	type IForecastPredictabilityScore,
} from "./ForecastPredictabilityScore";

describe("ForecastPredictabilityScore", () => {
	const mockPercentiles: IPercentileValue[] = [
		{ percentile: 50, value: 5 },
		{ percentile: 70, value: 7 },
		{ percentile: 85, value: 10 },
		{ percentile: 95, value: 15 },
	];

	const mockForecastResults = new Map<number, number>([
		[3, 1],
		[5, 5],
		[7, 12],
		[10, 6],
		[15, 1],
	]);

	const predictabilityScore = 0.75;

	it("should create a ForecastPredictabilityScore with the correct properties", () => {
		const forecast = new ForecastPredictabilityScore(
			mockPercentiles,
			predictabilityScore,
			mockForecastResults,
		);

		expect(forecast.percentiles).toBe(mockPercentiles);
		expect(forecast.predictabilityScore).toBe(predictabilityScore);
		expect(forecast.forecastResults).toBe(mockForecastResults);
	});

	it("should implement the IForecastPredictabilityScore interface", () => {
		const forecast: IForecastPredictabilityScore =
			new ForecastPredictabilityScore(
				mockPercentiles,
				predictabilityScore,
				mockForecastResults,
			);

		expect(forecast).toBeInstanceOf(ForecastPredictabilityScore);
		expect(forecast).toHaveProperty("percentiles");
		expect(forecast).toHaveProperty("predictabilityScore");
		expect(forecast).toHaveProperty("forecastResults");
	});

	it("should handle empty percentiles array", () => {
		const emptyPercentiles: IPercentileValue[] = [];
		const forecast = new ForecastPredictabilityScore(
			emptyPercentiles,
			0.5,
			mockForecastResults,
		);

		expect(forecast.percentiles).toEqual(emptyPercentiles);
		expect(forecast.percentiles.length).toBe(0);
		expect(forecast.predictabilityScore).toBe(0.5);
		expect(forecast.forecastResults).toBe(mockForecastResults);
	});

	it("should handle empty forecast results map", () => {
		const emptyResults = new Map<number, number>();
		const forecast = new ForecastPredictabilityScore(
			mockPercentiles,
			0.3,
			emptyResults,
		);

		expect(forecast.percentiles).toBe(mockPercentiles);
		expect(forecast.predictabilityScore).toBe(0.3);
		expect(forecast.forecastResults).toBe(emptyResults);
		expect(forecast.forecastResults.size).toBe(0);
	});

	it("should handle zero predictability score", () => {
		const forecast = new ForecastPredictabilityScore(
			mockPercentiles,
			0,
			mockForecastResults,
		);

		expect(forecast.predictabilityScore).toBe(0);
		expect(forecast.percentiles).toBe(mockPercentiles);
		expect(forecast.forecastResults).toBe(mockForecastResults);
	});

	it("should handle maximum predictability score", () => {
		const forecast = new ForecastPredictabilityScore(
			mockPercentiles,
			1,
			mockForecastResults,
		);

		expect(forecast.predictabilityScore).toBe(1);
		expect(forecast.percentiles).toBe(mockPercentiles);
		expect(forecast.forecastResults).toBe(mockForecastResults);
	});

	it("should preserve the exact Map instance passed to constructor", () => {
		const specificMap = new Map<number, number>([
			[1, 10],
			[2, 20],
			[3, 30],
		]);

		const forecast = new ForecastPredictabilityScore(
			mockPercentiles,
			0.6,
			specificMap,
		);

		expect(forecast.forecastResults).toBe(specificMap);
		expect(forecast.forecastResults.get(1)).toBe(10);
		expect(forecast.forecastResults.get(2)).toBe(20);
		expect(forecast.forecastResults.get(3)).toBe(30);
	});

	it("should preserve the exact percentiles array instance passed to constructor", () => {
		const specificPercentiles: IPercentileValue[] = [
			{ percentile: 25, value: 2 },
			{ percentile: 50, value: 4 },
			{ percentile: 75, value: 6 },
		];

		const forecast = new ForecastPredictabilityScore(
			specificPercentiles,
			0.8,
			mockForecastResults,
		);

		expect(forecast.percentiles).toBe(specificPercentiles);
		expect(forecast.percentiles[0].percentile).toBe(25);
		expect(forecast.percentiles[0].value).toBe(2);
		expect(forecast.percentiles[1].percentile).toBe(50);
		expect(forecast.percentiles[1].value).toBe(4);
		expect(forecast.percentiles[2].percentile).toBe(75);
		expect(forecast.percentiles[2].value).toBe(6);
	});

	it("should handle negative predictability scores", () => {
		const forecast = new ForecastPredictabilityScore(
			mockPercentiles,
			-0.1,
			mockForecastResults,
		);

		expect(forecast.predictabilityScore).toBe(-0.1);
		expect(forecast.percentiles).toBe(mockPercentiles);
		expect(forecast.forecastResults).toBe(mockForecastResults);
	});

	it("should handle predictability scores greater than 1", () => {
		const forecast = new ForecastPredictabilityScore(
			mockPercentiles,
			1.5,
			mockForecastResults,
		);

		expect(forecast.predictabilityScore).toBe(1.5);
		expect(forecast.percentiles).toBe(mockPercentiles);
		expect(forecast.forecastResults).toBe(mockForecastResults);
	});

	it("should handle fractional predictability scores with precision", () => {
		const preciseScore = 0.123456789;
		const forecast = new ForecastPredictabilityScore(
			mockPercentiles,
			preciseScore,
			mockForecastResults,
		);

		expect(forecast.predictabilityScore).toBe(preciseScore);
	});

	it("should maintain immutability of constructor parameters", () => {
		const originalPercentiles: IPercentileValue[] = [
			{ percentile: 50, value: 5 },
		];
		const originalResults = new Map<number, number>([[1, 1]]);
		const originalScore = 0.5;

		const forecast = new ForecastPredictabilityScore(
			originalPercentiles,
			originalScore,
			originalResults,
		);

		// Modify original data
		originalPercentiles.push({ percentile: 90, value: 10 });
		originalResults.set(2, 2);

		// Forecast should still reference the original instances
		expect(forecast.percentiles).toBe(originalPercentiles);
		expect(forecast.forecastResults).toBe(originalResults);
		expect(forecast.percentiles.length).toBe(2); // Modified array
		expect(forecast.forecastResults.size).toBe(2); // Modified map
	});
});
