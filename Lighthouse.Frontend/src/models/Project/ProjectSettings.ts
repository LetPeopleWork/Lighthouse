import { IMilestone } from "./Milestone";

export interface IProjectSettings {
    id: number;
    name: string;
    workItemTypes: string[];
    milestones: IMilestone[];
    workItemQuery: string;
    unparentedItemsQuery: string;
    defaultAmountOfWorkItemsPerFeature: number;
    workTrackingSystemConnectionId: number;
    sizeEstimateField: string;
}


export class ProjectSettings implements IProjectSettings {
    id: number;
    name: string;
    workItemTypes: string[];
    milestones: IMilestone[];
    workItemQuery: string;
    unparentedItemsQuery: string;
    defaultAmountOfWorkItemsPerFeature: number;
    workTrackingSystemConnectionId: number;
    sizeEstimateField: string;

    constructor(
        id: number,
        name: string,
        workItemTypes: string[],
        milestones: IMilestone[],
        workItemQuery: string,
        unparentedItemsQuery: string,
        defaultAmountOfWorkItemsPerFeature: number,
        workTrackingSystemConnectionId: number,
        sizeEstimateField: string
    ) {
        this.id = id;
        this.name = name;
        this.workItemTypes = workItemTypes;
        this.milestones = milestones;
        this.workItemQuery = workItemQuery;
        this.unparentedItemsQuery = unparentedItemsQuery;
        this.defaultAmountOfWorkItemsPerFeature = defaultAmountOfWorkItemsPerFeature;
        this.workTrackingSystemConnectionId = workTrackingSystemConnectionId;
        this.sizeEstimateField = sizeEstimateField;
    }
}
