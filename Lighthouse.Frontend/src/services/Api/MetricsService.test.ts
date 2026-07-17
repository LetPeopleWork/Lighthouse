import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { ICumulativeStateTimeResponse } from "../../models/Metrics/CumulativeStateTime";
import type { ICumulativeStateTimeCandidatesResponse } from "../../models/Metrics/CumulativeStateTimeCandidates";
import type { ICumulativeStateTimeItemsResponse } from "../../models/Metrics/CumulativeStateTimeItems";
import type { IFlowEfficiencyInfo } from "../../models/Metrics/FlowEfficiencyInfo";
import type { IPerStatePercentileValues } from "../../models/PerStatePercentileValues";
import { TeamMetricsService } from "./TeamMetricsService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

function getMockPerStatePercentileValues(
	overrides?: Partial<IPerStatePercentileValues>,
): IPerStatePercentileValues {
	return {
		state: "In Progress",
		percentiles: [
			{ percentile: 50, value: 3 },
			{ percentile: 85, value: 9 },
		],
		...overrides,
	};
}

function getMockCumulativeStateTimeResponse(
	overrides?: Partial<ICumulativeStateTimeResponse>,
): ICumulativeStateTimeResponse {
	return {
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
		...overrides,
	};
}

describe("MetricsService getAgeInStatePercentiles", () => {
	let metricsService: TeamMetricsService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		metricsService = new TeamMetricsService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("fetches per-state percentile values for an entity and window", async () => {
		const perState: IPerStatePercentileValues[] = [
			getMockPerStatePercentileValues({ state: "In Progress" }),
			getMockPerStatePercentileValues({
				state: "Review",
				percentiles: [{ percentile: 50, value: 1 }],
			}),
		];
		mockedAxios.get.mockResolvedValueOnce({ data: perState });

		const startDate = new Date("2023-01-01");
		const endDate = new Date("2023-01-31");
		const result = await metricsService.getAgeInStatePercentiles(
			7,
			startDate,
			endDate,
		);

		expect(result).toEqual(perState);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/7/metrics/ageInStatePercentiles?startDate=2023-01-01&endDate=2023-01-31",
		);
	});

	it("propagates errors when fetching per-state percentiles fails", async () => {
		mockedAxios.get.mockRejectedValueOnce(new Error("Network Error"));

		await expect(
			metricsService.getAgeInStatePercentiles(1, new Date(), new Date()),
		).rejects.toThrow("Network Error");
	});
});

describe("MetricsService getWorkItemAgePercentiles", () => {
	let metricsService: TeamMetricsService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		metricsService = new TeamMetricsService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("fetches and parses work item age percentiles for an entity and window", async () => {
		const percentiles = [
			{ percentile: 50, value: 4 },
			{ percentile: 85, value: 9 },
		];
		mockedAxios.get.mockResolvedValueOnce({ data: percentiles });

		const result = await metricsService.getWorkItemAgePercentiles(
			7,
			new Date("2023-01-01"),
			new Date("2023-01-31"),
		);

		expect(result).toEqual(percentiles);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/7/metrics/workItemAgePercentiles?startDate=2023-01-01&endDate=2023-01-31",
		);
	});

	it("propagates errors when fetching work item age percentiles fails", async () => {
		mockedAxios.get.mockRejectedValueOnce(new Error("Network Error"));

		await expect(
			metricsService.getWorkItemAgePercentiles(1, new Date(), new Date()),
		).rejects.toThrow("Network Error");
	});
});

describe("MetricsService getCumulativeStateTimeForTeam", () => {
	let metricsService: TeamMetricsService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		metricsService = new TeamMetricsService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("fetches the systemic per-state cumulative bar data for a team and window", async () => {
		const response = getMockCumulativeStateTimeResponse();
		mockedAxios.get.mockResolvedValueOnce({ data: response });

		const startDate = new Date("2023-01-01");
		const endDate = new Date("2023-01-31");
		const result = await metricsService.getCumulativeStateTimeForTeam(
			7,
			startDate,
			endDate,
		);

		expect(result).toEqual(response);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/7/metrics/cumulativeStateTime?startDate=2023-01-01&endDate=2023-01-31",
		);
	});

	it("appends the definitionId query param when scoping to a named cycle time", async () => {
		const response = getMockCumulativeStateTimeResponse();
		mockedAxios.get.mockResolvedValueOnce({ data: response });

		await metricsService.getCumulativeStateTimeForTeam(
			7,
			new Date("2023-01-01"),
			new Date("2023-01-31"),
			undefined,
			3,
		);

		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/7/metrics/cumulativeStateTime?startDate=2023-01-01&endDate=2023-01-31&definitionId=3",
		);
	});
});

