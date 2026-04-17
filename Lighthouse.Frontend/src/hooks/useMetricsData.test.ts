import { renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IBlackoutPeriod } from "../models/BlackoutPeriod";
import type { IFeatureOwner } from "../models/IFeatureOwner";
import { RunChartData } from "../models/Metrics/RunChartData";
import type { IPercentileValue } from "../models/PercentileValue";
import type { IPortfolio } from "../models/Portfolio/Portfolio";
import type { IWorkItem } from "../models/WorkItem";
import type {
	IMetricsService,
	IProjectMetricsService,
} from "../services/Api/MetricsService";
import { createMockApiServiceContext } from "../tests/MockApiServiceProvider";
import { useMetricsData } from "./useMetricsData";

const mockBlackoutPeriodService = {
	getAll: vi.fn(),
	create: vi.fn(),
	update: vi.fn(),
	delete: vi.fn(),
};

const mockApiServiceContext = createMockApiServiceContext({
	blackoutPeriodService: mockBlackoutPeriodService,
});

vi.mock("react", async () => {
	const actual = await vi.importActual("react");
	return {
		...actual,
		useContext: () => mockApiServiceContext,
	};
});

vi.mock("../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => key,
		isLoading: false,
		error: null,
		refetchTerminology: () => {},
	}),
}));

function createMockTeamMetricsService(): IMetricsService<IWorkItem> {
	return {
		getThroughput: vi.fn().mockResolvedValue(new RunChartData({}, 30, 0)),
		getStartedItems: vi.fn().mockResolvedValue(new RunChartData({}, 30, 0)),
		getWorkInProgressOverTime: vi
			.fn()
			.mockResolvedValue(new RunChartData({}, 30, 0)),
		getInProgressItems: vi.fn().mockResolvedValue([]),
		getCycleTimeData: vi.fn().mockResolvedValue([]),
		getCycleTimePercentiles: vi.fn().mockResolvedValue([]),
		getMultiItemForecastPredictabilityScore: vi
			.fn()
			.mockResolvedValue({ probability: 0 }),
		getTotalWorkItemAge: vi.fn().mockResolvedValue(42),
		getThroughputPbc: vi.fn().mockResolvedValue(null),
		getWipPbc: vi.fn().mockResolvedValue(null),
		getTotalWorkItemAgePbc: vi.fn().mockResolvedValue(null),
		getCycleTimePbc: vi.fn().mockResolvedValue(null),
		getEstimationVsCycleTimeData: vi
			.fn()
			.mockResolvedValue({ status: "NotConfigured" }),
		getArrivals: vi.fn().mockResolvedValue(new RunChartData({}, 30, 0)),
		getArrivalsPbc: vi.fn().mockResolvedValue(null),
	};
}

function createMockProjectMetricsService(): IProjectMetricsService {
	return {
		...createMockTeamMetricsService(),
		getAllFeaturesForSizeChart: vi.fn().mockResolvedValue([]),
		getSizePercentiles: vi.fn().mockResolvedValue([]),
		getFeatureSizePbc: vi.fn().mockResolvedValue(null),
		getFeatureSizeEstimation: vi
			.fn()
			.mockResolvedValue({ status: "NotConfigured" }),
	} as unknown as IProjectMetricsService;
}

function createMockEntity(overrides?: Partial<IFeatureOwner>): IFeatureOwner {
	return {
		id: 1,
		name: "Test Team",
		lastUpdated: new Date(),
		features: [],
		remainingFeatures: 0,
		tags: [],
		serviceLevelExpectationProbability: 0,
		serviceLevelExpectationRange: 0,
		systemWIPLimit: 0,
		...overrides,
	};
}

function createMockPortfolioEntity(
	overrides?: Partial<IPortfolio>,
): IPortfolio {
	return {
		...createMockEntity(),
		involvedTeams: [],
		featureSizeTargetProbability: 0,
		featureSizeTargetRange: 0,
		...overrides,
	} as IPortfolio;
}

const startDate = new Date("2024-01-01");
const endDate = new Date("2024-06-30");

