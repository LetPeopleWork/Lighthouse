import type { IBoard } from "../../models/Boards/Board";
import type { IBoardInformation } from "../../models/Boards/BoardInformation";
import { BaseApiService } from "./BaseApiService";

export interface IWizardService {
	getBoards(workTrackingSystemConnectionId: number): Promise<IBoard[]>;
	getBoardInformation(
		workTrackingSystemConnectionId: number,
		boardId: string,
	): Promise<IBoardInformation>;
}

export class WizardService extends BaseApiService implements IWizardService {
	async getBoards(workTrackingSystemConnectionId: number): Promise<IBoard[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IBoard[]>(
				`/wizards/${workTrackingSystemConnectionId}/boards`,
			);

			return response.data;
		});
	}

	async getBoardInformation(
		workTrackingSystemConnectionId: number,
		boardId: string,
	): Promise<IBoardInformation> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IBoardInformation>(
				`/wizards/${workTrackingSystemConnectionId}/boards/${boardId}`,
			);

			return response.data;
		});
	}
}
