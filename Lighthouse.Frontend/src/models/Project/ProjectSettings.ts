import type { IBaseSettings } from "../Common/BaseSettings";
import type { ITeam } from "../Team/Team";
import type { IMilestone } from "./Milestone";

export interface IProjectSettings extends IBaseSettings {
	milestones: IMilestone[];
	unparentedItemsQuery: string;
	involvedTeams: ITeam[];
	owningTeam?: ITeam;

	overrideRealChildCountStates: string[];

	usePercentileToCalculateDefaultAmountOfWorkItems: boolean;
	defaultAmountOfWorkItemsPerFeature: number;
	defaultWorkItemPercentile: number;
	historicalFeaturesWorkItemQuery: string;
	sizeEstimateField?: string;
	featureOwnerField?: string;
}
