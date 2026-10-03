import type { IRefinementView } from "../../models/Refinement/Refinement";
import { BaseApiService } from "./BaseApiService";

export interface IRefinementService {
	getRefinement(teamId: number): Promise<IRefinementView>;
}

export class RefinementService
	extends BaseApiService
	implements IRefinementService
{
	public async getRefinement(teamId: number): Promise<IRefinementView> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IRefinementView>(
				`/teams/${teamId}/refinement`,
			);
			return response.data;
		});
	}
}
