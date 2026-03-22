import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { IBlackoutPeriod } from "../../models/BlackoutPeriod";
import { BlackoutPeriodService } from "./BlackoutPeriodService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

describe("BlackoutPeriodService", () => {
	let service: BlackoutPeriodService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		service = new BlackoutPeriodService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("should get all blackout periods", async () => {
		const mockPeriods: IBlackoutPeriod[] = [
			{
				id: 1,
				start: "2026-01-01",
				end: "2026-01-05",
				description: "New Year",
			},
			{ id: 2, start: "2026-06-01", end: "2026-06-03", description: "Summer" },
		];
		mockedAxios.get.mockResolvedValueOnce({ data: mockPeriods });

		const result = await service.getAll();

		expect(result).toEqual(mockPeriods);
		expect(mockedAxios.get).toHaveBeenCalledWith("/blackout-periods");
	});

	it("should create a blackout period", async () => {
		const newPeriod = {
			start: "2026-04-10",
			end: "2026-04-15",
			description: "Spring break",
		};
		const createdPeriod: IBlackoutPeriod = { id: 1, ...newPeriod };
		mockedAxios.post.mockResolvedValueOnce({ data: createdPeriod });

		const result = await service.create(newPeriod);

		expect(result).toEqual(createdPeriod);
		expect(mockedAxios.post).toHaveBeenCalledWith(
			"/blackout-periods",
			newPeriod,
		);
	});

	it("should update a blackout period", async () => {
		const updateData = {
			start: "2026-05-01",
			end: "2026-05-10",
			description: "Updated",
		};
		const updatedPeriod: IBlackoutPeriod = { id: 1, ...updateData };
		mockedAxios.put.mockResolvedValueOnce({ data: updatedPeriod });

		const result = await service.update(1, updateData);

		expect(result).toEqual(updatedPeriod);
		expect(mockedAxios.put).toHaveBeenCalledWith(
			"/blackout-periods/1",
			updateData,
		);
	});

	it("should delete a blackout period", async () => {
		mockedAxios.delete.mockResolvedValueOnce({});

		await service.delete(1);

		expect(mockedAxios.delete).toHaveBeenCalledWith("/blackout-periods/1");
	});
});
