import type { IBoard } from "../../models/Boards/Board";
import type { IBoardInformation } from "../../models/Boards/BoardInformation";
import { BaseApiService } from "./BaseApiService";

export interface IWizardService {
	getJiraBoards(workTrackingSystemConnectionId: number): Promise<IBoard[]>;
	getJiraBoardInformation(
		workTrackingSystemConnectionId: number,
		boardId: number,
	): Promise<IBoardInformation>;
}

export class WizardService extends BaseApiService implements IWizardService {
	async getJiraBoards(
		workTrackingSystemConnectionId: number,
	): Promise<IBoard[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IBoard[]>(
				`/wizards/jira/${workTrackingSystemConnectionId}/boards`,
			);

			return response.data;
		});
	}

	async getJiraBoardInformation(
		workTrackingSystemConnectionId: number,
		boardId: number,
	): Promise<IBoardInformation> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IBoardInformation>(
				`/wizards/jira/${workTrackingSystemConnectionId}/boards/${boardId}`,
			);

			return response.data;
		});
	}
}
