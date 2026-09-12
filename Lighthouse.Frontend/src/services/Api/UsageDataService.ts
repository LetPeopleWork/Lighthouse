import type {
	IUsageDataState,
	UsageDataDecisionValue,
} from "../../models/UsageData/UsageData";
import { BaseApiService } from "./BaseApiService";

const CONSENT_TOKEN_HEADER = "X-Lighthouse-UsageData-Token";

export interface IUsageDataService {
	getState(token: string | null): Promise<IUsageDataState>;
	recordDecision(decision: UsageDataDecisionValue): Promise<string>;
	revoke(token: string): Promise<void>;
}

export class UsageDataService
	extends BaseApiService
	implements IUsageDataService
{
	async getState(token: string | null): Promise<IUsageDataState> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IUsageDataState>(
				"/usagedata/state",
				token ? { headers: { [CONSENT_TOKEN_HEADER]: token } } : undefined,
			);
			return response.data;
		});
	}

	async recordDecision(decision: UsageDataDecisionValue): Promise<string> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.post<{ token: string }>(
				"/usagedata/consent",
				{ decision },
			);
			return response.data.token;
		});
	}

	async revoke(token: string): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.delete("/usagedata/consent", {
				headers: { [CONSENT_TOKEN_HEADER]: token },
			});
		});
	}
}
