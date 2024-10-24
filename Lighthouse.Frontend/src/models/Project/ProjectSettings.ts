import { IMilestone } from "./Milestone";

export interface IProjectSettings {
    id: number;
    name: string;
    workItemTypes: string[];
    milestones: IMilestone[];
    workItemQuery: string;
    unparentedItemsQuery: string;

    usePercentileToCalculateDefaultAmountOfWorkItems: boolean;
    defaultAmountOfWorkItemsPerFeature: number;
    defaultWorkItemPercentile: number;
    historicalFeaturesWorkItemQuery: string;

    workTrackingSystemConnectionId: number;
    sizeEstimateField?: string;
}


export class ProjectSettings implements IProjectSettings {
    id: number;
    name: string;
    workItemTypes: string[];
    milestones: IMilestone[];
    workItemQuery: string;
    unparentedItemsQuery: string;

    usePercentileToCalculateDefaultAmountOfWorkItems: boolean;
    defaultAmountOfWorkItemsPerFeature: number;
    defaultWorkItemPercentile: number;
    historicalFeaturesWorkItemQuery: string;

    workTrackingSystemConnectionId: number;
    sizeEstimateField?: string;

    constructor(
        id: number,
        name: string,
        workItemTypes: string[],
        milestones: IMilestone[],
        workItemQuery: string,
        unparentedItemsQuery: string,
        usePercentileToCalculateDefaultAmountOfWorkItems: boolean,
        defaultAmountOfWorkItemsPerFeature: number,
        defaultWorkItemPercentile: number,
        historicalFeaturesWorkItemQuery: string,
        workTrackingSystemConnectionId: number,
        sizeEstimateField: string = ""
    ) {
        this.id = id;
        this.name = name;
        this.workItemTypes = workItemTypes;
        this.milestones = milestones;
        this.workItemQuery = workItemQuery;
        this.unparentedItemsQuery = unparentedItemsQuery;
        this.usePercentileToCalculateDefaultAmountOfWorkItems = usePercentileToCalculateDefaultAmountOfWorkItems;
        this.defaultAmountOfWorkItemsPerFeature = defaultAmountOfWorkItemsPerFeature;
        this.defaultWorkItemPercentile = defaultWorkItemPercentile;
        this.historicalFeaturesWorkItemQuery = historicalFeaturesWorkItemQuery;
        this.workTrackingSystemConnectionId = workTrackingSystemConnectionId;
        this.sizeEstimateField = sizeEstimateField;
    }
}
