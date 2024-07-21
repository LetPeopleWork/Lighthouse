import { IMilestone } from "./Milestone";

export interface IProjectSetting {
    id: number;
    name: string;
    workItemTypes: string[];
    milestones: IMilestone[];
    workItemQuery: string;
    unparentedItemsQuery: string;
    defaultAmountOfWorkItemsPerFeature: number;
    workTrackingSystemConnectionId: number;
}


export class ProjectSetting implements IProjectSetting {
    id: number;
    name: string;
    workItemTypes: string[];
    milestones: IMilestone[];
    workItemQuery: string;
    unparentedItemsQuery: string;
    defaultAmountOfWorkItemsPerFeature: number;
    workTrackingSystemConnectionId: number;

    constructor(
        id: number,
        name: string,
        workItemTypes: string[],
        milestones: IMilestone[],
        workItemQuery: string,
        unparentedItemsQuery: string,
        defaultAmountOfWorkItemsPerFeature: number,
        workTrackingSystemConnectionId: number
    ) {
        this.id = id;
        this.name = name;
        this.workItemTypes = workItemTypes;
        this.milestones = milestones;
        this.workItemQuery = workItemQuery;
        this.unparentedItemsQuery = unparentedItemsQuery;
        this.defaultAmountOfWorkItemsPerFeature = defaultAmountOfWorkItemsPerFeature;
        this.workTrackingSystemConnectionId = workTrackingSystemConnectionId;
    }
}