describe("useMetricsData", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		mockBlackoutPeriodService.getAll.mockResolvedValue([]);
	});

	describe("Baseline fetch orchestration", () => {
		it("should call all core metrics service methods on mount", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();

			renderHook(() => useMetricsData(entity, service, startDate, endDate));

			await waitFor(() => {
				expect(service.getThroughput).toHaveBeenCalledWith(
					entity.id,
					startDate,
					endDate,
				);
			});

			expect(service.getStartedItems).toHaveBeenCalledWith(
				entity.id,
				startDate,
				endDate,
			);
			expect(service.getInProgressItems).toHaveBeenCalledWith(entity.id);
			expect(service.getWorkInProgressOverTime).toHaveBeenCalledWith(
				entity.id,
				startDate,
				endDate,
			);
			expect(service.getCycleTimeData).toHaveBeenCalledWith(
				entity.id,
				startDate,
				endDate,
			);
			expect(service.getCycleTimePercentiles).toHaveBeenCalledWith(
				entity.id,
				startDate,
				endDate,
			);
			expect(
				service.getMultiItemForecastPredictabilityScore,
			).toHaveBeenCalledWith(entity.id, startDate, endDate);
			expect(service.getTotalWorkItemAge).toHaveBeenCalledWith(entity.id);
			expect(service.getEstimationVsCycleTimeData).toHaveBeenCalledWith(
				entity.id,
				startDate,
				endDate,
			);
		});

		it("should call all PBC methods on mount", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();

			renderHook(() => useMetricsData(entity, service, startDate, endDate));

			await waitFor(() => {
				expect(service.getThroughputPbc).toHaveBeenCalledWith(
					entity.id,
					startDate,
					endDate,
				);
			});

			expect(service.getWipPbc).toHaveBeenCalledWith(
				entity.id,
				startDate,
				endDate,
			);
			expect(service.getTotalWorkItemAgePbc).toHaveBeenCalledWith(
				entity.id,
				startDate,
				endDate,
			);
			expect(service.getCycleTimePbc).toHaveBeenCalledWith(
				entity.id,
				startDate,
				endDate,
			);
		});

		it("should fetch blackout periods on mount", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();
			const blackouts: IBlackoutPeriod[] = [
				{
					id: 1,
					start: "2024-03-01",
					end: "2024-03-05",
					description: "Holiday",
				},
			];
			mockBlackoutPeriodService.getAll.mockResolvedValue(blackouts);

			const { result } = renderHook(() =>
				useMetricsData(entity, service, startDate, endDate),
			);

			await waitFor(() => {
				expect(result.current.blackoutPeriods).toEqual(blackouts);
			});
		});

		it("should populate throughput data from service", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();
			const throughput = new RunChartData({}, 30, 15);
			vi.mocked(service.getThroughput).mockResolvedValue(throughput);

			const { result } = renderHook(() =>
				useMetricsData(entity, service, startDate, endDate),
			);

			await waitFor(() => {
				expect(result.current.throughputData).toEqual(throughput);
			});
		});

		it("should populate totalWorkItemAge from service", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();
			vi.mocked(service.getTotalWorkItemAge).mockResolvedValue(99);

			const { result } = renderHook(() =>
				useMetricsData(entity, service, startDate, endDate),
			);

			await waitFor(() => {
				expect(result.current.totalWorkItemAge).toBe(99);
			});
		});
	});

	describe("Project-only fetch gating", () => {
		it("should call project-specific methods for IProjectMetricsService", async () => {
			const entity = createMockEntity();
			const service = createMockProjectMetricsService();

			renderHook(() => useMetricsData(entity, service, startDate, endDate));

			await waitFor(() => {
				expect(service.getSizePercentiles).toHaveBeenCalledWith(
					entity.id,
					startDate,
					endDate,
				);
			});

			expect(service.getAllFeaturesForSizeChart).toHaveBeenCalledWith(
				entity.id,
				startDate,
				endDate,
			);
			expect(service.getFeatureSizePbc).toHaveBeenCalledWith(
				entity.id,
				startDate,
				endDate,
			);
			expect(service.getFeatureSizeEstimation).toHaveBeenCalledWith(
				entity.id,
				startDate,
				endDate,
			);
		});

		it("should NOT call project-specific methods for team metrics service", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();

			renderHook(() => useMetricsData(entity, service, startDate, endDate));

			await waitFor(() => {
				expect(service.getThroughput).toHaveBeenCalled();
			});

			expect(service).not.toHaveProperty("getSizePercentiles");
			expect(service).not.toHaveProperty("getAllFeaturesForSizeChart");
		});

		it("should populate size percentiles for project service", async () => {
			const entity = createMockEntity();
			const service = createMockProjectMetricsService();
			const sizePercentiles: IPercentileValue[] = [
				{ percentile: 50, value: 5 },
				{ percentile: 85, value: 13 },
			];
			vi.mocked(service.getSizePercentiles).mockResolvedValue(sizePercentiles);

			const { result } = renderHook(() =>
				useMetricsData(entity, service, startDate, endDate),
			);

			await waitFor(() => {
				expect(result.current.sizePercentileValues).toEqual(sizePercentiles);
			});
		});
	});

	describe("Service level expectation extraction", () => {
		it("should extract SLE from entity with valid SLE properties", async () => {
			const entity = createMockEntity({
				serviceLevelExpectationProbability: 85,
				serviceLevelExpectationRange: 14,
			});
			const service = createMockTeamMetricsService();

			const { result } = renderHook(() =>
				useMetricsData(entity, service, startDate, endDate),
			);

			await waitFor(() => {
				expect(result.current.serviceLevelExpectation).toEqual({
					percentile: 85,
					value: 14,
				});
			});
		});

		it("should NOT set SLE when probability is 0", async () => {
			const entity = createMockEntity({
				serviceLevelExpectationProbability: 0,
				serviceLevelExpectationRange: 14,
			});
			const service = createMockTeamMetricsService();

			const { result } = renderHook(() =>
				useMetricsData(entity, service, startDate, endDate),
			);

			await waitFor(() => {
				expect(result.current.throughputData).not.toBeNull();
			});

			expect(result.current.serviceLevelExpectation).toBeNull();
		});

		it("should NOT set SLE when range is 0", async () => {
			const entity = createMockEntity({
				serviceLevelExpectationProbability: 85,
				serviceLevelExpectationRange: 0,
			});
			const service = createMockTeamMetricsService();

			const { result } = renderHook(() =>
				useMetricsData(entity, service, startDate, endDate),
			);

			await waitFor(() => {
				expect(result.current.throughputData).not.toBeNull();
			});

			expect(result.current.serviceLevelExpectation).toBeNull();
		});
	});

	describe("Feature-size target extraction", () => {
		it("should extract featureSizeTarget from portfolio entity", async () => {
			const entity = createMockPortfolioEntity({
				featureSizeTargetProbability: 85,
				featureSizeTargetRange: 8,
			});
			const service = createMockProjectMetricsService();

			const { result } = renderHook(() =>
				useMetricsData(entity, service, startDate, endDate),
			);

			await waitFor(() => {
				expect(result.current.featureSizeTarget).toEqual({
					percentile: 85,
					value: 8,
				});
			});
		});

		it("should NOT set featureSizeTarget when probability is 0", async () => {
			const entity = createMockPortfolioEntity({
				featureSizeTargetProbability: 0,
				featureSizeTargetRange: 8,
			});
			const service = createMockProjectMetricsService();

			const { result } = renderHook(() =>
				useMetricsData(entity, service, startDate, endDate),
			);

			await waitFor(() => {
				expect(result.current.throughputData).not.toBeNull();
			});

			expect(result.current.featureSizeTarget).toBeNull();
		});

		it("should NOT set featureSizeTarget for team entity without portfolio fields", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();

			const { result } = renderHook(() =>
				useMetricsData(entity, service, startDate, endDate),
			);

			await waitFor(() => {
				expect(result.current.throughputData).not.toBeNull();
			});

			expect(result.current.featureSizeTarget).toBeNull();
		});
	});

	describe("Graceful fallback on failure", () => {
		it("should fall back to empty blackout periods on service error", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();
			mockBlackoutPeriodService.getAll.mockRejectedValue(
				new Error("Network error"),
			);

			const { result } = renderHook(() =>
				useMetricsData(entity, service, startDate, endDate),
			);

			await waitFor(() => {
				expect(result.current.throughputData).not.toBeNull();
			});

			expect(result.current.blackoutPeriods).toEqual([]);
		});

		it("should keep null for predictability data on fetch error", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();
			vi.mocked(
				service.getMultiItemForecastPredictabilityScore,
			).mockRejectedValue(new Error("Service error"));
			const consoleSpy = vi
				.spyOn(console, "error")
				.mockImplementation(() => {});

			const { result } = renderHook(() =>
				useMetricsData(entity, service, startDate, endDate),
			);

			await waitFor(() => {
				expect(result.current.throughputData).not.toBeNull();
			});

			expect(result.current.predictabilityData).toBeNull();
			consoleSpy.mockRestore();
		});

		it("should keep null for throughput on fetch error", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();
			vi.mocked(service.getThroughput).mockRejectedValue(
				new Error("Fetch error"),
			);
			const consoleSpy = vi
				.spyOn(console, "error")
				.mockImplementation(() => {});

			const { result } = renderHook(() =>
				useMetricsData(entity, service, startDate, endDate),
			);

			await waitFor(() => {
				expect(consoleSpy).toHaveBeenCalled();
			});

			expect(result.current.throughputData).toBeNull();
			consoleSpy.mockRestore();
		});
	});
});
