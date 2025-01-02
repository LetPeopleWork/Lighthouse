import { describe, expect, it } from "vitest";
import type { IHowManyForecast } from "./HowManyForecast";
import { type IManualForecast, ManualForecast } from "./ManualForecast";
import type { IWhenForecast } from "./WhenForecast";

describe("ManualForecast", () => {
	const mockWhenForecast: IWhenForecast = {
		probability: 75,
		expectedDate: new Date("2025-01-01"),
	};

	const mockHowManyForecast: IHowManyForecast = {
		probability: 80,
		expectedItems: 100,
	};

	it("should create a ManualForecast with the correct properties", () => {
		const whenForecasts: IWhenForecast[] = [mockWhenForecast];
		const howManyForecasts: IHowManyForecast[] = [mockHowManyForecast];
		const likelihood = 85;
		const forecast = new ManualForecast(
			12,
			new Date(),
			whenForecasts,
			howManyForecasts,
			likelihood,
		);

		expect(forecast.whenForecasts).toBe(whenForecasts);
		expect(forecast.howManyForecasts).toBe(howManyForecasts);
		expect(forecast.likelihood).toBe(likelihood);
	});

	it("should set a default likelihood of 0 if not provided", () => {
		const whenForecasts: IWhenForecast[] = [mockWhenForecast];
		const howManyForecasts: IHowManyForecast[] = [mockHowManyForecast];
		const forecast = new ManualForecast(
			12,
			new Date(),
			whenForecasts,
			howManyForecasts,
		);

		expect(forecast.likelihood).toBe(0);
	});

	it("should implement the IManualForecast interface", () => {
		const whenForecasts: IWhenForecast[] = [mockWhenForecast];
		const howManyForecasts: IHowManyForecast[] = [mockHowManyForecast];
		const forecast: IManualForecast = new ManualForecast(
			12,
			new Date(),
			whenForecasts,
			howManyForecasts,
			85,
		);

		expect(forecast).toBeInstanceOf(ManualForecast);
		expect(forecast).toHaveProperty("remainingItems");
		expect(forecast).toHaveProperty("targetDate");
		expect(forecast).toHaveProperty("whenForecasts");
		expect(forecast).toHaveProperty("howManyForecasts");
		expect(forecast).toHaveProperty("likelihood");
	});
});
