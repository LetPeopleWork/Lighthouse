import type { Feature } from "../Feature";
import type { IFeatureOwner } from "../IFeatureOwner";
import type { ITeam, Team } from "../Team/Team";
import type { IMilestone } from "./Milestone";

export interface IProject extends IFeatureOwner {
	name: string;
	id: number;
	involvedTeams: ITeam[];
	lastUpdated: Date;
	milestones: IMilestone[];
}

export class Project implements IProject {
	name!: string;
	id!: number;

	features!: Feature[];
	involvedTeams!: Team[];
	milestones!: IMilestone[];

	lastUpdated!: Date;

	constructor(
		name: string,
		id: number,
		involvedTeams: Team[],
		features: Feature[],
		milestones: IMilestone[],
		lastUpdated: Date,
	) {
		this.name = name;
		this.id = id;
		this.involvedTeams = involvedTeams;
		this.lastUpdated = lastUpdated;
		this.features = features || [];
		this.milestones = milestones;
	}

	get remainingWork(): number {
		return this.features.reduce(
			(acc, feature) => acc + feature.getRemainingWorkForFeature(),
			0,
		);
	}

	get totalWork(): number {
		return this.features.reduce(
			(acc, feature) => acc + feature.getTotalWorkForFeature(),
			0,
		);
	}

	get remainingFeatures(): number {
		return this.features.length;
	}
}
