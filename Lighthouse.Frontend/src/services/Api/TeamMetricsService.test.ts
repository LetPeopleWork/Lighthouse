import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { RunChartData } from "../../models/Metrics/RunChartData";
import type { IWorkItem } from "../../models/WorkItem";
import { generateWorkItemMapForRunChart } from "../../tests/TestDataProvider";
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
		const workItems = generateWorkItemMapForRunChart([3, 4, 5, 6, 7]);

		const mockThroughputData = {
			workItemsPerUnitOfTime: workItems,
			history: 5,
			total: 25,
		};

		mockedAxios.get.mockResolvedValueOnce({ data: mockThroughputData });

		const startDate = new Date("2023-01-01");
		const endDate = new Date("2023-01-31");
		const result = await teamMetricsService.getThroughput(
			1,
			startDate,
			endDate,
		);

		expect(result).toBeInstanceOf(RunChartData);
		expect(result.workItemsPerUnitOfTime).toEqual(workItems);
		expect(result.history).toBe(5);
		expect(result.total).toBe(25);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/1/metrics/throughput?startDate=2023-01-01&endDate=2023-01-31",
		);
	});

	it("should get started items for a team", async () => {
		const workItems = generateWorkItemMapForRunChart([3, 4, 5, 6, 7]);

		const mockStartedData = {
			workItemsPerUnitOfTime: workItems,
			history: 5,
			total: 25,
		};

		mockedAxios.get.mockResolvedValueOnce({ data: mockStartedData });

		const startDate = new Date("2023-01-01");
		const endDate = new Date("2023-01-31");
		const result = await teamMetricsService.getStartedItems(
			1,
			startDate,
			endDate,
		);

		expect(result).toBeInstanceOf(RunChartData);
		expect(result.workItemsPerUnitOfTime).toEqual(workItems);
		expect(result.history).toBe(5);
		expect(result.total).toBe(25);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/1/metrics/started?startDate=2023-01-01&endDate=2023-01-31",
		);
	});

	it("should get features in progress for a team", async () => {
		const mockFeaturesInProgress = [
			createMockWorkItem("Feature A"),
			createMockWorkItem("Feature B"),
			createMockWorkItem("Feature C"),
		];

		mockedAxios.get.mockResolvedValueOnce({ data: mockFeaturesInProgress });

		const result = await teamMetricsService.getFeaturesInProgress(
			1,
			new Date(2025, 5, 15),
		);

		expect(result).toHaveLength(3);
		expect(result[0].name).toBe("Feature A");
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/1/metrics/featuresInProgress?asOfDate=2025-06-15",
		);
	});

	it("should get in-progress items for a team", async () => {
		const mockWorkItems: IWorkItem[] = [
			createMockWorkItem("Item A"),
			createMockWorkItem("Item B"),
		];

		mockedAxios.get.mockResolvedValueOnce({ data: mockWorkItems });

		const result = await teamMetricsService.getInProgressItems(
			1,
			new Date(2025, 5, 15),
		);

		expect(result).toHaveLength(2);
		expect(result[0].name).toBe("Item A");
		expect(result[0].startedDate).toBeInstanceOf(Date);
		expect(result[1].startedDate).toBeInstanceOf(Date);
		expect(result[0].closedDate).toBeInstanceOf(Date);
		expect(result[1].closedDate).toBeInstanceOf(Date);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/1/metrics/wip?asOfDate=2025-06-15",
		);
	});

	it("should handle errors when getting in-progress items", async () => {
		const errorMessage = "Network Error";
		mockedAxios.get.mockRejectedValueOnce(new Error(errorMessage));

		await expect(
			teamMetricsService.getInProgressItems(1, new Date()),
		).rejects.toThrow(errorMessage);
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

		await expect(
			teamMetricsService.getFeaturesInProgress(1, new Date()),
		).rejects.toThrow(errorMessage);
	});

	it("should get total work item age for a team", async () => {
		const mockTotalAge = 157;

		mockedAxios.get.mockResolvedValueOnce({ data: mockTotalAge });

		const result = await teamMetricsService.getTotalWorkItemAge(
			1,
			new Date(2025, 5, 15),
		);

		expect(result).toBe(157);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/1/metrics/totalWorkItemAge?asOfDate=2025-06-15",
		);
	});

	it("should handle errors when getting total work item age", async () => {
		const errorMessage = "Network Error";
		mockedAxios.get.mockRejectedValueOnce(new Error(errorMessage));

		await expect(
			teamMetricsService.getTotalWorkItemAge(1, new Date()),
		).rejects.toThrow(errorMessage);
	});

	it("should get throughput info for a team", async () => {
		const mockInfo = {
			total: 4,
			dailyAverage: 0.4,
			comparison: {
				direction: "up",
				metricLabel: "Total Throughput",
				currentLabel: "2026-04-05 – 2026-04-14",
				currentValue: "4",
				previousLabel: "2026-03-26 – 2026-04-04",
				previousValue: "2",
				percentageDelta: "+100.0%",
			},
		};

		mockedAxios.get.mockResolvedValueOnce({ data: mockInfo });

		const startDate = new Date("2026-04-05");
		const endDate = new Date("2026-04-14");
		const result = await teamMetricsService.getThroughputInfo(
			1,
			startDate,
			endDate,
		);

		expect(result.total).toBe(4);
		expect(result.comparison.direction).toBe("up");
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/1/metrics/throughputInfo?startDate=2026-04-05&endDate=2026-04-14",
		);
	});

	it("should get arrivals info for a team", async () => {
		const mockInfo = {
			total: 3,
			dailyAverage: 0.3,
			comparison: {
				direction: "down",
				metricLabel: "Total Arrivals",
				currentLabel: "2026-04-05 – 2026-04-14",
				currentValue: "3",
				previousLabel: "2026-03-26 – 2026-04-04",
				previousValue: "5",
				percentageDelta: "-40.0%",
			},
		};

		mockedAxios.get.mockResolvedValueOnce({ data: mockInfo });

		const startDate = new Date("2026-04-05");
		const endDate = new Date("2026-04-14");
		const result = await teamMetricsService.getArrivalsInfo(
			1,
			startDate,
			endDate,
		);

		expect(result.total).toBe(3);
		expect(result.comparison.direction).toBe("down");
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/1/metrics/arrivalsInfo?startDate=2026-04-05&endDate=2026-04-14",
		);
	});

	it("should get WIP overview info for a team", async () => {
		const mockInfo = {
			count: 5,
			comparison: {
				direction: "up",
				metricLabel: "WIP",
				currentLabel: "2026-04-14",
				currentValue: "5",
				previousLabel: "2026-04-05",
				previousValue: "3",
				percentageDelta: "+66.7%",
			},
		};

		mockedAxios.get.mockResolvedValueOnce({ data: mockInfo });

		const startDate = new Date("2026-04-05");
		const endDate = new Date("2026-04-14");
		const result = await teamMetricsService.getWipOverviewInfo(
			1,
			startDate,
			endDate,
		);

		expect(result.count).toBe(5);
		expect(result.comparison.direction).toBe("up");
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/1/metrics/wipOverviewInfo?startDate=2026-04-05&endDate=2026-04-14",
		);
	});

	it("should get features worked on info for a team", async () => {
		const mockInfo = {
			count: 2,
			comparison: {
				direction: "down",
				metricLabel: "Features Being Worked On",
				currentLabel: "2026-04-14",
				currentValue: "2",
				previousLabel: "2026-04-05",
				previousValue: "3",
				percentageDelta: "-33.3%",
			},
		};

		mockedAxios.get.mockResolvedValueOnce({ data: mockInfo });

		const startDate = new Date("2026-04-05");
		const endDate = new Date("2026-04-14");
		const result = await teamMetricsService.getFeaturesWorkedOnInfo(
			1,
			startDate,
			endDate,
		);

		expect(result.count).toBe(2);
		expect(result.comparison.direction).toBe("down");
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/1/metrics/featuresWorkedOnInfo?startDate=2026-04-05&endDate=2026-04-14",
		);
	});

	it("should get total work item age info for a team", async () => {
		const mockInfo = {
			totalAge: 42,
			comparison: {
				direction: "up",
				metricLabel: "Total Work Item Age",
				currentLabel: "2026-04-14",
				currentValue: "42",
				previousLabel: "2026-04-05",
				previousValue: "30",
				percentageDelta: "+40.0%",
			},
		};

		mockedAxios.get.mockResolvedValueOnce({ data: mockInfo });

		const startDate = new Date("2026-04-05");
		const endDate = new Date("2026-04-14");
		const result = await teamMetricsService.getTotalWorkItemAgeInfo(
			1,
			startDate,
			endDate,
		);

		expect(result.totalAge).toBe(42);
		expect(result.comparison.direction).toBe("up");
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/1/metrics/totalWorkItemAgeInfo?startDate=2026-04-05&endDate=2026-04-14",
		);
	});

	it("should get predictability score info for a team", async () => {
		const mockInfo = {
			score: 0.73,
			comparison: {
				direction: "up",
				metricLabel: "Predictability Score",
				currentLabel: "2026-04-05 – 2026-04-14",
				currentValue: "73%",
				previousLabel: "2026-03-26 – 2026-04-04",
				previousValue: "60%",
				percentageDelta: "+13pp",
			},
		};

		mockedAxios.get.mockResolvedValueOnce({ data: mockInfo });

		const startDate = new Date("2026-04-05");
		const endDate = new Date("2026-04-14");
		const result = await teamMetricsService.getPredictabilityScoreInfo(
			1,
			startDate,
			endDate,
		);

		expect(result.score).toBe(0.73);
		expect(result.comparison.direction).toBe("up");
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/1/metrics/predictabilityScoreInfo?startDate=2026-04-05&endDate=2026-04-14",
		);
	});

	it("should get cycle time percentiles info for a team", async () => {
		const mockInfo = {
			percentiles: [
				{ percentile: 50, value: 5 },
				{ percentile: 85, value: 10 },
			],
			comparison: {
				direction: "up",
				metricLabel: "Cycle Time Percentiles",
				currentLabel: "2026-04-05 – 2026-04-14",
				currentValue: null,
				previousLabel: "2026-03-26 – 2026-04-04",
				previousValue: null,
				percentageDelta: null,
				detailRows: [
					{ label: "50th", currentValue: "5", previousValue: "3" },
					{ label: "85th", currentValue: "10", previousValue: "7" },
				],
			},
		};

		mockedAxios.get.mockResolvedValueOnce({ data: mockInfo });

		const startDate = new Date("2026-04-05");
		const endDate = new Date("2026-04-14");
		const result = await teamMetricsService.getCycleTimePercentilesInfo(
			1,
			startDate,
			endDate,
		);

		expect(result.percentiles).toHaveLength(2);
		expect(result.comparison.direction).toBe("up");
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/1/metrics/cycleTimePercentilesInfo?startDate=2026-04-05&endDate=2026-04-14",
		);
	});

	const createMockWorkItem = (name: string): IWorkItem => ({
		id: Math.floor(Math.random() * 1000),
		referenceId: Math.floor(Math.random() * 1000).toString(),
		url: "",
		name,
		workItemAge: 7,
		startedDate: new Date("2023-01-15"),
		closedDate: new Date("2023-01-20"),
		cycleTime:
			Math.floor(
				(new Date("2023-01-20").getTime() - new Date("2023-01-15").getTime()) /
					(1000 * 60 * 60 * 24),
			) + 1,
		state: "In Progress",
		stateCategory: "Doing",
		type: "Task",
		parentWorkItemReference: "",
		isBlocked: false,
	});
});
