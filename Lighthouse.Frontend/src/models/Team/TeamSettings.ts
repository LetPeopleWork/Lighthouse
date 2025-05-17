import type { IBaseSettings } from "../Common/BaseSettings";

export interface ITeamSettings extends IBaseSettings {
	throughputHistory: number;
	useFixedDatesForThroughput: boolean;
	throughputHistoryStartDate: Date;
	throughputHistoryEndDate: Date;
	featureWIP: number;
	relationCustomField: string;
	automaticallyAdjustFeatureWIP: boolean;
}
