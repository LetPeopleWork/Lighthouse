import type { IBoard } from "../../models/Board";
import { BaseApiService } from "./BaseApiService";

export interface IWizardService {
	getJiraBoards(workTrackingSystemConnectionId: number): Promise<IBoard[]>;
}

export class WizardService extends BaseApiService implements IWizardService {
	async getJiraBoards(
		workTrackingSystemConnectionId: number,
	): Promise<IBoard[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IBoard[]>(
				`/wizards/jira/boards/${workTrackingSystemConnectionId}`,
			);

			return response.data;
		});
	}
}
