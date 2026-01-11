import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { IBoard } from "../../models/Board";
import { WizardService } from "./WizardService";

vi.mock("axios");
const mockedAxios = vi.mocked(axios, true);

describe("WizardService", () => {
	let wizardService: WizardService;

	beforeEach(() => {
		mockedAxios.create.mockReturnThis();
		wizardService = new WizardService();
	});

	afterEach(() => {
		vi.resetAllMocks();
	});

	describe("getJiraBoards", () => {
		it("should return Jira Boards", async () => {
			const workTrackingSystemConnectionId = 1;
			const mockBoards: IBoard[] = [
				{
					id: 1,
					name: "Epic Board",
				},
				{
					id: 2,
					name: "Story Board",
				},
			];

			mockedAxios.get.mockResolvedValue({
				data: mockBoards,
			});

			// Act
			const result = await wizardService.getJiraBoards(
				workTrackingSystemConnectionId,
			);

			// Assert
			expect(mockedAxios.get).toHaveBeenCalledWith(
				`/wizards/jira/boards/${workTrackingSystemConnectionId}`,
			);
			expect(result).toHaveLength(2);
			expect(result[0].name).toBe("Epic Board");
			expect(result[1].name).toBe("Story Board");
		});
	});
});
