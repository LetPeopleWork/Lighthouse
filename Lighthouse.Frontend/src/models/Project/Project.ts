import { plainToInstance, Transform, Type } from "class-transformer";
import "reflect-metadata";
import type { IEntityReference } from "../EntityReference";
import { type IWhenForecast, WhenForecast } from "../Forecasts/WhenForecast";
import type { IFeatureOwner } from "../IFeatureOwner";
import { type IMilestone, Milestone } from "./Milestone";

export interface IProject extends IFeatureOwner {
	involvedTeams: IEntityReference[];
	milestones: IMilestone[];
	totalWorkItems: number;
	remainingWorkItems: number;
	forecasts: IWhenForecast[];
}

export class Project implements IProject {
	name!: string;
	id!: number;

	features: IEntityReference[] = [];
	involvedTeams: IEntityReference[] = [];

	tags: string[] = [];

	totalWorkItems: number = 0;
	remainingWorkItems: number = 0;

	@Transform(({ value }) => value.map(Milestone.fromBackend), {
		toClassOnly: true,
	})
	milestones: IMilestone[] = [];

	@Transform(({ value }) => value.map(WhenForecast.fromBackend), {
		toClassOnly: true,
	})
	forecasts: IWhenForecast[] = [];

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
