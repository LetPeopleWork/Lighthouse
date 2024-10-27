import axios from 'axios';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { SettingsService } from './SettingsService';
import { IRefreshSettings, RefreshSettings } from '../../models/AppSettings/RefreshSettings';
import { ITeamSettings } from '../../models/Team/TeamSettings';
import { IProjectSettings, ProjectSettings } from '../../models/Project/ProjectSettings';

vi.mock('axios');
const mockedAxios = vi.mocked(axios, true);

describe('SettingsService', () => {
    let settingsService: SettingsService;

    beforeEach(() => {
        mockedAxios.create.mockReturnThis();
        settingsService = new SettingsService();
    });

    afterEach(() => {
        vi.resetAllMocks();
    });

    it('should get refresh settings', async () => {
        const mockResponse: IRefreshSettings = new RefreshSettings(20, 20, 20);
        mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

        const settingName = 'exampleSetting';
        const refreshSettings = await settingsService.getRefreshSettings(settingName);

        expect(refreshSettings).toEqual(mockResponse);
        expect(mockedAxios.get).toHaveBeenCalledWith(`/appsettings/${settingName}Refresh`);
    });

    it('should update refresh settings', async () => {
        const mockRefreshSettings: IRefreshSettings = new RefreshSettings(20, 20, 20);
        mockedAxios.put.mockResolvedValueOnce({});

        const settingName = 'exampleSetting';
        await settingsService.updateRefreshSettings(settingName, mockRefreshSettings);

        expect(mockedAxios.put).toHaveBeenCalledWith(`/appsettings/${settingName}Refresh`, mockRefreshSettings);
    });

    it('should get default team settings', async () => {
        const mockResponse: ITeamSettings = {
            id: 1,
            name: "Team 1",
            throughputHistory: 30,
            featureWIP: 1,
            workItemQuery: "Query",
            workItemTypes: ["Epic"],
            workTrackingSystemConnectionId: 12,
            relationCustomField: "",
            toDoStates: ["New"],
            doingStates: ["Active"],
            doneStates: ["Done"]
        };        
        mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

        const teamSettings = await settingsService.getDefaultTeamSettings();

        expect(teamSettings).toEqual(mockResponse);
        expect(mockedAxios.get).toHaveBeenCalledWith(`/appsettings/defaultteamsettings`);
    });

    it('should update default team settings', async () => {
        const mockTeamSettings: ITeamSettings = {
            id: 1,
            name: "Team 1",
            throughputHistory: 30,
            featureWIP: 1,
            workItemQuery: "Query",
            workItemTypes: ["User Story", "Bug"],
            workTrackingSystemConnectionId: 12,
            relationCustomField: "",
            toDoStates: ["New"],
            doingStates: ["Active"],
            doneStates: ["Done"]
        };        
        mockedAxios.put.mockResolvedValueOnce({});

        await settingsService.updateDefaultTeamSettings(mockTeamSettings);

        expect(mockedAxios.put).toHaveBeenCalledWith(`/appsettings/defaultteamsettings`, mockTeamSettings);
    });

    it('should get default project settings', async () => {
        const mockResponse: IProjectSettings = new ProjectSettings(1, "Project A", ["Epic"], [], "Query", "Unparented Query", false, 15, 85, "", 2, "EstimatedSize");
        mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

        const projectSettings = await settingsService.getDefaultProjectSettings();

        expect(projectSettings).toEqual(mockResponse);
        expect(mockedAxios.get).toHaveBeenCalledWith(`/appsettings/defaultprojectsettings`);
    });

    it('should update default project settings', async () => {
        const mockProjectSettings: IProjectSettings = new ProjectSettings(1, "Project A", ["Epic"], [], "Query", "Unparented Query", false, 15, 85, "", 2, "EstimatedSize");
        mockedAxios.put.mockResolvedValueOnce({});

        await settingsService.updateDefaultProjectSettings(mockProjectSettings);

        expect(mockedAxios.put).toHaveBeenCalledWith(`/appsettings/defaultprojectsettings`, mockProjectSettings);
    });
});