function getMockCumulativeStateTimeItemsResponse(
	overrides?: Partial<ICumulativeStateTimeItemsResponse>,
): ICumulativeStateTimeItemsResponse {
	return {
		state: "Review",
		items: [
			{
				workItemId: 1,
				referenceId: "ITEM-1",
				title: "Item one",
				type: "Story",
				state: "Review",
				stateCategory: "Doing",
				url: null,
				daysContributed: 4,
			},
		],
		...overrides,
	};
}

describe("MetricsService getCumulativeStateTimeItemsForTeam", () => {
	let metricsService: TeamMetricsService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		metricsService = new TeamMetricsService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("fetches the contributing items for a state, team and window", async () => {
		const response = getMockCumulativeStateTimeItemsResponse();
		mockedAxios.get.mockResolvedValueOnce({ data: response });

		const result = await metricsService.getCumulativeStateTimeItemsForTeam(
			7,
			"Review",
			new Date("2023-01-01"),
			new Date("2023-01-31"),
		);

		expect(result).toEqual(response);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/7/metrics/cumulativeStateTime/items?startDate=2023-01-01&endDate=2023-01-31&state=Review",
		);
	});

	it("appends each selected itemId as a repeated query parameter", async () => {
		const response = getMockCumulativeStateTimeItemsResponse();
		mockedAxios.get.mockResolvedValueOnce({ data: response });

		await metricsService.getCumulativeStateTimeItemsForTeam(
			7,
			"In Review",
			new Date("2023-01-01"),
			new Date("2023-01-31"),
			[11, 22],
		);

		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/7/metrics/cumulativeStateTime/items?startDate=2023-01-01&endDate=2023-01-31&state=In%20Review&itemIds=11&itemIds=22",
		);
	});
});

describe("MetricsService getCumulativeStateTimeItemsForPortfolio", () => {
	let metricsService: TeamMetricsService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		metricsService = new TeamMetricsService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("fetches the contributing items for a state, portfolio and window", async () => {
		const response = getMockCumulativeStateTimeItemsResponse({
			state: "Testing",
		});
		mockedAxios.get.mockResolvedValueOnce({ data: response });

		const result = await metricsService.getCumulativeStateTimeItemsForPortfolio(
			9,
			"Testing",
			new Date("2023-01-01"),
			new Date("2023-01-31"),
		);

		expect(result).toEqual(response);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/9/metrics/cumulativeStateTime/items?startDate=2023-01-01&endDate=2023-01-31&state=Testing",
		);
	});
});

function getMockCumulativeStateTimeCandidatesResponse(
	overrides?: Partial<ICumulativeStateTimeCandidatesResponse>,
): ICumulativeStateTimeCandidatesResponse {
	return {
		items: [
			{
				workItemId: 11,
				referenceId: "ITEM-11",
				title: "Build the thing",
				workItemType: "Story",
			},
		],
		...overrides,
	};
}

describe("MetricsService cumulative state time candidates", () => {
	let metricsService: TeamMetricsService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		metricsService = new TeamMetricsService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("fetches the candidate pool for a team and window", async () => {
		const response = getMockCumulativeStateTimeCandidatesResponse();
		mockedAxios.get.mockResolvedValueOnce({ data: response });

		const result = await metricsService.getCumulativeStateTimeCandidatesForTeam(
			7,
			new Date("2023-01-01"),
			new Date("2023-01-31"),
		);

		expect(result).toEqual(response);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/7/metrics/cumulativeStateTime/candidates?startDate=2023-01-01&endDate=2023-01-31",
		);
	});

	it("fetches the candidate pool for a portfolio and window", async () => {
		const response = getMockCumulativeStateTimeCandidatesResponse({
			items: [],
		});
		mockedAxios.get.mockResolvedValueOnce({ data: response });

		const result =
			await metricsService.getCumulativeStateTimeCandidatesForPortfolio(
				9,
				new Date("2023-01-01"),
				new Date("2023-01-31"),
			);

		expect(result).toEqual(response);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/9/metrics/cumulativeStateTime/candidates?startDate=2023-01-01&endDate=2023-01-31",
		);
	});
});

function getMockFlowEfficiencyInfo(
	overrides?: Partial<IFlowEfficiencyInfo>,
): IFlowEfficiencyInfo {
	return {
		isConfigured: true,
		hasDataInScope: true,
		efficiencyPercent: 72,
		totalDoingDays: 40,
		waitDays: 11,
		...overrides,
	};
}

