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
			const result = await service.getInProgressItems(
				projectId,
				new Date(2025, 5, 15),
			);

			// Assert
			expect(mockGet).toHaveBeenCalledWith(
				`/portfolios/${projectId}/metrics/wip?asOfDate=2025-06-15`,
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

		it("should use local calendar date components, not UTC conversion", () => {
			// A date created with local components (as a date picker would)
			// must serialize using the local year/month/day, not the UTC year/month/day.
			const localStart = new Date(2026, 2, 28); // March 28, 2026 local
			const localEnd = new Date(2026, 2, 28);

			const result = service.getDateFormatString(localStart, localEnd);

			// The query string must reflect the local calendar date (2026-03-28),
			// regardless of what UTC date that midnight maps to.
			expect(result).toBe("startDate=2026-03-28&endDate=2026-03-28");
		});

		it("should produce stable yyyy-MM-dd for DST-inactive winter dates", () => {
			// July 15 in Southern Hemisphere (or Jan 15 in Northern) — DST-inactive
			// for positive-offset timezones like NZ (NZST = UTC+12, no DST)
			const winterDate = new Date(2026, 6, 15); // July 15, 2026
			const result = service.getDateFormatString(winterDate, winterDate);
			expect(result).toBe("startDate=2026-07-15&endDate=2026-07-15");
		});

		it("should produce stable yyyy-MM-dd for DST-active summer dates", () => {
			// Late March in Southern Hemisphere — DST-active for NZ (NZDT = UTC+13)
			const summerDate = new Date(2026, 2, 28); // March 28, 2026
			const result = service.getDateFormatString(summerDate, summerDate);
			expect(result).toBe("startDate=2026-03-28&endDate=2026-03-28");
		});

		it("should handle year-end boundary dates correctly", () => {
			const nyeStart = new Date(2025, 11, 31); // Dec 31, 2025
			const nydEnd = new Date(2026, 0, 1); // Jan 1, 2026
			const result = service.getDateFormatString(nyeStart, nydEnd);
			expect(result).toBe("startDate=2025-12-31&endDate=2026-01-01");
		});

		it("should zero-pad single-digit months and days", () => {
			const start = new Date(2026, 0, 5); // Jan 5
			const end = new Date(2026, 8, 2); // Sep 2
			const result = service.getDateFormatString(start, end);
			expect(result).toBe("startDate=2026-01-05&endDate=2026-09-02");
		});
	});

	describe("getTotalWorkItemAge", () => {
		it("should call the correct API endpoint and return total age", async () => {
			// Arrange
			const mockTotalAge = 250;
			mockGet.mockResolvedValueOnce({ data: mockTotalAge });

			// Act
			const result = await service.getTotalWorkItemAge(
				projectId,
				new Date(2025, 5, 15),
			);

			// Assert
			expect(mockGet).toHaveBeenCalledWith(
				`/portfolios/${projectId}/metrics/totalWorkItemAge?asOfDate=2025-06-15`,
			);
			expect(result).toBe(250);
		});

		it("should handle errors when getting total work item age", async () => {
			// Arrange
			const errorMessage = "Network Error";
			mockGet.mockRejectedValueOnce(new Error(errorMessage));

			// Act & Assert
			await expect(
				service.getTotalWorkItemAge(projectId, new Date()),
			).rejects.toThrow(errorMessage);
		});
	});

	describe("getFeatureSizePbc", () => {
		it("should call the correct API endpoint and return PBC data", async () => {
			// Arrange
			const mockPbcData = {
				status: "Ready",
				statusReason: "",
				xAxisKind: "DateTime",
				average: 8,
				upperNaturalProcessLimit: 14,
				lowerNaturalProcessLimit: 2,
				baselineConfigured: false,
				dataPoints: [
					{
						xValue: "2023-01-10T00:00:00",
						yValue: 5,
						specialCauses: [],
						workItemIds: [1],
					},
				],
			};
			mockGet.mockResolvedValueOnce({ data: mockPbcData });

			// Act
			const result = await service.getFeatureSizePbc(
				projectId,
				startDate,
				endDate,
			);

			// Assert
			expect(mockGet).toHaveBeenCalledWith(
				`/portfolios/${projectId}/metrics/featureSize/pbc?startDate=2023-01-01&endDate=2023-01-31`,
			);
			expect(result).toEqual(mockPbcData);
			expect(result.xAxisKind).toBe("DateTime");
			expect(result.dataPoints).toHaveLength(1);
			expect(result.dataPoints[0].yValue).toBe(5);
		});
	});

	describe("getThroughputInfo", () => {
		it("should call the correct API endpoint and return throughput info", async () => {
			const mockInfo = {
				total: 7,
				dailyAverage: 0.7,
				comparison: {
					direction: "up",
					metricLabel: "Total Throughput",
					currentLabel: "2023-01-01 – 2023-01-31",
					currentValue: "7",
					previousLabel: "2022-12-01 – 2022-12-31",
					previousValue: "4",
					percentageDelta: "+75.0%",
				},
			};
			mockGet.mockResolvedValueOnce({ data: mockInfo });

			const result = await service.getThroughputInfo(
				projectId,
				startDate,
				endDate,
			);

			expect(mockGet).toHaveBeenCalledWith(
				`/portfolios/${projectId}/metrics/throughputInfo?startDate=2023-01-01&endDate=2023-01-31`,
			);
			expect(result.total).toBe(7);
			expect(result.comparison.direction).toBe("up");
		});
	});

	describe("getArrivalsInfo", () => {
		it("should call the correct API endpoint and return arrivals info", async () => {
			const mockInfo = {
				total: 3,
				dailyAverage: 0.3,
				comparison: {
					direction: "none",
					metricLabel: "Total Arrivals",
					currentLabel: "2023-01-01 – 2023-01-31",
					currentValue: "3",
					previousLabel: "2022-12-01 – 2022-12-31",
					previousValue: "0",
					percentageDelta: null,
				},
			};
			mockGet.mockResolvedValueOnce({ data: mockInfo });

			const result = await service.getArrivalsInfo(
				projectId,
				startDate,
				endDate,
			);

			expect(mockGet).toHaveBeenCalledWith(
				`/portfolios/${projectId}/metrics/arrivalsInfo?startDate=2023-01-01&endDate=2023-01-31`,
			);
			expect(result.total).toBe(3);
			expect(result.comparison.direction).toBe("none");
		});
	});

	describe("getFeatureSizePercentilesInfo", () => {
		it("should call the correct API endpoint and return feature size percentiles info", async () => {
			const mockInfo = {
				percentiles: [
					{ percentile: 50, value: 5 },
					{ percentile: 85, value: 12 },
				],
				comparison: {
					direction: "up",
					metricLabel: "Feature Size Percentiles",
					detailRows: [
						{ label: "50th", currentValue: "5", previousValue: "3" },
						{ label: "85th", currentValue: "12", previousValue: "10" },
					],
				},
			};
			mockGet.mockResolvedValueOnce({ data: mockInfo });

			const result = await service.getFeatureSizePercentilesInfo(
				projectId,
				startDate,
				endDate,
			);

			expect(mockGet).toHaveBeenCalledWith(
				`/portfolios/${projectId}/metrics/featureSizePercentilesInfo?startDate=2023-01-01&endDate=2023-01-31`,
			);
			expect(result.percentiles).toHaveLength(2);
			expect(result.comparison.direction).toBe("up");
		});
	});

	describe("getWipOverviewInfo", () => {
		it("should call the correct API endpoint and return WIP overview info", async () => {
			const mockInfo = {
				count: 5,
				comparison: {
					direction: "up",
					metricLabel: "WIP",
					currentLabel: "2023-01-31",
					currentValue: "5",
					previousLabel: "2023-01-01",
					previousValue: "3",
					percentageDelta: "+66.7%",
				},
			};
			mockGet.mockResolvedValueOnce({ data: mockInfo });

			const result = await service.getWipOverviewInfo(
				projectId,
				startDate,
				endDate,
			);

			expect(mockGet).toHaveBeenCalledWith(
				`/portfolios/${projectId}/metrics/wipOverviewInfo?startDate=2023-01-01&endDate=2023-01-31`,
			);
			expect(result.count).toBe(5);
			expect(result.comparison.direction).toBe("up");
		});
	});

	describe("getTotalWorkItemAgeInfo", () => {
		it("should call the correct API endpoint and return total work item age info", async () => {
			const mockInfo = {
				totalAge: 42,
				comparison: {
					direction: "up",
					metricLabel: "Total Work Item Age",
					currentLabel: "2023-01-31",
					currentValue: "42",
					previousLabel: "2023-01-01",
					previousValue: "30",
					percentageDelta: "+40.0%",
				},
			};
			mockGet.mockResolvedValueOnce({ data: mockInfo });

			const result = await service.getTotalWorkItemAgeInfo(
				projectId,
				startDate,
				endDate,
			);

			expect(mockGet).toHaveBeenCalledWith(
				`/portfolios/${projectId}/metrics/totalWorkItemAgeInfo?startDate=2023-01-01&endDate=2023-01-31`,
			);
			expect(result.totalAge).toBe(42);
			expect(result.comparison.direction).toBe("up");
		});
	});

	describe("getPredictabilityScoreInfo", () => {
		it("should call the correct API endpoint and return predictability score info", async () => {
			const mockInfo = {
				score: 0.73,
				comparison: {
					direction: "up",
					metricLabel: "Predictability Score",
					currentLabel: "2023-01-01 – 2023-01-31",
					currentValue: "73%",
					previousLabel: "2022-12-01 – 2022-12-31",
					previousValue: "60%",
					percentageDelta: "+13pp",
				},
			};
			mockGet.mockResolvedValueOnce({ data: mockInfo });

			const result = await service.getPredictabilityScoreInfo(
				projectId,
				startDate,
				endDate,
			);

			expect(mockGet).toHaveBeenCalledWith(
				`/portfolios/${projectId}/metrics/predictabilityScoreInfo?startDate=2023-01-01&endDate=2023-01-31`,
			);
			expect(result.score).toBe(0.73);
			expect(result.comparison.direction).toBe("up");
		});
	});

	describe("getCycleTimePercentilesInfo", () => {
		it("should call the correct API endpoint and return cycle time percentiles info", async () => {
			const mockInfo = {
				percentiles: [
					{ percentile: 50, value: 5 },
					{ percentile: 85, value: 10 },
				],
				comparison: {
					direction: "up",
					metricLabel: "Cycle Time Percentiles",
					detailRows: [
						{ label: "50th", currentValue: "5", previousValue: "3" },
						{ label: "85th", currentValue: "10", previousValue: "7" },
					],
				},
			};
			mockGet.mockResolvedValueOnce({ data: mockInfo });

			const result = await service.getCycleTimePercentilesInfo(
				projectId,
				startDate,
				endDate,
			);

			expect(mockGet).toHaveBeenCalledWith(
				`/portfolios/${projectId}/metrics/cycleTimePercentilesInfo?startDate=2023-01-01&endDate=2023-01-31`,
			);
			expect(result.percentiles).toHaveLength(2);
			expect(result.comparison.direction).toBe("up");
		});
	});
});
