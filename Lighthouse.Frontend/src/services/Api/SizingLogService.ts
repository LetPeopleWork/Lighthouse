import type { AxiosRequestConfig } from "axios";
import type {
	IRefinementRow,
	ISizingComment,
	ISizingLog,
	ISizingVote,
	IVotedRow,
	SizingAnswer,
} from "../../models/Refinement/Refinement";
import { BaseApiService } from "./BaseApiService";

const VOTER_KEY_HEADER = "X-Lighthouse-Voter-Key";

/** A browser without a key - every signed-in one - sends no header at all, never an empty one. */
export const withVoterKey = (voterKey: string | null): AxiosRequestConfig =>
	voterKey === null ? {} : { headers: { [VOTER_KEY_HEADER]: voterKey } };

/** A Work Item reference may hold a slash or a hash, and must stay one segment of the address. */
const workItemAddress = (teamId: number, workItemReference: string): string =>
	`/teams/${teamId}/refinement/work-items/${encodeURIComponent(workItemReference)}`;

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
	): Promise<IVotedRow>;
	addComment(
		teamId: number,
		workItemReference: string,
		comment: ISizingComment,
		voterKey: string | null,
	): Promise<IRefinementRow>;
	/** Names the answer the voter saw pressed, so a vote they changed since on another device stays. */
	takeBackMyVote(
		teamId: number,
		workItemReference: string,
		answer: SizingAnswer,
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
	): Promise<IVotedRow> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.post<IVotedRow>(
				`${workItemAddress(teamId, workItemReference)}/votes`,
				vote,
				withVoterKey(voterKey),
			);
			return response.data;
		});
	}

	public async addComment(
		teamId: number,
		workItemReference: string,
		comment: ISizingComment,
		voterKey: string | null,
	): Promise<IRefinementRow> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.post<IRefinementRow>(
				`${workItemAddress(teamId, workItemReference)}/comments`,
				comment,
				withVoterKey(voterKey),
			);
			return response.data;
		});
	}

	public async takeBackMyVote(
		teamId: number,
		workItemReference: string,
		answer: SizingAnswer,
		voterKey: string | null,
	): Promise<IRefinementRow> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.delete<IRefinementRow>(
				`${workItemAddress(teamId, workItemReference)}/votes/mine?answer=${answer}`,
				withVoterKey(voterKey),
			);
			return response.data;
		});
	}

	public async getLog(
		teamId: number,
		workItemReference: string,
		voterKey: string | null,
	): Promise<ISizingLog> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<ISizingLog>(
				`${workItemAddress(teamId, workItemReference)}/log`,
				withVoterKey(voterKey),
			);
			return response.data;
		});
	}
}
