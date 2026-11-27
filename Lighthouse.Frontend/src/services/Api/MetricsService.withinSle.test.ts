import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { TeamMetricsService } from "./TeamMetricsService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

describe("MetricsService Within SLE calls", () => {
	let service: TeamMetricsService;
	const startDate = new Date(2026, 0, 1);
	const endDate = new Date(2026, 0, 31);

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		service = new TeamMetricsService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("asks the team's Within SLE info for the window and returns the answer", async () => {
		const answer = { verdict: "NoSle", closedCount: 0, withinCount: 0 };
		mockedAxios.get.mockResolvedValueOnce({ data: answer });

		const result = await service.getWithinSleInfo(7, startDate, endDate);

		expect(result).toEqual(answer);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/7/metrics/withinSleInfo?startDate=2026-01-01&endDate=2026-01-31",
		);
	});

	it("asks the team's Within SLE over time for the horizon and window and returns the answer", async () => {
		const answer = { horizon: 60, days: [] };
		mockedAxios.get.mockResolvedValueOnce({ data: answer });

		const result = await service.getWithinSleOverTime(
			7,
			60,
			startDate,
			endDate,
		);

		expect(result).toEqual(answer);
		expect(mockedAxios.get).toHaveBeenCalledWith(
			"/teams/7/metrics/within-sle-over-time?horizon=60&startDate=2026-01-01&endDate=2026-01-31",
		);
	});
});
