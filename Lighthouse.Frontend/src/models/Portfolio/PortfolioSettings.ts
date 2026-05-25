import type { IBaseSettings } from "../Common/BaseSettings";
import type { IEntityReference } from "../EntityReference";

export interface IPortfolioSettings extends IBaseSettings {
	owningTeam?: IEntityReference;

	overrideRealChildCountStates: string[];

	usePercentileToCalculateDefaultAmountOfWorkItems: boolean;
	defaultAmountOfWorkItemsPerFeature: number;
	defaultWorkItemPercentile: number;
	percentileHistoryInDays: number;
	sizeEstimateAdditionalFieldDefinitionId: number | null;
	featureOwnerAdditionalFieldDefinitionId: number | null;
}
