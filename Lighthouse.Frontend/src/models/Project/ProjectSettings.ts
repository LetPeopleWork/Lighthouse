import type { IBaseSettings } from "../Common/BaseSettings";
import type { IEntityReference } from "../EntityReference";
import type { IMilestone } from "./Milestone";

export interface IProjectSettings extends IBaseSettings {
	milestones: IMilestone[];
	unparentedItemsQuery: string;
	involvedTeams: IEntityReference[];
	owningTeam?: IEntityReference;

	overrideRealChildCountStates: string[];

	usePercentileToCalculateDefaultAmountOfWorkItems: boolean;
	defaultAmountOfWorkItemsPerFeature: number;
	defaultWorkItemPercentile: number;
	historicalFeaturesWorkItemQuery: string;
	sizeEstimateField?: string;
	featureOwnerField?: string;
}
