import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { Throughput } from "../../models/Forecasts/Throughput";
import type { IWorkItem } from "../../models/WorkItem";
import { TeamMetricsService } from "./TeamMetricsService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

describe("TeamMetricsService", () => {
	let teamMetricsService: TeamMetricsService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		teamMetricsService = new TeamMetricsService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("should get throughput for a team", async () => {
		const mockThroughputData = {
			throughputPerUnitOfTime: 5,
			history: [3, 4, 5, 6, 7],
			totalThroughput: 25,
		};

		mockedAxios.get.mockResolvedValueOnce({ data: mockThroughputData });

		const result = await teamMetricsService.getThroughput(1);

		expect(result).toBeInstanceOf(Throughput);
		expect(result.throughputPerUnitOfTime).toBe(5);
		expect(result.history).toEqual([3, 4, 5, 6, 7]);
		expect(result.totalThroughput).toBe(25);
		expect(mockedAxios.get).toHaveBeenCalledWith("/teams/1/metrics/throughput");
	});

	it("should get features in progress for a team", async () => {
		const mockFeaturesInProgress = ["Feature A", "Feature B", "Feature C"];

		mockedAxios.get.mockResolvedValueOnce({ data: mockFeaturesInProgress });

		const result = await teamMetricsService.getFeaturesInProgress(1);

		expect(result).toEqual(["Feature A", "Feature B", "Feature C"]);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/1/metrics/featuresInProgress",
		);
	});

	it("should get work items for a team", async () => {
		const startDate = new Date("2023-01-01");
		const closedDate = new Date("2023-01-10");

		const mockWorkItems = [
			{
				id: 1,
				workItemReference: "Work Item 1",
				url: "",
				name: "Work Item 1",
				startedDate: startDate.toISOString(),
				closedDate: closedDate.toISOString(),
			},
			{
				id: 2,
				workItemReference: "Work Item 2",
				url: "",
				name: "Work Item 2",
				startedDate: startDate.toISOString(),
				closedDate: closedDate.toISOString(),
			},
		];

		const expectedWorkItems: IWorkItem[] = [
			{
				id: 1,
				workItemReference: "Work Item 1",
				url: "",
				name: "Work Item 1",
				startedDate: startDate,
				closedDate: closedDate,
			},
			{
				id: 2,
				workItemReference: "Work Item 2",
				url: "",
				name: "Work Item 2",
				startedDate: startDate,
				closedDate: closedDate,
			},
		];

		mockedAxios.get.mockResolvedValueOnce({ data: mockWorkItems });

		const result = await teamMetricsService.getWorkItems(1);

		expect(result).toEqual(expectedWorkItems);
		expect(result[0].startedDate).toBeInstanceOf(Date);
		expect(result[0].closedDate).toBeInstanceOf(Date);
		expect(mockedAxios.get).toHaveBeenCalledWith("/teams/1/workitems");
	});

	it("should handle errors when getting throughput", async () => {
		const errorMessage = "Network Error";
		mockedAxios.get.mockRejectedValueOnce(new Error(errorMessage));

		await expect(teamMetricsService.getThroughput(1)).rejects.toThrow(
			errorMessage,
		);
	});

	it("should handle errors when getting features in progress", async () => {
		const errorMessage = "Network Error";
		mockedAxios.get.mockRejectedValueOnce(new Error(errorMessage));

		await expect(teamMetricsService.getFeaturesInProgress(1)).rejects.toThrow(
			errorMessage,
		);
	});

	it("should handle errors when getting work items", async () => {
		const errorMessage = "Network Error";
		mockedAxios.get.mockRejectedValueOnce(new Error(errorMessage));

		await expect(teamMetricsService.getWorkItems(1)).rejects.toThrow(
			errorMessage,
		);
	});
});
