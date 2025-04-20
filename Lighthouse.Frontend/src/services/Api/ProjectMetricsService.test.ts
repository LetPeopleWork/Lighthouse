import { describe, expect, it, vi } from "vitest";
import { Feature } from "../../models/Feature";
import { RunChartData } from "../../models/Forecasts/RunChartData";
import type { IPercentileValue } from "../../models/PercentileValue";
import { ProjectMetricsService } from "./ProjectMetricsService";

describe("ProjectMetricsService", () => {
	let service: ProjectMetricsService;

	const mockGet = vi.fn();
	const projectId = 123;
	const startDate = new Date("2023-01-01");
	const endDate = new Date("2023-01-31");

	beforeEach(() => {
		service = new ProjectMetricsService();
		// @ts-ignore - Mocking private field
		service.apiService = { get: mockGet };
	});

	describe("getThroughputForProject", () => {
		it("should call the correct API endpoint and return RunChartData", async () => {
			// Arrange
			const mockResponse = {
				data: {
					valuePerUnitOfTime: [1, 2, 3],
					history: 3,
					total: 6,
				},
			};
			mockGet.mockResolvedValueOnce(mockResponse);

			// Act
			const result = await service.getThroughputForProject(
				projectId,
				startDate,
				endDate,
			);

			// Assert
			expect(mockGet).toHaveBeenCalledWith(
				`/projects/${projectId}/metrics/throughput?startDate=2023-01-01&endDate=2023-01-31`,
			);
			expect(result).toBeInstanceOf(RunChartData);
			expect(result.valuePerUnitOfTime).toEqual([1, 2, 3]);
			expect(result.history).toBe(3);
			expect(result.total).toBe(6);
		});
	});

	describe("getFeaturesInProgressOverTimeForProject", () => {
		it("should call the correct API endpoint and return RunChartData", async () => {
			// Arrange
			const mockResponse = {
				data: {
					valuePerUnitOfTime: [4, 5, 6],
					history: 3,
					total: 15,
				},
			};
			mockGet.mockResolvedValueOnce(mockResponse);

			// Act
			const result = await service.getFeaturesInProgressOverTimeForProject(
				projectId,
				startDate,
				endDate,
			);

			// Assert
			expect(mockGet).toHaveBeenCalledWith(
				`/projects/${projectId}/metrics/featuresInProgressOverTime?startDate=2023-01-01&endDate=2023-01-31`,
			);
			expect(result).toBeInstanceOf(RunChartData);
			expect(result.valuePerUnitOfTime).toEqual([4, 5, 6]);
			expect(result.history).toBe(3);
			expect(result.total).toBe(15);
		});
	});

	describe("getInProgressFeaturesForProject", () => {
		it("should call the correct API endpoint and process work items correctly", async () => {
			// Arrange
			const mockWorkItems = [
				new Feature(
					"Feature 1", // name
					1, // id
					"F-1", // workItemReference
					"In Progress", // state
					"Feature", // type
					new Date("2023-01-17T00:00:00Z"), // lastUpdated
					false, // isUsingDefaultFeatureSize
					{}, // projects
					{}, // remainingWork
					{}, // totalWork
					{}, // milestoneLikelihood
					[], // forecasts
					"https://example.com/1", // url
					"Doing", // stateCategory
					new Date("2023-01-10T00:00:00Z"), // startedDate
					new Date("2023-01-20T00:00:00Z"), // closedDate
					10, // cycleTime
					10, // workItemAge
				),
				new Feature(
					"Feature 2", // name
					2, // id
					"F-2", // workItemReference
					"In Progress", // state
					"Feature", // type
					new Date("2023-01-17T00:00:00Z"), // lastUpdated
					false, // isUsingDefaultFeatureSize
					{}, // projects
					{}, // remainingWork
					{}, // totalWork
					{}, // milestoneLikelihood
					[], // forecasts
					"https://example.com/2", // url
					"Doing", // stateCategory
					new Date("2023-01-15T00:00:00Z"), // startedDate
					new Date("2023-01-30T00:00:00Z"), // closedDate
					15, // cycleTime
					15, // workItemAge
				),
			];
			mockGet.mockResolvedValueOnce({ data: mockWorkItems });

			// Act
			const result = await service.getInProgressFeaturesForProject(projectId);

			// Assert
			expect(mockGet).toHaveBeenCalledWith(
				`/projects/${projectId}/metrics/inProgressFeatures`,
			);
			expect(result.length).toBe(2);
			expect(result[0].id).toBe(1);
			expect(result[1].id).toBe(2);
			expect(result[0].startedDate).toBeInstanceOf(Date);
			expect(result[1].closedDate).toBeInstanceOf(Date);
		});
	});

	describe("getCycleTimePercentilesForProject", () => {
		it("should call the correct API endpoint and return percentile values", async () => {
			// Arrange
			const mockPercentiles: IPercentileValue[] = [
				{ percentile: 50, value: 5 },
				{ percentile: 85, value: 10 },
				{ percentile: 95, value: 15 },
			];
			mockGet.mockResolvedValueOnce({ data: mockPercentiles });

			// Act
			const result = await service.getCycleTimePercentilesForProject(
				projectId,
				startDate,
				endDate,
			);

			// Assert
			expect(mockGet).toHaveBeenCalledWith(
				`/projects/${projectId}/metrics/cycleTimePercentiles?startDate=2023-01-01&endDate=2023-01-31`,
			);
			expect(result).toEqual(mockPercentiles);
		});
	});

	describe("getCycleTimeDataForProject", () => {
		it("should call the correct API endpoint and process features correctly", async () => {
			// Arrange
			const mockFeatures = [
				new Feature(
					"Feature 3", // name
					3, // id
					"F-3", // workItemReference
					"Done", // state
					"Feature", // type
					new Date("2023-01-17T00:00:00Z"), // lastUpdated
					false, // isUsingDefaultFeatureSize
					{}, // projects
					{}, // remainingWork
					{}, // totalWork
					{}, // milestoneLikelihood
					[], // forecasts
					"https://example.com/3", // url
					"Done", // stateCategory
					new Date("2023-01-10T00:00:00Z"), // startedDate
					new Date("2023-01-17T00:00:00Z"), // closedDate
					7, // cycleTime
					7, // workItemAge
				),
				new Feature(
					"Feature 4", // name
					4, // id
					"F-4", // workItemReference
					"Done", // state
					"Feature", // type
					new Date("2023-01-17T00:00:00Z"), // lastUpdated
					false, // isUsingDefaultFeatureSize
					{}, // projects
					{}, // remainingWork
					{}, // totalWork
					{}, // milestoneLikelihood
					[], // forecasts
					"https://example.com/4", // url
					"Done", // stateCategory
					new Date("2023-01-05T00:00:00Z"), // startedDate
					new Date("2023-01-17T00:00:00Z"), // closedDate
					12, // cycleTime
					12, // workItemAge
				),
			];
			mockGet.mockResolvedValueOnce({ data: mockFeatures });

			// Act
			const result = await service.getCycleTimeDataForProject(
				projectId,
				startDate,
				endDate,
			);

			// Assert
			expect(mockGet).toHaveBeenCalledWith(
				`/projects/${projectId}/metrics/cycleTimeData?startDate=2023-01-01&endDate=2023-01-31`,
			);
			expect(result.length).toBe(2);
			expect(result[0].id).toBe(3);
			expect(result[1].id).toBe(4);
			expect(result[0].startedDate).toBeInstanceOf(Date);
			expect(result[1].closedDate).toBeInstanceOf(Date);
		});
	});

	describe("getDateFormatString", () => {
		it("should format dates correctly", () => {
			// Act
			const result = service.getDateFormatString(startDate, endDate);

			// Assert
			expect(result).toBe("startDate=2023-01-01&endDate=2023-01-31");
		});
	});
});
