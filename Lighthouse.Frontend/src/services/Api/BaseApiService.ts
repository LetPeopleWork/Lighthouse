import axios, { type AxiosInstance } from "axios";
import { Feature, type IFeature } from "../../models/Feature";
import { type IMilestone, Milestone } from "../../models/Project/Milestone";
import { type IProject, Project } from "../../models/Project/Project";
import type { IProjectSettings } from "../../models/Project/ProjectSettings";
import { type ITeam, Team } from "../../models/Team/Team";

export class BaseApiService {
	protected apiService: AxiosInstance;

	constructor() {
		let baseUrl = "/api";
		if (import.meta.env.VITE_API_BASE_URL !== undefined) {
			baseUrl = import.meta.env.VITE_API_BASE_URL;
		}

		this.apiService = axios.create({
			baseURL: baseUrl,
		});
	}

	protected async withErrorHandling<T>(
		asyncFunction: () => Promise<T>,
	): Promise<T> {
		try {
			return await asyncFunction();
		} catch (error) {
			console.error("Error during async function execution:", error);
			throw error;
		}
	}

	protected static deserializeTeam(item: ITeam) {
		if (item == null) {
			return null;
		}

		return Team.fromBackend(item);
	}

	protected static deserializeProject(item: IProject): Project {
		return Project.fromBackend(item);
	}

	protected static deserializeFeatures(featureData: IFeature[]): Feature[] {
		return featureData.map((feature: IFeature) => {
			return Feature.fromBackend(feature);
		});
	}

	protected deserializeProjectSettings(
		item: IProjectSettings,
	): IProjectSettings {
		const milestones = item.milestones.map((milestone: IMilestone) => {
			return Milestone.fromBackend(milestone);
		});

		item.milestones = milestones;

		return item;
	}
}
