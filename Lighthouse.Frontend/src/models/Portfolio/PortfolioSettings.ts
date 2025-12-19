import type { IBaseSettings } from "../Common/BaseSettings";
import type { IEntityReference } from "../EntityReference";

export interface IPortfolioSettings extends IBaseSettings {
	unparentedItemsQuery: string;
	involvedTeams: IEntityReference[];
	owningTeam?: IEntityReference;

	overrideRealChildCountStates: string[];

	usePercentileToCalculateDefaultAmountOfWorkItems: boolean;
	defaultAmountOfWorkItemsPerFeature: number;
	defaultWorkItemPercentile: number;
	percentileHistoryInDays: number;
	sizeEstimateField?: string;
	featureOwnerField?: string;
}
