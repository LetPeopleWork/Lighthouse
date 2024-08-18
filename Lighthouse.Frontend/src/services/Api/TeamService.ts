import { BaseApiService } from './BaseApiService';
import { ITeam, Team } from '../../models/Team/Team';
import { ITeamSettings } from '../../models/Team/TeamSettings';
import { Throughput } from '../../models/Forecasts/Throughput';

export interface ITeamService {
    getTeams(): Promise<Team[]>;
    getTeam(id: number): Promise<Team | null>;
    deleteTeam(id: number): Promise<void>;
    getTeamSettings(id: number): Promise<ITeamSettings>;
    updateTeam(teamSettings: ITeamSettings): Promise<ITeamSettings>;
    createTeam(teamSettings: ITeamSettings): Promise<ITeamSettings>;
}

export class TeamService extends BaseApiService implements ITeamService {
    async getTeams(): Promise<Team[]> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<ITeam[]>('/teams');
            return response.data.map(this.deserializeTeam);
        });
    }

    async getTeam(id: number): Promise<Team | null> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<ITeam>(`/teams/${id}`);
            return this.deserializeTeam(response.data);
        });
    }

    async deleteTeam(id: number): Promise<void> {
        return this.withErrorHandling(async () => {
            await this.apiService.delete<void>(`/teams/${id}`);
        });
    }

    async getTeamSettings(id: number): Promise<ITeamSettings> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<ITeamSettings>(`/teams/${id}/settings`);
            return this.deserializeTeamSettings(response.data);
        });
    }

    async createTeam(teamSettings: ITeamSettings): Promise<ITeamSettings> {
        return await this.withErrorHandling(async () => {
            const response = await this.apiService.post<ITeamSettings>(`/teams`, teamSettings);
            return this.deserializeTeamSettings(response.data);
        });
    }

    async updateTeam(teamSettings: ITeamSettings): Promise<ITeamSettings> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.put<ITeamSettings>(`/teams/${teamSettings.id}`, teamSettings);
            return this.deserializeTeamSettings(response.data);
        });
    }

    async updateThroughput(teamId: number): Promise<void> {
        return this.withErrorHandling(async () => {
            await this.apiService.post<void>(`/throughput/${teamId}`);
        });
    }

    async getThroughput(teamId: number): Promise<Throughput> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<number[]>(`/throughput/${teamId}`);
            return new Throughput(response.data);
        });
    }

    async updateForecast(teamId: number): Promise<void> {
        await this.withErrorHandling(async () => {
            await this.apiService.post<void>(`/forecast/update/${teamId}`);
        });
    }
}