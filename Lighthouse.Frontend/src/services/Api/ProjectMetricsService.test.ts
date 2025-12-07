import { describe, expect, it, vi } from "vitest";
import { Feature } from "../../models/Feature";
import { RunChartData } from "../../models/Metrics/RunChartData";
import type { IPercentileValue } from "../../models/PercentileValue";
import { generateWorkItemMapForRunChart } from "../../tests/TestDataProvider";
import { ProjectMetricsService } from "./ProjectMetricsService";

describe("ProjectMetricsService", () => {
	let service: ProjectMetricsService;

	const mockGet = vi.fn();
	const projectId = 123;
	const startDate = new Date("2023-01-01");
	const endDate = new Date("2023-01-31");

	beforeEach(() => {
		service = new ProjectMetricsService();
		// @ts-expect-error - Mocking private field
		service.apiService = { get: mockGet };
	});

	describe("getThroughputForProject", () => {
		it("should call the correct API endpoint and return RunChartData", async () => {
			const workItems = generateWorkItemMapForRunChart([1, 2, 3]);

			const mockResponse = {
				data: {
					workItemsPerUnitOfTime: workItems,
					history: 3,
					total: 6,
				},
			};
			mockGet.mockResolvedValueOnce(mockResponse);

			// Act
			const result = await service.getThroughput(projectId, startDate, endDate);

			// Assert
			expect(mockGet).toHaveBeenCalledWith(
				`/portfolios/${projectId}/metrics/throughput?startDate=2023-01-01&endDate=2023-01-31`,
			);
			expect(result).toBeInstanceOf(RunChartData);
			expect(result.workItemsPerUnitOfTime).toEqual(workItems);
			expect(result.history).toBe(3);
			expect(result.total).toBe(6);
		});
	});

	describe("getStartedItems", () => {
		it("should call the correct API endpoint and return RunChartData", async () => {
			const workItems = generateWorkItemMapForRunChart([2, 3, 4]);

			const mockResponse = {
				data: {
					workItemsPerUnitOfTime: workItems,
					history: 3,
					total: 9,
				},
			};
			mockGet.mockResolvedValueOnce(mockResponse);

			// Act
			const result = await service.getStartedItems(
				projectId,
				startDate,
				endDate,
			);

			// Assert
			expect(mockGet).toHaveBeenCalledWith(
				`/portfolios/${projectId}/metrics/started?startDate=2023-01-01&endDate=2023-01-31`,
			);
			expect(result).toBeInstanceOf(RunChartData);
			expect(result.workItemsPerUnitOfTime).toEqual(workItems);
			expect(result.history).toBe(3);
			expect(result.total).toBe(9);
		});
	});

	describe("getFeaturesInProgressOverTimeForProject", () => {
		it("should call the correct API endpoint and return RunChartData", async () => {
			const workItems = generateWorkItemMapForRunChart([4, 5, 6]);

			const mockResponse = {
				data: {
					workItemsPerUnitOfTime: workItems,
					history: 3,
					total: 15,
				},
			};
			mockGet.mockResolvedValueOnce(mockResponse);

			// Act
			const result = await service.getWorkInProgressOverTime(
				projectId,
				startDate,
				endDate,
			);

			// Assert
			expect(mockGet).toHaveBeenCalledWith(
				`/portfolios/${projectId}/metrics/wipOverTime?startDate=2023-01-01&endDate=2023-01-31`,
			);
			expect(result).toBeInstanceOf(RunChartData);
			expect(result.workItemsPerUnitOfTime).toEqual(workItems);
			expect(result.history).toBe(3);
			expect(result.total).toBe(15);
		});
	});

	describe("getInProgressFeaturesForProject", () => {
		it("should call the correct API endpoint and process work items correctly", async () => {
			// Arrange

			const feature1 = new Feature();
			feature1.name = "Feature 1";
			feature1.id = 1;
			feature1.referenceId = "F-1";
			feature1.state = "In Progress";
			feature1.type = "Feature";
			feature1.startedDate = new Date("2023-01-10T00:00:00Z");
			feature1.closedDate = new Date("2023-01-20T00:00:00Z");

			const feature2 = new Feature();
			feature2.name = "Feature 2";
			feature2.id = 2;
			feature2.referenceId = "F-2";
			feature2.state = "In Progress";
			feature2.type = "Feature";
			feature2.startedDate = new Date("2023-01-15T00:00:00Z");
			feature2.closedDate = new Date("2023-01-30T00:00:00Z");

			const mockWorkItems = [feature1, feature2];
			mockGet.mockResolvedValueOnce({ data: mockWorkItems });

			// Act
			const result = await service.getInProgressItems(projectId);

			// Assert
			expect(mockGet).toHaveBeenCalledWith(
				`/portfolios/${projectId}/metrics/currentwip`,
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
			const result = await service.getCycleTimePercentiles(
				projectId,
				startDate,
				endDate,
			);

			// Assert
			expect(mockGet).toHaveBeenCalledWith(
				`/portfolios/${projectId}/metrics/cycleTimePercentiles?startDate=2023-01-01&endDate=2023-01-31`,
			);
			expect(result).toEqual(mockPercentiles);
		});
	});

	describe("getSizePercentilesForProject", () => {
		it("should call the correct API endpoint and return percentile values", async () => {
			// Arrange
			const mockPercentiles: IPercentileValue[] = [
				{ percentile: 50, value: 5 },
				{ percentile: 85, value: 10 },
				{ percentile: 95, value: 15 },
			];
			mockGet.mockResolvedValueOnce({ data: mockPercentiles });

			// Act
			const result = await service.getSizePercentiles(
				projectId,
				startDate,
				endDate,
			);

			// Assert
			expect(mockGet).toHaveBeenCalledWith(
				`/portfolios/${projectId}/metrics/sizePercentiles?startDate=2023-01-01&endDate=2023-01-31`,
			);
			expect(result).toEqual(mockPercentiles);
		});
	});

	describe("getCycleTimeDataForProject", () => {
		it("should call the correct API endpoint and process features correctly", async () => {
			const feature1 = new Feature();
			feature1.name = "Feature 1";
			feature1.id = 3;
			feature1.referenceId = "F-3";
			feature1.state = "Done";
			feature1.startedDate = new Date("2023-01-10T00:00:00Z");
			feature1.closedDate = new Date("2023-01-20T00:00:00Z");

			const feature2 = new Feature();
			feature2.name = "Feature 2";
			feature2.id = 4;
			feature2.referenceId = "F-4";
			feature2.state = "Done";
			feature2.startedDate = new Date("2023-01-15T00:00:00Z");
			feature2.closedDate = new Date("2023-01-25T00:00:00Z");

			const mockFeatures = [feature1, feature2];
			mockGet.mockResolvedValueOnce({ data: mockFeatures });

			// Act
			const result = await service.getCycleTimeData(
				projectId,
				startDate,
				endDate,
			);

			// Assert
			expect(mockGet).toHaveBeenCalledWith(
				`/portfolios/${projectId}/metrics/cycleTimeData?startDate=2023-01-01&endDate=2023-01-31`,
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

	describe("getTotalWorkItemAge", () => {
		it("should call the correct API endpoint and return total age", async () => {
			// Arrange
			const mockTotalAge = 250;
			mockGet.mockResolvedValueOnce({ data: mockTotalAge });

			// Act
			const result = await service.getTotalWorkItemAge(projectId);

			// Assert
			expect(mockGet).toHaveBeenCalledWith(
				`/portfolios/${projectId}/metrics/totalWorkItemAge`,
			);
			expect(result).toBe(250);
		});

		it("should handle errors when getting total work item age", async () => {
			// Arrange
			const errorMessage = "Network Error";
			mockGet.mockRejectedValueOnce(new Error(errorMessage));

			// Act & Assert
			await expect(service.getTotalWorkItemAge(projectId)).rejects.toThrow(
				errorMessage,
			);
		});
	});
});
