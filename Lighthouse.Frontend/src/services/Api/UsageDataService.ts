import type {
	IUsageDataState,
	UsageDataDecisionValue,
	UsageDataRouteKey,
	UsageDataWorkTrackingSystem,
} from "../../models/UsageData/UsageData";
import { BaseApiService } from "./BaseApiService";

const CONSENT_TOKEN_HEADER = "X-Lighthouse-UsageData-Token";

/**
 * Everything a browser is allowed to report. A name that is not on this list is not an unknown
 * event to be dealt with later - the server refuses to read the message at all.
 *
 * The values are the words rather than numbers because that is how the server both answers and
 * reads them; a numbered mirror would name whichever member happens to sit at that position.
 */
export const UsageDataEventName = {
	TeamTabOpened: "TeamTabOpened",
	PortfolioTabOpened: "PortfolioTabOpened",
	TeamCreated: "TeamCreated",
	TeamDeleted: "TeamDeleted",
	PortfolioCreated: "PortfolioCreated",
	PortfolioDeleted: "PortfolioDeleted",
	TeamManualForecastRun: "TeamManualForecastRun",
	WorkTrackingSystemConnected: "WorkTrackingSystemConnected",
	TeamRefreshTriggered: "TeamRefreshTriggered",
	PortfolioRefreshTriggered: "PortfolioRefreshTriggered",
} as const;

export type UsageDataEventName =
	(typeof UsageDataEventName)[keyof typeof UsageDataEventName];

/**
 * One thing that happened. Nothing here is text: choices from closed lists and numbers, so there is
 * no field in which a page address, a name somebody picked or a sentence somebody typed could
 * travel - not because the sender is careful, but because no such field exists.
 *
 * Only the two tab openings say which page somebody was on. The rest are things somebody did, which
 * happen on no particular page, and the server refuses one that names a page anyway.
 */
export interface IUsageDataEvent {
	name: UsageDataEventName;
	route?: UsageDataRouteKey;
	/** Only on a connection being set up, and only ever the kind of system, never one of yours. */
	workTrackingSystem?: UsageDataWorkTrackingSystem;
	/** How long before this batch was handed in the thing happened, so a reader can order them. */
	offsetMs: number;
	sequence: number;
}

export interface IUsageDataService {
	getState(token: string | null): Promise<IUsageDataState>;
	recordDecision(decision: UsageDataDecisionValue): Promise<string>;
	revoke(token: string): Promise<void>;
	acknowledgeAsked(token: string | null): Promise<void>;
	postEvents(token: string, events: IUsageDataEvent[]): Promise<void>;
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

	/**
	 * Tells the server this browser has just been shown the dialog uninvited, so the next window is
	 * measured from the question rather than from an answer that did not change. Sent without a
	 * token too, where it does nothing: the browser has no row to be recorded against and remembers
	 * this itself, and calling either way keeps the caller from branching on something it cannot see.
	 */
	async acknowledgeAsked(token: string | null): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.post(
				"/usagedata/asked",
				undefined,
				token ? { headers: { [CONSENT_TOKEN_HEADER]: token } } : undefined,
			);
		});
	}

	async postEvents(token: string, events: IUsageDataEvent[]): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.post(
				"/usagedata/events",
				{ events },
				{ headers: { [CONSENT_TOKEN_HEADER]: token } },
			);
		});
	}
}