describe("MetricsService flow efficiency info", () => {
	let metricsService: TeamMetricsService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		metricsService = new TeamMetricsService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("fetches the whole-set flow efficiency info for a team and window", async () => {
		const response = getMockFlowEfficiencyInfo();
		mockedAxios.get.mockResolvedValueOnce({ data: response });

		const result = await metricsService.getFlowEfficiencyInfoForTeam(
			7,
			new Date("2023-01-01"),
			new Date("2023-01-31"),
		);

		expect(result).toEqual(response);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/7/metrics/flowEfficiencyInfo?startDate=2023-01-01&endDate=2023-01-31",
		);
	});

	it("fetches the whole-set flow efficiency info for a portfolio and window", async () => {
		const response = getMockFlowEfficiencyInfo({
			isConfigured: false,
			hasDataInScope: false,
			efficiencyPercent: 0,
			totalDoingDays: 0,
			waitDays: 0,
		});
		mockedAxios.get.mockResolvedValueOnce({ data: response });

		const result = await metricsService.getFlowEfficiencyInfoForPortfolio(
			9,
			new Date("2023-01-01"),
			new Date("2023-01-31"),
		);

		expect(result).toEqual(response);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/9/metrics/flowEfficiencyInfo?startDate=2023-01-01&endDate=2023-01-31",
		);
	});
});

describe("MetricsService cycle time percentiles with definitionId", () => {
	let metricsService: TeamMetricsService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		metricsService = new TeamMetricsService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("omits the definitionId query param for the default percentiles", async () => {
		mockedAxios.get.mockResolvedValueOnce({ data: [] });

		await metricsService.getCycleTimePercentiles(
			7,
			new Date("2023-01-01"),
			new Date("2023-01-31"),
		);

		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/7/metrics/cycleTimePercentiles?startDate=2023-01-01&endDate=2023-01-31",
		);
	});

	it("appends the definitionId query param for a named definition", async () => {
		mockedAxios.get.mockResolvedValueOnce({ data: [] });

		await metricsService.getCycleTimePercentiles(
			7,
			new Date("2023-01-01"),
			new Date("2023-01-31"),
			1,
		);

		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/7/metrics/cycleTimePercentiles?startDate=2023-01-01&endDate=2023-01-31&definitionId=1",
		);
	});
});

describe("MetricsService cycle time percentiles info with definitionId", () => {
	let metricsService: TeamMetricsService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		metricsService = new TeamMetricsService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("omits the definitionId query param for the default trend info", async () => {
		mockedAxios.get.mockResolvedValueOnce({
			data: { percentiles: [], comparison: {} },
		});

		await metricsService.getCycleTimePercentilesInfo(
			7,
			new Date("2023-01-01"),
			new Date("2023-01-31"),
		);

		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/7/metrics/cycleTimePercentilesInfo?startDate=2023-01-01&endDate=2023-01-31",
		);
	});

	it("appends the definitionId query param for a named definition trend info", async () => {
		mockedAxios.get.mockResolvedValueOnce({
			data: { percentiles: [], comparison: {} },
		});

		await metricsService.getCycleTimePercentilesInfo(
			7,
			new Date("2023-01-01"),
			new Date("2023-01-31"),
			1,
		);

		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/7/metrics/cycleTimePercentilesInfo?startDate=2023-01-01&endDate=2023-01-31&definitionId=1",
		);
	});
});

describe("MetricsService getCycleTimeData named cycle times", () => {
	let metricsService: TeamMetricsService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		metricsService = new TeamMetricsService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	const rawItem = (namedCycleTimes: unknown) => ({
		id: 1,
		name: "PHX-204",
		referenceId: "PHX-204",
		url: null,
		state: "Done",
		stateCategory: "Done",
		type: "Story",
		startedDate: "2023-01-01T00:00:00Z",
		closedDate: "2023-01-15T00:00:00Z",
		cycleTime: 14,
		workItemAge: 0,
		parentWorkItemReference: "",
		isBlocked: false,
		namedCycleTimes,
	});

	it("keeps the validated named cycle times from the response", async () => {
		mockedAxios.get.mockResolvedValueOnce({
			data: [rawItem([{ definitionId: 1, days: 47 }])],
		});

		const result = await metricsService.getCycleTimeData(
			7,
			new Date("2023-01-01"),
			new Date("2023-01-31"),
		);

		expect(result[0].namedCycleTimes).toEqual([{ definitionId: 1, days: 47 }]);
	});

	it("falls back to an empty list when the named cycle times are malformed", async () => {
		mockedAxios.get.mockResolvedValueOnce({
			data: [rawItem([{ definitionId: "nope" }])],
		});

		const result = await metricsService.getCycleTimeData(
			7,
			new Date("2023-01-01"),
			new Date("2023-01-31"),
		);

		expect(result[0].namedCycleTimes).toEqual([]);
	});
});
