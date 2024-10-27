import { IMilestone } from "./Milestone";

export interface IProjectSettings {
    id: number;
    name: string;
    workItemTypes: string[];
    milestones: IMilestone[];
    workItemQuery: string;
    unparentedItemsQuery: string;
    
    toDoStates: string[];
    doingStates: string[];
    doneStates: string[];

    usePercentileToCalculateDefaultAmountOfWorkItems: boolean;
    defaultAmountOfWorkItemsPerFeature: number;
    defaultWorkItemPercentile: number;
    historicalFeaturesWorkItemQuery: string;

    workTrackingSystemConnectionId: number;
    sizeEstimateField?: string;
}
