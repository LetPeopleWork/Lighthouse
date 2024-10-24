import axios from 'axios';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { ProjectService } from './ProjectService';
import { IProject, Project } from '../../models/Project/Project';
import { IProjectSettings, ProjectSettings } from '../../models/Project/ProjectSettings';

vi.mock('axios');
const mockedAxios = vi.mocked(axios, true);

describe('ProjectService', () => {
    let projectService: ProjectService;

    beforeEach(() => {
        mockedAxios.create.mockReturnThis();
        projectService = new ProjectService();
    });

    afterEach(() => {
        vi.resetAllMocks();
    });

    it('should get all projects', async () => {
        const mockResponse: IProject[] = [
            new Project("Project 1", 1, [], [], [], new Date('2023-09-01T12:00:00Z'))
        ];

        mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

        const projects = await projectService.getProjects();

        expect(projects).toEqual([
            new Project('Project 1', 1, [], [], [], new Date('2023-09-01T12:00:00Z'))
        ]);
        expect(mockedAxios.get).toHaveBeenCalledWith('/projects');
    });

    it('should get a project by id', async () => {
        const mockProject: IProject = new Project("Project 1", 1, [], [], [], new Date('2023-09-01T12:00:00Z'));

        mockedAxios.get.mockResolvedValueOnce({ data: mockProject });

        const project = await projectService.getProject(1);

        expect(project).toEqual(
            new Project('Project 1', 1, [], [], [], new Date('2023-09-01T12:00:00Z'))
        );
        expect(mockedAxios.get).toHaveBeenCalledWith('/projects/1');
    });

    it('should delete a project by id', async () => {
        mockedAxios.delete.mockResolvedValueOnce({});

        await projectService.deleteProject(1);

        expect(mockedAxios.delete).toHaveBeenCalledWith('/projects/1');
    });

    it('should get project settings by id', async () => {
        const mockSettings: IProjectSettings = new ProjectSettings(1, "ProjectSetting", ["Epic"], [], "Query", "Unparented Query", false, 10, 85, "", 0, "Size");

        mockedAxios.get.mockResolvedValueOnce({ data: mockSettings });

        const settings = await projectService.getProjectSettings(1);

        expect(settings).toEqual(mockSettings);
        expect(mockedAxios.get).toHaveBeenCalledWith('/projects/1/settings');
    });

    it('should update project settings', async () => {
        const projectSettings: IProjectSettings = new ProjectSettings(1, "ProjectSetting", ["Epic"], [], "Query", "Unparented Query", false, 10, 85, "", 0, "Size");

        mockedAxios.put.mockResolvedValueOnce({ data: projectSettings });

        const updatedSettings = await projectService.updateProject(projectSettings);

        expect(updatedSettings).toEqual(projectSettings);
        expect(mockedAxios.put).toHaveBeenCalledWith('/projects/1', projectSettings);
    });

    it('should create a new project', async () => {
        const newProjectSettings: IProjectSettings = new ProjectSettings(1, "ProjectSetting", ["Epic"], [], "Query", "Unparented Query", false, 10, 85, "", 0, "Size");

        const mockResponse: IProjectSettings = new ProjectSettings(2, "ProjectSetting", ["Epic"], [], "Query", "Unparented Query", false, 10, 85, "", 0, "Size");

        mockedAxios.post.mockResolvedValueOnce({ data: mockResponse });

        const createdSettings = await projectService.createProject(newProjectSettings);

        expect(createdSettings).toEqual(mockResponse);
        expect(mockedAxios.post).toHaveBeenCalledWith('/projects', newProjectSettings);
    });

    it('should refresh features for a project by id', async () => {
        const mockProject: IProject = new Project("Project 1", 1, [], [], [], new Date('2023-09-01T12:00:00Z'));

        mockedAxios.post.mockResolvedValueOnce({ data: mockProject });

        const refreshedProject = await projectService.refreshFeaturesForProject(1);

        expect(refreshedProject).toEqual(
            new Project('Project 1', 1, [], [], [], new Date('2023-09-01T12:00:00Z'))
        );
        expect(mockedAxios.post).toHaveBeenCalledWith('/projects/refresh/1');
    });

    it('should refresh forecasts for a project by id', async () => {
        const mockProject: IProject = new Project("Project 1", 1, [], [], [], new Date('2023-09-01T12:00:00Z'));

        mockedAxios.post.mockResolvedValueOnce({ data: mockProject });

        const refreshedProject = await projectService.refreshForecastsForProject(1);

        expect(refreshedProject).toEqual(
            new Project('Project 1', 1, [], [], [], new Date('2023-09-01T12:00:00Z'))
        );
        expect(mockedAxios.post).toHaveBeenCalledWith('/forecast/update/1');
    });
});