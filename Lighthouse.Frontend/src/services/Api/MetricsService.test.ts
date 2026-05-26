import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { ICumulativeStateTimeResponse } from "../../models/Metrics/CumulativeStateTime";
import type { ICumulativeStateTimeItemsResponse } from "../../models/Metrics/CumulativeStateTimeItems";
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
});

function getMockCumulativeStateTimeItemsResponse(
	overrides?: Partial<ICumulativeStateTimeItemsResponse>,
): ICumulativeStateTimeItemsResponse {
	return {
		state: "Review",
		items: [
			{
				referenceId: "ITEM-1",
				parentReferenceId: null,
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
