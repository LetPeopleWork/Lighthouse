import type { IRefinementView } from "../../models/Refinement/Refinement";
import { readVoterKey } from "../Refinement/voterStore";
import { BaseApiService } from "./BaseApiService";
import { withVoterKey } from "./SizingLogService";

export interface IRefinementService {
	getRefinement(teamId: number): Promise<IRefinementView>;
}

export class RefinementService
	extends BaseApiService
	implements IRefinementService
{
	public async getRefinement(teamId: number): Promise<IRefinementView> {
		return this.withErrorHandling(async () => {
			const address = `/teams/${teamId}/refinement`;
			const voterKey = readVoterKey();
			const response =
				voterKey === null
					? await this.apiService.get<IRefinementView>(address)
					: await this.apiService.get<IRefinementView>(
							address,
							withVoterKey(voterKey),
						);
			return response.data;
		});
	}
}
