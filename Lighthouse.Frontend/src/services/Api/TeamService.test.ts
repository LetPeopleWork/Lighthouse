import axios from 'axios';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { TeamService } from './TeamService';
import { ITeam, Team } from '../../models/Team/Team';
import { ITeamSettings, TeamSettings } from '../../models/Team/TeamSettings';
import { Throughput } from '../../models/Forecasts/Throughput';

vi.mock('axios');
const mockedAxios = vi.mocked(axios, true);

describe('TeamService', () => {
    let teamService: TeamService;

    beforeEach(() => {
        mockedAxios.create.mockReturnThis();
        teamService = new TeamService();
    });

    afterEach(() => {
        vi.resetAllMocks();
    });

    it('should get teams', async () => {
        const mockResponse: ITeam[] = [
            new Team("Team A", 1, [], [], 1),
            new Team("Team B", 2, [], [], 1),
        ];

        mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

        const teams = await teamService.getTeams();

        expect(teams).toEqual([            
            new Team("Team A", 1, [], [], 1),
            new Team("Team B", 2, [], [], 1),
        ]);
        expect(mockedAxios.get).toHaveBeenCalledWith('/teams');
    });

    it('should get a single team by id', async () => {
        const mockResponse: ITeam = new Team("Team A", 1, [], [], 1);

        mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

        const team = await teamService.getTeam(1);

        expect(team).toEqual(new Team("Team A", 1, [], [], 1));
        expect(mockedAxios.get).toHaveBeenCalledWith('/teams/1');
    });

    it('should return null for a missing team', async () => {
        mockedAxios.get.mockResolvedValueOnce({ data: null });

        const team = await teamService.getTeam(999);

        expect(team).toBeNull();
        expect(mockedAxios.get).toHaveBeenCalledWith('/teams/999');
    });

    it('should delete a team', async () => {
        mockedAxios.delete.mockResolvedValueOnce({});

        await teamService.deleteTeam(1);

        expect(mockedAxios.delete).toHaveBeenCalledWith('/teams/1');
    });

    it('should get team settings', async () => {
        const mockResponse: ITeamSettings = new TeamSettings(1, "Team A", 30, 1, "Query", ["Epic"], 12, "");

        mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

        const teamSettings = await teamService.getTeamSettings(1);

        expect(teamSettings).toEqual(mockResponse);
        expect(mockedAxios.get).toHaveBeenCalledWith('/teams/1/settings');
    });

    it('should create a team', async () => {
        const newTeamSettings: ITeamSettings = new TeamSettings(0, "New Team", 30, 1, "Query", ["Epic"], 12, "");
        const mockResponse: ITeamSettings = { ...newTeamSettings, id: 1 };

        mockedAxios.post.mockResolvedValueOnce({ data: mockResponse });

        const createdTeamSettings = await teamService.createTeam(newTeamSettings);

        expect(createdTeamSettings).toEqual(mockResponse);
        expect(mockedAxios.post).toHaveBeenCalledWith('/teams', newTeamSettings);
    });

    it('should update a team', async () => {
        const updatedTeamSettings: ITeamSettings = new TeamSettings(1, "Updated Team", 30, 1, "Query", ["Epic"], 12, "");

        mockedAxios.put.mockResolvedValueOnce({ data: updatedTeamSettings });

        const result = await teamService.updateTeam(updatedTeamSettings);

        expect(result).toEqual(updatedTeamSettings);
        expect(mockedAxios.put).toHaveBeenCalledWith('/teams/1', updatedTeamSettings);
    });

    it('should update throughput for a team', async () => {
        mockedAxios.post.mockResolvedValueOnce({});

        await teamService.updateThroughput(1);

        expect(mockedAxios.post).toHaveBeenCalledWith('/throughput/1');
    });

    it('should get throughput for a team', async () => {
        const mockResponse: number[] = [5, 10, 15];

        mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

        const throughput = await teamService.getThroughput(1);

        expect(throughput).toEqual(new Throughput(mockResponse));
        expect(mockedAxios.get).toHaveBeenCalledWith('/throughput/1');
    });

    it('should update forecast for a team', async () => {
        mockedAxios.post.mockResolvedValueOnce({});

        await teamService.updateForecast(1);

        expect(mockedAxios.post).toHaveBeenCalledWith('/forecast/update/1');
    });
});