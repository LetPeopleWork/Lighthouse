import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
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
