import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { HowManyForecast } from "../../models/Forecasts/HowManyForecast";
import {
	type IManualForecast,
	ManualForecast,
} from "../../models/Forecasts/ManualForecast";
import { WhenForecast } from "../../models/Forecasts/WhenForecast";
import { ForecastService } from "./ForecastService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

describe("ForecastService", () => {
	let forecastService: ForecastService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		forecastService = new ForecastService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("should run a manual forecast for a team", async () => {
		const teamId = 1;
		const remainingItems = 10;
		const targetDate = new Date("2023-10-01");

		const whenForecast = new WhenForecast();
		whenForecast.probability = 0.75;
		whenForecast.expectedDate = new Date("2023-10-15T00:00:00Z");

		const mockResponse: IManualForecast = new ManualForecast(
			remainingItems,
			targetDate,
			[whenForecast],
			[new HowManyForecast(0.75, 8)],
			0.9,
		);

		mockedAxios.post.mockResolvedValueOnce({ data: mockResponse });

		const result = await forecastService.runManualForecast(
			teamId,
			remainingItems,
			targetDate,
		);

		const expectedWhenForecast = new WhenForecast();
		expectedWhenForecast.probability = 0.75;
		expectedWhenForecast.expectedDate = new Date("2023-10-15T00:00:00Z");

		expect(result).toEqual(
			new ManualForecast(
				10,
				new Date("2023-10-01T00:00:00Z"),
				[expectedWhenForecast],
				[new HowManyForecast(0.75, 8)],
				0.9,
			),
		);
		expect(mockedAxios.post).toHaveBeenCalledWith(
			`/forecast/manual/${teamId}`,
			{
				remainingItems,
				targetDate,
			},
		);
	});

	it("should handle empty whenForecasts and howManyForecasts", async () => {
		const teamId = 2;
		const remainingItems = 5;
		const targetDate = new Date("2023-12-01");

		const mockResponse: IManualForecast = new ManualForecast(
			remainingItems,
			targetDate,
			[],
			[],
			0.85,
		);

		mockedAxios.post.mockResolvedValueOnce({ data: mockResponse });

		const result = await forecastService.runManualForecast(
			teamId,
			remainingItems,
			targetDate,
		);

		expect(result).toEqual(
			new ManualForecast(5, new Date("2023-12-01T00:00:00Z"), [], [], 0.85),
		);
		expect(mockedAxios.post).toHaveBeenCalledWith(
			`/forecast/manual/${teamId}`,
			{
				remainingItems,
				targetDate,
			},
		);
	});

	it("should throw an error if API call fails", async () => {
		const teamId = 3;
		const remainingItems = 12;
		const targetDate = new Date("2023-11-01");

		mockedAxios.post.mockRejectedValueOnce(new Error("API error"));

		await expect(
			forecastService.runManualForecast(teamId, remainingItems, targetDate),
		).rejects.toThrow("API error");

		expect(mockedAxios.post).toHaveBeenCalledWith(
			`/forecast/manual/${teamId}`,
			{
				remainingItems,
				targetDate,
			},
		);
	});
});
