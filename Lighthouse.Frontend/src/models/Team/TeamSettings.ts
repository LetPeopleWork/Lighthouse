import type { IBaseSettings } from "../Common/BaseSettings";
import type { IRefinementSettings } from "../Refinement/Refinement";

export const DEFAULT_THROUGHPUT_HISTORY_DAYS = 90;
export const DEFAULT_FEATURE_WIP = 0;

export interface ITeamSettings extends IBaseSettings {
	throughputHistory: number;
	useFixedDatesForThroughput: boolean;
	throughputHistoryStartDate: Date;
	throughputHistoryEndDate: Date;
	featureWIP: number;
	automaticallyAdjustFeatureWIP: boolean;
	forecastFilterRuleSetJson?: string | null;
	refinement?: IRefinementSettings | null;
}
