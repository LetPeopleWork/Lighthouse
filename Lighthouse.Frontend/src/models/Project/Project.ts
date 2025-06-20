import { plainToInstance, Transform, Type } from "class-transformer";
import "reflect-metadata";
import type { IEntityReference } from "../EntityReference";
import { Feature, type IFeature } from "../Feature";
import type { IFeatureOwner } from "../IFeatureOwner";
import { Team } from "../Team/Team";
import { type IMilestone, Milestone } from "./Milestone";

export interface IProject extends IFeatureOwner {
	involvedTeams: IEntityReference[];
	milestones: IMilestone[];

	get remainingWork(): number;
	get totalWork(): number;
	get remainingFeatures(): number;
}

export class Project implements IProject {
	name!: string;
	id!: number;

	@Type(() => Feature)
	@Transform(({ value }) => value.map(Feature.fromBackend), {
		toClassOnly: true,
	})
	features: IFeature[] = [];

	@Transform(({ value }) => value.map(Team.fromBackend), { toClassOnly: true })
	involvedTeams: IEntityReference[] = [];

	tags: string[] = [];

	@Transform(({ value }) => value.map(Milestone.fromBackend), {
		toClassOnly: true,
	})
	milestones: IMilestone[] = [];

	@Type(() => Date)
	lastUpdated: Date = new Date();

	serviceLevelExpectationProbability = 0;
	serviceLevelExpectationRange = 0;

	systemWIPLimit = 0;

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

	static fromBackend(data: IProject): Project {
		return plainToInstance(Project, data);
	}
}
