import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
	BurndownEntry,
	LighthouseChartData,
	LighthouseChartFeatureData,
} from "../../models/Charts/LighthouseChartData";
import { Milestone } from "../../models/Project/Milestone";
import { ChartService } from "./ChartService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

describe("ChartService", () => {
	let chartService: ChartService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		chartService = new ChartService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("should get lighthouse chart data for a project", async () => {
		const projectId = 1;
		const startDate = new Date("2024-01-01");
		const sampleRate = 7;

		const mockResponse = {
			features: [
				{
					name: "Feature 1",
					remainingItemsTrend: [
						{ date: "2024-01-02T00:00:00Z", remainingItems: 10 },
					],
					forecasts: ["2024-01-15T00:00:00Z"],
				},
			],
			milestones: [
				{ id: 1, name: "Milestone 1", date: "2024-02-01T00:00:00Z" },
			],
		};

		mockedAxios.post.mockResolvedValueOnce({ data: mockResponse });

		const result = await chartService.getLighthouseChartData(
			projectId,
			startDate,
			sampleRate,
		);

		const expectedFeatureData = new LighthouseChartFeatureData(
			"Feature 1",
			[new Date("2024-01-15T00:00:00Z")],
			[new BurndownEntry(new Date("2024-01-02T00:00:00Z"), 10)],
		);

		// Manually set the color to match the expected value
		expectedFeatureData.color = result.features[0].color;

		expect(result).toEqual(
			new LighthouseChartData(
				[expectedFeatureData],
				[new Milestone(1, "Milestone 1", new Date("2024-02-01T00:00:00Z"))],
			),
		);
		expect(mockedAxios.post).toHaveBeenCalledWith(
			`/lighthousechart/${projectId}`,
			{
				startDate: startDate,
				sampleRate: sampleRate,
			},
		);
	});

	it("should handle empty features and milestones", async () => {
		const projectId = 2;
		const startDate = new Date("2024-01-01");
		const sampleRate = 4;

		const mockResponse = {
			features: [],
			milestones: [],
		};

		mockedAxios.post.mockResolvedValueOnce({ data: mockResponse });

		const result = await chartService.getLighthouseChartData(
			projectId,
			startDate,
			sampleRate,
		);

		expect(result).toEqual(new LighthouseChartData([], []));
		expect(mockedAxios.post).toHaveBeenCalledWith(
			`/lighthousechart/${projectId}`,
			{
				startDate: startDate,
				sampleRate: sampleRate,
			},
		);
	});

	it("should throw an error if API call fails", async () => {
		const projectId = 3;
		const startDate = new Date("2024-01-01");
		const sampleRate = 1;

		mockedAxios.post.mockRejectedValueOnce(new Error("API error"));

		await expect(
			chartService.getLighthouseChartData(projectId, startDate, sampleRate),
		).rejects.toThrow("API error");

		expect(mockedAxios.post).toHaveBeenCalledWith(
			`/lighthousechart/${projectId}`,
			{
				startDate: startDate,
				sampleRate: sampleRate,
			},
		);
	});
});
