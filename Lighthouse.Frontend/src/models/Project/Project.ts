import { plainToInstance, Transform, Type } from "class-transformer";
import "reflect-metadata";
import type { IEntityReference } from "../EntityReference";
import type { IFeatureOwner } from "../IFeatureOwner";
import { type IMilestone, Milestone } from "./Milestone";

export interface IProject extends IFeatureOwner {
	involvedTeams: IEntityReference[];
	milestones: IMilestone[];
}

export class Project implements IProject {
	name!: string;
	id!: number;

	features: IEntityReference[] = [];
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

	get remainingFeatures(): number {
		return this.features.length;
	}

	static fromBackend(data: IProject): Project {
		return plainToInstance(Project, data);
	}
}
