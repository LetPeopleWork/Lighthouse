import axios from 'axios';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { WorkTrackingSystemService } from './WorkTrackingSystemService';
import { IWorkTrackingSystemConnection, WorkTrackingSystemConnection } from '../../models/WorkTracking/WorkTrackingSystemConnection';
import { WorkTrackingSystemOption } from '../../models/WorkTracking/WorkTrackingSystemOption';

vi.mock('axios');
const mockedAxios = vi.mocked(axios, true);

describe('WorkTrackingSystemService', () => {
    let workTrackingSystemService: WorkTrackingSystemService;

    beforeEach(() => {
        mockedAxios.create.mockReturnThis();
        workTrackingSystemService = new WorkTrackingSystemService();
    });

    afterEach(() => {
        vi.resetAllMocks();
    });

    it('should get supported work tracking systems', async () => {
        const mockResponse: IWorkTrackingSystemConnection[] = [
            {
                id: 2,
                name: 'Jira',
                workTrackingSystem: 'JIRA',
                options: [{ key: 'apiToken', value: 'token123', isSecret: true }]
            }
        ];

        mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

        const workTrackingSystems = await workTrackingSystemService.getWorkTrackingSystems();

        expect(workTrackingSystems).toEqual([
            new WorkTrackingSystemConnection('Jira', 'JIRA', [new WorkTrackingSystemOption('apiToken', 'token123', true)], 2)
        ]);
        expect(mockedAxios.get).toHaveBeenCalledWith('/worktrackingsystemconnections/supported');
    });

    it('should validate work tracking system connection', async () => {
        const mockConnection: IWorkTrackingSystemConnection = {
            id: 1,
            name: 'Jira',
            workTrackingSystem: 'JIRA',
            options: [{ key: 'apiToken', value: 'token123', isSecret: true }]
        };
        mockedAxios.post.mockResolvedValueOnce({ data: true });

        const isValid = await workTrackingSystemService.validateWorkTrackingSystemConnection(mockConnection);

        expect(isValid).toBe(true);
        expect(mockedAxios.post).toHaveBeenCalledWith('/worktrackingsystemconnections/validate', mockConnection);
    });

    it('should get configured work tracking systems', async () => {
        const mockResponse: IWorkTrackingSystemConnection[] = [
            {
                id: 2,
                name: 'Azure DevOps',
                workTrackingSystem: 'ADO',
                options: [{ key: 'apiToken', value: 'adoToken', isSecret: true }]
            }
        ];

        mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

        const configuredSystems = await workTrackingSystemService.getConfiguredWorkTrackingSystems();

        expect(configuredSystems).toEqual([
            new WorkTrackingSystemConnection('Azure DevOps', 'ADO', [new WorkTrackingSystemOption('apiToken', 'adoToken', true)], 2)
        ]);
        expect(mockedAxios.get).toHaveBeenCalledWith('/worktrackingsystemconnections');
    });

    it('should add a new work tracking system connection', async () => {
        const newConnection: IWorkTrackingSystemConnection = {
            id: 0,
            name: 'Jira',
            workTrackingSystem: 'JIRA',
            options: [{ key: 'apiToken', value: 'token123', isSecret: true }]
        };

        const mockResponse: IWorkTrackingSystemConnection = {
            ...newConnection,
            id: 3
        };

        mockedAxios.post.mockResolvedValueOnce({ data: mockResponse });

        const createdConnection = await workTrackingSystemService.addNewWorkTrackingSystemConnection(newConnection);

        expect(createdConnection).toEqual(new WorkTrackingSystemConnection('Jira', 'JIRA', [new WorkTrackingSystemOption('apiToken', 'token123', true)], 3));
        expect(mockedAxios.post).toHaveBeenCalledWith('/worktrackingsystemconnections', newConnection);
    });

    it('should update a work tracking system connection', async () => {
        const updatedConnection: IWorkTrackingSystemConnection = {
            id: 1,
            name: 'Jira',
            workTrackingSystem: 'JIRA',
            options: [{ key: 'apiToken', value: 'updatedToken123', isSecret: true }]
        };

        mockedAxios.put.mockResolvedValueOnce({ data: updatedConnection });

        const result = await workTrackingSystemService.updateWorkTrackingSystemConnection(updatedConnection);

        expect(result).toEqual(new WorkTrackingSystemConnection('Jira', 'JIRA', [new WorkTrackingSystemOption('apiToken', 'updatedToken123', true)], 1));
        expect(mockedAxios.put).toHaveBeenCalledWith('/worktrackingsystemconnections/1', updatedConnection);
    });

    it('should delete a work tracking system connection', async () => {
        mockedAxios.delete.mockResolvedValueOnce({});

        await workTrackingSystemService.deleteWorkTrackingSystemConnection(1);

        expect(mockedAxios.delete).toHaveBeenCalledWith('/worktrackingsystemconnections/1');
    });
});