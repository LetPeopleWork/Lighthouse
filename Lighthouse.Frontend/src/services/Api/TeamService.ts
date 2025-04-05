import type { ITeam, Team } from "../../models/Team/Team";
import type { ITeamSettings } from "../../models/Team/TeamSettings";
import { BaseApiService } from "./BaseApiService";

export interface ITeamService {
	getTeams(): Promise<Team[]>;
	getTeam(id: number): Promise<Team | null>;
	deleteTeam(id: number): Promise<void>;
	getTeamSettings(id: number): Promise<ITeamSettings>;
	validateTeamSettings(teamSettings: ITeamSettings): Promise<boolean>;
	updateTeam(teamSettings: ITeamSettings): Promise<ITeamSettings>;
	createTeam(teamSettings: ITeamSettings): Promise<ITeamSettings>;
	updateTeamData(teamId: number): Promise<void>;
}

export class TeamService extends BaseApiService implements ITeamService {
	async getTeams(): Promise<Team[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<ITeam[]>("/teams");
			return response.data
				.map(BaseApiService.deserializeTeam)
				.filter((team): team is Team => team !== null);
		});
	}

	async getTeam(id: number): Promise<Team | null> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<ITeam>(`/teams/${id}`);
			return BaseApiService.deserializeTeam(response.data);
		});
	}

	async deleteTeam(id: number): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.delete<void>(`/teams/${id}`);
		});
	}

	async getTeamSettings(id: number): Promise<ITeamSettings> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<ITeamSettings>(
				`/teams/${id}/settings`,
			);
			const teamSettings = response.data;

			teamSettings.throughputHistoryStartDate = new Date(
				response.data.throughputHistoryStartDate,
			);

			teamSettings.throughputHistoryEndDate = new Date(
				response.data.throughputHistoryEndDate,
			);

			return teamSettings;
		});
	}

	async createTeam(teamSettings: ITeamSettings): Promise<ITeamSettings> {
		return await this.withErrorHandling(async () => {
			const response = await this.apiService.post<ITeamSettings>(
				"/teams",
				teamSettings,
			);
			return response.data;
		});
	}

	async validateTeamSettings(teamSettings: ITeamSettings): Promise<boolean> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.post<boolean>(
				"/teams/validate",
				teamSettings,
			);
			return response.data;
		});
	}

	async updateTeam(teamSettings: ITeamSettings): Promise<ITeamSettings> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.put<ITeamSettings>(
				`/teams/${teamSettings.id}`,
				teamSettings,
			);
			return response.data;
		});
	}

	async updateTeamData(teamId: number): Promise<void> {
		this.withErrorHandling(async () => {
			await this.apiService.post<ITeam>(`/teams/${teamId}`);
		});
	}

	async updateForecast(teamId: number): Promise<void> {
		await this.withErrorHandling(async () => {
			await this.apiService.post<void>(`/forecast/update/${teamId}`);
		});
	}
}
