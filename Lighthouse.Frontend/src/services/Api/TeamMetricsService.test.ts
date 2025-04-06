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

		const startDate = new Date("2023-01-01");
		const endDate = new Date("2023-01-31");
		const result = await teamMetricsService.getThroughput(
			1,
			startDate,
			endDate,
		);

		expect(result).toBeInstanceOf(Throughput);
		expect(result.throughputPerUnitOfTime).toBe(5);
		expect(result.history).toEqual([3, 4, 5, 6, 7]);
		expect(result.totalThroughput).toBe(25);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/1/metrics/throughput?startDate=2023-01-01&endDate=2023-01-31",
		);
	});

	it("should get features in progress for a team", async () => {
		const mockFeaturesInProgress = [
			createMockWorkItem("Feature A"),
			createMockWorkItem("Feature B"),
			createMockWorkItem("Feature C"),
		];

		mockedAxios.get.mockResolvedValueOnce({ data: mockFeaturesInProgress });

		const result = await teamMetricsService.getFeaturesInProgress(1);

		expect(result).toHaveLength(3);
		expect(result[0].name).toBe("Feature A");
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/1/metrics/featuresInProgress",
		);
	});

	it("should get in-progress items for a team", async () => {
		const mockWorkItems: IWorkItem[] = [
			createMockWorkItem("Item A"),
			createMockWorkItem("Item B"),
		];

		mockedAxios.get.mockResolvedValueOnce({ data: mockWorkItems });

		const result = await teamMetricsService.getInProgressItems(1);

		expect(result).toHaveLength(2);
		expect(result[0].name).toBe("Item A");
		expect(result[0].startedDate).toBeInstanceOf(Date);
		expect(result[1].startedDate).toBeInstanceOf(Date);
		expect(result[0].closedDate).toBeInstanceOf(Date);
		expect(result[1].closedDate).toBeInstanceOf(Date);
		expect(mockedAxios.get).toHaveBeenCalledWith("/teams/1/metrics/wip");
	});

	it("should handle errors when getting in-progress items", async () => {
		const errorMessage = "Network Error";
		mockedAxios.get.mockRejectedValueOnce(new Error(errorMessage));

		await expect(teamMetricsService.getInProgressItems(1)).rejects.toThrow(
			errorMessage,
		);
	});

	it("should handle errors when getting throughput", async () => {
		const errorMessage = "Network Error";
		mockedAxios.get.mockRejectedValueOnce(new Error(errorMessage));

		await expect(
			teamMetricsService.getThroughput(1, new Date(), new Date()),
		).rejects.toThrow(errorMessage);
	});

	it("should handle errors when getting features in progress", async () => {
		const errorMessage = "Network Error";
		mockedAxios.get.mockRejectedValueOnce(new Error(errorMessage));

		await expect(teamMetricsService.getFeaturesInProgress(1)).rejects.toThrow(
			errorMessage,
		);
	});

	const createMockWorkItem = (name: string): IWorkItem => ({
		id: Math.floor(Math.random() * 1000),
		workItemReference: Math.floor(Math.random() * 1000).toString(),
		url: "",
		name,
		startedDate: new Date("2023-01-15"),
		closedDate: new Date("2023-01-20"),
		state: "In Progress",
		type: "Task",
	});
});
