import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { IBoard } from "../../models/Boards/Board";
import type { IBoardInformation } from "../../models/Boards/BoardInformation";
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
				`/wizards/jira/${workTrackingSystemConnectionId}/boards`,
			);
			expect(result).toHaveLength(2);
			expect(result[0].name).toBe("Epic Board");
			expect(result[1].name).toBe("Story Board");
		});

		it("should return Jira Board Information", async () => {
			const workTrackingSystemConnectionId = 1;
			const boardId = 8;

			const mobckBoardInfo: IBoardInformation = {
				dataRetrievalValue: "someValue",
				workItemTypes: ["Type1", "Type2"],
				toDoStates: ["ToDo1", "ToDo2"],
				doingStates: ["Doing1", "Doing2"],
				doneStates: ["Done1", "Done2"],
			};

			mockedAxios.get.mockResolvedValue({
				data: mobckBoardInfo,
			});

			// Act
			const result = await wizardService.getJiraBoardInformation(
				workTrackingSystemConnectionId,
				boardId,
			);

			// Assert
			expect(mockedAxios.get).toHaveBeenCalledWith(
				`/wizards/jira/${workTrackingSystemConnectionId}/boards/${boardId}`,
			);

			expect(result).toEqual(mobckBoardInfo);
			expect(result.dataRetrievalValue).toBe("someValue");
			expect(result.workItemTypes).toContain("Type1");
			expect(result.workItemTypes).toContain("Type2");
			expect(result.toDoStates).toContain("ToDo1");
			expect(result.toDoStates).toContain("ToDo2");
			expect(result.doingStates).toContain("Doing1");
			expect(result.doingStates).toContain("Doing2");
			expect(result.doneStates).toContain("Done1");
			expect(result.doneStates).toContain("Done2");
		});
	});
});
