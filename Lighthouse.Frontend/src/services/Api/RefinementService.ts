import type { IRefinementView } from "../../models/Refinement/Refinement";
import { BaseApiService } from "./BaseApiService";

// RED scaffold written by DISTILL for Epic #6136; DELIVER replaces the body and removes the marker.
export const __SCAFFOLD__ = true;

export interface IRefinementService {
	getRefinement(teamId: number): Promise<IRefinementView>;
}

export class RefinementService
	extends BaseApiService
	implements IRefinementService
{
	public async getRefinement(teamId: number): Promise<IRefinementView> {
		throw new Error(
			`Not yet implemented -- RED scaffold: the Refinement tab of Team ${teamId}`,
		);
	}
}
