import { BaseApiService } from "./BaseApiService";

export type SurveyNudgeAction = "TakeSurvey" | "RemindLater" | "NoInterest";

export interface SurveyNudgeState {
	nextEligibleAt: string | null;
}

export interface ISurveyNudgeService {
	getState(): Promise<SurveyNudgeState>;
	recordAction(action: SurveyNudgeAction): Promise<void>;
}

export class SurveyNudgeService
	extends BaseApiService
	implements ISurveyNudgeService
{
	async getState(): Promise<SurveyNudgeState> {
		return this.withErrorHandling(async () => {
			const response =
				await this.apiService.get<SurveyNudgeState>("/surveynudge");
			return response.data;
		});
	}

	async recordAction(action: SurveyNudgeAction): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.post("/surveynudge", { action });
		});
	}
}
