import { renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IBlackoutPeriod } from "../models/BlackoutPeriod";
import type { IFeatureOwner } from "../models/IFeatureOwner";
import type { ICumulativeStateTimeResponse } from "../models/Metrics/CumulativeStateTime";
import { RunChartData } from "../models/Metrics/RunChartData";
import type { IPercentileValue } from "../models/PercentileValue";
import type { IPerStatePercentileValues } from "../models/PerStatePercentileValues";
import type { IPortfolio } from "../models/Portfolio/Portfolio";
import {
	getFetchKeysForCategories,
	type MetricsFetchKey,
} from "../pages/Common/MetricsView/categoryMetadata";
import type {
	IProjectMetricsService,
	ITeamMetricsService,
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

function createMockTeamMetricsService(): ITeamMetricsService {
	return {
		getThroughput: vi.fn().mockResolvedValue(new RunChartData({}, 30, 0)),
		getWorkInProgressOverTime: vi
			.fn()
			.mockResolvedValue(new RunChartData({}, 30, 0)),
		getInProgressItems: vi.fn().mockResolvedValue([]),
		getCycleTimeData: vi.fn().mockResolvedValue([]),
		getCycleTimePercentiles: vi.fn().mockResolvedValue([]),
		getWorkItemAgePercentiles: vi.fn().mockResolvedValue([]),
		getAgeInStatePercentiles: vi.fn().mockResolvedValue([]),
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
		getFeaturesInProgress: vi.fn().mockResolvedValue([]),
		getForecastInputCandidates: vi.fn().mockResolvedValue({
			remainingItems: 0,
			wipItems: 0,
		}),
		getThroughputInfo: vi.fn().mockResolvedValue({
			total: 0,
			dailyAverage: 0,
			comparison: { direction: "none", metricLabel: "Total Throughput" },
		}),
		getArrivalsInfo: vi.fn().mockResolvedValue({
			total: 0,
			dailyAverage: 0,
			comparison: { direction: "none", metricLabel: "Total Arrivals" },
		}),
		getWipOverviewInfo: vi.fn().mockResolvedValue({
			count: 0,
			comparison: { direction: "none", metricLabel: "WIP" },
		}),
		getFeaturesWorkedOnInfo: vi.fn().mockResolvedValue({
			count: 0,
			comparison: {
				direction: "none",
				metricLabel: "Features Being Worked On",
			},
		}),
		getTotalWorkItemAgeInfo: vi.fn().mockResolvedValue({
			totalAge: 0,
			comparison: { direction: "none", metricLabel: "Total Work Item Age" },
		}),
		getPredictabilityScoreInfo: vi.fn().mockResolvedValue({
			score: 0,
			comparison: { direction: "none", metricLabel: "Predictability Score" },
		}),
		getCycleTimePercentilesInfo: vi.fn().mockResolvedValue({
			percentiles: [],
			comparison: {
				direction: "none",
				metricLabel: "Cycle Time Percentiles",
			},
		}),
		getCumulativeStateTimeForTeam: vi.fn().mockResolvedValue({ states: [] }),
		getCumulativeStateTimeItemsForTeam: vi
			.fn()
			.mockResolvedValue({ state: "", items: [] }),
		getCumulativeStateTimeItemsForPortfolio: vi
			.fn()
			.mockResolvedValue({ state: "", items: [] }),
		getCumulativeStateTimeCandidatesForTeam: vi
			.fn()
			.mockResolvedValue({ items: [] }),
		getCumulativeStateTimeCandidatesForPortfolio: vi
			.fn()
			.mockResolvedValue({ items: [] }),
		getFlowEfficiencyInfoForTeam: vi.fn(),
		getFlowEfficiencyInfoForPortfolio: vi.fn(),
		getBlockedCountHistory: vi.fn().mockResolvedValue([]),
		getBlockedItemsAtDate: vi.fn().mockResolvedValue([]),
		getPercentilesOverTime: vi.fn().mockResolvedValue([]),
		getProcessBehaviorOverTime: vi.fn().mockResolvedValue([]),
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
		getFeatureSizePercentilesInfo: vi.fn().mockResolvedValue({
			percentiles: [],
			comparison: {
				direction: "none",
				metricLabel: "Feature Size Percentiles",
			},
		}),
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

/** The five methods the hook probes to decide a service is portfolio-shaped. */
const portfolioProbeMethods = [
	"getAllFeaturesForSizeChart",
	"getSizePercentiles",
	"getFeatureSizePbc",
	"getFeatureSizeEstimation",
	"getFeatureSizePercentilesInfo",
] as const;

/** Every key whose effect is reachable only through the portfolio discriminator. */
const portfolioOnlyFetchKeys: ReadonlySet<MetricsFetchKey> = new Set([
	"featureSizeData",
	"featureSizePbc",
	"featureSizeEstimation",
	"featureSizePercentilesInfo",
]);

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

			expect(service.getArrivals).toHaveBeenCalledWith(
				entity.id,
				startDate,
				endDate,
			);
			expect(service.getInProgressItems).toHaveBeenCalledWith(
				entity.id,
				endDate,
			);
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
			expect(service.getTotalWorkItemAge).toHaveBeenCalledWith(
				entity.id,
				endDate,
			);
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

	describe("Per-state pace percentiles fetch", () => {
		it("should populate perStatePercentileValues without disturbing cycle time percentiles", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();
			const perState: IPerStatePercentileValues[] = [
				{ state: "In Progress", percentiles: [{ percentile: 50, value: 3 }] },
			];
			const cycleTimePercentiles: IPercentileValue[] = [
				{ percentile: 85, value: 12 },
			];
			vi.mocked(service.getAgeInStatePercentiles).mockResolvedValue(perState);
			vi.mocked(service.getCycleTimePercentiles).mockResolvedValue(
				cycleTimePercentiles,
			);

			const { result } = renderHook(() =>
				useMetricsData(entity, service, startDate, endDate),
			);

			await waitFor(() => {
				expect(result.current.perStatePercentileValues).toEqual(perState);
			});

			expect(service.getAgeInStatePercentiles).toHaveBeenCalledWith(
				entity.id,
				startDate,
				endDate,
			);
			expect(result.current.percentileValues).toEqual(cycleTimePercentiles);
		});
	});

	describe("Work Item Age Percentiles previous-period fetch", () => {
		/**
		 * The trend's whole credibility rests on the SECOND window, and a wrong window is invisible
		 * from the widget: it still renders an arrow, just a meaningless one. This is exactly how the
		 * blocked-trend fetch-window defect (US-03 AC0) escaped every rendering test and was caught
		 * only at this seam, so the window is pinned here rather than at the view.
		 *
		 * D5: the comparison window ends the day BEFORE the selected range starts, and spans the same
		 * length. The backend snapshots on the window's endDate, so that boundary day is the part
		 * that actually selects the comparison point.
		 */
		it("reads the same endpoint over a same-length window ending the day before the range", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();

			renderHook(() => useMetricsData(entity, service, startDate, endDate));

			await waitFor(() => {
				expect(service.getWorkItemAgePercentiles).toHaveBeenCalledTimes(2);
			});

			const oneDayMs = 24 * 60 * 60 * 1000;
			const expectedPreviousEnd = new Date(startDate.getTime() - oneDayMs);
			const expectedPreviousStart = new Date(
				expectedPreviousEnd.getTime() -
					(endDate.getTime() - startDate.getTime()),
			);

			expect(service.getWorkItemAgePercentiles).toHaveBeenCalledWith(
				entity.id,
				startDate,
				endDate,
			);
			expect(service.getWorkItemAgePercentiles).toHaveBeenCalledWith(
				entity.id,
				expectedPreviousStart,
				expectedPreviousEnd,
			);
		});

		it("exposes current and previous snapshots as separate readings", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();
			const current: IPercentileValue[] = [{ percentile: 85, value: 9 }];
			const previous: IPercentileValue[] = [{ percentile: 85, value: 4 }];
			vi.mocked(service.getWorkItemAgePercentiles)
				.mockResolvedValueOnce(current)
				.mockResolvedValueOnce(previous);

			const { result } = renderHook(() =>
				useMetricsData(entity, service, startDate, endDate),
			);

			await waitFor(() => {
				expect(result.current.workItemAgePercentilesValues).toEqual(current);
			});

			expect(result.current.previousWorkItemAgePercentilesValues).toEqual(
				previous,
			);
		});
	});

	describe("Systemic cumulative state time fetch", () => {
		it("should populate cumulativeStateTime without disturbing cycle time percentiles", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();
			const cumulative: ICumulativeStateTimeResponse = {
				states: [
					{
						state: "In Progress",
						workflowOrder: 0,
						totalDays: 12.5,
						completedContributionDays: 8,
						ongoingContributionDays: 4.5,
						itemCount: 5,
						completedItemCount: 3,
						ongoingItemCount: 2,
						meanDays: 2.5,
						medianDays: 2,
					},
				],
			};
			const cycleTimePercentiles: IPercentileValue[] = [
				{ percentile: 85, value: 12 },
			];
			vi.mocked(service.getCumulativeStateTimeForTeam).mockResolvedValue(
				cumulative,
			);
			vi.mocked(service.getCycleTimePercentiles).mockResolvedValue(
				cycleTimePercentiles,
			);

			const { result } = renderHook(() =>
				useMetricsData(entity, service, startDate, endDate),
			);

			await waitFor(() => {
				expect(result.current.cumulativeStateTime).toEqual(cumulative);
			});

			expect(service.getCumulativeStateTimeForTeam).toHaveBeenCalledWith(
				entity.id,
				startDate,
				endDate,
			);
			expect(result.current.percentileValues).toEqual(cycleTimePercentiles);
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

		/**
		 * The owner-type discriminator is a five-way conjunction, and a conjunction is exactly the
		 * shape that degrades silently: loosen any one `&&` into `||` and a TEAM service that
		 * happens to expose a single portfolio-shaped method is promoted to a portfolio, firing
		 * four portfolio-only fetches against an endpoint set it does not have.
		 *
		 * Each case drops exactly one probe method and asserts the promotion does not happen; the
		 * control render (all five present) asserts the fetches DO fire, so the case cannot be
		 * satisfied by a discriminator that simply never returns true.
		 */
		it.each(portfolioProbeMethods)(
			"does not treat a service missing %s as a portfolio service",
			async (missingMethod) => {
				const entity = createMockPortfolioEntity();
				const { [missingMethod]: _dropped, ...incompleteService } =
					createMockProjectMetricsService() as unknown as Record<
						string,
						unknown
					>;
				const completeService = createMockProjectMetricsService();

				renderHook(() =>
					useMetricsData(
						entity,
						incompleteService as unknown as IProjectMetricsService,
						startDate,
						endDate,
						portfolioOnlyFetchKeys,
					),
				);
				renderHook(() =>
					useMetricsData(
						entity,
						completeService,
						startDate,
						endDate,
						portfolioOnlyFetchKeys,
					),
				);

				await waitFor(() => {
					expect(
						completeService.getFeatureSizePercentilesInfo,
					).toHaveBeenCalledTimes(1);
				});
				expect(completeService.getSizePercentiles).toHaveBeenCalledTimes(1);
				expect(
					completeService.getAllFeaturesForSizeChart,
				).toHaveBeenCalledTimes(1);
				expect(completeService.getFeatureSizePbc).toHaveBeenCalledTimes(1);
				expect(completeService.getFeatureSizeEstimation).toHaveBeenCalledTimes(
					1,
				);

				for (const probe of portfolioProbeMethods) {
					if (probe === missingMethod) continue;
					expect(
						incompleteService[probe],
						`${probe} must not be fetched while ${missingMethod} is absent`,
					).not.toHaveBeenCalled();
				}
			},
		);

		/**
		 * The team-side twin of the discriminator above, and the same failure mode: the
		 * features-worked-on fetch is reachable only through `isTeamMetricsService`, so a guard
		 * that stopped guarding would call a method the service does not have — a TypeError raised
		 * inside an effect, which is a blank dashboard rather than a missing widget.
		 */
		it("does not fetch features-worked-on info from a service that cannot report it", async () => {
			const entity = createMockEntity();
			const {
				getFeaturesWorkedOnInfo: _dropped,
				...serviceWithoutFeaturesWorkedOn
			} = createMockTeamMetricsService();
			const completeService = createMockTeamMetricsService();
			const featuresWorkedOnKeys: ReadonlySet<MetricsFetchKey> = new Set([
				"featuresWorkedOnInfo",
			]);

			renderHook(() =>
				useMetricsData(
					entity,
					serviceWithoutFeaturesWorkedOn as unknown as ITeamMetricsService,
					startDate,
					endDate,
					featuresWorkedOnKeys,
				),
			);
			renderHook(() =>
				useMetricsData(
					entity,
					completeService,
					startDate,
					endDate,
					featuresWorkedOnKeys,
				),
			);

			await waitFor(() => {
				expect(completeService.getFeaturesWorkedOnInfo).toHaveBeenCalledTimes(
					1,
				);
			});
			expect(serviceWithoutFeaturesWorkedOn).not.toHaveProperty(
				"getFeaturesWorkedOnInfo",
			);
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

		it("fetches blockedCountHistory and populates the blockedCountHistory field", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();
			const mockSnapshots = [
				{ recordedAt: "2026-07-01", blockedCount: 3 },
				{ recordedAt: "2026-07-02", blockedCount: 7 },
			];
			vi.mocked(service.getBlockedCountHistory).mockResolvedValue(
				mockSnapshots,
			);

			const { result } = renderHook(() =>
				useMetricsData(entity, service, startDate, endDate),
			);

			await waitFor(() => {
				expect(result.current.blockedCountHistory).not.toBeNull();
			});

			expect(result.current.blockedCountHistory).toEqual(mockSnapshots);
		});

		/**
		 * DISTILL RED-pending spec — Story 5508 (widget-loose-ends) slice 02, US-03 AC0.
		 *
		 * UPSTREAM-4: this is the seam the defect actually lives at, and the reason a fully green
		 * `blockedTrend.test.ts` never caught it. The hook fetched the history with the dashboard's own
		 * [startDate, endDate]; the controller filters `RecordedAt >= startDate`; `computeBlockedTrend`
		 * then looks for its baseline at `startDate − 1 day` — one day outside the window that was just
		 * fetched. The baseline was therefore unfindable on every instance and every range, and the
		 * widget has never rendered a comparison.
		 *
		 * The selector tests all build histories that contain a pre-boundary snapshot by hand, so they
		 * pass over a widget that cannot work. Only an assertion on the requested RANGE catches this.
		 *
		 * describe.skip = RED scaffold; DELIVER enables it (ADR-025).
		 */
		it("requests blocked-count history from the day BEFORE the range start, so the trend baseline is inside the window (AC0)", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();

			renderHook(() => useMetricsData(entity, service, startDate, endDate));

			await waitFor(() => {
				expect(service.getBlockedCountHistory).toHaveBeenCalled();
			});

			const [, requestedStart, requestedEnd] = vi.mocked(
				service.getBlockedCountHistory,
			).mock.calls[0];

			// startDate is 2024-01-01, so the baseline day is 2023-12-31.
			const baselineDay = new Date(startDate);
			baselineDay.setDate(baselineDay.getDate() - 1);

			// Exactly one day earlier, not merely "earlier": the widening exists so the
			// baseline day lands INSIDE the fetched window, and any wider reach would be a
			// different (unasserted) request. `toBeLessThanOrEqual` passed for an epoch-zero
			// start too, which is how a broken date arithmetic survived mutation testing.
			expect(new Date(requestedStart).getTime()).toBe(baselineDay.getTime());
			expect(new Date(requestedEnd).getTime()).toBe(endDate.getTime());
		});

		it("sets blockedCountHistory to null on fetch error", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();
			vi.mocked(service.getBlockedCountHistory).mockRejectedValue(
				new Error("Service error"),
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

			expect(result.current.blockedCountHistory).toBeNull();
			consoleSpy.mockRestore();
		});
	});

	describe("Category-scoped fetch gating (Bug #5571)", () => {
		function activeKeys(
			...keys: MetricsFetchKey[]
		): ReadonlySet<MetricsFetchKey> {
			return new Set(keys);
		}

		function countServiceCalls(service: object): number {
			let total = 0;
			for (const value of Object.values(service)) {
				if (vi.isMockFunction(value)) {
					total += value.mock.calls.length;
				}
			}
			return total;
		}

		it("skips every fetch whose key is not active", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();

			renderHook(() =>
				useMetricsData(
					entity,
					service,
					startDate,
					endDate,
					activeKeys("throughput"),
				),
			);

			await waitFor(() => {
				expect(service.getThroughput).toHaveBeenCalledTimes(1);
			});

			expect(service.getThroughputPbc).not.toHaveBeenCalled();
			expect(service.getWipPbc).not.toHaveBeenCalled();
			expect(service.getTotalWorkItemAgePbc).not.toHaveBeenCalled();
			expect(service.getCycleTimePbc).not.toHaveBeenCalled();
			expect(service.getArrivalsPbc).not.toHaveBeenCalled();
			expect(service.getAgeInStatePercentiles).not.toHaveBeenCalled();
			expect(service.getCumulativeStateTimeForTeam).not.toHaveBeenCalled();
			expect(service.getWorkInProgressOverTime).not.toHaveBeenCalled();
			expect(service.getEstimationVsCycleTimeData).not.toHaveBeenCalled();
			expect(service.getCycleTimeData).not.toHaveBeenCalled();
			expect(service.getInProgressItems).not.toHaveBeenCalled();
			expect(service.getBlockedItemsAtDate).not.toHaveBeenCalled();
			expect(service.getBlockedCountHistory).not.toHaveBeenCalled();
			expect(service.getFeaturesWorkedOnInfo).not.toHaveBeenCalled();
			expect(mockBlackoutPeriodService.getAll).not.toHaveBeenCalled();
		});

		it("keeps fetching for an active key exactly once across re-renders", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();

			const { rerender } = renderHook(
				({ keys }: { keys: ReadonlySet<MetricsFetchKey> }) =>
					useMetricsData(entity, service, startDate, endDate, keys),
				{ initialProps: { keys: activeKeys("throughput", "arrivals") } },
			);

			await waitFor(() => {
				expect(service.getThroughput).toHaveBeenCalledTimes(1);
			});

			// A fresh Set with identical contents: the gate is a derived primitive, so an
			// unchanged key set must not thrash the effect into a second round trip.
			rerender({ keys: activeKeys("throughput", "arrivals") });
			rerender({ keys: activeKeys("throughput", "arrivals") });

			expect(service.getThroughput).toHaveBeenCalledTimes(1);
			expect(service.getArrivals).toHaveBeenCalledTimes(1);
		});

		it("fetches a newly activated key once without refetching the already-active ones", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();

			const { rerender } = renderHook(
				({ keys }: { keys: ReadonlySet<MetricsFetchKey> }) =>
					useMetricsData(entity, service, startDate, endDate, keys),
				{ initialProps: { keys: activeKeys("throughput") } },
			);

			await waitFor(() => {
				expect(service.getThroughput).toHaveBeenCalledTimes(1);
			});
			expect(service.getWipPbc).not.toHaveBeenCalled();

			rerender({ keys: activeKeys("throughput", "pbcCore") });

			await waitFor(() => {
				expect(service.getWipPbc).toHaveBeenCalledTimes(1);
			});
			expect(service.getTotalWorkItemAgePbc).toHaveBeenCalledTimes(1);
			expect(service.getThroughput).toHaveBeenCalledTimes(1);

			rerender({ keys: activeKeys("throughput", "pbcCore") });

			expect(service.getWipPbc).toHaveBeenCalledTimes(1);
			expect(service.getThroughput).toHaveBeenCalledTimes(1);
		});

		it("fetches everything when no key set is supplied", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();

			renderHook(() => useMetricsData(entity, service, startDate, endDate));

			await waitFor(() => {
				expect(service.getThroughput).toHaveBeenCalledTimes(1);
			});

			expect(service.getThroughputPbc).toHaveBeenCalledTimes(1);
			expect(service.getAgeInStatePercentiles).toHaveBeenCalledTimes(1);
			expect(service.getCumulativeStateTimeForTeam).toHaveBeenCalledTimes(1);
			expect(service.getWorkInProgressOverTime).toHaveBeenCalledTimes(1);
			expect(service.getEstimationVsCycleTimeData).toHaveBeenCalledTimes(1);
			expect(mockBlackoutPeriodService.getAll).toHaveBeenCalledTimes(1);
		});

		it("separates the cumulative-state-time fetch from the cycle-time batch", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();

			renderHook(() =>
				useMetricsData(
					entity,
					service,
					startDate,
					endDate,
					activeKeys("cumulativeStateTime"),
				),
			);

			await waitFor(() => {
				expect(service.getCumulativeStateTimeForTeam).toHaveBeenCalledTimes(1);
			});

			expect(service.getCycleTimeData).not.toHaveBeenCalled();
			expect(service.getCycleTimePercentiles).not.toHaveBeenCalled();
			expect(service.getWorkItemAgePercentiles).not.toHaveBeenCalled();
			expect(service.getAgeInStatePercentiles).not.toHaveBeenCalled();
			expect(service.getFlowEfficiencyInfoForTeam).not.toHaveBeenCalled();
		});

		it("separates the age-in-state fetch from the cycle-time batch", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();

			renderHook(() =>
				useMetricsData(
					entity,
					service,
					startDate,
					endDate,
					activeKeys("ageInStatePercentiles"),
				),
			);

			await waitFor(() => {
				expect(service.getAgeInStatePercentiles).toHaveBeenCalledTimes(1);
			});

			expect(service.getCycleTimeData).not.toHaveBeenCalled();
			expect(service.getCumulativeStateTimeForTeam).not.toHaveBeenCalled();
		});

		it("keeps the cycle-time batch parallel when its own keys are active", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();

			renderHook(() =>
				useMetricsData(
					entity,
					service,
					startDate,
					endDate,
					activeKeys(
						"cycleTimeData",
						"cycleTimePercentiles",
						"workItemAgePercentiles",
						"flowEfficiency",
					),
				),
			);

			await waitFor(() => {
				expect(service.getCycleTimeData).toHaveBeenCalledTimes(1);
			});

			expect(service.getCycleTimePercentiles).toHaveBeenCalledTimes(1);
			// current period + previous period (D5)
			expect(service.getWorkItemAgePercentiles).toHaveBeenCalledTimes(2);
			expect(service.getFlowEfficiencyInfoForTeam).toHaveBeenCalledTimes(1);
			expect(service.getAgeInStatePercentiles).not.toHaveBeenCalled();
			expect(service.getCumulativeStateTimeForTeam).not.toHaveBeenCalled();
		});

		it("splits the core process-behaviour charts from the chart-only ones", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();

			renderHook(() =>
				useMetricsData(
					entity,
					service,
					startDate,
					endDate,
					activeKeys("pbcCore"),
				),
			);

			await waitFor(() => {
				expect(service.getWipPbc).toHaveBeenCalledTimes(1);
			});

			expect(service.getTotalWorkItemAgePbc).toHaveBeenCalledTimes(1);
			expect(service.getThroughputPbc).not.toHaveBeenCalled();
			expect(service.getCycleTimePbc).not.toHaveBeenCalled();
			expect(service.getArrivalsPbc).not.toHaveBeenCalled();
		});

		it("fetches only the chart-only process-behaviour charts for pbcCharts", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();

			renderHook(() =>
				useMetricsData(
					entity,
					service,
					startDate,
					endDate,
					activeKeys("pbcCharts"),
				),
			);

			await waitFor(() => {
				expect(service.getThroughputPbc).toHaveBeenCalledTimes(1);
			});

			expect(service.getCycleTimePbc).toHaveBeenCalledTimes(1);
			expect(service.getArrivalsPbc).toHaveBeenCalledTimes(1);
			expect(service.getWipPbc).not.toHaveBeenCalled();
			expect(service.getTotalWorkItemAgePbc).not.toHaveBeenCalled();
		});

		it("splits the work-in-progress-over-time fetch from the in-progress items fetch", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();

			renderHook(() =>
				useMetricsData(
					entity,
					service,
					startDate,
					endDate,
					activeKeys("inProgressItems"),
				),
			);

			await waitFor(() => {
				expect(service.getInProgressItems).toHaveBeenCalledTimes(1);
			});

			expect(service.getWorkInProgressOverTime).not.toHaveBeenCalled();
			expect(service.getBlockedItemsAtDate).not.toHaveBeenCalled();
		});

		it("splits the portfolio feature-size group by consumer", async () => {
			const entity = createMockPortfolioEntity();
			const service = createMockProjectMetricsService();

			renderHook(() =>
				useMetricsData(
					entity,
					service,
					startDate,
					endDate,
					activeKeys("featureSizeData"),
				),
			);

			await waitFor(() => {
				expect(service.getSizePercentiles).toHaveBeenCalledTimes(1);
			});

			expect(service.getAllFeaturesForSizeChart).toHaveBeenCalledTimes(1);
			expect(service.getFeatureSizePbc).not.toHaveBeenCalled();
			expect(service.getFeatureSizeEstimation).not.toHaveBeenCalled();
			expect(service.getFeatureSizePercentilesInfo).not.toHaveBeenCalled();
		});

		it("fetches the feature-size percentiles info without the estimation data", async () => {
			const entity = createMockPortfolioEntity();
			const service = createMockProjectMetricsService();

			renderHook(() =>
				useMetricsData(
					entity,
					service,
					startDate,
					endDate,
					activeKeys("featureSizePercentilesInfo"),
				),
			);

			await waitFor(() => {
				expect(service.getFeatureSizePercentilesInfo).toHaveBeenCalledTimes(1);
			});

			expect(service.getFeatureSizeEstimation).not.toHaveBeenCalled();
			expect(service.getSizePercentiles).not.toHaveBeenCalled();
			expect(service.getFeatureSizePbc).not.toHaveBeenCalled();
		});

		it("still refetches an active key when the date window moves", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();
			const laterEnd = new Date("2024-07-31");

			const { rerender } = renderHook(
				({ end }: { end: Date }) =>
					useMetricsData(
						entity,
						service,
						startDate,
						end,
						activeKeys("throughput"),
					),
				{ initialProps: { end: endDate } },
			);

			await waitFor(() => {
				expect(service.getThroughput).toHaveBeenCalledTimes(1);
			});

			rerender({ end: laterEnd });

			await waitFor(() => {
				expect(service.getThroughput).toHaveBeenCalledTimes(2);
			});
			expect(service.getThroughput).toHaveBeenLastCalledWith(
				entity.id,
				startDate,
				laterEnd,
			);
		});

		it("holds the first-open request budget for the default team view", async () => {
			const entity = createMockEntity();
			const service = createMockTeamMetricsService();

			renderHook(() =>
				useMetricsData(
					entity,
					service,
					startDate,
					endDate,
					getFetchKeysForCategories(["flow-overview"], "team"),
				),
			);

			await waitFor(() => {
				expect(service.getThroughput).toHaveBeenCalledTimes(1);
			});

			// Measured baseline before the gate: 29 calls from this hook alone (RCA §1.1). The
			// budget is asserted exactly so that a fetch added without a fetch-key declaration
			// cannot slip back onto the default view unnoticed (RCA root cause B).
			const total =
				countServiceCalls(service) +
				countServiceCalls(mockBlackoutPeriodService);
			expect(total).toBe(19);
		});

		/**
		 * The zero point of the gate. Every other test in this block hands over a key set that
		 * happens to contain the keys it asserts on, so no test ever exercises a guard from the
		 * excluded side — a guard that stopped guarding would still look green everywhere else.
		 *
		 * The portfolio-shaped mock is a strict superset of the team-shaped one (it spreads it and
		 * adds the five portfolio probes), so a single empty-key render covers every gated effect
		 * on both sides. The service methods are enumerated from the mock rather than listed by
		 * hand, so a fetch added to the hook later is held to this budget automatically.
		 */
		it("performs no service call at all when the active key set is empty", async () => {
			const entity = createMockPortfolioEntity();
			const service = createMockProjectMetricsService();
			// An independent render whose key IS active. Its fetch landing is what proves the
			// effects have flushed, so the assertions below cannot pass vacuously simply because
			// nothing has had a chance to run yet.
			const controlService = createMockTeamMetricsService();

			renderHook(() =>
				useMetricsData(entity, service, startDate, endDate, activeKeys()),
			);
			renderHook(() =>
				useMetricsData(
					entity,
					controlService,
					startDate,
					endDate,
					activeKeys("throughput"),
				),
			);

			await waitFor(() => {
				expect(controlService.getThroughput).toHaveBeenCalledTimes(1);
			});

			for (const [methodName, method] of Object.entries(service)) {
				if (!vi.isMockFunction(method)) continue;
				expect(
					method,
					`${methodName} must not be called for an empty key set`,
				).not.toHaveBeenCalled();
			}
			expect(mockBlackoutPeriodService.getAll).not.toHaveBeenCalled();
			expect(countServiceCalls(service)).toBe(0);
		});
	});
});
