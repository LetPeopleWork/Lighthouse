import type {
	IRefinementRow,
	ISizingComment,
	ISizingLog,
	ISizingVote,
} from "../../models/Refinement/Refinement";
import { BaseApiService } from "./BaseApiService";

export const __SCAFFOLD__ = true;

const NOT_YET_IMPLEMENTED = "Not yet implemented -- RED scaffold";

/**
 * The writes to a Team's sizing log - a vote, a comment, taking one's vote back - and one Work Item's
 * log. Without sign-in the browser's voter key travels with every call; with sign-in it is ignored.
 */
export interface ISizingLogService {
	castVote(
		teamId: number,
		workItemReference: string,
		vote: ISizingVote,
		voterKey: string | null,
	): Promise<IRefinementRow>;
	addComment(
		teamId: number,
		workItemReference: string,
		comment: ISizingComment,
		voterKey: string | null,
	): Promise<IRefinementRow>;
	takeBackMyVote(
		teamId: number,
		workItemReference: string,
		voterKey: string | null,
	): Promise<IRefinementRow>;
	getLog(
		teamId: number,
		workItemReference: string,
		voterKey: string | null,
	): Promise<ISizingLog>;
}

export class SizingLogService
	extends BaseApiService
	implements ISizingLogService
{
	public async castVote(
		teamId: number,
		workItemReference: string,
		vote: ISizingVote,
		voterKey: string | null,
	): Promise<IRefinementRow> {
		throw new Error(
			`${NOT_YET_IMPLEMENTED}: vote on ${workItemReference} of Team ${teamId} (${vote.answer}, ${voterKey === null ? "no key" : "key"})`,
		);
	}

	public async addComment(
		teamId: number,
		workItemReference: string,
		comment: ISizingComment,
		voterKey: string | null,
	): Promise<IRefinementRow> {
		throw new Error(
			`${NOT_YET_IMPLEMENTED}: comment on ${workItemReference} of Team ${teamId} (${comment.channel}, ${voterKey === null ? "no key" : "key"})`,
		);
	}

	public async takeBackMyVote(
		teamId: number,
		workItemReference: string,
		voterKey: string | null,
	): Promise<IRefinementRow> {
		throw new Error(
			`${NOT_YET_IMPLEMENTED}: take back on ${workItemReference} of Team ${teamId} (${voterKey === null ? "no key" : "key"})`,
		);
	}

	public async getLog(
		teamId: number,
		workItemReference: string,
		voterKey: string | null,
	): Promise<ISizingLog> {
		throw new Error(
			`${NOT_YET_IMPLEMENTED}: log of ${workItemReference} of Team ${teamId} (${voterKey === null ? "no key" : "key"})`,
		);
	}
}
