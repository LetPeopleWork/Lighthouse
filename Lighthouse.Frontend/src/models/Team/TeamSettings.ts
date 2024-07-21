export interface ITeamSettings {
    id: number;
    name: string;
    throughputHistory: number;
    featureWIP: number;
    workItemQuery: string;
    workItemTypes: string[];
    workTrackingSystemConnectionId: number;
    relationCustomField: string;
}

export class TeamSettings implements ITeamSettings {
    id: number;
    name: string;
    throughputHistory: number;
    featureWIP: number;
    workItemQuery: string;
    workItemTypes: string[];
    workTrackingSystemConnectionId: number;
    relationCustomField: string;

    constructor(
        id: number,
        name: string,
        throughputHistory: number,
        featureWIP: number,
        workItemQuery: string,
        workItemTypes: string[],
        workTrackingSystemConnectionId: number,
        relationCustomField: string
    ) {
        this.id = id;
        this.name = name;
        this.throughputHistory = throughputHistory;
        this.featureWIP = featureWIP;
        this.workItemQuery = workItemQuery;
        this.workItemTypes = workItemTypes;
        this.workTrackingSystemConnectionId = workTrackingSystemConnectionId;
        this.relationCustomField = relationCustomField;
    }
}
