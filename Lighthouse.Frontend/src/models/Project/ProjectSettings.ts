import type { ITeam } from "../Team/Team";
import type { IMilestone } from "./Milestone";

export interface IProjectSettings {
	id: number;
	name: string;
	workItemTypes: string[];
	milestones: IMilestone[];
	workItemQuery: string;
	unparentedItemsQuery: string;
	involvedTeams: ITeam[];
	owningTeam?: ITeam;

	toDoStates: string[];
	doingStates: string[];
	doneStates: string[];

	overrideRealChildCountStates: string[];

	usePercentileToCalculateDefaultAmountOfWorkItems: boolean;
	defaultAmountOfWorkItemsPerFeature: number;
	defaultWorkItemPercentile: number;
	historicalFeaturesWorkItemQuery: string;

	workTrackingSystemConnectionId: number;
	sizeEstimateField?: string;
	featureOwnerField?: string;
}
