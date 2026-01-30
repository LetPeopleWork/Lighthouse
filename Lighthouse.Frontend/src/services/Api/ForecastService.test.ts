import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
	BacktestResult,
	type IBacktestResult,
} from "../../models/Forecasts/BacktestResult";
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

	describe("runItemPrediction", () => {
		it("should run an item prediction for a team with work item types", async () => {
			const teamId = 1;
			const startDate = new Date("2023-09-01");
			const endDate = new Date("2023-09-30");
			const targetDate = new Date("2023-10-15");
			const workItemTypes = ["Bug", "Feature", "Task"];

			const whenForecast = new WhenForecast();
			whenForecast.probability = 0.85;
			whenForecast.expectedDate = new Date("2023-10-20T00:00:00Z");

			const mockResponse: IManualForecast = new ManualForecast(
				15,
				targetDate,
				[whenForecast],
				[new HowManyForecast(0.85, 12)],
				0.92,
			);

			mockedAxios.post.mockResolvedValueOnce({ data: mockResponse });

			const result = await forecastService.runItemPrediction(
				teamId,
				startDate,
				endDate,
				targetDate,
				workItemTypes,
			);

			const expectedWhenForecast = new WhenForecast();
			expectedWhenForecast.probability = 0.85;
			expectedWhenForecast.expectedDate = new Date("2023-10-20T00:00:00Z");

			expect(result).toEqual(
				new ManualForecast(
					15,
					new Date("2023-10-15T00:00:00Z"),
					[expectedWhenForecast],
					[new HowManyForecast(0.85, 12)],
					0.92,
				),
			);
			expect(mockedAxios.post).toHaveBeenCalledWith(
				`/forecast/itemprediction/${teamId}`,
				{
					startDate,
					endDate,
					targetDate,
					workItemTypes,
				},
			);
		});

		it("should run an item prediction with empty work item types array", async () => {
			const teamId = 2;
			const startDate = new Date("2023-08-01");
			const endDate = new Date("2023-08-31");
			const targetDate = new Date("2023-09-15");
			const workItemTypes: string[] = [];

			const mockResponse: IManualForecast = new ManualForecast(
				8,
				targetDate,
				[],
				[],
				0.75,
			);

			mockedAxios.post.mockResolvedValueOnce({ data: mockResponse });

			const result = await forecastService.runItemPrediction(
				teamId,
				startDate,
				endDate,
				targetDate,
				workItemTypes,
			);

			expect(result).toEqual(
				new ManualForecast(8, new Date("2023-09-15T00:00:00Z"), [], [], 0.75),
			);
			expect(mockedAxios.post).toHaveBeenCalledWith(
				`/forecast/itemprediction/${teamId}`,
				{
					startDate,
					endDate,
					targetDate,
					workItemTypes,
				},
			);
		});

		it("should run an item prediction with multiple work item types", async () => {
			const teamId = 3;
			const startDate = new Date("2023-07-01");
			const endDate = new Date("2023-07-31");
			const targetDate = new Date("2023-08-30");
			const workItemTypes = ["User Story", "Bug", "Epic", "Task", "Test Case"];

			const whenForecasts = [
				Object.assign(new WhenForecast(), {
					probability: 0.5,
					expectedDate: new Date("2023-08-25T00:00:00Z"),
				}),
				Object.assign(new WhenForecast(), {
					probability: 0.8,
					expectedDate: new Date("2023-08-30T00:00:00Z"),
				}),
				Object.assign(new WhenForecast(), {
					probability: 0.95,
					expectedDate: new Date("2023-09-05T00:00:00Z"),
				}),
			];

			const howManyForecasts = [
				new HowManyForecast(0.5, 20),
				new HowManyForecast(0.8, 25),
				new HowManyForecast(0.95, 30),
			];

			const mockResponse: IManualForecast = new ManualForecast(
				22,
				targetDate,
				whenForecasts,
				howManyForecasts,
				0.88,
			);

			mockedAxios.post.mockResolvedValueOnce({ data: mockResponse });

			const result = await forecastService.runItemPrediction(
				teamId,
				startDate,
				endDate,
				targetDate,
				workItemTypes,
			);

			const expectedWhenForecasts = [
				Object.assign(new WhenForecast(), {
					probability: 0.5,
					expectedDate: new Date("2023-08-25T00:00:00Z"),
				}),
				Object.assign(new WhenForecast(), {
					probability: 0.8,
					expectedDate: new Date("2023-08-30T00:00:00Z"),
				}),
				Object.assign(new WhenForecast(), {
					probability: 0.95,
					expectedDate: new Date("2023-09-05T00:00:00Z"),
				}),
			];

			expect(result).toEqual(
				new ManualForecast(
					22,
					new Date("2023-08-30T00:00:00Z"),
					expectedWhenForecasts,
					howManyForecasts,
					0.88,
				),
			);
			expect(mockedAxios.post).toHaveBeenCalledWith(
				`/forecast/itemprediction/${teamId}`,
				{
					startDate,
					endDate,
					targetDate,
					workItemTypes,
				},
			);
		});

		it("should handle single work item type", async () => {
			const teamId = 4;
			const startDate = new Date("2023-06-01");
			const endDate = new Date("2023-06-30");
			const targetDate = new Date("2023-07-15");
			const workItemTypes = ["Bug"];

			const whenForecast = new WhenForecast();
			whenForecast.probability = 0.9;
			whenForecast.expectedDate = new Date("2023-07-10T00:00:00Z");

			const mockResponse: IManualForecast = new ManualForecast(
				5,
				targetDate,
				[whenForecast],
				[new HowManyForecast(0.9, 4)],
				0.95,
			);

			mockedAxios.post.mockResolvedValueOnce({ data: mockResponse });

			const result = await forecastService.runItemPrediction(
				teamId,
				startDate,
				endDate,
				targetDate,
				workItemTypes,
			);

			const expectedWhenForecast = new WhenForecast();
			expectedWhenForecast.probability = 0.9;
			expectedWhenForecast.expectedDate = new Date("2023-07-10T00:00:00Z");

			expect(result).toEqual(
				new ManualForecast(
					5,
					new Date("2023-07-15T00:00:00Z"),
					[expectedWhenForecast],
					[new HowManyForecast(0.9, 4)],
					0.95,
				),
			);
			expect(mockedAxios.post).toHaveBeenCalledWith(
				`/forecast/itemprediction/${teamId}`,
				{
					startDate,
					endDate,
					targetDate,
					workItemTypes,
				},
			);
		});

		it("should handle date range where start date equals end date", async () => {
			const teamId = 5;
			const sameDate = new Date("2023-05-15");
			const startDate = sameDate;
			const endDate = sameDate;
			const targetDate = new Date("2023-06-01");
			const workItemTypes = ["Feature"];

			const mockResponse: IManualForecast = new ManualForecast(
				3,
				targetDate,
				[],
				[new HowManyForecast(0.6, 2)],
				0.7,
			);

			mockedAxios.post.mockResolvedValueOnce({ data: mockResponse });

			const result = await forecastService.runItemPrediction(
				teamId,
				startDate,
				endDate,
				targetDate,
				workItemTypes,
			);

			expect(result).toEqual(
				new ManualForecast(
					3,
					new Date("2023-06-01T00:00:00Z"),
					[],
					[new HowManyForecast(0.6, 2)],
					0.7,
				),
			);
			expect(mockedAxios.post).toHaveBeenCalledWith(
				`/forecast/itemprediction/${teamId}`,
				{
					startDate,
					endDate,
					targetDate,
					workItemTypes,
				},
			);
		});

		it("should throw an error if item prediction API call fails", async () => {
			const teamId = 6;
			const startDate = new Date("2023-04-01");
			const endDate = new Date("2023-04-30");
			const targetDate = new Date("2023-05-15");
			const workItemTypes = ["Bug", "Feature"];

			mockedAxios.post.mockRejectedValueOnce(
				new Error("Item prediction API error"),
			);

			await expect(
				forecastService.runItemPrediction(
					teamId,
					startDate,
					endDate,
					targetDate,
					workItemTypes,
				),
			).rejects.toThrow("Item prediction API error");

			expect(mockedAxios.post).toHaveBeenCalledWith(
				`/forecast/itemprediction/${teamId}`,
				{
					startDate,
					endDate,
					targetDate,
					workItemTypes,
				},
			);
		});

		it("should handle API response with zero remaining items", async () => {
			const teamId = 7;
			const startDate = new Date("2023-03-01");
			const endDate = new Date("2023-03-31");
			const targetDate = new Date("2023-04-15");
			const workItemTypes = ["Task"];

			const mockResponse: IManualForecast = new ManualForecast(
				0,
				targetDate,
				[],
				[],
				1,
			);

			mockedAxios.post.mockResolvedValueOnce({ data: mockResponse });

			const result = await forecastService.runItemPrediction(
				teamId,
				startDate,
				endDate,
				targetDate,
				workItemTypes,
			);

			expect(result).toEqual(
				new ManualForecast(0, new Date("2023-04-15T00:00:00Z"), [], [], 1),
			);
			expect(mockedAxios.post).toHaveBeenCalledWith(
				`/forecast/itemprediction/${teamId}`,
				{
					startDate,
					endDate,
					targetDate,
					workItemTypes,
				},
			);
		});
	});

	describe("runBacktest", () => {
		it("should run a backtest for a team with valid parameters", async () => {
			const teamId = 1;
			const startDate = new Date("2023-06-01");
			const endDate = new Date("2023-06-30");
			const historicalWindowDays = 30;

			const mockResponse: IBacktestResult = {
				startDate: "2023-06-01",
				endDate: "2023-06-30",
				historicalWindowDays: 30,
				percentiles: [
					{ probability: 50, value: 10 },
					{ probability: 70, value: 12 },
					{ probability: 85, value: 15 },
					{ probability: 95, value: 18 },
				],
				actualThroughput: 12,
			};

			mockedAxios.post.mockResolvedValueOnce({ data: mockResponse });

			const result = await forecastService.runBacktest(
				teamId,
				startDate,
				endDate,
				historicalWindowDays,
			);

			expect(result).toBeInstanceOf(BacktestResult);
			expect(result.startDate).toEqual(new Date("2023-06-01"));
			expect(result.endDate).toEqual(new Date("2023-06-30"));
			expect(result.historicalWindowDays).toBe(30);
			expect(result.percentiles).toHaveLength(4);
			expect(result.percentiles[0]).toBeInstanceOf(HowManyForecast);
			expect(result.percentiles[0].probability).toBe(50);
			expect(result.percentiles[0].value).toBe(10);
			expect(result.actualThroughput).toBe(12);

			expect(mockedAxios.post).toHaveBeenCalledWith(
				`/forecast/backtest/${teamId}`,
				{
					startDate: "2023-06-01",
					endDate: "2023-06-30",
					historicalWindowDays,
				},
			);
		});

		it("should format dates as date-only strings", async () => {
			const teamId = 2;
			const startDate = new Date("2023-07-15T14:30:00Z");
			const endDate = new Date("2023-08-15T09:00:00Z");
			const historicalWindowDays = 45;

			const mockResponse: IBacktestResult = {
				startDate: "2023-07-15",
				endDate: "2023-08-15",
				historicalWindowDays: 45,
				percentiles: [{ probability: 50, value: 8 }],
				actualThroughput: 9,
			};

			mockedAxios.post.mockResolvedValueOnce({ data: mockResponse });

			await forecastService.runBacktest(
				teamId,
				startDate,
				endDate,
				historicalWindowDays,
			);

			expect(mockedAxios.post).toHaveBeenCalledWith(
				`/forecast/backtest/${teamId}`,
				{
					startDate: "2023-07-15",
					endDate: "2023-08-15",
					historicalWindowDays,
				},
			);
		});

		it("should handle empty percentiles array", async () => {
			const teamId = 3;
			const startDate = new Date("2023-05-01");
			const endDate = new Date("2023-05-31");
			const historicalWindowDays = 30;

			const mockResponse: IBacktestResult = {
				startDate: "2023-05-01",
				endDate: "2023-05-31",
				historicalWindowDays: 30,
				percentiles: [],
				actualThroughput: 0,
			};

			mockedAxios.post.mockResolvedValueOnce({ data: mockResponse });

			const result = await forecastService.runBacktest(
				teamId,
				startDate,
				endDate,
				historicalWindowDays,
			);

			expect(result.percentiles).toEqual([]);
			expect(result.actualThroughput).toBe(0);
		});

		it("should throw an error if backtest API call fails", async () => {
			const teamId = 4;
			const startDate = new Date("2023-04-01");
			const endDate = new Date("2023-04-30");
			const historicalWindowDays = 30;

			mockedAxios.post.mockRejectedValueOnce(new Error("Backtest API error"));

			await expect(
				forecastService.runBacktest(
					teamId,
					startDate,
					endDate,
					historicalWindowDays,
				),
			).rejects.toThrow("Backtest API error");

			expect(mockedAxios.post).toHaveBeenCalledWith(
				`/forecast/backtest/${teamId}`,
				{
					startDate: "2023-04-01",
					endDate: "2023-04-30",
					historicalWindowDays,
				},
			);
		});

		it("should deserialize all percentile forecasts correctly", async () => {
			const teamId = 5;
			const startDate = new Date("2023-03-01");
			const endDate = new Date("2023-03-31");
			const historicalWindowDays = 60;

			const mockResponse: IBacktestResult = {
				startDate: "2023-03-01",
				endDate: "2023-03-31",
				historicalWindowDays: 60,
				percentiles: [
					{ probability: 50, value: 5 },
					{ probability: 70, value: 7 },
					{ probability: 85, value: 10 },
					{ probability: 95, value: 14 },
				],
				actualThroughput: 8,
			};

			mockedAxios.post.mockResolvedValueOnce({ data: mockResponse });

			const result = await forecastService.runBacktest(
				teamId,
				startDate,
				endDate,
				historicalWindowDays,
			);

			expect(result.percentiles).toHaveLength(4);
			expect(result.percentiles[0].probability).toBe(50);
			expect(result.percentiles[0].value).toBe(5);
			expect(result.percentiles[1].probability).toBe(70);
			expect(result.percentiles[1].value).toBe(7);
			expect(result.percentiles[2].probability).toBe(85);
			expect(result.percentiles[2].value).toBe(10);
			expect(result.percentiles[3].probability).toBe(95);
			expect(result.percentiles[3].value).toBe(14);
		});
	});
});
