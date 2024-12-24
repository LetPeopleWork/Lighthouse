import { APIRequestContext } from '@playwright/test';

export async function createTeam(api: APIRequestContext, name: string, workTrackingSystemConnectionId: number)
    : Promise<{ id: number, name: string }> {


    const response = await api.post('/api/Teams', {
        data: {
            id: 0,
            name: name,
            throughputHistory: 30,
            featureWIP: 1,
            workItemQuery: `System.TeamProject = ${name}`,
            workItemTypes: ['User Story', 'Bug'],
            toDoStates: ['New'],
            inProgressStates: ['Active', 'Resolved'],
            doneStates: ['Closed'],
            relationCustomField: '',
            automaticallyAdjustFeatureWIP: false,
            workTrackingSystemConnectionId: workTrackingSystemConnectionId,
        }
    });
    return response.json();
}

export async function deleteTeam(api: APIRequestContext, teamId: number): Promise<void> {
    await api.delete(`/api/Teams/${teamId}`);
}