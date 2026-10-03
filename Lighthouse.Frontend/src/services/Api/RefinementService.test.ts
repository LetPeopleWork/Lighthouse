import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { IRefinementView } from "../../models/Refinement/Refinement";
import { RefinementService } from "./RefinementService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

describe("RefinementService", () => {
	let refinementService: RefinementService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		refinementService = new RefinementService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	it("reads a Team's refinement from that Team's address and answers what the server said", async () => {
		const answer: IRefinementView = {
			refinementConfigured: true,
			workItems: [
				{
					referenceId: "GR-058",
					name: "User activity tracking",
					url: "https://tracker.example/browse/GR-058",
					state: "Next",
					parentReferenceId: "GR-010",
				},
			],
		};
		mockedAxios.get.mockResolvedValueOnce({ data: answer });

		const refinement = await refinementService.getRefinement(7);

		expect(refinement).toEqual(answer);
		expect(mockedAxios.get).toHaveBeenCalledWith("/teams/7/refinement");
	});

	it("passes a failed read on to the caller", async () => {
		mockedAxios.get.mockRejectedValueOnce(new Error("Network Error"));

		await expect(refinementService.getRefinement(7)).rejects.toThrow(
			"Network Error",
		);
	});
});
